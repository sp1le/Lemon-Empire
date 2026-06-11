using UnityEngine;
using LemonEmpire.Core;

namespace LemonEmpire.Trading
{
    public static class DialogueGenerator
    {
        public static string GenerateLabelInspectionDialogue(NPCBuyer npc, ItemBase bottle, out bool wantsHaggle, out int discountAmount)
        {
            wantsHaggle = false;
            discountAmount = 0;

            if (npc == null || bottle == null)
            {
                return "Хм, странный напиток...";
            }

            // 1. Packaging reaction
            string pkgReaction = "";
            bool pkgRejected = false;

            if (npc.Archetype == NPCArchetype.Hipsters)
            {
                if (bottle.Packaging == PackagingType.Glass)
                {
                    pkgReaction = "Ух ты, стекло! Настоящий крафт, экологично и стильно.";
                }
                else
                {
                    pkgReaction = "Пластик или жестянка? Это несерьезно, я пью только из стекла!";
                    pkgRejected = true;
                }
            }
            else if (npc.Archetype == NPCArchetype.Kids)
            {
                if (bottle.Packaging == PackagingType.Plastic)
                {
                    pkgReaction = "О, пластиковая бутылка! Её удобно держать и пить на ходу!";
                }
                else
                {
                    pkgReaction = "Ну, упаковка обычная. Но в пластике было бы удобнее.";
                }
            }
            else if (npc.Archetype == NPCArchetype.Athletes)
            {
                if (bottle.Packaging == PackagingType.Can)
                {
                    pkgReaction = "Жестяная банка! Выглядит как стильный профессиональный изотоник!";
                }
                else
                {
                    pkgReaction = "Хм, обычная тара. Спортивная банка смотрелась бы круче.";
                }
            }
            else // Party Animals
            {
                pkgReaction = "Упаковка нормальная, главное — что внутри!";
            }

            if (pkgRejected)
            {
                wantsHaggle = false;
                discountAmount = 0;
                return $"[Тара] {pkgReaction}\n[Вердикт] Я это точно не возьму. До свидания!";
            }

            // 2. Stats evaluation & absolute violations
            System.Collections.Generic.List<string> statComments = new System.Collections.Generic.List<string>();

            // Alcohol checks (absolute violations)
            if (npc.Archetype == NPCArchetype.Kids && bottle.Alcohol > 0f)
            {
                wantsHaggle = false;
                discountAmount = 0;
                return $"[Тара] {pkgReaction}\n[Характеристики] Эй! Тут целых {bottle.Alcohol:F0}% алкоголя! Детям такое продавать нельзя!\n[Вердикт] Я ухожу и пожалуюсь родителям!";
            }
            if (npc.Archetype == NPCArchetype.Athletes && bottle.Alcohol > 0f)
            {
                wantsHaggle = false;
                discountAmount = 0;
                return $"[Тара] {pkgReaction}\n[Характеристики] В этом напитке {bottle.Alcohol:F0}% алкоголя? Спорт и алкоголь несовместимы!\n[Вердикт] Нет, спасибо, мне тренироваться надо.";
            }
            if (npc.Archetype == NPCArchetype.Hipsters && bottle.Alcohol > 5f)
            {
                wantsHaggle = false;
                discountAmount = 0;
                return $"[Тара] {pkgReaction}\n[Характеристики] Тут целых {bottle.Alcohol:F0}% алкоголя! Это не легкий крафт, а тяжелое пойло!\n[Вердикт] Отвратительно, я такое не пью.";
            }

            // Match checking
            bool matchesSugar = bottle.Sugar >= npc.MinSugar && bottle.Sugar <= npc.MaxSugar;
            bool matchesAlcohol = bottle.Alcohol >= npc.MinAlcohol && bottle.Alcohol <= npc.MaxAlcohol;
            bool matchesCarbonation = bottle.Carbonation >= npc.MinCarbonation && bottle.Carbonation <= npc.MaxCarbonation;
            bool matchesPkg = !npc.PreferredPackaging.HasValue || bottle.Packaging == npc.PreferredPackaging.Value;

            // Generate specific comment lines
            // Sugar comments
            if (npc.Archetype == NPCArchetype.Kids)
            {
                if (matchesSugar)
                    statComments.Add($"Целых {bottle.Sugar:F0}% сахара! Будет супер-сладко и вкусно!");
                else
                    statComments.Add($"Тут всего {bottle.Sugar:F0}% сахара... Маловато сладости.");
            }
            else if (npc.Archetype == NPCArchetype.Athletes)
            {
                if (matchesSugar)
                    statComments.Add($"Отлично, сахара минимум ({bottle.Sugar:F0}%) — никакой лишней глюкозы.");
                else
                    statComments.Add($"Ого, {bottle.Sugar:F0}% сахара! Это же чистый удар по моей форме, слишком сладко!");
            }
            else if (npc.Archetype == NPCArchetype.Hipsters)
            {
                if (matchesSugar)
                    statComments.Add($"Содержание сахара ({bottle.Sugar:F0}%) в идеальном балансе для крафта.");
                else
                    statComments.Add($"Сахар ({bottle.Sugar:F0}%) нарушает рецептуру, баланс вкуса сбит.");
            }
            else // Party Animals
            {
                statComments.Add($"Сахара здесь {bottle.Sugar:F0}% — сойдет для запивки.");
            }

            // Carbonation comments
            if (npc.Archetype == NPCArchetype.Kids)
            {
                if (matchesCarbonation)
                    statComments.Add($"Обожаю пузырьки! ({bottle.Carbonation:F0}% газов — то что надо!)");
                else
                    statComments.Add($"Газов маловато ({bottle.Carbonation:F0}%), почти не шипит.");
            }
            else if (npc.Archetype == NPCArchetype.Athletes)
            {
                if (matchesCarbonation)
                    statComments.Add($"Газация слабая ({bottle.Carbonation:F0}%), не вызовет спазмов при беге.");
                else
                    statComments.Add($"Слишком сильно газировано ({bottle.Carbonation:F0}%) — живот скрутит на тренировке!");
            }
            else if (npc.Archetype == NPCArchetype.PartyAnimals)
            {
                if (matchesCarbonation)
                    statComments.Add($"Да! Вот это газы ({bottle.Carbonation:F0}%)! Настоящий шипучий взрыв!");
                else
                    statComments.Add($"Слишком вялые газы ({bottle.Carbonation:F0}%), почти выдохся.");
            }

            // Alcohol comments for Party Animals
            if (npc.Archetype == NPCArchetype.PartyAnimals)
            {
                if (matchesAlcohol)
                    statComments.Add($"О, {bottle.Alcohol:F0}% алкоголя! Вот это я понимаю — топливо для вечеринки!");
                else
                    statComments.Add($"Всего {bottle.Alcohol:F0}% алкоголя? Вы издеваетесь, это же вода!");
            }
            else if (bottle.Alcohol == 0f)
            {
                statComments.Add("Напиток полностью безалкогольный.");
            }

            // Temperature comments
            bool matchesTemp = bottle.Temperature <= npc.MaxTemperature;
            if (matchesTemp)
            {
                if (bottle.Temperature < 10f)
                {
                    statComments.Add($"Ого, прохладный (всего {bottle.Temperature:F1}°C), отлично освежает!");
                }
                else if (bottle.Temperature < 20f)
                {
                    statComments.Add($"Напиток умеренной температуры ({bottle.Temperature:F1}°C).");
                }
                else
                {
                    statComments.Add($"Уф, теплый лимонад ({bottle.Temperature:F1}°C)... Не помешало бы охладить.");
                }
            }
            else
            {
                statComments.Add($"Ужас! На улице жара, а лимонад теплый ({bottle.Temperature:F1}°C)! Мне нужно холоднее {npc.MaxTemperature:F0}°C!");
            }

            string statsReaction = string.Join("\n", statComments);

            // Calculate match count
            int matchCount = 0;
            int totalExpectedMatches = 4;
            if (matchesSugar) matchCount++;
            if (matchesAlcohol) matchCount++;
            if (matchesCarbonation) matchCount++;
            if (matchesPkg) matchCount++;

            if (npc.MaxTemperature < 100f)
            {
                totalExpectedMatches++;
                if (matchesTemp) matchCount++;
            }

            string verdictText = "";
            float toleranceMultiplier = bottle.Temperature <= 12f ? 1.3f : 1.0f;

            if (matchCount == totalExpectedMatches)
            {
                if (bottle.RetailPrice <= 25 * toleranceMultiplier)
                {
                    verdictText = "Лимонад просто идеальный! И цена отличная, беру без лишних слов!";
                }
                else if (bottle.RetailPrice <= 45 * toleranceMultiplier)
                {
                    wantsHaggle = true;
                    discountAmount = Mathf.RoundToInt(bottle.RetailPrice * 0.3f);
                    verdictText = $"Напиток шикарен, но цена (${bottle.RetailPrice}) кусается. Сделаешь скидку в ${discountAmount}?";
                }
                else
                {
                    verdictText = $"Рецепт отличный, но ${bottle.RetailPrice} — это грабеж! Я не готов столько платить.";
                }
            }
            else if (matchCount >= (totalExpectedMatches / 2))
            {
                if (bottle.RetailPrice <= 15 * toleranceMultiplier)
                {
                    verdictText = "Не все параметры совпали, но цена копеечная. Забираю!";
                }
                else if (bottle.RetailPrice <= 30 * toleranceMultiplier)
                {
                    wantsHaggle = true;
                    discountAmount = Mathf.RoundToInt(bottle.RetailPrice * 0.4f);
                    verdictText = $"Неплохо, но не идеально. Скинешь ${discountAmount} от цены в ${bottle.RetailPrice}?";
                }
                else
                {
                    verdictText = $"Параметры средние, а цена в ${bottle.RetailPrice} слишком высока за такой напиток.";
                }
            }
            else
            {
                verdictText = "Этот напиток мне совсем не подходит. Поищу что-нибудь другое.";
            }

            return $"[Тара] {pkgReaction}\n[Характеристики]\n{statsReaction}\n[Вердикт] {verdictText}";
        }
    }
}
