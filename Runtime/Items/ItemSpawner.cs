using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using FatalOdds.Runtime;

namespace FatalOdds.Runtime
{
    public class ItemSpawner : MonoBehaviour
    {
        [Header("Universal Prefab Configuration")]
        [SerializeField] private GameObject universalPickupPrefab;
        [SerializeField] private bool autoFindPrefab = true;

        [Header("Spawning Configuration")]
        [SerializeField] private Transform spawnPoint;
        [SerializeField] private float spawnRadius = 5f;
        [SerializeField] private bool spawnOnStart = false;
        [SerializeField] private float autoSpawnInterval = 0f; // 0 = disabled

        [Header("Rarity Weights")]
        [SerializeField] private float commonWeight = 50f;
        [SerializeField] private float uncommonWeight = 30f;
        [SerializeField] private float rareWeight = 15f;
        [SerializeField] private float epicWeight = 4f;
        [SerializeField] private float legendaryWeight = 0.9f;
        [SerializeField] private float artifactWeight = 0.1f;

        [Header("Testing Controls")]
        [SerializeField] private bool enableDebugKeys = true;

        // Static cache for all spawners to share
        private static GameObject cachedUniversalPrefab;
        private static List<ItemDefinition> cachedItemDefinitions;
        private static Dictionary<ItemRarity, List<ItemDefinition>> itemsByRarity;
        private static bool isCacheInitialized = false;

        // Instance data
        private float[] rarityWeights;
        private float totalWeight;

        #region Unity Lifecycle

        void Start()
        {
            InitializeSpawner();

            if (spawnOnStart)
            {
                SpawnRandomItem();
            }

            if (autoSpawnInterval > 0)
            {
                InvokeRepeating(nameof(SpawnRandomItem), autoSpawnInterval, autoSpawnInterval);
            }
        }

        void Update()
        {
            if (enableDebugKeys)
            {
                HandleDebugInput();
            }
        }

        #endregion

        #region Initialization

        private void InitializeSpawner()
        {
            // Initialize static cache if needed
            if (!isCacheInitialized)
            {
                InitializeStaticCache();
            }

            // Set up this instance's universal prefab
            if (autoFindPrefab && universalPickupPrefab == null)
            {
                universalPickupPrefab = GetUniversalPrefab();
            }

            // Set up rarity weights
            SetupRarityWeights();

            Debug.Log($"[UnifiedItemSpawner] Initialized with {GetAllItems().Count} items and universal prefab: {(universalPickupPrefab != null ? "Found" : "Missing")}");
        }

        private static void InitializeStaticCache()
        {
            if (isCacheInitialized) return;

            cachedItemDefinitions = new List<ItemDefinition>();
            itemsByRarity = new Dictionary<ItemRarity, List<ItemDefinition>>();

            // Initialize rarity lists
            foreach (ItemRarity rarity in System.Enum.GetValues(typeof(ItemRarity)))
            {
                itemsByRarity[rarity] = new List<ItemDefinition>();
            }

            LoadAllItemDefinitions();
            LoadUniversalPrefab();

            isCacheInitialized = true;
        }

        private static void LoadAllItemDefinitions()
        {
#if UNITY_EDITOR
            // In editor, use AssetDatabase
            string[] guids = UnityEditor.AssetDatabase.FindAssets("t:ItemDefinition");
            foreach (string guid in guids)
            {
                string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
                ItemDefinition item = UnityEditor.AssetDatabase.LoadAssetAtPath<ItemDefinition>(path);
                if (item != null)
                {
                    cachedItemDefinitions.Add(item);
                    itemsByRarity[item.rarity].Add(item);
                }
            }
#else
            // In build, load from Resources
            ItemDefinition[] items = Resources.LoadAll<ItemDefinition>("");
            foreach (var item in items)
            {
                cachedItemDefinitions.Add(item);
                itemsByRarity[item.rarity].Add(item);
            }
#endif

            Debug.Log($"[UnifiedItemSpawner] Loaded {cachedItemDefinitions.Count} item definitions");

            // Log distribution
            foreach (var kvp in itemsByRarity)
            {
                if (kvp.Value.Count > 0)
                    Debug.Log($"  {kvp.Key}: {kvp.Value.Count} items");
            }
        }

        private static void LoadUniversalPrefab()
        {
            if (cachedUniversalPrefab != null) return;

            // Try different possible paths for the universal prefab
            string[] possiblePaths = {
                "Assets/FatalOdds/Prefabs/UniversalItemPickup.prefab",
                "Assets/FatalOdds/Generated/Prefabs/UniversalItemPickup.prefab",
                "UniversalItemPickup" // Resources folder
            };

            foreach (string path in possiblePaths)
            {
                if (path.StartsWith("Assets/"))
                {
#if UNITY_EDITOR
                    cachedUniversalPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(path);
#endif
                }
                else
                {
                    cachedUniversalPrefab = Resources.Load<GameObject>(path);
                }

                if (cachedUniversalPrefab != null)
                {
                    Debug.Log($"[UnifiedItemSpawner] Loaded universal prefab from: {path}");
                    break;
                }
            }

            if (cachedUniversalPrefab == null)
            {
                Debug.LogWarning("[UnifiedItemSpawner] Universal pickup prefab not found! Create one using 'Window/Fatal Odds/Create Universal Prefab'");
            }
        }

        private void SetupRarityWeights()
        {
            rarityWeights = new float[]
            {
                commonWeight,      // Common
                uncommonWeight,    // Uncommon
                rareWeight,        // Rare
                epicWeight,        // Epic
                legendaryWeight,   // Legendary
                artifactWeight     // Artifact
            };

            totalWeight = rarityWeights.Sum();
        }

        #endregion

        #region Public Instance Methods

        // Spawn a random item using this spawner's configuration
        [ContextMenu("Spawn Random Item")]
        public void SpawnRandomItem()
        {
            ItemRarity selectedRarity = SelectRandomRarity();
            SpawnItemOfRarity(selectedRarity);
        }

        // Spawn an item of specific rarity using this spawner's configuration
        public void SpawnItemOfRarity(ItemRarity rarity)
        {
            Vector3 spawnPosition = GetRandomSpawnPosition();
            SpawnItemOfRarity(rarity, spawnPosition, GetRandomRotation(), transform);
        }

        // Spawn a specific item using this spawner's configuration
        public void SpawnSpecificItem(ItemDefinition item, int stackCount = 1)
        {
            Vector3 spawnPosition = GetRandomSpawnPosition();
            SpawnSpecificItem(item, spawnPosition, stackCount, GetRandomRotation(), transform);
        }


        /// Clear all existing pickups in the scene

        [ContextMenu("Clear All Pickups")]
        public static void ClearAllPickups()
        {
            ItemPickup[] existingPickups = FindObjectsOfType<ItemPickup>();

            foreach (var pickup in existingPickups)
            {
                if (Application.isPlaying)
                {
                    Destroy(pickup.gameObject);
                }
                else
                {
                    DestroyImmediate(pickup.gameObject);
                }
            }

            Debug.Log($"[UnifiedItemSpawner] Cleared {existingPickups.Length} pickups");
        }


        /// Spawn one item of each rarity for testing

        [ContextMenu("Spawn All Rarities")]
        public void SpawnAllRarities()
        {
            Vector3 currentPos = GetSpawnPoint();
            float spacing = 2.5f;

            foreach (ItemRarity rarity in System.Enum.GetValues(typeof(ItemRarity)))
            {
                SpawnItemOfRarity(rarity, currentPos, Quaternion.identity, transform);
                currentPos.x += spacing;
            }
        }

         
        /// Spawn multiple random items at once
        
        [ContextMenu("Spawn Multiple Random Items")]
        public void SpawnMultipleRandomItems()
        {
            int count = Random.Range(3, 8);
            for (int i = 0; i < count; i++)
            {
                SpawnRandomItem();
            }
            Debug.Log($"[UnifiedItemSpawner] Spawned {count} random items");
        }

        #endregion

        #region Static Universal Spawning Methods

        
        /// MAIN METHOD: Spawn any item anywhere using the universal prefab system
        
        public static GameObject SpawnUniversalItem(ItemDefinition item, Vector3 position, int stackCount = 1, Quaternion rotation = default, Transform parent = null)
        {
            if (item == null)
            {
                Debug.LogError("[UnifiedItemSpawner] Cannot spawn null item!");
                return null;
            }

            // Ensure we have the universal prefab
            GameObject prefabToUse = GetUniversalPrefab();
            if (prefabToUse == null)
            {
                Debug.LogError("[UnifiedItemSpawner] No universal pickup prefab available! Create one using 'Window/Fatal Odds/Create Universal Prefab'");
                return null;
            }

            // Use identity rotation if none specified
            if (rotation == default)
                rotation = Quaternion.Euler(0, Random.Range(0f, 360f), 0);

            // Instantiate the universal pickup prefab
            GameObject pickup = Object.Instantiate(prefabToUse, position, rotation, parent);

            // Get the UniversalItemPickup component and initialize it with the item
            ItemPickup pickupComponent = pickup.GetComponent<ItemPickup>();
            if (pickupComponent != null)
            {
                pickupComponent.Initialize(item, stackCount);
            }
            else
            {
                Debug.LogError("[UnifiedItemSpawner] Universal pickup prefab doesn't have UniversalItemPickup component!");
                Object.Destroy(pickup);
                return null;
            }

            // Set appropriate name for debugging
            pickup.name = $"Universal_{item.itemName}";

            Debug.Log($"[UnifiedItemSpawner] Spawned {item.itemName} (x{stackCount}) at {position}");
            return pickup;
        }


        // Spawn a random item of the specified rarity
        public static GameObject SpawnItemOfRarity(ItemRarity rarity, Vector3 position, Quaternion rotation = default, Transform parent = null)
        {
            var item = GetRandomItemOfRarity(rarity);
            if (item == null)
            {
                Debug.LogWarning($"[UnifiedItemSpawner] No items of rarity {rarity} found!");
                return null;
            }

            return SpawnUniversalItem(item, position, 1, rotation, parent);
        }

        // Spawn a specific item at a position
        public static GameObject SpawnSpecificItem(ItemDefinition item, Vector3 position, int stackCount = 1, Quaternion rotation = default, Transform parent = null)
        {
            return SpawnUniversalItem(item, position, stackCount, rotation, parent);
        }

        // Spawn multiple items in a circle pattern
        public static List<GameObject> SpawnItemCircle(List<ItemDefinition> items, Vector3 centerPosition, float radius = 3f, Transform parent = null)
        {
            List<GameObject> spawnedItems = new List<GameObject>();

            if (items == null || items.Count == 0) return spawnedItems;

            float angleStep = 360f / items.Count;

            for (int i = 0; i < items.Count; i++)
            {
                float angle = i * angleStep * Mathf.Deg2Rad;
                Vector3 offset = new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle)) * radius;
                Vector3 spawnPosition = centerPosition + offset;

                GameObject pickup = SpawnUniversalItem(items[i], spawnPosition, 1, default, parent);
                if (pickup != null)
                {
                    spawnedItems.Add(pickup);
                }
            }

            return spawnedItems;
        }

        // Spawn items in an explosion pattern (like from a treasure chest)
        public static List<GameObject> SpawnItemExplosion(List<ItemDefinition> items, Vector3 centerPosition, float force = 5f, Transform parent = null)
        {
            List<GameObject> spawnedItems = new List<GameObject>();

            if (items == null || items.Count == 0) return spawnedItems;

            foreach (var item in items)
            {
                Vector3 randomDirection = Random.insideUnitSphere;
                randomDirection.y = Mathf.Abs(randomDirection.y); // Keep items above ground
                Vector3 spawnPosition = centerPosition + randomDirection * Random.Range(0.5f, 2f);

                GameObject pickup = SpawnUniversalItem(item, spawnPosition, 1, default, parent);
                if (pickup != null)
                {
                    spawnedItems.Add(pickup);

                    // Add physics force for dramatic effect
                    Rigidbody rb = pickup.GetComponent<Rigidbody>();
                    if (rb == null)
                    {
                        rb = pickup.AddComponent<Rigidbody>();
                        rb.drag = 2f; // So items settle quickly
                    }

                    Vector3 explosionForce = randomDirection.normalized * force;
                    explosionForce.y = Mathf.Max(explosionForce.y, 2f); // Minimum upward force
                    rb.AddForce(explosionForce, ForceMode.Impulse);
                }
            }

            return spawnedItems;
        }

        #endregion

        #region Static Data Access Methods

        // Get all available items
        public static List<ItemDefinition> GetAllItems()
        {
            if (!isCacheInitialized) InitializeStaticCache();
            return new List<ItemDefinition>(cachedItemDefinitions ?? new List<ItemDefinition>());
        }

        // Get items by rarity
        public static List<ItemDefinition> GetItemsByRarity(ItemRarity rarity)
        {
            if (!isCacheInitialized) InitializeStaticCache();

            if (itemsByRarity != null && itemsByRarity.TryGetValue(rarity, out List<ItemDefinition> items))
            {
                return new List<ItemDefinition>(items);
            }

            return new List<ItemDefinition>();
        }

        // Get a random item of the specified rarity
        public static ItemDefinition GetRandomItemOfRarity(ItemRarity rarity)
        {
            var items = GetItemsByRarity(rarity);
            return items.Count > 0 ? items[Random.Range(0, items.Count)] : null;
        }

        // Get the universal prefab (loads it if not cached)
        public static GameObject GetUniversalPrefab()
        {
            if (cachedUniversalPrefab == null)
            {
                LoadUniversalPrefab();
            }
            return cachedUniversalPrefab;
        }

        // Refresh the cached item definitions (call when new items are created)
        public static void RefreshItemCache()
        {
            isCacheInitialized = false;
            cachedItemDefinitions = null;
            itemsByRarity = null;
            InitializeStaticCache();
        }

        #endregion

        #region Utility Methods

        // Check if a position is safe for spawning (not inside colliders)
        public static bool IsPositionSafe(Vector3 position, float checkRadius = 0.5f)
        {
            Collider[] overlapping = Physics.OverlapSphere(position, checkRadius);

            // Filter out triggers (they're usually fine to spawn in)
            foreach (var collider in overlapping)
            {
                if (!collider.isTrigger)
                {
                    return false;
                }
            }

            return true;
        }

        // Find a safe position near the target position
        public static Vector3 FindSafePosition(Vector3 targetPosition, float searchRadius = 5f, int maxAttempts = 10)
        {
            if (IsPositionSafe(targetPosition))
            {
                return targetPosition;
            }

            for (int i = 0; i < maxAttempts; i++)
            {
                Vector3 randomOffset = Random.insideUnitSphere * searchRadius;
                randomOffset.y = 0; // Keep on ground level
                Vector3 testPosition = targetPosition + randomOffset;

                if (IsPositionSafe(testPosition))
                {
                    return testPosition;
                }
            }

            // If we can't find a safe position, return the original
            Debug.LogWarning($"[UnifiedItemSpawner] Could not find safe position near {targetPosition}");
            return targetPosition;
        }

        #endregion

        #region Private Instance Methods

        private void HandleDebugInput()
        {
            // Spacebar to spawn random item
            if (Input.GetKeyDown(KeyCode.Space))
            {
                SpawnRandomItem();
            }

            // Number keys to spawn specific rarities
            if (Input.GetKeyDown(KeyCode.Alpha1)) SpawnItemOfRarity(ItemRarity.Common);
            if (Input.GetKeyDown(KeyCode.Alpha2)) SpawnItemOfRarity(ItemRarity.Uncommon);
            if (Input.GetKeyDown(KeyCode.Alpha3)) SpawnItemOfRarity(ItemRarity.Rare);
            if (Input.GetKeyDown(KeyCode.Alpha4)) SpawnItemOfRarity(ItemRarity.Epic);
            if (Input.GetKeyDown(KeyCode.Alpha5)) SpawnItemOfRarity(ItemRarity.Legendary);
            if (Input.GetKeyDown(KeyCode.Alpha6)) SpawnItemOfRarity(ItemRarity.Artifact);

            // C to clear all pickups
            if (Input.GetKeyDown(KeyCode.C))
            {
                ClearAllPickups();
            }
        }

        private ItemRarity SelectRandomRarity()
        {
            float randomValue = Random.Range(0f, totalWeight);
            float currentWeight = 0f;

            for (int i = 0; i < rarityWeights.Length; i++)
            {
                currentWeight += rarityWeights[i];
                if (randomValue <= currentWeight)
                {
                    return (ItemRarity)i;
                }
            }

            return ItemRarity.Common; // Fallback
        }

        private Vector3 GetRandomSpawnPosition()
        {
            Vector3 basePosition = GetSpawnPoint();
            Vector3 randomOffset = Random.insideUnitSphere * spawnRadius;
            randomOffset.y = Mathf.Abs(randomOffset.y); // Keep items above ground
            return basePosition + randomOffset;
        }

        private Vector3 GetSpawnPoint()
        {
            return spawnPoint != null ? spawnPoint.position : transform.position;
        }

        private Quaternion GetRandomRotation()
        {
            return Quaternion.Euler(0, Random.Range(0f, 360f), 0);
        }

        #endregion

        #region Simple Convenience Methods

        // Simple convenience methods for common spawning patterns
        public static GameObject SpawnCommonItem(Vector3 position) => SpawnItemOfRarity(ItemRarity.Common, position);
        public static GameObject SpawnUncommonItem(Vector3 position) => SpawnItemOfRarity(ItemRarity.Uncommon, position);
        public static GameObject SpawnRareItem(Vector3 position) => SpawnItemOfRarity(ItemRarity.Rare, position);
        public static GameObject SpawnEpicItem(Vector3 position) => SpawnItemOfRarity(ItemRarity.Epic, position);
        public static GameObject SpawnLegendaryItem(Vector3 position) => SpawnItemOfRarity(ItemRarity.Legendary, position);
        public static GameObject SpawnArtifactItem(Vector3 position) => SpawnItemOfRarity(ItemRarity.Artifact, position);

        #endregion

        #region Editor Debug

        void OnDrawGizmos()
        {
            // Draw spawn radius
            Vector3 center = GetSpawnPoint();

            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(center, spawnRadius);

            Gizmos.color = Color.red;
            Gizmos.DrawWireCube(center, Vector3.one * 0.5f);
        }

        #endregion
    }
}