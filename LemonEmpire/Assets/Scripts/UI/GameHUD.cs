using UnityEngine;
using UnityEngine.UI;
using LemonEmpire.Core;
using LemonEmpire.Player;

namespace LemonEmpire.UI
{

    public class GameHUD : MonoBehaviour
    {
        private Text _balanceText;
        private Text _timeText;
        private Text _promptText;
        private Text _carryText;

        private PlayerInteraction _interaction;
        private PlayerCarry _carry;
        private Font _font;

        private void Start()
        {

            _font = Font.CreateDynamicFontFromOSFont("Arial", 14);
            if (_font == null)
            {
                string[] fonts = Font.GetOSInstalledFontNames();
                if (fonts.Length > 0)
                    _font = Font.CreateDynamicFontFromOSFont(fonts[0], 14);
            }

            BuildHUD();

            var player = FindFirstObjectByType<PlayerController>();
            if (player != null)
            {
                _interaction = player.GetComponent<PlayerInteraction>();
                _carry = player.GetComponent<PlayerCarry>();
            }

            if (EconomyManager.Instance != null)
                EconomyManager.Instance.OnBalanceChanged += UpdateBalance;
        }

        private void OnDestroy()
        {
            if (EconomyManager.Instance != null)
                EconomyManager.Instance.OnBalanceChanged -= UpdateBalance;
        }

        private void Update()
        {
            UpdatePrompt();
            UpdateCarryDisplay();
            UpdateTime();
        }

        private void UpdateTime()
        {
            if (_timeText != null && TimeManager.Instance != null)
            {
                int h = Mathf.FloorToInt(TimeManager.Instance.CurrentTimeOfDay);
                int m = Mathf.FloorToInt((TimeManager.Instance.CurrentTimeOfDay - h) * 60f);
                _timeText.text = $"День {TimeManager.Instance.CurrentDay} - {h:00}:{m:00}";
            }
        }

        private void UpdateBalance(int balance)
        {
            if (_balanceText != null)
                _balanceText.text = $"$ {balance}";
        }

        private void UpdatePrompt()
        {
            if (_promptText == null || _interaction == null) return;

            string prompt = _interaction.GetCurrentPrompt();
            if (!string.IsNullOrEmpty(prompt))
            {
                _promptText.text = prompt;
                _promptText.transform.parent.gameObject.SetActive(true);
            }
            else
            {
                _promptText.transform.parent.gameObject.SetActive(false);
            }
        }

        private void UpdateCarryDisplay()
        {
            if (_carryText == null || _carry == null) return;

            if (_carry.IsCarrying)
            {
                _carryText.text = _carry.CarriedItem.DisplayName;
                _carryText.transform.parent.gameObject.SetActive(true);
            }
            else
            {
                _carryText.transform.parent.gameObject.SetActive(false);
            }
        }

        private void BuildHUD()
        {
            var canvasGO = new GameObject("HUDCanvas");
            canvasGO.transform.SetParent(transform);
            var canvas = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 50;
            var scaler = canvasGO.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            canvasGO.AddComponent<UnityEngine.UI.GraphicRaycaster>();

            _balanceText = CreateLabel(canvasGO.transform, "Balance", "$ 500",
                new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1),
                new Vector2(20, -20), new Vector2(250, 50),
                28, new Color(0.6f, 0.95f, 0.6f), TextAnchor.MiddleLeft,
                new Color(0, 0, 0, 0.5f));

            _timeText = CreateLabel(canvasGO.transform, "Time", "День 1 - 08:00",
                new Vector2(1, 1), new Vector2(1, 1), new Vector2(1, 1),
                new Vector2(-20, -20), new Vector2(250, 50),
                28, Color.white, TextAnchor.MiddleRight,
                new Color(0, 0, 0, 0.5f));

            _promptText = CreateLabel(canvasGO.transform, "Prompt", "",
                new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0.5f, 0),
                new Vector2(0, 80), new Vector2(400, 45),
                22, Color.white, TextAnchor.MiddleCenter,
                new Color(0, 0, 0, 0.6f));
            _promptText.transform.parent.gameObject.SetActive(false);

            _carryText = CreateLabel(canvasGO.transform, "Carry", "",
                new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0.5f, 1),
                new Vector2(0, -20), new Vector2(300, 40),
                20, new Color(1f, 0.85f, 0.3f), TextAnchor.MiddleCenter,
                new Color(0, 0, 0, 0.4f));
            _carryText.transform.parent.gameObject.SetActive(false);

            BuildSummaryUI(canvasGO.transform);
        }

        private GameObject _summaryPanel;
        private Text _summaryTitle;
        private Text _summaryStats;
        private Button _summaryBtn;

        private void BuildSummaryUI(Transform parent)
        {
            _summaryPanel = new GameObject("SummaryPanel", typeof(RectTransform));
            _summaryPanel.transform.SetParent(parent, false);
            var rt = _summaryPanel.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            var img = _summaryPanel.AddComponent<Image>();
            img.color = new Color(0, 0, 0, 0.8f);

            _summaryTitle = CreateLabel(_summaryPanel.transform, "Sum_Title", "СМЕНА ЗАВЕРШЕНА",
                new Vector2(0.5f, 0.7f), new Vector2(0.5f, 0.7f), new Vector2(0.5f, 0.5f),
                Vector2.zero, new Vector2(600, 80),
                42, Color.white, TextAnchor.MiddleCenter, Color.clear);

            _summaryStats = CreateLabel(_summaryPanel.transform, "Sum_Stats", "Выручка: $0\nРасходы: $0\nПрибыль: $0\n\nПродано бутылок: 0",
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                Vector2.zero, new Vector2(500, 300),
                26, new Color(0.8f, 0.8f, 0.8f), TextAnchor.MiddleCenter, new Color(0.1f, 0.1f, 0.1f, 0.8f));

            var btnGO = new GameObject("Sum_Btn", typeof(RectTransform));
            btnGO.transform.SetParent(_summaryPanel.transform, false);
            var btnRT = btnGO.GetComponent<RectTransform>();
            btnRT.anchorMin = new Vector2(0.5f, 0.3f);
            btnRT.anchorMax = new Vector2(0.5f, 0.3f);
            btnRT.pivot = new Vector2(0.5f, 0.5f);
            btnRT.sizeDelta = new Vector2(300, 60);

            var btnImg = btnGO.AddComponent<Image>();
            btnImg.color = new Color(0.2f, 0.6f, 0.2f);
            _summaryBtn = btnGO.AddComponent<Button>();

            CreateLabel(btnGO.transform, "BtnTxt", "НАЧАТЬ СЛЕДУЮЩИЙ ДЕНЬ",
                Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f),
                Vector2.zero, Vector2.zero, 18, Color.white, TextAnchor.MiddleCenter, Color.clear);

            _summaryBtn.onClick.AddListener(() => {
                _summaryPanel.SetActive(false);
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
                TimeManager.Instance?.StartShift();
            });

            _summaryPanel.SetActive(false);
        }

        public void ShowDaySummary(TimeManager timeMan, EconomyManager ecoMan)
        {
            if (_summaryPanel == null) return;

            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            int profit = ecoMan.DailyRevenue - ecoMan.DailyExpenses;

            _summaryTitle.text = $"ДЕНЬ {timeMan.CurrentDay} ЗАВЕРШЁН";
            _summaryStats.text =
                $"Выручка: <color=#88ff88>+${ecoMan.DailyRevenue}</color>\n" +
                $"Расходы: <color=#ff8888>${ecoMan.DailyExpenses}</color>\n" +
                $"Прибыль: <b>${profit}</b>\n\n" +
                $"Продано бутылок: {ecoMan.BottlesSold}";

            _summaryPanel.SetActive(true);
        }

        private Text CreateLabel(Transform parent, string name, string text,
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
            txt.font = _font;

            return txt;
        }
    }
}
