import os
import json
from openai import AsyncOpenAI
from dotenv import load_dotenv
from app.schemas.models import PlayerStateRequest, NpcResponse, DialogOption

# Загружаем ключи из файла .env
load_dotenv()

# По умолчанию направляем на локальный сервер (например Ollama или LM Studio)
# Но если переопределить LLM_BASE_URL (напр. грок) и ключ, то заработает с другими
client = AsyncOpenAI(
    base_url=os.getenv("LLM_BASE_URL", "https://generativelanguage.googleapis.com/v1beta/openai/"),
    api_key=os.getenv("LLM_API_KEY", os.getenv("GEMINI_API_KEY", "your-gemini-key-missing"))
)

# Для быстрой генерации и точного JSON - gemini-2.5-flash
MODEL_NAME = os.getenv("LLM_MODEL", "gemini-2.5-flash")

async def generate_dialogue(player_data: PlayerStateRequest) -> NpcResponse:
    # 1. Формирование динамического описания
    char_desc = []
    if player_data.morale > 75:
        char_desc.append("очень радостным и энергичным")
    elif player_data.morale < 30:
        char_desc.append("крайне подавленным и грустным")
        
    if player_data.outfit:
        char_desc.append(f"одет в '{player_data.outfit}'")
        
    desc_str = ", ".join(char_desc) if char_desc else "ничем не выделяется"

    if player_data.dialogue_step == 1:
        # ЭТАП 1: Знакомство и Реакция на статы
        prompt = f"""Сценарий для видеоигры. Главный герой — продавец ЛИМОНАДА. К нему подходит ПОКУПАТЕЛЬ.

Характеристики продавца: {desc_str} (НЕ упоминай еду и голод, только эти характеристики).
Приветливость (репутация): {player_data.reaction_score}/100

ЗАДАЧА ЭТАПА 1:
1. `npc_greeting` — ПОКУПАТЕЛЬ здоровается и комментирует внешний вид или настроение продавца.
2. `player_options` — 2 РАЗНЫХ варианта ответа ПРОДАВЦА (тип "normal"). Продавец отвечает на комментарий и спрашивает, будет ли покупатель ЛИМОНАД.
Искуственный Интеллект НЕ ИМЕЕТ ПРАВА копировать пример! Напиши свой текст про ЛИМОНАД!

ПРИМЕР СТРУКТУРЫ JSON (ВНИМАНИЕ: ЗДЕСЬ ПРИМЕР ПРО ПРОДАЖУ ЯБЛОК. ТЕБЕ НУЖНО НАПИСАТЬ ПРО ПРОДАЖУ ЛИМОНАДА!):
{{
    "npc_greeting": "Здарова! Какая у тебя странная шляпа. Дела идут плохо?",
    "player_options": [
        {{"id": 1, "text": "Нормальная шляпа! Будешь покупать мои яблоки или нет?", "type": "normal"}},
        {{"id": 2, "text": "Да, времена тяжелые... не хотите купить свежее яблочко?", "type": "normal"}}
    ]
}}
Ответь строго в формате JSON, используя твои собственные фразы про ЛИМОНАД:"""
    else:
        # ЭТАП 2: Торг и Сделка
        import random
        if player_data.haggle_discount > 0:
            haggle_text = f'ПОКУПАТЕЛЬ хочет купить лимонад, но нагло просит четкую скидку в {player_data.haggle_discount} долларов! (ОБЯЗАТЕЛЬНО упомяни эту сумму денег).'
            bargain_rule = f'Один ответ продавца: согласиться на скидку в {player_data.haggle_discount}$ (укажи type: "agree_discount"). Второй ответ: отказаться делать скидку (type: "normal").'
            example_json = """{
    "npc_greeting": "Беру! Но только если сделаешь скидку на эти яблоки в 10 баксов.",
    "player_options": [
        {"id": 1, "text": "Яблоки стоят своих денег, никаких скидок.", "type": "normal"},
        {"id": 2, "text": "Хорошо, держи яблоко со скидкой в 10$.", "type": "agree_discount"}
    ]
}"""
        else:
            haggle_text = 'ПОКУПАТЕЛЬ готов купить товар.'
            offer_2_for_1 = random.random() < 0.3
            if offer_2_for_1:
                bargain_rule = 'Один из ответов продавца: предложить акцию "Два по цене одного" (укажи type: "bargain_2_for_1"). Второй: обычная продажа (type: "normal").'
                example_json = """{
    "npc_greeting": "Отлично, я готов купить эти яблоки.",
    "player_options": [
        {"id": 1, "text": "Смотри, акция: два яблока по цене одного!", "type": "bargain_2_for_1"},
        {"id": 2, "text": "Отлично, покупай яблоко.", "type": "normal"}
    ]
}"""
            else:
                bargain_rule = 'Все варианты ответа продавца — обычное согласие продать товар (укажи type: "normal").'
                example_json = """{
    "npc_greeting": "Супер! Давай свои яблоки.",
    "player_options": [
        {"id": 1, "text": "Вот ваше яблоко, с вас 50 баксов.", "type": "normal"},
        {"id": 2, "text": "Отличный выбор, держите яблоко.", "type": "normal"}
    ]
}"""

        prompt = f"""Сценарий для видеоигры. Главный герой — продавец ЛИМОНАДА (Игрок).
Диалог с ПОКУПАТЕЛЕМ продолжается.
ПРОДАВЕЦ ранее сказал: "{player_data.player_reply}"

ЗАДАЧА ЭТАПА 2:
1. `npc_greeting` — ПОКУПАТЕЛЬ реагирует на предыдущую фразу продавца. {haggle_text}
2. `player_options` — 2 варианта ответа ПРОДАВЦА.
3. {bargain_rule}
4. Если Мораль ({player_data.morale}) меньше 20, один вариант ответа может быть жалким (type: "desperate" вместо normal).
Искуственный Интеллект НЕ ИМЕЕТ ПРАВА копировать пример! Напиши свой текст про ЛИМОНАД!

ПРИМЕР СТРУКТУРЫ JSON (ВНИМАНИЕ: ЗДЕСЬ ПРИМЕР ПРО ПРОДАЖУ ЯБЛОК. ТЕБЕ НУЖНО НАПИСАТЬ ПРО ПРОДАЖУ ЛИМОНАДА ВМЕСТО ЯБЛОК!):
{example_json}
ВЕСЬ ТЕКСТ НА РУССКОМ ЯЗЫКЕ. Ответь строго в формате JSON, используя твои собственные фразы про ЛИМОНАД:"""

    try:
        response = await client.chat.completions.create(
            model=MODEL_NAME,
            messages=[
                {"role": "system", "content": "You are a helpful assistant. You must reply strictly in Russian language and output only valid JSON."},
                {"role": "user", "content": prompt}
            ],
            temperature=0.7,
            timeout=180.0
        )

        # Защита от мусора или пустого ответа:
        content = response.choices[0].message.content
        if not content:
            print("Внимание: Модель вернула пустой ответ. Проверьте логи или фильтры безопасности.")
            response_text = ""
        else:
            response_text = content.strip()
            # Удаляем маркеры markdown
            import re
            response_text = re.sub(r'^```[a-zA-Z]*\n', '', response_text)
            response_text = re.sub(r'\n```$', '', response_text)
            response_text = response_text.strip()
            
            match = re.search(r'\{.*\}', response_text, re.DOTALL)
            if match:
                response_text = match.group(0)
                
        print(f"[LLM DEBUG] Raw Text: {response_text}")

        # Парсим текст ответа в словарь
        data = json.loads(response_text)

        # Pydantic сам проверит структуру
        return NpcResponse(**data)

    except Exception as e:
        print(f"Ошибка LLM: {e}")
        try:
            with open("error.log", "w", encoding="utf-8") as f:
                import traceback
                f.write(traceback.format_exc())
        except:
            pass

        return NpcResponse(
            npc_greeting="*NPC погружен в свои мысли...*",
            player_options=[
                DialogOption(id=1, text="Эй, постой!", type="normal"),
                DialogOption(id=2, text="[Уйти]", type="normal")
            ]
        )