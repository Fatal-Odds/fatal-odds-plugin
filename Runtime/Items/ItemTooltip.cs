using UnityEngine;
using UnityEngine.UI;
using FatalOdds.Runtime;

#if UNITY_TEXTMESHPRO
using TMPro;
#endif

namespace FatalOdds.Runtime
{
    // Tooltip UI that shows detailed item information on hover
    public class ItemTooltip : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private GameObject tooltipPanel;
#if UNITY_TEXTMESHPRO
        [SerializeField] private TextMeshProUGUI itemNameText;
        [SerializeField] private TextMeshProUGUI rarityText;
        [SerializeField] private TextMeshProUGUI stackInfoText;
        [SerializeField] private TextMeshProUGUI descriptionText;
        [SerializeField] private TextMeshProUGUI effectsText;
        [SerializeField] private TextMeshProUGUI flavorText;
#else
        [SerializeField] private Text itemNameText;
        [SerializeField] private Text rarityText;
        [SerializeField] private Text stackInfoText;
        [SerializeField] private Text descriptionText;
        [SerializeField] private Text effectsText;
        [SerializeField] private Text flavorText;
#endif
        [SerializeField] private Image rarityBorderImage;
        [SerializeField] private Image backgroundImage;

        [Header("Positioning")]
        [SerializeField] private Vector2 offset = new Vector2(10, 10);
        [SerializeField] private bool followCursor = true;
        [SerializeField] private float edgeBuffer = 20f; // Distance from screen edges

        [Header("Animation")]
        [SerializeField] private float fadeInDuration = 0.15f;
        [SerializeField] private AnimationCurve fadeInCurve = new AnimationCurve(new Keyframe(0f, 0f), new Keyframe(1f, 1f));

        [Header("Styling")]
        [SerializeField] private ItemSlotUI.RarityColorSet rarityColors;

        // Runtime state
        private CanvasGroup canvasGroup;
        private RectTransform rectTransform;
        private Canvas parentCanvas;
        private Camera uiCamera;
        private bool isVisible = false;

        private void Awake()
        {
            SetupTooltip();
        }

        private void Start()
        {
            Hide(instant: true);
        }

        private void Update()
        {
            if (isVisible && followCursor)
            {
                UpdatePosition(Input.mousePosition);
            }
        }

        private void SetupTooltip()
        {
            // Ensure we have required components
            canvasGroup = tooltipPanel.GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                canvasGroup = tooltipPanel.AddComponent<CanvasGroup>();
            }

            rectTransform = tooltipPanel.GetComponent<RectTransform>();
            parentCanvas = GetComponentInParent<Canvas>();

            // Find UI camera
            if (parentCanvas.renderMode == RenderMode.ScreenSpaceCamera)
            {
                uiCamera = parentCanvas.worldCamera;
            }
            else if (parentCanvas.renderMode == RenderMode.ScreenSpaceOverlay)
            {
                uiCamera = null; // No camera needed for overlay
            }

            // Setup default rarity colors if not set
            if (rarityColors.common == Color.clear)
            {
                rarityColors = new ItemSlotUI.RarityColorSet();
            }

            // Setup animation curve if not configured
            if (fadeInCurve == null || fadeInCurve.keys.Length == 0)
            {
                fadeInCurve = CreateEaseOutCurve();
            }
        }

        private AnimationCurve CreateEaseOutCurve()
        {
            // Create an ease-out curve manually
            Keyframe[] keys = new Keyframe[]
            {
                new Keyframe(0f, 0f, 0f, 2f), // Start slow
                new Keyframe(1f, 1f, 0f, 0f)  // End fast
            };
            return new AnimationCurve(keys);
        }

        public void Show(ItemDefinition item, int stackCount, Vector3 worldPosition)
        {
            if (item == null) return;

            // Update content
            UpdateTooltipContent(item, stackCount);

            // Show and position
            ShowTooltip(worldPosition);
        }

        public void ShowBasic(string itemName, int stackCount, Vector3 worldPosition)
        {
            // Fallback for items without definitions
            UpdateBasicContent(itemName, stackCount);
            ShowTooltip(worldPosition);
        }

        private void UpdateTooltipContent(ItemDefinition item, int stackCount)
        {
            // Item name
            if (itemNameText != null)
            {
                itemNameText.text = item.itemName;
                itemNameText.color = rarityColors.GetColor(item.rarity);
            }

            // Rarity
            if (rarityText != null)
            {
                rarityText.text = item.rarity.ToString();
                rarityText.color = rarityColors.GetColor(item.rarity);
            }

            // Stack information
            if (stackInfoText != null)
            {
                if (stackCount > 1)
                {
                    string stackType = item.stackingType == ItemStackingType.Linear
                        ? "Linear Stacking"
                        : "Diminishing Returns";
                    stackInfoText.text = $"x{stackCount} ({stackType})";
                }
                else
                {
                    stackInfoText.text = "";
                }
            }

            // Description
            if (descriptionText != null)
            {
                descriptionText.text = item.description;
            }

            // Effects with stacking calculations
            if (effectsText != null)
            {
                effectsText.text = GetEffectsText(item, stackCount);
            }

            // Flavor text
            if (flavorText != null)
            {
                if (!string.IsNullOrEmpty(item.flavorText))
                {
                    flavorText.text = $"\"{item.flavorText}\"";
                    flavorText.gameObject.SetActive(true);
                }
                else
                {
                    flavorText.gameObject.SetActive(false);
                }
            }

            // Update visual styling
            UpdateTooltipStyling(item.rarity);
        }

        private void UpdateBasicContent(string itemName, int stackCount)
        {
            // Basic fallback content
            if (itemNameText != null)
            {
                itemNameText.text = itemName;
                itemNameText.color = Color.white;
            }

            if (rarityText != null)
            {
                rarityText.text = "Unknown";
                rarityText.color = Color.gray;
            }

            if (stackInfoText != null)
            {
                stackInfoText.text = stackCount > 1 ? $"x{stackCount}" : "";
            }

            if (descriptionText != null)
            {
                descriptionText.text = "A mysterious item with unknown properties.";
            }

            if (effectsText != null)
            {
                effectsText.text = "Effects unknown - no definition found.";
            }

            if (flavorText != null)
            {
                flavorText.gameObject.SetActive(false);
            }

            UpdateTooltipStyling(ItemRarity.Common);
        }

        private string GetEffectsText(ItemDefinition item, int stackCount)
        {
            if (item.modifiers == null || item.modifiers.Count == 0)
            {
                return "No stat effects.";
            }

            System.Text.StringBuilder effects = new System.Text.StringBuilder();

            foreach (var modifier in item.modifiers)
            {
                // Calculate effective value with stacking
                float baseValue = modifier.value;
                float effectiveValue = CalculateStackedValue(baseValue, stackCount, item.stackingType);

                // Format the effect text
                string effectLine = FormatEffectText(modifier, baseValue, effectiveValue, stackCount);
                effects.AppendLine(effectLine);
            }

            return effects.ToString().TrimEnd();
        }

        private float CalculateStackedValue(float baseValue, int stackCount, ItemStackingType stackingType)
        {
            if (stackCount <= 1) return baseValue;

            switch (stackingType)
            {
                case ItemStackingType.Linear:
                    return baseValue * stackCount;

                case ItemStackingType.Diminishing:
                    return baseValue + (baseValue * 0.5f * (stackCount - 1));

                default:
                    return baseValue * stackCount;
            }
        }

        private string FormatEffectText(StatModifierData modifier, float baseValue, float effectiveValue, int stackCount)
        {
            string statName = !string.IsNullOrEmpty(modifier.statDisplayName)
                ? modifier.statDisplayName
                : "Unknown Stat";

            string baseText = GetModifierDisplayText(modifier.modifierType, baseValue, statName);

            if (stackCount > 1)
            {
                string effectiveText = GetModifierDisplayText(modifier.modifierType, effectiveValue, statName);
                return $"• {baseText} → {effectiveText}";
            }
            else
            {
                return $"• {baseText}";
            }
        }

        private string GetModifierDisplayText(ModifierType type, float value, string statName)
        {
            switch (type)
            {
                case ModifierType.Flat:
                    return $"{(value >= 0 ? "+" : "")}{value:F1} {statName}";

                case ModifierType.PercentageAdditive:
                    float pct = value * 100f;
                    return $"{(pct >= 0 ? "+" : "")}{pct:F0}% {statName}";

                case ModifierType.Percentage:
                case ModifierType.PercentageMultiplicative:
                    float mult = (value - 1f) * 100f;
                    return $"{(mult >= 0 ? "+" : "")}{mult:F0}% {statName}";

                case ModifierType.Override:
                    return $"Set {statName} to {value:F1}";

                case ModifierType.Minimum:
                    return $"Min {statName}: {value:F1}";

                case ModifierType.Maximum:
                    return $"Max {statName}: {value:F1}";

                default:
                    return $"{value:F1} {statName}";
            }
        }

        private void UpdateTooltipStyling(ItemRarity rarity)
        {
            Color rarityColor = rarityColors.GetColor(rarity);

            // Update border
            if (rarityBorderImage != null)
            {
                rarityBorderImage.color = rarityColor;
            }

            // Update background with subtle rarity tint
            if (backgroundImage != null)
            {
                Color bgColor = Color.Lerp(Color.black, rarityColor, 0.1f);
                bgColor.a = 0.9f;
                backgroundImage.color = bgColor;
            }
        }

        private void ShowTooltip(Vector3 worldPosition)
        {
            if (isVisible) return;

            isVisible = true;
            tooltipPanel.SetActive(true);

            // Ensure it's rendered above all siblings
            tooltipPanel.transform.SetAsLastSibling();

            // Position and animate...
            UpdatePosition(worldPosition);
            StartCoroutine(FadeIn());
        }

        private void UpdatePosition(Vector3 screenPosition)
        {
            if (rectTransform == null) return;

            Vector2 screenPos = screenPosition;

            // Convert screen position to canvas position if needed
            if (parentCanvas.renderMode != RenderMode.ScreenSpaceOverlay)
            {
                RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    parentCanvas.transform as RectTransform,
                    screenPos,
                    uiCamera,
                    out Vector2 localPos);
                screenPos = localPos;
            }

            // Apply offset
            Vector2 targetPos = screenPos + offset;

            // Get tooltip size
            Vector2 tooltipSize = rectTransform.sizeDelta;

            // Prevent tooltip from going off screen
            Vector2 canvasSize = parentCanvas.GetComponent<RectTransform>().sizeDelta;

            // Check right edge
            if (targetPos.x + tooltipSize.x > canvasSize.x - edgeBuffer)
            {
                targetPos.x = screenPos.x - offset.x - tooltipSize.x;
            }

            // Check left edge
            if (targetPos.x < edgeBuffer)
            {
                targetPos.x = edgeBuffer;
            }

            // Check top edge
            if (targetPos.y + tooltipSize.y > canvasSize.y - edgeBuffer)
            {
                targetPos.y = screenPos.y - offset.y - tooltipSize.y;
            }

            // Check bottom edge
            if (targetPos.y < edgeBuffer)
            {
                targetPos.y = edgeBuffer;
            }

            // Apply position
            rectTransform.anchoredPosition = targetPos;
        }

        public void Hide(bool instant = false)
        {
            if (!isVisible) return;

            isVisible = false;

            if (instant)
            {
                canvasGroup.alpha = 0f;
                tooltipPanel.SetActive(false);
            }
            else
            {
                StartCoroutine(FadeOut());
            }
        }

        private System.Collections.IEnumerator FadeIn()
        {
            float elapsed = 0f;
            canvasGroup.alpha = 0f;

            while (elapsed < fadeInDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float progress = elapsed / fadeInDuration;
                canvasGroup.alpha = fadeInCurve.Evaluate(progress);
                yield return null;
            }

            canvasGroup.alpha = 1f;
        }

        private System.Collections.IEnumerator FadeOut()
        {
            float elapsed = 0f;
            float startAlpha = canvasGroup.alpha;

            while (elapsed < fadeInDuration * 0.5f) // Fade out faster
            {
                elapsed += Time.unscaledDeltaTime;
                float progress = elapsed / (fadeInDuration * 0.5f);
                canvasGroup.alpha = Mathf.Lerp(startAlpha, 0f, progress);
                yield return null;
            }

            canvasGroup.alpha = 0f;
            tooltipPanel.SetActive(false);
        }

        // Public API
        public bool IsVisible => isVisible;

        private void OnValidate()
        {
            if (fadeInDuration < 0.05f) fadeInDuration = 0.05f;
            if (edgeBuffer < 0f) edgeBuffer = 0f;
        }
    }
}