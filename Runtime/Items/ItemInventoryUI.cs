using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

#if UNITY_TEXTMESHPRO
using TMPro;
#endif

namespace FatalOdds.Runtime
{
    [DefaultExecutionOrder(-50)]
    public sealed class ItemInventoryUI : MonoBehaviour
    {
        /*─── Inspector ───────*/
        #region Inspector
        [Header("UI")]
        [SerializeField] GameObject inventoryPanel;
        [SerializeField] Transform itemGridParent;
        [SerializeField] GameObject itemSlotPrefab;

        [Header("Input")]
        [SerializeField] KeyCode toggleKey = KeyCode.Tab;
        [SerializeField] bool holdToShow = false;

        [Header("Auto Setup")]
        [SerializeField] bool createMissingPrefab = true;
        [SerializeField] bool createMissingTooltip = true;

        [Header("Camera Integration")]
        [SerializeField] string cameraInputMethodName = "DisableInput";
        [SerializeField] string cameraReenableMethodName = "EnableInput";

        [Header("Debug")]
        [SerializeField] bool logEvents = true;
        #endregion

        /*─── Runtime ────────*/
        readonly List<GameObject> activeSlots = new();
        readonly Dictionary<string, ItemDefinition> itemDefs = new();
        ModifierManager modifierManager;
        bool isVisible;

        private Component cameraController;

        // UI Text elements - created dynamically
#if UNITY_TEXTMESHPRO
        private TextMeshProUGUI inventoryTitleText;
        private TextMeshProUGUI emptyInventoryText;
        private TextMeshProUGUI instructionText;
#else
        private Text inventoryTitleText;
        private Text emptyInventoryText;
        private Text instructionText;
#endif

        /*─── Unity lifecycle ─────*/
        void Awake()
        {
            // Basic validation and auto-setup
            if (!inventoryPanel) Debug.LogError("ItemInventoryUI: InventoryPanel missing.");
            if (!itemGridParent) Debug.LogError("ItemInventoryUI: ItemGridParent missing.");

            // Auto-create missing prefab if enabled
            if (!itemSlotPrefab && createMissingPrefab)
            {
                itemSlotPrefab = CreateDefaultItemSlotPrefab();
            }

            if (!itemSlotPrefab) Debug.LogError("ItemInventoryUI: ItemSlotPrefab missing.");

            // Find camera controller dynamically to avoid assembly reference issues
            FindCameraController();

            // Lookups
            modifierManager = FindObjectOfType<ModifierManager>();
            LoadItemDefinitions();

            // Create tooltip system if missing
            if (createMissingTooltip)
            {
                EnsureTooltipSystem();
            }

            // Create UI text elements dynamically
            CreateUITextElements();

            if (inventoryPanel != null)
            {
                inventoryPanel.SetActive(false);
            }
            isVisible = false;

            // Delete all items on the ground to avoid the issue of restarting the game causes enemies to drop and leave items on the floor.
            ItemSpawner.ClearAllPickups();
        }

        void Update() => ProcessInput();

        /*─── Auto Setup Methods ─────*/
        private GameObject CreateDefaultItemSlotPrefab()
        {
            Debug.Log("[ItemInventoryUI] Creating default item slot prefab...");

            GameObject prefab = new GameObject("DefaultItemSlot");

            // Add RectTransform
            RectTransform rect = prefab.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(80, 80);

            // Add background image
            Image bg = prefab.AddComponent<Image>();
            bg.color = new Color(0.2f, 0.2f, 0.2f, 0.8f);

            // CRITICAL: Add ItemSlotUI component and ensure it's properly configured
            ItemSlotUI slotUI = prefab.AddComponent<ItemSlotUI>();

            // Force the slot UI to set up its missing components immediately
            // We need to temporarily activate the prefab for this to work
            bool wasActive = prefab.activeSelf;
            prefab.SetActive(true);

            // This will trigger the Awake() method and create missing components
            slotUI.SendMessage("Awake", SendMessageOptions.DontRequireReceiver);

            prefab.SetActive(wasActive);

            Debug.Log("[ItemInventoryUI] Created default item slot prefab with ItemSlotUI component");
            return prefab;
        }

        // Creates (only once) a tooltip runner whose root object is already a UI element
        private void EnsureTooltipSystem()
        {
            if (InventoryTooltipRuntime.Instance != null)
                return;

            Debug.Log("[ItemInventoryUI] Creating tooltip system…");

            // Create the GameObject *with* a RectTransform up-front so it replaces the
            // default Transform and is UI-ready straight away.
            GameObject tooltipObj = new GameObject("InventoryTooltipRuntime", typeof(RectTransform), typeof(CanvasGroup));

            // Centrally anchor/size the RectTransform
            RectTransform rt = tooltipObj.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = Vector2.zero;
            rt.anchoredPosition = Vector2.zero;

            // Add the runner component
            tooltipObj.AddComponent<InventoryTooltipRuntime>();

            Debug.Log("[ItemInventoryUI] Created tooltip system");
        }

        /*─── Camera Integration ─────*/
        void FindCameraController()
        {
            // Look for ThirdPersonCamera component in the scene
            var cameras = FindObjectsOfType<MonoBehaviour>();
            foreach (var cam in cameras)
            {
                if (cam.GetType().Name == "ThirdPersonCamera" || cam.GetType().Name == "CameraInputManager")
                {
                    cameraController = cam;
                    if (logEvents) Debug.Log($"[Inventory] Found camera controller: {cam.gameObject.name}");
                    break;
                }
            }

            if (cameraController == null && logEvents)
            {
                Debug.LogWarning("[Inventory] No camera controller found - cursor management may not work properly");
            }
        }

        void SetCameraInputEnabled(bool enabled)
        {
            if (cameraController == null) return;

            try
            {
                string methodName = enabled ? cameraReenableMethodName : cameraInputMethodName;
                var method = cameraController.GetType().GetMethod(methodName);
                if (method != null)
                {
                    method.Invoke(cameraController, null);
                    if (logEvents) Debug.Log($"[Inventory] Called {methodName} on camera");
                }
                else
                {
                    Debug.LogWarning($"[Inventory] Method {methodName} not found on camera controller");
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[Inventory] Error calling camera method: {e.Message}");
            }
        }

        /*─── Dynamic UI Text Creation ─*/
        void CreateUITextElements()
        {
            if (inventoryPanel == null) return;

            try
            {
                // Ensure inventory panel is properly set up
                SetupInventoryPanel();

                // Find or create a content area for our text elements
                Transform contentArea = FindOrCreateContentArea();

                // Create title text
                inventoryTitleText = CreateText("InventoryTitle", contentArea, "INVENTORY", 32, FontStyle.Bold);
                if (inventoryTitleText != null)
                {
                    PositionText(inventoryTitleText.gameObject, new Vector2(0f, 0.9f), new Vector2(1f, 1f));
                    SetTextAlignment(inventoryTitleText, TextAnchor.MiddleCenter);
                    SetTextColor(inventoryTitleText, new Color(1f, 0.9f, 0.7f)); // Golden color
                }

                // Create empty inventory message
                emptyInventoryText = CreateText("EmptyMessage", contentArea,
                    "Your inventory is empty.\nDefeat enemies and find items to fill it!", 18, FontStyle.Normal);
                if (emptyInventoryText != null)
                {
                    PositionText(emptyInventoryText.gameObject, new Vector2(0.1f, 0.35f), new Vector2(0.9f, 0.65f));
                    SetTextAlignment(emptyInventoryText, TextAnchor.MiddleCenter);
                    SetTextColor(emptyInventoryText, new Color(0.8f, 0.8f, 0.8f));
                    EnableWordWrap(emptyInventoryText);
                }

                // Create instruction text
                instructionText = CreateText("Instructions", contentArea,
                    "Press TAB to close • Hover items for details", 14, FontStyle.Italic);
                if (instructionText != null)
                {
                    PositionText(instructionText.gameObject, new Vector2(0f, 0f), new Vector2(1f, 0.08f));
                    SetTextAlignment(instructionText, TextAnchor.MiddleCenter);
                    SetTextColor(instructionText, new Color(0.7f, 0.7f, 0.7f));
                }

                // Ensure item grid is properly positioned
                SetupItemGrid();

                if (logEvents) Debug.Log("[Inventory] Created dynamic UI text elements");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[Inventory] Error creating UI text elements: {e.Message}");
            }
        }

        void SetupInventoryPanel()
        {
            if (inventoryPanel == null) return;

            // Ensure the inventory panel fills the screen properly
            RectTransform panelRect = inventoryPanel.GetComponent<RectTransform>();
            if (panelRect != null)
            {
                panelRect.anchorMin = Vector2.zero;
                panelRect.anchorMax = Vector2.one;
                panelRect.sizeDelta = Vector2.zero;
                panelRect.anchoredPosition = Vector2.zero;
            }

            // Ensure there's a background
            Image panelImage = inventoryPanel.GetComponent<Image>();
            if (panelImage == null)
            {
                panelImage = inventoryPanel.AddComponent<Image>();
            }
            panelImage.color = new Color(0f, 0f, 0f, 0.8f); // Semi-transparent black
        }

        void SetupItemGrid()
        {
            if (itemGridParent == null) return;

            // Position the grid on the left side of the screen
            RectTransform gridRect = itemGridParent.GetComponent<RectTransform>();
            if (gridRect != null)
            {
                gridRect.anchorMin = new Vector2(0.05f, 0.15f); // Left side, leave room for title
                gridRect.anchorMax = new Vector2(0.7f, 0.85f);   // Don't go all the way to right
                gridRect.sizeDelta = Vector2.zero;
                gridRect.anchoredPosition = Vector2.zero;
            }

            // Ensure grid layout is properly configured
            GridLayoutGroup grid = itemGridParent.GetComponent<GridLayoutGroup>();
            if (grid == null)
            {
                grid = itemGridParent.gameObject.AddComponent<GridLayoutGroup>();
            }

            grid.cellSize = new Vector2(80, 80);
            grid.spacing = new Vector2(10, 10);
            grid.startCorner = GridLayoutGroup.Corner.UpperLeft;
            grid.startAxis = GridLayoutGroup.Axis.Horizontal;
            grid.childAlignment = TextAnchor.UpperLeft;
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = 6; // 6 items per row
        }

        Transform FindOrCreateContentArea()
        {
            // Look for existing content area first
            Transform contentArea = inventoryPanel.transform.Find("Content");

            if (contentArea == null)
            {
                // Create a content area
                GameObject contentObj = new GameObject("Content");
                contentObj.transform.SetParent(inventoryPanel.transform, false);

                RectTransform contentRect = contentObj.AddComponent<RectTransform>();
                contentRect.anchorMin = new Vector2(0.05f, 0.05f);
                contentRect.anchorMax = new Vector2(0.95f, 0.95f);
                contentRect.sizeDelta = Vector2.zero;
                contentRect.anchoredPosition = Vector2.zero;

                contentArea = contentObj.transform;
            }

            return contentArea;
        }

#if UNITY_TEXTMESHPRO
        TextMeshProUGUI CreateText(string name, Transform parent, string text, int fontSize, FontStyle style)
        {
            GameObject textObj = new GameObject(name);
            textObj.transform.SetParent(parent, false);

            RectTransform textRect = textObj.AddComponent<RectTransform>();
            textRect.sizeDelta = new Vector2(0, fontSize + 4);

            TextMeshProUGUI textComponent = textObj.AddComponent<TextMeshProUGUI>();
            textComponent.text = text;
            textComponent.fontSize = fontSize;
            textComponent.color = Color.white;
            textComponent.fontStyle = (FontStyles)style;
            textComponent.alignment = TextAlignmentOptions.Left;

            return textComponent;
        }

        void SetTextAlignment(TextMeshProUGUI textComponent, TextAnchor anchor)
        {
            textComponent.alignment = anchor switch
            {
                TextAnchor.MiddleCenter => TextAlignmentOptions.Center,
                TextAnchor.UpperCenter => TextAlignmentOptions.Top,
                TextAnchor.LowerCenter => TextAlignmentOptions.Bottom,
                _ => TextAlignmentOptions.Left
            };
        }

        void SetTextColor(TextMeshProUGUI textComponent, Color color)
        {
            textComponent.color = color;
        }

        void EnableWordWrap(TextMeshProUGUI textComponent)
        {
            textComponent.enableWordWrapping = true;
        }
#else
        Text CreateText(string name, Transform parent, string text, int fontSize, FontStyle style)
        {
            GameObject textObj = new GameObject(name);
            textObj.transform.SetParent(parent, false);

            RectTransform textRect = textObj.AddComponent<RectTransform>();
            textRect.sizeDelta = new Vector2(0, fontSize + 4);

            Text textComponent = textObj.AddComponent<Text>();
            textComponent.text = text;
            textComponent.fontSize = fontSize;
            textComponent.color = Color.white;
            textComponent.fontStyle = style;
            textComponent.alignment = TextAnchor.UpperLeft;
            textComponent.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            return textComponent;
        }

        void SetTextAlignment(Text textComponent, TextAnchor anchor)
        {
            textComponent.alignment = anchor;
        }

        void SetTextColor(Text textComponent, Color color)
        {
            textComponent.color = color;
        }

        void EnableWordWrap(Text textComponent)
        {
            textComponent.horizontalOverflow = HorizontalWrapMode.Wrap;
            textComponent.verticalOverflow = VerticalWrapMode.Overflow;
        }
#endif

        void PositionText(GameObject textObj, Vector2 anchorMin, Vector2 anchorMax)
        {
            RectTransform rect = textObj.GetComponent<RectTransform>();
            if (rect != null)
            {
                rect.anchorMin = anchorMin;
                rect.anchorMax = anchorMax;
                rect.sizeDelta = Vector2.zero;
                rect.anchoredPosition = Vector2.zero;
            }
        }

        /*─── Input handling ─────*/
        void ProcessInput()
        {
            if (holdToShow)
            {
                bool shouldShow = Input.GetKey(toggleKey);
                if (shouldShow != isVisible) SetInventoryVisible(shouldShow);
            }
            else if (Input.GetKeyDown(toggleKey))
            {
                ToggleInventory();
            }
        }

        /*─── Public API (used by other scripts) ─────*/
        public bool IsVisible => isVisible;

        public void ToggleInventory() => SetInventoryVisible(!isVisible);

        public void SetInventoryVisible(bool visible, bool instant = false)
        {
            if (visible == isVisible) return;

            isVisible = visible;

            // ─── Cursor & camera ──
            if (isVisible)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
                SetCameraInputEnabled(false);
            }
            else
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
                SetCameraInputEnabled(true);
            }

            // ─── Panel toggle ─────
            if (inventoryPanel != null)
                inventoryPanel.SetActive(isVisible);

            if (logEvents)
                Debug.Log($"[Inventory] {(isVisible ? "Shown" : "Hidden")}");

            // ─── Refresh / cleanup 
            if (isVisible) RefreshDisplay();
            else HideTooltip();
        }

        public void ShowTooltip(string itemName, int stack, Vector3 worldPos)
        {
            Debug.Log($"[ItemInventoryUI] ShowTooltip called for {itemName} x{stack}");

            if (InventoryTooltipRuntime.Instance == null)
            {
                Debug.LogWarning("[ItemInventoryUI] InventoryTooltipRuntime.Instance is null!");
                EnsureTooltipSystem(); // Try to create it

                // Wait a frame and try again
                if (InventoryTooltipRuntime.Instance == null)
                {
                    Debug.LogError("[ItemInventoryUI] Still no tooltip system after creation attempt");
                    return;
                }
            }

            if (itemDefs.TryGetValue(itemName, out var def))
            {
                Debug.Log($"[ItemInventoryUI] Found item definition for {itemName}, showing tooltip");
                InventoryTooltipRuntime.Instance.Show(def, stack, worldPos);
            }
            else
            {
                Debug.LogWarning($"[ItemInventoryUI] No item definition found for {itemName}, showing fallback tooltip");
                InventoryTooltipRuntime.Instance.Show(null, stack, worldPos);
            }
        }

        public void HideTooltip()
        {
            if (InventoryTooltipRuntime.Instance != null)
            {
                InventoryTooltipRuntime.Instance.Hide();
            }
        }

        void RefreshDisplay()
        {
            Debug.Log("[ItemInventoryUI] RefreshDisplay called");

            ClearSlots();

            if (!modifierManager)
            {
                Debug.Log("[ItemInventoryUI] No modifier manager found, showing empty state");
                ShowEmptyState();
                return;
            }

            bool hasItems = false;
            var itemStacks = modifierManager.ItemStacks;

            Debug.Log($"[ItemInventoryUI] Found {itemStacks.Count} different items in ItemStacks");

            foreach (var kvp in itemStacks)
            {
                string itemName = kvp.Key;
                int count = kvp.Value;

                Debug.Log($"[ItemInventoryUI] Checking item: {itemName} with count: {count}");

                if (count > 0)
                {
                    Debug.Log($"[ItemInventoryUI] Creating slot for {itemName} x{count}");
                    CreateSlot(itemName, count);
                    hasItems = true;
                }
            }

            if (!hasItems)
            {
                Debug.Log("[ItemInventoryUI] No items found, showing empty state");
                ShowEmptyState();
            }
            else
            {
                Debug.Log("[ItemInventoryUI] Found items, hiding empty state");
                HideEmptyState();
            }
        }

        void ShowEmptyState()
        {
            if (emptyInventoryText != null)
            {
                emptyInventoryText.gameObject.SetActive(true);
            }
        }

        void HideEmptyState()
        {
            if (emptyInventoryText != null)
            {
                emptyInventoryText.gameObject.SetActive(false);
            }
        }

        void CreateSlot(string itemName, int count)
        {
            if (itemSlotPrefab == null)
            {
                Debug.LogError("[ItemInventoryUI] itemSlotPrefab is null!");
                return;
            }

            if (itemGridParent == null)
            {
                Debug.LogError("[ItemInventoryUI] itemGridParent is null!");
                return;
            }

            GameObject slotObj = Instantiate(itemSlotPrefab, itemGridParent);
            slotObj.name = $"Slot_{itemName}";

            // Ensure the instantiated object has ItemSlotUI component
            var slot = slotObj.GetComponent<ItemSlotUI>();
            if (slot == null)
            {
                Debug.LogWarning($"[ItemInventoryUI] Adding missing ItemSlotUI component to instantiated slot");
                slot = slotObj.AddComponent<ItemSlotUI>();
            }

            if (slot != null)
            {
                itemDefs.TryGetValue(itemName, out ItemDefinition def);
                slot.Setup(itemName, count, def, this);
                Debug.Log($"[ItemInventoryUI] Created slot for {itemName} with ItemSlotUI component");
            }
            else
            {
                Debug.LogError($"[ItemInventoryUI] Failed to get or add ItemSlotUI component!");
                Destroy(slotObj);
                return;
            }

            activeSlots.Add(slotObj);
        }

        void ClearSlots()
        {
            foreach (var go in activeSlots)
            {
                if (go != null) Destroy(go);
            }
            activeSlots.Clear();
        }

        void LoadItemDefinitions()
        {
            itemDefs.Clear();
            try
            {
                foreach (var def in ItemSpawner.GetAllItems())
                {
                    if (def) itemDefs[def.itemName] = def;
                }
                if (logEvents) Debug.Log($"[Inventory] Loaded {itemDefs.Count} item definitions");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[Inventory] Error loading item definitions: {e.Message}");
            }
        }

        /*─── Event Handling ─────*/
        void OnApplicationFocus(bool hasFocus)
        {
            // Ensure cursor state is correct when returning to the game
            if (hasFocus && !isVisible)
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
        }

        void OnDestroy()
        {
            // Ensure we don't leave the game paused
            if (Time.timeScale == 0f)
            {
                Time.timeScale = 1f;
            }
        }
    }
}