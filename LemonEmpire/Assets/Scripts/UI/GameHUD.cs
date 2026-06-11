using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using LemonEmpire.Core;
using LemonEmpire.Player;

namespace LemonEmpire.UI
{
    public class GameHUD : MonoBehaviour
    {
        public static GameHUD Instance { get; private set; }

        private PlayerInteraction _interaction;
        private PlayerCarry _carry;

        public static string ActiveOrderText = "";

        // UI Toolkit elements
        private VisualElement _hudRoot;
        private Label _balanceLabel;
        private Label _timeLabel;
        private Label _satietyPercentText;
        private VisualElement _satietyProgressFill;
        private Label _moralePercentText;
        private VisualElement _moraleProgressFill;
        private VisualElement _cleanlinessFill;
        private Label _trendText;
        private VisualElement _ordersContainer;
        private VisualElement _promptPill;
        private Label _promptLabel;
        private VisualElement _carryPill;
        private Label _carryLabel;

        // Summary overlay (UI Toolkit)
        private VisualElement _summaryOverlay;
        private Label _summaryDayLabel;
        private Label _summaryRevenueValue;
        private Label _summaryExpensesValue;
        private Label _summaryProfitValue;
        private Label _summaryBottlesLabel;

        // Strange States overlays (UI Toolkit)
        private VisualElement _sugarRushOverlay;
        private VisualElement _paranoiaOverlay;
        private Label _paranoiaScannerLabel;
        private VisualElement _depressionOverlay;
        private VisualElement _sanitationPanicOverlay;
        private List<VisualElement> _raindrops = new List<VisualElement>();
        private List<Vector2> _raindropPositions = new List<Vector2>();
        private List<float> _raindropSpeeds = new List<float>();

        private VisualElement _carbonationOverlay;
        private List<VisualElement> _bubbles = new List<VisualElement>();
        private List<Vector2> _bubblePositions = new List<Vector2>();
        private List<float> _bubbleSpeeds = new List<float>();
        private VisualElement _alcoholOverlay;

        private float _orderPollTimer = 0f;

        private void Awake()
        {
            Instance = this;
        }

        private void Start()
        {
            // Find and deactivate ALL old uGUI HUD child panels (including SummaryPanel — replaced by UI Toolkit)
            var hudCanvas = GameObject.Find("HUDCanvas");
            if (hudCanvas != null)
            {
                foreach (Transform child in hudCanvas.transform)
                {
                    child.gameObject.SetActive(false);
                }
            }

            // Spawn Debug Console Pedestal dynamically in the scene for testing!
            var debugConsole = new GameObject("DebugConsolePedestal");
            debugConsole.transform.position = new Vector3(-9f, -0.75f, -9f); // placed out of the way in the corner
            var col = debugConsole.AddComponent<BoxCollider>();
            col.size = new Vector3(0.8f, 1.2f, 0.8f);
            col.isTrigger = false;

            var visual = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            visual.transform.SetParent(debugConsole.transform);
            visual.transform.localPosition = Vector3.zero;
            visual.transform.localScale = new Vector3(0.5f, 0.6f, 0.5f);
            var renderer = visual.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.material.color = new Color(0.91f, 0.64f, 0.26f, 1f); // Glowing Gold Pedestal
            }

            debugConsole.AddComponent<LemonEmpire.DebugTools.DebugTriggerConsole>();

            var vitalsCanvas = GameObject.Find("VitalsCanvas");
            if (vitalsCanvas != null)
            {
                vitalsCanvas.SetActive(false);
            }

            var vitalsUI = FindFirstObjectByType<VitalsUI>();
            if (vitalsUI != null)
            {
                vitalsUI.enabled = false;
            }

            var player = FindFirstObjectByType<PlayerController>();
            if (player != null)
            {
                _interaction = player.GetComponent<PlayerInteraction>();
                _carry = player.GetComponent<PlayerCarry>();

                if (player.GetComponent<PlayerStatusEffects>() == null)
                {
                    player.gameObject.AddComponent<PlayerStatusEffects>();
                }
            }

            BuildUI();

            if (EconomyManager.Instance != null)
            {
                EconomyManager.Instance.OnBalanceChanged += UpdateBalance;
                UpdateBalance(EconomyManager.Instance.Balance);
            }
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
            UpdateVitalsAndCleanliness();
            UpdateStrangeStates();

            _orderPollTimer -= Time.deltaTime;
            if (_orderPollTimer <= 0f)
            {
                _orderPollTimer = 1f;
                UpdateOrdersWaitlist();
            }
        }

        private void UpdateTime()
        {
            if (TimeManager.Instance != null)
            {
                int h = Mathf.FloorToInt(TimeManager.Instance.CurrentTimeOfDay);
                int m = Mathf.FloorToInt((TimeManager.Instance.CurrentTimeOfDay - h) * 60f);
                if (_timeLabel != null)
                {
                    _timeLabel.text = $"День {TimeManager.Instance.CurrentDay}   |   {h:00}:{m:00}";
                }

                string trendName = GameEventManager.CurrentTrend switch
                {
                    DailyTrend.Normal => "Обычный день",
                    DailyTrend.HeatWave => "Аномальная жара ☀️",
                    DailyTrend.PartyNight => "Ночная вечеринка 🥳",
                    DailyTrend.KidDay => "Детский день 🍭",
                    DailyTrend.Marathon => "Марафон 🏃",
                    _ => GameEventManager.CurrentTrend.ToString()
                };

                if (_trendText != null)
                {
                    _trendText.text = trendName;
                }
            }
        }

        private void UpdateBalance(float balance)
        {
            if (_balanceLabel != null)
                _balanceLabel.text = $"$ {balance:F2}";
        }

        private void UpdatePrompt()
        {
            if (_promptPill == null || _promptLabel == null || _interaction == null) return;

            string prompt = _interaction.GetCurrentPrompt();
            if (!string.IsNullOrEmpty(prompt))
            {
                _promptLabel.text = prompt;
                _promptPill.style.display = DisplayStyle.Flex;
            }
            else
            {
                _promptPill.style.display = DisplayStyle.None;
            }
        }

        private void UpdateCarryDisplay()
        {
            if (_carryPill == null || _carryLabel == null || _carry == null) return;

            if (_carry.IsCarrying)
            {
                _carryLabel.text = $"У вас в руках: {_carry.CarriedItem.DisplayName}";
                _carryPill.style.display = DisplayStyle.Flex;
            }
            else
            {
                _carryPill.style.display = DisplayStyle.None;
            }
        }

        private void UpdateVitalsAndCleanliness()
        {
            if (PlayerVitals.Instance != null)
            {
                float satiety = PlayerVitals.Instance.Satiety;
                float morale = PlayerVitals.Instance.Morale;

                if (_satietyProgressFill != null)
                    _satietyProgressFill.style.width = Length.Percent(satiety);
                if (_satietyPercentText != null)
                    _satietyPercentText.text = $"{Mathf.CeilToInt(satiety)}%";

                if (_moraleProgressFill != null)
                    _moraleProgressFill.style.width = Length.Percent(morale);
                if (_moralePercentText != null)
                    _moralePercentText.text = $"{Mathf.CeilToInt(morale)}%";
            }

            if (ShopCleanlinessManager.Instance != null && _cleanlinessFill != null)
            {
                float cleanliness = ShopCleanlinessManager.Instance.Cleanliness;
                _cleanlinessFill.style.width = Length.Percent(cleanliness);

                Color cleanColor = Color.Lerp(new Color(0.85f, 0.25f, 0.20f), new Color(0.11f, 0.82f, 0.60f), cleanliness / 100f);
                _cleanlinessFill.style.backgroundColor = new StyleColor(cleanColor);
            }
        }

        private void UpdateOrdersWaitlist()
        {
            if (_ordersContainer == null) return;
            _ordersContainer.Clear();

            var tables = Object.FindObjectsByType<LemonEmpire.Production.CustomerTable>(FindObjectsSortMode.None);
            bool hasOrders = false;

            System.Array.Sort(tables, (a, b) => string.Compare(a.gameObject.name, b.gameObject.name));

            for (int i = 0; i < tables.Length; i++)
            {
                var t = tables[i];
                if (t != null && t.CanInteract)
                {
                    hasOrders = true;

                    var row = new VisualElement();
                    row.style.flexDirection = FlexDirection.Row;
                    row.style.justifyContent = Justify.SpaceBetween;
                    row.style.alignItems = Align.Center;
                    row.style.marginBottom = 8f;
                    _ordersContainer.Add(row);

                    var leftSide = new VisualElement();
                    leftSide.style.flexDirection = FlexDirection.Row;
                    leftSide.style.alignItems = Align.Center;
                    leftSide.style.maxWidth = Length.Percent(70);
                    row.Add(leftSide);

                    string tableName = t.gameObject.name.Replace("Table", "Стол").Replace("table", "Стол");
                    
                    string desc = "Принять заказ";
                    if (t.State == LemonEmpire.Production.TableState.WaitingForDrink)
                    {
                        var field = typeof(LemonEmpire.Production.CustomerTable).GetField("activeOrderSpecifications", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                        string specs = field != null ? (string)field.GetValue(t) : "";
                        if (!string.IsNullOrEmpty(specs))
                        {
                            desc = specs.Replace("Заказ стола: ", "");
                        }
                        else
                        {
                            desc = "Ожидает напиток";
                        }
                    }

                    var textLbl = new Label($"{tableName}: {desc}");
                    textLbl.style.fontSize = 13f;
                    textLbl.style.color = new StyleColor(Color.white);
                    textLbl.style.unityFontStyleAndWeight = FontStyle.Bold;
                    leftSide.Add(textLbl);

                    var rightSide = new VisualElement();
                    rightSide.style.flexDirection = FlexDirection.Row;
                    rightSide.style.alignItems = Align.Center;
                    row.Add(rightSide);

                    var timerField = typeof(LemonEmpire.Production.CustomerTable).GetField("tablePatienceTimer", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    float patience = timerField != null ? (float)timerField.GetValue(t) : 0f;

                    var hourGlass = new Label("⏳");
                    hourGlass.style.fontSize = 14f;
                    hourGlass.style.marginRight = 6f;
                    rightSide.Add(hourGlass);

                    var timerLbl = new Label($"{Mathf.RoundToInt(patience)}с");
                    timerLbl.style.fontSize = 13f;
                    timerLbl.style.color = new StyleColor(patience < 15f ? new Color(0.98f, 0.36f, 0.32f, 1f) : Color.white);
                    timerLbl.style.unityFontStyleAndWeight = FontStyle.Bold;
                    rightSide.Add(timerLbl);
                }
            }

            if (!hasOrders)
            {
                var noOrdersLbl = new Label("Нет активных заказов");
                noOrdersLbl.style.fontSize = 13f;
                noOrdersLbl.style.color = new StyleColor(new Color(0.58f, 0.55f, 0.51f, 1f));
                noOrdersLbl.style.unityFontStyleAndWeight = FontStyle.Italic;
                _ordersContainer.Add(noOrdersLbl);
            }
        }

        private void BuildUI()
        {
            var uiDocGO = new GameObject("HUDUIDocument");
            uiDocGO.transform.SetParent(transform);
            var uiDocument = uiDocGO.AddComponent<UIDocument>();

            var settings = Resources.Load<PanelSettings>("TabletPanelSettings");
            if (settings == null)
            {
                settings = ScriptableObject.CreateInstance<PanelSettings>();
                settings.scaleMode = PanelScaleMode.ScaleWithScreenSize;
                settings.referenceResolution = new Vector2Int(2560, 1440);
                settings.screenMatchMode = PanelScreenMatchMode.MatchWidthOrHeight;
                settings.match = 0.5f;
            }
            uiDocument.panelSettings = settings;

            _hudRoot = uiDocument.rootVisualElement;
            _hudRoot.style.width = Length.Percent(100);
            _hudRoot.style.height = Length.Percent(100);
            _hudRoot.style.position = Position.Absolute;
            _hudRoot.style.left = 0f;
            _hudRoot.style.top = 0f;
            _hudRoot.style.right = 0f;
            _hudRoot.style.bottom = 0f;

            // 1. Vitals Panel (Top-Left)
            var vitalsPanel = new VisualElement();
            vitalsPanel.style.position = Position.Absolute;
            vitalsPanel.style.left = 40f;
            vitalsPanel.style.top = 40f;
            vitalsPanel.style.width = 320f;
            vitalsPanel.style.height = 90f;
            vitalsPanel.style.backgroundColor = new StyleColor(new Color(0.08f, 0.08f, 0.09f, 0.85f));
            vitalsPanel.style.SetBorderRadius(20f);
            vitalsPanel.style.SetBorderWidth(1f);
            vitalsPanel.style.SetBorderColor(new Color(0.2f, 0.15f, 0.12f, 1f));
            vitalsPanel.style.paddingLeft = 16f;
            vitalsPanel.style.paddingRight = 16f;
            vitalsPanel.style.paddingTop = 12f;
            vitalsPanel.style.paddingBottom = 12f;
            vitalsPanel.style.justifyContent = Justify.SpaceBetween;
            _hudRoot.Add(vitalsPanel);

            // Satiety Row
            var satRow = new VisualElement();
            satRow.style.flexDirection = FlexDirection.Row;
            satRow.style.alignItems = Align.Center;
            satRow.style.justifyContent = Justify.SpaceBetween;
            vitalsPanel.Add(satRow);

            var satIcon = new Label("🍔");
            satIcon.style.fontSize = 18f;
            satIcon.style.marginRight = 8f;
            satRow.Add(satIcon);

            var satTrack = new VisualElement();
            satTrack.style.width = 180f;
            satTrack.style.height = 10f;
            satTrack.style.backgroundColor = new StyleColor(new Color(0.19f, 0.13f, 0.09f, 1f));
            satTrack.style.SetBorderRadius(5f);
            satTrack.style.overflow = Overflow.Hidden;
            satRow.Add(satTrack);

            _satietyProgressFill = new VisualElement();
            _satietyProgressFill.style.height = Length.Percent(100);
            _satietyProgressFill.style.width = Length.Percent(100);
            _satietyProgressFill.style.backgroundColor = new StyleColor(new Color(0.98f, 0.78f, 0.25f, 1f));
            satTrack.Add(_satietyProgressFill);

            _satietyPercentText = new Label("100%");
            _satietyPercentText.style.fontSize = 15f;
            _satietyPercentText.style.color = new StyleColor(Color.white);
            _satietyPercentText.style.unityFontStyleAndWeight = FontStyle.Bold;
            _satietyPercentText.style.width = 45f;
            _satietyPercentText.style.unityTextAlign = TextAnchor.MiddleRight;
            satRow.Add(_satietyPercentText);

            // Morale Row
            var morRow = new VisualElement();
            morRow.style.flexDirection = FlexDirection.Row;
            morRow.style.alignItems = Align.Center;
            morRow.style.justifyContent = Justify.SpaceBetween;
            vitalsPanel.Add(morRow);

            var morIcon = new Label("😊");
            morIcon.style.fontSize = 18f;
            morIcon.style.marginRight = 8f;
            morRow.Add(morIcon);

            var morTrack = new VisualElement();
            morTrack.style.width = 180f;
            morTrack.style.height = 10f;
            morTrack.style.backgroundColor = new StyleColor(new Color(0.19f, 0.13f, 0.09f, 1f));
            morTrack.style.SetBorderRadius(5f);
            morTrack.style.overflow = Overflow.Hidden;
            morRow.Add(morTrack);

            _moraleProgressFill = new VisualElement();
            _moraleProgressFill.style.height = Length.Percent(100);
            _moraleProgressFill.style.width = Length.Percent(100);
            _moraleProgressFill.style.backgroundColor = new StyleColor(new Color(0.11f, 0.82f, 0.60f, 1f));
            morTrack.Add(_moraleProgressFill);

            _moralePercentText = new Label("100%");
            _moralePercentText.style.fontSize = 15f;
            _moralePercentText.style.color = new StyleColor(Color.white);
            _moralePercentText.style.unityFontStyleAndWeight = FontStyle.Bold;
            _moralePercentText.style.width = 45f;
            _moralePercentText.style.unityTextAlign = TextAnchor.MiddleRight;
            morRow.Add(_moralePercentText);

            // 2. Cleanliness Panel (Top-Center)
            var cleanlinessPanel = new VisualElement();
            cleanlinessPanel.style.position = Position.Absolute;
            cleanlinessPanel.style.top = 40f;
            cleanlinessPanel.style.alignSelf = Align.Center;
            cleanlinessPanel.style.width = 280f;
            cleanlinessPanel.style.height = 76f;
            cleanlinessPanel.style.backgroundColor = new StyleColor(new Color(0.08f, 0.08f, 0.09f, 0.85f));
            cleanlinessPanel.style.SetBorderRadius(16f);
            cleanlinessPanel.style.SetBorderWidth(1f);
            cleanlinessPanel.style.SetBorderColor(new Color(0.2f, 0.15f, 0.12f, 1f));
            cleanlinessPanel.style.paddingLeft = 14f;
            cleanlinessPanel.style.paddingRight = 14f;
            cleanlinessPanel.style.paddingTop = 8f;
            cleanlinessPanel.style.paddingBottom = 8f;
            cleanlinessPanel.style.alignItems = Align.Center;
            cleanlinessPanel.style.justifyContent = Justify.SpaceBetween;
            _hudRoot.Add(cleanlinessPanel);

            var timeRow = new VisualElement();
            timeRow.style.flexDirection = FlexDirection.Row;
            timeRow.style.alignItems = Align.Center;
            timeRow.style.marginBottom = 2f;
            cleanlinessPanel.Add(timeRow);

            _timeLabel = new Label("День 1   |   08:00");
            _timeLabel.style.fontSize = 13f;
            _timeLabel.style.color = new StyleColor(Color.white);
            _timeLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            timeRow.Add(_timeLabel);

            var cleanTitle = new Label("ЧИСТОТА ТОРГОВОГО ЗАЛА");
            cleanTitle.style.fontSize = 11f;
            cleanTitle.style.color = new StyleColor(new Color(0.58f, 0.55f, 0.51f, 1f));
            cleanTitle.style.unityFontStyleAndWeight = FontStyle.Bold;
            cleanTitle.style.letterSpacing = 1.2f;
            cleanlinessPanel.Add(cleanTitle);

            var cleanTrack = new VisualElement();
            cleanTrack.style.width = 250f;
            cleanTrack.style.height = 8f;
            cleanTrack.style.backgroundColor = new StyleColor(new Color(0.19f, 0.13f, 0.09f, 1f));
            cleanTrack.style.SetBorderRadius(4f);
            cleanTrack.style.overflow = Overflow.Hidden;
            cleanlinessPanel.Add(cleanTrack);

            _cleanlinessFill = new VisualElement();
            _cleanlinessFill.style.height = Length.Percent(100);
            _cleanlinessFill.style.width = Length.Percent(100);
            _cleanlinessFill.style.backgroundColor = new StyleColor(new Color(0.11f, 0.82f, 0.60f, 1f));
            cleanTrack.Add(_cleanlinessFill);

            // 3. Trend Panel (Top-Right)
            var trendPanel = new VisualElement();
            trendPanel.style.position = Position.Absolute;
            trendPanel.style.right = 40f;
            trendPanel.style.top = 40f;
            trendPanel.style.width = 280f;
            trendPanel.style.height = 85f;
            trendPanel.style.backgroundColor = new StyleColor(new Color(0.08f, 0.08f, 0.09f, 0.85f));
            trendPanel.style.SetBorderRadius(16f);
            trendPanel.style.SetBorderWidth(1f);
            trendPanel.style.SetBorderColor(new Color(0.91f, 0.64f, 0.26f, 1f));
            trendPanel.style.paddingLeft = 16f;
            trendPanel.style.paddingRight = 16f;
            trendPanel.style.paddingTop = 10f;
            trendPanel.style.paddingBottom = 10f;
            trendPanel.style.justifyContent = Justify.SpaceBetween;
            _hudRoot.Add(trendPanel);

            var trendHeader = new Label("ТРЕНД ДНЯ");
            trendHeader.style.fontSize = 12f;
            trendHeader.style.color = new StyleColor(new Color(0.91f, 0.64f, 0.26f, 1f));
            trendHeader.style.unityFontStyleAndWeight = FontStyle.Bold;
            trendHeader.style.unityTextAlign = TextAnchor.MiddleRight;
            trendPanel.Add(trendHeader);

            _trendText = new Label("Обычный день");
            _trendText.style.fontSize = 14f;
            _trendText.style.color = new StyleColor(Color.white);
            _trendText.style.unityFontStyleAndWeight = FontStyle.Bold;
            _trendText.style.unityTextAlign = TextAnchor.MiddleRight;
            trendPanel.Add(_trendText);

            // 4. Wallet Panel (Bottom-Left)
            var walletPanel = new VisualElement();
            walletPanel.style.position = Position.Absolute;
            walletPanel.style.left = 40f;
            walletPanel.style.bottom = 40f;
            walletPanel.style.width = 240f;
            walletPanel.style.height = 70f;
            walletPanel.style.backgroundColor = new StyleColor(new Color(0.08f, 0.08f, 0.09f, 0.85f));
            walletPanel.style.SetBorderRadius(16f);
            walletPanel.style.SetBorderWidth(1f);
            walletPanel.style.SetBorderColor(new Color(0.2f, 0.15f, 0.12f, 1f));
            walletPanel.style.alignItems = Align.Center;
            walletPanel.style.justifyContent = Justify.Center;
            _hudRoot.Add(walletPanel);

            _balanceLabel = new Label("$0.00");
            _balanceLabel.style.fontSize = 28f;
            _balanceLabel.style.color = new StyleColor(new Color(0f, 1f, 0.53f, 1f));
            _balanceLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            walletPanel.Add(_balanceLabel);

            // 5. Active Orders Panel (Bottom-Right)
            var ordersPanel = new VisualElement();
            ordersPanel.style.position = Position.Absolute;
            ordersPanel.style.right = 40f;
            ordersPanel.style.bottom = 40f;
            ordersPanel.style.width = 330f;
            ordersPanel.style.minHeight = 110f;
            ordersPanel.style.maxHeight = 320f;
            ordersPanel.style.backgroundColor = new StyleColor(new Color(0.08f, 0.08f, 0.09f, 0.85f));
            ordersPanel.style.SetBorderRadius(16f);
            ordersPanel.style.SetBorderWidth(1f);
            ordersPanel.style.SetBorderColor(new Color(0.2f, 0.15f, 0.12f, 1f));
            ordersPanel.style.paddingLeft = 16f;
            ordersPanel.style.paddingRight = 16f;
            ordersPanel.style.paddingTop = 12f;
            ordersPanel.style.paddingBottom = 12f;
            _hudRoot.Add(ordersPanel);

            var ordersHeader = new Label("Заказы Гостей (Официант)");
            ordersHeader.style.fontSize = 14f;
            ordersHeader.style.color = new StyleColor(new Color(0.91f, 0.64f, 0.26f, 1f));
            ordersHeader.style.unityFontStyleAndWeight = FontStyle.Bold;
            ordersHeader.style.marginBottom = 6f;
            ordersPanel.Add(ordersHeader);

            var ordersDivider = new VisualElement();
            ordersDivider.style.height = 1f;
            ordersDivider.style.backgroundColor = new StyleColor(new Color(0.19f, 0.13f, 0.09f, 1f));
            ordersDivider.style.marginBottom = 10f;
            ordersPanel.Add(ordersDivider);

            _ordersContainer = new VisualElement();
            _ordersContainer.style.flexGrow = 1f;
            ordersPanel.Add(_ordersContainer);

            // 6. Interaction Prompt (Center Overlay)
            _promptPill = new VisualElement();
            _promptPill.style.position = Position.Absolute;
            _promptPill.style.alignSelf = Align.Center;
            _promptPill.style.top = Length.Percent(58);
            _promptPill.style.height = 54f;
            _promptPill.style.backgroundColor = new StyleColor(new Color(0.08f, 0.08f, 0.09f, 0.88f));
            _promptPill.style.SetBorderRadius(14f);
            _promptPill.style.SetBorderWidth(2f);
            _promptPill.style.SetBorderColor(new Color(0.91f, 0.64f, 0.26f, 1f));
            _promptPill.style.paddingLeft = 24f;
            _promptPill.style.paddingRight = 24f;
            _promptPill.style.alignItems = Align.Center;
            _promptPill.style.justifyContent = Justify.Center;
            _promptPill.style.display = DisplayStyle.None;
            _hudRoot.Add(_promptPill);

            _promptLabel = new Label("");
            _promptLabel.style.fontSize = 16f;
            _promptLabel.style.color = new StyleColor(Color.white);
            _promptLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            _promptPill.Add(_promptLabel);

            // 7. Carry Display Pill (Bottom Center, just below prompt)
            _carryPill = new VisualElement();
            _carryPill.style.position = Position.Absolute;
            _carryPill.style.alignSelf = Align.Center;
            _carryPill.style.top = Length.Percent(65);
            _carryPill.style.height = 44f;
            _carryPill.style.backgroundColor = new StyleColor(new Color(0.08f, 0.08f, 0.09f, 0.85f));
            _carryPill.style.SetBorderRadius(10f);
            _carryPill.style.SetBorderWidth(1f);
            _carryPill.style.SetBorderColor(new Color(0.85f, 0.82f, 0.77f, 1f));
            _carryPill.style.paddingLeft = 16f;
            _carryPill.style.paddingRight = 16f;
            _carryPill.style.alignItems = Align.Center;
            _carryPill.style.justifyContent = Justify.Center;
            _carryPill.style.display = DisplayStyle.None;
            _hudRoot.Add(_carryPill);

            _carryLabel = new Label("");
            _carryLabel.style.fontSize = 14f;
            _carryLabel.style.color = new StyleColor(Color.white);
            _carryLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            _carryPill.Add(_carryLabel);

            // ── 8. Summary Overlay (hidden by default) ──────────────────────────
            _summaryOverlay = new VisualElement();
            _summaryOverlay.style.position = Position.Absolute;
            _summaryOverlay.style.left = 0f;
            _summaryOverlay.style.top = 0f;
            _summaryOverlay.style.right = 0f;
            _summaryOverlay.style.bottom = 0f;
            _summaryOverlay.style.backgroundColor = new StyleColor(new Color(0f, 0f, 0f, 0.60f));
            _summaryOverlay.style.alignItems = Align.Center;
            _summaryOverlay.style.justifyContent = Justify.Center;
            _summaryOverlay.style.display = DisplayStyle.None;
            _hudRoot.Add(_summaryOverlay);

            // Card
            var summaryCard = new VisualElement();
            summaryCard.style.width = 420f;
            summaryCard.style.backgroundColor = new StyleColor(new Color(0.078f, 0.078f, 0.09f, 0.94f));
            summaryCard.style.SetBorderRadius(20f);
            summaryCard.style.SetBorderWidth(1f);
            summaryCard.style.SetBorderColor(new Color(0.72f, 0.55f, 0.20f, 1f));
            summaryCard.style.paddingLeft = 32f;
            summaryCard.style.paddingRight = 32f;
            summaryCard.style.paddingTop = 28f;
            summaryCard.style.paddingBottom = 28f;
            summaryCard.style.alignItems = Align.Center;
            _summaryOverlay.Add(summaryCard);

            // Header
            _summaryDayLabel = new Label("ДЕНЬ 1 ЗАВЕРШЁН");
            _summaryDayLabel.style.fontSize = 22f;
            _summaryDayLabel.style.color = new StyleColor(new Color(0.91f, 0.72f, 0.30f, 1f));
            _summaryDayLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            _summaryDayLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
            _summaryDayLabel.style.marginBottom = 16f;
            summaryCard.Add(_summaryDayLabel);

            // Gold divider
            var summaryDivider = new VisualElement();
            summaryDivider.style.width = Length.Percent(100);
            summaryDivider.style.height = 1f;
            summaryDivider.style.backgroundColor = new StyleColor(new Color(0.72f, 0.55f, 0.20f, 0.5f));
            summaryDivider.style.marginBottom = 20f;
            summaryCard.Add(summaryDivider);

            // Stats container
            var statsContainer = new VisualElement();
            statsContainer.style.width = Length.Percent(100);
            statsContainer.style.marginBottom = 8f;
            summaryCard.Add(statsContainer);

            // Revenue row
            var revenueRow = MakeSummaryRow(statsContainer);
            var revLabel = new Label("Выручка");
            revLabel.style.fontSize = 16f;
            revLabel.style.color = new StyleColor(new Color(0.70f, 0.68f, 0.65f, 1f));
            revenueRow.Add(revLabel);

            _summaryRevenueValue = new Label("+$0");
            _summaryRevenueValue.style.fontSize = 16f;
            _summaryRevenueValue.style.color = new StyleColor(new Color(0.33f, 0.87f, 0.42f, 1f));
            _summaryRevenueValue.style.unityFontStyleAndWeight = FontStyle.Bold;
            revenueRow.Add(_summaryRevenueValue);

            // Expenses row
            var expensesRow = MakeSummaryRow(statsContainer);
            var expLabel = new Label("Расходы");
            expLabel.style.fontSize = 16f;
            expLabel.style.color = new StyleColor(new Color(0.70f, 0.68f, 0.65f, 1f));
            expensesRow.Add(expLabel);

            _summaryExpensesValue = new Label("$0");
            _summaryExpensesValue.style.fontSize = 16f;
            _summaryExpensesValue.style.color = new StyleColor(new Color(0.95f, 0.38f, 0.35f, 1f));
            _summaryExpensesValue.style.unityFontStyleAndWeight = FontStyle.Bold;
            expensesRow.Add(_summaryExpensesValue);

            // Profit divider
            var profitDivider = new VisualElement();
            profitDivider.style.width = Length.Percent(100);
            profitDivider.style.height = 1f;
            profitDivider.style.backgroundColor = new StyleColor(new Color(0.30f, 0.28f, 0.25f, 1f));
            profitDivider.style.marginTop = 4f;
            profitDivider.style.marginBottom = 14f;
            statsContainer.Add(profitDivider);

            // Profit row
            var profitRow = MakeSummaryRow(statsContainer);
            profitRow.style.marginBottom = 0f;
            var profitLabel = new Label("Прибыль");
            profitLabel.style.fontSize = 18f;
            profitLabel.style.color = new StyleColor(Color.white);
            profitLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            profitRow.Add(profitLabel);

            _summaryProfitValue = new Label("$0");
            _summaryProfitValue.style.fontSize = 18f;
            _summaryProfitValue.style.color = new StyleColor(Color.white);
            _summaryProfitValue.style.unityFontStyleAndWeight = FontStyle.Bold;
            profitRow.Add(_summaryProfitValue);

            // Bottles sold
            _summaryBottlesLabel = new Label("Продано бутылок: 0");
            _summaryBottlesLabel.style.fontSize = 14f;
            _summaryBottlesLabel.style.color = new StyleColor(new Color(0.55f, 0.52f, 0.48f, 1f));
            _summaryBottlesLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
            _summaryBottlesLabel.style.marginTop = 16f;
            _summaryBottlesLabel.style.marginBottom = 24f;
            summaryCard.Add(_summaryBottlesLabel);

            // Button "НАЧАТЬ СЛЕДУЮЩИЙ ДЕНЬ"
            var nextDayBtn = new VisualElement();
            nextDayBtn.style.width = Length.Percent(85);
            nextDayBtn.style.height = 48f;
            nextDayBtn.style.backgroundColor = new StyleColor(new Color(0.80f, 0.60f, 0.15f, 1f));
            nextDayBtn.style.SetBorderRadius(12f);
            nextDayBtn.style.alignItems = Align.Center;
            nextDayBtn.style.justifyContent = Justify.Center;
            summaryCard.Add(nextDayBtn);

            var nextDayLabel = new Label("НАЧАТЬ СЛЕДУЮЩИЙ ДЕНЬ");
            nextDayLabel.style.fontSize = 15f;
            nextDayLabel.style.color = new StyleColor(new Color(0.10f, 0.08f, 0.05f, 1f));
            nextDayLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            nextDayLabel.style.letterSpacing = 1f;
            nextDayBtn.Add(nextDayLabel);

            nextDayBtn.RegisterCallback<ClickEvent>(evt => {
                _summaryOverlay.style.display = DisplayStyle.None;
                UnityEngine.Cursor.lockState = UnityEngine.CursorLockMode.Locked;
                UnityEngine.Cursor.visible = false;
                TimeManager.Instance?.StartShift();
            });

            nextDayBtn.RegisterCallback<MouseEnterEvent>(evt => {
                nextDayBtn.style.backgroundColor = new StyleColor(new Color(0.90f, 0.70f, 0.20f, 1f));
            });
            nextDayBtn.RegisterCallback<MouseLeaveEvent>(evt => {
                nextDayBtn.style.backgroundColor = new StyleColor(new Color(0.80f, 0.60f, 0.15f, 1f));
            });

            BuildStrangeStateOverlays();
            BuildPriceInputModal();
        }

        public void ShowDaySummary(TimeManager timeMan, EconomyManager ecoMan)
        {
            UnityEngine.Cursor.lockState = UnityEngine.CursorLockMode.None;
            UnityEngine.Cursor.visible = true;

            if (_summaryOverlay == null) return;

            float profit = ecoMan.DailyRevenue - ecoMan.DailyExpenses;

            _summaryDayLabel.text = $"ДЕНЬ {timeMan.CurrentDay} ЗАВЕРШЁН";
            _summaryRevenueValue.text = $"+${ecoMan.DailyRevenue:F2}";
            _summaryExpensesValue.text = $"${ecoMan.DailyExpenses:F2}";
            _summaryProfitValue.text = profit >= 0 ? $"${profit:F2}" : $"-${Mathf.Abs(profit):F2}";
            _summaryBottlesLabel.text = $"Продано бутылок: {ecoMan.BottlesSold}";

            // Color profit based on positive/negative
            _summaryProfitValue.style.color = new StyleColor(
                profit >= 0 ? Color.white : new Color(0.95f, 0.38f, 0.35f, 1f)
            );

            _summaryOverlay.style.display = DisplayStyle.Flex;
        }

        /// <summary>Helper to create a stat row for the summary card.</summary>
        private VisualElement MakeSummaryRow(VisualElement parent)
        {
            var row = new VisualElement();
            row.style.flexDirection = FlexDirection.Row;
            row.style.justifyContent = Justify.SpaceBetween;
            row.style.width = Length.Percent(100);
            row.style.marginBottom = 12f;
            parent.Add(row);
            return row;
        }

        private Texture2D CreateVignetteTexture(Color color)
        {
            int size = 64;
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            float center = size / 2f;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dist = Vector2.Distance(new Vector2(x, y), new Vector2(center, center)) / center;
                    float alpha = Mathf.Clamp01(dist);
                    alpha = Mathf.SmoothStep(0f, 1f, alpha);
                    tex.SetPixel(x, y, new Color(color.r, color.g, color.b, alpha * color.a));
                }
            }
            tex.Apply();
            return tex;
        }

        private void BuildStrangeStateOverlays()
        {
            // ── 9. Sugar Rush Overlay (Yellow-Orange radial vignette texture!) ──────────────────────────
            _sugarRushOverlay = new VisualElement();
            _sugarRushOverlay.style.position = Position.Absolute;
            _sugarRushOverlay.style.left = 0f;
            _sugarRushOverlay.style.top = 0f;
            _sugarRushOverlay.style.right = 0f;
            _sugarRushOverlay.style.bottom = 0f;
            _sugarRushOverlay.style.backgroundImage = new StyleBackground(CreateVignetteTexture(new Color(1f, 0.60f, 0f, 0.50f)));
            _sugarRushOverlay.style.display = DisplayStyle.None;
            _hudRoot.Add(_sugarRushOverlay);

            // ── 10. Lemon Paranoia Overlay (Noir sepia radial vignette texture!) ──────────────────────────
            _paranoiaOverlay = new VisualElement();
            _paranoiaOverlay.style.position = Position.Absolute;
            _paranoiaOverlay.style.left = 0f;
            _paranoiaOverlay.style.top = 0f;
            _paranoiaOverlay.style.right = 0f;
            _paranoiaOverlay.style.bottom = 0f;
            _paranoiaOverlay.style.backgroundImage = new StyleBackground(CreateVignetteTexture(new Color(0.12f, 0.08f, 0.04f, 0.70f)));
            _paranoiaOverlay.style.display = DisplayStyle.None;
            _hudRoot.Add(_paranoiaOverlay);

            var topScan = new VisualElement();
            topScan.style.position = Position.Absolute;
            topScan.style.left = Length.Percent(10);
            topScan.style.right = Length.Percent(10);
            topScan.style.top = Length.Percent(15);
            topScan.style.height = 1f;
            topScan.style.backgroundColor = new StyleColor(new Color(0.91f, 0.64f, 0.26f, 0.5f));
            _paranoiaOverlay.Add(topScan);

            var bottomScan = new VisualElement();
            bottomScan.style.position = Position.Absolute;
            bottomScan.style.left = Length.Percent(10);
            bottomScan.style.right = Length.Percent(10);
            bottomScan.style.bottom = Length.Percent(15);
            bottomScan.style.height = 1f;
            bottomScan.style.backgroundColor = new StyleColor(new Color(0.91f, 0.64f, 0.26f, 0.5f));
            _paranoiaOverlay.Add(bottomScan);

            // Move the warning text safely lower (top = 180f) to prevent overlapping the central Time/Cleanliness UI HUD panels!
            _paranoiaScannerLabel = new Label("[ШПИОН КОНКУРЕНТОВ ДЕТЕКТИРОВАН]");
            _paranoiaScannerLabel.style.position = Position.Absolute;
            _paranoiaScannerLabel.style.top = 180f;
            _paranoiaScannerLabel.style.alignSelf = Align.Center;
            _paranoiaScannerLabel.style.fontSize = 15f;
            _paranoiaScannerLabel.style.color = new StyleColor(new Color(0.95f, 0.38f, 0.35f, 1f));
            _paranoiaScannerLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            _paranoiaOverlay.Add(_paranoiaScannerLabel);

            // ── 11. Financial Depression Overlay (Deep blue radial vignette texture!) ──────────────────────────
            _depressionOverlay = new VisualElement();
            _depressionOverlay.style.position = Position.Absolute;
            _depressionOverlay.style.left = 0f;
            _depressionOverlay.style.top = 0f;
            _depressionOverlay.style.right = 0f;
            _depressionOverlay.style.bottom = 0f;
            _depressionOverlay.style.backgroundImage = new StyleBackground(CreateVignetteTexture(new Color(0.02f, 0.10f, 0.35f, 0.60f)));
            _depressionOverlay.style.display = DisplayStyle.None;
            _hudRoot.Add(_depressionOverlay);

            for (int i = 0; i < 20; i++)
            {
                var drop = new VisualElement();
                drop.style.position = Position.Absolute;
                drop.style.width = 3f;
                drop.style.height = 15f;
                drop.style.backgroundColor = new StyleColor(new Color(0.4f, 0.7f, 1.0f, 0.6f));
                drop.style.borderTopLeftRadius = 1.5f;
                drop.style.borderTopRightRadius = 1.5f;
                drop.style.borderBottomLeftRadius = 1.5f;
                drop.style.borderBottomRightRadius = 1.5f;

                float rx = UnityEngine.Random.Range(20f, 2540f);
                float ry = UnityEngine.Random.Range(-50f, 1400f);
                float speed = UnityEngine.Random.Range(400f, 900f);

                drop.style.left = rx;
                drop.style.top = ry;

                _depressionOverlay.Add(drop);
                _raindrops.Add(drop);
                _raindropPositions.Add(new Vector2(rx, ry));
                _raindropSpeeds.Add(speed);
            }

            // ── 11b. Stylized rain cloud centered at the top! ──────────────────────────
            var rainCloud = new VisualElement();
            rainCloud.style.position = Position.Absolute;
            rainCloud.style.top = 20f;
            rainCloud.style.alignSelf = Align.Center;
            rainCloud.style.width = 280f;
            rainCloud.style.height = 65f;
            rainCloud.style.backgroundColor = new StyleColor(new Color(0.12f, 0.16f, 0.24f, 0.90f));
            rainCloud.style.borderTopLeftRadius = 32f;
            rainCloud.style.borderTopRightRadius = 32f;
            rainCloud.style.borderBottomLeftRadius = 15f;
            rainCloud.style.borderBottomRightRadius = 15f;
            rainCloud.style.borderLeftColor = new StyleColor(new Color(0.2f, 0.3f, 0.5f, 1f));
            rainCloud.style.borderRightColor = new StyleColor(new Color(0.2f, 0.3f, 0.5f, 1f));
            rainCloud.style.borderTopColor = new StyleColor(new Color(0.2f, 0.3f, 0.5f, 1f));
            rainCloud.style.borderBottomColor = new StyleColor(new Color(0.2f, 0.3f, 0.5f, 1f));
            rainCloud.style.borderLeftWidth = 1f;
            rainCloud.style.borderRightWidth = 1f;
            rainCloud.style.borderTopWidth = 1f;
            rainCloud.style.borderBottomWidth = 1f;
            
            var cloudLabel = new Label("ФИНАНСОВАЯ ДЕПРЕССИЯ");
            cloudLabel.style.color = new StyleColor(new Color(0.4f, 0.7f, 1.0f, 1f));
            cloudLabel.style.fontSize = 11f;
            cloudLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            cloudLabel.style.alignSelf = Align.Center;
            cloudLabel.style.marginTop = 24f;
            rainCloud.Add(cloudLabel);
            _depressionOverlay.Add(rainCloud);

            // ── 12. Sanitation Panic Overlay (Toxic green radial vignette texture!) ──────────────────────────
            _sanitationPanicOverlay = new VisualElement();
            _sanitationPanicOverlay.style.position = Position.Absolute;
            _sanitationPanicOverlay.style.left = 0f;
            _sanitationPanicOverlay.style.top = 0f;
            _sanitationPanicOverlay.style.right = 0f;
            _sanitationPanicOverlay.style.bottom = 0f;
            _sanitationPanicOverlay.style.backgroundImage = new StyleBackground(CreateVignetteTexture(new Color(0f, 0.45f, 0.05f, 0.50f)));
            _sanitationPanicOverlay.style.display = DisplayStyle.None;
            _hudRoot.Add(_sanitationPanicOverlay);

            // ── 13. Carbonation Overload Overlay (Cyan/teal radial vignette texture!) ──────────────────────────
            _carbonationOverlay = new VisualElement();
            _carbonationOverlay.style.position = Position.Absolute;
            _carbonationOverlay.style.left = 0f;
            _carbonationOverlay.style.top = 0f;
            _carbonationOverlay.style.right = 0f;
            _carbonationOverlay.style.bottom = 0f;
            _carbonationOverlay.style.backgroundImage = new StyleBackground(CreateVignetteTexture(new Color(0f, 0.40f, 0.45f, 0.50f)));
            _carbonationOverlay.style.display = DisplayStyle.None;
            _hudRoot.Add(_carbonationOverlay);

            // Spawn 15 procedurally floating up bubbles!
            for (int i = 0; i < 15; i++)
            {
                var bubble = new VisualElement();
                bubble.style.position = Position.Absolute;
                float size = UnityEngine.Random.Range(8f, 22f);
                bubble.style.width = size;
                bubble.style.height = size;
                bubble.style.backgroundColor = new StyleColor(new Color(0.6f, 0.9f, 1.0f, 0.35f));
                bubble.style.borderLeftColor = new StyleColor(new Color(0.8f, 0.95f, 1.0f, 0.70f));
                bubble.style.borderRightColor = new StyleColor(new Color(0.8f, 0.95f, 1.0f, 0.70f));
                bubble.style.borderTopColor = new StyleColor(new Color(0.8f, 0.95f, 1.0f, 0.70f));
                bubble.style.borderBottomColor = new StyleColor(new Color(0.8f, 0.95f, 1.0f, 0.70f));
                bubble.style.borderLeftWidth = 1f;
                bubble.style.borderRightWidth = 1f;
                bubble.style.borderTopWidth = 1f;
                bubble.style.borderBottomWidth = 1f;
                bubble.style.borderTopLeftRadius = size * 0.5f;
                bubble.style.borderTopRightRadius = size * 0.5f;
                bubble.style.borderBottomLeftRadius = size * 0.5f;
                bubble.style.borderBottomRightRadius = size * 0.5f;

                float rx = UnityEngine.Random.Range(20f, 2540f);
                float ry = UnityEngine.Random.Range(100f, 1400f);
                float speed = UnityEngine.Random.Range(80f, 220f);

                bubble.style.left = rx;
                bubble.style.top = ry;

                _carbonationOverlay.Add(bubble);
                _bubbles.Add(bubble);
                _bubblePositions.Add(new Vector2(rx, ry));
                _bubbleSpeeds.Add(speed);
            }

            // ── 14. Alcohol Intoxication Overlay (Pinkish-purple radial vignette texture!) ──────────────────────────
            _alcoholOverlay = new VisualElement();
            _alcoholOverlay.style.position = Position.Absolute;
            _alcoholOverlay.style.left = 0f;
            _alcoholOverlay.style.top = 0f;
            _alcoholOverlay.style.right = 0f;
            _alcoholOverlay.style.bottom = 0f;
            _alcoholOverlay.style.backgroundImage = new StyleBackground(CreateVignetteTexture(new Color(0.55f, 0.05f, 0.40f, 0.65f)));
            _alcoholOverlay.style.display = DisplayStyle.None;
            _hudRoot.Add(_alcoholOverlay);
        }

        private void UpdateStrangeStates()
        {
            var effects = LemonEmpire.Player.PlayerStatusEffects.Instance;
            if (effects == null) return;

            // 1. Sugar Rush Overlay - Gentle breathing Yellow-Orange Vignette (using slow opacity pulse, not rapid blinking/flashing)
            if (effects.IsSugarRushActive)
            {
                _sugarRushOverlay.style.display = DisplayStyle.Flex;
                float pulse = 0.30f + Mathf.Sin(Time.time * 1.2f) * 0.10f; // smooth slow pulse breathing between 0.20 and 0.40 opacity
                _sugarRushOverlay.style.opacity = pulse;
            }
            else
            {
                _sugarRushOverlay.style.display = DisplayStyle.None;
            }

            // 2. Lemon Paranoia Overlay
            if (effects.IsLemonParanoiaActive)
            {
                _paranoiaOverlay.style.display = DisplayStyle.Flex;
                bool blink = Mathf.FloorToInt(Time.time * 2f) % 2 == 0;
                _paranoiaScannerLabel.style.visibility = blink ? Visibility.Visible : Visibility.Hidden;
            }
            else
            {
                _paranoiaOverlay.style.display = DisplayStyle.None;
            }

            // 3. Financial Depression Overlay
            if (effects.IsFinancialDepressionActive)
            {
                _depressionOverlay.style.display = DisplayStyle.Flex;
                
                for (int i = 0; i < _raindrops.Count; i++)
                {
                    Vector2 pos = _raindropPositions[i];
                    pos.y += _raindropSpeeds[i] * Time.deltaTime;
                    if (pos.y > 1450f)
                    {
                        pos.y = -20f;
                        pos.x = UnityEngine.Random.Range(20f, 2540f);
                        _raindropSpeeds[i] = UnityEngine.Random.Range(400f, 900f);
                    }
                    _raindropPositions[i] = pos;
                    _raindrops[i].style.left = pos.x;
                    _raindrops[i].style.top = pos.y;
                }
            }
            else
            {
                _depressionOverlay.style.display = DisplayStyle.None;
            }

            // 4. Sanitation Panic Overlay - High frequency color shifting pulse!
            if (effects.IsSanitationPanicActive)
            {
                _sanitationPanicOverlay.style.display = DisplayStyle.Flex;
                float panicPulse = 0.35f + Mathf.Sin(Time.time * 12f) * 0.15f; // Fast high-frequency pulsing!
                _sanitationPanicOverlay.style.opacity = panicPulse;
                
                // Dynamically shift background color between toxic green and alarm red!
                float colorLerp = Mathf.PingPong(Time.time * 4f, 1f);
                Color currentColor = Color.Lerp(new Color(0f, 0.45f, 0.05f, 0.5f), new Color(0.6f, 0.05f, 0.05f, 0.5f), colorLerp);
                _sanitationPanicOverlay.style.backgroundImage = new StyleBackground(CreateVignetteTexture(currentColor));
            }
            else
            {
                _sanitationPanicOverlay.style.display = DisplayStyle.None;
            }

            // 5. Carbonation Overload - Cyan Vignette and Floating Bubbles!
            if (effects.IsCarbonationOverloadActive)
            {
                _carbonationOverlay.style.display = DisplayStyle.Flex;
                for (int i = 0; i < _bubbles.Count; i++)
                {
                    Vector2 pos = _bubblePositions[i];
                    pos.y -= _bubbleSpeeds[i] * Time.deltaTime; // Floating UP!
                    if (pos.y < -30f)
                    {
                        pos.y = 1450f; // reset to bottom
                        pos.x = UnityEngine.Random.Range(20f, 2540f);
                        _bubbleSpeeds[i] = UnityEngine.Random.Range(80f, 220f);
                    }
                    _bubblePositions[i] = pos;
                    _bubbles[i].style.left = pos.x;
                    _bubbles[i].style.top = pos.y;
                }
            }
            else
            {
                _carbonationOverlay.style.display = DisplayStyle.None;
            }

            // 6. Alcohol Intoxication - Swaying Pinkish-Purple Vignette and Head Spinning!
            if (effects.IsAlcoholIntoxicationActive)
            {
                _alcoholOverlay.style.display = DisplayStyle.Flex;
                float alcoholPulse = 0.45f + Mathf.Sin(Time.time * 2f) * 0.15f; // breathing effect
                _alcoholOverlay.style.opacity = alcoholPulse;
                
                // Slow rotation/spin sway representation
                float spinRotation = Mathf.Sin(Time.time * 0.8f) * 4f; // 4 degrees slow sway
                _alcoholOverlay.style.rotate = new StyleRotate(new Rotate(new Angle(spinRotation)));
            }
            else
            {
                _alcoholOverlay.style.display = DisplayStyle.None;
            }
        }

        // Price Input modal elements
        private VisualElement _priceInputModal;
        private Label _priceInputSubtitle;
        private TextField _priceInputField;
        private Button _priceInputConfirmBtn;
        private Button _priceInputCancelBtn;
        private System.Action<float> _priceInputCallback;

        private void BuildPriceInputModal()
        {
            _priceInputModal = new VisualElement();
            _priceInputModal.style.position = Position.Absolute;
            _priceInputModal.style.left = 0f;
            _priceInputModal.style.top = 0f;
            _priceInputModal.style.right = 0f;
            _priceInputModal.style.bottom = 0f;
            _priceInputModal.style.backgroundColor = new StyleColor(new Color(0f, 0f, 0f, 0.60f));
            _priceInputModal.style.alignItems = Align.Center;
            _priceInputModal.style.justifyContent = Justify.Center;
            _priceInputModal.style.display = DisplayStyle.None;
            _hudRoot.Add(_priceInputModal);

            var priceCard = new VisualElement();
            priceCard.style.width = 380f;
            priceCard.style.backgroundColor = new StyleColor(new Color(0.08f, 0.08f, 0.09f, 0.96f));
            priceCard.style.SetBorderRadius(16f);
            priceCard.style.SetBorderWidth(1.5f);
            priceCard.style.SetBorderColor(new Color(0.91f, 0.64f, 0.26f, 1f));
            priceCard.style.paddingLeft = 24f;
            priceCard.style.paddingRight = 24f;
            priceCard.style.paddingTop = 24f;
            priceCard.style.paddingBottom = 24f;
            _priceInputModal.Add(priceCard);

            var priceTitle = new Label("УСТАНОВИТЬ ЦЕНУ");
            priceTitle.style.fontSize = 18f;
            priceTitle.style.color = new StyleColor(new Color(0.91f, 0.64f, 0.26f, 1f));
            priceTitle.style.unityFontStyleAndWeight = FontStyle.Bold;
            priceTitle.style.marginBottom = 4f;
            priceCard.Add(priceTitle);

            _priceInputSubtitle = new Label("Стеллаж");
            _priceInputSubtitle.style.fontSize = 13f;
            _priceInputSubtitle.style.color = new StyleColor(Color.white);
            _priceInputSubtitle.style.marginBottom = 16f;
            _priceInputSubtitle.style.unityFontStyleAndWeight = FontStyle.Italic;
            priceCard.Add(_priceInputSubtitle);

            _priceInputField = new TextField();
            _priceInputField.style.height = 40f;
            _priceInputField.style.backgroundColor = new StyleColor(Color.white);
            _priceInputField.style.SetBorderRadius(8f);
            _priceInputField.style.marginBottom = 20f;
            _priceInputField.style.paddingLeft = 8f;
            _priceInputField.style.paddingRight = 8f;
            
            var priceTextInput = _priceInputField.Q("unity-text-input");
            if (priceTextInput != null)
            {
                priceTextInput.style.color = new StyleColor(new Color(0.1f, 0.1f, 0.1f, 1f));
                priceTextInput.style.fontSize = 16f;
                priceTextInput.style.unityFontStyleAndWeight = FontStyle.Bold;
            }
            priceCard.Add(_priceInputField);

            var btnRow = new VisualElement();
            btnRow.style.flexDirection = FlexDirection.Row;
            btnRow.style.justifyContent = Justify.SpaceBetween;
            btnRow.style.width = Length.Percent(100);
            priceCard.Add(btnRow);

            _priceInputCancelBtn = new Button();
            _priceInputCancelBtn.text = "Отмена";
            _priceInputCancelBtn.style.width = Length.Percent(46);
            _priceInputCancelBtn.style.height = 40f;
            _priceInputCancelBtn.style.backgroundColor = new StyleColor(new Color(0.2f, 0.2f, 0.22f, 1f));
            _priceInputCancelBtn.style.color = new StyleColor(Color.white);
            _priceInputCancelBtn.style.SetBorderRadius(8f);
            _priceInputCancelBtn.style.unityFontStyleAndWeight = FontStyle.Bold;
            _priceInputCancelBtn.style.SetBorderWidth(0f);
            btnRow.Add(_priceInputCancelBtn);

            _priceInputConfirmBtn = new Button();
            _priceInputConfirmBtn.text = "Принять";
            _priceInputConfirmBtn.style.width = Length.Percent(46);
            _priceInputConfirmBtn.style.height = 40f;
            _priceInputConfirmBtn.style.backgroundColor = new StyleColor(new Color(0.91f, 0.64f, 0.26f, 1f));
            _priceInputConfirmBtn.style.color = new StyleColor(new Color(0.1f, 0.08f, 0.05f, 1f));
            _priceInputConfirmBtn.style.SetBorderRadius(8f);
            _priceInputConfirmBtn.style.unityFontStyleAndWeight = FontStyle.Bold;
            _priceInputConfirmBtn.style.SetBorderWidth(0f);
            btnRow.Add(_priceInputConfirmBtn);

            _priceInputCancelBtn.RegisterCallback<MouseEnterEvent>(evt => _priceInputCancelBtn.style.backgroundColor = new StyleColor(new Color(0.25f, 0.25f, 0.28f, 1f)));
            _priceInputCancelBtn.RegisterCallback<MouseLeaveEvent>(evt => _priceInputCancelBtn.style.backgroundColor = new StyleColor(new Color(0.2f, 0.2f, 0.22f, 1f)));
            _priceInputConfirmBtn.RegisterCallback<MouseEnterEvent>(evt => _priceInputConfirmBtn.style.backgroundColor = new StyleColor(new Color(0.98f, 0.72f, 0.3f, 1f)));
            _priceInputConfirmBtn.RegisterCallback<MouseLeaveEvent>(evt => _priceInputConfirmBtn.style.backgroundColor = new StyleColor(new Color(0.91f, 0.64f, 0.26f, 1f)));

            _priceInputCancelBtn.clicked += HidePriceInputPanel;
            _priceInputConfirmBtn.clicked += SubmitPriceInput;

            _priceInputField.RegisterCallback<KeyDownEvent>(evt => {
                if (evt.keyCode == KeyCode.Return || evt.keyCode == KeyCode.KeypadEnter)
                {
                    SubmitPriceInput();
                }
                else if (evt.keyCode == KeyCode.Escape)
                {
                    HidePriceInputPanel();
                }
            });
        }

        public void ShowPriceInputPanel(string standType, float currentPrice, System.Action<float> onConfirm)
        {
            if (_priceInputModal == null) return;

            _priceInputSubtitle.text = standType;
            _priceInputField.value = currentPrice.ToString("F2", System.Globalization.CultureInfo.InvariantCulture);
            _priceInputCallback = onConfirm;

            _priceInputModal.style.display = DisplayStyle.Flex;

            UnityEngine.Cursor.lockState = UnityEngine.CursorLockMode.None;
            UnityEngine.Cursor.visible = true;

            _priceInputField.Focus();
        }

        private void HidePriceInputPanel()
        {
            if (_priceInputModal == null) return;
            _priceInputModal.style.display = DisplayStyle.None;

            UnityEngine.Cursor.lockState = UnityEngine.CursorLockMode.Locked;
            UnityEngine.Cursor.visible = false;
        }

        private void SubmitPriceInput()
        {
            if (_priceInputField == null) return;
            
            float newPrice = ParsePrice(_priceInputField.value);
            _priceInputCallback?.Invoke(newPrice);
            HidePriceInputPanel();
        }

        private float ParsePrice(string text)
        {
            text = text.Replace(',', '.').Trim();
            if (float.TryParse(text, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out float result))
            {
                return Mathf.Max(0f, result);
            }
            return 0f;
        }
    }
}
