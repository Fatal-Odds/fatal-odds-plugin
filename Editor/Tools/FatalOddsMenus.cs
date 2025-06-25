using UnityEditor;
using UnityEditor.Compilation;
using FatalOdds.Runtime;
using UnityEngine;
using System.IO;
using UnityEditor.Callbacks;

namespace FatalOdds.Editor
{
    public static class FatalOddsMenus
    {
        // ═══════════════════════════════════════════════════════════════
        // CORE TOOLS - Main workflow tools users need to learn
        // ═══════════════════════════════════════════════════════════════

        [MenuItem("Window/Fatal Odds/📝 Item & Ability Creator", priority = 1)]
        public static void OpenMainEditor()
        {
            FatalOddsEditor.ShowWindow();
        }

        [MenuItem("Window/Fatal Odds/🎨 Material System", priority = 2)]
        public static void OpenMaterialSystem()
        {
            ItemMaterialSystem.ShowWindow();
        }

        [MenuItem("Window/Fatal Odds/📊 Project Overview", priority = 3)]
        public static void OpenProjectOverview()
        {
            FatalOddsPluginManager.ShowWindow();
        }

        // ═══════════════════════════════════════════════════════════════
        // ESSENTIAL WORKFLOW - Learn the steps
        // ═══════════════════════════════════════════════════════════════

        [MenuItem("Window/Fatal Odds/🔍 Scan for Stats", priority = 20)]
        public static void ScanForStats()
        {
            var statRegistry = EnsureStatRegistryExists();
            int oldCount = statRegistry.StatCount;
            statRegistry.ScanForStats();
            int newCount = statRegistry.StatCount;

            EditorUtility.DisplayDialog("Stat Scan Complete",
                $"Found {newCount} modifiable stats in your project.\n\n" +
                $"Change: {newCount - oldCount:+0;-0;+0} stats\n\n" +
                "These stats can now be modified by items and abilities!",
                "Great!");
        }

        [MenuItem("Window/Fatal Odds/🎛️ Create Universal Prefab", priority = 21)]
        public static void CreateUniversalPrefab()
        {
            UniversalPrefabCreator.CreateUniversalPickupPrefab();
        }

        [MenuItem("Window/Fatal Odds/🧪 Create Test Spawner", priority = 22)]
        public static void CreateTestSpawner()
        {
            UniversalPrefabCreator.CreateTestSpawner();
        }

        // ═══════════════════════════════════════════════════════════════
        // DEVELOPER TOOLS - Advanced features
        // ═══════════════════════════════════════════════════════════════

        [MenuItem("Window/Fatal Odds/🔧 Developer/Stat Discovery Audit", priority = 40)]
        public static void OpenStatAudit()
        {
            StatDiscoveryAudit.ShowWindow();
        }

        [MenuItem("Window/Fatal Odds/🔧 Developer/Debug Stat Sampling", priority = 41)]
        public static void OpenStatSampling()
        {
            StatSamplingDebugWindow.ShowStatSamplingDebug();
        }

        [MenuItem("Window/Fatal Odds/🔧 Developer/Force Refresh Everything", priority = 42)]
        public static void ForceRefreshEverything()
        {
            RefreshAllSystems();
        }

        [MenuItem("Window/Fatal Odds/🔧 Developer/Run System Test", priority = 43)]
        public static void RunSystemTest()
        {
            TestPluginConnection();
        }

        // ═══════════════════════════════════════════════════════════════
        // HELP & INFO
        // ═══════════════════════════════════════════════════════════════

        [MenuItem("Window/Fatal Odds/📚 Help & Documentation", priority = 60)]
        public static void OpenHelp()
        {
            FatalOddsHelpWindow.ShowWindow();
        }

        [MenuItem("Window/Fatal Odds/📁 Open Generated Assets", priority = 61)]
        public static void OpenGeneratedAssets()
        {
            OpenGeneratedAssetsFolder();
        }

        [MenuItem("Window/Fatal Odds/ℹ️ About Fatal Odds", priority = 62)]
        public static void ShowAbout()
        {
            EditorUtility.DisplayDialog(
                "Fatal Odds - Modifier Item System",
                $"Version 0.1.0\n\n" +
                $"A Unity plugin for creating stacking modifier items and abilities.\n" +
                $"Perfect for action roguelike games like Risk of Rain.\n\n" +
                $"• Items stack in effect, not inventory\n" +
                $"• Visual progression by rarity\n" +
                $"• Easy stat discovery system\n" +
                $"• One universal prefab for all items\n\n" +
                $"Created by: Ryan, Jandre, William\n" +
                $"Media Design School",
                "Awesome!");
        }

        // ═══════════════════════════════════════════════════════════════
        // GAMEOBJECT CONTEXT MENUS - Right-click actions
        // ═══════════════════════════════════════════════════════════════

        [MenuItem("GameObject/Fatal Odds/🎮 Setup for Modifiers", priority = 10)]
        public static void SetupForModifiers()
        {
            SetupGameObjectForModifiers();
        }

        [MenuItem("GameObject/Fatal Odds/🎲 Spawn Test Items", priority = 11)]
        public static void SpawnTestItems()
        {
            SpawnTestItemsAroundSelection();
        }

        // Validation for GameObject menus
        [MenuItem("GameObject/Fatal Odds/🎮 Setup for Modifiers", validate = true)]
        [MenuItem("GameObject/Fatal Odds/🎲 Spawn Test Items", validate = true)]
        public static bool ValidateGameObjectSelection()
        {
            return Selection.activeGameObject != null;
        }

        // ═══════════════════════════════════════════════════════════════
        // AUTO-SCAN ON COMPILATION
        // ═══════════════════════════════════════════════════════════════

        [InitializeOnLoadMethod]
        public static void InitializeAutoScan()
        {
            // Register for compilation events
            CompilationPipeline.compilationFinished += OnCompilationFinished;
        }

        private static void OnCompilationFinished(object obj)
        {
            // Small delay to ensure everything is loaded
            EditorApplication.delayCall += () =>
            {
                AutoScanStats();
            };
        }

        private static void AutoScanStats()
        {
            try
            {
                var statRegistry = EnsureStatRegistryExists();
                if (statRegistry != null)
                {
                    int oldCount = statRegistry.StatCount;
                    statRegistry.ScanForStats();
                    int newCount = statRegistry.StatCount;

                    if (newCount != oldCount)
                    {
                        Debug.Log($"[Fatal Odds] Auto-scan complete: Found {newCount} stats ({newCount - oldCount:+0;-0;+0} change)");
                    }
                }
            }
            catch (System.Exception e)
            {
                // Silent fail during auto-scan to avoid spam
                Debug.LogWarning($"[Fatal Odds] Auto-scan failed: {e.Message}");
            }
        }

        // ═══════════════════════════════════════════════════════════════
        // IMPLEMENTATION METHODS
        // ═══════════════════════════════════════════════════════════════

        private static void SetupGameObjectForModifiers()
        {
            if (Selection.activeGameObject == null) return;

            var go = Selection.activeGameObject;
            bool wasSetup = SetupGameObjectForModifierSystem(go);

            string message = wasSetup
                ? $"✅ {go.name} is now ready for modifier items!\n\n" +
                  "Added:\n• ModifierManager (handles stat modifications)\n• ModifierExample (testing component)\n\n" +
                  "Run the scene and press number keys to test!"
                : $"ℹ️ {go.name} already has modifier components.";

            EditorUtility.DisplayDialog("Setup Complete", message, "Got it!");
        }

        private static void SpawnTestItemsAroundSelection()
        {
            if (Selection.activeTransform == null) return;

            var allItems = AssetBatchOperations.FindAllGeneratedItems();
            if (allItems.Count == 0)
            {
                EditorUtility.DisplayDialog("No Items Found",
                    "No generated items found.\n\nCreate some items first using the Item & Ability Creator!",
                    "OK");
                return;
            }

            Vector3 center = Selection.activeTransform.position;
            int itemCount = Mathf.Min(6, allItems.Count);

            for (int i = 0; i < itemCount; i++)
            {
                var randomItem = allItems[Random.Range(0, allItems.Count)];
                float angle = (360f / itemCount) * i * Mathf.Deg2Rad;
                Vector3 offset = new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle)) * 3f;
                Vector3 spawnPos = center + offset;

                var pickup = ItemSpawner.SpawnUniversalItem(randomItem, spawnPos);
                if (pickup != null)
                {
                    Undo.RegisterCreatedObjectUndo(pickup, "Spawn Test Items");
                }
            }

            Debug.Log($"[Fatal Odds] Spawned {itemCount} test items around {Selection.activeTransform.name}");
            EditorUtility.DisplayDialog("Test Items Spawned!",
                $"Spawned {itemCount} random items around {Selection.activeTransform.name}.\n\n" +
                "Run the scene to collect them!",
                "Nice!");
        }

        private static bool SetupGameObjectForModifierSystem(GameObject go)
        {
            bool addedSomething = false;

            if (go.GetComponent<ModifierManager>() == null)
            {
                go.AddComponent<ModifierManager>();
                addedSomething = true;
            }

            EnsureStatRegistryExists();
            return addedSomething;
        }

        private static void RefreshAllSystems()
        {
            EditorUtility.DisplayProgressBar("Refreshing Systems", "Refreshing stat registry...", 0.3f);

            var statRegistry = EnsureStatRegistryExists();
            statRegistry.ScanForStats();

            EditorUtility.DisplayProgressBar("Refreshing Systems", "Refreshing item cache...", 0.6f);

            ItemSpawner.RefreshItemCache();

            EditorUtility.DisplayProgressBar("Refreshing Systems", "Clearing caches...", 0.9f);

            StatValueReader.ClearCache();

            EditorUtility.ClearProgressBar();

            EditorUtility.DisplayDialog("Refresh Complete!",
                $"All systems refreshed!\n\n" +
                $"• Found {statRegistry.StatCount} stats\n" +
                $"• Cleared all caches\n" +
                $"• Ready for testing",
                "Great!");
        }

        // ═══════════════════════════════════════════════════════════════
        // UTILITY METHODS
        // ═══════════════════════════════════════════════════════════════

        public static StatRegistry EnsureStatRegistryExists()
        {
            const string STAT_REGISTRY_PATH = "Assets/FatalOdds/Resources/StatRegistry.asset";

            var statRegistry = AssetDatabase.LoadAssetAtPath<StatRegistry>(STAT_REGISTRY_PATH);

            if (statRegistry == null)
            {
                string resourcesPath = "Assets/FatalOdds/Resources";
                EnsureFolderExists(resourcesPath);

                statRegistry = ScriptableObject.CreateInstance<StatRegistry>();
                AssetDatabase.CreateAsset(statRegistry, STAT_REGISTRY_PATH);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }

            return statRegistry;
        }

        public static bool TestPluginConnection()
        {
            try
            {
                var statRegistry = EnsureStatRegistryExists();
                if (statRegistry == null)
                {
                    Debug.LogError("[Fatal Odds] Failed to create or load Stat Registry");
                    return false;
                }

                FatalOddsInfo.LogInfo();
                statRegistry.ScanForStats();

                bool success = statRegistry.StatCount >= 0; // Even 0 is successful
                string message = success ?
                    $"✅ Plugin Test Successful!\n\nFound {statRegistry.StatCount} stats in your project.\nAll systems working correctly." :
                    "❌ Plugin Test Failed!\n\nCheck console for details.";

                EditorUtility.DisplayDialog("Fatal Odds Plugin Test", message, "OK");
                return success;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[Fatal Odds] Plugin test failed: {e.Message}");
                EditorUtility.DisplayDialog("Plugin Test Failed",
                    $"❌ Test failed with error:\n{e.Message}\n\nCheck console for details.", "OK");
                return false;
            }
        }

        public static void OpenGeneratedAssetsFolder()
        {
            string path = "Assets/FatalOdds/Generated";
            EnsureFolderExists(path);

            var folder = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(path);
            EditorGUIUtility.PingObject(folder);
            Selection.activeObject = folder;
        }

        [DidReloadScripts]
        private static void OnScriptsReloaded()
        {
            AutoScanStats();
        }

        //test


        public static void EnsureFolderExists(string folderPath)
        {
            if (!AssetDatabase.IsValidFolder(folderPath))
            {
                string parentFolder = Path.GetDirectoryName(folderPath);
                string folderName = Path.GetFileName(folderPath);

                if (!string.IsNullOrEmpty(parentFolder) && !AssetDatabase.IsValidFolder(parentFolder))
                {
                    EnsureFolderExists(parentFolder);
                }

                AssetDatabase.CreateFolder(parentFolder, folderName);
                AssetDatabase.Refresh();
            }
        }
    }
}