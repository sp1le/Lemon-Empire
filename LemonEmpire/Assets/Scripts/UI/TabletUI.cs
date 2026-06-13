using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UIElements;
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

        [Header("UI References")]
        [SerializeField] private UIDocument uiDocument;

        public bool IsOpen => _isOpen;
        private bool _isOpen;
        private int _deliveryOffset;
        private int _currentTab = 0;

        private VisualElement _root;
        private VisualElement _contentArea;
        private Label _statusBarCenter;

        private Button _btnHome;
        private Button _btnInventory;
        private Button _btnBrewCraft;
        private Button _btnUpgrades;

        private Volume _blurVolume;

        // BrewCraft state (persists across tab switches)
        private string _brewName = "Ягодный Взрыв";
        private PackagingType _brewPackaging = PackagingType.Glass;
        private float _brewSugar = 50f;
        private float _brewCarbonation = 50f;
        private float _brewAlcohol = 0f;

        // ─────────── Color Palette (warm iPad aesthetic) ───────────
        private static readonly Color ColorBgTablet   = new Color(0.96f, 0.95f, 0.92f, 1f); // #F4F1EA
        private static readonly Color ColorBgSidebar  = new Color(0.92f, 0.90f, 0.86f, 1f); // #EAE6DC
        private static readonly Color ColorBgCard     = new Color(1f,    1f,    1f,    1f); // #FFFFFF
        private static readonly Color ColorStatusBar  = new Color(0.08f, 0.08f, 0.09f, 1f); // #141417
        private static readonly Color ColorTextDark   = new Color(0.31f, 0.21f, 0.16f, 1f); // #4E3629
        private static readonly Color ColorTextMuted  = new Color(0.58f, 0.55f, 0.51f, 1f); // #948C82
        private static readonly Color ColorGreen      = new Color(0.11f, 0.43f, 0.33f, 1f); // #1D6E54
        private static readonly Color ColorOrange     = new Color(0.91f, 0.64f, 0.26f, 1f); // #E8A243
        private static readonly Color ColorBrownBtn   = new Color(0.36f, 0.23f, 0.13f, 1f); // #5C3A21
        private static readonly Color ColorBorder     = new Color(0.85f, 0.82f, 0.77f, 1f); // #D9D2C5
        private static readonly Color ColorBorderDark = new Color(0.2f,  0.15f, 0.12f, 1f);

        // ─────────── Lifecycle ───────────

        private void Start()
        {
            // Deactivate any old uGUI canvas so it doesn't render on top
            var oldCanvas = transform.Find("TabletCanvas");
            if (oldCanvas != null)
                oldCanvas.gameObject.SetActive(false);

            // Destroy stale UIDocument from previous play sessions
            var existingDoc = GameObject.Find("TabletUIDocument");
            if (existingDoc != null)
                Destroy(existingDoc);
            uiDocument = null;

            BuildUI();

            if (_root != null)
                _root.style.display = DisplayStyle.None;
        }

        public void Toggle()
        {
            if (_isOpen) Close();
            else Open();
        }

        private void Open()
        {
            _isOpen = true;
            if (_root != null)
                _root.style.display = DisplayStyle.Flex;
            Time.timeScale = 0f;
            UnityEngine.Cursor.lockState = CursorLockMode.None;
            UnityEngine.Cursor.visible = true;
            EnableBlur(true);
            RefreshStatusBar();
            RefreshTab(_currentTab);
        }

        public void Close()
        {
            _isOpen = false;
            if (_root != null)
                _root.style.display = DisplayStyle.None;
            Time.timeScale = 1f;
            UnityEngine.Cursor.lockState = CursorLockMode.Locked;
            UnityEngine.Cursor.visible = false;
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

        // ─────────── Delivery Logic ───────────

        public void OrderItem(ShopItemData item, int quantity)
        {
            if (quantity <= 0) return;
            int totalCost = item.price * quantity;
            if (EconomyManager.Instance == null || !EconomyManager.Instance.TrySpend(totalCost))
                return;
            StartCoroutine(DeliverItems(item, quantity));
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
                    (_deliveryOffset / 3) * deliverySpacing);
                Vector3 spawnPos = deliveryZone.position + offset;
                _deliveryOffset = (_deliveryOffset + 1) % 9;

                if (item.prefab != null)
                {
                    var spawned = Instantiate(item.prefab, spawnPos, Quaternion.identity);
                    var ib = spawned.GetComponent<ItemBase>();
                    if (ib != null && ib.IsCrate)
                    {
                        ib.Amount = item.defaultAmount > 0 ? item.defaultAmount : 6;
                        if (item.itemType == ItemType.BottlePack)
                        {
                            ib.SetupDrink("Лимонад", 50f, 50f, 0f, PackagingType.Glass, ib.Amount);
                        }
                    }
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
                var mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                switch (item.itemType)
                {
                    case ItemType.LemonCrate:  mat.color = new Color(1f, 0.85f, 0.1f);  break;
                    case ItemType.SugarBag:    mat.color = new Color(0.95f, 0.95f, 0.95f); break;
                    case ItemType.BottlePack:  mat.color = new Color(0.6f, 0.85f, 0.9f);  break;
                    default:                   mat.color = Color.gray; break;
                }
                renderer.material = mat;
            }

            go.AddComponent<Rigidbody>().mass = 2f;
            var itemBase = go.AddComponent<ItemBase>();
            itemBase.Setup(item.itemType, item.name, -1f, item.defaultAmount);
        }

        private IEnumerator DeliverCustomDrinkCrate(
            string name, float sug, float carb, float alc, PackagingType pkg, int cost)
        {
            yield return new WaitForSecondsRealtime(5f);

            if (deliveryZone == null)
            {
                Debug.LogWarning("TabletUI: No delivery zone for BrewCraft!");
                yield break;
            }

            Vector3 offset = new Vector3(
                (_deliveryOffset % 3) * deliverySpacing,
                0.5f,
                (_deliveryOffset / 3) * deliverySpacing);
            Vector3 spawnPos = deliveryZone.position + offset;
            _deliveryOffset = (_deliveryOffset + 1) % 9;

            // Find BottlePack prefab
            GameObject prefabToUse = null;
            foreach (var si in shopItems)
            {
                if (si.itemType == ItemType.BottlePack && si.prefab != null)
                {
                    prefabToUse = si.prefab;
                    break;
                }
            }

            GameObject crateGO = prefabToUse != null
                ? Instantiate(prefabToUse, spawnPos, Quaternion.identity)
                : null;

            if (crateGO == null)
            {
                crateGO = GameObject.CreatePrimitive(PrimitiveType.Cube);
                crateGO.transform.position = spawnPos;
                crateGO.transform.localScale = new Vector3(0.6f, 0.4f, 0.6f);
                crateGO.AddComponent<Rigidbody>().mass = 3f;
            }

            var itemBase = crateGO.GetComponent<ItemBase>();
            if (itemBase == null) itemBase = crateGO.AddComponent<ItemBase>();
            itemBase.SetupDrink(name, sug, carb, alc, pkg, 6);
        }

        // ─────────── BrewCraft Cost ───────────

        public float CalculateBrewPrice()
        {
            float price = 20f + _brewSugar * 0.1f + _brewCarbonation * 0.1f + _brewAlcohol * 2f;
            if (_brewPackaging == PackagingType.Glass)
            {
                price *= 1.2f;
            }
            else if (_brewPackaging == PackagingType.Can)
            {
                price *= 1.1f;
            }
            return Mathf.Round(price);
        }

        private void RefreshUI()
        {
            RefreshStatusBar();
        }

        // ─────────── UI Toolkit Building ───────────

        private void BuildUI()
        {
            var uiDocGO = new GameObject("TabletUIDocument");
            uiDocGO.transform.SetParent(transform);
            uiDocument = uiDocGO.AddComponent<UIDocument>();

            // Create PanelSettings programmatically if not found in Resources
            var settings = Resources.Load<PanelSettings>("TabletPanelSettings");
            if (settings == null)
            {
                settings = ScriptableObject.CreateInstance<PanelSettings>();
                settings.scaleMode = PanelScaleMode.ScaleWithScreenSize;
                settings.referenceResolution = new Vector2Int(1920, 1080);
                settings.match = 0.5f;
            }
            uiDocument.panelSettings = settings;

            _root = uiDocument.rootVisualElement;
            _root.style.width  = Length.Percent(100);
            _root.style.height = Length.Percent(100);
            _root.style.flexDirection  = FlexDirection.Column;
            _root.style.alignItems     = Align.Center;
            _root.style.justifyContent = Justify.Center;
            // Dark semi-transparent backdrop
            _root.style.backgroundColor = new StyleColor(new Color(0f, 0f, 0f, 0.75f));

            // ── Bezel (matte iPad-style physical border) ──
            var bezel = new VisualElement();
            bezel.style.width  = Length.Percent(76);
            bezel.style.height = Length.Percent(92);
            bezel.style.backgroundColor = new StyleColor(new Color(0.06f, 0.06f, 0.07f, 1f));
            bezel.style.SetBorderRadius(40f);
            bezel.style.paddingLeft   = 16f;
            bezel.style.paddingRight  = 16f;
            bezel.style.paddingTop    = 16f;
            bezel.style.paddingBottom = 16f;
            bezel.style.SetBorderWidth(2f);
            bezel.style.SetBorderColor(Color.black);
            _root.Add(bezel);

            // ── Frame (screen inside bezel) ──
            var frame = new VisualElement();
            frame.style.width  = Length.Percent(100);
            frame.style.height = Length.Percent(100);
            frame.style.backgroundColor = new StyleColor(ColorStatusBar);
            frame.style.SetBorderRadius(32f);
            frame.style.overflow       = Overflow.Hidden;
            frame.style.flexDirection  = FlexDirection.Column;
            bezel.Add(frame);

            // ── Top Status Bar (LemonOS style) ──
            var topBar = new VisualElement();
            topBar.style.height          = Length.Percent(6);
            topBar.style.backgroundColor = new StyleColor(ColorStatusBar);
            topBar.style.flexDirection   = FlexDirection.Row;
            topBar.style.alignItems      = Align.Center;
            topBar.style.paddingLeft     = 24f;
            topBar.style.paddingRight    = 24f;
            topBar.style.borderTopLeftRadius  = 32f;
            topBar.style.borderTopRightRadius = 32f;
            frame.Add(topBar);

            var statusLeft = new Label("LemonOS v1.4");
            statusLeft.style.color = new StyleColor(new Color(0.88f, 0.88f, 0.92f, 1f));
            statusLeft.style.fontSize = 20f;
            statusLeft.style.unityFontStyleAndWeight = FontStyle.Bold;
            topBar.Add(statusLeft);

            _statusBarCenter = new Label("");
            _statusBarCenter.style.flexGrow  = 1f;
            _statusBarCenter.style.color     = new StyleColor(new Color(0.88f, 0.88f, 0.92f, 1f));
            _statusBarCenter.style.fontSize  = 18f;
            _statusBarCenter.style.unityTextAlign = TextAnchor.MiddleCenter;
            topBar.Add(_statusBarCenter);

            var statusRight = new Label("● Online");
            statusRight.style.color    = new StyleColor(new Color(0.35f, 0.85f, 0.35f, 1f));
            statusRight.style.fontSize = 18f;
            statusRight.style.unityFontStyleAndWeight = FontStyle.Bold;
            topBar.Add(statusRight);

            // ── Workspace (sidebar + content) ──
            var workspace = new VisualElement();
            workspace.style.flexGrow      = 1f;
            workspace.style.flexDirection = FlexDirection.Row;
            workspace.style.overflow      = Overflow.Hidden;
            frame.Add(workspace);

            // Left Sidebar
            var sidebar = new VisualElement();
            sidebar.style.width           = Length.Percent(13);
            sidebar.style.backgroundColor = new StyleColor(ColorBgSidebar);
            sidebar.style.flexDirection   = FlexDirection.Column;
            sidebar.style.alignItems      = Align.Center;
            sidebar.style.paddingTop      = 40f;
            sidebar.style.borderRightWidth = 2f;
            sidebar.style.borderRightColor = new StyleColor(ColorBorder);
            sidebar.style.borderBottomLeftRadius = 32f;
            workspace.Add(sidebar);

            _btnHome      = CreateSidebarIconButton(sidebar, "Cafe",    0);
            _btnInventory = CreateSidebarIconButton(sidebar, "Archive", 1);
            _btnBrewCraft = CreateSidebarIconButton(sidebar, "Flask",   2);
            _btnUpgrades  = CreateSidebarIconButton(sidebar, "Chart",   3);

            // Content Area
            _contentArea = new VisualElement();
            _contentArea.style.flexGrow          = 1f;
            _contentArea.style.backgroundColor   = new StyleColor(ColorBgTablet);
            _contentArea.style.paddingLeft        = 40f;
            _contentArea.style.paddingRight       = 40f;
            _contentArea.style.paddingTop         = 30f;
            _contentArea.style.paddingBottom      = 30f;
            _contentArea.style.borderBottomRightRadius = 32f;
            _contentArea.style.flexDirection      = FlexDirection.Column;
            workspace.Add(_contentArea);
        }

        // ─────────── Sidebar Icon Buttons ───────────

        private Button CreateSidebarIconButton(VisualElement parent, string iconType, int tabIndex)
        {
            var btn = new Button();
            btn.style.width  = 88f;
            btn.style.height = 88f;
            btn.style.SetBorderRadius(22f);
            btn.style.SetBorderWidth(2f);
            btn.style.SetBorderColor(ColorBorder);
            btn.style.backgroundColor = new StyleColor(ColorBgCard);
            btn.style.marginBottom    = 24f;
            btn.style.alignItems      = Align.Center;
            btn.style.justifyContent  = Justify.Center;
            btn.style.paddingLeft  = 0f;
            btn.style.paddingRight = 0f;
            btn.style.paddingTop   = 0f;
            btn.style.paddingBottom = 0f;

            var iconContainer = new VisualElement();
            iconContainer.name = "IconContainer";
            iconContainer.style.width  = Length.Percent(76);
            iconContainer.style.height = Length.Percent(76);
            iconContainer.style.alignItems      = Align.Center;
            iconContainer.style.justifyContent  = Justify.Center;
            btn.Add(iconContainer);

            var texture = Resources.Load<Texture2D>(iconType switch
            {
                "Cafe" => "UI/Icons/IconHome",
                "Archive" => "UI/Icons/IconInventory",
                "Flask" => "UI/Icons/IconBrewCraft",
                "Chart" => "UI/Icons/IconUpgrades",
                _ => ""
            });

            if (texture != null)
            {
                iconContainer.style.backgroundImage = new StyleBackground(texture);
            }
            else
            {
                BuildSidebarIcon(iconContainer, iconType);
            }

            btn.clicked += () => RefreshTab(tabIndex);
            parent.Add(btn);
            return btn;
        }

        private void BuildSidebarIcon(VisualElement parent, string iconType)
        {
            if (iconType == "Flask")
            {
                // Tilted chemical flask
                var tilt = new VisualElement();
                tilt.style.width  = Length.Percent(100);
                tilt.style.height = Length.Percent(100);
                tilt.style.rotate = new Rotate(Angle.Degrees(-30f));
                tilt.style.alignItems      = Align.Center;
                tilt.style.justifyContent  = Justify.Center;
                parent.Add(tilt);

                var body = new VisualElement();
                body.style.width  = Length.Percent(32);
                body.style.height = Length.Percent(70);
                body.style.backgroundColor = new StyleColor(new Color(0.9f, 0.95f, 1f, 0.8f));
                body.style.SetBorderRadius(12f);
                body.style.SetBorderWidth(2f);
                body.style.SetBorderColor(ColorTextDark);
                body.style.position = Position.Absolute;
                body.style.bottom   = 0f;
                tilt.Add(body);

                var liquid = new VisualElement();
                liquid.style.width  = Length.Percent(84);
                liquid.style.height = Length.Percent(55);
                liquid.style.backgroundColor = new StyleColor(new Color(0.35f, 0.85f, 0.35f, 1f));
                liquid.style.SetBorderRadius(8f);
                liquid.style.position = Position.Absolute;
                liquid.style.bottom   = 2f;
                liquid.style.left     = 1f;
                body.Add(liquid);

                var neck = new VisualElement();
                neck.style.width  = Length.Percent(45);
                neck.style.height = Length.Percent(15);
                neck.style.backgroundColor = new StyleColor(new Color(0.9f, 0.95f, 1f, 0.9f));
                neck.style.SetBorderRadius(2f);
                neck.style.SetBorderWidth(2f);
                neck.style.SetBorderColor(ColorTextDark);
                neck.style.position = Position.Absolute;
                neck.style.top = 10f;
                tilt.Add(neck);
            }
            else if (iconType == "Cafe")
            {
                // Cafe storefront icon
                var walls = new VisualElement();
                walls.style.width  = Length.Percent(75);
                walls.style.height = Length.Percent(55);
                walls.style.backgroundColor = new StyleColor(new Color(0.75f, 0.75f, 0.8f, 1f));
                walls.style.SetBorderRadius(8f);
                walls.style.SetBorderWidth(2f);
                walls.style.SetBorderColor(ColorTextDark);
                walls.style.position       = Position.Absolute;
                walls.style.bottom         = 4f;
                walls.style.flexDirection  = FlexDirection.Row;
                walls.style.justifyContent = Justify.Center;
                parent.Add(walls);

                var doorLeft = new VisualElement();
                doorLeft.style.width  = Length.Percent(20);
                doorLeft.style.height = Length.Percent(75);
                doorLeft.style.backgroundColor = new StyleColor(new Color(0.3f, 0.8f, 0.9f, 1f));
                doorLeft.style.SetBorderRadius(2f);
                doorLeft.style.position = Position.Absolute;
                doorLeft.style.bottom  = 0f;
                doorLeft.style.left    = Length.Percent(26);
                walls.Add(doorLeft);

                var doorRight = new VisualElement();
                doorRight.style.width  = Length.Percent(20);
                doorRight.style.height = Length.Percent(75);
                doorRight.style.backgroundColor = new StyleColor(new Color(0.3f, 0.8f, 0.9f, 1f));
                doorRight.style.SetBorderRadius(2f);
                doorRight.style.position = Position.Absolute;
                doorRight.style.bottom   = 0f;
                doorRight.style.right    = Length.Percent(26);
                walls.Add(doorRight);

                var roof = new VisualElement();
                roof.style.width  = Length.Percent(90);
                roof.style.height = Length.Percent(28);
                roof.style.backgroundColor = new StyleColor(new Color(0.95f, 0.3f, 0.45f, 1f));
                roof.style.SetBorderRadius(4f);
                roof.style.SetBorderWidth(2f);
                roof.style.SetBorderColor(ColorTextDark);
                roof.style.position = Position.Absolute;
                roof.style.bottom   = Length.Percent(48);
                parent.Add(roof);

                var sign = new VisualElement();
                sign.style.width  = Length.Percent(46);
                sign.style.height = Length.Percent(22);
                sign.style.backgroundColor = new StyleColor(new Color(0.18f, 0.15f, 0.22f, 1f));
                sign.style.SetBorderRadius(4f);
                sign.style.SetBorderWidth(2f);
                sign.style.SetBorderColor(ColorTextDark);
                sign.style.position       = Position.Absolute;
                sign.style.top            = 2f;
                sign.style.alignItems     = Align.Center;
                sign.style.justifyContent = Justify.Center;
                parent.Add(sign);

                var signLbl = new Label("24H");
                signLbl.style.fontSize = 8f;
                signLbl.style.color    = new StyleColor(new Color(0.95f, 0.4f, 0.6f, 1f));
                signLbl.style.unityFontStyleAndWeight = FontStyle.Bold;
                sign.Add(signLbl);
            }
            else if (iconType == "Chart")
            {
                var grid = new VisualElement();
                grid.style.width    = Length.Percent(75);
                grid.style.height   = Length.Percent(75);
                grid.style.position = Position.Absolute;
                parent.Add(grid);

                // Chart baseline
                var baseline = new VisualElement();
                baseline.style.width  = Length.Percent(90);
                baseline.style.height = 2f;
                baseline.style.backgroundColor = new StyleColor(ColorTextMuted);
                baseline.style.position = Position.Absolute;
                baseline.style.bottom   = Length.Percent(10);
                baseline.style.left     = Length.Percent(5);
                grid.Add(baseline);

                // Rising line 1
                var line1 = new VisualElement();
                line1.style.width  = Length.Percent(55);
                line1.style.height = 3f;
                line1.style.backgroundColor = new StyleColor(new Color(0.2f, 0.55f, 0.9f, 1f));
                line1.style.position = Position.Absolute;
                line1.style.bottom   = Length.Percent(35);
                line1.style.left     = Length.Percent(5);
                line1.style.rotate   = new Rotate(Angle.Degrees(-25f));
                grid.Add(line1);

                // Rising line 2
                var line2 = new VisualElement();
                line2.style.width  = Length.Percent(50);
                line2.style.height = 3f;
                line2.style.backgroundColor = new StyleColor(new Color(0.2f, 0.55f, 0.9f, 1f));
                line2.style.position = Position.Absolute;
                line2.style.top      = Length.Percent(35);
                line2.style.right    = Length.Percent(10);
                line2.style.rotate   = new Rotate(Angle.Degrees(35f));
                grid.Add(line2);

                // Arrow dot
                var arrow = new VisualElement();
                arrow.style.width  = 6f;
                arrow.style.height = 6f;
                arrow.style.SetBorderRadius(3f);
                arrow.style.backgroundColor = new StyleColor(new Color(0.2f, 0.55f, 0.9f, 1f));
                arrow.style.position = Position.Absolute;
                arrow.style.top      = Length.Percent(10);
                arrow.style.right    = Length.Percent(10);
                grid.Add(arrow);
            }
            else if (iconType == "Archive")
            {
                // Cardboard box icon
                var boxBase = new VisualElement();
                boxBase.style.width = Length.Percent(70);
                boxBase.style.height = Length.Percent(55);
                boxBase.style.backgroundColor = new StyleColor(new Color(0.78f, 0.62f, 0.44f, 1f)); // Brown cardboard
                boxBase.style.SetBorderRadius(4f);
                boxBase.style.SetBorderWidth(2f);
                boxBase.style.SetBorderColor(ColorTextDark);
                boxBase.style.position = Position.Absolute;
                boxBase.style.bottom = 4f;
                parent.Add(boxBase);

                var lid = new VisualElement();
                lid.style.width = Length.Percent(76);
                lid.style.height = Length.Percent(18);
                lid.style.backgroundColor = new StyleColor(new Color(0.70f, 0.54f, 0.36f, 1f)); // Slightly darker lid
                lid.style.SetBorderRadius(2f);
                lid.style.SetBorderWidth(2f);
                lid.style.SetBorderColor(ColorTextDark);
                lid.style.position = Position.Absolute;
                lid.style.bottom = 35f;
                lid.style.left = Length.Percent(-3);
                boxBase.Add(lid);

                var tape = new VisualElement();
                tape.style.width = Length.Percent(25);
                tape.style.height = Length.Percent(40);
                tape.style.backgroundColor = new StyleColor(new Color(0.85f, 0.8f, 0.65f, 1f)); // Cream tape
                tape.style.position = Position.Absolute;
                tape.style.bottom = 12f;
                tape.style.left = Length.Percent(37);
                tape.style.SetBorderWidth(1f);
                tape.style.SetBorderColor(ColorTextDark);
                boxBase.Add(tape);
            }
            else if (iconType == "Cart")
            {
                // Cart basket
                var basket = new VisualElement();
                basket.style.width = Length.Percent(55);
                basket.style.height = Length.Percent(40);
                basket.style.backgroundColor = new StyleColor(StyleKeyword.Null); // transparent body
                basket.style.SetBorderWidth(3f);
                basket.style.SetBorderColor(ColorTextDark);
                basket.style.SetBorderRadius(4f);
                basket.style.position = Position.Absolute;
                basket.style.bottom = 14f;
                basket.style.left = Length.Percent(25);
                parent.Add(basket);

                // Cart handle
                var handle = new VisualElement();
                handle.style.width = 3f;
                handle.style.height = Length.Percent(45);
                handle.style.backgroundColor = new StyleColor(ColorTextDark);
                handle.style.position = Position.Absolute;
                handle.style.bottom = 18f;
                handle.style.left = Length.Percent(18);
                parent.Add(handle);

                var handleGrip = new VisualElement();
                handleGrip.style.width = Length.Percent(12);
                handleGrip.style.height = 3f;
                handleGrip.style.backgroundColor = new StyleColor(ColorTextDark);
                handleGrip.style.position = Position.Absolute;
                handleGrip.style.bottom = 32f;
                handleGrip.style.left = Length.Percent(10);
                parent.Add(handleGrip);

                // Wheels
                var wheel1 = new VisualElement();
                wheel1.style.width = 10f;
                wheel1.style.height = 10f;
                wheel1.style.backgroundColor = new StyleColor(ColorTextDark);
                wheel1.style.SetBorderRadius(5f);
                wheel1.style.position = Position.Absolute;
                wheel1.style.bottom = 4f;
                wheel1.style.left = Length.Percent(30);
                parent.Add(wheel1);

                var wheel2 = new VisualElement();
                wheel2.style.width = 10f;
                wheel2.style.height = 10f;
                wheel2.style.backgroundColor = new StyleColor(ColorTextDark);
                wheel2.style.SetBorderRadius(5f);
                wheel2.style.position = Position.Absolute;
                wheel2.style.bottom = 4f;
                wheel2.style.left = Length.Percent(65);
                parent.Add(wheel2);
            }
        }

        // ─────────── Status Bar ───────────

        private void RefreshStatusBar()
        {
            float balance = EconomyManager.Instance != null ? EconomyManager.Instance.Balance : 0f;
            string dayName  = "Понедельник";
            string timeStr  = "08:00";

            if (TimeManager.Instance != null)
            {
                dayName = TimeManager.Instance.CurrentDay switch
                {
                    1 => "Понедельник",
                    2 => "Вторник",
                    3 => "Среда",
                    4 => "Четверг",
                    5 => "Пятница",
                    6 => "Суббота",
                    _ => "Воскресенье"
                };
                int h = Mathf.FloorToInt(TimeManager.Instance.CurrentTimeOfDay);
                int m = Mathf.FloorToInt((TimeManager.Instance.CurrentTimeOfDay - h) * 60f);
                timeStr = $"{h:00}:{m:00}";
            }

            if (_statusBarCenter != null)
                _statusBarCenter.text = $"{dayName}  ·  {timeStr}   |   Баланс: ${balance:F0}   |   Tab — закрыть";
        }

        // ─────────── Tab Routing ───────────

        private void RefreshTab(int tabIndex)
        {
            _currentTab = tabIndex;
            _contentArea.Clear();

            // Highlight active sidebar button
            UpdateSidebarButton(_btnHome,      0, tabIndex);
            UpdateSidebarButton(_btnInventory, 1, tabIndex);
            UpdateSidebarButton(_btnBrewCraft, 2, tabIndex);
            UpdateSidebarButton(_btnUpgrades,  3, tabIndex);

            // Tab header
            string headerTitle = tabIndex switch
            {
                0 => "LemonOS — Панель Управления",
                1 => "LemonOS — Склад и Запасы",
                2 => "BrewCraft — Конструктор Напитков",
                3 => "LemonOS — Улучшения Кафе",
                _ => "LemonOS"
            };

            var tabHeader = new Label(headerTitle);
            tabHeader.style.color = new StyleColor(ColorTextDark);
            tabHeader.style.fontSize = 32f;
            tabHeader.style.unityFontStyleAndWeight = FontStyle.Bold;
            tabHeader.style.marginBottom = 12f;
            _contentArea.Add(tabHeader);

            var separator = new VisualElement();
            separator.style.height          = 2f;
            separator.style.backgroundColor = new StyleColor(ColorBorder);
            separator.style.marginBottom    = 22f;
            _contentArea.Add(separator);

            var container = new VisualElement();
            container.style.flexGrow      = 1f;
            container.style.flexDirection = FlexDirection.Row;
            _contentArea.Add(container);

            if      (tabIndex == 0) BuildHomeTab(container);
            else if (tabIndex == 1) BuildInventoryTab(container);
            else if (tabIndex == 2) BuildBrewCraftTab(container);
            else if (tabIndex == 3) BuildUpgradesTab(container);

            RefreshStatusBar();
        }

        private void UpdateSidebarButton(Button btn, int btnTab, int activeTab)
        {
            if (btn == null) return;
            bool active = btnTab == activeTab;
            btn.style.backgroundColor = new StyleColor(active ? ColorOrange : ColorBgCard);
            btn.style.SetBorderColor(active ? ColorOrange : ColorBorder);
            var ic = btn.Q("IconContainer");
            if (ic != null) ic.style.opacity = active ? 1f : 0.55f;
        }

        // ─────────── Tab 0: Home (Dashboard) ───────────

        private void BuildHomeTab(VisualElement parent)
        {
            var eco     = EconomyManager.Instance;
            var timeMan = TimeManager.Instance;

            var card = new VisualElement();
            card.style.flexGrow       = 1f;
            card.style.backgroundColor = new StyleColor(ColorBgCard);
            card.style.SetBorderRadius(24f);
            card.style.SetBorderWidth(2f);
            card.style.SetBorderColor(ColorBorder);
            card.style.paddingLeft   = 40f;
            card.style.paddingRight  = 40f;
            card.style.paddingTop    = 40f;
            card.style.paddingBottom = 40f;
            card.style.flexDirection = FlexDirection.Row;
            parent.Add(card);

            // ── Left Column: day info + trend ──
            var leftCol = new VisualElement();
            leftCol.style.width             = Length.Percent(50);
            leftCol.style.flexDirection     = FlexDirection.Column;
            leftCol.style.justifyContent    = Justify.SpaceBetween;
            card.Add(leftCol);

            var dayLbl = new Label(timeMan != null ? $"ДЕНЬ {timeMan.CurrentDay}" : "ДЕНЬ 1");
            dayLbl.style.fontSize = 42f;
            dayLbl.style.color    = new StyleColor(ColorTextDark);
            dayLbl.style.unityFontStyleAndWeight = FontStyle.Bold;
            leftCol.Add(dayLbl);

            // Time
            string timeString = "08:00";
            if (timeMan != null)
            {
                int h = Mathf.FloorToInt(timeMan.CurrentTimeOfDay);
                int m = Mathf.FloorToInt((timeMan.CurrentTimeOfDay - h) * 60f);
                timeString = $"{h:00}:{m:00}";
            }
            var timeLbl = new Label($"Время: {timeString}");
            timeLbl.style.fontSize = 24f;
            timeLbl.style.color    = new StyleColor(ColorTextMuted);
            timeLbl.style.marginTop = 8f;
            leftCol.Add(timeLbl);

            // Trend badge
            string trendText  = "🍋  Обычный день";
            Color  trendColor = ColorGreen;
            if (GameEventManager.CurrentTrend != DailyTrend.Normal)
            {
                trendText = GameEventManager.CurrentTrend switch
                {
                    DailyTrend.HeatWave   => "☀️  Аномальная жара",
                    DailyTrend.PartyNight => "🥳  Ночная вечеринка",
                    DailyTrend.KidDay     => "🍭  Детский день",
                    DailyTrend.Marathon   => "🏃  Марафон",
                    _                     => "🍋  Обычный день",
                };
                trendColor = ColorOrange;
            }

            var trendBox = new VisualElement();
            trendBox.style.backgroundColor = new StyleColor(trendColor);
            trendBox.style.SetBorderRadius(12f);
            trendBox.style.paddingLeft   = 20f;
            trendBox.style.paddingRight  = 20f;
            trendBox.style.paddingTop    = 12f;
            trendBox.style.paddingBottom = 12f;
            trendBox.style.marginTop     = 20f;
            trendBox.style.alignSelf     = Align.FlexStart;
            leftCol.Add(trendBox);

            var trendLbl = new Label(trendText);
            trendLbl.style.fontSize = 18f;
            trendLbl.style.color    = new StyleColor(Color.white);
            trendLbl.style.unityFontStyleAndWeight = FontStyle.Bold;
            trendBox.Add(trendLbl);

            // ── Right Column: economy + end-shift button ──
            var rightCol = new VisualElement();
            rightCol.style.width          = Length.Percent(50);
            rightCol.style.paddingLeft    = 40f;
            rightCol.style.flexDirection  = FlexDirection.Column;
            rightCol.style.justifyContent = Justify.SpaceBetween;
            card.Add(rightCol);

            float revenue  = eco != null ? eco.DailyRevenue  : 0f;
            float expenses = eco != null ? eco.DailyExpenses  : 0f;
            float profit   = revenue - expenses;
            int   sold     = eco != null ? eco.BottlesSold   : 0;
            float balance  = eco != null ? eco.Balance       : 0f;

            string[] lines =
            {
                $"Баланс:   ${balance:F0}",
                $"Выручка:  +${revenue:F0}",
                $"Расходы:  -${expenses:F0}",
                $"Прибыль:  ${profit:F0}",
                $"Продано:  {sold} шт",
            };
            Color[] lineColors =
            {
                ColorTextDark,
                new Color(0.11f, 0.43f, 0.33f),
                new Color(0.75f, 0.2f, 0.2f),
                profit >= 0 ? ColorGreen : new Color(0.75f, 0.2f, 0.2f),
                ColorTextMuted,
            };

            var statsCol = new VisualElement();
            statsCol.style.flexDirection = FlexDirection.Column;
            rightCol.Add(statsCol);

            for (int i = 0; i < lines.Length; i++)
            {
                var row = new Label(lines[i]);
                row.style.fontSize  = 22f;
                row.style.color     = new StyleColor(lineColors[i]);
                row.style.marginBottom = 10f;
                if (i == 0 || i == 3) row.style.unityFontStyleAndWeight = FontStyle.Bold;
                statsCol.Add(row);
            }

            var endBtn = new Button();
            endBtn.text = "ЗАВЕРШИТЬ СМЕНУ";
            endBtn.style.height      = 65f;
            endBtn.style.marginTop   = 20f;
            endBtn.style.backgroundColor = new StyleColor(new Color(0.85f, 0.32f, 0.31f));
            endBtn.style.SetBorderRadius(16f);
            endBtn.style.color    = new StyleColor(Color.white);
            endBtn.style.fontSize = 22f;
            endBtn.style.unityFontStyleAndWeight = FontStyle.Bold;
            endBtn.style.SetBorderWidth(0f);
            endBtn.clicked += () =>
            {
                Close();
                if (TimeManager.Instance != null && TimeManager.Instance.IsShiftActive)
                    TimeManager.Instance.EndShift();
            };
            rightCol.Add(endBtn);
        }

        // ─────────── Tab 1: Shop (СЫРЬЁ) ───────────

        private void BuildShopTab(VisualElement parent)
        {
            var container = new VisualElement();
            container.style.flexGrow = 1f;
            container.style.flexDirection = FlexDirection.Column;
            parent.Add(container);

            var scroll = new ScrollView();
            scroll.style.flexGrow = 1f;
            container.Add(scroll);

            if (shopItems == null || shopItems.Count == 0)
            {
                var emptyLbl = new Label("Каталог пуст.");
                emptyLbl.style.fontSize = 20f;
                emptyLbl.style.color = new StyleColor(ColorTextMuted);
                emptyLbl.style.unityTextAlign = TextAnchor.MiddleCenter;
                emptyLbl.style.marginTop = 80f;
                scroll.Add(emptyLbl);
                return;
            }

            foreach (var item in shopItems)
            {
                if (item.itemType == ItemType.EquipmentBox) continue;

                var card = new VisualElement();
                card.style.height          = 95f;
                card.style.backgroundColor = new StyleColor(ColorBgCard);
                card.style.SetBorderRadius(16f);
                card.style.SetBorderWidth(2f);
                card.style.SetBorderColor(ColorBorder);
                card.style.paddingLeft  = 25f;
                card.style.paddingRight = 25f;
                card.style.flexDirection   = FlexDirection.Row;
                card.style.alignItems      = Align.Center;
                card.style.justifyContent  = Justify.SpaceBetween;
                card.style.marginBottom    = 14f;
                scroll.Add(card);

                // Left side: Info
                var info = new VisualElement();
                card.Add(info);

                var title = new Label(item.name);
                title.style.fontSize = 22f;
                title.style.color    = new StyleColor(ColorTextDark);
                title.style.unityFontStyleAndWeight = FontStyle.Bold;
                title.style.marginBottom = 4f;
                info.Add(title);

                var details = new Label($"Доставка: {item.deliveryTime:F0}с   ·   В упаковке: {item.defaultAmount} шт.");
                details.style.fontSize = 16f;
                details.style.color    = new StyleColor(ColorTextMuted);
                info.Add(details);

                // Right side: Controls & Buy
                var controls = new VisualElement();
                controls.style.flexDirection = FlexDirection.Row;
                controls.style.alignItems = Align.Center;
                card.Add(controls);

                int quantity = 1;

                var qtyContainer = new VisualElement();
                qtyContainer.style.flexDirection = FlexDirection.Row;
                qtyContainer.style.alignItems = Align.Center;
                qtyContainer.style.marginRight = 20f;
                controls.Add(qtyContainer);

                var minusBtn = new Button { text = "−" };
                minusBtn.style.width = 36f;
                minusBtn.style.height = 36f;
                minusBtn.style.fontSize = 20f;
                minusBtn.style.unityFontStyleAndWeight = FontStyle.Bold;
                minusBtn.style.SetBorderRadius(18f);
                minusBtn.style.SetBorderWidth(1f);
                minusBtn.style.SetBorderColor(ColorBorder);
                minusBtn.style.backgroundColor = new StyleColor(ColorBgSidebar);
                minusBtn.style.color = new StyleColor(ColorTextDark);
                qtyContainer.Add(minusBtn);

                var qtyLbl = new Label("1");
                qtyLbl.style.fontSize = 20f;
                qtyLbl.style.unityFontStyleAndWeight = FontStyle.Bold;
                qtyLbl.style.color = new StyleColor(ColorTextDark);
                qtyLbl.style.marginLeft = 12f;
                qtyLbl.style.marginRight = 12f;
                qtyLbl.style.width = 30f;
                qtyLbl.style.unityTextAlign = TextAnchor.MiddleCenter;
                qtyContainer.Add(qtyLbl);

                var plusBtn = new Button { text = "+" };
                plusBtn.style.width = 36f;
                plusBtn.style.height = 36f;
                plusBtn.style.fontSize = 20f;
                plusBtn.style.unityFontStyleAndWeight = FontStyle.Bold;
                plusBtn.style.SetBorderRadius(18f);
                plusBtn.style.SetBorderWidth(1f);
                plusBtn.style.SetBorderColor(ColorBorder);
                plusBtn.style.backgroundColor = new StyleColor(ColorBgSidebar);
                plusBtn.style.color = new StyleColor(ColorTextDark);
                qtyContainer.Add(plusBtn);

                var actionBtn = new Button();
                actionBtn.style.width  = 160f;
                actionBtn.style.height = 48f;
                actionBtn.style.SetBorderRadius(12f);
                actionBtn.style.fontSize = 18f;
                actionBtn.style.unityFontStyleAndWeight = FontStyle.Bold;
                actionBtn.style.SetBorderWidth(0f);
                actionBtn.style.color = new StyleColor(Color.white);
                controls.Add(actionBtn);

                // Local update action
                System.Action updateState = null;
                updateState = () =>
                {
                    qtyLbl.text = quantity.ToString();
                    int totalCost = item.price * quantity;
                    actionBtn.text = $"Заказать (${totalCost})";
                    
                    float balance = EconomyManager.Instance != null ? EconomyManager.Instance.Balance : 0f;
                    bool canAfford = balance >= totalCost;
                    
                    actionBtn.style.backgroundColor = new StyleColor(canAfford ? ColorGreen : new Color(0.75f, 0.2f, 0.2f));
                };

                minusBtn.clicked += () =>
                {
                    if (quantity > 1)
                    {
                        quantity--;
                        updateState();
                    }
                };

                plusBtn.clicked += () =>
                {
                    if (quantity < 10)
                    {
                        quantity++;
                        updateState();
                    }
                };

                actionBtn.clicked += () =>
                {
                    int totalCost = item.price * quantity;
                    float balance = EconomyManager.Instance != null ? EconomyManager.Instance.Balance : 0f;
                    if (balance >= totalCost)
                    {
                        OrderItem(item, quantity);
                        RefreshTab(_currentTab);
                    }
                };

                updateState();
            }
        }

        // ─────────── Tab 2: Inventory (СКЛАД) ───────────

        private void BuildInventoryTab(VisualElement parent)
        {
            var stands = FindObjectsByType<Production.DisplayStand>(FindObjectsSortMode.None);
            var items  = FindObjectsByType<ItemBase>(FindObjectsSortMode.None);

            var crates = new List<ItemBase>();
            foreach (var item in items)
            {
                if (item.IsCrate && !item.IsBeingCarried && item.Amount > 0)
                {
                    crates.Add(item);
                }
            }

            int totalCapacity = 0;
            int totalShelved  = 0;
            var drinkDetails  = new Dictionary<string, (int shelvedCount, int boxedCount, float price)>();

            // Shelves parsing
            foreach (var stand in stands)
            {
                totalCapacity += stand.MaxCapacity;
                totalShelved  += stand.ShelfItems.Count;
                foreach (var shelfItem in stand.ShelfItems)
                {
                    string name = string.IsNullOrEmpty(shelfItem.DrinkName) ? "Без названия" : shelfItem.DrinkName;
                    if (!drinkDetails.ContainsKey(name))
                    {
                        drinkDetails[name] = (0, 0, shelfItem.RetailPrice);
                    }
                    var (shCount, bxCount, pr) = drinkDetails[name];
                    drinkDetails[name] = (shCount + 1, bxCount, pr);
                }
            }

            // Crates parsing
            int totalBoxedAmount = 0;
            foreach (var crate in crates)
            {
                string name = string.IsNullOrEmpty(crate.DrinkName) ? "Без названия" : crate.DrinkName;
                totalBoxedAmount += crate.Amount;
                if (!drinkDetails.ContainsKey(name))
                {
                    drinkDetails[name] = (0, 0, crate.RetailPrice);
                }
                var (shCount, bxCount, pr) = drinkDetails[name];
                drinkDetails[name] = (shCount, bxCount + crate.Amount, pr);
            }

            // Tab container
            var container = new VisualElement();
            container.style.flexGrow = 1f;
            container.style.flexDirection = FlexDirection.Column;
            parent.Add(container);

            // Summary Card
            var summaryCard = new VisualElement();
            summaryCard.style.backgroundColor = new StyleColor(ColorBgCard);
            summaryCard.style.SetBorderRadius(20f);
            summaryCard.style.SetBorderWidth(2f);
            summaryCard.style.SetBorderColor(ColorBorder);
            summaryCard.style.paddingLeft = 24f;
            summaryCard.style.paddingRight = 24f;
            summaryCard.style.paddingTop = 20f;
            summaryCard.style.paddingBottom = 20f;
            summaryCard.style.marginBottom = 20f;
            summaryCard.style.flexDirection = FlexDirection.Column;
            container.Add(summaryCard);

            float fillPercent = totalCapacity > 0 ? (totalShelved * 100f / totalCapacity) : 0f;

            var statusRow = new VisualElement();
            statusRow.style.flexDirection = FlexDirection.Row;
            statusRow.style.justifyContent = Justify.SpaceBetween;
            statusRow.style.marginBottom = 10f;
            summaryCard.Add(statusRow);

            var summaryLbl = new Label($"Витрины: {totalShelved} / {totalCapacity} ({fillPercent:F0}%)");
            summaryLbl.style.fontSize = 22f;
            summaryLbl.style.unityFontStyleAndWeight = FontStyle.Bold;
            summaryLbl.style.color = new StyleColor(ColorTextDark);
            statusRow.Add(summaryLbl);

            var cratesSummaryLbl = new Label($"Коробки на складе: {crates.Count} шт ({totalBoxedAmount} бутылок)");
            cratesSummaryLbl.style.fontSize = 20f;
            cratesSummaryLbl.style.color = new StyleColor(ColorTextMuted);
            statusRow.Add(cratesSummaryLbl);

            // Progress bar
            var progressBarBg = new VisualElement();
            progressBarBg.style.height = 16f;
            progressBarBg.style.backgroundColor = new StyleColor(ColorBgSidebar);
            progressBarBg.style.SetBorderRadius(8f);
            progressBarBg.style.overflow = Overflow.Hidden;
            summaryCard.Add(progressBarBg);

            var progressBarFill = new VisualElement();
            progressBarFill.style.height = Length.Percent(100);
            progressBarFill.style.width = Length.Percent(Mathf.Clamp(fillPercent, 2f, 100f));
            progressBarFill.style.backgroundColor = new StyleColor(new Color(0.11f, 0.43f, 0.33f, 1f));
            progressBarFill.style.SetBorderRadius(8f);
            progressBarBg.Add(progressBarFill);

            // Scrollable drink catalog
            var scroll = new ScrollView();
            scroll.style.flexGrow = 1f;
            container.Add(scroll);

            if (drinkDetails.Count == 0)
            {
                var emptyLbl = new Label("Товар на складе отсутствует.\nИспользуйте вкладку РЕЦЕПТ, чтобы заказать напитки.");
                emptyLbl.style.fontSize = 20f;
                emptyLbl.style.color = new StyleColor(ColorTextMuted);
                emptyLbl.style.unityTextAlign = TextAnchor.MiddleCenter;
                emptyLbl.style.marginTop = 80f;
                scroll.Add(emptyLbl);
                return;
            }

            foreach (var detail in drinkDetails)
            {
                var rowCard = new VisualElement();
                rowCard.style.backgroundColor = new StyleColor(ColorBgCard);
                rowCard.style.SetBorderRadius(16f);
                rowCard.style.SetBorderWidth(2f);
                rowCard.style.SetBorderColor(ColorBorder);
                rowCard.style.paddingLeft = 24f;
                rowCard.style.paddingRight = 24f;
                rowCard.style.paddingTop = 18f;
                rowCard.style.paddingBottom = 18f;
                rowCard.style.marginBottom = 12f;
                rowCard.style.flexDirection = FlexDirection.Row;
                rowCard.style.justifyContent = Justify.SpaceBetween;
                rowCard.style.alignItems = Align.Center;
                scroll.Add(rowCard);

                // Left Info
                var leftInfo = new VisualElement();
                rowCard.Add(leftInfo);

                var dName = new Label(detail.Key);
                dName.style.fontSize = 22f;
                dName.style.unityFontStyleAndWeight = FontStyle.Bold;
                dName.style.color = new StyleColor(ColorTextDark);
                dName.style.marginBottom = 6f;
                leftInfo.Add(dName);

                var countsRow = new VisualElement();
                countsRow.style.flexDirection = FlexDirection.Row;
                leftInfo.Add(countsRow);

                var shelfLbl = new Label($"На полках: {detail.Value.shelvedCount} шт  |  ");
                shelfLbl.style.fontSize = 18f;
                shelfLbl.style.color = new StyleColor(new Color(0.11f, 0.43f, 0.33f, 1f));
                countsRow.Add(shelfLbl);

                var boxedLbl = new Label($"В коробках: {detail.Value.boxedCount} шт");
                boxedLbl.style.fontSize = 18f;
                boxedLbl.style.color = new StyleColor(new Color(0.2f, 0.45f, 0.75f, 1f));
                countsRow.Add(boxedLbl);

                // Right Info (Price)
                if (detail.Value.price > 0f)
                {
                    var priceLbl = new Label($"${detail.Value.price:F0}");
                    priceLbl.style.fontSize = 22f;
                    priceLbl.style.unityFontStyleAndWeight = FontStyle.Bold;
                    priceLbl.style.color = new StyleColor(ColorOrange);
                    rowCard.Add(priceLbl);
                }
            }
        }

        // ─────────── Tab 2: BrewCraft (РЕЦЕПТ) ───────────

        private void BuildBrewCraftTab(VisualElement parent)
        {
            string drinkName  = _brewName;
            float  sugarVal   = _brewSugar;
            float  carbVal    = _brewCarbonation;
            float  alcVal     = _brewAlcohol;
            PackagingType selectedPkg = _brewPackaging;

            // ── Left card: controls ──
            var leftCard = new VisualElement();
            leftCard.style.width           = Length.Percent(54);
            leftCard.style.backgroundColor = new StyleColor(ColorBgCard);
            leftCard.style.SetBorderRadius(24f);
            leftCard.style.SetBorderWidth(2f);
            leftCard.style.SetBorderColor(ColorBorder);
            leftCard.style.paddingLeft   = 30f;
            leftCard.style.paddingRight  = 30f;
            leftCard.style.paddingTop    = 25f;
            leftCard.style.paddingBottom = 25f;
            leftCard.style.flexDirection = FlexDirection.Column;
            parent.Add(leftCard);

            var brewTitle = new Label("Конструктор Рецепта");
            brewTitle.style.fontSize = 24f;
            brewTitle.style.color    = new StyleColor(ColorTextDark);
            brewTitle.style.unityFontStyleAndWeight = FontStyle.Bold;
            brewTitle.style.marginBottom = 16f;
            leftCard.Add(brewTitle);

            // Name field label
            var nameLbl = new Label("Название напитка");
            nameLbl.style.fontSize = 16f;
            nameLbl.style.color    = new StyleColor(ColorTextMuted);
            nameLbl.style.marginBottom = 6f;
            leftCard.Add(nameLbl);

            // Name input (TextField)
            var nameField = new TextField();
            nameField.value = drinkName;
            nameField.maxLength = 19;
            nameField.style.marginBottom = 20f;
            nameField.style.height       = 45f;
            nameField.style.fontSize     = 18f;
            leftCard.Add(nameField);

            // Packaging segment buttons
            var pkgLbl = new Label("Тара");
            pkgLbl.style.fontSize = 16f;
            pkgLbl.style.color    = new StyleColor(ColorTextMuted);
            pkgLbl.style.marginBottom = 6f;
            leftCard.Add(pkgLbl);

            var pkgContainer = new VisualElement();
            pkgContainer.style.flexDirection = FlexDirection.Row;
            pkgContainer.style.marginBottom  = 20f;
            leftCard.Add(pkgContainer);

            var btnPlastic = new Button(); btnPlastic.text = "Пластик";
            var btnCan     = new Button(); btnCan.text     = "Банка";
            var btnGlass   = new Button(); btnGlass.text   = "Стекло";

            foreach (var b in new[] { btnPlastic, btnCan, btnGlass })
            {
                b.style.flexGrow    = 1f;
                b.style.height      = 45f;
                b.style.marginRight = 8f;
                b.style.SetBorderRadius(10f);
                b.style.SetBorderWidth(1.5f);
                b.style.SetBorderColor(ColorBorder);
                b.style.fontSize = 16f;
                b.style.unityFontStyleAndWeight = FontStyle.Bold;
                pkgContainer.Add(b);
            }

            // Sliders Header
            var compLbl = new Label("Состав жидкости");
            compLbl.style.color    = new StyleColor(ColorTextDark);
            compLbl.style.fontSize = 18f;
            compLbl.style.unityFontStyleAndWeight = FontStyle.Bold;
            compLbl.style.marginBottom = 12f;
            leftCard.Add(compLbl);

            // Sugar
            var (sugarSlider, sugarValLbl) = AddSliderRow(leftCard, "Сахар:", sugarVal, 0f, 100f, "%");
            // Carb
            var (carbSlider, carbValLbl)   = AddSliderRow(leftCard, "Газ:", carbVal, 0f, 100f, "%");
            // Alcohol
            var (alcSlider, alcValLbl)     = AddSliderRow(leftCard, "Алкоголь:", alcVal, 0f, 100f, "%");

            // ── Right card: preview + order ──
            var rightCard = new VisualElement();
            rightCard.style.flexGrow       = 1f;
            rightCard.style.marginLeft     = 20f;
            rightCard.style.backgroundColor = new StyleColor(ColorBgCard);
            rightCard.style.SetBorderRadius(24f);
            rightCard.style.SetBorderWidth(2f);
            rightCard.style.SetBorderColor(ColorBorderDark);
            rightCard.style.paddingLeft   = 25f;
            rightCard.style.paddingRight  = 25f;
            rightCard.style.paddingTop    = 25f;
            rightCard.style.paddingBottom = 25f;
            rightCard.style.flexDirection = FlexDirection.Column;
            rightCard.style.alignItems    = Align.Center;
            parent.Add(rightCard);

            var previewTitle = new Label("Превью & Заказ");
            previewTitle.style.fontSize = 24f;
            previewTitle.style.color    = new StyleColor(ColorTextDark);
            previewTitle.style.unityFontStyleAndWeight = FontStyle.Bold;
            previewTitle.style.marginBottom = 16f;
            rightCard.Add(previewTitle);

            // Bottle visual container
            var visualContainer = new VisualElement();
            visualContainer.style.width = Length.Percent(100);
            visualContainer.style.height = 250f;
            visualContainer.style.alignItems = Align.Center;
            visualContainer.style.justifyContent = Justify.Center;
            visualContainer.style.marginBottom = 16f;
            rightCard.Add(visualContainer);

            // Cost labels
            var costTip = new Label("Себестоимость партии (6 шт)");
            costTip.style.fontSize = 16f;
            costTip.style.color    = new StyleColor(ColorTextMuted);
            costTip.style.marginBottom = 6f;
            rightCard.Add(costTip);

            var costValLbl = new Label("$0");
            costValLbl.style.fontSize = 32f;
            costValLbl.style.color    = new StyleColor(ColorGreen);
            costValLbl.style.unityFontStyleAndWeight = FontStyle.Bold;
            costValLbl.style.marginBottom = 16f;
            rightCard.Add(costValLbl);

            var orderBtn = new Button();
            orderBtn.text = "Заказать партию (6 шт)";
            orderBtn.style.width      = Length.Percent(100);
            orderBtn.style.height     = 60f;
            orderBtn.style.SetBorderRadius(16f);
            orderBtn.style.SetBorderWidth(0f);
            orderBtn.style.backgroundColor = new StyleColor(ColorGreen);
            orderBtn.style.color           = new StyleColor(Color.white);
            orderBtn.style.fontSize        = 20f;
            orderBtn.style.unityFontStyleAndWeight = FontStyle.Bold;
            rightCard.Add(orderBtn);

            // ── Wire up interactivity ──
            Action updatePkgButtons = () =>
            {
                StyleButton(btnPlastic, selectedPkg == PackagingType.Plastic);
                StyleButton(btnCan,     selectedPkg == PackagingType.Can);
                StyleButton(btnGlass,   selectedPkg == PackagingType.Glass);
            };

            Action updateCostAndUI = () =>
            {
                // Instant Visual Update
                UpdateVisualPreview(visualContainer, selectedPkg, drinkName, sugarVal, carbVal, alcVal);

                // Batch cost calculation based on uGUI formula
                float unitPrice = 20f + sugarVal * 0.1f + carbVal * 0.1f + alcVal * 2f;
                if (selectedPkg == PackagingType.Glass) unitPrice *= 1.2f;
                else if (selectedPkg == PackagingType.Can) unitPrice *= 1.1f;
                unitPrice = Mathf.Round(unitPrice);

                float batchCost = unitPrice * 6f;
                costValLbl.text = $"${batchCost:F0} (${unitPrice:F0}/шт)";

                float bal    = EconomyManager.Instance != null ? EconomyManager.Instance.Balance : 0f;
                bool canBuy  = bal >= batchCost;
                orderBtn.style.backgroundColor = new StyleColor(canBuy ? ColorGreen : new Color(0.75f, 0.2f, 0.2f));
                orderBtn.enabledSelf = canBuy;
            };

            nameField.RegisterValueChangedCallback(evt =>
            {
                drinkName = evt.newValue;
                if (drinkName != null && drinkName.Length > 19)
                {
                    drinkName = drinkName.Substring(0, 19);
                }
                _brewName = drinkName;
                updateCostAndUI();
            });

            btnPlastic.clicked += () => { selectedPkg = PackagingType.Plastic; _brewPackaging = selectedPkg; updatePkgButtons(); updateCostAndUI(); };
            btnCan.clicked     += () => { selectedPkg = PackagingType.Can;     _brewPackaging = selectedPkg; updatePkgButtons(); updateCostAndUI(); };
            btnGlass.clicked   += () => { selectedPkg = PackagingType.Glass;   _brewPackaging = selectedPkg; updatePkgButtons(); updateCostAndUI(); };

            sugarSlider.RegisterValueChangedCallback(evt => { sugarVal = evt.newValue; _brewSugar = sugarVal; sugarValLbl.text = $"{sugarVal:F0}%"; updateCostAndUI(); });
            carbSlider.RegisterValueChangedCallback(evt  => { carbVal  = evt.newValue; _brewCarbonation = carbVal; carbValLbl.text = $"{carbVal:F0}%"; updateCostAndUI(); });
            alcSlider.RegisterValueChangedCallback(evt   => { alcVal   = evt.newValue; _brewAlcohol = alcVal; alcValLbl.text = $"{alcVal:F0}%"; updateCostAndUI(); });

            orderBtn.clicked += () =>
            {
                float unitPrice = 20f + sugarVal * 0.1f + carbVal * 0.1f + alcVal * 2f;
                if (selectedPkg == PackagingType.Glass) unitPrice *= 1.2f;
                else if (selectedPkg == PackagingType.Can) unitPrice *= 1.1f;
                unitPrice = Mathf.Round(unitPrice);
                int cost = Mathf.RoundToInt(unitPrice * 6f);

                if (EconomyManager.Instance != null && EconomyManager.Instance.TrySpend(cost))
                {
                    StartCoroutine(DeliverCustomDrinkCrate(drinkName, sugarVal, carbVal, alcVal, selectedPkg, cost));
                    RefreshUI();
                    updateCostAndUI();
                }
            };

            updatePkgButtons();
            updateCostAndUI();
        }

        private void UpdateVisualPreview(VisualElement parent, PackagingType pkg, string drinkName, float sugarVal, float carbVal, float alcVal)
        {
            parent.Clear();

            var bottleGroup = new VisualElement();
            bottleGroup.style.width = Length.Percent(100);
            bottleGroup.style.height = 250f;
            bottleGroup.style.alignItems = Align.Center;
            bottleGroup.style.justifyContent = Justify.Center;
            parent.Add(bottleGroup);

            Color uguiLiquidColor = new Color(
                Mathf.Lerp(0.3f, 0.9f, sugarVal / 100f),
                Mathf.Lerp(0.7f, 0.9f, carbVal / 100f),
                Mathf.Lerp(0.9f, 0.5f, alcVal / 100f),
                0.85f
            );

            if (pkg == PackagingType.Can)
            {
                var canBody = new VisualElement();
                canBody.style.width = 120f;
                canBody.style.height = 200f;
                canBody.style.backgroundColor = new StyleColor(new Color(0.75f, 0.75f, 0.77f, 1f)); // Metallic silver
                canBody.style.SetBorderRadius(15f);
                canBody.style.SetBorderWidth(2f);
                canBody.style.SetBorderColor(ColorTextDark);
                canBody.style.overflow = Overflow.Hidden;
                bottleGroup.Add(canBody);

                // Top rim
                var topRim = new VisualElement();
                topRim.style.height = 8f;
                topRim.style.backgroundColor = new StyleColor(new Color(0.55f, 0.55f, 0.57f, 1f));
                topRim.style.borderBottomWidth = 1f;
                topRim.style.borderBottomColor = ColorTextDark;
                canBody.Add(topRim);

                // Shine line
                var shine = new VisualElement();
                shine.style.position = Position.Absolute;
                shine.style.left = 15f;
                shine.style.width = 12f;
                shine.style.top = 0f;
                shine.style.bottom = 0f;
                shine.style.backgroundColor = new StyleColor(new Color(1f, 1f, 1f, 0.15f));
                canBody.Add(shine);

                // Ribbon label
                var labelRibbon = new VisualElement();
                labelRibbon.style.height = 55f;
                labelRibbon.style.backgroundColor = new StyleColor(ColorOrange);
                labelRibbon.style.position = Position.Absolute;
                labelRibbon.style.left = 0f;
                labelRibbon.style.right = 0f;
                labelRibbon.style.top = 65f;
                labelRibbon.style.alignItems = Align.Center;
                labelRibbon.style.justifyContent = Justify.Center;
                canBody.Add(labelRibbon);

                var ribbonText = new Label(string.IsNullOrWhiteSpace(drinkName) ? "НАЗВАНИЕ НАПИТКА" : drinkName.ToUpper());
                ribbonText.style.color = new StyleColor(Color.white);
                ribbonText.style.fontSize = 14f;
                ribbonText.style.unityFontStyleAndWeight = FontStyle.Bold;
                ribbonText.style.unityTextAlign = TextAnchor.MiddleCenter;
                ribbonText.style.whiteSpace = WhiteSpace.Normal;
                ribbonText.style.paddingLeft = 4f;
                ribbonText.style.paddingRight = 4f;
                labelRibbon.Add(ribbonText);

                // Bottom rim
                var bottomRim = new VisualElement();
                bottomRim.style.position = Position.Absolute;
                bottomRim.style.bottom = 0f;
                bottomRim.style.left = 0f;
                bottomRim.style.right = 0f;
                bottomRim.style.height = 8f;
                bottomRim.style.backgroundColor = new StyleColor(new Color(0.55f, 0.55f, 0.57f, 1f));
                bottomRim.style.borderTopWidth = 1f;
                bottomRim.style.borderTopColor = ColorTextDark;
                canBody.Add(bottomRim);
            }
            else if (pkg == PackagingType.Plastic)
            {
                var bottleBody = new VisualElement();
                bottleBody.style.width = 110f;
                bottleBody.style.height = 220f;
                Color plasticColor = new Color(0.85f, 0.98f, 0.95f, 0.3f);
                bottleBody.style.backgroundColor = new StyleColor(plasticColor);

                bottleBody.style.borderTopLeftRadius = 35f;
                bottleBody.style.borderTopRightRadius = 35f;
                bottleBody.style.borderBottomLeftRadius = 12f;
                bottleBody.style.borderBottomRightRadius = 12f;
                bottleBody.style.SetBorderWidth(2f);
                bottleBody.style.SetBorderColor(ColorTextDark);
                bottleBody.style.overflow = Overflow.Hidden;
                bottleGroup.Add(bottleBody);

                var liquid = new VisualElement();
                liquid.style.position = Position.Absolute;
                liquid.style.bottom = 0f;
                liquid.style.left = 0f;
                liquid.style.right = 0f;
                liquid.style.height = Length.Percent(80);
                liquid.style.backgroundColor = new StyleColor(uguiLiquidColor);
                liquid.style.borderBottomLeftRadius = 10f;
                liquid.style.borderBottomRightRadius = 10f;
                bottleBody.Add(liquid);

                var neckRing = new VisualElement();
                neckRing.style.position = Position.Absolute;
                neckRing.style.top = 30f;
                neckRing.style.left = 28f;
                neckRing.style.right = 28f;
                neckRing.style.height = 4f;
                neckRing.style.backgroundColor = new StyleColor(ColorTextDark);
                bottleBody.Add(neckRing);

                var cap = new VisualElement();
                cap.style.position = Position.Absolute;
                cap.style.top = 0f;
                cap.style.left = 35f;
                cap.style.right = 35f;
                cap.style.height = 15f;
                cap.style.backgroundColor = new StyleColor(ColorOrange);
                cap.style.borderBottomWidth = 1f;
                cap.style.borderBottomColor = ColorTextDark;
                bottleBody.Add(cap);

                var labelRibbon = new VisualElement();
                labelRibbon.style.height = 55f;
                labelRibbon.style.backgroundColor = new StyleColor(ColorOrange);
                labelRibbon.style.position = Position.Absolute;
                labelRibbon.style.left = 0f;
                labelRibbon.style.right = 0f;
                labelRibbon.style.top = 90f;
                labelRibbon.style.alignItems = Align.Center;
                labelRibbon.style.justifyContent = Justify.Center;
                bottleBody.Add(labelRibbon);

                var ribbonText = new Label(string.IsNullOrWhiteSpace(drinkName) ? "НАЗВАНИЕ НАПИТКА" : drinkName.ToUpper());
                ribbonText.style.color = new StyleColor(Color.white);
                ribbonText.style.fontSize = 14f;
                ribbonText.style.unityFontStyleAndWeight = FontStyle.Bold;
                ribbonText.style.unityTextAlign = TextAnchor.MiddleCenter;
                ribbonText.style.whiteSpace = WhiteSpace.Normal;
                ribbonText.style.paddingLeft = 4f;
                ribbonText.style.paddingRight = 4f;
                labelRibbon.Add(ribbonText);
            }
            else // Glass
            {
                var glassGroup = new VisualElement();
                glassGroup.style.flexDirection = FlexDirection.Column;
                glassGroup.style.alignItems = Align.Center;
                glassGroup.style.height = 230f;
                glassGroup.style.width = 110f;
                bottleGroup.Add(glassGroup);

                Color glassColor = new Color(0.9f, 0.95f, 1f, 0.35f);

                var cap = new VisualElement();
                cap.style.width = 30f;
                cap.style.height = 15f;
                cap.style.backgroundColor = new StyleColor(ColorOrange);
                cap.style.borderTopLeftRadius = 4f;
                cap.style.borderTopRightRadius = 4f;
                cap.style.SetBorderWidth(2f);
                cap.style.SetBorderColor(ColorTextDark);
                glassGroup.Add(cap);

                var neck = new VisualElement();
                neck.style.width = 24f;
                neck.style.height = 20f;
                neck.style.backgroundColor = new StyleColor(Color.white);
                neck.style.borderLeftWidth = 2f;
                neck.style.borderRightWidth = 2f;
                neck.style.borderLeftColor = ColorTextDark;
                neck.style.borderRightColor = ColorTextDark;
                glassGroup.Add(neck);

                var body = new VisualElement();
                body.style.width = 110f;
                body.style.height = 155f;
                body.style.backgroundColor = new StyleColor(glassColor);
                body.style.borderTopLeftRadius = 40f;
                body.style.borderTopRightRadius = 40f;
                body.style.borderBottomLeftRadius = 12f;
                body.style.borderBottomRightRadius = 12f;
                body.style.SetBorderWidth(2f);
                body.style.SetBorderColor(ColorTextDark);
                body.style.overflow = Overflow.Hidden;
                body.style.marginTop = -2f; // overlap to look connected
                glassGroup.Add(body);

                var bodyLiquid = new VisualElement();
                bodyLiquid.style.position = Position.Absolute;
                bodyLiquid.style.bottom = 0f;
                bodyLiquid.style.left = 0f;
                bodyLiquid.style.right = 0f;
                bodyLiquid.style.height = Length.Percent(80);
                bodyLiquid.style.backgroundColor = new StyleColor(uguiLiquidColor);
                body.Add(bodyLiquid);

                var labelRibbon = new VisualElement();
                labelRibbon.style.height = 55f;
                labelRibbon.style.backgroundColor = new StyleColor(ColorOrange);
                labelRibbon.style.position = Position.Absolute;
                labelRibbon.style.left = 0f;
                labelRibbon.style.right = 0f;
                labelRibbon.style.top = 55f;
                labelRibbon.style.alignItems = Align.Center;
                labelRibbon.style.justifyContent = Justify.Center;
                body.Add(labelRibbon);

                var ribbonText = new Label(string.IsNullOrWhiteSpace(drinkName) ? "НАЗВАНИЕ НАПИТКА" : drinkName.ToUpper());
                ribbonText.style.color = new StyleColor(Color.white);
                ribbonText.style.fontSize = 14f;
                ribbonText.style.unityFontStyleAndWeight = FontStyle.Bold;
                ribbonText.style.unityTextAlign = TextAnchor.MiddleCenter;
                ribbonText.style.whiteSpace = WhiteSpace.Normal;
                ribbonText.style.paddingLeft = 4f;
                ribbonText.style.paddingRight = 4f;
                labelRibbon.Add(ribbonText);
            }
        }

        private (Slider slider, Label valueLbl) AddSliderRow(
            VisualElement parent, string label, float initial, float min, float max, string suffix)
        {
            var row = new VisualElement();
            row.style.flexDirection = FlexDirection.Row;
            row.style.alignItems    = Align.Center;
            row.style.marginBottom  = 14f;
            parent.Add(row);

            var lbl = new Label(label);
            lbl.style.width    = 130f;
            lbl.style.color    = new StyleColor(ColorTextDark);
            lbl.style.fontSize = 18f;
            lbl.style.unityFontStyleAndWeight = FontStyle.Bold;
            row.Add(lbl);

            var slider = new Slider(min, max);
            slider.value     = initial;
            slider.style.flexGrow = 1f;
            row.Add(slider);

            var valLbl = new Label($"{initial:F0}{suffix}");
            valLbl.style.width    = 55f;
            valLbl.style.color    = new StyleColor(ColorTextDark);
            valLbl.style.fontSize = 18f;
            valLbl.style.unityFontStyleAndWeight = FontStyle.Bold;
            valLbl.style.unityTextAlign = TextAnchor.MiddleRight;
            row.Add(valLbl);

            // Dynamically style UI Toolkit slider tracks and handle
            slider.RegisterCallback<GeometryChangedEvent>(evt => {
                var tracker = slider.Q("unity-tracker");
                if (tracker != null)
                {
                    tracker.style.height = 12f;
                    tracker.style.backgroundColor = new StyleColor(new Color(0.85f, 0.82f, 0.77f, 1f));
                    tracker.style.SetBorderWidth(0f);
                    tracker.style.SetBorderRadius(6f);
                    tracker.style.top = new Length(50, LengthUnit.Percent);
                    tracker.style.marginTop = -6f;
                }
                var dragger = slider.Q("unity-dragger");
                if (dragger != null)
                {
                    dragger.style.width = 26f;
                    dragger.style.height = 26f;
                    dragger.style.backgroundColor = new StyleColor(ColorOrange);
                    dragger.style.SetBorderWidth(0f);
                    dragger.style.SetBorderRadius(13f);
                    dragger.style.top = new Length(50, LengthUnit.Percent);
                    dragger.style.marginTop = -13f;
                }
                var fill = slider.Q("unity-fill-tracker");
                if (fill != null)
                {
                    fill.style.backgroundColor = new StyleColor(ColorOrange);
                    fill.style.height = 12f;
                    fill.style.SetBorderRadius(6f);
                    fill.style.top = new Length(50, LengthUnit.Percent);
                    fill.style.marginTop = -6f;
                }
            });

            return (slider, valLbl);
        }

        private void StyleButton(Button btn, bool active)
        {
            btn.style.backgroundColor = new StyleColor(active ? ColorBrownBtn : Color.clear);
            btn.style.color           = new StyleColor(active ? Color.white : ColorTextDark);
            btn.style.SetBorderColor(active ? ColorBrownBtn : ColorBorder);
            btn.style.unityFontStyleAndWeight = active ? FontStyle.Bold : FontStyle.Normal;
        }

        // ─────────── Tab 3: Upgrades (УЛУЧШЕНИЯ) ───────────

        private void BuildUpgradesTab(VisualElement parent)
        {
            var upgrades = new List<(string displayName, string managerName, int cost, Func<bool> isUnlocked)>
            {
                ("Расширение зала (Левое крыло)", "Разширение зала (Левое крыло)", 1000, () => UpgradeManager.IsLeftHallUnlocked),
                ("Музыкальный автомат (Jukebox)",  "Музыкальный автомат (Jukebox)",  300, () => UpgradeManager.HasJukebox),
                ("Торговый автомат (Snacks)",       "Торговый автомат (Snacks)",       150, () => UpgradeManager.HasSnackVending),
                ("Неоновая вывеска (Neon)",         "Неоновая вывеска (Neon)",         250, () => UpgradeManager.HasNeonSigns),
                ("Уличная мусорка для коробок",     "Уличная мусорка для коробок",     200, () => UpgradeManager.HasTrashBin),
                ("Двойной холодильник (Double)",    "Двойной холодильник",             600, () => UpgradeManager.HasDoubleCooler),
                ("Премиум-полки (Premium)",        "Премиум-полки",                   400, () => UpgradeManager.HasPremiumShelves),
            };

            var scroll = new ScrollView();
            scroll.style.flexGrow = 1f;
            parent.Add(scroll);

            foreach (var upg in upgrades)
                CreateUpgradeEntry(scroll, upg.displayName, upg.managerName, upg.cost, upg.isUnlocked);
        }

        private void CreateUpgradeEntry(VisualElement parent, string displayName, string managerName, int cost, Func<bool> isUnlocked)
        {
            var card = new VisualElement();
            card.style.height          = 95f;
            card.style.backgroundColor = new StyleColor(ColorBgCard);
            card.style.SetBorderRadius(16f);
            card.style.SetBorderWidth(2f);
            card.style.SetBorderColor(ColorBorder);
            card.style.paddingLeft  = 25f;
            card.style.paddingRight = 25f;
            card.style.flexDirection   = FlexDirection.Row;
            card.style.alignItems      = Align.Center;
            card.style.justifyContent  = Justify.SpaceBetween;
            card.style.marginBottom    = 14f;
            parent.Add(card);

            var info = new VisualElement();
            card.Add(info);

            var title = new Label(displayName);
            title.style.fontSize = 22f;
            title.style.color    = new StyleColor(ColorTextDark);
            title.style.unityFontStyleAndWeight = FontStyle.Bold;
            title.style.marginBottom = 4f;
            info.Add(title);

            bool   purchased  = isUnlocked();
            string priceText  = purchased ? "КУПЛЕНО" : $"${cost}";
            Color  priceColor = purchased ? ColorTextMuted : ColorGreen;

            var priceLbl = new Label(priceText);
            priceLbl.style.fontSize = 18f;
            priceLbl.style.color    = new StyleColor(priceColor);
            priceLbl.style.unityFontStyleAndWeight = purchased ? FontStyle.Normal : FontStyle.Bold;
            info.Add(priceLbl);

            string itemPath = managerName switch
            {
                "Музыкальный автомат (Jukebox)" => "Items/Jukebox",
                "Торговый автомат (Snacks)" => "Items/VendingMachine",
                "Двойной холодильник" => "Items/CoolerStand",
                "Премиум-полки" => "Items/DisplayStand",
                _ => null
            };

            bool isUniquePlaceable = managerName == "Торговый автомат (Snacks)";

            var actionBtn = new Button();
            actionBtn.style.width  = 130f;
            actionBtn.style.height = 48f;
            actionBtn.style.SetBorderRadius(12f);
            actionBtn.style.fontSize = 18f;
            actionBtn.style.unityFontStyleAndWeight = FontStyle.Bold;
            actionBtn.style.SetBorderWidth(0f);

            if (!purchased)
            {
                float balance  = EconomyManager.Instance != null ? EconomyManager.Instance.Balance : 0f;
                bool canAfford = balance >= cost;

                if (managerName == "Музыкальный автомат (Jukebox)" && !UpgradeManager.IsLeftHallUnlocked)
                {
                    actionBtn.text = "Закрыто";
                    actionBtn.style.backgroundColor = new StyleColor(ColorBorder);
                    actionBtn.style.color           = new StyleColor(ColorTextMuted);
                    actionBtn.enabledSelf = false;
                }
                else
                {
                    actionBtn.text = "Купить";
                    actionBtn.style.backgroundColor = new StyleColor(canAfford ? ColorGreen : new Color(0.75f, 0.2f, 0.2f));
                    actionBtn.style.color           = new StyleColor(Color.white);
                    actionBtn.enabledSelf = true;

                    string capManagerName = managerName;
                    int    capCost = cost;
                    actionBtn.clicked += () =>
                    {
                        if (UpgradeManager.TryPurchaseUpgrade(capManagerName, capCost))
                            RefreshTab(3); // Rebuild upgrades tab to reflect purchase
                    };
                }
            }
            else
            {
                if (isUniquePlaceable && itemPath != null)
                {
                    bool isPlaced = false;
                    var item = Resources.Load<BuildableItemSO>(itemPath);
                    if (item != null && BuildModeManager.Instance != null)
                    {
                        isPlaced = BuildModeManager.Instance.IsItemAlreadyBuilt(item);
                    }

                    if (isPlaced)
                    {
                        actionBtn.text = "✓ Установлено";
                        actionBtn.style.backgroundColor = new StyleColor(new Color(0.85f, 0.82f, 0.77f, 1f));
                        actionBtn.style.color           = new StyleColor(ColorTextMuted);
                        actionBtn.enabledSelf = false;
                    }
                    else
                    {
                        actionBtn.text = "Разместить";
                        actionBtn.style.backgroundColor = new StyleColor(ColorGreen);
                        actionBtn.style.color           = new StyleColor(Color.white);
                        actionBtn.enabledSelf = true;

                        actionBtn.clicked += () =>
                        {
                            Close();
                            if (BuildModeManager.Instance != null)
                            {
                                if (!BuildModeManager.Instance.IsBuildModeActive)
                                {
                                    BuildModeManager.Instance.ToggleBuildMode();
                                }
                                if (item != null)
                                {
                                    BuildModeManager.Instance.StartPlacement(item);
                                }
                            }
                        };
                    }
                }
                else
                {
                    actionBtn.text = "✓ Куплено";
                    actionBtn.style.backgroundColor = new StyleColor(new Color(0.85f, 0.82f, 0.77f, 1f));
                    actionBtn.style.color           = new StyleColor(ColorTextMuted);
                    actionBtn.enabledSelf = false;
                }
            }

            card.Add(actionBtn);
        }
    }
}
