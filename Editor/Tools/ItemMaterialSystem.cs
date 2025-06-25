#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.IO;
using FatalOdds.Runtime;

namespace FatalOdds.Editor
{
    public class ItemMaterialSystem : EditorWindow
    {
        public static void ShowWindow()
        {
            var window = GetWindow<ItemMaterialSystem>("Item Materials");
            window.minSize = new Vector2(400, 600);
            window.Show();
        }

        private bool useAdvancedTextures = true;
        private bool enableEmission = true;
        private float emissionStrength = 1.2f;
        private bool createNormalMaps = true;

        private void OnGUI()
        {
            EditorGUILayout.LabelField("🎨 Item Material System", EditorStyles.boldLabel);
            EditorGUILayout.Space(5);

            EditorGUILayout.HelpBox("Create high-quality materials for item rarities with proper visual progression.", MessageType.Info);
            EditorGUILayout.Space(10);

            DrawSettings();
            DrawMaterialGeneration();
            DrawPreview();
            DrawUtilities();
        }

        private void DrawSettings()
        {
            EditorGUILayout.LabelField("Material Settings", EditorStyles.boldLabel);
            EditorGUILayout.BeginVertical("box");

            useAdvancedTextures = EditorGUILayout.Toggle("Advanced Textures", useAdvancedTextures);
            EditorGUILayout.HelpBox("Creates unique textures for each rarity with surface details", MessageType.Info);

            enableEmission = EditorGUILayout.Toggle("Emission Effects", enableEmission);
            if (enableEmission)
            {
                emissionStrength = EditorGUILayout.Slider("Emission Strength", emissionStrength, 0.5f, 3f);
            }

            createNormalMaps = EditorGUILayout.Toggle("Normal Maps", createNormalMaps);

            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(10);
        }

        private void DrawMaterialGeneration()
        {
            EditorGUILayout.LabelField("Generate Materials", EditorStyles.boldLabel);
            EditorGUILayout.BeginVertical("box");

            if (GUILayout.Button("🌟 Create All Rarity Materials", GUILayout.Height(35)))
            {
                CreateAllMaterials();
            }

            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("Individual Rarities:", EditorStyles.miniLabel);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Common")) CreateMaterial(ItemRarity.Common);
            if (GUILayout.Button("Uncommon")) CreateMaterial(ItemRarity.Uncommon);
            if (GUILayout.Button("Rare")) CreateMaterial(ItemRarity.Rare);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Epic")) CreateMaterial(ItemRarity.Epic);
            if (GUILayout.Button("Legendary")) CreateMaterial(ItemRarity.Legendary);
            if (GUILayout.Button("Artifact")) CreateMaterial(ItemRarity.Artifact);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(10);
        }

        private void DrawPreview()
        {
            EditorGUILayout.LabelField("Visual Preview", EditorStyles.boldLabel);
            EditorGUILayout.BeginVertical("box");

            EditorGUILayout.LabelField("Material Properties by Rarity:");

            var rarities = System.Enum.GetValues(typeof(ItemRarity));
            foreach (ItemRarity rarity in rarities)
            {
                EditorGUILayout.BeginHorizontal();

                // Color preview
                Color rarityColor = GetRarityColor(rarity);
                EditorGUI.BeginDisabledGroup(true);
                EditorGUILayout.ColorField(GUIContent.none, rarityColor, false, false, false, GUILayout.Width(30));
                EditorGUI.EndDisabledGroup();

                // Properties
                EditorGUILayout.LabelField($"{rarity}", GUILayout.Width(80));
                EditorGUILayout.LabelField($"Metal: {GetMetallic(rarity):F1}", GUILayout.Width(60));
                EditorGUILayout.LabelField($"Smooth: {GetSmoothness(rarity):F1}", GUILayout.Width(70));

                if (enableEmission && ShouldHaveEmission(rarity))
                {
                    EditorGUILayout.LabelField($"Glow: {GetEmissionIntensity(rarity):F1}", GUILayout.Width(50));
                }

                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawUtilities()
        {
            EditorGUILayout.LabelField("Utilities", EditorStyles.boldLabel);
            EditorGUILayout.BeginVertical("box");

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Open Materials Folder"))
            {
                OpenMaterialsFolder();
            }
            if (GUILayout.Button("Test Materials"))
            {
                TestMaterials();
            }
            EditorGUILayout.EndHorizontal();

            if (GUILayout.Button("Apply Materials to Existing Prefabs"))
            {
                ApplyMaterialsToExistingPrefabs();
            }

            EditorGUILayout.EndVertical();
        }

        private void CreateAllMaterials()
        {
            Debug.Log("[Item Materials] Creating materials for all rarities...");

            var rarities = System.Enum.GetValues(typeof(ItemRarity));
            int created = 0;

            foreach (ItemRarity rarity in rarities)
            {
                if (CreateMaterial(rarity))
                {
                    created++;
                }
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            EditorUtility.DisplayDialog("Materials Created",
                $"Successfully created {created} high-quality materials!", "Great!");
        }

        private bool CreateMaterial(ItemRarity rarity)
        {
            try
            {
                string materialsPath = "Assets/FatalOdds/Generated/Materials";
                EnsureFolderExists(materialsPath);

                // Create material with best shader
                Material material = new Material(GetBestShader());
                material.name = $"Item_{rarity}";

                // Configure base properties
                ConfigureBaseMaterial(material, rarity);

                // Create and apply texture if needed
                if (useAdvancedTextures)
                {
                    Texture2D diffuseTexture = CreateDiffuseTexture(rarity);
                    string texturePath = $"{materialsPath}/Tex_{rarity}_Diffuse.png";
                    SaveTexture(diffuseTexture, texturePath);
                    material.mainTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);

                    // Normal map
                    if (createNormalMaps)
                    {
                        Texture2D normalTexture = CreateNormalTexture(rarity);
                        string normalPath = $"{materialsPath}/Tex_{rarity}_Normal.png";
                        SaveTexture(normalTexture, normalPath);
                        SetNormalMap(material, AssetDatabase.LoadAssetAtPath<Texture2D>(normalPath));
                    }
                }

                // Save material
                string materialPath = $"{materialsPath}/Item_{rarity}.mat";
                AssetDatabase.CreateAsset(material, materialPath);

                Debug.Log($"Created material: {rarity}");
                return true;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Failed to create material for {rarity}: {e.Message}");
                return false;
            }
        }

        private void ConfigureBaseMaterial(Material mat, ItemRarity rarity)
        {
            Color baseColor = GetRarityColor(rarity);

            // Base color
            SetProperty(mat, "_BaseColor", baseColor);
            SetProperty(mat, "_Color", baseColor);

            // Surface properties
            float metallic = GetMetallic(rarity);
            float smoothness = GetSmoothness(rarity);

            SetProperty(mat, "_Metallic", metallic);
            SetProperty(mat, "_Smoothness", smoothness);
            SetProperty(mat, "_Glossiness", smoothness);

            // Emission for higher rarities
            if (enableEmission && ShouldHaveEmission(rarity))
            {
                mat.EnableKeyword("_EMISSION");
                Color emissionColor = baseColor * GetEmissionIntensity(rarity);
                SetProperty(mat, "_EmissionColor", emissionColor);
            }

            // Special handling for artifact rarity
            if (rarity == ItemRarity.Artifact)
            {
                SetProperty(mat, "_Metallic", 0.95f);
                SetProperty(mat, "_Smoothness", 0.98f);

                if (enableEmission)
                {
                    // Pulsing red energy effect
                    Color artifactEmission = Color.Lerp(baseColor, Color.red, 0.3f);
                    SetProperty(mat, "_EmissionColor", artifactEmission * emissionStrength * 2f);
                }
            }
        }

        private Texture2D CreateDiffuseTexture(ItemRarity rarity)
        {
            int size = 512;
            Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, true);

            Color baseColor = GetRarityColor(rarity);

            for (int x = 0; x < size; x++)
            {
                for (int y = 0; y < size; y++)
                {
                    Color pixelColor = GeneratePixelColor(x, y, size, rarity, baseColor);
                    texture.SetPixel(x, y, pixelColor);
                }
            }

            texture.Apply();
            return texture;
        }

        private Color GeneratePixelColor(int x, int y, int size, ItemRarity rarity, Color baseColor)
        {
            float fx = (float)x / size;
            float fy = (float)y / size;
            Vector2 center = Vector2.one * 0.5f;
            float angle = Mathf.Atan2(fy - 0.5f, fx - 0.5f);
            float dist = Vector2.Distance(new Vector2(fx, fy), center);

            switch (rarity)
            {
                case ItemRarity.Common:
                    // Subtle fabric weave
                    float weave = Mathf.Sin(fx * 80) * Mathf.Sin(fy * 80) * 0.05f + 0.95f;
                    return baseColor * weave;

                case ItemRarity.Uncommon:
                    // Brushed metal lines
                    float brushed = Mathf.Sin(fx * 120) * 0.08f + 0.92f;
                    float patina = Mathf.PerlinNoise(fx * 20, fy * 20) * 0.1f + 0.9f;
                    return baseColor * brushed * patina;

                case ItemRarity.Rare:
                    // Crystal facets
                    float facets = Mathf.Floor(angle / (Mathf.PI / 8)) * (Mathf.PI / 8);
                    float crystal = Mathf.Cos(facets * 8) * 0.15f + 0.85f;
                    float sparkle = Mathf.PerlinNoise(fx * 60, fy * 60);
                    if (sparkle > 0.9f) crystal += 0.2f;
                    return baseColor * crystal;

                case ItemRarity.Epic:
                    // Magical energy swirls
                    float swirl1 = Mathf.Sin(dist * 25 + angle * 6) * 0.2f;
                    float swirl2 = Mathf.Sin(dist * 15 - angle * 4) * 0.15f;
                    float energy = (swirl1 + swirl2) * 0.5f + 0.7f;
                    return baseColor * energy;

                case ItemRarity.Legendary:
                    // Divine radiance with rays
                    float radiance = 1.0f - dist;
                    radiance = Mathf.Pow(radiance, 0.3f);
                    float rays = Mathf.Abs(Mathf.Sin(angle * 16)) * 0.25f + 0.75f;
                    float divine = radiance * rays;
                    return Color.Lerp(baseColor, Color.white, divine * 0.4f);

                case ItemRarity.Artifact:
                    // Ancient runic patterns
                    float runes1 = Mathf.PerlinNoise(fx * 40, fy * 40) * 0.3f;
                    float runes2 = Mathf.Sin(fx * fy * 150) * 0.1f;
                    float veins = Mathf.PerlinNoise(fx * 80, fy * 20) * Mathf.PerlinNoise(fx * 20, fy * 80);

                    if (veins > 0.75f)
                    {
                        // Red energy veins
                        return Color.Lerp(baseColor, new Color(1f, 0.3f, 0.2f, 1f), 0.6f);
                    }
                    else
                    {
                        return baseColor * (runes1 + runes2 + 0.6f);
                    }

                default:
                    return baseColor;
            }
        }

        private Texture2D CreateNormalTexture(ItemRarity rarity)
        {
            int size = 256;
            Texture2D normalMap = new Texture2D(size, size, TextureFormat.RGBA32, true);

            for (int x = 0; x < size; x++)
            {
                for (int y = 0; y < size; y++)
                {
                    float fx = (float)x / size;
                    float fy = (float)y / size;

                    // Generate height for normal calculation
                    float height = GenerateHeightForNormal(fx, fy, rarity);

                    // Simple normal calculation
                    Vector3 normal = new Vector3(0, 0, 1);
                    normal.x = Mathf.Sin(fx * 30) * GetBumpStrength(rarity);
                    normal.y = Mathf.Sin(fy * 30) * GetBumpStrength(rarity);
                    normal = normal.normalized;

                    // Pack normal into color
                    Color normalColor = new Color(
                        normal.x * 0.5f + 0.5f,
                        normal.y * 0.5f + 0.5f,
                        normal.z * 0.5f + 0.5f,
                        1f
                    );

                    normalMap.SetPixel(x, y, normalColor);
                }
            }

            normalMap.Apply();
            return normalMap;
        }

        private float GenerateHeightForNormal(float fx, float fy, ItemRarity rarity)
        {
            switch (rarity)
            {
                case ItemRarity.Common:
                    return Mathf.PerlinNoise(fx * 16, fy * 16) * 0.1f;
                case ItemRarity.Uncommon:
                    return Mathf.Sin(fx * 100) * 0.05f + Mathf.PerlinNoise(fx * 8, fy * 8) * 0.1f;
                case ItemRarity.Rare:
                    return Mathf.PerlinNoise(fx * 32, fy * 32) * 0.2f;
                case ItemRarity.Epic:
                case ItemRarity.Legendary:
                case ItemRarity.Artifact:
                    return Mathf.PerlinNoise(fx * 40, fy * 40) * 0.25f;
                default:
                    return 0.1f;
            }
        }

        // Helper methods
        private Shader GetBestShader()
        {
            string[] shaders = {
                "Universal Render Pipeline/Lit",
                "URP/Lit",
                "Standard",
                "Diffuse"
            };

            foreach (string name in shaders)
            {
                Shader shader = Shader.Find(name);
                if (shader != null) return shader;
            }

            return Shader.Find("Diffuse");
        }

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

        private float GetMetallic(ItemRarity rarity)
        {
            switch (rarity)
            {
                case ItemRarity.Common: return 0.1f;
                case ItemRarity.Uncommon: return 0.3f;
                case ItemRarity.Rare: return 0.5f;
                case ItemRarity.Epic: return 0.7f;
                case ItemRarity.Legendary: return 0.85f;
                case ItemRarity.Artifact: return 0.9f;
                default: return 0.2f;
            }
        }

        private float GetSmoothness(ItemRarity rarity)
        {
            switch (rarity)
            {
                case ItemRarity.Common: return 0.4f;
                case ItemRarity.Uncommon: return 0.55f;
                case ItemRarity.Rare: return 0.7f;
                case ItemRarity.Epic: return 0.8f;
                case ItemRarity.Legendary: return 0.9f;
                case ItemRarity.Artifact: return 0.95f;
                default: return 0.5f;
            }
        }

        private bool ShouldHaveEmission(ItemRarity rarity)
        {
            return rarity >= ItemRarity.Uncommon;
        }

        private float GetEmissionIntensity(ItemRarity rarity)
        {
            switch (rarity)
            {
                case ItemRarity.Uncommon: return emissionStrength * 0.3f;
                case ItemRarity.Rare: return emissionStrength * 0.6f;
                case ItemRarity.Epic: return emissionStrength * 1f;
                case ItemRarity.Legendary: return emissionStrength * 1.4f;
                case ItemRarity.Artifact: return emissionStrength * 2f;
                default: return 0f;
            }
        }

        private float GetBumpStrength(ItemRarity rarity)
        {
            switch (rarity)
            {
                case ItemRarity.Common: return 0.2f;
                case ItemRarity.Uncommon: return 0.3f;
                case ItemRarity.Rare: return 0.5f;
                case ItemRarity.Epic: return 0.7f;
                case ItemRarity.Legendary: return 0.8f;
                case ItemRarity.Artifact: return 1f;
                default: return 0.3f;
            }
        }

        private void SetProperty<T>(Material mat, string property, T value)
        {
            if (mat.HasProperty(property))
            {
                if (value is Color color)
                    mat.SetColor(property, color);
                else if (value is float floatValue)
                    mat.SetFloat(property, floatValue);
            }
        }

        private void SetNormalMap(Material mat, Texture2D normalTexture)
        {
            if (mat.HasProperty("_BumpMap"))
            {
                mat.SetTexture("_BumpMap", normalTexture);
                SetProperty(mat, "_BumpScale", GetBumpStrength(ItemRarity.Common));
            }
            else if (mat.HasProperty("_NormalMap"))
            {
                mat.SetTexture("_NormalMap", normalTexture);
            }
        }

        private void SaveTexture(Texture2D texture, string path)
        {
            byte[] pngData = texture.EncodeToPNG();
            File.WriteAllBytes(path, pngData);
            AssetDatabase.ImportAsset(path);

            // Configure texture import settings
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer != null)
            {
                importer.textureType = TextureImporterType.Default;
                importer.mipmapEnabled = true;
                importer.wrapMode = TextureWrapMode.Repeat;
                importer.filterMode = FilterMode.Trilinear;
                importer.anisoLevel = 4;
                AssetDatabase.ImportAsset(path);
            }
        }

        private void EnsureFolderExists(string path)
        {
            if (!AssetDatabase.IsValidFolder(path))
            {
                string parent = Path.GetDirectoryName(path);
                string folder = Path.GetFileName(path);

                if (!string.IsNullOrEmpty(parent) && !AssetDatabase.IsValidFolder(parent))
                {
                    EnsureFolderExists(parent);
                }

                AssetDatabase.CreateFolder(parent, folder);
            }
        }

        private void OpenMaterialsFolder()
        {
            string path = "Assets/FatalOdds/Generated/Materials";
            EnsureFolderExists(path);

            var folder = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(path);
            EditorGUIUtility.PingObject(folder);
            Selection.activeObject = folder;
        }

        private void TestMaterials()
        {
            // Create test spheres with each material
            Vector3 spawnPos = Vector3.zero;
            float spacing = 2.5f;

            var rarities = System.Enum.GetValues(typeof(ItemRarity));
            foreach (ItemRarity rarity in rarities)
            {
                string materialPath = $"Assets/FatalOdds/Generated/Materials/Item_{rarity}.mat";
                Material mat = AssetDatabase.LoadAssetAtPath<Material>(materialPath);

                if (mat != null)
                {
                    GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                    sphere.name = $"Test_{rarity}";
                    sphere.transform.position = spawnPos;
                    sphere.GetComponent<MeshRenderer>().material = mat;

                    spawnPos.x += spacing;
                }
            }

            Debug.Log("[Item Materials] Created test spheres with materials");
        }

        private void ApplyMaterialsToExistingPrefabs()
        {
            string[] prefabGuids = AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/FatalOdds/Generated/Prefabs" });
            int updated = 0;

            foreach (string guid in prefabGuids)
            {
                string prefabPath = AssetDatabase.GUIDToAssetPath(guid);
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);

                if (prefab != null)
                {
                    // FIXED: Changed from ItemPickup to UniversalItemPickup
                    var pickup = prefab.GetComponent<ItemPickup>();
                    if (pickup != null && pickup.GetItemDefinition() != null)
                    {
                        string materialPath = $"Assets/FatalOdds/Generated/Materials/Item_{pickup.GetItemDefinition().rarity}.mat";
                        Material material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);

                        if (material != null)
                        {
                            var renderers = prefab.GetComponentsInChildren<MeshRenderer>();
                            foreach (var renderer in renderers)
                            {
                                if (renderer.gameObject.name.Contains("Visual") ||
                                    renderer.gameObject.name.Contains("Model") ||
                                    renderer.gameObject.name.Contains("DefaultMesh"))  // Added for UniversalItemPickup
                                {
                                    renderer.sharedMaterial = material;
                                    updated++;
                                    break;
                                }
                            }
                            EditorUtility.SetDirty(prefab);
                        }
                    }
                }
            }

            AssetDatabase.SaveAssets();
            EditorUtility.DisplayDialog("Materials Applied",
                $"Updated materials on {updated} prefabs!", "OK");
        }
    }
}
#endif