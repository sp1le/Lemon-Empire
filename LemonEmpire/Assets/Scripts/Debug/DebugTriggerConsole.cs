using UnityEngine;
using LemonEmpire.Core;
using LemonEmpire.Player;
using LemonEmpire.Trading;
using LemonEmpire.UI;
using UnityEngine.UIElements;

namespace LemonEmpire.DebugTools
{
    public class DebugTriggerConsole : MonoBehaviour, IInteractable
    {
        public string InteractionPrompt => "[E] Открыть Debug-панель Состояний";
        public bool CanInteract => true;

        private UIDocument _debugUIDoc;
        private VisualElement _debugRoot;

        public void Interact(PlayerInteractionContext context)
        {
            OpenDebugPanel();
        }

        private void OpenDebugPanel()
        {
            Time.timeScale = 0f;
            UnityEngine.Cursor.lockState = UnityEngine.CursorLockMode.None;
            UnityEngine.Cursor.visible = true;

            if (_debugUIDoc == null)
            {
                var go = new GameObject("DebugUIDocument");
                go.transform.SetParent(transform);
                _debugUIDoc = go.AddComponent<UIDocument>();
                
                var settings = Resources.Load<PanelSettings>("TabletPanelSettings");
                if (settings != null)
                {
                    _debugUIDoc.panelSettings = settings;
                }

                _debugRoot = new VisualElement();
                _debugRoot.style.position = Position.Absolute;
                _debugRoot.style.left = 0f;
                _debugRoot.style.top = 0f;
                _debugRoot.style.right = 0f;
                _debugRoot.style.bottom = 0f;
                _debugRoot.style.backgroundColor = new StyleColor(new Color(0f, 0f, 0f, 0.75f));
                _debugRoot.style.alignItems = Align.Center;
                _debugRoot.style.justifyContent = Justify.Center;

                var card = new VisualElement();
                card.style.width = 460f;
                card.style.backgroundColor = new StyleColor(new Color(0.08f, 0.08f, 0.10f, 0.96f));
                card.style.borderTopLeftRadius = 16f;
                card.style.borderTopRightRadius = 16f;
                card.style.borderBottomLeftRadius = 16f;
                card.style.borderBottomRightRadius = 16f;
                card.style.borderLeftWidth = 2f;
                card.style.borderRightWidth = 2f;
                card.style.borderTopWidth = 2f;
                card.style.borderBottomWidth = 2f;
                card.style.borderLeftColor = new StyleColor(new Color(0.91f, 0.64f, 0.26f, 1f));
                card.style.borderRightColor = new StyleColor(new Color(0.91f, 0.64f, 0.26f, 1f));
                card.style.borderTopColor = new StyleColor(new Color(0.91f, 0.64f, 0.26f, 1f));
                card.style.borderBottomColor = new StyleColor(new Color(0.91f, 0.64f, 0.26f, 1f));
                card.style.paddingLeft = 24f;
                card.style.paddingRight = 24f;
                card.style.paddingTop = 20f;
                card.style.paddingBottom = 20f;
                _debugRoot.Add(card);

                var title = new Label("ПАНЕЛЬ ОТЛАДКИ СОСТОЯНИЙ");
                title.style.fontSize = 18f;
                title.style.color = new StyleColor(new Color(0.91f, 0.64f, 0.26f, 1f));
                title.style.unityFontStyleAndWeight = FontStyle.Bold;
                title.style.unityTextAlign = TextAnchor.MiddleCenter;
                title.style.marginBottom = 16f;
                card.Add(title);

                // Buttons
                CreateDebugButton(card, "Вызвать Диалог Иван (Хипстер)", () => {
                    CloseDebugPanel();
                    TriggerMockDialogue();
                });

                CreateDebugButton(card, "Включить Сахарный Кайф (30с)", () => {
                    CloseDebugPanel();
                    PlayerStatusEffects.Instance?.TriggerSugarRush(30f);
                });

                CreateDebugButton(card, "Включить Лимонную Паранойю", () => {
                    CloseDebugPanel();
                    if (PlayerStatusEffects.Instance != null)
                    {
                        var field = typeof(PlayerStatusEffects).GetField("_failedNegotiationsCount", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                        if (field != null) field.SetValue(PlayerStatusEffects.Instance, 3);
                    }
                });

                CreateDebugButton(card, "Включить Финансовую Депрессию", () => {
                    CloseDebugPanel();
                    SetPlayerBalanceReflection(10);
                });

                CreateDebugButton(card, "Включить Газовую Перегрузку (30с)", () => {
                    CloseDebugPanel();
                    PlayerStatusEffects.Instance?.TriggerCarbonationOverload(30f);
                });

                CreateDebugButton(card, "Включить Алкогольное Опьянение (40с)", () => {
                    CloseDebugPanel();
                    PlayerStatusEffects.Instance?.TriggerAlcoholIntoxication(40f);
                });

                CreateDebugButton(card, "Восстановить баланс ($1000)", () => {
                    CloseDebugPanel();
                    SetPlayerBalanceReflection(1000);
                    if (PlayerStatusEffects.Instance != null)
                    {
                        PlayerStatusEffects.Instance.ResetParanoia();
                    }
                });

                CreateDebugButton(card, "[ Закрыть ]", () => {
                    CloseDebugPanel();
                });

                _debugUIDoc.rootVisualElement.Add(_debugRoot);
            }

            _debugRoot.style.display = DisplayStyle.Flex;
        }

        private void SetPlayerBalanceReflection(int newBalance)
        {
            if (EconomyManager.Instance == null) return;

            var balanceField = typeof(EconomyManager).GetField("_balance", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (balanceField != null)
            {
                balanceField.SetValue(EconomyManager.Instance, newBalance);
            }

            var eventField = typeof(EconomyManager).GetField("OnBalanceChanged", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (eventField != null)
            {
                var handler = (System.Action<int>)eventField.GetValue(EconomyManager.Instance);
                handler?.Invoke(newBalance);
            }
        }

        private void CloseDebugPanel()
        {
            if (_debugRoot != null)
            {
                _debugRoot.style.display = DisplayStyle.None;
            }
            Time.timeScale = 1f;
            UnityEngine.Cursor.lockState = UnityEngine.CursorLockMode.Locked;
            UnityEngine.Cursor.visible = false;
        }

        private void CreateDebugButton(VisualElement container, string labelText, System.Action onClickAction)
        {
            var btn = new Button();
            btn.text = labelText;
            btn.style.height = 40f;
            btn.style.marginBottom = 10f;
            btn.style.backgroundColor = new StyleColor(new Color(0.16f, 0.16f, 0.18f, 1f));
            btn.style.color = new StyleColor(Color.white);
            btn.style.unityFontStyleAndWeight = FontStyle.Bold;
            btn.style.borderTopLeftRadius = 8f;
            btn.style.borderTopRightRadius = 8f;
            btn.style.borderBottomLeftRadius = 8f;
            btn.style.borderBottomRightRadius = 8f;
            btn.style.borderLeftWidth = 1f;
            btn.style.borderRightWidth = 1f;
            btn.style.borderTopWidth = 1f;
            btn.style.borderBottomWidth = 1f;
            btn.style.borderLeftColor = new StyleColor(new Color(0.3f, 0.3f, 0.33f, 1f));
            btn.style.borderRightColor = new StyleColor(new Color(0.3f, 0.3f, 0.33f, 1f));
            btn.style.borderTopColor = new StyleColor(new Color(0.3f, 0.3f, 0.33f, 1f));
            btn.style.borderBottomColor = new StyleColor(new Color(0.3f, 0.3f, 0.33f, 1f));
            
            btn.RegisterCallback<ClickEvent>(evt => onClickAction());
            btn.RegisterCallback<MouseEnterEvent>(evt => btn.style.backgroundColor = new StyleColor(new Color(0.25f, 0.25f, 0.28f, 1f)));
            btn.RegisterCallback<MouseLeaveEvent>(evt => btn.style.backgroundColor = new StyleColor(new Color(0.16f, 0.16f, 0.18f, 1f)));
            
            container.Add(btn);
        }

        private void TriggerMockDialogue()
        {
            var npcGo = new GameObject("Иван (Хипстер)");
            var agent = npcGo.AddComponent<UnityEngine.AI.NavMeshAgent>();
            agent.enabled = false;
            var npc = npcGo.AddComponent<NPCBuyer>();
            
            var archField = typeof(NPCBuyer).GetProperty("Archetype", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
            if (archField != null) archField.SetValue(npc, NPCArchetype.Hipsters);
            
            var setupMethod = typeof(NPCBuyer).GetMethod("SetupArchetypeStats", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (setupMethod != null) setupMethod.Invoke(npc, null);

            var bottleGo = new GameObject("Ягодный Взрыв");
            var col = bottleGo.AddComponent<BoxCollider>();
            col.enabled = false;
            var rb = bottleGo.AddComponent<Rigidbody>();
            rb.isKinematic = true;
            var bottle = bottleGo.AddComponent<ItemBase>();
            
            bottle.SetupDrink("Ягодный Взрыв", 45f, 80f, 4f, PackagingType.Glass, 1);
            bottle.Temperature = 6f; // Холод
            bottle.RetailPrice = 18;

            var counter = FindFirstObjectByType<Production.ServiceCounter>();
            if (counter != null)
            {
                var field = typeof(Production.ServiceCounter).GetField("placedDrink", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (field != null) field.SetValue(counter, bottle);
            }

            DialogueUI.Instance?.StartDialogue(npc, 50f, success => {
                Destroy(npcGo);
                Destroy(bottleGo);
            });
        }
    }
}
