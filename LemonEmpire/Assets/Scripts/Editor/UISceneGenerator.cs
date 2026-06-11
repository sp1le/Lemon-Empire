using UnityEngine;
using UnityEngine.UI;
using UnityEditor;

namespace LemonEmpire.Editor
{
    public class UISceneGenerator : EditorWindow
    {
        [MenuItem("Lemon Empire/Generate UI on Scene")]
        public static void ShowWindow()
        {
            GetWindow<UISceneGenerator>("UI Generator");
        }

        private void OnGUI()
        {
            GUILayout.Label("UI Scene Generator", EditorStyles.boldLabel);
            GUILayout.Space(10);

            EditorGUILayout.HelpBox(
                "После генерации UI:\n" +
                "1. Назначьте GameHUD скрипт на объект GameHUD\n" +
                "2. Назначьте VitalsUI скрипт на объект VitalsUI\n" +
                "3. Назначьте DialogueUI скрипт на объект DialogueUI\n" +
                "4. Перетащите UI элементы в SerializeField в Inspector",
                MessageType.Info);

            GUILayout.Space(10);

            if (GUILayout.Button("Generate Game HUD", GUILayout.Height(40)))
            {
                GenerateGameHUD();
            }

            GUILayout.Space(5);

            if (GUILayout.Button("Generate Vitals UI", GUILayout.Height(40)))
            {
                GenerateVitalsUI();
            }

            GUILayout.Space(5);

            if (GUILayout.Button("Generate Dialogue UI", GUILayout.Height(40)))
            {
                GenerateDialogueUI();
            }

            GUILayout.Space(5);

            if (GUILayout.Button("Generate ALL UI", GUILayout.Height(60)))
            {
                GenerateGameHUD();
                GenerateVitalsUI();
                GenerateDialogueUI();
            }
        }

        private static void GenerateGameHUD()
        {
            var existing = GameObject.Find("GameHUD");
            if (existing != null)
            {
                if (!EditorUtility.DisplayDialog("GameHUD exists",
                    "GameHUD already exists. Replace it?", "Yes", "No"))
                    return;
                DestroyImmediate(existing);
            }

            var hudRoot = new GameObject("GameHUD");
            Undo.RegisterCreatedObjectUndo(hudRoot, "Create GameHUD");

            // Canvas
            var canvasGO = new GameObject("HUDCanvas");
            canvasGO.transform.SetParent(hudRoot.transform);
            var canvas = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 50;
            var scaler = canvasGO.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            canvasGO.AddComponent<GraphicRaycaster>();

            // Balance (top-left) - Balance_Text
            var balanceText = CreateLabel(canvasGO.transform, "Balance", "$ 500",
                new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1),
                new Vector2(20, -20), new Vector2(240, 50),
                28, new Color(0.6f, 0.95f, 0.6f), TextAnchor.MiddleLeft,
                new Color(0.10f, 0.10f, 0.14f, 0.85f));
            balanceText.name = "Balance_Text";

            // Time (top-right) - Time_Text
            var timeText = CreateLabel(canvasGO.transform, "Time", "День 1 - 08:00",
                new Vector2(1, 1), new Vector2(1, 1), new Vector2(1, 1),
                new Vector2(-20, -20), new Vector2(250, 50),
                28, Color.white, TextAnchor.MiddleRight,
                new Color(0.10f, 0.10f, 0.14f, 0.85f));
            timeText.name = "Time_Text";

            // Prompt (bottom-center) - Prompt_Text
            var promptText = CreateLabel(canvasGO.transform, "Prompt", "[E] Взаимодействовать",
                new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0.5f, 0),
                new Vector2(0, 80), new Vector2(400, 45),
                22, Color.white, TextAnchor.MiddleCenter,
                new Color(0.10f, 0.10f, 0.14f, 0.85f));
            promptText.name = "Prompt_Text";
            promptText.transform.parent.gameObject.SetActive(false);

            // Carry (top-center) - Carry_Text
            var carryText = CreateLabel(canvasGO.transform, "Carry", "Лимон",
                new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0.5f, 1),
                new Vector2(0, -20), new Vector2(300, 40),
                20, new Color(1f, 0.85f, 0.3f), TextAnchor.MiddleCenter,
                new Color(0.10f, 0.10f, 0.14f, 0.85f));
            carryText.name = "Carry_Text";
            carryText.transform.parent.gameObject.SetActive(false);

            // Summary Panel
            CreateSummaryPanel(canvasGO.transform);

            Debug.Log("<color=green>GameHUD generated! Назначьте GameHUD.cs скрипт и перетащите UI элементы в Inspector.</color>");
            Selection.activeGameObject = hudRoot;
        }

        private static void CreateSummaryPanel(Transform parent)
        {
            var summaryPanel = new GameObject("SummaryPanel", typeof(RectTransform));
            summaryPanel.transform.SetParent(parent, false);
            var rt = summaryPanel.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            var img = summaryPanel.AddComponent<Image>();
            img.color = new Color(0, 0, 0, 0.8f);

            // Title - Sum_Title_Text
            var titleText = CreateLabel(summaryPanel.transform, "Sum_Title", "СМЕНА ЗАВЕРШЕНА",
                new Vector2(0.5f, 0.7f), new Vector2(0.5f, 0.7f), new Vector2(0.5f, 0.5f),
                Vector2.zero, new Vector2(600, 80),
                42, Color.white, TextAnchor.MiddleCenter, Color.clear);
            titleText.name = "Sum_Title_Text";

            // Stats - Sum_Stats_Text
            var statsText = CreateLabel(summaryPanel.transform, "Sum_Stats",
                "Выручка: $0\nРасходы: $0\nПрибыль: $0\n\nПродано бутылок: 0",
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                Vector2.zero, new Vector2(500, 300),
                26, new Color(0.8f, 0.8f, 0.8f), TextAnchor.MiddleCenter,
                new Color(0.1f, 0.1f, 0.1f, 0.8f));
            statsText.name = "Sum_Stats_Text";

            // Button - Sum_Btn
            var btnGO = new GameObject("Sum_Btn", typeof(RectTransform));
            btnGO.transform.SetParent(summaryPanel.transform, false);
            var btnRT = btnGO.GetComponent<RectTransform>();
            btnRT.anchorMin = new Vector2(0.5f, 0.3f);
            btnRT.anchorMax = new Vector2(0.5f, 0.3f);
            btnRT.pivot = new Vector2(0.5f, 0.5f);
            btnRT.sizeDelta = new Vector2(300, 60);

            var btnImg = btnGO.AddComponent<Image>();
            btnImg.color = new Color(0.2f, 0.6f, 0.2f);
            btnGO.AddComponent<Button>();

            CreateLabel(btnGO.transform, "BtnTxt", "НАЧАТЬ СЛЕДУЮЩИЙ ДЕНЬ",
                Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f),
                Vector2.zero, Vector2.zero, 18, Color.white, TextAnchor.MiddleCenter, Color.clear);

            summaryPanel.SetActive(false);
        }

        private static void GenerateVitalsUI()
        {
            var existing = GameObject.Find("VitalsUI");
            if (existing != null)
            {
                if (!EditorUtility.DisplayDialog("VitalsUI exists",
                    "VitalsUI already exists. Replace it?", "Yes", "No"))
                    return;
                DestroyImmediate(existing);
            }

            var vitalsRoot = new GameObject("VitalsUI");
            Undo.RegisterCreatedObjectUndo(vitalsRoot, "Create VitalsUI");

            // Canvas
            var canvasGo = new GameObject("VitalsCanvas");
            canvasGo.transform.SetParent(vitalsRoot.transform);
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 90;
            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            canvasGo.AddComponent<GraphicRaycaster>();

            // Container panel
            var container = CreatePanel(canvasGo.transform, "VitalsContainer",
                new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(20, -100), new Vector2(240, 130));
            var containerImg = container.GetComponent<Image>();
            containerImg.color = new Color(0.10f, 0.10f, 0.14f, 0.85f);

            // Title
            CreateSimpleLabel(container.transform, "Title", "СОСТОЯНИЕ",
                new Vector2(12, -6), new Vector2(228, 24),
                12, new Color(0.6f, 0.6f, 0.65f));

            // Bars
            float yStart = -32f;
            float barHeight = 22f;
            float spacing = 30f;

            CreateBarRow(container.transform, "Satiety", "🍔",
                yStart, barHeight, new Color(0.98f, 0.78f, 0.25f));

            CreateBarRow(container.transform, "Morale", "😤",
                yStart - spacing, barHeight, new Color(0.30f, 0.82f, 0.60f));

            CreateBarRow(container.transform, "Look", "👔",
                yStart - spacing * 2, barHeight, new Color(0.40f, 0.70f, 0.95f));

            Debug.Log("<color=green>VitalsUI generated! Назначьте VitalsUI.cs скрипт и перетащите UI элементы в Inspector.</color>");
            Selection.activeGameObject = vitalsRoot;
        }

        private static void CreateBarRow(Transform parent, string name, string icon,
            float yPos, float height, Color fillColor)
        {
            float leftPad = 12f;
            float rightPad = 12f;
            float iconWidth = 24f;
            float valueWidth = 36f;
            float barLeft = leftPad + iconWidth + 6;
            float barRight = 240 - rightPad - valueWidth - 6;
            float barWidth = barRight - barLeft;

            // Icon
            CreateSimpleLabel(parent, name + "_Icon", icon,
                new Vector2(leftPad, yPos), new Vector2(iconWidth, height),
                16, Color.white);

            // Bar background
            var barBg = new GameObject(name + "_BarBG");
            barBg.transform.SetParent(parent, false);
            var bgRT = barBg.AddComponent<RectTransform>();
            bgRT.anchorMin = new Vector2(0, 1);
            bgRT.anchorMax = new Vector2(0, 1);
            bgRT.pivot = new Vector2(0, 1);
            bgRT.anchoredPosition = new Vector2(barLeft, yPos);
            bgRT.sizeDelta = new Vector2(barWidth, height);
            var bgImg = barBg.AddComponent<Image>();
            bgImg.color = new Color(0.18f, 0.18f, 0.22f, 1f);

            // Bar fill - {name}_Fill
            var fillGo = new GameObject(name + "_Fill");
            fillGo.transform.SetParent(barBg.transform, false);
            var fillRT = fillGo.AddComponent<RectTransform>();
            fillRT.anchorMin = Vector2.zero;
            fillRT.anchorMax = Vector2.one;
            fillRT.offsetMin = Vector2.zero;
            fillRT.offsetMax = Vector2.zero;

            var fillImg = fillGo.AddComponent<Image>();
            fillImg.color = fillColor;
            fillImg.type = Image.Type.Simple;

            // Value text - {name}_Value
            var valueText = CreateSimpleLabel(parent, name + "_Value", "100",
                new Vector2(barRight + 6, yPos), new Vector2(valueWidth, height),
                13, new Color(0.85f, 0.85f, 0.9f));
            valueText.alignment = TextAnchor.MiddleRight;
        }

        private static void GenerateDialogueUI()
        {
            var existing = GameObject.Find("DialogueUI");
            if (existing != null)
            {
                if (!EditorUtility.DisplayDialog("DialogueUI exists",
                    "DialogueUI already exists. Replace it?", "Yes", "No"))
                    return;
                DestroyImmediate(existing);
            }

            var dialogueRoot = new GameObject("DialogueUI");
            Undo.RegisterCreatedObjectUndo(dialogueRoot, "Create DialogueUI");

            var canvasGo = new GameObject("DialogueCanvas");
            canvasGo.transform.SetParent(dialogueRoot.transform);
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 200;
            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            canvasGo.AddComponent<GraphicRaycaster>();

            var panel = new GameObject("DialoguePanel");
            panel.transform.SetParent(canvasGo.transform, false);
            var panelRT = panel.AddComponent<RectTransform>();
            panelRT.anchorMin = new Vector2(0.15f, 0.05f);
            panelRT.anchorMax = new Vector2(0.85f, 0.4f);
            panelRT.offsetMin = Vector2.zero;
            panelRT.offsetMax = Vector2.zero;

            var panelImg = panel.AddComponent<Image>();
            panelImg.color = new Color(0.08f, 0.08f, 0.12f, 0.95f);

            // NPC Name - NPCName
            var npcNameText = CreateSimpleText(panel.transform, "NPCName",
                new Vector2(0.02f, 0.8f), new Vector2(0.98f, 0.98f),
                "NPC Name", 22, Color.yellow, TextAnchor.MiddleLeft);

            // NPC Dialogue Text - NPCText
            var npcText = CreateSimpleText(panel.transform, "NPCText",
                new Vector2(0.02f, 0.45f), new Vector2(0.98f, 0.78f),
                "Привет! Что продаёшь?", 18, Color.white, TextAnchor.UpperLeft);

            // Reply Buttons (3) - Reply_0, Reply_1, Reply_2
            for (int i = 0; i < 3; i++)
            {
                float yMax = 0.4f - i * 0.13f;
                float yMin = yMax - 0.11f;

                var btnGo = new GameObject($"Reply_{i}");
                btnGo.transform.SetParent(panel.transform, false);
                var btnRT = btnGo.AddComponent<RectTransform>();
                btnRT.anchorMin = new Vector2(0.02f, yMin);
                btnRT.anchorMax = new Vector2(0.98f, yMax);
                btnRT.offsetMin = Vector2.zero;
                btnRT.offsetMax = Vector2.zero;

                var btnImg = btnGo.AddComponent<Image>();
                btnImg.color = new Color(0.15f, 0.15f, 0.22f, 1f);

                var btn = btnGo.AddComponent<Button>();
                var colors = btn.colors;
                colors.highlightedColor = new Color(0.3f, 0.3f, 0.5f);
                colors.pressedColor = new Color(0.4f, 0.4f, 0.2f);
                btn.colors = colors;

                // Reply text - Reply_{i}_Text
                var replyText = CreateSimpleText(btnGo.transform, $"Reply_{i}_Text",
                    Vector2.zero, Vector2.one,
                    $"Вариант ответа {i + 1}", 16, new Color(0.9f, 0.9f, 0.85f), TextAnchor.MiddleLeft);
            }

            panel.SetActive(false);

            Debug.Log("<color=green>DialogueUI generated! Назначьте DialogueUI.cs скрипт и перетащите UI элементы в Inspector.</color>");
            Selection.activeGameObject = dialogueRoot;
        }

        // Helper methods
        private static Text CreateLabel(Transform parent, string name, string text,
            Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot,
            Vector2 position, Vector2 size,
            int fontSize, Color textColor, TextAnchor alignment, Color bgColor)
        {
            var bgGO = new GameObject(name + "_BG", typeof(RectTransform));
            bgGO.transform.SetParent(parent, false);
            var bgRT = bgGO.GetComponent<RectTransform>();
            bgRT.anchorMin = anchorMin;
            bgRT.anchorMax = anchorMax;
            bgRT.pivot = pivot;
            bgRT.anchoredPosition = position;
            bgRT.sizeDelta = size;

            var bgImg = bgGO.AddComponent<Image>();
            bgImg.color = bgColor;

            var txtGO = new GameObject(name + "_Text", typeof(RectTransform));
            txtGO.transform.SetParent(bgGO.transform, false);
            var txtRT = txtGO.GetComponent<RectTransform>();
            txtRT.anchorMin = Vector2.zero;
            txtRT.anchorMax = Vector2.one;
            txtRT.offsetMin = new Vector2(8, 0);
            txtRT.offsetMax = new Vector2(-8, 0);

            var txt = txtGO.AddComponent<Text>();
            txt.text = text;
            txt.fontSize = fontSize;
            txt.color = textColor;
            txt.alignment = alignment;
            txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            return txt;
        }

        private static Text CreateSimpleLabel(Transform parent, string name, string content,
            Vector2 position, Vector2 size, int fontSize, Color color)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0, 1);
            rt.anchorMax = new Vector2(0, 1);
            rt.pivot = new Vector2(0, 1);
            rt.anchoredPosition = position;
            rt.sizeDelta = size;

            var text = go.AddComponent<Text>();
            text.text = content;
            text.fontSize = fontSize;
            text.color = color;
            text.alignment = TextAnchor.MiddleLeft;
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Overflow;

            return text;
        }

        private static Text CreateSimpleText(Transform parent, string name,
            Vector2 anchorMin, Vector2 anchorMax,
            string content, int fontSize, Color color, TextAnchor alignment)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.offsetMin = new Vector2(10, 0);
            rt.offsetMax = new Vector2(-10, 0);

            var text = go.AddComponent<Text>();
            text.text = content;
            text.fontSize = fontSize;
            text.color = color;
            text.alignment = alignment;
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Truncate;

            return text;
        }

        private static GameObject CreatePanel(Transform parent, string name,
            Vector2 anchorMin, Vector2 anchorMax,
            Vector2 anchoredPos, Vector2 sizeDelta)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.pivot = new Vector2(0, 1);
            rt.anchoredPosition = anchoredPos;
            rt.sizeDelta = sizeDelta;

            go.AddComponent<Image>();
            return go;
        }
    }
}
