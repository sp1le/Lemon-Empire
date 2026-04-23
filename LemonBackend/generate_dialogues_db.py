import os
import json
import asyncio
import random
from openai import AsyncOpenAI
import time
from dotenv import load_dotenv

load_dotenv()

# Убедись, что в .env прописан GEMINI_API_KEY
API_URL = os.getenv("LLM_BASE_URL", "https://generativelanguage.googleapis.com/v1beta/openai/")
API_KEY = os.getenv("GEMINI_API_KEY", "")

client = AsyncOpenAI(
    base_url=API_URL,
    api_key=API_KEY
)

# Модель, у которой есть лимит 500 в день на AI Studio (на момент апреля 2024)
MODEL_NAME = "gemini-3.1-flash-lite-preview"
DB_FILE = "standard_dialogues.json"

ARCHETYPES = []
for morale in ["low", "normal", "high"]:
    for reaction in ["low", "normal", "high"]:
        for sales_event in ["normal", "haggle", "2for1"]:
            ARCHETYPES.append((morale, reaction, sales_event))

VARIATIONS_PER_ARCHETYPE = 18  # Итого ~486 диалогов
TOTAL_GOAL = len(ARCHETYPES) * VARIATIONS_PER_ARCHETYPE

def load_existing_db():
    if os.path.exists(DB_FILE):
        try:
            with open(DB_FILE, "r", encoding="utf-8") as f:
                data = json.load(f)
                return data.get("dialogues", [])
        except:
            return []
    return []

def save_db(dialogues):
    with open(DB_FILE, "w", encoding="utf-8") as f:
        json.dump({"dialogues": dialogues}, f, ensure_ascii=False, indent=2)

async def generate_single_tree(morale, reaction, sales_event):
    if morale == "low":
        morale_desc = "крайне подавленным, грустным, выглядит унизительно"
    elif morale == "high":
        morale_desc = "очень радостным, энергичным, светится от счастья"
    else:
        morale_desc = "обычным, нейтральным"

    if reaction == "low":
        reaction_desc = "Покупатель относится к продавцу с недоверием и пренебрежением"
    elif reaction == "high":
        reaction_desc = "Покупатель очень уважает продавца и доверяет ему"
    else:
        reaction_desc = "Покупатель относится нейтрально"

    if sales_event == "haggle":
        bargain_rule = 'Торг: Покупатель просит скидку. Опции: 1 вариант (agree_discount) - согласиться на скидку, 2 вариант (normal) - отказать в скидке.'
    elif sales_event == "2for1":
        bargain_rule = 'Акция: Продавец решает предложить акцию. Опции: 1 вариант (bargain_2_for_1) - предложить 2 по цене 1, 2 вариант (normal) - обычная продажа.'
    else:
        bargain_rule = 'Обычная продажа: Оба варианта ответа (normal) - простая продажа ЛИМОНАДА без акций.'

    desperate_rule = 'Если Мораль "low", один вариант ответа на первом или втором этапе должен быть жалким (type: "desperate" вместо normal).'

    prompt = f"""Сгенерируй случайное дерево диалога о продаже ЛИМОНАДА.
Вводные:
Мораль продавца: {morale} (Выглядит: {morale_desc}).
Репутация у покупателя: {reaction} ({reaction_desc}).
Событие торговли: {sales_event} ({bargain_rule}).
{desperate_rule}

Задача: Написать дерево развития диалога в формате JSON.
- У продавца всегда 2 варианта ответов (опция 0 и опция 1).
- На втором этапе ПОКУПАТЕЛЬ отвечает по-разному в зависимости от того, что выбрал продавец!

ОБРАЗЕЦ JSON:
{{
    "condition_morale": "{morale}",
    "condition_reaction": "{reaction}",
    "condition_sales_event": "{sales_event}",
    "step1_npc_greeting": "Эй, продавец! Ты выглядишь грустно. Почем лимонад?",
    "step1_options": [
        {{"id": 0, "text": "Всего 100 баксов. Берете?", "type": "normal"}},
        {{"id": 1, "text": "Умоляю, купите хоть один, я умираю с голоду...", "type": "desperate"}}
    ],
    "step2_response_to_0": "Дороговато, но давай.",
    "step2_options_0": [
        {{"id": 0, "text": "Спасибо за покупку!", "type": "normal"}},
        {{"id": 1, "text": "Отличный выбор.", "type": "normal"}}
    ],
    "step2_response_to_1": "Жалкое зрелище. Ну ладно, давай сюда.",
    "step2_options_1": [
        {{"id": 0, "text": "Век не забуду вашей доброты!", "type": "desperate"}},
        {{"id": 1, "text": "Держите.", "type": "normal"}}
    ]
}}
ВНИМАНИЕ: Сгенерируй ПРИНЦИПИАЛЬНО НОВЫЙ ТЕКСТ. Учитывай все правила продажи. Ответь строго в формате JSON без markdown:"""

    try:
        response = await client.chat.completions.create(
            model=MODEL_NAME,
            messages=[{"role": "user", "content": prompt}],
            temperature=1.0,
        )
        content = response.choices[0].message.content.strip()
        import re
        content = re.sub(r'^```[a-zA-Z]*\n', '', content)
        content = re.sub(r'\n```$', '', content)
        content = content.strip()
        match = re.search(r'\{.*\}', content, re.DOTALL)
        if match:
            content = match.group(0)
        
        data = json.loads(content)
        # Базовая валидация структуры
        if "step1_npc_greeting" in data and "step2_response_to_0" in data:
            return data
    except Exception as e:
        err = str(e).lower()
        if "503" in err or "high demand" in err:
            return "RETRY_503"
        if "429" in err or "rate limit" in err:
            return "RETRY_429"
        print(f"Ошибка API: {e}")
    return None

async def main():
    if not API_KEY:
        print("Ошибка: GEMINI_API_KEY не установлен!")
        return

    # 1. Загружаем то что уже есть
    current_dialogues = load_existing_db()
    print(f"База загружена. Уже готово: {len(current_dialogues)} / {TOTAL_GOAL} диалогов.")

    # 2. Считаем, сколько нам не хватает для каждого архетипа
    counts = {}
    for (m, r, s) in ARCHETYPES:
        counts[(m, r, s)] = 0
    
    for d in current_dialogues:
        key = (d["condition_morale"], d["condition_reaction"], d["condition_sales_event"])
        if key in counts:
            counts[key] += 1

    # 3. Формируем список задач (только то, чего не хватает)
    tasks_to_run = []
    for (m, r, s), current_count in counts.items():
        needed = VARIATIONS_PER_ARCHETYPE - current_count
        for _ in range(needed):
            tasks_to_run.append((m, r, s))

    if not tasks_to_run:
        print("Вся база уже сгенерирована! Работать не над чем.")
        return

    print(f"Осталось сгенерировать: {len(tasks_to_run)} диалогов.")
    random.shuffle(tasks_to_run) # Чтобы не идти по порядку и разнообразить базу

    # 4. Цикл генерации батчами
    batch_size = 2
    for i in range(0, len(tasks_to_run), batch_size):
        batch = tasks_to_run[i:i+batch_size]
        
        print(f"Обработка батча {i//batch_size + 1}... ", end="", flush=True)
        
        coroutines = [generate_single_tree(m, r, s) for (m, r, s) in batch]
        results = await asyncio.gather(*coroutines)
        
        batch_success = 0
        quota_hit = False

        for res in results:
            if isinstance(res, dict):
                current_dialogues.append(res)
                batch_success += 1
            elif res in ["RETRY_503", "RETRY_429"]:
                quota_hit = True

        # Сразу сохраняем прогресс
        if batch_success > 0:
            save_db(current_dialogues)
            print(f"Успешно: +{batch_success} (Всего: {len(current_dialogues)})")

        if quota_hit:
            print("\n[!] ВНИМАНИЕ: Лимит API или перегрузка сервера. Останавливаем работу, чтобы не тратить RPD.")
            print("Попробуй запустить скрипт через пару часов или завтра. Весь текущий прогресс сохранен.")
            break

        # Пауза для соблюдения RPM (15 в минуту)
        await asyncio.sleep(8)

    print(f"\nРабота завершена. Итого в базе: {len(current_dialogues)} диалогов.")

if __name__ == "__main__":
    asyncio.run(main())
