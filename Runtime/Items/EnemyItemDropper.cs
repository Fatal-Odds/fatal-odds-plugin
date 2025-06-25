using UnityEngine;
using FatalOdds.Runtime;

namespace FatalOdds.Runtime
{
    [AddComponentMenu("Fatal Odds/Immediate Item Dropper")]
    public class EnemyItemDropper : MonoBehaviour
    {
        [Header("Drop Settings")]
        [SerializeField] private float dropChance = 0.3f; // 30% chance to drop
        [SerializeField] private int minItems = 1;
        [SerializeField] private int maxItems = 2;

        [Header("Enemy Reference (Optional)")]
        [SerializeField] private MonoBehaviour enemyScript; // Reference to enemy script
        [SerializeField] private string[] healthFieldNames = { "health", "currentHealth", "hp", "hitPoints", "Health" };
        [SerializeField] private string[] isDeadFieldNames = { "isDead", "dead", "isAlive" }; // isAlive = true means alive

        [Header("Drop Triggers")]
        [SerializeField] private bool dropOnDestroy = true;
        [SerializeField] private bool dropOnHealthZero = true;
        [SerializeField] private bool requireDeathConfirmation = false; // Only drop if enemy is actually dead

        [Header("Rarity Weights")]
        [SerializeField] private float commonWeight = 70f;
        [SerializeField] private float uncommonWeight = 20f;
        [SerializeField] private float rareWeight = 8f;
        [SerializeField] private float epicWeight = 1.8f;
        [SerializeField] private float legendaryWeight = 0.2f;
        [SerializeField] private float artifactWeight = 0.05f;

        [Header("Drop Physics")]
        [SerializeField] private float maxDropDistance = 1.5f; // Maximum distance from enemy
        [SerializeField] private float floatHeight = 1f; // Height above ground to spawn
        [SerializeField] private bool useGroundCheck = true; // Raycast to find ground
        [SerializeField] private LayerMask groundLayers = 1; // What counts as ground

        [Header("Effects")]
        [SerializeField] private GameObject dropEffect;
        [SerializeField] private AudioClip dropSound;

        [Header("Debug")]
        [SerializeField] private bool enableDebugLogs = true;

        // State tracking
        private bool hasDropped = false;
        private System.Reflection.FieldInfo healthField;
        private System.Reflection.FieldInfo deadField;
        private bool isAliveField = false; // True if the field represents "isAlive" instead of "isDead"

        void Start()
        {
            // Auto-find enemy script if not assigned
            if (enemyScript == null)
            {
                enemyScript = FindEnemyScript();
            }

            // Set up reflection for health/death checking
            if (enemyScript != null)
            {
                SetupReflection();
            }
        }

        void Update()
        {
            // Check if enemy died during gameplay (not just OnDestroy)
            if (dropOnHealthZero && !hasDropped && enemyScript != null)
            {
                if (IsEnemyDead())
                {
                    if (enableDebugLogs)
                        Debug.Log($"[ImmediateItemDropper] {gameObject.name} detected as dead during Update, triggering drop");

                    TriggerDrop();
                }
            }
        }

        void OnDestroy()
        {
            // Only drop if we're being destroyed in play mode and haven't already dropped
            if (!Application.isPlaying || !dropOnDestroy || hasDropped) return;

            // Check if enemy is actually dead (if required)
            if (requireDeathConfirmation && enemyScript != null)
            {
                if (!IsEnemyDead())
                {
                    if (enableDebugLogs)
                        Debug.Log($"[ImmediateItemDropper] {gameObject.name} destroyed but enemy not dead, skipping drop");
                    return;
                }
            }

            if (enableDebugLogs)
                Debug.Log($"[ImmediateItemDropper] {gameObject.name} OnDestroy triggered, rolling for drop");

            // Roll for drop chance
            if (Random.value <= dropChance)
            {
                DropItemsImmediately();
            }
            else
            {
                if (enableDebugLogs)
                    Debug.Log($"[ImmediateItemDropper] {gameObject.name} failed drop chance roll ({Random.value:F2} > {dropChance:F2})");
            }
        }

        public void TriggerDrop()
        {
            if (hasDropped) return;

            if (enableDebugLogs)
                Debug.Log($"[ImmediateItemDropper] {gameObject.name} TriggerDrop called");

            if (Random.value <= dropChance)
            {
                DropItemsImmediately();
            }
            else
            {
                hasDropped = true; // Mark as dropped so we don't try again
                if (enableDebugLogs)
                    Debug.Log($"[ImmediateItemDropper] {gameObject.name} failed drop chance roll in TriggerDrop");
            }
        }

        public void ForceDropItems()
        {
            if (!hasDropped)
            {
                if (enableDebugLogs)
                    Debug.Log($"[ImmediateItemDropper] {gameObject.name} ForceDropItems called");

                DropItemsImmediately();
            }
        }

        public void DropSpecificItems(ItemDefinition[] items)
        {
            if (hasDropped || items == null || items.Length == 0) return;

            hasDropped = true;

            if (enableDebugLogs)
                Debug.Log($"[ImmediateItemDropper] {gameObject.name} dropping {items.Length} specific items");

            // Spawn drop effect
            SpawnDropEffect();

            // Drop each specific item
            for (int i = 0; i < items.Length; i++)
            {
                if (items[i] != null)
                {
                    DropSpecificItem(items[i], i);
                }
            }
        }

        private void DropItemsImmediately()
        {
            if (hasDropped) return;
            hasDropped = true;

            // Determine number of items to drop
            int itemCount = Random.Range(minItems, maxItems + 1);

            if (enableDebugLogs)
                Debug.Log($"[ImmediateItemDropper] {gameObject.name} dropping {itemCount} items immediately");

            // Spawn drop effect
            SpawnDropEffect();

            // Drop each item immediately
            for (int i = 0; i < itemCount; i++)
            {
                DropSingleItem(i);
            }
        }

        private void DropSingleItem(int index)
        {
            // Get random rarity based on weights
            ItemRarity rarity = GetWeightedRandomRarity();

            // Get safe position near enemy
            Vector3 dropPosition = GetSafeDropPosition(index);

            // Spawn the item using your universal system
            GameObject droppedItem = ItemSpawner.SpawnItemOfRarity(rarity, dropPosition);

            if (droppedItem != null)
            {
                // NO physics force - items stay put and float nicely
                EnsureFloatingBehavior(droppedItem);

                if (enableDebugLogs)
                    Debug.Log($"[ImmediateItemDropper] Dropped {rarity} item at {dropPosition}");
            }
            else
            {
                Debug.LogWarning($"[ImmediateItemDropper] Failed to spawn {rarity} item - no items of that rarity available!");
            }
        }

        private void DropSpecificItem(ItemDefinition item, int index)
        {
            Vector3 dropPosition = GetSafeDropPosition(index);

            // Spawn the specific item
            GameObject droppedItem = ItemSpawner.SpawnUniversalItem(item, dropPosition);

            if (droppedItem != null)
            {
                EnsureFloatingBehavior(droppedItem);

                if (enableDebugLogs)
                    Debug.Log($"[ImmediateItemDropper] Dropped specific item: {item.itemName}");
            }
        }

        private Vector3 GetSafeDropPosition(int index)
        {
            Vector3 enemyPosition = transform.position;

            // For multiple items, arrange in a small circle around the enemy
            if (index == 0)
            {
                // First item drops at enemy position (with ground check)
                return GetGroundPosition(enemyPosition);
            }
            else
            {
                // Additional items in a small circle
                float angle = (360f / maxItems) * index * Mathf.Deg2Rad;
                float distance = Mathf.Min(maxDropDistance, 1f + (index * 0.3f)); // Small distances only

                Vector3 offset = new Vector3(
                    Mathf.Cos(angle) * distance,
                    0f,
                    Mathf.Sin(angle) * distance
                );

                Vector3 targetPosition = enemyPosition + offset;
                return GetGroundPosition(targetPosition);
            }
        }

        // Find the ground position using raycast
        private Vector3 GetGroundPosition(Vector3 position)
        {
            if (!useGroundCheck)
            {
                return position + Vector3.up * floatHeight;
            }

            // Raycast down to find ground
            Vector3 rayStart = position + Vector3.up * 10f; // Start high
            if (Physics.Raycast(rayStart, Vector3.down, out RaycastHit hit, 20f, groundLayers))
            {
                return hit.point + Vector3.up * floatHeight;
            }

            // Fallback: use original position with float height
            return position + Vector3.up * floatHeight;
        }

        // Ensure item floats nicely instead of bouncing around
        private void EnsureFloatingBehavior(GameObject droppedItem)
        {
            // Remove any rigidbody to prevent physics chaos
            Rigidbody rb = droppedItem.GetComponent<Rigidbody>();
            if (rb != null)
            {
                DestroyImmediate(rb);
            }

            // The UniversalItemPickup already has floating animation built-in
            // So we don't need to add anything - just make sure it stays put!
        }

        private void SpawnDropEffect()
        {
            // Spawn drop effect
            if (dropEffect != null)
            {
                Instantiate(dropEffect, transform.position, Quaternion.identity);
            }

            // Play drop sound
            if (dropSound != null)
            {
                AudioSource.PlayClipAtPoint(dropSound, transform.position);
            }
        }

        private ItemRarity GetWeightedRandomRarity()
        {
            float totalWeight = commonWeight + uncommonWeight + rareWeight + epicWeight + legendaryWeight + artifactWeight;
            float randomValue = Random.Range(0f, totalWeight);

            float currentWeight = 0f;

            currentWeight += commonWeight;
            if (randomValue <= currentWeight) return ItemRarity.Common;

            currentWeight += uncommonWeight;
            if (randomValue <= currentWeight) return ItemRarity.Uncommon;

            currentWeight += rareWeight;
            if (randomValue <= currentWeight) return ItemRarity.Rare;

            currentWeight += epicWeight;
            if (randomValue <= currentWeight) return ItemRarity.Epic;

            currentWeight += legendaryWeight;
            if (randomValue <= currentWeight) return ItemRarity.Legendary;

            return ItemRarity.Artifact; // Fallback to rarest
        }

        private bool IsEnemyDead()
        {
            if (enemyScript == null) return false;

            // Check dead field first (more reliable)
            if (deadField != null)
            {
                try
                {
                    object value = deadField.GetValue(enemyScript);
                    if (value is bool boolVal)
                    {
                        // If it's an "isAlive" field, invert the result
                        return isAliveField ? !boolVal : boolVal;
                    }
                }
                catch (System.Exception e)
                {
                    if (enableDebugLogs)
                        Debug.LogWarning($"[ImmediateItemDropper] Error reading dead field: {e.Message}");
                }
            }

            // Check health field
            if (healthField != null)
            {
                try
                {
                    object value = healthField.GetValue(enemyScript);
                    float health = 0f;

                    if (value is float floatVal) health = floatVal;
                    else if (value is int intVal) health = intVal;

                    return health <= 0f;
                }
                catch (System.Exception e)
                {
                    if (enableDebugLogs)
                        Debug.LogWarning($"[ImmediateItemDropper] Error reading health field: {e.Message}");
                }
            }

            return false; // Default to alive if we can't determine
        }

        private MonoBehaviour FindEnemyScript()
        {
            MonoBehaviour[] components = GetComponents<MonoBehaviour>();

            foreach (var comp in components)
            {
                if (comp == null || comp == this) continue;

                string typeName = comp.GetType().Name.ToLower();

                // Look for enemy-like script names
                if (typeName.Contains("enemy") ||
                    typeName.Contains("monster") ||
                    typeName.Contains("hostile") ||
                    typeName.Contains("mob") ||
                    typeName.Contains("raider") ||
                    typeName.Contains("viking"))
                {
                    if (enableDebugLogs)
                        Debug.Log($"[ImmediateItemDropper] Auto-found enemy script: {comp.GetType().Name}");
                    return comp;
                }
            }

            if (enableDebugLogs)
                Debug.LogWarning($"[ImmediateItemDropper] Could not auto-find enemy script on {gameObject.name}");

            return null;
        }

        private void SetupReflection()
        {
            if (enemyScript == null) return;

            System.Type enemyType = enemyScript.GetType();

            // Look for death/alive fields first (more reliable)
            foreach (string fieldName in isDeadFieldNames)
            {
                var field = enemyType.GetField(fieldName,
                    System.Reflection.BindingFlags.Public |
                    System.Reflection.BindingFlags.NonPublic |
                    System.Reflection.BindingFlags.Instance);

                if (field != null && field.FieldType == typeof(bool))
                {
                    deadField = field;
                    isAliveField = fieldName.ToLower().Contains("alive");

                    if (enableDebugLogs)
                        //Debug.Log($"[ImmediateItemDropper] Found death field: {fieldName} (isAlive: {isAliveField})");
                    break;
                }
            }

            // Look for health fields
            foreach (string fieldName in healthFieldNames)
            {
                var field = enemyType.GetField(fieldName,
                    System.Reflection.BindingFlags.Public |
                    System.Reflection.BindingFlags.NonPublic |
                    System.Reflection.BindingFlags.Instance);

                if (field != null && (field.FieldType == typeof(float) || field.FieldType == typeof(int)))
                {
                    healthField = field;

                    if (enableDebugLogs)
                        //Debug.Log($"[ImmediateItemDropper] Found health field: {fieldName}");
                    break;
                }
            }

            if (deadField == null && healthField == null && enableDebugLogs)
            {
                Debug.LogWarning($"[ImmediateItemDropper] No health or death fields found on {enemyType.Name}");
            }
        }

        // Public properties
        public bool HasDropped => hasDropped;
        public void SetDropChance(float newChance) => dropChance = Mathf.Clamp01(newChance);
        public void SetDropCount(int min, int max) { minItems = min; maxItems = max; }

        void OnDrawGizmosSelected()
        {
            // Show max drop distance
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(transform.position, maxDropDistance);

            // Show float height
            Gizmos.color = Color.yellow;
            Vector3 floatPos = transform.position + Vector3.up * floatHeight;
            Gizmos.DrawWireCube(floatPos, Vector3.one * 0.2f);
        }

        void OnValidate()
        {
            // Clamp values in editor
            dropChance = Mathf.Clamp01(dropChance);
            minItems = Mathf.Max(0, minItems);
            maxItems = Mathf.Max(minItems, maxItems);
            maxDropDistance = Mathf.Max(0.1f, maxDropDistance);
            floatHeight = Mathf.Max(0f, floatHeight);
        }
    }
}