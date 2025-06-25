using System.Collections;
using UnityEngine;
using FatalOdds.Runtime;

namespace FatalOdds.Runtime
{
    [RequireComponent(typeof(Collider))]
    public class ItemPickup : MonoBehaviour
    {
        [Header("Item Configuration")]
        [SerializeField] private ItemDefinition itemDefinition;
        [SerializeField] private int stackCount = 1;

        [Header("Universal Visual Components")]
        [SerializeField] private Transform itemModelParent;
        [SerializeField] private MeshRenderer defaultMeshRenderer;
        [SerializeField] private MeshFilter defaultMeshFilter;

        [Header("Rarity Effects (All Built-in)")]
        [SerializeField] private Light[] rarityGlows; // One for each rarity
        [SerializeField] private ParticleSystem[] rarityParticles; // One for each rarity
        [SerializeField] private GameObject[] rarityEffectGroups; // Group objects for organization

        [Header("Animation Components")]
        [SerializeField] private Transform floatingPivot;
        [SerializeField] private Transform rotatingPivot;

        [Header("Pickup Settings")]
        [SerializeField] private float pickupRange = 2f;
        [SerializeField] private bool autoPickup = true;
        [SerializeField] private bool requirePlayerTag = true;
        [SerializeField] private string playerTag = "Player";

        [Header("Audio")]
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private AudioClip[] pickupSounds; // Different sounds per rarity

        [Header("Notification Settings")]
        [SerializeField] private bool showPickupNotification = true;
        [SerializeField] private PickupNotificationUI notificationUI; // Direct reference option

        // Runtime state
        private ItemRarity currentRarity = ItemRarity.Common;
        private bool isInitialized = false;
        private bool isBeingCollected = false;
        private Coroutine floatingCoroutine;
        private Coroutine rotatingCoroutine;

        // Default meshes for different item types
        [Header("Default Meshes")]
        [SerializeField] private Mesh cubeMesh;
        [SerializeField] private Mesh sphereMesh;
        [SerializeField] private Mesh capsuleMesh;

        // Rarity colors that match ItemMaterialSystem
        private static readonly Color[] RarityColors = {
            new Color(0.85f, 0.85f, 0.85f, 1f), // Common
            new Color(0.4f, 0.8f, 0.4f, 1f),    // Uncommon  
            new Color(0.3f, 0.5f, 0.9f, 1f),    // Rare
            new Color(0.7f, 0.4f, 0.9f, 1f),    // Epic
            new Color(1f, 0.8f, 0.3f, 1f),      // Legendary
            new Color(0.8f, 0.3f, 0.3f, 1f)     // Artifact
        };

        void Start()
        {
            // If no notification UI is assigned, try to find one
            if (notificationUI == null)
            {
                notificationUI = FindObjectOfType<PickupNotificationUI>();
            }

            // If we have an item definition assigned, initialize immediately
            if (itemDefinition != null)
            {
                Initialize(itemDefinition, stackCount);
            }
        }

        // Initialize this pickup with a specific item
        public void Initialize(ItemDefinition item, int count = 1)
        {
            if (item == null)
            {
                Debug.LogWarning("[UniversalItemPickup] Cannot initialize with null item definition!");
                return;
            }

            itemDefinition = item;
            stackCount = count;
            currentRarity = item.rarity;
            isInitialized = true;

            // Configure all visual aspects
            ConfigureVisuals();
            ConfigureRarityEffects();
            ConfigureAnimation();
            ConfigureAudio();

            // Update name for easier debugging
            gameObject.name = $"Pickup_{item.itemName}";

            Debug.Log($"[UniversalItemPickup] Initialized as {item.itemName} (Rarity: {item.rarity})");
        }

        // Configure the visual representation of the item
        private void ConfigureVisuals()
        {
            if (defaultMeshRenderer == null || defaultMeshFilter == null) return;

            // Set the mesh based on item type or use a default
            Mesh meshToUse = GetMeshForItemType();
            defaultMeshFilter.mesh = meshToUse;

            // Try to use ItemMaterialSystem material first, fallback to basic material
            Material rarityMaterial = LoadItemMaterialSystemMaterial() ?? CreateBasicRarityMaterial();
            defaultMeshRenderer.material = rarityMaterial;

            // Scale based on rarity (higher rarity = slightly larger)
            float scale = 0.5f + ((int)currentRarity * 0.1f);
            if (itemModelParent != null)
            {
                itemModelParent.localScale = Vector3.one * scale;
            }
        }

        // Get appropriate mesh for the item type
        private Mesh GetMeshForItemType()
        {
            if (itemDefinition.itemName.ToLower().Contains("potion") ||
                itemDefinition.itemName.ToLower().Contains("flask"))
                return capsuleMesh;
            else if (itemDefinition.itemName.ToLower().Contains("orb") ||
                     itemDefinition.itemName.ToLower().Contains("gem") ||
                     itemDefinition.itemName.ToLower().Contains("core"))
                return sphereMesh;
            else
                return cubeMesh; // Default
        }

        // Try to load material from ItemMaterialSystem
        private Material LoadItemMaterialSystemMaterial()
        {
#if UNITY_EDITOR
            string materialPath = $"Assets/FatalOdds/Generated/Materials/Item_{currentRarity}.mat";
            Material material = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            if (material != null)
            {
                return new Material(material); // Create instance to avoid modifying shared material
            }
#endif
            return null;
        }

        // Create basic material as fallback
        private Material CreateBasicRarityMaterial()
        {
            // Try different shaders for best compatibility
            Shader shader = Shader.Find("Universal Render Pipeline/Lit") ??
                           Shader.Find("Standard") ??
                           Shader.Find("Diffuse");

            Material material = new Material(shader);
            Color baseColor = RarityColors[(int)currentRarity];

            // Set base color (URP and Built-in compatibility)
            if (material.HasProperty("_BaseColor"))
                material.SetColor("_BaseColor", baseColor);
            else if (material.HasProperty("_Color"))
                material.SetColor("_Color", baseColor);

            // Set material properties based on rarity
            switch (currentRarity)
            {
                case ItemRarity.Common:
                    SetMaterialProperty(material, "_Metallic", 0.1f);
                    SetMaterialProperty(material, "_Smoothness", 0.4f);
                    SetMaterialProperty(material, "_Glossiness", 0.4f);
                    break;
                case ItemRarity.Uncommon:
                    SetMaterialProperty(material, "_Metallic", 0.3f);
                    SetMaterialProperty(material, "_Smoothness", 0.55f);
                    SetMaterialProperty(material, "_Glossiness", 0.55f);
                    break;
                case ItemRarity.Rare:
                    SetMaterialProperty(material, "_Metallic", 0.5f);
                    SetMaterialProperty(material, "_Smoothness", 0.7f);
                    SetMaterialProperty(material, "_Glossiness", 0.7f);
                    material.EnableKeyword("_EMISSION");
                    SetMaterialProperty(material, "_EmissionColor", baseColor * 0.6f);
                    break;
                case ItemRarity.Epic:
                    SetMaterialProperty(material, "_Metallic", 0.7f);
                    SetMaterialProperty(material, "_Smoothness", 0.8f);
                    SetMaterialProperty(material, "_Glossiness", 0.8f);
                    material.EnableKeyword("_EMISSION");
                    SetMaterialProperty(material, "_EmissionColor", baseColor * 1f);
                    break;
                case ItemRarity.Legendary:
                    SetMaterialProperty(material, "_Metallic", 0.85f);
                    SetMaterialProperty(material, "_Smoothness", 0.9f);
                    SetMaterialProperty(material, "_Glossiness", 0.9f);
                    material.EnableKeyword("_EMISSION");
                    SetMaterialProperty(material, "_EmissionColor", baseColor * 1.4f);
                    break;
                case ItemRarity.Artifact:
                    SetMaterialProperty(material, "_Metallic", 0.9f);
                    SetMaterialProperty(material, "_Smoothness", 0.95f);
                    SetMaterialProperty(material, "_Glossiness", 0.95f);
                    material.EnableKeyword("_EMISSION");
                    SetMaterialProperty(material, "_EmissionColor", baseColor * 2f);
                    break;
            }

            return material;
        }

        // Helper to set material properties safely
        private void SetMaterialProperty(Material mat, string property, float value)
        {
            if (mat.HasProperty(property))
                mat.SetFloat(property, value);
        }

        private void SetMaterialProperty(Material mat, string property, Color value)
        {
            if (mat.HasProperty(property))
                mat.SetColor(property, value);
        }

        // Configure rarity-specific effects (lights and particles)
        private void ConfigureRarityEffects()
        {
            // Disable all effects first
            DisableAllEffects();

            int rarityIndex = (int)currentRarity;

            // Enable glow for uncommon and above
            if (currentRarity >= ItemRarity.Uncommon && rarityGlows != null && rarityIndex < rarityGlows.Length)
            {
                Light glowLight = rarityGlows[rarityIndex];
                if (glowLight != null)
                {
                    glowLight.gameObject.SetActive(true);
                    glowLight.color = RarityColors[rarityIndex];
                    glowLight.intensity = 0.3f + (rarityIndex * 0.2f);
                    glowLight.range = 3f + (rarityIndex * 1f);
                }
            }

            // Enable particles for rare and above
            if (currentRarity >= ItemRarity.Rare && rarityParticles != null && rarityIndex < rarityParticles.Length)
            {
                ParticleSystem particles = rarityParticles[rarityIndex];
                if (particles != null)
                {
                    particles.gameObject.SetActive(true);

                    var main = particles.main;
                    main.startColor = RarityColors[rarityIndex];
                    main.maxParticles = 10 + (rarityIndex * 5);

                    particles.Play();
                }
            }

            // Enable effect groups if they exist
            if (rarityEffectGroups != null && rarityIndex < rarityEffectGroups.Length)
            {
                GameObject effectGroup = rarityEffectGroups[rarityIndex];
                if (effectGroup != null)
                {
                    effectGroup.SetActive(true);
                }
            }
        }

        // Disable all rarity effects
        private void DisableAllEffects()
        {
            // Disable all glow lights
            if (rarityGlows != null)
            {
                foreach (Light light in rarityGlows)
                {
                    if (light != null)
                        light.gameObject.SetActive(false);
                }
            }

            // Disable all particle systems
            if (rarityParticles != null)
            {
                foreach (ParticleSystem ps in rarityParticles)
                {
                    if (ps != null)
                    {
                        ps.gameObject.SetActive(false);
                        ps.Stop();
                    }
                }
            }

            // Disable all effect groups
            if (rarityEffectGroups != null)
            {
                foreach (GameObject group in rarityEffectGroups)
                {
                    if (group != null)
                        group.SetActive(false);
                }
            }
        }

        // Configure floating and rotation animations
        private void ConfigureAnimation()
        {
            // Stop any existing animations
            if (floatingCoroutine != null)
                StopCoroutine(floatingCoroutine);
            if (rotatingCoroutine != null)
                StopCoroutine(rotatingCoroutine);

            // Start floating animation
            if (floatingPivot != null)
            {
                float floatSpeed = 2f + ((int)currentRarity * 0.3f); // Higher rarity = faster float
                float floatHeight = 0.3f + ((int)currentRarity * 0.1f); // Higher rarity = more float
                floatingCoroutine = StartCoroutine(FloatingAnimation(floatSpeed, floatHeight));
            }

            // Start rotation animation
            if (rotatingPivot != null)
            {
                float rotSpeed = 45f + ((int)currentRarity * 15f); // Higher rarity = faster rotation
                rotatingCoroutine = StartCoroutine(RotationAnimation(rotSpeed));
            }
        }

        // Configure audio based on rarity
        private void ConfigureAudio()
        {
            if (audioSource != null && pickupSounds != null)
            {
                int rarityIndex = (int)currentRarity;
                if (rarityIndex < pickupSounds.Length && pickupSounds[rarityIndex] != null)
                {
                    audioSource.clip = pickupSounds[rarityIndex];
                }
            }
        }

        // Floating animation coroutine
        private IEnumerator FloatingAnimation(float speed, float height)
        {
            Vector3 startPos = floatingPivot.localPosition;

            while (!isBeingCollected)
            {
                float offset = Mathf.Sin(Time.time * speed) * height;
                floatingPivot.localPosition = startPos + Vector3.up * offset;
                yield return null;
            }
        }

        // Rotation animation coroutine
        private IEnumerator RotationAnimation(float speed)
        {
            while (!isBeingCollected)
            {
                rotatingPivot.Rotate(Vector3.up, speed * Time.deltaTime);
                yield return null;
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!isInitialized || !autoPickup || isBeingCollected) return;

            if (requirePlayerTag && !other.CompareTag(playerTag)) return;

            // Roguelike pickup: Apply item effects directly to player via ModifierManager
            ModifierManager modifierManager = other.GetComponent<ModifierManager>();
            if (modifierManager != null)
            {
                // Apply the item's modifiers to the player (stacking system)
                itemDefinition.ApplyToTarget(other.gameObject, stackCount);

                Debug.Log($"[UniversalItemPickup] Player collected {itemDefinition.itemName} (x{stackCount}) - Effects applied!");

                // SHOW PICKUP NOTIFICATION
                if (showPickupNotification)
                {
                    ShowPickupNotification();
                }

                CollectItem();
            }
            else
            {
                Debug.LogWarning($"[UniversalItemPickup] Player has no ModifierManager! Cannot apply {itemDefinition.itemName} effects.");

                // Still collect the item and show notification
                Debug.Log($"[UniversalItemPickup] Player picked up {itemDefinition.itemName} x{stackCount} (no effects applied)");

                if (showPickupNotification)
                {
                    ShowPickupNotification();
                }

                CollectItem();
            }
        }

        // Show pickup notification
        private void ShowPickupNotification()
        {
            // Try assigned notification UI first
            if (notificationUI != null)
            {
                notificationUI.ShowPickupNotification(itemDefinition, stackCount);
                return;
            }

            // Fallback: try to find in scene
            PickupNotificationUI foundNotificationUI = FindObjectOfType<PickupNotificationUI>();
            if (foundNotificationUI != null)
            {
                foundNotificationUI.ShowPickupNotification(itemDefinition, stackCount);
                return;
            }

            // Last resort: create a simple notification
            Debug.Log($"[UniversalItemPickup] No notification system found - creating simple notification");
            CreateSimpleNotification();
        }

        // Create a simple notification if no notification system exists
        private void CreateSimpleNotification()
        {
            // Create a temporary notification GameObject
            GameObject notification = new GameObject("SimplePickupNotification");
            notification.transform.position = transform.position + Vector3.up * 2f;

            // Add floating text component (you could expand this)
            var floatingText = notification.AddComponent<FloatingText>();
            if (floatingText != null)
            {
                string text = stackCount > 1 ? $"+{stackCount}x {itemDefinition.itemName}" : $"+{itemDefinition.itemName}";
                floatingText.ShowText(text, GetRarityColor(currentRarity), 1.5f);
            }

            // Destroy after animation
            Destroy(notification, 2f);
        }

        // Get rarity color
        private Color GetRarityColor(ItemRarity rarity)
        {
            return RarityColors[(int)rarity];
        }

        // Collect the item and play effects
        private void CollectItem()
        {
            if (isBeingCollected) return;
            isBeingCollected = true;

            // Play pickup sound
            PlayPickupSound();

            // Add pickup effect if available
            if (itemDefinition.pickupEffect != null)
            {
                Instantiate(itemDefinition.pickupEffect, transform.position, Quaternion.identity);
            }

            // Start collection animation and destroy
            StartCoroutine(CollectionAnimation());
        }

        // Collection animation
        private IEnumerator CollectionAnimation()
        {
            float animationTime = 0.3f;
            Vector3 startScale = transform.localScale;
            Vector3 startPos = transform.position;

            float elapsed = 0f;
            while (elapsed < animationTime)
            {
                elapsed += Time.deltaTime;
                float progress = elapsed / animationTime;

                // Scale down and move up
                transform.localScale = Vector3.Lerp(startScale, Vector3.zero, progress);
                transform.position = Vector3.Lerp(startPos, startPos + Vector3.up * 1f, progress);

                yield return null;
            }

            Destroy(gameObject);
        }

        // Play pickup sound effect
        private void PlayPickupSound()
        {
            if (audioSource != null && audioSource.clip != null)
            {
                audioSource.Play();
            }
        }

        // Testing method to set a random item
        [ContextMenu("Set Random Item")]
        public void SetRandomItemForTesting()
        {
            var allItems = ItemSpawner.GetAllItems();
            if (allItems != null && allItems.Count > 0)
            {
                var randomItem = allItems[Random.Range(0, allItems.Count)];
                Initialize(randomItem, Random.Range(1, 6));
            }
            else
            {
                Debug.LogWarning("No items found for random selection!");
            }
        }

        // Public getters
        public ItemDefinition GetItemDefinition() => itemDefinition;
        public int GetStackCount() => stackCount;

        // Public setter for notification UI (useful for dependency injection)
        public void SetNotificationUI(PickupNotificationUI ui)
        {
            notificationUI = ui;
        }

        void OnValidate()
        {
            // In editor, reinitialize if we have an item assigned
            if (itemDefinition != null && Application.isPlaying)
            {
                Initialize(itemDefinition, stackCount);
            }
        }
    }

    // Simple floating text component for fallback notifications
    public class FloatingText : MonoBehaviour
    {
        public void ShowText(string text, Color color, float duration)
        {
            StartCoroutine(FloatAndFade(text, color, duration));
        }

        private IEnumerator FloatAndFade(string text, Color color, float duration)
        {
            // Simple upward movement
            Vector3 startPos = transform.position;
            Vector3 endPos = startPos + Vector3.up * 3f;

            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;

                transform.position = Vector3.Lerp(startPos, endPos, t);
                yield return null;
            }
        }
    }

    public interface IInventory
    {
        bool AddItem(ItemDefinition item, int count);
    }
}