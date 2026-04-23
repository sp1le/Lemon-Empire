using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;
using LemonEmpire.Core;

namespace LemonEmpire.UI
{

    public class TabletUI : MonoBehaviour
    {
        [Header("Shop Catalog")]
        [SerializeField] private List<ShopItemData> shopItems = new();

        [Header("Delivery")]
        [SerializeField] private Transform deliveryZone;
        [SerializeField] private float deliverySpacing = 1.2f;

        [Header("UI References (auto-created if empty)")]
        [SerializeField] private Canvas tabletCanvas;
        [SerializeField] private GameObject tabletPanel;

        public bool IsOpen => _isOpen;
        private bool _isOpen;
        private int _deliveryOffset;
        private int _currentTab = 1;
        private GameObject _contentArea;
        private List<TabletShopEntry> _entries = new();
        private Text _balanceText;
        private Text _homeStatsText;

        private Player.ThirdPersonCamera _camera;

        private void Start()
        {
            _camera = FindFirstObjectByType<Player.ThirdPersonCamera>();

            if (tabletCanvas == null)
                BuildUI();

            tabletPanel.SetActive(false);
        }

        public void Toggle()
        {
            if (_isOpen) Close();
            else Open();
        }

        private Volume _blurVolume;

        private void Open()
        {
            _isOpen = true;
            tabletPanel.SetActive(true);
            Time.timeScale = 0f;

            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            EnableBlur(true);
            RefreshBalance();
            RefreshTab(_currentTab);
        }

        private void RefreshBalance()
        {
            if (_balanceText != null && EconomyManager.Instance != null)
            {
                _balanceText.text = $"Баланс: ${EconomyManager.Instance.Balance}";
            }
        }

        public void Close()
        {
            _isOpen = false;
            tabletPanel.SetActive(false);
            Time.timeScale = 1f;

            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;

            EnableBlur(false);
        }

        private void EnableBlur(bool enable)
        {
            if (_blurVolume == null && enable)
            {
                var go = new GameObject("TabletBlurVolume");
                go.transform.SetParent(transform);
                _blurVolume = go.AddComponent<Volume>();
                _blurVolume.isGlobal = true;
                _blurVolume.priority = 100;

                var profile = ScriptableObject.CreateInstance<VolumeProfile>();
                _blurVolume.profile = profile;

                if (!profile.Has<DepthOfField>())
                {
                    var dof = profile.Add<DepthOfField>();
                    dof.active = true;
                    dof.mode.value = DepthOfFieldMode.Gaussian;
                    dof.gaussianStart.value = 0f;
                    dof.gaussianEnd.value = 0f;
                    dof.gaussianMaxRadius.value = 1.5f;
                }
            }

            if (_blurVolume != null)
                _blurVolume.gameObject.SetActive(enable);
        }

        private void RefreshUI()
        {
            int balance = EconomyManager.Instance != null ? EconomyManager.Instance.Balance : 0;

            if (_balanceText != null)
                _balanceText.text = $"Баланс: ${balance}";

            foreach (var entry in _entries)
            {
                entry.UpdateAffordability(balance);
            }
        }

        public void OrderItem(ShopItemData item, int quantity)
        {
            if (quantity <= 0) return;

            int totalCost = item.price * quantity;
            if (EconomyManager.Instance == null || !EconomyManager.Instance.TrySpend(totalCost))
                return;

            StartCoroutine(DeliverItems(item, quantity));
            RefreshUI();
        }

        private IEnumerator DeliverItems(ShopItemData item, int quantity)
        {
            yield return new WaitForSecondsRealtime(item.deliveryTime);

            if (deliveryZone == null)
            {
                Debug.LogWarning("TabletUI: No delivery zone assigned!");
                yield break;
            }

            for (int i = 0; i < quantity; i++)
            {
                Vector3 offset = new Vector3(
                    (_deliveryOffset % 3) * deliverySpacing,
                    0.5f,
                    (_deliveryOffset / 3) * deliverySpacing
                );

                Vector3 spawnPos = deliveryZone.position + offset;
                _deliveryOffset = (_deliveryOffset + 1) % 9;

                if (item.prefab != null)
                {
                    Instantiate(item.prefab, spawnPos, Quaternion.identity);
                }
                else
                {

                    CreateDefaultItem(item, spawnPos);
                }
            }
        }

        private void CreateDefaultItem(ShopItemData item, Vector3 position)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = item.name;
            go.transform.position = position;
            go.transform.localScale = new Vector3(0.6f, 0.6f, 0.6f);

            var renderer = go.GetComponent<Renderer>();
            if (renderer != null)
            {
                Material mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                switch (item.itemType)
                {
                    case ItemType.LemonCrate:
                        mat.color = new Color(1f, 0.85f, 0.1f);
                        break;
                    case ItemType.SugarBag:
                        mat.color = new Color(0.95f, 0.95f, 0.95f);
                        break;
                    case ItemType.BottlePack:
                        mat.color = new Color(0.6f, 0.85f, 0.9f);
                        break;
                    default:
                        mat.color = Color.gray;
                        break;
                }
                renderer.material = mat;
            }

            var rb = go.AddComponent<Rigidbody>();
            rb.mass = 2f;

            var itemBase = go.AddComponent<ItemBase>();
            itemBase.Setup(item.itemType, item.name, -1f, item.defaultAmount);
        }

        #region UI Building (runtime — no prefab needed)

        private void BuildUI()
        {

            var canvasGO = new GameObject("TabletCanvas");
            canvasGO.transform.SetParent(transform);
            tabletCanvas = canvasGO.AddComponent<Canvas>();
            tabletCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            tabletCanvas.sortingOrder = 100;
            canvasGO.AddComponent<UnityEngine.UI.CanvasScaler>().uiScaleMode =
                UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
            canvasGO.AddComponent<UnityEngine.UI.GraphicRaycaster>();

            if (FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
            {
                var es = new GameObject("EventSystem");
                es.AddComponent<UnityEngine.EventSystems.EventSystem>();
                es.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
            }

            tabletPanel = CreatePanel(canvasGO.transform, "TabletPanel",
                new Color(0f, 0f, 0f, 0.7f), Vector2.zero, Vector2.one);

            var frame = CreatePanel(tabletPanel.transform, "TabletFrame",
                new Color(0.15f, 0.15f, 0.18f, 1f),
                new Vector2(0.2f, 0.1f), new Vector2(0.8f, 0.9f));

            var topBar = CreatePanel(frame.transform, "TopBar", new Color(0.1f, 0.1f, 0.12f, 1f),
                new Vector2(0, 0.9f), new Vector2(1, 1));

            var btnHome = CreateButton(topBar.transform, "BtnHome", "СМЕНА",
                new Vector2(0.05f, 0.1f), new Vector2(0.3f, 0.9f), new Color(0.2f, 0.3f, 0.4f));
            var btnSup = CreateButton(topBar.transform, "BtnSup", "СЫРЬЁ",
                new Vector2(0.35f, 0.1f), new Vector2(0.6f, 0.9f), new Color(0.3f, 0.3f, 0.3f));
            var btnEq = CreateButton(topBar.transform, "BtnEq", "СТАНКИ",
                new Vector2(0.65f, 0.1f), new Vector2(0.95f, 0.9f), new Color(0.3f, 0.3f, 0.3f));

            btnHome.onClick.AddListener(() => RefreshTab(0));
            btnSup.onClick.AddListener(() => RefreshTab(1));
            btnEq.onClick.AddListener(() => RefreshTab(2));

            _balanceText = CreateText(frame.transform, "Balance", "Баланс: $---",
                new Vector2(0f, 0.85f), new Vector2(1f, 0.9f), 20,
                new Color(0.6f, 0.9f, 0.6f));

            _contentArea = CreatePanel(frame.transform, "ContentArea", Color.clear,
                new Vector2(0, 0.08f), new Vector2(1, 0.85f));

            CreateText(frame.transform, "CloseHint", "Tab — закрыть",
                new Vector2(0f, 0f), new Vector2(1f, 0.06f), 14,
                new Color(0.5f, 0.5f, 0.5f));
        }

        private void RefreshTab(int tabIndex)
        {
            _currentTab = tabIndex;
            _entries.Clear();

            foreach (Transform child in _contentArea.transform)
            {
                Destroy(child.gameObject);
            }

            if (tabIndex == 0)
            {
                BuildHomeTab();
            }
            else if (tabIndex == 1)
            {
                BuildShopTab(false);
            }
            else if (tabIndex == 2)
            {
                BuildShopTab(true);
            }
        }

        private void BuildHomeTab()
        {
            var eco = EconomyManager.Instance;
            var timeMan = TimeManager.Instance;

            string stats = "НЕТ ДАННЫХ";
            if (eco != null && timeMan != null)
            {
                int h = Mathf.FloorToInt(timeMan.CurrentTimeOfDay);
                int m = Mathf.FloorToInt((timeMan.CurrentTimeOfDay - h) * 60f);

                stats = $"<size=36>ДЕНЬ {timeMan.CurrentDay}</size>\n\n" +
                        $"Время: <color=#ffeecc>{h:00}:{m:00}</color>\n\n" +
                        $"Выручка: <color=#88ff88>+${eco.DailyRevenue}</color>\n" +
                        $"Расходы: <color=#ff8888>${eco.DailyExpenses}</color>\n" +
                        $"Прибыль: <b>${eco.DailyRevenue - eco.DailyExpenses}</b>\n\n" +
                        $"Продано: {eco.BottlesSold} шт";
            }

            _homeStatsText = CreateText(_contentArea.transform, "StatsText", stats,
                new Vector2(0.1f, 0.4f), new Vector2(0.9f, 1f), 24, Color.white);

            var endBtn = CreateButton(_contentArea.transform, "EndDayBtn", "ЗАВЕРШИТЬ СМЕНУ",
                new Vector2(0.2f, 0.1f), new Vector2(0.8f, 0.3f), new Color(0.8f, 0.3f, 0.2f));

            endBtn.GetComponentInChildren<UnityEngine.UI.Text>().fontSize = 24;

            endBtn.onClick.AddListener(() => {
                Close();
                if (TimeManager.Instance != null && TimeManager.Instance.IsShiftActive)
                {
                    TimeManager.Instance.EndShift();
                }
            });
        }

        private void BuildShopTab(bool equipmentOnly)
        {
            float yStart = 0.95f;
            float itemHeight = 0.15f;
            int count = 0;

            for (int i = 0; i < shopItems.Count; i++)
            {
                var item = shopItems[i];
                bool isEquip = item.itemType == ItemType.EquipmentBox;
                if (isEquip != equipmentOnly) continue;

                float yMax = yStart - count * (itemHeight + 0.03f);
                float yMin = yMax - itemHeight;

                var entry = CreateShopEntry(_contentArea.transform, item,
                    new Vector2(0.05f, yMin), new Vector2(0.95f, yMax));
                _entries.Add(entry);
                count++;
            }
        }

        private TabletShopEntry CreateShopEntry(Transform parent, ShopItemData item,
            Vector2 anchorMin, Vector2 anchorMax)
        {
            var bg = CreatePanel(parent, $"Entry_{item.name}",
                new Color(0.22f, 0.22f, 0.26f, 1f), anchorMin, anchorMax);

            CreateText(bg.transform, "Name", item.name,
                new Vector2(0.02f, 0.5f), new Vector2(0.4f, 1f), 18, Color.white);

            CreateText(bg.transform, "Price", $"${item.price}",
                new Vector2(0.02f, 0f), new Vector2(0.4f, 0.5f), 16,
                new Color(0.98f, 0.78f, 0.15f));

            var qtyText = CreateText(bg.transform, "Qty", "1",
                new Vector2(0.48f, 0.2f), new Vector2(0.62f, 0.8f), 22, Color.white);

            var minusBtn = CreateButton(bg.transform, "MinusBtn", "−",
                new Vector2(0.40f, 0.15f), new Vector2(0.48f, 0.85f),
                new Color(0.35f, 0.35f, 0.4f));

            var plusBtn = CreateButton(bg.transform, "PlusBtn", "+",
                new Vector2(0.62f, 0.15f), new Vector2(0.70f, 0.85f),
                new Color(0.35f, 0.35f, 0.4f));

            var orderBtn = CreateButton(bg.transform, "OrderBtn", "Заказать",
                new Vector2(0.72f, 0.15f), new Vector2(0.98f, 0.85f),
                new Color(0.2f, 0.55f, 0.2f));

            var entry = bg.AddComponent<TabletShopEntry>();
            entry.Setup(this, item, qtyText, minusBtn, plusBtn, orderBtn);
            return entry;
        }

        private GameObject CreatePanel(Transform parent, string name, Color color,
            Vector2 anchorMin, Vector2 anchorMax)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);

            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            var img = go.AddComponent<UnityEngine.UI.Image>();
            img.color = color;

            return go;
        }

        private UnityEngine.UI.Text CreateText(Transform parent, string name, string text,
            Vector2 anchorMin, Vector2 anchorMax, int fontSize, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);

            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.offsetMin = new Vector2(10, 0);
            rt.offsetMax = new Vector2(-10, 0);

            var txt = go.AddComponent<UnityEngine.UI.Text>();
            txt.text = text;
            txt.fontSize = fontSize;
            txt.color = color;
            txt.alignment = TextAnchor.MiddleCenter;
            txt.font = Font.CreateDynamicFontFromOSFont("Arial", fontSize);

            return txt;
        }

        private UnityEngine.UI.Button CreateButton(Transform parent, string name, string label,
            Vector2 anchorMin, Vector2 anchorMax, Color bgColor)
        {
            var go = CreatePanel(parent, name, bgColor, anchorMin, anchorMax);

            var btn = go.AddComponent<UnityEngine.UI.Button>();
            var colors = btn.colors;
            colors.highlightedColor = bgColor * 1.3f;
            colors.pressedColor = bgColor * 0.7f;
            btn.colors = colors;

            CreateText(go.transform, "Label", label,
                Vector2.zero, Vector2.one, 16, Color.white);

            return btn;
        }

        #endregion
    }
}
