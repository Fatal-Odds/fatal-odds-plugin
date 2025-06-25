using UnityEngine;
using FatalOdds.Runtime;

namespace FatalOdds.Runtime
{
    [AddComponentMenu("Fatal Odds/Instant Pickup System")]
    public class InstantPickupSystem : MonoBehaviour
    {
        [Header("Detection Settings")]
        [SerializeField] private float pickupRange = 3f;
        [SerializeField] private LayerMask itemLayerMask = -1; // All layers by default
        [SerializeField] private string playerTag = "Player";
        [SerializeField] private float scanInterval = 0.1f; // How often to scan for items

        [Header("Player Reference")]
        [SerializeField] private Transform playerTransform;
        [SerializeField] private ModifierManager playerModifierManager;

        [Header("Notifications")]
        [SerializeField] private bool showPickupNotifications = true;
        [SerializeField] private bool showDebugLogs = true;
        [SerializeField] private PickupNotificationUI notificationUI; // Direct reference option

        [Header("Effects")]
        [SerializeField] private GameObject pickupEffect;
        [SerializeField] private AudioClip pickupSound;
        [SerializeField] private AudioSource audioSource;

        // Runtime state
        private float lastScanTime;

        void Start()
        {
            // Auto-find player if not assigned
            if (playerTransform == null)
            {
                FindPlayer();
            }

            // Auto-find modifier manager if not assigned
            if (playerModifierManager == null && playerTransform != null)
            {
                playerModifierManager = playerTransform.GetComponent<ModifierManager>();
            }

            // Auto-find notification UI if not assigned
            if (notificationUI == null)
            {
                notificationUI = FindObjectOfType<PickupNotificationUI>();
            }

            // Auto-find audio source
            if (audioSource == null)
            {
                audioSource = GetComponent<AudioSource>();
                if (audioSource == null)
                {
                    audioSource = gameObject.AddComponent<AudioSource>();
                    audioSource.playOnAwake = false;
                    audioSource.spatialBlend = 0f; // 2D sound for UI
                }
            }

            if (showDebugLogs)
                Debug.Log($"[InstantPickupSystem] Initialized. Player: {(playerTransform ? playerTransform.name : "Not Found")}");
        }

        void Update()
        {
            // Only scan periodically to avoid performance issues
            if (Time.time - lastScanTime < scanInterval) return;
            lastScanTime = Time.time;

            ScanForNearbyItems();
        }

        private void FindPlayer()
        {
            // Try to find by tag first
            GameObject playerObj = GameObject.FindGameObjectWithTag(playerTag);
            if (playerObj != null)
            {
                playerTransform = playerObj.transform;
                if (showDebugLogs)
                    Debug.Log($"[InstantPickupSystem] Found player by tag: {playerObj.name}");
                return;
            }

            // Fallback: find any object with ModifierManager (likely the player)
            ModifierManager[] managers = FindObjectsOfType<ModifierManager>();
            if (managers.Length > 0)
            {
                playerTransform = managers[0].transform;
                if (showDebugLogs)
                    Debug.Log($"[InstantPickupSystem] Found player by ModifierManager: {playerTransform.name}");
            }
        }

        private void ScanForNearbyItems()
        {
            if (playerTransform == null) return;

            // Find all ItemPickup components in range
            ItemPickup[] allPickups = FindObjectsOfType<ItemPickup>();

            foreach (ItemPickup pickup in allPickups)
            {
                if (pickup == null || pickup.gameObject == null) continue;

                // Check if item is within pickup range
                float distance = Vector3.Distance(playerTransform.position, pickup.transform.position);
                if (distance <= pickupRange)
                {
                    // Instantly collect this item
                    CollectItem(pickup);
                }
            }
        }

        private void CollectItem(ItemPickup pickup)
        {
            try
            {
                ItemDefinition itemDef = pickup.GetItemDefinition();
                int stackCount = pickup.GetStackCount();

                if (itemDef == null)
                {
                    if (showDebugLogs)
                        Debug.LogWarning($"[InstantPickupSystem] Item pickup has no definition: {pickup.gameObject.name}");
                    return;
                }

                // Apply item effects to player
                if (playerModifierManager != null)
                {
                    itemDef.ApplyToTarget(playerTransform.gameObject, stackCount);

                    if (showDebugLogs)
                        Debug.Log($"[InstantPickupSystem] Collected {itemDef.itemName} x{stackCount} - Effects applied!");
                }
                else
                {
                    if (showDebugLogs)
                        Debug.LogWarning("[InstantPickupSystem] No ModifierManager found on player - effects not applied");
                }

                // Show pickup notification
                if (showPickupNotifications)
                {
                    ShowPickupNotification(itemDef, stackCount);
                }

                // Play pickup effects
                PlayPickupEffects(pickup.transform.position);

                // Destroy the pickup object
                Destroy(pickup.gameObject);

                if (showDebugLogs)
                    Debug.Log($"[InstantPickupSystem] Successfully collected and destroyed {itemDef.itemName}");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[InstantPickupSystem] Error collecting item: {e.Message}");
            }
        }

        private void ShowPickupNotification(ItemDefinition itemDef, int stackCount)
        {
            // Try assigned notification UI first
            if (notificationUI != null)
            {
                notificationUI.ShowPickupNotification(itemDef, stackCount);
                return;
            }

            // Fallback: find notification system in scene
            PickupNotificationUI foundNotificationUI = FindObjectOfType<PickupNotificationUI>();
            if (foundNotificationUI != null)
            {
                foundNotificationUI.ShowPickupNotification(itemDef, stackCount);
                return;
            }

            // Last resort: create a simple console notification
            Debug.Log($"[PICKUP] +{stackCount}x {itemDef.itemName} ({itemDef.rarity})");
        }

        private void PlayPickupEffects(Vector3 position)
        {
            // Spawn pickup effect
            if (pickupEffect != null)
            {
                GameObject effect = Instantiate(pickupEffect, position, Quaternion.identity);
                Destroy(effect, 2f); // Auto-cleanup
            }

            // Play pickup sound
            if (pickupSound != null && audioSource != null)
            {
                audioSource.PlayOneShot(pickupSound);
            }
        }

        // Public methods for manual control
        public void SetPickupRange(float range)
        {
            pickupRange = Mathf.Max(0.1f, range);
        }

        public void SetScanInterval(float interval)
        {
            scanInterval = Mathf.Max(0.05f, interval);
        }

        public void SetPlayerReference(Transform player)
        {
            playerTransform = player;
            if (player != null)
            {
                playerModifierManager = player.GetComponent<ModifierManager>();
            }
        }

        // Set notification UI reference (useful for dependency injection)
        public void SetNotificationUI(PickupNotificationUI ui)
        {
            notificationUI = ui;
        }

        // Force scan for nearby items (useful for testing)
        [ContextMenu("Force Scan")]
        public void ForceScan()
        {
            ScanForNearbyItems();
        }

        // Debug visualization
        void OnDrawGizmosSelected()
        {
            if (playerTransform != null)
            {
                Gizmos.color = Color.green;
                Gizmos.DrawWireSphere(playerTransform.position, pickupRange);

                // Show detected items
                ItemPickup[] allPickups = FindObjectsOfType<ItemPickup>();
                foreach (ItemPickup pickup in allPickups)
                {
                    if (pickup == null) continue;

                    float distance = Vector3.Distance(playerTransform.position, pickup.transform.position);
                    if (distance <= pickupRange)
                    {
                        Gizmos.color = Color.yellow;
                        Gizmos.DrawLine(playerTransform.position, pickup.transform.position);
                        Gizmos.DrawWireCube(pickup.transform.position, Vector3.one * 0.5f);
                    }
                }
            }
            else
            {
                // Show pickup range at this object's position as fallback
                Gizmos.color = Color.red;
                Gizmos.DrawWireSphere(transform.position, pickupRange);
            }
        }
    }
}