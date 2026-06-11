using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using LemonEmpire.Core;
using LemonEmpire.Trading;
using LemonEmpire.Network;

namespace LemonEmpire.UI
{
    public class DialogueUI : MonoBehaviour
    {
        public static DialogueUI Instance { get; private set; }

        private NPCBuyer _currentNPC;
        private float _currentReaction;
        private Action<bool> _onDialogueComplete;

        private UIDocument _dialogueUIDoc;
        private VisualElement _dialogueRoot;

        // UI Toolkit references
        private Label _archetypeHeaderLabel;
        private Label _npcDialogueTextLabel;
        private VisualElement _receiptCard;
        private Label _receiptDrinkNameLabel;
        private Label _statTaraLabel;
        private Label _statSugarLabel;
        private Label _statGasLabel;
        private Label _statAlcoholLabel;
        private Label _statTempLabel;
        private VisualElement _buttonsContainer;
        private VisualElement _censorBar;

        public bool IsActive => _dialogueRoot != null && _dialogueRoot.style.display == DisplayStyle.Flex;

        private int _currentHaggleDiscount = 0;
        private bool _isDesperate = false;
        private float _jitterTimer = 0f;
        private static int _illegalSalesCount = 0;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            // Deactivate old uGUI dialogue canvas
            var canvas = GameObject.Find("DialogueCanvas");
            if (canvas != null)
            {
                canvas.SetActive(false);
            }
        }

        private void Start()
        {
            BuildUI();
        }

        private void Update()
        {
            if (IsActive && Player.PlayerStatusEffects.Instance != null)
            {
                if (Player.PlayerStatusEffects.Instance.IsSugarRushActive)
                {
                    _jitterTimer -= Time.unscaledDeltaTime;
                    if (_jitterTimer <= 0f)
                    {
                        _jitterTimer = 0.08f;
                        DistortPrices();
                    }
                }
                else
                {
                    // Reset distortions
                    if (_dialogueRoot != null)
                    {
                        _dialogueRoot.transform.rotation = Quaternion.identity;
                        _dialogueRoot.transform.scale = Vector3.one;
                    }
                }
            }
        }

        private void DistortPrices()
        {
            if (_dialogueRoot != null)
            {
                float rZ = UnityEngine.Random.Range(-2f, 2f);
                float rS = UnityEngine.Random.Range(0.97f, 1.03f);
                _dialogueRoot.transform.rotation = Quaternion.Euler(0, 0, rZ);
                _dialogueRoot.transform.scale = new Vector3(rS, rS, 1f);
            }
        }

        private void BuildUI()
        {
            var go = new GameObject("DialogueUIDocument");
            go.transform.SetParent(transform);
            _dialogueUIDoc = go.AddComponent<UIDocument>();

            var settings = Resources.Load<PanelSettings>("TabletPanelSettings");
            if (settings != null)
            {
                _dialogueUIDoc.panelSettings = settings;
            }

            _dialogueRoot = _dialogueUIDoc.rootVisualElement;
            _dialogueRoot.style.position = Position.Absolute;
            _dialogueRoot.style.left = 0f;
            _dialogueRoot.style.top = 0f;
            _dialogueRoot.style.right = 0f;
            _dialogueRoot.style.bottom = 0f;
            _dialogueRoot.style.backgroundColor = new StyleColor(new Color(0f, 0f, 0f, 0.75f));
            _dialogueRoot.style.alignItems = Align.Center;
            _dialogueRoot.style.justifyContent = Justify.Center;
            _dialogueRoot.style.display = DisplayStyle.None;

            // Horizontal Central columns container
            var colsContainer = new VisualElement();
            colsContainer.style.flexDirection = FlexDirection.Row;
            colsContainer.style.alignItems = Align.Stretch;
            colsContainer.style.justifyContent = Justify.Center;
            _dialogueRoot.Add(colsContainer);

            // ── Column 1: Speech Box ──────────────────────────
            var speechCard = new VisualElement();
            speechCard.style.width = 529f; // Enlarged by 15% from 460f
            speechCard.style.marginRight = 28f; // Enlarged by 15% from 24f
            speechCard.style.backgroundColor = new StyleColor(new Color(0.08f, 0.08f, 0.09f, 0.94f));
            speechCard.style.borderTopLeftRadius = 18f;
            speechCard.style.borderTopRightRadius = 18f;
            speechCard.style.borderBottomLeftRadius = 18f;
            speechCard.style.borderBottomRightRadius = 18f;
            speechCard.style.borderLeftWidth = 1f;
            speechCard.style.borderRightWidth = 1f;
            speechCard.style.borderTopWidth = 1f;
            speechCard.style.borderBottomWidth = 1f;
            speechCard.style.borderLeftColor = new StyleColor(new Color(0.23f, 0.21f, 0.18f, 1f));
            speechCard.style.borderRightColor = new StyleColor(new Color(0.23f, 0.21f, 0.18f, 1f));
            speechCard.style.borderTopColor = new StyleColor(new Color(0.23f, 0.21f, 0.18f, 1f));
            speechCard.style.borderBottomColor = new StyleColor(new Color(0.23f, 0.21f, 0.18f, 1f));
            speechCard.style.paddingLeft = 28f;
            speechCard.style.paddingRight = 28f;
            speechCard.style.paddingTop = 23f;
            speechCard.style.paddingBottom = 23f;
            colsContainer.Add(speechCard);

            _archetypeHeaderLabel = new Label("АРХЕТИП: ХИПСТЕР (ИВАН)");
            _archetypeHeaderLabel.style.fontSize = 14f; // Enlarged by 15% from 12f
            _archetypeHeaderLabel.style.color = new StyleColor(new Color(0.72f, 0.55f, 0.20f, 1f));
            _archetypeHeaderLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            _archetypeHeaderLabel.style.marginBottom = 18f;
            speechCard.Add(_archetypeHeaderLabel);

            // Add the black censor bar overlay
            _censorBar = new VisualElement();
            _censorBar.style.position = Position.Absolute;
            _censorBar.style.left = 20f;
            _censorBar.style.top = 20f;
            _censorBar.style.width = 480f;
            _censorBar.style.height = 28f;
            _censorBar.style.backgroundColor = new StyleColor(Color.black);
            _censorBar.style.alignItems = Align.Center;
            _censorBar.style.justifyContent = Justify.Center;
            _censorBar.style.borderLeftColor = new StyleColor(Color.red);
            _censorBar.style.borderRightColor = new StyleColor(Color.red);
            _censorBar.style.borderTopColor = new StyleColor(Color.red);
            _censorBar.style.borderBottomColor = new StyleColor(Color.red);
            _censorBar.style.borderLeftWidth = 1.5f;
            _censorBar.style.borderRightWidth = 1.5f;
            _censorBar.style.borderTopWidth = 1.5f;
            _censorBar.style.borderBottomWidth = 1.5f;

            var censorLabel = new Label("ШПИОН ДЕТЕКТИРОВАН // ЦЕНЗУРА ГБ");
            censorLabel.style.color = new StyleColor(Color.red);
            censorLabel.style.fontSize = 12f;
            censorLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            _censorBar.Add(censorLabel);
            _censorBar.style.display = DisplayStyle.None;
            speechCard.Add(_censorBar);

            _npcDialogueTextLabel = new Label("...");
            _npcDialogueTextLabel.style.fontSize = 17f; // Enlarged by 15% from 15f
            _npcDialogueTextLabel.style.color = new StyleColor(Color.white);
            _npcDialogueTextLabel.style.unityFontStyleAndWeight = FontStyle.Normal;
            _npcDialogueTextLabel.style.whiteSpace = WhiteSpace.Normal;
            speechCard.Add(_npcDialogueTextLabel);

            // ── Column 2: Dash-Bordered Receipt ──────────────────────────
            _receiptCard = new VisualElement();
            _receiptCard.style.width = 449f; // Enlarged by 15% from 390f
            _receiptCard.style.marginRight = 28f; // Enlarged by 15% from 24f
            _receiptCard.style.backgroundColor = new StyleColor(Color.white);
            _receiptCard.style.borderTopLeftRadius = 14f;
            _receiptCard.style.borderTopRightRadius = 14f;
            _receiptCard.style.borderBottomLeftRadius = 14f;
            _receiptCard.style.borderBottomRightRadius = 14f;
            
            // UI Toolkit dashed border styles
            _receiptCard.style.borderLeftWidth = 2f;
            _receiptCard.style.borderRightWidth = 2f;
            _receiptCard.style.borderTopWidth = 2f;
            _receiptCard.style.borderBottomWidth = 2f;
            _receiptCard.style.borderLeftColor = new StyleColor(new Color(0.72f, 0.55f, 0.20f, 1f));
            _receiptCard.style.borderRightColor = new StyleColor(new Color(0.72f, 0.55f, 0.20f, 1f));
            _receiptCard.style.borderTopColor = new StyleColor(new Color(0.72f, 0.55f, 0.20f, 1f));
            _receiptCard.style.borderBottomColor = new StyleColor(new Color(0.72f, 0.55f, 0.20f, 1f));
            _receiptCard.style.paddingLeft = 23f;
            _receiptCard.style.paddingRight = 23f;
            _receiptCard.style.paddingTop = 28f;
            _receiptCard.style.paddingBottom = 28f;
            _receiptCard.style.alignItems = Align.Center;
            colsContainer.Add(_receiptCard);

            _receiptDrinkNameLabel = new Label("Ягодный Взрыв");
            _receiptDrinkNameLabel.style.fontSize = 23f; // Enlarged by 15% from 20f
            _receiptDrinkNameLabel.style.color = new StyleColor(new Color(0.08f, 0.08f, 0.09f, 1f));
            _receiptDrinkNameLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            _receiptDrinkNameLabel.style.marginBottom = 7f;
            _receiptCard.Add(_receiptDrinkNameLabel);

            var receiptDivider = new VisualElement();
            receiptDivider.style.width = Length.Percent(100);
            receiptDivider.style.height = 1f;
            receiptDivider.style.backgroundColor = new StyleColor(new Color(0.08f, 0.08f, 0.09f, 0.8f));
            receiptDivider.style.marginBottom = 18f;
            _receiptCard.Add(receiptDivider);

            // Receipt stats rows
            _statTaraLabel = CreateReceiptRow(_receiptCard, "Тара:", "Стекло");
            _statSugarLabel = CreateReceiptRow(_receiptCard, "Сахар:", "45%");
            _statGasLabel = CreateReceiptRow(_receiptCard, "Газ:", "80%");
            _statAlcoholLabel = CreateReceiptRow(_receiptCard, "Алкоголь:", "4%");
            _statTempLabel = CreateReceiptRow(_receiptCard, "Темп.:", "6°C (Холод)");
            _statTempLabel.style.color = new StyleColor(new Color(0f, 0.40f, 1.0f, 1f)); // Blue Cold

            var receiptDividerBottom = new VisualElement();
            receiptDividerBottom.style.width = Length.Percent(100);
            receiptDividerBottom.style.height = 1f;
            receiptDividerBottom.style.backgroundColor = new StyleColor(new Color(0.08f, 0.08f, 0.09f, 0.3f));
            receiptDividerBottom.style.marginTop = 18f;
            receiptDividerBottom.style.marginBottom = 18f;
            _receiptCard.Add(receiptDividerBottom);

            var footer = new Label("ООО \"Лемон Импайр\"");
            footer.style.fontSize = 13f; // Enlarged by 15% from 11f
            footer.style.color = new StyleColor(new Color(0.5f, 0.5f, 0.5f, 1f));
            footer.style.unityTextAlign = TextAnchor.MiddleCenter;
            _receiptCard.Add(footer);

            // ── Column 3: Bargaining Tray ──────────────────────────
            var bargainCard = new VisualElement();
            bargainCard.style.width = 575f; // Enlarged by 15% from 500f
            bargainCard.style.backgroundColor = new StyleColor(new Color(0.08f, 0.08f, 0.09f, 0.94f));
            bargainCard.style.borderTopLeftRadius = 18f;
            bargainCard.style.borderTopRightRadius = 18f;
            bargainCard.style.borderBottomLeftRadius = 18f;
            bargainCard.style.borderBottomRightRadius = 18f;
            bargainCard.style.borderLeftWidth = 1f;
            bargainCard.style.borderRightWidth = 1f;
            bargainCard.style.borderTopWidth = 1f;
            bargainCard.style.borderBottomWidth = 1f;
            bargainCard.style.borderLeftColor = new StyleColor(new Color(0.23f, 0.21f, 0.18f, 1f));
            bargainCard.style.borderRightColor = new StyleColor(new Color(0.23f, 0.21f, 0.18f, 1f));
            bargainCard.style.borderTopColor = new StyleColor(new Color(0.23f, 0.21f, 0.18f, 1f));
            bargainCard.style.borderBottomColor = new StyleColor(new Color(0.23f, 0.21f, 0.18f, 1f));
            bargainCard.style.paddingLeft = 28f;
            bargainCard.style.paddingRight = 28f;
            bargainCard.style.paddingTop = 23f;
            bargainCard.style.paddingBottom = 23f;
            colsContainer.Add(bargainCard);

            var optionsTitle = new Label("ВАРИАНТЫ ТОРГА");
            optionsTitle.style.fontSize = 14f; // Enlarged by 15% from 12f
            optionsTitle.style.color = new StyleColor(new Color(0.72f, 0.55f, 0.20f, 1f));
            optionsTitle.style.unityFontStyleAndWeight = FontStyle.Bold;
            optionsTitle.style.marginBottom = 18f;
            bargainCard.Add(optionsTitle);

            _buttonsContainer = new VisualElement();
            _buttonsContainer.style.flexGrow = 1f;
            bargainCard.Add(_buttonsContainer);
        }

        private Label CreateReceiptRow(VisualElement parent, string title, string val)
        {
            var row = new VisualElement();
            row.style.flexDirection = FlexDirection.Row;
            row.style.justifyContent = Justify.SpaceBetween;
            row.style.width = Length.Percent(100);
            row.style.marginBottom = 6f;
            parent.Add(row);

            var titleLbl = new Label(title);
            titleLbl.style.fontSize = 15f; // Enlarged by 15% from 13f
            titleLbl.style.color = new StyleColor(new Color(0.08f, 0.08f, 0.09f, 0.6f));
            row.Add(titleLbl);

            var valLbl = new Label(val);
            valLbl.style.fontSize = 15f; // Enlarged by 15% from 13f
            valLbl.style.color = new StyleColor(new Color(0.08f, 0.08f, 0.09f, 1f));
            valLbl.style.unityFontStyleAndWeight = FontStyle.Bold;
            row.Add(valLbl);

            return valLbl;
        }

        public void PrefetchDialogue(NPCBuyer npc, float reaction)
        {
            if (npc.CachedDialogResponse != null) return;

            var db = DialogueDatabase.Instance;
            if (db == null) return;

            float morale = PlayerVitals.Instance != null ? PlayerVitals.Instance.Morale : 50f;
            if (Player.PlayerStatusEffects.Instance != null && Player.PlayerStatusEffects.Instance.IsFinancialDepressionActive)
            {
                morale = 0f;
            }

            var greetingLine = db.GetGreeting(reaction);
            string greeting = greetingLine != null ? greetingLine.text : "Привет.";

            List<DialogueLine> replies = db.GetPlayerReplies(morale);
            DialogOption[] options = new DialogOption[replies.Count];
            float[] bonuses = new float[replies.Count];

            for (int i = 0; i < replies.Count; i++)
            {
                options[i] = new DialogOption
                {
                    id = i,
                    text = replies[i].text,
                    type = replies[i].effect ?? "normal"
                };
                bonuses[i] = replies[i].reactionBonus;
            }

            npc.CachedDialogResponse = new NpcResponse
            {
                npc_greeting = greeting,
                player_options = options
            };
            npc.ReplyReactionBonuses = bonuses;
        }

        public void StartDialogue(NPCBuyer npc, float reaction, Action<bool> onComplete)
        {
            _currentNPC = npc;
            _currentReaction = reaction;
            _onDialogueComplete = onComplete;
            _currentHaggleDiscount = 0;
            _isDesperate = false;

            Time.timeScale = 0f;
            UnityEngine.Cursor.lockState = UnityEngine.CursorLockMode.None;
            UnityEngine.Cursor.visible = true;

            // Archetype styling
            if (_archetypeHeaderLabel != null)
            {
                string nameStr = (Player.PlayerStatusEffects.Instance != null && Player.PlayerStatusEffects.Instance.IsLemonParanoiaActive) 
                    ? "Шпион конкурентов" : npc.gameObject.name;
                _archetypeHeaderLabel.text = $"АРХЕТИП: {npc.Archetype.ToString().ToUpper()} ({nameStr.ToUpper()})";
            }

            if (_censorBar != null)
            {
                bool paranoia = Player.PlayerStatusEffects.Instance != null && Player.PlayerStatusEffects.Instance.IsLemonParanoiaActive;
                _censorBar.style.display = paranoia ? DisplayStyle.Flex : DisplayStyle.None;
            }

            // Check if there is a ServiceCounter Placed Bottle
            var counter = FindFirstObjectByType<Production.ServiceCounter>();
            ItemBase bottle = (counter != null) ? counter.PlacedDrink : null;
            
            if (bottle != null)
            {
                _receiptCard.style.display = DisplayStyle.Flex;
                PopulateReceipt(bottle);
                ShowServiceCounterDialogue(npc, bottle);
            }
            else
            {
                // Stand dialogue - Hide product receipt
                _receiptCard.style.display = DisplayStyle.None;
                if (_currentNPC.CachedDialogResponse == null)
                {
                    PrefetchDialogue(_currentNPC, _currentReaction);
                }
                ShowDialogue();
            }

            if (_dialogueRoot != null)
            {
                _dialogueRoot.style.display = DisplayStyle.Flex;
            }
        }

        private void PopulateReceipt(ItemBase bottle)
        {
            if (_receiptDrinkNameLabel != null) _receiptDrinkNameLabel.text = bottle.DrinkName;
            if (_statTaraLabel != null) _statTaraLabel.text = bottle.Packaging.ToString();
            if (_statSugarLabel != null) _statSugarLabel.text = $"{Mathf.RoundToInt(bottle.Sugar)}%";
            if (_statGasLabel != null) _statGasLabel.text = $"{Mathf.RoundToInt(bottle.Carbonation)}%";
            if (_statAlcoholLabel != null) _statAlcoholLabel.text = $"{Mathf.RoundToInt(bottle.Alcohol)}%";
            
            if (_statTempLabel != null)
            {
                string tempType = bottle.Temperature <= 12f ? "Холод" : "Теплый";
                _statTempLabel.text = $"{Mathf.RoundToInt(bottle.Temperature)}°C ({tempType})";
                _statTempLabel.style.color = new StyleColor(bottle.Temperature <= 12f ? new Color(0f, 0.40f, 1.0f, 1f) : new Color(0.95f, 0.38f, 0.35f, 1f));
            }
        }

        private void AddDialogueButton(string text, Color dotColor, bool enabled, System.Action onClick)
        {
            var btn = new VisualElement();
            btn.style.flexDirection = FlexDirection.Row;
            btn.style.alignItems = Align.Center;
            btn.style.minHeight = 53f; // Enlarged by 15% from 46f
            btn.style.marginBottom = 12f; // Enlarged by 15% from 10f
            btn.style.paddingLeft = 18f; // Enlarged by 15% from 16f
            btn.style.paddingRight = 18f; // Enlarged by 15% from 16f
            btn.style.paddingTop = 9f; // Enlarged by 15% from 8f
            btn.style.paddingBottom = 9f; // Enlarged by 15% from 8f
            btn.style.width = Length.Percent(100); // Constrain width to 100% of container to force text wrapping
            btn.style.backgroundColor = new StyleColor(new Color(0.12f, 0.12f, 0.14f, 0.90f));
            btn.style.borderTopLeftRadius = 12f; // Enlarged by 15% from 10f
            btn.style.borderTopRightRadius = 12f;
            btn.style.borderBottomLeftRadius = 12f;
            btn.style.borderBottomRightRadius = 12f;
            btn.style.borderLeftWidth = 1f;
            btn.style.borderRightWidth = 1f;
            btn.style.borderTopWidth = 1f;
            btn.style.borderBottomWidth = 1f;
            btn.style.borderLeftColor = new StyleColor(new Color(0.25f, 0.22f, 0.18f, 1f));
            btn.style.borderRightColor = new StyleColor(new Color(0.25f, 0.22f, 0.18f, 1f));
            btn.style.borderTopColor = new StyleColor(new Color(0.25f, 0.22f, 0.18f, 1f));
            btn.style.borderBottomColor = new StyleColor(new Color(0.25f, 0.22f, 0.18f, 1f));

            if (!enabled)
            {
                btn.style.opacity = 0.40f;
                dotColor = new Color(0.5f, 0.5f, 0.5f, 1f); // Grey dot
            }

            // Distort price label in Sugar Rush
            if (Player.PlayerStatusEffects.Instance != null && Player.PlayerStatusEffects.Instance.IsSugarRushActive)
            {
                text = text.Replace("$18.00", "$☠.☠☠")
                           .Replace("$14.40", "$??.??")
                           .Replace("18", "☠☠")
                           .Replace("14", "??")
                           .Replace("$", "💸");
            }

            var dot = new VisualElement();
            dot.style.width = 12f; // Enlarged by 15% from 10f
            dot.style.height = 12f;
            dot.style.borderTopLeftRadius = 6f; // Enlarged by 15% from 5f
            dot.style.borderTopRightRadius = 6f;
            dot.style.borderBottomLeftRadius = 6f;
            dot.style.borderBottomRightRadius = 6f;
            dot.style.backgroundColor = new StyleColor(dotColor);
            dot.style.marginRight = 14f; // Enlarged by 15% from 12f
            dot.style.flexShrink = 0f;
            btn.Add(dot);

            var lbl = new Label(text);
            lbl.style.fontSize = 16f; // Enlarged by 15% from 14f
            lbl.style.color = new StyleColor(Color.white);
            lbl.style.unityFontStyleAndWeight = FontStyle.Bold;
            lbl.style.whiteSpace = WhiteSpace.Normal;
            lbl.style.flexGrow = 1f;
            lbl.style.flexShrink = 1f;
            btn.Add(lbl);

            if (enabled)
            {
                btn.RegisterCallback<ClickEvent>(evt => onClick());
                btn.RegisterCallback<MouseEnterEvent>(evt => {
                    btn.style.backgroundColor = new StyleColor(new Color(0.18f, 0.18f, 0.22f, 0.95f));
                    btn.style.borderLeftColor = new StyleColor(new Color(0.91f, 0.64f, 0.26f, 1f));
                    btn.style.borderRightColor = new StyleColor(new Color(0.91f, 0.64f, 0.26f, 1f));
                    btn.style.borderTopColor = new StyleColor(new Color(0.91f, 0.64f, 0.26f, 1f));
                    btn.style.borderBottomColor = new StyleColor(new Color(0.91f, 0.64f, 0.26f, 1f));
                    btn.transform.scale = new Vector3(1.02f, 1.02f, 1f);
                });
                btn.RegisterCallback<MouseLeaveEvent>(evt => {
                    btn.style.backgroundColor = new StyleColor(new Color(0.12f, 0.12f, 0.14f, 0.90f));
                    btn.style.borderLeftColor = new StyleColor(new Color(0.25f, 0.22f, 0.18f, 1f));
                    btn.style.borderRightColor = new StyleColor(new Color(0.25f, 0.22f, 0.18f, 1f));
                    btn.style.borderTopColor = new StyleColor(new Color(0.25f, 0.22f, 0.18f, 1f));
                    btn.style.borderBottomColor = new StyleColor(new Color(0.25f, 0.22f, 0.18f, 1f));
                    btn.transform.scale = Vector3.one;
                });
            }

            _buttonsContainer.Add(btn);
        }

        private void ShowServiceCounterDialogue(NPCBuyer npc, ItemBase bottle)
        {
            _buttonsContainer.Clear();

            // Intercept if Paranoia is active!
            if (Player.PlayerStatusEffects.Instance != null && Player.PlayerStatusEffects.Instance.IsLemonParanoiaActive)
            {
                if (_npcDialogueTextLabel != null)
                {
                    _npcDialogueTextLabel.text = "[ШПИОН ДЕТЕКТИРОВАН]\n\n" + 
                        "Этот подозрительный субъект слишком пристально высматривает коммерческую тайну нашего лимонада! Его маскировка выглядит фальшиво, а блокнот так и просится под карандаш. " + 
                        "Что будем делать, босс?";
                }

                AddDialogueButton("🔴 «Я знаю, что ты шпион конкурентов! Убирайся!»", new Color(0.95f, 0.38f, 0.35f, 1f), true, () => {
                    if (_npcDialogueTextLabel != null)
                        _npcDialogueTextLabel.text = "«Ч-что?! Какой еще шпион?! Вы сумасшедший!»\n\n(Покупатель в панике бросает бутылку и убегает)";
                    ShowServiceCloseButton(false);
                });

                AddDialogueButton("⚡ «Признавайся, сколько тебе заплатили за слежку?!»", new Color(0.90f, 0.70f, 0.15f, 1f), true, () => {
                    bool intimidated = UnityEngine.Random.value < 0.5f;
                    if (intimidated)
                    {
                        float fearPrice = bottle.RetailPrice * 1.5f;
                        if (_npcDialogueTextLabel != null)
                            _npcDialogueTextLabel.text = $"«Пожалуйста, не кричите! Я просто хотел попить! Держите ваши ${fearPrice:F2} и не трогайте меня!»\n\n(Испуганный шпион переплачивает 50% и в спешке убегает с бутылкой)";
                        _buttonsContainer.Clear();
                        AddDialogueButton("Забрать деньги", new Color(0.33f, 0.87f, 0.42f, 1f), true, () => {
                            SellBottle(bottle, fearPrice);
                        });
                    }
                    else
                    {
                        if (_npcDialogueTextLabel != null)
                            _npcDialogueTextLabel.text = "«Да вы просто безумец! Я вызываю полицию!»\n\n(Покупатель бросает напиток и в страхе убегает)";
                        ShowServiceCloseButton(false);
                    }
                });

                AddDialogueButton("🔴 «Твоя маскировка ужасна! Вон отсюда!»", new Color(0.95f, 0.38f, 0.35f, 1f), true, () => {
                    if (_npcDialogueTextLabel != null)
                        _npcDialogueTextLabel.text = "«Сумасшедший дом... Больше ни ногой сюда!»\n\n(Покупатель быстро ретируется)";
                    ShowServiceCloseButton(false);
                });

                return;
            }

            bool wantsHaggle;
            int discountAmount;
            string generatedText = DialogueGenerator.GenerateLabelInspectionDialogue(npc, bottle, out wantsHaggle, out discountAmount);

            if (_npcDialogueTextLabel != null)
                _npcDialogueTextLabel.text = generatedText;

            if (npc.Archetype == NPCArchetype.Kids && bottle.Alcohol > 0f)
            {
                AddDialogueButton("🔴 Попытаться продать из-под полы", new Color(0.95f, 0.38f, 0.35f, 1f), true, () => {
                    float catchChance = 0.30f + 0.20f * _illegalSalesCount;
                    if (UnityEngine.Random.value < catchChance)
                    {
                        if (EconomyManager.Instance != null)
                        {
                            EconomyManager.Instance.ForceSpend(500f);
                        }

                        var counter = FindFirstObjectByType<Production.ServiceCounter>();
                        if (counter != null && counter.PlacedDrink == bottle)
                        {
                            counter.ClearPlacedDrink();
                        }
                        bottle.Consume();
                        _illegalSalesCount = 0;

                        if (_npcDialogueTextLabel != null)
                        {
                            _npcDialogueTextLabel.text = $"[ПОЛИЦИЯ] Вас поймали на продаже алкоголя несовершеннолетнему!\nНаложен штраф $500! Напиток конфискован.";
                        }
                        ShowServiceCloseButton(false);
                    }
                    else
                    {
                        _illegalSalesCount++;
                        SellBottle(bottle, bottle.RetailPrice);
                        if (_npcDialogueTextLabel != null)
                        {
                            _npcDialogueTextLabel.text = $"[Сделка успешна] Вам повезло! Ребенок тихо забрал бутылку и ушел. (Безнаказанных сделок подряд: {_illegalSalesCount})";
                        }
                    }
                });

                AddDialogueButton("Отказать в сделке / Забрать бутылку", new Color(0.95f, 0.38f, 0.35f, 1f), true, () => {
                    CancelServiceDeal(bottle);
                });

                return;
            }

            float morale = PlayerVitals.Instance != null ? PlayerVitals.Instance.Morale : 50f;
            float cash = EconomyManager.Instance != null ? EconomyManager.Instance.Balance : 1000f;

            bool isRejected = generatedText.Contains("Я это точно не возьму") || 
                              generatedText.Contains("Мне такое нельзя") || 
                              generatedText.Contains("несовместимы") || 
                              generatedText.Contains("тяжелое пойло") ||
                              generatedText.Contains("совсем не подходит");

            if (!isRejected)
            {
                if (!wantsHaggle)
                {
                    AddDialogueButton($"Продать за полную цену (${bottle.RetailPrice:F2})", new Color(0.33f, 0.87f, 0.42f, 1f), true, () => {
                        SellBottle(bottle, bottle.RetailPrice);
                    });
                }
                else
                {
                    float discountedPrice = Mathf.Max(0f, bottle.RetailPrice - discountAmount);
                    AddDialogueButton($"Предложить скидку 20% (${discountedPrice:F2})", new Color(0.95f, 0.60f, 0.22f, 1f), true, () => {
                        SellBottle(bottle, discountedPrice);
                    });
                }
            }

            // Charisma option (Morale > 70%)
            bool isCharismaActive = morale > 70f;
            AddDialogueButton("Харизматичное Убеждение (Требует Мораль > 70%)", new Color(0.90f, 0.70f, 0.15f, 1f), isCharismaActive, () => {
                TryPersuade(npc, bottle, isRejected);
            });

            // Desperation option (Morale < 20% or Financial Depression) and cash < 50
            bool isDepressed = Player.PlayerStatusEffects.Instance != null && Player.PlayerStatusEffects.Instance.IsFinancialDepressionActive;
            bool isDesperateActive = (morale < 20f || isDepressed) && cash < 50;
            AddDialogueButton("Расплакаться (Неактивно)", new Color(0.3f, 0.65f, 1.0f, 1f), isDesperateActive, () => {
                TryWeep(npc, bottle);
            });

            // Always add cancel option
            AddDialogueButton("Отказать в сделке / Забрать бутылку", new Color(0.95f, 0.38f, 0.35f, 1f), true, () => {
                CancelServiceDeal(bottle);
            });
        }

        private void SellBottle(ItemBase bottle, float price)
        {
            if (EconomyManager.Instance != null)
            {
                EconomyManager.Instance.Earn(price);
            }

            var counter = FindFirstObjectByType<Production.ServiceCounter>();
            if (counter != null && counter.PlacedDrink == bottle)
            {
                counter.ClearPlacedDrink();
            }

            bottle.Consume();

            if (_npcDialogueTextLabel != null)
                _npcDialogueTextLabel.text = $"[Сделка успешна] Спасибо! Напиток куплен за ${price:F2}.";

            ShowServiceCloseButton(true);
        }

        private void TryPersuade(NPCBuyer npc, ItemBase bottle, bool originallyRejected)
        {
            bool isAbsoluteRejection = (npc.Archetype == NPCArchetype.Kids && bottle.Alcohol > 0f) ||
                                       (npc.Archetype == NPCArchetype.Athletes && bottle.Alcohol > 0f) ||
                                       (npc.Archetype == NPCArchetype.Hipsters && bottle.Alcohol > 5f);

            if (isAbsoluteRejection)
            {
                if (_npcDialogueTextLabel != null)
                    _npcDialogueTextLabel.text = "Харизма тут бесполезна! Никакие слова не заставят меня взять это.";
                ShowServiceCloseButton(false);
                return;
            }

            bool success = UnityEngine.Random.value < 0.6f;
            if (success)
            {
                if (_npcDialogueTextLabel != null)
                    _npcDialogueTextLabel.text = "Хм... Твои слова звучат убедительно. Ладно, я возьму его по полной стоимости!";
                
                _buttonsContainer.Clear();
                AddDialogueButton("Продать по базовой цене", new Color(0.33f, 0.87f, 0.42f, 1f), true, () => {
                    SellBottle(bottle, bottle.RetailPrice);
                });
            }
            else
            {
                if (_npcDialogueTextLabel != null)
                    _npcDialogueTextLabel.text = "Нет, твои байки на меня не действуют. Не пытайся меня заговорить!";
                ShowServiceCloseButton(false);
            }
        }

        private void TryWeep(NPCBuyer npc, ItemBase bottle)
        {
            bool isAbsoluteRejection = (npc.Archetype == NPCArchetype.Kids && bottle.Alcohol > 0f) ||
                                       (npc.Archetype == NPCArchetype.Athletes && bottle.Alcohol > 0f) ||
                                       (npc.Archetype == NPCArchetype.Hipsters && bottle.Alcohol > 5f);

            if (isAbsoluteRejection)
            {
                if (_npcDialogueTextLabel != null)
                    _npcDialogueTextLabel.text = "Слезы не помогут. Я не собираюсь травиться этим.";
                ShowServiceCloseButton(false);
                return;
            }

            bool success = UnityEngine.Random.value < 0.5f;
            if (success)
            {
                if (_npcDialogueTextLabel != null)
                    _npcDialogueTextLabel.text = "Ох... Бедолага, на тебя больно смотреть. Ладно, давай сюда бутылку, держи деньги.";

                _buttonsContainer.Clear();
                AddDialogueButton("Отдать напиток", new Color(0.33f, 0.87f, 0.42f, 1f), true, () => {
                    SellBottle(bottle, bottle.RetailPrice);
                });
            }
            else
            {
                if (_npcDialogueTextLabel != null)
                    _npcDialogueTextLabel.text = "Фу, какая жалость. Иди ной в другом месте, я не занимаюсь благотворительностью.";
                ShowServiceCloseButton(false);
            }
        }

        private void CancelServiceDeal(ItemBase bottle)
        {
            var carry = FindFirstObjectByType<Player.PlayerCarry>();
            if (carry != null)
            {
                if (carry.TryPickup(bottle))
                {
                    var counter = FindFirstObjectByType<Production.ServiceCounter>();
                    if (counter != null && counter.PlacedDrink == bottle)
                    {
                        counter.ClearPlacedDrink();
                    }
                }
            }

            if (_npcDialogueTextLabel != null)
                _npcDialogueTextLabel.text = "Сделка отменена. Вы забрали напиток обратно.";

            ShowServiceCloseButton(false);
        }

        private void ShowServiceCloseButton(bool success)
        {
            _buttonsContainer.Clear();
            AddDialogueButton("[ Закрыть ]", new Color(0.85f, 0.82f, 0.77f, 1f), true, () => {
                EndDialogue(success);
            });
        }

        // --- Original TradeStand Dialogue Support ---
        private void ShowDialogue()
        {
            _buttonsContainer.Clear();
            var response = _currentNPC.CachedDialogResponse;
            if (response == null || response.player_options == null || response.player_options.Length == 0)
            {
                if (_npcDialogueTextLabel != null)
                    _npcDialogueTextLabel.text = response?.npc_greeting ?? "Привет...";
                ShowFallbackClose();
                return;
            }

            if (_npcDialogueTextLabel != null)
                _npcDialogueTextLabel.text = response.npc_greeting;

            for (int i = 0; i < response.player_options.Length; i++)
            {
                var opt = response.player_options[i];
                AddDialogueButton(opt.text, new Color(0.72f, 0.55f, 0.20f, 1f), true, () => {
                    OnReplyChosen(opt);
                });
            }
        }

        private void ShowFallbackClose()
        {
            AddDialogueButton("[ Уйти ]", new Color(0.85f, 0.82f, 0.77f, 1f), true, () => {
                EndDialogue(false);
            });
        }

        private void OnReplyChosen(DialogOption reply)
        {
            float reactionBonus = 0f;
            if (_currentNPC.ReplyReactionBonuses != null &&
                reply.id >= 0 && reply.id < _currentNPC.ReplyReactionBonuses.Length)
            {
                reactionBonus = _currentNPC.ReplyReactionBonuses[reply.id];
            }

            float finalReaction = _currentReaction + reactionBonus;

            if (reply.type == "desperate")
                _isDesperate = true;

            bool success = false;
            string responseText = "Торговля со стенда отключена.";

            var db = DialogueDatabase.Instance;
            if (db != null)
            {
                var line = success ? db.GetSuccessLine(finalReaction, _isDesperate) : db.GetFailLine(finalReaction);
                responseText = line != null ? line.text : "Недоступно.";
            }

            if (_npcDialogueTextLabel != null)
                _npcDialogueTextLabel.text = responseText;

            _buttonsContainer.Clear();
            AddDialogueButton("[ Закрыть ]", new Color(0.85f, 0.82f, 0.77f, 1f), true, () => {
                EndDialogue(success);
            });
        }

        private void EndDialogue(bool success)
        {
            if (_dialogueRoot != null)
                _dialogueRoot.style.display = DisplayStyle.None;

            Time.timeScale = 1f;
            UnityEngine.Cursor.lockState = UnityEngine.CursorLockMode.Locked;
            UnityEngine.Cursor.visible = false;

            var vitals = PlayerVitals.Instance;
            if (vitals != null)
            {
                if (success)
                    vitals.OnDealSuccess();
                else
                    vitals.OnDealFailed();
            }

            _onDialogueComplete?.Invoke(success);
            _currentNPC = null;
            _onDialogueComplete = null;
        }
    }
}
