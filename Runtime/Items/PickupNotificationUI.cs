using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

#if UNITY_TEXTMESHPRO
using TMPro;
#endif

namespace FatalOdds.Runtime
{
    public class PickupNotificationUI : MonoBehaviour
    {
        [Header("Notification Settings")]
        [SerializeField] private float notificationDuration = 2.5f;
        [SerializeField] private float fadeInTime = 0.3f;
        [SerializeField] private float fadeOutTime = 0.7f;
        [SerializeField] private int maxSimultaneousNotifications = 5;

        [Header("Positioning")]
        [SerializeField] private Vector2 startPosition = new Vector2(50f, -100f); // Top-left offset
        [SerializeField] private float notificationSpacing = 40f;
        [SerializeField] private bool stackFromTop = true;

        [Header("Animation")]
        [SerializeField] private AnimationType animationType = AnimationType.SlideAndFade;
        [SerializeField] private float slideDistance = 50f;

        [Header("Auto Setup")]
        [SerializeField] private bool autoCreateCanvas = true;

        [Header("Debug")]
        [SerializeField] private bool enableDebugLogs = true;

        // Runtime state
        private Canvas hudCanvas;
        private List<NotificationInstance> activeNotifications = new List<NotificationInstance>();
        private Queue<NotificationData> pendingNotifications = new Queue<NotificationData>();
        private bool isInitialized = false;

        public enum AnimationType
        {
            SlideAndFade,
            PopAndFade,
            FadeOnly
        }

        [System.Serializable]
        private class NotificationData
        {
            public string itemName;
            public int stackCount;
            public ItemRarity rarity;
            public Color rarityColor;

            public NotificationData(string name, int stack, ItemRarity itemRarity, Color color)
            {
                itemName = name;
                stackCount = stack;
                rarity = itemRarity;
                rarityColor = color;
            }
        }

        private class NotificationInstance
        {
            public GameObject notificationObject;
            public RectTransform rectTransform;
            public CanvasGroup canvasGroup;
            public NotificationData data;
            public float timeRemaining;
            public bool isFadingOut;

#if UNITY_TEXTMESHPRO
            public TextMeshProUGUI textComponent;
#else
            public Text textComponent;
#endif

            public Vector2 targetPosition;
            public Vector2 startPosition;
        }

        #region Unity Lifecycle

        void Awake()
        {
            // Try to initialize immediately
            if (autoCreateCanvas)
            {
                InitializeNotificationSystem();
            }
        }

        void Start()
        {
            // Ensure initialization happens
            if (!isInitialized)
            {
                InitializeNotificationSystem();
            }
        }

        void Update()
        {
            if (!isInitialized) return;

            UpdateActiveNotifications();
            ProcessPendingNotifications();
        }

        #endregion

        #region Initialization

        private void InitializeNotificationSystem()
        {
            if (isInitialized) return;

            try
            {
                // Find or create canvas
                hudCanvas = FindOrCreateCanvas();
                if (hudCanvas == null)
                {
                    Debug.LogError("[PickupNotificationUI] Failed to create or find canvas!");
                    return;
                }

                isInitialized = true;

                if (enableDebugLogs)
                    Debug.Log("[PickupNotificationUI] Successfully initialized");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[PickupNotificationUI] Initialization failed: {e.Message}");
            }
        }

        private Canvas FindOrCreateCanvas()
        {
            // Look for existing overlay canvas first
            Canvas[] canvases = FindObjectsOfType<Canvas>();
            foreach (var canvas in canvases)
            {
                if (canvas.renderMode == RenderMode.ScreenSpaceOverlay)
                {
                    if (enableDebugLogs)
                        Debug.Log($"[PickupNotificationUI] Using existing canvas: {canvas.name}");
                    return canvas;
                }
            }

            // Create new overlay canvas if none found
            if (autoCreateCanvas)
            {
                return CreateOverlayCanvas();
            }

            return null;
        }

        private Canvas CreateOverlayCanvas()
        {
            try
            {
                GameObject canvasObj = new GameObject("NotificationCanvas");
                Canvas canvas = canvasObj.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.sortingOrder = 1000; // Very high priority

                CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920, 1080);

                canvasObj.AddComponent<GraphicRaycaster>();

                if (enableDebugLogs)
                    Debug.Log("[PickupNotificationUI] Created notification canvas");

                return canvas;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[PickupNotificationUI] Failed to create canvas: {e.Message}");
                return null;
            }
        }

        #endregion

        #region Public API

        // Show pickup notification for an item
        public void ShowPickupNotification(string itemName, int stackCount = 1, ItemRarity rarity = ItemRarity.Common)
        {
            if (!isInitialized)
            {
                InitializeNotificationSystem();
                if (!isInitialized)
                {
                    Debug.LogWarning("[PickupNotificationUI] Failed to initialize - falling back to console log");
                    Debug.Log($"[PICKUP] +{stackCount}x {itemName} ({rarity})");
                    return;
                }
            }

            Color rarityColor = GetRarityColor(rarity);
            NotificationData data = new NotificationData(itemName, stackCount, rarity, rarityColor);

            // Add to queue to prevent overwhelming
            pendingNotifications.Enqueue(data);

            if (enableDebugLogs)
                Debug.Log($"[PickupNotificationUI] Queued notification: {itemName} x{stackCount} ({rarity})");
        }

        // Show pickup notification with item definition
        public void ShowPickupNotification(ItemDefinition item, int stackCount = 1)
        {
            if (item == null)
            {
                Debug.LogWarning("[PickupNotificationUI] Cannot show notification for null item");
                return;
            }
            ShowPickupNotification(item.itemName, stackCount, item.rarity);
        }

        // Clear all notifications immediately
        public void ClearAllNotifications()
        {
            foreach (var notification in activeNotifications)
            {
                if (notification?.notificationObject != null)
                {
                    Destroy(notification.notificationObject);
                }
            }
            activeNotifications.Clear();
            pendingNotifications.Clear();

            if (enableDebugLogs)
                Debug.Log("[PickupNotificationUI] Cleared all notifications");
        }

        #endregion

        #region Notification Management

        private void ProcessPendingNotifications()
        {
            // Only process if we have room for more notifications
            if (activeNotifications.Count >= maxSimultaneousNotifications || pendingNotifications.Count == 0)
                return;

            NotificationData data = pendingNotifications.Dequeue();
            CreateNotification(data);
        }

        private void CreateNotification(NotificationData data)
        {
            try
            {
                // Create notification object
                GameObject notificationObj = new GameObject($"Notification_{data.itemName}");
                notificationObj.transform.SetParent(hudCanvas.transform, false);

                // Setup RectTransform
                RectTransform rect = notificationObj.AddComponent<RectTransform>();
                rect.sizeDelta = new Vector2(300, 35);

                // Setup CanvasGroup for fading
                CanvasGroup canvasGroup = notificationObj.AddComponent<CanvasGroup>();
                canvasGroup.alpha = 0f;

                // Create background
                CreateNotificationBackground(notificationObj, data.rarityColor);

                // Create text component
                var textComponent = CreateNotificationText(notificationObj, data);

                // Calculate positions
                Vector2 targetPos = CalculateTargetPosition();
                Vector2 startPos = GetStartPosition(targetPos);

                // Position the notification
                rect.anchoredPosition = startPos;

                // Create notification instance
                NotificationInstance instance = new NotificationInstance
                {
                    notificationObject = notificationObj,
                    rectTransform = rect,
                    canvasGroup = canvasGroup,
                    textComponent = textComponent,
                    data = data,
                    timeRemaining = notificationDuration,
                    isFadingOut = false,
                    targetPosition = targetPos,
                    startPosition = startPos
                };

                activeNotifications.Add(instance);

                // Start animation
                StartCoroutine(AnimateNotification(instance));

                if (enableDebugLogs)
                    Debug.Log($"[PickupNotificationUI] Created notification for {data.itemName}");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[PickupNotificationUI] Error creating notification: {e.Message}");
            }
        }

        private void CreateNotificationBackground(GameObject parent, Color rarityColor)
        {
            Image background = parent.AddComponent<Image>();

            // Create a semi-transparent background with rarity color tint
            Color bgColor = Color.Lerp(Color.black, rarityColor, 0.3f);
            bgColor.a = 0.8f;
            background.color = bgColor;

            // Add subtle border effect
            Outline outline = parent.AddComponent<Outline>();
            outline.effectColor = rarityColor;
            outline.effectDistance = new Vector2(1, 1);
        }

#if UNITY_TEXTMESHPRO
        private TextMeshProUGUI CreateNotificationText(GameObject parent, NotificationData data)
        {
            TextMeshProUGUI textComponent = parent.AddComponent<TextMeshProUGUI>();

            // Configure text
            string displayText = data.stackCount > 1 ? $"+{data.stackCount}x {data.itemName}" : $"+{data.itemName}";
            textComponent.text = displayText;
            textComponent.fontSize = 16;
            textComponent.fontStyle = FontStyles.Bold;
            textComponent.color = data.rarityColor;
            textComponent.alignment = TextAlignmentOptions.Center;

            return textComponent;
        }
#else
        private Text CreateNotificationText(GameObject parent, NotificationData data)
        {
            Text textComponent = parent.AddComponent<Text>();

            // Configure text
            string displayText = data.stackCount > 1 ? $"+{data.stackCount}x {data.itemName}" : $"+{data.itemName}";
            textComponent.text = displayText;
            textComponent.fontSize = 16;
            textComponent.fontStyle = FontStyle.Bold;
            textComponent.color = data.rarityColor;
            textComponent.alignment = TextAnchor.MiddleCenter;
            textComponent.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            // Add shadow for better readability
            Shadow shadow = parent.AddComponent<Shadow>();
            shadow.effectColor = Color.black;
            shadow.effectDistance = new Vector2(1, -1);

            return textComponent;
        }
#endif

        private Vector2 CalculateTargetPosition()
        {
            float yOffset = startPosition.y;
            if (stackFromTop)
            {
                yOffset -= activeNotifications.Count * notificationSpacing;
            }
            else
            {
                yOffset += activeNotifications.Count * notificationSpacing;
            }

            return new Vector2(startPosition.x, yOffset);
        }

        private Vector2 GetStartPosition(Vector2 targetPos)
        {
            switch (animationType)
            {
                case AnimationType.SlideAndFade:
                    return new Vector2(targetPos.x - slideDistance, targetPos.y);
                case AnimationType.PopAndFade:
                    return targetPos; // Start at target for pop effect
                case AnimationType.FadeOnly:
                default:
                    return targetPos;
            }
        }

        #endregion

        #region Animation

        private IEnumerator AnimateNotification(NotificationInstance instance)
        {
            // Fade in and slide in
            yield return StartCoroutine(AnimateIn(instance));

            // Wait for display duration
            while (instance.timeRemaining > fadeOutTime && !instance.isFadingOut)
            {
                instance.timeRemaining -= Time.unscaledDeltaTime;
                yield return null;
            }

            // Fade out
            instance.isFadingOut = true;
            yield return StartCoroutine(AnimateOut(instance));

            // Remove from active list and destroy
            activeNotifications.Remove(instance);
            if (instance.notificationObject != null)
            {
                Destroy(instance.notificationObject);
            }
        }

        private IEnumerator AnimateIn(NotificationInstance instance)
        {
            float elapsed = 0f;
            Vector2 startPos = instance.startPosition;
            Vector2 targetPos = instance.targetPosition;

            while (elapsed < fadeInTime)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = elapsed / fadeInTime;

                // Smooth easing
                float easeT = Mathf.SmoothStep(0f, 1f, t);

                // Animate position
                if (animationType == AnimationType.SlideAndFade)
                {
                    instance.rectTransform.anchoredPosition = Vector2.Lerp(startPos, targetPos, easeT);
                }
                else if (animationType == AnimationType.PopAndFade)
                {
                    // Pop effect with scale
                    float scale = Mathf.Lerp(0f, 1f, easeT);
                    instance.rectTransform.localScale = Vector3.one * scale;
                }

                // Animate alpha
                instance.canvasGroup.alpha = Mathf.Lerp(0f, 1f, easeT);

                yield return null;
            }

            // Ensure final values
            instance.rectTransform.anchoredPosition = targetPos;
            instance.rectTransform.localScale = Vector3.one;
            instance.canvasGroup.alpha = 1f;
        }

        private IEnumerator AnimateOut(NotificationInstance instance)
        {
            float elapsed = 0f;
            float startAlpha = instance.canvasGroup.alpha;
            Vector2 startPos = instance.rectTransform.anchoredPosition;

            while (elapsed < fadeOutTime)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = elapsed / fadeOutTime;

                // Smooth easing
                float easeT = Mathf.SmoothStep(0f, 1f, t);

                // Animate alpha
                instance.canvasGroup.alpha = Mathf.Lerp(startAlpha, 0f, easeT);

                // Optional slide out
                if (animationType == AnimationType.SlideAndFade)
                {
                    Vector2 slideOut = startPos + Vector2.right * (slideDistance * easeT);
                    instance.rectTransform.anchoredPosition = slideOut;
                }
                else if (animationType == AnimationType.PopAndFade)
                {
                    // Shrink on exit
                    float scale = Mathf.Lerp(1f, 0f, easeT);
                    instance.rectTransform.localScale = Vector3.one * scale;
                }

                yield return null;
            }

            instance.canvasGroup.alpha = 0f;
        }

        #endregion

        #region Update Logic

        private void UpdateActiveNotifications()
        {
            for (int i = activeNotifications.Count - 1; i >= 0; i--)
            {
                var notification = activeNotifications[i];

                if (notification == null || notification.notificationObject == null)
                {
                    activeNotifications.RemoveAt(i);
                    continue;
                }

                // Update remaining time
                if (!notification.isFadingOut)
                {
                    notification.timeRemaining -= Time.unscaledDeltaTime;
                }
            }

            // Reposition notifications to fill gaps
            RepositionNotifications();
        }

        private void RepositionNotifications()
        {
            for (int i = 0; i < activeNotifications.Count; i++)
            {
                var notification = activeNotifications[i];
                if (notification?.rectTransform == null) continue;

                // Calculate new target position based on current index
                float yOffset = startPosition.y;
                if (stackFromTop)
                {
                    yOffset -= i * notificationSpacing;
                }
                else
                {
                    yOffset += i * notificationSpacing;
                }

                Vector2 newTargetPos = new Vector2(startPosition.x, yOffset);
                notification.targetPosition = newTargetPos;

                // Smoothly move to new position
                Vector2 currentPos = notification.rectTransform.anchoredPosition;
                Vector2 lerpedPos = Vector2.Lerp(currentPos, newTargetPos, Time.unscaledDeltaTime * 8f);
                notification.rectTransform.anchoredPosition = lerpedPos;
            }
        }

        #endregion

        #region Utility

        private Color GetRarityColor(ItemRarity rarity)
        {
            switch (rarity)
            {
                case ItemRarity.Common: return new Color(0.85f, 0.85f, 0.85f, 1f);
                case ItemRarity.Uncommon: return new Color(0.4f, 0.8f, 0.4f, 1f);
                case ItemRarity.Rare: return new Color(0.3f, 0.5f, 0.9f, 1f);
                case ItemRarity.Epic: return new Color(0.7f, 0.4f, 0.9f, 1f);
                case ItemRarity.Legendary: return new Color(1f, 0.8f, 0.3f, 1f);
                case ItemRarity.Artifact: return new Color(0.8f, 0.3f, 0.3f, 1f);
                default: return Color.white;
            }
        }

        #endregion
    }
}