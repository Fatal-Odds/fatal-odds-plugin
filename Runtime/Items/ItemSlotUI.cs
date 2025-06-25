using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

namespace FatalOdds.Runtime
{
    public sealed class ItemSlotUI : MonoBehaviour,
                                     IPointerEnterHandler,
                                     IPointerExitHandler
    {
        /* ─── Inspector References ─── */

        [Header("UI References")]
        [SerializeField] private Image itemIconImage;
        [SerializeField] private Image backgroundImage;
        [SerializeField] private Image rarityBorderImage;

        [SerializeField] private TextMeshProUGUI stackCountText;
        [SerializeField] private TextMeshProUGUI itemNameText;

        [SerializeField] private GameObject stackCountBackground;

        [Header("Rarity Colours")]
        [SerializeField] private RarityColorSet rarityColours;

        [Header("Auto-setup")]
        [SerializeField] private bool createMissingComponents = true;

        /* ─── Runtime State ─── */

        private string itemName;
        private int stackCount;
        private ItemDefinition itemDefinition;
        private ItemInventoryUI parentInventory;
        private bool isHovered;

        /* ─── Support Types ─── */

        [System.Serializable]
        public class RarityColorSet
        {
            public Color common = new(0.85f, 0.85f, 0.85f, 1f);
            public Color uncommon = new(0.40f, 0.80f, 0.40f, 1f);
            public Color rare = new(0.30f, 0.50f, 0.90f, 1f);
            public Color epic = new(0.70f, 0.40f, 0.90f, 1f);
            public Color legendary = new(1.00f, 0.80f, 0.30f, 1f);
            public Color artifact = new(0.80f, 0.30f, 0.30f, 1f);

            public Color GetColor(ItemRarity rarity)
            {
                return rarity switch
                {
                    ItemRarity.Uncommon => uncommon,
                    ItemRarity.Rare => rare,
                    ItemRarity.Epic => epic,
                    ItemRarity.Legendary => legendary,
                    ItemRarity.Artifact => artifact,
                    _ => common
                };
            }
        }

        /* ─── Unity Lifecycle ─── */

        private void Awake()
        {
            EnsureRarityColours();

            if (createMissingComponents)
            {
                SetupMissingComponents();
            }
        }

        private void OnValidate()
        {
            EnsureRarityColours();
        }

        /* ─── Public API ─── */

        public void Setup(string itemName,
                          int stackCount,
                          ItemDefinition definition,
                          ItemInventoryUI inventory)
        {
            if (string.IsNullOrEmpty(itemName) || stackCount <= 0)
            {
                Debug.LogError("[ItemSlotUI] Invalid setup data.");
                return;
            }

            this.itemName = itemName;
            this.stackCount = stackCount;
            itemDefinition = definition;
            parentInventory = inventory;

            if (createMissingComponents &&
                (itemNameText == null || stackCountText == null))
            {
                SetupMissingComponents();
            }

            RefreshDisplay();
        }

        // Call when stack count changes externally
        public void UpdateStackCount(int newCount)
        {
            stackCount = newCount;
            UpdateStackCountVisual();

            if (isHovered && parentInventory != null)
            {
                parentInventory.ShowTooltip(itemName, stackCount, Input.mousePosition);
            }
        }

        /* ─── Pointer Events ─── */

        public void OnPointerEnter(PointerEventData eventData)
        {
            isHovered = true;

            parentInventory?.ShowTooltip(itemName, stackCount, Input.mousePosition);
            AddHoverEffect();
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            isHovered = false;

            parentInventory?.HideTooltip();
            RemoveHoverEffect();
        }

        /* ─── Internal Helpers ─── */

        private void EnsureRarityColours()
        {
            rarityColours ??= new RarityColorSet();
        }

        private void SetupMissingComponents()
        {
            // Background (also the raycast target)
            if (backgroundImage == null)
            {
                backgroundImage = gameObject.GetComponent<Image>() ??
                                  gameObject.AddComponent<Image>();
            }

            backgroundImage.color = new Color(0.18f, 0.18f, 0.18f, 0.90f);
            backgroundImage.raycastTarget = true;

            // Name text (top centre)
            if (itemNameText == null)
            {
                itemNameText = CreateText("ItemNameText",
                                          fontSize: 10,
                                          anchorMin: new Vector2(0.05f, 0.65f),
                                          anchorMax: new Vector2(0.95f, 0.95f));
            }

            // Stack text (bottom-right)
            if (stackCountText == null)
            {
                stackCountText = CreateText("StackCountText",
                                            fontSize: 12,
                                            anchorMin: new Vector2(0.60f, 0.05f),
                                            anchorMax: new Vector2(0.95f, 0.35f));
            }

            // Stack background
            if (stackCountBackground == null && stackCountText != null)
            {
                var bg = new GameObject("StackBG", typeof(RectTransform), typeof(Image));
                bg.transform.SetParent(transform, false);

                var rt = bg.GetComponent<RectTransform>();
                rt.anchorMin = new Vector2(0.60f, 0.05f);
                rt.anchorMax = new Vector2(0.95f, 0.35f);
                rt.sizeDelta = Vector2.zero;

                bg.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.70f);
                stackCountBackground = bg;

                stackCountText.transform.SetAsLastSibling();
            }

            // Rarity border
            if (rarityBorderImage == null)
            {
                var border = new GameObject("RarityBorder",
                                            typeof(RectTransform),
                                            typeof(Image),
                                            typeof(Outline));

                border.transform.SetParent(transform, false);

                var rt = border.GetComponent<RectTransform>();
                rt.anchorMin = Vector2.zero;
                rt.anchorMax = Vector2.one;
                rt.sizeDelta = Vector2.zero;

                rarityBorderImage = border.GetComponent<Image>();
                rarityBorderImage.color = Color.clear;
                border.GetComponent<Outline>().effectDistance = new Vector2(2f, 2f);
                border.transform.SetAsFirstSibling();
            }
        }

        private TextMeshProUGUI CreateText(string name,
                                           int fontSize,
                                           Vector2 anchorMin,
                                           Vector2 anchorMax)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            go.transform.SetParent(transform, false);

            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.sizeDelta = Vector2.zero;

            var txt = go.GetComponent<TextMeshProUGUI>();
            txt.text = "";
            txt.fontSize = fontSize;
            txt.fontStyle = FontStyles.Bold;
            txt.color = Color.white;
            txt.alignment = TextAlignmentOptions.Center;
            txt.enableWordWrapping = true;

            txt.enableAutoSizing = true;   // let TMP shrink the font if needed
            txt.fontSizeMin = 8;      // lower bound – tweak to taste
            txt.fontSizeMax = fontSize;

            return txt;
        }

        /* ─── Visual refresh ─── */

        private void RefreshDisplay()
        {
            gameObject.name = $"ItemSlot_{itemName}";
            UpdateNameText();
            UpdateStackCountVisual();
            UpdateRarityVisuals();
            UpdateIcon();
        }

        private void UpdateNameText()
        {
            if (itemNameText != null)
            {
                itemNameText.text = FormatItemName(itemName);
            }
        }

        private void UpdateStackCountVisual()
        {
            if (stackCountText == null)
            {
                return;
            }

            bool showCount = stackCount > 1;
            stackCountText.gameObject.SetActive(showCount);

            if (stackCountBackground != null)
            {
                stackCountBackground.SetActive(showCount);
            }

            if (showCount)
            {
                stackCountText.text = stackCount.ToString();
            }
        }

        private void UpdateRarityVisuals()
        {
            EnsureRarityColours();

            ItemRarity rarity = itemDefinition?.rarity ?? ItemRarity.Common;
            Color rarityColor = rarityColours.GetColor(rarity);

            if (rarityBorderImage != null)
            {
                rarityBorderImage.color = rarityColor;
            }

            if (itemNameText != null)
            {
                itemNameText.color = Color.white;
            }

            if (backgroundImage != null)
            {
                Color bg = Color.Lerp(new Color(0.18f, 0.18f, 0.18f, 0.90f),
                                      rarityColor, 0.20f);
                bg.a = 0.90f;
                backgroundImage.color = bg;
            }

            AddRarityOutline(rarity, rarityColor);
        }

        private void AddRarityOutline(ItemRarity rarity, Color rarityColor)
        {
            if ((int)rarity < (int)ItemRarity.Rare)
            {
                return;
            }

            var outline = gameObject.GetComponent<Outline>() ??
                          gameObject.AddComponent<Outline>();

            outline.effectColor = rarityColor;
            outline.effectDistance = Vector2.one * (1f + ((int)rarity - 2) * 0.5f);
        }

        private void UpdateIcon()
        {
            if (itemIconImage == null || itemDefinition == null)
            {
                return;
            }

            if (itemDefinition.icon != null)
            {
                itemIconImage.sprite = Sprite.Create(itemDefinition.icon,
                                                     new Rect(0, 0,
                                                              itemDefinition.icon.width,
                                                              itemDefinition.icon.height),
                                                     new Vector2(0.5f, 0.5f));
            }
            else
            {
                itemIconImage.sprite = GetDefaultIcon();
                itemIconImage.color = rarityColours.GetColor(itemDefinition.rarity);
            }
        }

        private static Sprite GetDefaultIcon()
        {
            return Resources.GetBuiltinResource<Sprite>("UI/Skin/UISprite.psd");
        }

        /* ─── Hover visuals ─── */

        private void AddHoverEffect()
        {
            transform.localScale = Vector3.one * 1.10f;

            if (backgroundImage != null)
            {
                Color c = backgroundImage.color;
                backgroundImage.color = Color.Lerp(c, Color.white, 0.30f);
            }
        }

        private static string FormatItemName(string raw)
        {
            return string.IsNullOrEmpty(raw) ? "" : raw.Replace('_', ' ');
        }

        private void RemoveHoverEffect()
        {
            transform.localScale = Vector3.one;
            UpdateRarityVisuals();
        }

        /* ─── Public Read-only Props ─── */

        public string ItemName => itemName;
        public int StackCount => stackCount;
        public ItemDefinition Definition => itemDefinition;
        public bool IsHovered => isHovered;
    }
}
