using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using LemonEmpire.Core;
using LemonEmpire.Trading;
using LemonEmpire.Network;

namespace LemonEmpire.UI
{
    public class DialogueUI : MonoBehaviour
    {
        public static DialogueUI Instance { get; private set; }

        private Canvas _canvas;
        private GameObject _panel;
        private Text _npcNameText;
        private Text _npcDialogueText;
        private List<Button> _replyButtons = new();
        private List<Text> _replyTexts = new();

        private NPCBuyer _currentNPC;
        private float _currentReaction;
        private Action<bool> _onDialogueComplete;

        public bool IsActive => _panel != null && _panel.activeSelf;

        private int _currentDialogueStep = 1;
        private int _currentHaggleDiscount = 0;
        private bool _isDesperate = false;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            BuildUI();
            _panel.SetActive(false);
        }

        /// <summary>
        /// Предзагрузка диалога для NPC — всё из локального JSON, без бэкенда.
        /// </summary>
        public void PrefetchDialogue(NPCBuyer npc, float reaction)
        {
            if (npc.CachedDialogResponse != null)
                return;

            var db = DialogueDatabase.Instance;
            if (db == null)
            {
                Debug.LogWarning("[DialogueUI] DialogueDatabase.Instance не найден, диалог будет fallback.");
                return;
            }

            float morale = PlayerVitals.Instance != null ? PlayerVitals.Instance.Morale : 50f;

            // Получаем приветствие из JSON
            var greetingLine = db.GetGreeting(reaction);
            string greeting = greetingLine != null ? greetingLine.text : "Привет.";

            // Получаем варианты ответов из JSON
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

            Debug.Log($"<color=green>[DialogueUI] Диалог для {npc.gameObject.name} загружен из JSON (greeting: {greeting})</color>");
        }

        public void StartDialogue(NPCBuyer npc, float reaction, Action<bool> onComplete)
        {
            _currentNPC = npc;
            _currentReaction = reaction;
            _onDialogueComplete = onComplete;
            _currentDialogueStep = 1;
            _currentHaggleDiscount = 0;
            _isDesperate = false;

            _panel.SetActive(true);
            Time.timeScale = 0f;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            _npcNameText.text = npc.gameObject.name;

            foreach (var btn in _replyButtons)
                btn.gameObject.SetActive(false);

            // Если диалог не был предзагружен — грузим сейчас
            if (_currentNPC.CachedDialogResponse == null)
            {
                PrefetchDialogue(_currentNPC, _currentReaction);
            }

            ShowDialogue();
        }

        private void ShowDialogue()
        {
            var response = _currentNPC.CachedDialogResponse;
            if (response == null || response.player_options == null || response.player_options.Length == 0)
            {
                _npcDialogueText.text = response?.npc_greeting ?? "Привет...";
                ShowFallbackClose();
                return;
            }

            _npcDialogueText.text = response.npc_greeting;

            for (int i = 0; i < _replyButtons.Count; i++)
            {
                if (i < response.player_options.Length)
                {
                    _replyButtons[i].gameObject.SetActive(true);
                    _replyTexts[i].text = response.player_options[i].text;

                    var opt = response.player_options[i];
                    _replyButtons[i].onClick.RemoveAllListeners();
                    _replyButtons[i].onClick.AddListener(() => OnReplyChosen(opt));
                }
                else
                {
                    _replyButtons[i].gameObject.SetActive(false);
                }
            }
        }

        private void ShowFallbackClose()
        {
            _replyButtons[0].gameObject.SetActive(true);
            _replyTexts[0].text = "[ Уйти ]";
            _replyButtons[0].onClick.RemoveAllListeners();
            _replyButtons[0].onClick.AddListener(() => EndDialogue(false));
        }

        private void OnReplyChosen(DialogOption reply)
        {
            // Применяем бонус реакции от выбранного ответа
            float reactionBonus = 0f;
            if (_currentNPC.ReplyReactionBonuses != null &&
                reply.id >= 0 && reply.id < _currentNPC.ReplyReactionBonuses.Length)
            {
                reactionBonus = _currentNPC.ReplyReactionBonuses[reply.id];
            }

            float finalReaction = _currentReaction + reactionBonus;

            if (reply.type == "desperate")
                _isDesperate = true;

            // --- Оценка результата сделки ---
            bool success = false;
            string responseText = "";

            var db = DialogueDatabase.Instance;

            if (reply.type == "bargain_2_for_1" || reply.type == "deal")
            {
                // Попытка 2 за 1
                if (_currentNPC.TargetStand != null && _currentNPC.TargetStand.StockedCount >= 2)
                {
                    float fairPriceForTwo = _currentNPC.TargetStand.AverageQuality * 1.0f * (1f + finalReaction * 0.01f) * 2f;
                    fairPriceForTwo = Mathf.Max(fairPriceForTwo, 20f);

                    if (_currentNPC.TargetStand.Price <= fairPriceForTwo)
                    {
                        _currentNPC.TargetStand.SellMultiple(2, _currentNPC.TargetStand.Price);
                        success = true;
                    }
                }

                if (db != null)
                {
                    var line = success ? db.GetSuccessLine(finalReaction, _isDesperate) : db.GetFailLine(finalReaction);
                    responseText = line != null ? line.text : (success ? "Отличная сделка!" : "Нет, дорого.");
                }
                else
                {
                    responseText = success ? "О, два по цене одного? Отличная сделка, забираю оба!" : "Слишком дорого!";
                }
            }
            else if (reply.type == "desperate")
            {
                success = UnityEngine.Random.value < 0.5f;
                if (success && _currentNPC.TargetStand != null)
                {
                    _currentNPC.TargetStand.SellMultiple(1, _currentNPC.TargetStand.Price);
                }

                if (db != null)
                {
                    var line = success ? db.GetSuccessLine(finalReaction, true) : db.GetFailLine(finalReaction);
                    responseText = line != null ? line.text : (success ? "Ладно, давай." : "Нет, иди отсюда.");
                }
                else
                {
                    responseText = success ? "Ладно... бедолага, давай сюда." : "Я не занимаюсь благотворительностью, отвали.";
                }
            }
            else if (reply.type == "agree_discount")
            {
                if (_currentNPC.TargetStand != null && _currentNPC.TargetStand.StockedCount >= 1)
                {
                    int discountedPrice = Mathf.Max(0, _currentNPC.TargetStand.Price - _currentHaggleDiscount);
                    _currentNPC.TargetStand.SellMultiple(1, discountedPrice);
                    success = true;
                }

                if (db != null)
                {
                    var line = success ? db.GetSuccessLine(finalReaction, _isDesperate) : db.GetFailLine(finalReaction);
                    responseText = line != null ? line.text : (success ? "Спасибо!" : "Нет.");
                }
                else
                {
                    responseText = success ? "Отлично! Спасибо за скидку, забираю." : "Нет лимонада? До свидания.";
                }
            }
            else
            {
                // Обычная покупка
                float fairPrice = (_currentNPC.TargetStand != null)
                    ? _currentNPC.TargetStand.AverageQuality * 1.0f * (1f + finalReaction * 0.01f)
                    : 50f;
                fairPrice = Mathf.Max(fairPrice, 10f);
                float price = _currentNPC.TargetStand != null ? _currentNPC.TargetStand.Price : 100;

                success = price <= fairPrice;
                if (success && _currentNPC.TargetStand != null && _currentNPC.TargetStand.StockedCount >= 1)
                {
                    _currentNPC.TargetStand.SellMultiple(1, _currentNPC.TargetStand.Price);
                }
                else if (success)
                {
                    success = false; // No stock left
                }

                if (db != null)
                {
                    var line = success ? db.GetSuccessLine(finalReaction, _isDesperate) : db.GetFailLine(finalReaction);
                    responseText = line != null ? line.text : (success ? "Беру!" : "Дорого.");
                }
                else
                {
                    responseText = success ? "Хм, отличная цена. Беру!" : "Слишком дорого. Я пойду.";
                }
            }

            // Показываем финальную реплику NPC
            _npcDialogueText.text = responseText;

            for (int i = 0; i < _replyButtons.Count; i++)
            {
                _replyButtons[i].gameObject.SetActive(false);
            }

            _replyButtons[0].gameObject.SetActive(true);
            _replyTexts[0].text = "[ Закрыть ]";
            _replyButtons[0].onClick.RemoveAllListeners();

            bool finalSuccess = success;
            _replyButtons[0].onClick.AddListener(() => EndDialogue(finalSuccess));
        }

        private void EndDialogue(bool success)
        {
            _panel.SetActive(false);
            Time.timeScale = 1f;
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;

            // Update morale based on deal result
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

        private void BuildUI()
        {
            var canvasGo = new GameObject("DialogueCanvas");
            canvasGo.transform.SetParent(transform);
            _canvas = canvasGo.AddComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _canvas.sortingOrder = 200;
            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            canvasGo.AddComponent<GraphicRaycaster>();

            _panel = new GameObject("DialoguePanel");
            _panel.transform.SetParent(canvasGo.transform, false);
            var panelRT = _panel.AddComponent<RectTransform>();
            panelRT.anchorMin = new Vector2(0.15f, 0.05f);
            panelRT.anchorMax = new Vector2(0.85f, 0.4f);
            panelRT.offsetMin = Vector2.zero;
            panelRT.offsetMax = Vector2.zero;

            var panelImg = _panel.AddComponent<Image>();
            panelImg.color = new Color(0.08f, 0.08f, 0.12f, 0.95f);

            // NPC Name
            _npcNameText = CreateText(_panel.transform, "NPCName",
                new Vector2(0.02f, 0.8f), new Vector2(0.98f, 0.98f),
                "", 22, Color.yellow, TextAnchor.MiddleLeft);

            // NPC Dialogue Text
            _npcDialogueText = CreateText(_panel.transform, "NPCText",
                new Vector2(0.02f, 0.45f), new Vector2(0.98f, 0.78f),
                "", 18, Color.white, TextAnchor.UpperLeft);

            // Reply Buttons (3 max)
            for (int i = 0; i < 3; i++)
            {
                float yMax = 0.4f - i * 0.13f;
                float yMin = yMax - 0.11f;

                var btnGo = new GameObject($"Reply_{i}");
                btnGo.transform.SetParent(_panel.transform, false);
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

                var txt = CreateText(btnGo.transform, "Text",
                    Vector2.zero, Vector2.one,
                    "", 16, new Color(0.9f, 0.9f, 0.85f), TextAnchor.MiddleLeft);

                _replyButtons.Add(btn);
                _replyTexts.Add(txt);
            }
        }

        private Text CreateText(Transform parent, string name,
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
    }
}
