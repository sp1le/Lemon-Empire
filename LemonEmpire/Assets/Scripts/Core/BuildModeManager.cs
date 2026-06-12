using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using LemonEmpire.Player;
using LemonEmpire.Production;
using LemonEmpire.UI;

namespace LemonEmpire.Core
{
    public class BuildModeManager : MonoBehaviour
    {
        public static BuildModeManager Instance { get; private set; }

        private BuildGrid _grid;
        private bool _isBuildModeActive = false;
        private BuildableItemSO _selectedItem;
        private GameObject _previewInstance;
        private int _rotationAngle = 0;

        private Material _previewValidMaterial;
        private Material _previewInvalidMaterial;

        // UI Toolkit elements
        private VisualElement _catalogPanel;
        private VisualElement _topInstructionsPanel;
        private VisualElement _itemContainer;
        private Label _errorLabel;

        // Visual Grid
        private GameObject _gridVisualObject;

        // Moving & Selling state
        private bool _isMovingItem = false;
        private Vector3 _origPosition;
        private Quaternion _origRotation;
        private List<Vector2Int> _origCells = new List<Vector2Int>();
        private BuildableItemSO _origItemSO;
        private float _origCost;

        private PlacedFurniture _hoveredFurniture;
        private float _currentPlacementCost = 0f;
        private float _pivotOffsetY = 0f;

        private bool _leftClickPressedThisFrame = false;
        private bool _rightClickPressedThisFrame = false;

        public bool IsBuildModeActive => _isBuildModeActive;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            _grid = new BuildGrid();

            // Destroy starting unique objects pre-placed in scene
            var oldJukebox = GameObject.Find("ShopObjects/Jukebox");
            if (oldJukebox != null) Destroy(oldJukebox);

            var oldVending = GameObject.Find("ShopObjects/VendingMachine");
            if (oldVending != null) Destroy(oldVending);
        }

        private void Start()
        {
            InitializeMaterials();
            CreateGridVisual();
            InitializeGridOccupancy();
            SetupStartingFurniture();
        }

        private void InitializeMaterials()
        {
            var shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader != null)
            {
                _previewValidMaterial = new Material(shader);
                _previewValidMaterial.SetFloat("_Surface", 1);
                _previewValidMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                _previewValidMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                _previewValidMaterial.SetInt("_ZWrite", 0);
                _previewValidMaterial.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
                _previewValidMaterial.color = new Color(0.1f, 1.0f, 0.1f, 0.45f); // Soft Green

                _previewInvalidMaterial = new Material(shader);
                _previewInvalidMaterial.SetFloat("_Surface", 1);
                _previewInvalidMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                _previewInvalidMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                _previewInvalidMaterial.SetInt("_ZWrite", 0);
                _previewInvalidMaterial.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
                _previewInvalidMaterial.color = new Color(1.0f, 0.1f, 0.1f, 0.45f); // Soft Red
            }
        }

        private void CreateGridVisual()
        {
            if (_gridVisualObject != null) return;

            _gridVisualObject = new GameObject("BuildGridVisual");
            var filter = _gridVisualObject.AddComponent<MeshFilter>();
            var renderer = _gridVisualObject.AddComponent<MeshRenderer>();

            Mesh mesh = new Mesh();
            mesh.name = "GridLines";

            List<Vector3> vertices = new List<Vector3>();
            List<int> indices = new List<int>();

            float targetY = -1.34f;
            float start = -12f;
            float end = 12f;
            float step = 0.5f;

            int index = 0;

            for (float x = start; x <= end; x += step)
            {
                vertices.Add(new Vector3(x, targetY, start));
                vertices.Add(new Vector3(x, targetY, end));
                indices.Add(index++);
                indices.Add(index++);
            }

            for (float z = start; z <= end; z += step)
            {
                vertices.Add(new Vector3(start, targetY, z));
                vertices.Add(new Vector3(end, targetY, z));
                indices.Add(index++);
                indices.Add(index++);
            }

            mesh.SetVertices(vertices);
            mesh.SetIndices(indices.ToArray(), MeshTopology.Lines, 0);
            filter.mesh = mesh;

            var shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader != null)
            {
                var mat = new Material(shader);
                mat.color = new Color(1f, 1f, 1f, 0.15f); // Soft white lines
                mat.SetFloat("_Surface", 1);
                mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                mat.SetInt("_ZWrite", 0);
                mat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
                renderer.material = mat;
            }

            _gridVisualObject.SetActive(false);
        }

        private void InitializeGridOccupancy()
        {
            _grid.Clear();
            var colliders = FindObjectsByType<Collider>(FindObjectsSortMode.None);
            foreach (var col in colliders)
            {
                if (col.isTrigger) continue;
                if (col.name.Contains("StoreFloor") || col.name.Contains("StoreCeiling")) continue;
                if (col.transform.root.name == "Player") continue;
                if (col.name.Contains("DebugConsolePedestal")) continue;
                if (!col.gameObject.activeInHierarchy) continue;

                // Ignore environment colliders that should not occupy building cells
                string name = col.name;
                if (name.Contains("Grass") || name.Contains("Road") || name.Contains("Sidewalk") || 
                    name.Contains("Parking") || name.Contains("Leaves") || name.Contains("Molding") || 
                    name.Contains("Ceiling") || name.Contains("Roof") || name.Contains("Crown"))
                {
                    continue;
                }

                // Check Y bounds to see if they overlap with the floor plane (approx -1.30f)
                // A valid placed furniture must have bounds that cover the floor level (between -1.40f and -1.20f)
                if (col.bounds.min.y > -1.20f || col.bounds.max.y < -1.40f)
                {
                    continue;
                }

                _grid.OccupyGridFromCollider(col);
            }
        }

        private void SetupStartingFurniture()
        {
            var displayStands = FindObjectsByType<DisplayStand>(FindObjectsSortMode.None);
            var displaySO = Resources.Load<BuildableItemSO>("Items/DisplayStand");
            foreach (var ds in displayStands)
            {
                var col = ds.GetComponent<Collider>();
                if (col != null)
                {
                    var placed = ds.gameObject.AddComponent<PlacedFurniture>();
                    var cells = GetCellsFromCollider(col);
                    placed.Setup(displaySO, cells, 150f);
                }
            }

            var coolerStands = FindObjectsByType<CoolerStand>(FindObjectsSortMode.None);
            var coolerSO = Resources.Load<BuildableItemSO>("Items/CoolerStand");
            foreach (var cs in coolerStands)
            {
                var col = cs.GetComponent<Collider>();
                if (col != null)
                {
                    var placed = cs.gameObject.AddComponent<PlacedFurniture>();
                    var cells = GetCellsFromCollider(col);
                    placed.Setup(coolerSO, cells, 600f);
                }
            }

            var tables = FindObjectsByType<CustomerTable>(FindObjectsSortMode.None);
            var tableSO = Resources.Load<BuildableItemSO>("Items/CustomerTableGroup");
            foreach (var t in tables)
            {
                Transform parentGroup = t.transform.parent;
                if (parentGroup != null && parentGroup.name.StartsWith("CustomerTableGroup"))
                {
                    var col = parentGroup.GetComponentInChildren<Collider>();
                    if (col != null && parentGroup.GetComponent<PlacedFurniture>() == null)
                    {
                        var placed = parentGroup.gameObject.AddComponent<PlacedFurniture>();
                        var cells = GetCellsFromCollider(col);
                        placed.Setup(tableSO, cells, 400f);
                    }
                }
            }
        }

        private List<Vector2Int> GetCellsFromCollider(Collider col)
        {
            List<Vector2Int> cells = new List<Vector2Int>();
            if (col == null) return cells;
            Bounds b = col.bounds;

            int minX = Mathf.FloorToInt((b.min.x + 12f) / 0.5f);
            int maxX = Mathf.CeilToInt((b.max.x + 12f) / 0.5f) - 1;
            int minZ = Mathf.FloorToInt((b.min.z + 12f) / 0.5f);
            int maxZ = Mathf.CeilToInt((b.max.z + 12f) / 0.5f) - 1;

            for (int x = minX; x <= maxX; x++)
            {
                for (int z = minZ; z <= maxZ; z++)
                {
                    var cell = new Vector2Int(x, z);
                    if (_grid.IsValidCell(cell))
                    {
                        cells.Add(cell);
                    }
                }
            }
            return cells;
        }

        public void OnLeftClick()
        {
            _leftClickPressedThisFrame = true;
        }

        public void OnRightClick()
        {
            _rightClickPressedThisFrame = true;
        }

        private void LateUpdate()
        {
            _leftClickPressedThisFrame = false;
            _rightClickPressedThisFrame = false;
        }

        private void Update()
        {
            // Toggle Build Mode on Q
            if (UnityEngine.InputSystem.Keyboard.current != null && UnityEngine.InputSystem.Keyboard.current.qKey.wasPressedThisFrame)
            {
                // Only allow toggling if Tablet and Dialogue are closed
                var tablet = FindFirstObjectByType<TabletUI>();
                var diag = FindFirstObjectByType<DialogueUI>();
                if ((tablet == null || !tablet.IsOpen) && (diag == null || !diag.IsActive))
                {
                    ToggleBuildMode();
                }
            }

            if (!_isBuildModeActive) return;

            // Detect mouse clicks directly from the New Input System
            if (UnityEngine.InputSystem.Mouse.current != null)
            {
                if (UnityEngine.InputSystem.Mouse.current.leftButton.wasPressedThisFrame)
                {
                    _leftClickPressedThisFrame = true;
                }
                if (UnityEngine.InputSystem.Mouse.current.rightButton.wasPressedThisFrame)
                {
                    _rightClickPressedThisFrame = true;
                }
            }

            // Prevent clicking UI Toolkit elements (like the catalog) from triggering 3D clicks
            if (UnityEngine.EventSystems.EventSystem.current != null && 
                UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject())
            {
                _leftClickPressedThisFrame = false;
                _rightClickPressedThisFrame = false;
            }

            // Handle Canceling / Reset selection
            if (UnityEngine.InputSystem.Keyboard.current != null && UnityEngine.InputSystem.Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                if (_selectedItem != null) CancelPlacement();
                else ToggleBuildMode();
                return;
            }

            if (_rightClickPressedThisFrame)
            {
                CancelPlacement();
                return;
            }

            if (_selectedItem != null)
            {
                HandlePlacementMode();
            }
            else
            {
                HandleInspectMode();
            }
        }

        public void ToggleBuildMode()
        {
            _isBuildModeActive = !_isBuildModeActive;

            if (_isBuildModeActive)
            {
                // Lock player movement/actions via Cursor
                UnityEngine.Cursor.lockState = CursorLockMode.None;
                UnityEngine.Cursor.visible = true;

                if (_gridVisualObject != null) _gridVisualObject.SetActive(true);

                BuildUI();
                _catalogPanel.style.display = DisplayStyle.Flex;
                _topInstructionsPanel.style.display = DisplayStyle.Flex;

                ShowCategory(BuildCategory.Shelves);
            }
            else
            {
                CancelPlacement();

                // Restore cursor state
                UnityEngine.Cursor.lockState = CursorLockMode.Locked;
                UnityEngine.Cursor.visible = false;

                if (PlayerController.Instance != null)
                {
                    PlayerController.Instance.SetMoveInput(Vector2.zero);
                }

                if (_gridVisualObject != null) _gridVisualObject.SetActive(false);

                if (_catalogPanel != null) _catalogPanel.style.display = DisplayStyle.None;
                if (_topInstructionsPanel != null) _topInstructionsPanel.style.display = DisplayStyle.None;
            }
        }

        public void StartPlacement(BuildableItemSO item)
        {
            if (item == null) return;
            
            _selectedItem = item;
            
            // If it is unique and require upgrade, the cost is already paid in tablet
            if (item.isUnique && (item.itemName == "Музыкальный Автомат" || item.itemName == "Торговый Автомат"))
            {
                _currentPlacementCost = 0f;
            }
            else
            {
                _currentPlacementCost = item.cost;
            }
            
            _rotationAngle = 0;

            CreatePreview(item);

            // Lock camera/movement, lock cursor for Placement Mode!
            UnityEngine.Cursor.lockState = CursorLockMode.Locked;
            UnityEngine.Cursor.visible = false;
        }

        private void CancelPlacement()
        {
            if (_isMovingItem)
            {
                // Respawn at original position
                GameObject realObj = Instantiate(_origItemSO.realPrefab, _origPosition, _origRotation);
                realObj.name = _origItemSO.itemName;
                var placed = realObj.AddComponent<PlacedFurniture>();
                placed.Setup(_origItemSO, _origCells, _origCost);

                _grid.OccupyCells(_origCells);
                SetUniqueItemState(_origItemSO, true);

                _isMovingItem = false;
            }

            CreatePreview(null);
            _selectedItem = null;

            if (_errorLabel != null)
            {
                _errorLabel.style.display = DisplayStyle.None;
            }

            // Return to Inspect Mode (free cursor, lock camera/movement)
            if (_isBuildModeActive)
            {
                UnityEngine.Cursor.lockState = CursorLockMode.None;
                UnityEngine.Cursor.visible = true;
            }
        }

        private void CreatePreview(BuildableItemSO item)
        {
            if (_previewInstance != null) Destroy(_previewInstance);

            if (item == null) return;

            _previewInstance = Instantiate(item.realPrefab);
            _previewInstance.name = "BuildPreview";

            // Strip functional components to avoid issues while placing
            foreach (var c in _previewInstance.GetComponentsInChildren<Component>())
            {
                if (c is Transform || c is Renderer || c is MeshFilter) continue;

                if (c is Collider col)
                {
                    col.isTrigger = true;
                }
                else if (c is MonoBehaviour mb)
                {
                    mb.enabled = false;
                }
                else
                {
                    if (c is Rigidbody rb) Destroy(rb);
                    if (c is AudioSource src) Destroy(src);
                    if (c is Animator anim) Destroy(anim);
                }
            }

            // Calculate height offset to align bottom with pivot
            float lowestY = 0f;
            var renderers = _previewInstance.GetComponentsInChildren<Renderer>();
            if (renderers.Length > 0)
            {
                Bounds b = renderers[0].bounds;
                for (int i = 1; i < renderers.Length; i++)
                {
                    b.Encapsulate(renderers[i].bounds);
                }
                lowestY = b.min.y - _previewInstance.transform.position.y;
            }
            _pivotOffsetY = -lowestY;

            SetPreviewMaterial(_previewValidMaterial);
        }

        private void SetPreviewMaterial(Material mat)
        {
            if (_previewInstance == null) return;
            foreach (var r in _previewInstance.GetComponentsInChildren<Renderer>())
            {
                var mats = new Material[r.sharedMaterials.Length];
                for (int i = 0; i < mats.Length; i++)
                {
                    mats[i] = mat;
                }
                r.sharedMaterials = mats;
            }
        }

        private void HandlePlacementMode()
        {
            if (Camera.main == null || _previewInstance == null || _selectedItem == null) return;

            // Rotate preview on R
            if (UnityEngine.InputSystem.Keyboard.current != null && UnityEngine.InputSystem.Keyboard.current.rKey.wasPressedThisFrame)
            {
                _rotationAngle = (_rotationAngle + 90) % 360;
            }

            // Raycast mathematical floor plane from screen center
            Ray ray = new Ray(Camera.main.transform.position, Camera.main.transform.forward);
            Plane floorPlane = new Plane(Vector3.up, new Vector3(0f, -1.30f, 0f));
            bool raycastHit = false;
            Vector3 worldPos = Vector3.zero;

            if (floorPlane.Raycast(ray, out float enter) && enter > 0f && enter < 20f)
            {
                worldPos = ray.GetPoint(enter);
                raycastHit = true;
            }

            if (raycastHit)
            {
                _previewInstance.SetActive(true);
                Vector2Int gridCell = _grid.WorldToGrid(worldPos);
                Vector3 snappedWorldPos = _grid.GridToWorld(gridCell);

                Vector3 previewPos = snappedWorldPos;
                previewPos.y += _pivotOffsetY;

                _previewInstance.transform.position = previewPos;
                _previewInstance.transform.rotation = Quaternion.Euler(0f, _rotationAngle, 0f);

                List<Vector2Int> cellsToOccupy = GetOccupiedCells(gridCell, _selectedItem.sizeInCells, _rotationAngle);

                bool isOutside = false;
                bool isLeftHallLocked = false;
                bool isOccupied = false;

                foreach (var cell in cellsToOccupy)
                {
                    if (!_grid.IsValidCell(cell))
                    {
                        isOutside = true;
                    }
                    else
                    {
                        Vector3 wPos = _grid.GridToWorld(cell);
                        if (!UpgradeManager.IsLeftHallUnlocked && wPos.x < 0f)
                        {
                            isLeftHallLocked = true;
                        }
                        else if (_grid.IsCellOccupied(cell))
                        {
                            isOccupied = true;
                        }
                    }
                }

                bool isUniqueLimitReached = _selectedItem.isUnique && IsItemAlreadyBuilt(_selectedItem);
                bool isInsufficientFunds = EconomyManager.Instance != null && EconomyManager.Instance.Balance < _currentPlacementCost;

                bool isValid = !isOutside && !isLeftHallLocked && !isOccupied && !isUniqueLimitReached && !isInsufficientFunds;

                SetPreviewMaterial(isValid ? _previewValidMaterial : _previewInvalidMaterial);

                if (!isValid)
                {
                    string errorMsg = "";
                    if (isOutside)
                    {
                        errorMsg = "Зона находится вне границ магазина";
                    }
                    else if (isLeftHallLocked)
                    {
                        errorMsg = "Левый зал еще не разблокирован";
                    }
                    else if (isOccupied)
                    {
                        errorMsg = "Зона заблокирована или занята другим объектом";
                    }
                    else if (isUniqueLimitReached)
                    {
                        errorMsg = "Достигнут лимит на этот предмет (1/1)";
                    }
                    else if (isInsufficientFunds)
                    {
                        errorMsg = "Недостаточно средств для покупки";
                    }

                    if (_errorLabel != null)
                    {
                        _errorLabel.text = errorMsg;
                        _errorLabel.style.display = DisplayStyle.Flex;
                    }
                }
                else
                {
                    if (_errorLabel != null)
                    {
                        _errorLabel.style.display = DisplayStyle.None;
                    }
                }

                // Try place object on LMB click
                if (_leftClickPressedThisFrame && isValid)
                {
                    PlaceObject(previewPos, cellsToOccupy);
                }
            }
            else
            {
                _previewInstance.SetActive(false);
                if (_errorLabel != null)
                {
                    _errorLabel.text = "Слишком далеко или не на полу магазина";
                    _errorLabel.style.display = DisplayStyle.Flex;
                }
            }
        }

        private void PlaceObject(Vector3 position, List<Vector2Int> cells)
        {
            // Spend money if not moving
            bool transactionOk = true;
            if (_currentPlacementCost > 0f)
            {
                transactionOk = EconomyManager.Instance != null && EconomyManager.Instance.TrySpend(_currentPlacementCost);
            }

            if (transactionOk)
            {
                GameObject realObj = Instantiate(_selectedItem.realPrefab, position, Quaternion.Euler(0f, _rotationAngle, 0f));
                realObj.name = _selectedItem.itemName;

                var placed = realObj.AddComponent<PlacedFurniture>();
                placed.Setup(_selectedItem, cells, _currentPlacementCost);

                _grid.OccupyCells(cells);
                SetUniqueItemState(_selectedItem, true);

                _isMovingItem = false;
                
                var placedSO = _selectedItem;
                CreatePreview(null);
                _selectedItem = null;

                // Re-evaluate limits UI
                ShowCategory(placedSO.category);

                // Return to Inspect Mode (free cursor, lock camera/movement)
                UnityEngine.Cursor.lockState = CursorLockMode.None;
                UnityEngine.Cursor.visible = true;
            }
        }

        private void HandleInspectMode()
        {
            if (Camera.main == null) return;

            Vector2 mousePos = Vector2.zero;
            if (UnityEngine.InputSystem.Mouse.current != null)
            {
                mousePos = UnityEngine.InputSystem.Mouse.current.position.ReadValue();
            }

            Ray ray = Camera.main.ScreenPointToRay(mousePos);
            if (Physics.Raycast(ray, out RaycastHit hit, 50f))
            {
                var furniture = hit.transform.root.GetComponent<PlacedFurniture>();
                if (furniture != null)
                {
                    _hoveredFurniture = furniture;
                    
                    // LMB Click: Move item
                    if (_leftClickPressedThisFrame)
                    {
                        PickUpFurniture(furniture);
                    }
                    // Delete Key Click: Sell item
                    else if (UnityEngine.InputSystem.Keyboard.current != null && UnityEngine.InputSystem.Keyboard.current.deleteKey.wasPressedThisFrame)
                    {
                        SellFurniture(furniture);
                    }
                }
                else
                {
                    _hoveredFurniture = null;
                }
            }
            else
            {
                _hoveredFurniture = null;
            }
        }

        private void PickUpFurniture(PlacedFurniture furniture)
        {
            _isMovingItem = true;
            _origPosition = furniture.transform.position;
            _origRotation = furniture.transform.rotation;
            _origCells = new List<Vector2Int>(furniture.occupiedCells);
            _origItemSO = furniture.itemSO;
            _origCost = furniture.purchaseCost;

            _grid.FreeCells(_origCells);
            SetUniqueItemState(_origItemSO, false);

            StartPlacement(_origItemSO);
            _currentPlacementCost = 0f; // Free to place back

            Destroy(furniture.gameObject);
        }

        private void SellFurniture(PlacedFurniture furniture)
        {
            float refund = 0f;
            if (furniture.itemSO.isUnique && (furniture.itemSO.itemName == "Музыкальный Автомат" || furniture.itemSO.itemName == "Торговый Автомат"))
            {
                refund = furniture.itemSO.cost * 0.7f;
            }
            else
            {
                refund = furniture.purchaseCost * 0.7f;
            }

            if (EconomyManager.Instance != null)
            {
                EconomyManager.Instance.Earn(refund);
            }

            _grid.FreeCells(furniture.occupiedCells);
            SetUniqueItemState(furniture.itemSO, false);

            Destroy(furniture.gameObject);
            _hoveredFurniture = null;
        }

        private List<Vector2Int> GetOccupiedCells(Vector2Int baseCell, Vector2Int size, int rotation)
        {
            List<Vector2Int> cells = new List<Vector2Int>();
            int w = size.x;
            int h = size.y;

            if (rotation == 90 || rotation == 270)
            {
                w = size.y;
                h = size.x;
            }

            for (int x = 0; x < w; x++)
            {
                for (int z = 0; z < h; z++)
                {
                    cells.Add(new Vector2Int(baseCell.x + x, baseCell.y + z));
                }
            }
            return cells;
        }

        private void SetUniqueItemState(BuildableItemSO item, bool state)
        {
            if (item.itemName == "Музыкальный Автомат")
                UpgradeManager.HasJukebox = state;
            else if (item.itemName == "Торговый Автомат")
                UpgradeManager.HasSnackVending = state;
        }

        // ── UI Building ──

        // ── UI Building ──

        private void BuildUI()
        {
            var doc = GameObject.Find("HUDUIDocument")?.GetComponent<UIDocument>();
            if (doc == null) return;

            var root = doc.rootVisualElement;
            if (_catalogPanel != null) return; // Already built

            _catalogPanel = new VisualElement();
            _catalogPanel.style.position = Position.Absolute;
            _catalogPanel.style.bottom = 30f;
            _catalogPanel.style.alignSelf = Align.Center;
            _catalogPanel.style.width = 880f;
            _catalogPanel.style.height = 240f;
            _catalogPanel.style.backgroundColor = new StyleColor(new Color(0.06f, 0.06f, 0.07f, 0.95f));
            _catalogPanel.style.SetBorderRadius(24f);
            _catalogPanel.style.SetBorderWidth(1.5f);
            _catalogPanel.style.SetBorderColor(new Color(0.91f, 0.64f, 0.26f, 0.85f)); // Gold border
            _catalogPanel.style.paddingLeft = 24f;
            _catalogPanel.style.paddingRight = 24f;
            _catalogPanel.style.paddingTop = 12f;
            _catalogPanel.style.paddingBottom = 12f;
            _catalogPanel.style.display = DisplayStyle.None;
            root.Add(_catalogPanel);

            // Header Row
            var headerRow = new VisualElement();
            headerRow.style.flexDirection = FlexDirection.Row;
            headerRow.style.justifyContent = Justify.SpaceBetween;
            headerRow.style.alignItems = Align.Center;
            _catalogPanel.Add(headerRow);

            var titleLbl = new Label("КАТАЛОГ СТРОИТЕЛЬСТВА & ОБОРУДОВАНИЯ");
            titleLbl.style.color = new StyleColor(new Color(0.91f, 0.64f, 0.26f, 1f));
            titleLbl.style.fontSize = 14f;
            titleLbl.style.unityFontStyleAndWeight = FontStyle.Bold;
            headerRow.Add(titleLbl);

            // Close Button
            var closeBtn = new Button();
            closeBtn.text = "✖";
            closeBtn.style.width = 24f;
            closeBtn.style.height = 24f;
            closeBtn.style.backgroundColor = new StyleColor(Color.clear);
            closeBtn.style.color = new StyleColor(new Color(0.6f, 0.6f, 0.6f, 1f));
            closeBtn.style.SetBorderWidth(0f);
            closeBtn.style.fontSize = 14f;
            closeBtn.style.unityFontStyleAndWeight = FontStyle.Bold;
            closeBtn.style.transitionProperty = new List<StylePropertyName> { 
                new StylePropertyName("color"), 
                new StylePropertyName("scale") 
            };
            closeBtn.style.transitionDuration = new List<TimeValue> { 
                new TimeValue(0.12f, TimeUnit.Second) 
            };
            closeBtn.RegisterCallback<MouseEnterEvent>(evt => {
                closeBtn.style.color = new StyleColor(new Color(0.95f, 0.35f, 0.35f, 1f));
                closeBtn.style.scale = new StyleScale(new Scale(new Vector3(1.15f, 1.15f, 1f)));
            });
            closeBtn.RegisterCallback<MouseLeaveEvent>(evt => {
                closeBtn.style.color = new StyleColor(new Color(0.6f, 0.6f, 0.6f, 1f));
                closeBtn.style.scale = new StyleScale(new Scale(Vector3.one));
            });
            closeBtn.clicked += ToggleBuildMode;
            headerRow.Add(closeBtn);

            // Divider line
            var divider = new VisualElement();
            divider.style.height = 1f;
            divider.style.backgroundColor = new StyleColor(new Color(0.91f, 0.64f, 0.26f, 0.25f));
            divider.style.marginTop = 4f;
            divider.style.marginBottom = 8f;
            _catalogPanel.Add(divider);

            // Scroll View for items
            var scrollView = new ScrollView(ScrollViewMode.Horizontal);
            scrollView.style.flexGrow = 1f;
            scrollView.horizontalScrollerVisibility = ScrollerVisibility.Auto;
            scrollView.verticalScrollerVisibility = ScrollerVisibility.Hidden;
            _catalogPanel.Add(scrollView);

            // Style the Scrollbar to look premium and sleek
            var scroller = scrollView.horizontalScroller;
            if (scroller != null)
            {
                scroller.style.height = 6f;
                scroller.style.marginTop = 8f;
                scroller.style.marginBottom = 2f;
                scroller.style.backgroundColor = Color.clear;

                var lowBtn = scroller.Q("unity-low-button");
                if (lowBtn != null) lowBtn.style.display = DisplayStyle.None;

                var highBtn = scroller.Q("unity-high-button");
                if (highBtn != null) highBtn.style.display = DisplayStyle.None;

                // Reset slider container style
                var slider = scroller.Q("unity-slider");
                if (slider != null)
                {
                    slider.style.height = 6f;
                    slider.style.marginTop = 0f;
                    slider.style.marginBottom = 0f;
                    slider.style.top = 0f;
                    slider.style.bottom = 0f;
                }

                // Reset track style
                var tracker = scroller.Q("unity-tracker");
                if (tracker != null)
                {
                    tracker.style.backgroundColor = new StyleColor(new Color(0.12f, 0.12f, 0.14f, 0.6f));
                    tracker.style.SetBorderColor(Color.clear);
                    tracker.style.SetBorderWidth(0f);
                    tracker.style.SetBorderRadius(3f);
                    tracker.style.height = 6f;
                    tracker.style.marginTop = 0f;
                    tracker.style.marginBottom = 0f;
                    tracker.style.top = 0f;
                    tracker.style.bottom = 0f;
                }

                // Reset drag handle style
                var dragger = scroller.Q("unity-dragger");
                if (dragger != null)
                {
                    dragger.style.backgroundColor = new StyleColor(new Color(0.91f, 0.64f, 0.26f, 0.5f)); // Soft translucent gold
                    dragger.style.SetBorderColor(Color.clear);
                    dragger.style.SetBorderWidth(0f);
                    dragger.style.SetBorderRadius(3f);
                    dragger.style.height = 6f;
                    dragger.style.marginTop = 0f;
                    dragger.style.marginBottom = 0f;
                    dragger.style.top = 0f;
                    dragger.style.bottom = 0f;
                    dragger.style.maxWidth = 100f;

                    dragger.RegisterCallback<MouseEnterEvent>(evt => {
                        dragger.style.backgroundColor = new StyleColor(new Color(0.91f, 0.64f, 0.26f, 0.9f));
                    });
                    dragger.RegisterCallback<MouseLeaveEvent>(evt => {
                        dragger.style.backgroundColor = new StyleColor(new Color(0.91f, 0.64f, 0.26f, 0.5f));
                    });
                }
            }

            _itemContainer = new VisualElement();
            _itemContainer.style.flexDirection = FlexDirection.Row;
            _itemContainer.style.alignItems = Align.Center;
            _itemContainer.style.paddingLeft = 24f;
            _itemContainer.style.paddingRight = 24f;
            _itemContainer.style.marginBottom = 6f;
            scrollView.Add(_itemContainer);

            // Instructions Panel (Top)
            _topInstructionsPanel = new VisualElement();
            _topInstructionsPanel.style.position = Position.Absolute;
            _topInstructionsPanel.style.top = 140f;
            _topInstructionsPanel.style.alignSelf = Align.Center;
            _topInstructionsPanel.style.backgroundColor = new StyleColor(new Color(0.08f, 0.08f, 0.09f, 0.88f));
            _topInstructionsPanel.style.SetBorderRadius(10f);
            _topInstructionsPanel.style.SetBorderWidth(1f);
            _topInstructionsPanel.style.SetBorderColor(new Color(0.91f, 0.64f, 0.26f, 0.5f));
            _topInstructionsPanel.style.paddingLeft = 20f;
            _topInstructionsPanel.style.paddingRight = 20f;
            _topInstructionsPanel.style.paddingTop = 8f;
            _topInstructionsPanel.style.paddingBottom = 8f;
            _topInstructionsPanel.style.display = DisplayStyle.None;
            root.Add(_topInstructionsPanel);

            var instrLbl = new Label("[Q/Esc] Закрыть | [R] Поворот | [LMB] Установить | [RMB] Отмена\nНаведите на мебель: [LMB] Переместить | [Delete] Продать (70% стоимости)");
            instrLbl.style.color = new StyleColor(Color.white);
            instrLbl.style.fontSize = 13f;
            instrLbl.style.unityFontStyleAndWeight = FontStyle.Bold;
            instrLbl.style.unityTextAlign = TextAnchor.MiddleCenter;
            _topInstructionsPanel.Add(instrLbl);

            // Error Label (premium dark pill with red text)
            _errorLabel = new Label();
            _errorLabel.style.position = Position.Absolute;
            _errorLabel.style.bottom = 280f; // Adjusted for taller panel
            _errorLabel.style.alignSelf = Align.Center;
            _errorLabel.style.backgroundColor = new StyleColor(new Color(0.08f, 0.08f, 0.09f, 0.90f));
            _errorLabel.style.color = new StyleColor(new Color(0.95f, 0.26f, 0.26f, 1f));
            _errorLabel.style.SetBorderRadius(12f);
            _errorLabel.style.SetBorderWidth(1.5f);
            _errorLabel.style.SetBorderColor(new Color(0.95f, 0.26f, 0.26f, 0.6f));
            _errorLabel.style.paddingLeft = 20f;
            _errorLabel.style.paddingRight = 20f;
            _errorLabel.style.paddingTop = 8f;
            _errorLabel.style.paddingBottom = 8f;
            _errorLabel.style.fontSize = 14f;
            _errorLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            _errorLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
            _errorLabel.style.display = DisplayStyle.None;
            root.Add(_errorLabel);
        }

        private void ShowCategory(BuildCategory cat)
        {
            PopulateCatalog();
        }

        private void PopulateCatalog()
        {
            if (_itemContainer == null) return;
            _itemContainer.Clear();

            var items = Resources.LoadAll<BuildableItemSO>("Items");
            System.Array.Sort(items, (a, b) => a.itemName.CompareTo(b.itemName));

            foreach (var item in items)
            {
                var card = new VisualElement();
                card.style.width = 175f;
                card.style.height = 136f;
                card.style.backgroundColor = new StyleColor(new Color(0.09f, 0.09f, 0.11f, 0.98f));
                card.style.SetBorderRadius(14f);
                card.style.SetBorderWidth(1.5f);
                card.style.SetBorderColor(new Color(0.22f, 0.22f, 0.25f, 1f));
                card.style.paddingLeft = 10f;
                card.style.paddingRight = 10f;
                card.style.paddingTop = 8f;
                card.style.paddingBottom = 8f;
                card.style.marginRight = 12f;
                card.style.alignItems = Align.Stretch;
                card.style.justifyContent = Justify.SpaceBetween;

                // Micro-animations for hover
                card.style.transitionProperty = new List<StylePropertyName> { 
                    new StylePropertyName("scale"), 
                    new StylePropertyName("border-color"),
                    new StylePropertyName("background-color")
                };
                card.style.transitionDuration = new List<TimeValue> { 
                    new TimeValue(0.12f, TimeUnit.Second) 
                };

                card.RegisterCallback<MouseEnterEvent>(evt => {
                    card.style.scale = new StyleScale(new Scale(new Vector3(1.03f, 1.03f, 1f)));
                    card.style.SetBorderColor(new Color(0.91f, 0.64f, 0.26f, 1f)); // Glowing gold
                    card.style.backgroundColor = new StyleColor(new Color(0.14f, 0.14f, 0.17f, 0.98f));
                });
                card.RegisterCallback<MouseLeaveEvent>(evt => {
                    card.style.scale = new StyleScale(new Scale(Vector3.one));
                    card.style.SetBorderColor(new Color(0.22f, 0.22f, 0.25f, 1f));
                    card.style.backgroundColor = new StyleColor(new Color(0.09f, 0.09f, 0.11f, 0.98f));
                });

                _itemContainer.Add(card);

                // Category & Size Row
                var headerRow = new VisualElement();
                headerRow.style.flexDirection = FlexDirection.Row;
                headerRow.style.justifyContent = Justify.SpaceBetween;
                headerRow.style.alignItems = Align.Center;
                card.Add(headerRow);

                // Category Badge
                var badge = new Label(GetCategoryText(item.category));
                badge.style.fontSize = 9f;
                badge.style.unityFontStyleAndWeight = FontStyle.Bold;
                badge.style.unityTextAlign = TextAnchor.MiddleCenter;
                badge.style.paddingLeft = 6f;
                badge.style.paddingRight = 6f;
                badge.style.paddingTop = 2f;
                badge.style.paddingBottom = 2f;
                badge.style.SetBorderRadius(4f);
                SetBadgeStyle(badge, item.category);
                headerRow.Add(badge);

                // Size info
                var sizeLbl = new Label($"{item.sizeInCells.x}x{item.sizeInCells.y}");
                sizeLbl.style.fontSize = 10f;
                sizeLbl.style.color = new StyleColor(new Color(0.6f, 0.6f, 0.6f, 1f));
                sizeLbl.style.unityFontStyleAndWeight = FontStyle.Bold;
                headerRow.Add(sizeLbl);

                // Middle Group (Name + Specs) for consistent vertical alignment
                var midGroup = new VisualElement();
                midGroup.style.flexGrow = 1f;
                midGroup.style.justifyContent = Justify.Center;
                midGroup.style.marginTop = 4f;
                midGroup.style.marginBottom = 4f;
                card.Add(midGroup);

                // Item Name
                var nameLbl = new Label(item.itemName);
                nameLbl.style.fontSize = 11.5f;
                nameLbl.style.color = new StyleColor(Color.white);
                nameLbl.style.unityFontStyleAndWeight = FontStyle.Bold;
                nameLbl.style.unityTextAlign = TextAnchor.MiddleLeft;
                nameLbl.style.whiteSpace = WhiteSpace.Normal;
                nameLbl.style.marginBottom = 2f;
                midGroup.Add(nameLbl);

                // Specifications
                string specText = GetItemSpecificationText(item);
                var specLbl = new Label(specText);
                specLbl.style.fontSize = 9f;
                specLbl.style.color = new StyleColor(new Color(0.75f, 0.75f, 0.78f, 1f));
                specLbl.style.unityTextAlign = TextAnchor.MiddleLeft;
                midGroup.Add(specLbl);

                // Unlock & Place checks
                bool isUnlocked = IsItemUpgradePurchased(item);
                bool isPlaced = item.isUnique && IsItemAlreadyBuilt(item);

                var buyBtn = new Button();
                buyBtn.style.width = Length.Percent(100);
                buyBtn.style.height = 28f;
                buyBtn.style.SetBorderRadius(8f);
                buyBtn.style.SetBorderWidth(1.5f);
                buyBtn.style.unityFontStyleAndWeight = FontStyle.Bold;
                buyBtn.style.fontSize = 11f;

                // Button transitions
                buyBtn.style.transitionProperty = new List<StylePropertyName> { 
                    new StylePropertyName("background-color"), 
                    new StylePropertyName("border-color"),
                    new StylePropertyName("scale")
                };
                buyBtn.style.transitionDuration = new List<TimeValue> { 
                    new TimeValue(0.12f, TimeUnit.Second) 
                };

                if (!isUnlocked)
                {
                    buyBtn.text = "🔒 Нужен планшет";
                    buyBtn.style.backgroundColor = new StyleColor(new Color(0.18f, 0.12f, 0.12f, 0.8f));
                    buyBtn.style.color = new StyleColor(new Color(0.95f, 0.35f, 0.35f, 1f));
                    buyBtn.style.SetBorderColor(new Color(0.95f, 0.35f, 0.35f, 0.4f));
                    buyBtn.enabledSelf = false;
                }
                else if (isPlaced)
                {
                    buyBtn.text = "✓ Установлено (1/1)";
                    buyBtn.style.backgroundColor = new StyleColor(new Color(0.12f, 0.14f, 0.12f, 0.8f));
                    buyBtn.style.color = new StyleColor(new Color(0.4f, 0.7f, 0.4f, 0.8f));
                    buyBtn.style.SetBorderColor(new Color(0.4f, 0.7f, 0.4f, 0.4f));
                    buyBtn.enabledSelf = false;
                }
                else
                {
                    bool isFree = item.isUnique && (item.itemName == "Музыкальный Автомат" || item.itemName == "Торговый Автомат");
                    float price = isFree ? 0f : item.cost;
                    bool canAfford = EconomyManager.Instance == null || EconomyManager.Instance.Balance >= price;

                    if (isFree)
                    {
                        buyBtn.text = "Разместить (Бесплатно)";
                        buyBtn.style.backgroundColor = new StyleColor(new Color(0.12f, 0.22f, 0.15f, 0.9f));
                        buyBtn.style.color = new StyleColor(new Color(0.2f, 0.85f, 0.4f, 1f));
                        buyBtn.style.SetBorderColor(new Color(0.2f, 0.85f, 0.4f, 0.6f));
                    }
                    else
                    {
                        buyBtn.text = $"Купить (${price:F0})";
                        buyBtn.style.backgroundColor = new StyleColor(canAfford ? new Color(0.91f, 0.64f, 0.26f, 0.95f) : new Color(0.22f, 0.12f, 0.12f, 0.9f));
                        buyBtn.style.color = new StyleColor(canAfford ? new Color(0.1f, 0.08f, 0.05f, 1f) : new Color(0.95f, 0.35f, 0.35f, 1f));
                        buyBtn.style.SetBorderColor(canAfford ? new Color(0.91f, 0.64f, 0.26f, 0.5f) : new Color(0.95f, 0.35f, 0.35f, 0.5f));
                    }

                    if (canAfford)
                    {
                        buyBtn.clicked += () => StartPlacement(item);

                        buyBtn.RegisterCallback<MouseEnterEvent>(evt => {
                            buyBtn.style.scale = new StyleScale(new Scale(new Vector3(1.03f, 1.03f, 1f)));
                            if (isFree)
                            {
                                buyBtn.style.backgroundColor = new StyleColor(new Color(0.16f, 0.30f, 0.20f, 0.98f));
                                buyBtn.style.SetBorderColor(new Color(0.2f, 0.85f, 0.4f, 0.9f));
                            }
                            else
                            {
                                buyBtn.style.backgroundColor = new StyleColor(new Color(0.96f, 0.72f, 0.34f, 1f));
                                buyBtn.style.SetBorderColor(new Color(0.91f, 0.64f, 0.26f, 0.9f));
                            }
                        });
                        buyBtn.RegisterCallback<MouseLeaveEvent>(evt => {
                            buyBtn.style.scale = new StyleScale(new Scale(Vector3.one));
                            if (isFree)
                            {
                                buyBtn.style.backgroundColor = new StyleColor(new Color(0.12f, 0.22f, 0.15f, 0.9f));
                                buyBtn.style.SetBorderColor(new Color(0.2f, 0.85f, 0.4f, 0.6f));
                            }
                            else
                            {
                                buyBtn.style.backgroundColor = new StyleColor(new Color(0.91f, 0.64f, 0.26f, 0.95f));
                                buyBtn.style.SetBorderColor(new Color(0.91f, 0.64f, 0.26f, 0.5f));
                            }
                        });
                    }
                    else
                    {
                        buyBtn.enabledSelf = false;
                        buyBtn.text = "Недостаточно средств";
                    }
                }
                card.Add(buyBtn);
            }
        }

        public bool IsItemAlreadyBuilt(BuildableItemSO item)
        {
            var placedObjects = UnityEngine.Object.FindObjectsByType<PlacedFurniture>(FindObjectsSortMode.None);
            foreach (var placed in placedObjects)
            {
                if (placed.itemSO != null && placed.itemSO.itemName == item.itemName)
                {
                    return true;
                }
            }
            return false;
        }

        public bool IsItemUpgradePurchased(BuildableItemSO item)
        {
            if (item.itemName == "Музыкальный Автомат")
                return UpgradeManager.HasJukebox;
            if (item.itemName == "Торговый Автомат")
                return UpgradeManager.HasSnackVending;
            if (item.itemName == "Двойной Холодильник")
                return UpgradeManager.HasDoubleCooler;
            if (item.itemName == "Премиум Стеллаж")
                return UpgradeManager.HasPremiumShelves;
            return true;
        }

        private string GetCategoryText(BuildCategory cat)
        {
            return cat switch
            {
                BuildCategory.Shelves => "СТЕЛЛАЖИ",
                BuildCategory.Cooling => "ОХЛАЖДЕНИЕ",
                BuildCategory.Decor => "ДЕКОР",
                BuildCategory.Tables => "СТОЛЫ",
                _ => "ДРУГОЕ"
            };
        }

        private void SetBadgeStyle(Label badge, BuildCategory cat)
        {
            switch (cat)
            {
                case BuildCategory.Shelves:
                    badge.style.backgroundColor = new StyleColor(new Color(0.12f, 0.18f, 0.28f, 1f));
                    badge.style.color = new StyleColor(new Color(0.4f, 0.7f, 1.0f, 1f));
                    break;
                case BuildCategory.Cooling:
                    badge.style.backgroundColor = new StyleColor(new Color(0.1f, 0.24f, 0.25f, 1f));
                    badge.style.color = new StyleColor(new Color(0.3f, 0.9f, 0.95f, 1f));
                    break;
                case BuildCategory.Decor:
                    badge.style.backgroundColor = new StyleColor(new Color(0.22f, 0.15f, 0.28f, 1f));
                    badge.style.color = new StyleColor(new Color(0.8f, 0.5f, 0.95f, 1f));
                    break;
                case BuildCategory.Tables:
                    badge.style.backgroundColor = new StyleColor(new Color(0.28f, 0.2f, 0.12f, 1f));
                    badge.style.color = new StyleColor(new Color(1.0f, 0.7f, 0.3f, 1f));
                    break;
            }
        }

        private string GetItemSpecificationText(BuildableItemSO item)
        {
            return item.itemName switch
            {
                "Премиум Стеллаж" => "Вместимость: 36 бутылок",
                "Двойной Холодильник" => "Хранение охлажденных напитков",
                "Стол со Стульями" => "Место для отдыха клиентов",
                "Музыкальный Автомат" => "Повышает настроение клиентов",
                "Торговый Автомат" => "Пассивный доход от снеков",
                _ => "Элемент интерьера магазина"
            };
        }
    }
}
