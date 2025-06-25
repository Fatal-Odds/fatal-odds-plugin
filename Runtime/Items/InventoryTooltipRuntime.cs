using UnityEngine;
using UnityEngine.UI;
using System.Collections;

#if UNITY_TEXTMESHPRO
using TMPro;
#endif

namespace FatalOdds.Runtime
{

    [RequireComponent(typeof(RectTransform))]
    [RequireComponent(typeof(CanvasGroup))]

    public sealed class InventoryTooltipRuntime : MonoBehaviour
    {
        public static InventoryTooltipRuntime Instance { get; private set; }

        [Header("Layout")]
        [SerializeField] Vector2 panelSize = new(300, 200);
        [SerializeField] Vector2 cursorOffset = new(24, -24);
        [SerializeField] float edgeBuffer = 16f;

        [Header("Fade")]
        [SerializeField] float fadeDuration = 0.15f;

        // Runtime references
        Canvas hudCanvas;
        CanvasGroup cg;
        RectTransform rt;
        bool isInitialized = false;

#if UNITY_TEXTMESHPRO
        TextMeshProUGUI nameTxt, rarityTxt, stackTxt,
                        descTxt, effectsTxt, flavorTxt;
#else
        Text nameTxt, rarityTxt, stackTxt,
               descTxt, effectsTxt, flavorTxt;
#endif

        void Awake()
        {
            if (Instance != null)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            // Try to initialize immediately if possible
            try
            {
                InitializeTooltip();
                gameObject.SetActive(false); // Hide initially
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[InventoryTooltipRuntime] Failed to initialize in Awake: {e.Message}. Will retry later.");
                gameObject.SetActive(false);
            }
        }

        void Start()
        {
            if (!isInitialized)
            {
                InitializeTooltip();
            }
        }

        void InitializeTooltip()
        {
            if (isInitialized) return;

            try
            {
                BuildCanvasAndPanel();
                isInitialized = true;
                Debug.Log("[InventoryTooltipRuntime] Successfully initialized");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[InventoryTooltipRuntime] Failed to initialize: {e.Message}");
                Debug.LogError($"Stack trace: {e.StackTrace}");
            }
        }

        public void Show(ItemDefinition item, int stack, Vector3 worldPos)
        {
            if (!isInitialized)
            {
                InitializeTooltip();
                if (!isInitialized) return; // Still failed to initialize
            }

            try
            {
                // Handle null item case
                if (item == null)
                {
                    Debug.LogWarning("[InventoryTooltipRuntime] Showing tooltip with null item - using fallback");
                    ShowFallbackTooltip("Unknown Item", stack);
                    return;
                }

                // 1) Fill text
                SetText(nameTxt, item.itemName);
                SetText(rarityTxt, item.rarity.ToString());
                SetText(stackTxt, stack > 1 ? $"x{stack}" : string.Empty);
                SetText(descTxt, item.description ?? "No description available.");
                SetText(effectsTxt, EffectsToString(item, stack));
                SetText(flavorTxt, string.IsNullOrEmpty(item.flavorText)
                                   ? string.Empty
                                   : $"\"{item.flavorText}\"");

                // 2) Colour rarities quickly (simple tint)
                Color rareC = RarityColor(item.rarity);
                if (rarityTxt != null) rarityTxt.color = rareC;
                if (nameTxt != null) nameTxt.color = rareC;

                // 3) Move next to cursor
                PositionTooltip(Input.mousePosition);

                // 4) Fade-in
                gameObject.SetActive(true);        // make it active first
                StopAllCoroutines();               // then stop any old fades
                StartCoroutine(Fade(0, 1));        // now this always starts successfullyyea

                Debug.Log($"[InventoryTooltipRuntime] Showing tooltip for {item.itemName}");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[InventoryTooltipRuntime] Error showing tooltip: {e.Message}");
            }
        }

        private void ShowFallbackTooltip(string itemName, int stack)
        {
            try
            {
                SetText(nameTxt, itemName);
                SetText(rarityTxt, "Unknown");
                SetText(stackTxt, stack > 1 ? $"x{stack}" : string.Empty);
                SetText(descTxt, "Item information unavailable.");
                SetText(effectsTxt, "Effects unknown.");
                SetText(flavorTxt, string.Empty);

                // Default color
                Color defaultColor = Color.white;
                if (rarityTxt != null) rarityTxt.color = defaultColor;
                if (nameTxt != null) nameTxt.color = defaultColor;

                PositionTooltip(Input.mousePosition);
                gameObject.SetActive(true);        // make it active first
                StopAllCoroutines();               // then stop any old fades
                StartCoroutine(Fade(0, 1));        // now this always starts successfully


                Debug.Log($"[InventoryTooltipRuntime] Showing fallback tooltip for {itemName}");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[InventoryTooltipRuntime] Error showing fallback tooltip: {e.Message}");
            }
        }

        public void Hide()
        {
            if (!isInitialized || cg == null)   // nothing to do
                return;

            try
            {
                // If the object is already inactive there’s no point fading – just ensure alpha = 0
                if (!gameObject.activeInHierarchy)
                {
                    cg.alpha = 0f;
                    return;
                }

                // Make sure it’s active so the coroutine can run
                gameObject.SetActive(true);

                StopAllCoroutines();
                StartCoroutine(Fade(cg.alpha, 0f, deactivateWhenDone: true));   // ← auto-deactivate at the end

                Debug.Log("[InventoryTooltipRuntime] Hiding tooltip");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[InventoryTooltipRuntime] Error hiding tooltip: {e.Message}");
                cg.alpha = 0f;   // emergency hide – keep object active
            }
        }

        void SetText(Component t, string value)
        {
            if (t == null)
            {
                Debug.LogWarning("[InventoryTooltipRuntime] Attempted to set text on null component");
                return;
            }

            try
            {
#if UNITY_TEXTMESHPRO
                ((TextMeshProUGUI)t).text = value ?? "";
#else
                ((Text)t).text = value ?? "";
#endif
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[InventoryTooltipRuntime] Error setting text: {e.Message}");
            }
        }

        void PositionTooltip(Vector2 screenPos)
        {
            if (rt == null || hudCanvas == null) return;

            try
            {
                Vector2 target = screenPos + cursorOffset;
                Vector2 size = rt.sizeDelta == Vector2.zero ? panelSize : rt.sizeDelta;
                Vector2 canvasSize = hudCanvas.GetComponent<RectTransform>().sizeDelta;

                // Convert screen position to canvas position for screen space overlay
                if (hudCanvas.renderMode == RenderMode.ScreenSpaceOverlay)
                {
                    // For overlay canvas, we need to convert from screen coordinates
                    target = screenPos + cursorOffset;

                    // Adjust for canvas scale
                    float canvasScale = hudCanvas.scaleFactor;
                    target /= canvasScale;

                    // Canvas origin is at center for overlay mode, so adjust
                    target.x -= canvasSize.x * 0.5f;
                    target.y -= canvasSize.y * 0.5f;
                }

                // Clamp horizontally
                if (target.x + size.x > canvasSize.x * 0.5f - edgeBuffer)
                    target.x = canvasSize.x * 0.5f - size.x - edgeBuffer;
                if (target.x < -canvasSize.x * 0.5f + edgeBuffer)
                    target.x = -canvasSize.x * 0.5f + edgeBuffer;

                // Clamp vertically
                if (target.y - size.y < -canvasSize.y * 0.5f + edgeBuffer)
                    target.y = -canvasSize.y * 0.5f + size.y + edgeBuffer;
                if (target.y > canvasSize.y * 0.5f - edgeBuffer)
                    target.y = canvasSize.y * 0.5f - edgeBuffer;

                rt.anchoredPosition = target;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[InventoryTooltipRuntime] Error positioning tooltip: {e.Message}");
            }
        }

        IEnumerator Fade(float from, float to, bool deactivateWhenDone = false)
        {
            if (cg == null) yield break;

            cg.alpha = from;
            float t = 0f;
            while (t < fadeDuration)
            {
                t += Time.unscaledDeltaTime;
                cg.alpha = Mathf.Lerp(from, to, t / fadeDuration);
                yield return null;
            }
            cg.alpha = to;

            if (deactivateWhenDone && to == 0f)
                gameObject.SetActive(false);
        }

        // Builds/attaches the tooltip panel under a screen-space overlay canvas
        void BuildCanvasAndPanel()
        {
            /*──────── locate / create overlay canvas ─────────*/
            hudCanvas = FindExistingOverlayCanvas() ?? CreateOverlayCanvas();
            if (hudCanvas == null)
                throw new System.Exception("Failed to create or find overlay canvas");

            /*──────── configure the root ─*/
            gameObject.name = "InventoryTooltipRuntime";
            transform.SetParent(hudCanvas.transform, false);

            // We created this GO with a RectTransform, but add one if (somehow) it’s missing
            rt = gameObject.GetComponent<RectTransform>() ??
                 gameObject.AddComponent<RectTransform>();
            rt.sizeDelta = panelSize;

            /*──────── be sure we have a CanvasGroup */
            cg = GetComponent<CanvasGroup>();
            cg.alpha = 0f;
            cg.blocksRaycasts = false;
            cg.interactable = false;

            /*──────── background & layout */
            Image bg = gameObject.GetComponent<Image>() ??
                       gameObject.AddComponent<Image>();
            bg.color = new Color(0.10f, 0.10f, 0.10f, 0.95f);
            bg.raycastTarget = false;

            VerticalLayoutGroup vlg =
                gameObject.GetComponent<VerticalLayoutGroup>() ??
                gameObject.AddComponent<VerticalLayoutGroup>();
            vlg.padding = new RectOffset(12, 12, 12, 12);
            vlg.spacing = 6;
            vlg.childControlHeight = true;
            vlg.childForceExpandWidth = true;

            ContentSizeFitter csf =
                gameObject.GetComponent<ContentSizeFitter>() ??
                gameObject.AddComponent<ContentSizeFitter>();
            csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            /*──────── text fields ──*/
#if UNITY_TEXTMESHPRO
    nameTxt    = AddText(18, FontStyle.Bold,   "ItemName");
    rarityTxt  = AddText(14, FontStyle.Italic, "ItemRarity");
    stackTxt   = AddText(12, FontStyle.Normal, "StackInfo");
    descTxt    = AddText(14, FontStyle.Normal, "Description");
    effectsTxt = AddText(13, FontStyle.Normal, "Effects");
    flavorTxt  = AddText(12, FontStyle.Italic, "FlavorText");
#else
            nameTxt = AddText(18, FontStyle.Bold, "ItemName");
            rarityTxt = AddText(14, FontStyle.Italic, "ItemRarity");
            stackTxt = AddText(12, FontStyle.Normal, "StackInfo");
            descTxt = AddText(14, FontStyle.Normal, "Description");
            effectsTxt = AddText(13, FontStyle.Normal, "Effects");
            flavorTxt = AddText(12, FontStyle.Italic, "FlavorText");
#endif

            if (flavorTxt != null)
                flavorTxt.color = new Color(0.8f, 0.8f, 0.8f);

            Debug.Log("[InventoryTooltipRuntime] Successfully built canvas and panel");
        }

        Canvas FindExistingOverlayCanvas()
        {
            Canvas[] canvases = FindObjectsOfType<Canvas>();
            foreach (var canvas in canvases)
            {
                if (canvas.renderMode == RenderMode.ScreenSpaceOverlay)
                {
                    Debug.Log($"[InventoryTooltipRuntime] Found existing overlay canvas: {canvas.name}");
                    return canvas;
                }
            }
            return null;
        }

        Canvas CreateOverlayCanvas()
        {
            try
            {
                GameObject canvasObj = new GameObject("HUDCanvas");
                Canvas canvas = canvasObj.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.sortingOrder = 100;

                CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new(1920, 1080);

                canvasObj.AddComponent<GraphicRaycaster>();

                Debug.Log("[InventoryTooltipRuntime] Created new overlay canvas");
                return canvas;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[InventoryTooltipRuntime] Failed to create canvas: {e.Message}");
                return null;
            }
        }

#if UNITY_TEXTMESHPRO
        TextMeshProUGUI AddText(int size, FontStyle style, string objName)
    {
        var o  = new GameObject(objName);
        o.transform.SetParent(transform, false);

        var t  = o.AddComponent<TextMeshProUGUI>();
        t.fontSize          = size;
        t.enableWordWrapping = true;
        t.alignment         = TextAlignmentOptions.TopLeft;
        t.fontStyle         = (FontStyles)style;
        t.color             = Color.white;
        t.text              = "";
        t.raycastTarget     = false;     // ← KEY

        return t;
    }
#else
        Text AddText(int size, FontStyle style, string objName)
        {
            var o = new GameObject(objName);
            o.transform.SetParent(transform, false);

            var t = o.AddComponent<Text>();
            t.fontSize = size;
            t.fontStyle = style;
            t.color = Color.white;
            t.alignment = TextAnchor.UpperLeft;
            t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            t.text = "";
            t.raycastTarget = false;    // ← KEY

            return t;
        }
#endif

        Color RarityColor(ItemRarity r) =>
            r switch
            {
                ItemRarity.Uncommon => new Color(0.4f, 0.8f, 0.4f),
                ItemRarity.Rare => new Color(0.3f, 0.5f, 0.9f),
                ItemRarity.Epic => new Color(0.7f, 0.4f, 0.9f),
                ItemRarity.Legendary => new Color(1f, 0.8f, 0.3f),
                ItemRarity.Artifact => new Color(0.8f, 0.3f, 0.3f),
                _ => Color.white
            };

        string EffectsToString(ItemDefinition item, int stacks)
        {
            if (item.modifiers == null || item.modifiers.Count == 0)
                return "No stat effects.";

            var sb = new System.Text.StringBuilder();
            foreach (var m in item.modifiers)
            {
                float baseVal = m.value;
                float effVal = stacks > 1 ? baseVal * stacks : baseVal;

                string line = m.modifierType switch
                {
                    ModifierType.Flat => $"{Sign(effVal)}{effVal:F1} {m.statDisplayName}",
                    ModifierType.PercentageAdditive => $"{Sign(effVal * 100f)}{effVal * 100f:F0}% {m.statDisplayName}",
                    ModifierType.Percentage => $"{Sign((effVal - 1f) * 100f)}{(effVal - 1f) * 100f:F0}% {m.statDisplayName}",
                    _ => $"{effVal:F1} {m.statDisplayName}"
                };
                sb.Append('•').Append(' ').AppendLine(line);
            }
            return sb.ToString().TrimEnd();

            static string Sign(float v) => v >= 0 ? "+" : string.Empty;
        }

        void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }
    }
}