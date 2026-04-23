using UnityEngine;
using UnityEngine.UI;
using LemonEmpire.Core;

namespace LemonEmpire.UI
{
    public class VitalsUI : MonoBehaviour
    {
        private static VitalsUI _instance;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AutoCreate()
        {
            if (_instance != null) return;
            var go = new GameObject("VitalsUI");
            _instance = go.AddComponent<VitalsUI>();
            DontDestroyOnLoad(go);
        }

        private Image _satietyFill;
        private Image _moraleFill;
        private Image _lookFill;
        private Text _satietyText;
        private Text _moraleText;
        private Text _lookText;

        private readonly Color _satietyColor = new Color(0.98f, 0.78f, 0.25f);       // amber
        private readonly Color _satietyLowColor = new Color(0.85f, 0.25f, 0.20f);     // red
        private readonly Color _moraleColor = new Color(0.30f, 0.82f, 0.60f);          // teal
        private readonly Color _moraleLowColor = new Color(0.60f, 0.30f, 0.15f);      // brown
        private readonly Color _lookColor = new Color(0.40f, 0.70f, 0.95f);            // blue
        private readonly Color _bgColor = new Color(0.10f, 0.10f, 0.14f, 0.85f);
        private readonly Color _barBgColor = new Color(0.18f, 0.18f, 0.22f, 1f);

        private void Start()
        {
            BuildUI();
            RefreshUI();

            if (PlayerVitals.Instance != null)
                PlayerVitals.Instance.OnVitalsChanged += RefreshUI;
        }

        private void OnDestroy()
        {
            if (PlayerVitals.Instance != null)
                PlayerVitals.Instance.OnVitalsChanged -= RefreshUI;
        }

        private void RefreshUI()
        {
            var v = PlayerVitals.Instance;
            if (v == null) return;

            // Satiety
            float satNorm = v.Satiety / 100f;
            _satietyFill.rectTransform.anchorMax = new Vector2(satNorm, 1f);
            _satietyFill.color = Color.Lerp(_satietyLowColor, _satietyColor, satNorm);
            _satietyText.text = $"{Mathf.CeilToInt(v.Satiety)}";

            // Morale
            float morNorm = v.Morale / 100f;
            _moraleFill.rectTransform.anchorMax = new Vector2(morNorm, 1f);
            _moraleFill.color = Color.Lerp(_moraleLowColor, _moraleColor, morNorm);
            _moraleText.text = $"{Mathf.CeilToInt(v.Morale)}";

            // Look (range -30..+50, normalize to 0..1)
            float lookNorm = Mathf.InverseLerp(-30f, 50f, v.LookScore);
            _lookFill.rectTransform.anchorMax = new Vector2(lookNorm, 1f);
            _lookText.text = $"{(v.LookScore >= 0 ? "+" : "")}{v.LookScore:F0}";
        }

        private void BuildUI()
        {
            // Canvas
            var canvasGo = new GameObject("VitalsCanvas");
            canvasGo.transform.SetParent(transform);
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 90;
            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            canvasGo.AddComponent<GraphicRaycaster>();

            // Container panel — top-left
            var container = CreatePanel(canvasGo.transform, "VitalsContainer",
                new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(20, -100), new Vector2(260, -20));
            var containerRT = container.GetComponent<RectTransform>();
            containerRT.pivot = new Vector2(0, 1);
            containerRT.sizeDelta = new Vector2(240, 130);

            var containerImg = container.GetComponent<Image>();
            containerImg.color = _bgColor;

            // Round corners feel — we'll use a simple solid bg

            // Title
            CreateLabel(container.transform, "Title", "СОСТОЯНИЕ",
                new Vector2(12, -6), new Vector2(228, 24),
                12, new Color(0.6f, 0.6f, 0.65f));

            // Bars
            float yStart = -32f;
            float barHeight = 22f;
            float spacing = 30f;

            // Satiety bar
            CreateBarRow(container.transform, "Satiety", "🍔",
                yStart, barHeight, _satietyColor,
                out _satietyFill, out _satietyText);

            // Morale bar
            CreateBarRow(container.transform, "Morale", "😤",
                yStart - spacing, barHeight, _moraleColor,
                out _moraleFill, out _moraleText);

            // Look bar
            CreateBarRow(container.transform, "Look", "👔",
                yStart - spacing * 2, barHeight, _lookColor,
                out _lookFill, out _lookText);
        }

        private void CreateBarRow(Transform parent, string name, string icon,
            float yPos, float height, Color fillColor,
            out Image fillImg, out Text valueText)
        {
            float leftPad = 12f;
            float rightPad = 12f;
            float iconWidth = 24f;
            float valueWidth = 36f;
            float barLeft = leftPad + iconWidth + 6;
            float barRight = 240 - rightPad - valueWidth - 6;
            float barWidth = barRight - barLeft;

            // Icon
            CreateLabel(parent, name + "_Icon", icon,
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
            bgImg.color = _barBgColor;

            // Bar fill
            var fillGo = new GameObject(name + "_Fill");
            fillGo.transform.SetParent(barBg.transform, false);
            var fillRT = fillGo.AddComponent<RectTransform>();
            fillRT.anchorMin = Vector2.zero;
            fillRT.anchorMax = Vector2.one;
            fillRT.offsetMin = Vector2.zero;
            fillRT.offsetMax = Vector2.zero;

            fillImg = fillGo.AddComponent<Image>();
            fillImg.color = fillColor;
            fillImg.type = Image.Type.Simple;

            // Value text
            valueText = CreateLabel(parent, name + "_Value", "100",
                new Vector2(barRight + 6, yPos), new Vector2(valueWidth, height),
                13, new Color(0.85f, 0.85f, 0.9f));
            valueText.alignment = TextAnchor.MiddleRight;
        }

        private Text CreateLabel(Transform parent, string name, string content,
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

        private GameObject CreatePanel(Transform parent, string name,
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
