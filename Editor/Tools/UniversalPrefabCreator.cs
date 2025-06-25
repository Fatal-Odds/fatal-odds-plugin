using UnityEngine;
using UnityEditor;
using FatalOdds.Runtime;

namespace FatalOdds.Editor
{
    // Create the universal item pickup prefab with proper materials
    public static class UniversalPrefabCreator
    {
        public static void CreateUniversalPickupPrefab()
        {
            // Create the main GameObject
            GameObject pickup = new GameObject("UniversalItemPickup");

            // FIXED: Add required components FIRST before adding ItemPickup
            // Add collider for pickup detection (required by ItemPickup)
            SphereCollider collider = pickup.AddComponent<SphereCollider>();
            collider.isTrigger = true;
            collider.radius = 2f;

            // Add audio source
            AudioSource audioSource = pickup.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 1f; // 3D sound

            // NOW add the ItemPickup component (after required components)
            MonoBehaviour pickupComponent = null;
            
            // Method 1: Direct component addition (most reliable)
            try
            {
                pickupComponent = pickup.AddComponent<ItemPickup>();
                Debug.Log("[UniversalPrefabCreator] Successfully added ItemPickup component");
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"Direct ItemPickup addition failed: {e.Message}");
            }

            // Method 2: If direct addition failed, try reflection with assembly search
            if (pickupComponent == null)
            {
                try
                {
                    // Search through all loaded assemblies
                    var assemblies = System.AppDomain.CurrentDomain.GetAssemblies();
                    System.Type itemPickupType = null;

                    foreach (var assembly in assemblies)
                    {
                        // Try different possible type names
                        string[] typeNames = {
                            "FatalOdds.Runtime.ItemPickup",
                            "FatalOdds.Runtime.UniversalItemPickup",
                            "ItemPickup",
                            "UniversalItemPickup"
                        };

                        foreach (string typeName in typeNames)
                        {
                            itemPickupType = assembly.GetType(typeName);
                            if (itemPickupType != null)
                            {
                                Debug.Log($"[UniversalPrefabCreator] Found type: {typeName} in assembly: {assembly.GetName().Name}");
                                break;
                            }
                        }

                        if (itemPickupType != null) break;
                    }

                    if (itemPickupType != null)
                    {
                        pickupComponent = pickup.AddComponent(itemPickupType) as MonoBehaviour;
                        Debug.Log($"[UniversalPrefabCreator] Successfully added {itemPickupType.Name} component via reflection");
                    }
                }
                catch (System.Exception e)
                {
                    Debug.LogWarning($"Reflection method failed: {e.Message}");
                }
            }

            // Ensure we have a valid component before proceeding
            if (pickupComponent == null)
            {
                Debug.LogError("[UniversalPrefabCreator] Could not add ItemPickup component!");
                Object.DestroyImmediate(pickup);
                
                EditorUtility.DisplayDialog("Component Error",
                    "Could not add the ItemPickup component!\n\n" +
                    "This usually means there's a compilation error.\n" +
                    "Please check the Console for red error messages.",
                    "OK");
                return;
            }

            // Create visual structure (with null check)
            try
            {
                CreateVisualStructure(pickup, pickupComponent);
                CreateRarityEffects(pickup, pickupComponent);
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[UniversalPrefabCreator] Error setting up prefab structure: {e.Message}");
                Object.DestroyImmediate(pickup);
                return;
            }

            // Ensure folders exist and save as prefab
            string prefabPath = "Assets/FatalOdds/Prefabs/UniversalItemPickup.prefab";
            EnsurePrefabFolderExists();

            try
            {
                GameObject prefab = PrefabUtility.SaveAsPrefabAsset(pickup, prefabPath);
                Object.DestroyImmediate(pickup);

                // Select the created prefab
                Selection.activeObject = prefab;
                EditorGUIUtility.PingObject(prefab);

                Debug.Log($"[UniversalPrefabCreator] Created universal pickup prefab at: {prefabPath}");

                EditorUtility.DisplayDialog("Success!",
                    $"Universal Item Pickup prefab created successfully!\n\nLocation: {prefabPath}\n\nYou can now use this single prefab for all items.",
                    "Awesome!");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[UniversalPrefabCreator] Failed to save prefab: {e.Message}");
                Object.DestroyImmediate(pickup);
            }
        }

        // FIXED: Changed to use MonoBehaviour for flexibility with different component names
        private static void CreateVisualStructure(GameObject parent, MonoBehaviour component)
        {
            if (component == null)
            {
                Debug.LogError("[UniversalPrefabCreator] Component is null in CreateVisualStructure!");
                return;
            }

            // Create model parent (for scaling)
            GameObject modelParent = new GameObject("ModelParent");
            modelParent.transform.SetParent(parent.transform);
            modelParent.transform.localPosition = Vector3.zero;

            // Create floating pivot (for floating animation)
            GameObject floatingPivot = new GameObject("FloatingPivot");
            floatingPivot.transform.SetParent(modelParent.transform);
            floatingPivot.transform.localPosition = Vector3.zero;

            // Create rotating pivot (for rotation animation)
            GameObject rotatingPivot = new GameObject("RotatingPivot");
            rotatingPivot.transform.SetParent(floatingPivot.transform);
            rotatingPivot.transform.localPosition = Vector3.zero;

            // Create default mesh object
            GameObject meshObject = new GameObject("DefaultMesh");
            meshObject.transform.SetParent(rotatingPivot.transform);
            meshObject.transform.localPosition = Vector3.zero;
            meshObject.transform.localScale = Vector3.one * 0.5f;

            // Add mesh components
            MeshFilter meshFilter = meshObject.AddComponent<MeshFilter>();
            MeshRenderer meshRenderer = meshObject.AddComponent<MeshRenderer>();

            // Set default cube mesh
            meshFilter.mesh = GetBuiltinMesh("Cube");

            // Use ItemMaterialSystem default material or create fallback
            Material defaultMaterial = GetDefaultMaterial();
            meshRenderer.material = defaultMaterial;

            // Assign references using SerializedObject with proper error handling
            try
            {
                SerializedObject serializedComponent = new SerializedObject(component);

                SerializedProperty itemModelParentProp = serializedComponent.FindProperty("itemModelParent");
                SerializedProperty defaultMeshRendererProp = serializedComponent.FindProperty("defaultMeshRenderer");
                SerializedProperty defaultMeshFilterProp = serializedComponent.FindProperty("defaultMeshFilter");
                SerializedProperty floatingPivotProp = serializedComponent.FindProperty("floatingPivot");
                SerializedProperty rotatingPivotProp = serializedComponent.FindProperty("rotatingPivot");
                SerializedProperty audioSourceProp = serializedComponent.FindProperty("audioSource");

                if (itemModelParentProp != null) itemModelParentProp.objectReferenceValue = modelParent.transform;
                if (defaultMeshRendererProp != null) defaultMeshRendererProp.objectReferenceValue = meshRenderer;
                if (defaultMeshFilterProp != null) defaultMeshFilterProp.objectReferenceValue = meshFilter;
                if (floatingPivotProp != null) floatingPivotProp.objectReferenceValue = floatingPivot.transform;
                if (rotatingPivotProp != null) rotatingPivotProp.objectReferenceValue = rotatingPivot.transform;
                if (audioSourceProp != null) audioSourceProp.objectReferenceValue = parent.GetComponent<AudioSource>();

                // Store default meshes
                SerializedProperty cubeMeshProp = serializedComponent.FindProperty("cubeMesh");
                SerializedProperty sphereMeshProp = serializedComponent.FindProperty("sphereMesh");
                SerializedProperty capsuleMeshProp = serializedComponent.FindProperty("capsuleMesh");

                if (cubeMeshProp != null) cubeMeshProp.objectReferenceValue = GetBuiltinMesh("Cube");
                if (sphereMeshProp != null) sphereMeshProp.objectReferenceValue = GetBuiltinMesh("Sphere");
                if (capsuleMeshProp != null) capsuleMeshProp.objectReferenceValue = GetBuiltinMesh("Capsule");

                serializedComponent.ApplyModifiedProperties();
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[UniversalPrefabCreator] Could not set serialized properties: {e.Message}. Prefab will still work but may need manual setup.");
            }
        }

        // FIXED: Changed to use MonoBehaviour for flexibility with different component names
        private static void CreateRarityEffects(GameObject parent, MonoBehaviour component)
        {
            if (component == null)
            {
                Debug.LogError("[UniversalPrefabCreator] Component is null in CreateRarityEffects!");
                return;
            }

            // Create effects parent
            GameObject effectsParent = new GameObject("RarityEffects");
            effectsParent.transform.SetParent(parent.transform);
            effectsParent.transform.localPosition = Vector3.zero;

            // Create arrays for storing effect references
            Light[] rarityGlows = new Light[6]; // One for each rarity
            ParticleSystem[] rarityParticles = new ParticleSystem[6];
            GameObject[] rarityEffectGroups = new GameObject[6];

            // Create effect groups for each rarity
            string[] rarityNames = { "Common", "Uncommon", "Rare", "Epic", "Legendary", "Artifact" };
            Color[] rarityColors = GetRarityColors();

            for (int i = 0; i < 6; i++)
            {
                // Create group for this rarity
                GameObject rarityGroup = new GameObject($"{rarityNames[i]}Effects");
                rarityGroup.transform.SetParent(effectsParent.transform);
                rarityGroup.transform.localPosition = Vector3.zero;
                rarityGroup.SetActive(false); // Start disabled
                rarityEffectGroups[i] = rarityGroup;

                // Create glow light (for uncommon and above)
                if (i >= 1) // Uncommon+
                {
                    GameObject glowObject = new GameObject("Glow");
                    glowObject.transform.SetParent(rarityGroup.transform);
                    glowObject.transform.localPosition = Vector3.zero;

                    Light glowLight = glowObject.AddComponent<Light>();
                    glowLight.type = LightType.Point;
                    glowLight.color = rarityColors[i];
                    glowLight.shadows = LightShadows.None;
                    glowLight.intensity = 0.3f + (i * 0.2f);
                    glowLight.range = 3f + (i * 1f);

                    rarityGlows[i] = glowLight;
                }

                // Create particle system (for rare and above)
                if (i >= 2) // Rare+
                {
                    GameObject particleObject = new GameObject("Particles");
                    particleObject.transform.SetParent(rarityGroup.transform);
                    particleObject.transform.localPosition = Vector3.zero;

                    ParticleSystem particles = particleObject.AddComponent<ParticleSystem>();

                    // Configure particle system
                    var main = particles.main;
                    main.startLifetime = 2f;
                    main.startSpeed = 1f;
                    main.startSize = 0.1f;
                    main.startColor = rarityColors[i];
                    main.maxParticles = 10 + (i * 5);
                    main.simulationSpace = ParticleSystemSimulationSpace.Local;

                    // Shape
                    var shape = particles.shape;
                    shape.enabled = true;
                    shape.shapeType = ParticleSystemShapeType.Sphere;
                    shape.radius = 1f;

                    // Emission
                    var emission = particles.emission;
                    emission.rateOverTime = 5 + (i * 2);

                    // Velocity over lifetime
                    var velocity = particles.velocityOverLifetime;
                    velocity.enabled = true;
                    velocity.space = ParticleSystemSimulationSpace.Local;
                    velocity.radial = new ParticleSystem.MinMaxCurve(0.5f);

                    // Color over lifetime (fade out)
                    var colorOverLifetime = particles.colorOverLifetime;
                    colorOverLifetime.enabled = true;
                    Gradient gradient = new Gradient();
                    gradient.SetKeys(
                        new GradientColorKey[] { new GradientColorKey(rarityColors[i], 0f), new GradientColorKey(rarityColors[i], 1f) },
                        new GradientAlphaKey[] { new GradientAlphaKey(0.8f, 0f), new GradientAlphaKey(0f, 1f) }
                    );
                    colorOverLifetime.color = gradient;

                    particles.Stop(); // Start stopped
                    rarityParticles[i] = particles;
                }
            }

            // Assign arrays to component using SerializedObject with proper error handling
            try
            {
                SerializedObject serializedComponent = new SerializedObject(component);

                SerializedProperty rarityGlowsProp = serializedComponent.FindProperty("rarityGlows");
                SerializedProperty rarityParticlesProp = serializedComponent.FindProperty("rarityParticles");
                SerializedProperty rarityEffectGroupsProp = serializedComponent.FindProperty("rarityEffectGroups");

                if (rarityGlowsProp != null)
                {
                    rarityGlowsProp.arraySize = 6;
                    for (int i = 0; i < 6; i++)
                    {
                        rarityGlowsProp.GetArrayElementAtIndex(i).objectReferenceValue = rarityGlows[i];
                    }
                }

                if (rarityParticlesProp != null)
                {
                    rarityParticlesProp.arraySize = 6;
                    for (int i = 0; i < 6; i++)
                    {
                        rarityParticlesProp.GetArrayElementAtIndex(i).objectReferenceValue = rarityParticles[i];
                    }
                }

                if (rarityEffectGroupsProp != null)
                {
                    rarityEffectGroupsProp.arraySize = 6;
                    for (int i = 0; i < 6; i++)
                    {
                        rarityEffectGroupsProp.GetArrayElementAtIndex(i).objectReferenceValue = rarityEffectGroups[i];
                    }
                }

                serializedComponent.ApplyModifiedProperties();
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[UniversalPrefabCreator] Could not set rarity effect properties: {e.Message}. Effects may need manual setup.");
            }
        }

        // Get built-in Unity mesh by name
        private static Mesh GetBuiltinMesh(string meshName)
        {
            PrimitiveType primitiveType = PrimitiveType.Cube;
            switch (meshName.ToLowerInvariant())
            {
                case "sphere": primitiveType = PrimitiveType.Sphere; break;
                case "capsule": primitiveType = PrimitiveType.Capsule; break;
                case "cylinder": primitiveType = PrimitiveType.Cylinder; break;
            }

            GameObject temp = GameObject.CreatePrimitive(primitiveType);
            Mesh mesh = temp.GetComponent<MeshFilter>().sharedMesh;
            Object.DestroyImmediate(temp);
            return mesh;
        }

        // Get default material, preferring ItemMaterialSystem materials
        private static Material GetDefaultMaterial()
        {
            // Try to use Common material from ItemMaterialSystem
            string materialPath = "Assets/FatalOdds/Generated/Materials/Item_Common.mat";
            Material material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);

            if (material != null)
            {
                return material;
            }

            // Fallback: create basic material
            Shader shader = GetBestShader();
            material = new Material(shader);
            material.name = "Universal_Default";
            material.color = Color.white;

            if (material.HasProperty("_Metallic"))
                material.SetFloat("_Metallic", 0.1f);
            if (material.HasProperty("_Smoothness"))
                material.SetFloat("_Smoothness", 0.4f);
            else if (material.HasProperty("_Glossiness"))
                material.SetFloat("_Glossiness", 0.4f);

            return material;
        }

        private static Shader GetBestShader()
        {
            string[] shaderNames = {
                "Universal Render Pipeline/Lit",
                "URP/Lit",
                "Standard",
                "Diffuse"
            };

            foreach (string name in shaderNames)
            {
                Shader shader = Shader.Find(name);
                if (shader != null) return shader;
            }

            return Shader.Find("Diffuse");
        }

        private static Color[] GetRarityColors()
        {
            return new Color[] {
                new Color(0.85f, 0.85f, 0.85f, 1f), // Common
                new Color(0.4f, 0.8f, 0.4f, 1f),    // Uncommon
                new Color(0.3f, 0.5f, 0.9f, 1f),    // Rare
                new Color(0.7f, 0.4f, 0.9f, 1f),    // Epic
                new Color(1f, 0.8f, 0.3f, 1f),      // Legendary
                new Color(0.8f, 0.3f, 0.3f, 1f)     // Artifact
            };
        }

        private static void EnsurePrefabFolderExists()
        {
            string[] folders = { "Assets", "FatalOdds", "Prefabs" };
            string currentPath = "";

            foreach (string folder in folders)
            {
                string newPath = string.IsNullOrEmpty(currentPath) ? folder : $"{currentPath}/{folder}";

                if (!AssetDatabase.IsValidFolder(newPath))
                {
                    AssetDatabase.CreateFolder(currentPath, folder);
                }

                currentPath = newPath;
            }

            AssetDatabase.Refresh();
        }

        public static void CreateTestSpawner()
        {
            // Create spawner GameObject
            GameObject spawner = new GameObject("RandomItemSpawner");

            // Add the spawner component
            ItemSpawner spawnerComponent = spawner.AddComponent<ItemSpawner>();

            // Try to find the universal pickup prefab
            string prefabPath = "Assets/FatalOdds/Prefabs/UniversalItemPickup.prefab";
            GameObject universalPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);

            if (universalPrefab != null)
            {
                SerializedObject serializedSpawner = new SerializedObject(spawnerComponent);
                SerializedProperty prefabProp = serializedSpawner.FindProperty("universalPickupPrefab");
                if (prefabProp != null)
                {
                    prefabProp.objectReferenceValue = universalPrefab;
                    serializedSpawner.ApplyModifiedProperties();
                }
            }

            // Position the spawner
            spawner.transform.position = Vector3.zero;

            // Select it
            Selection.activeGameObject = spawner;

            Debug.Log("[UniversalPrefabCreator] Created test spawner. Press Space in play mode to spawn random items!");

            EditorUtility.DisplayDialog("Test Spawner Created!",
                "Test spawner created successfully!\n\n" +
                "Controls in Play Mode:\n" +
                "• SPACE - Spawn random item\n" +
                "• 1-6 - Spawn specific rarity\n" +
                "• C - Clear all pickups\n\n" +
                "Make sure you have some ItemDefinitions created first!",
                "Got it!");
        }
    }
}