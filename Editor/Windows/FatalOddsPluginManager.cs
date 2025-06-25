using UnityEngine;
using UnityEditor;
using FatalOdds.Runtime;
using System.Collections.Generic;
using System.Linq;

namespace FatalOdds.Editor
{
    
    /// Project dashboard showing plugin status, statistics, and quick diagnostics
    /// This is your "control center" to see what's happening with your modifier system
    
    public class FatalOddsPluginManager : EditorWindow
    {
        // This window is only called from the unified menu - no duplicate menu item
        public static void ShowWindow()
        {
            var window = GetWindow<FatalOddsPluginManager>("Fatal Odds Overview");
            window.minSize = new Vector2(600, 500);
            window.Show();
        }

        private Vector2 scrollPosition;
        private StatRegistry statRegistry;
        private int selectedTab = 0;
        private readonly string[] tabNames = { "📊 Project Status", "📈 Statistics", "🔧 Diagnostics" };

        // Plugin metadata
        private const string PLUGIN_NAME = "Fatal Odds - Project Overview";
        private const string VERSION = "0.1.0";

        private void OnEnable()
        {
            LoadStatRegistry();
        }

        private void LoadStatRegistry()
        {
            statRegistry = Resources.Load<StatRegistry>("StatRegistry");
            if (statRegistry == null)
            {
                // Use the unified menu's stat registry creation
                statRegistry = FatalOddsMenus.EnsureStatRegistryExists();
            }
        }

        private void OnGUI()
        {
            DrawHeader();
            DrawTabs();

            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

            switch (selectedTab)
            {
                case 0: DrawProjectStatusTab(); break;
                case 1: DrawStatisticsTab(); break;
                case 2: DrawDiagnosticsTab(); break;
            }

            EditorGUILayout.EndScrollView();
            DrawFooter();
        }

        private void DrawHeader()
        {
            EditorGUILayout.Space(10);

            // Clear purpose header - use full width for longer text
            GUIStyle titleStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 18,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                wordWrap = true
            };

            GUIStyle subtitleStyle = new GUIStyle(EditorStyles.miniLabel)
            {
                alignment = TextAnchor.MiddleCenter,
                wordWrap = true,
                normal = { textColor = Color.gray }
            };

            // Use full width layout instead of flexible space
            EditorGUILayout.LabelField("📊 Project Overview Dashboard", titleStyle, GUILayout.ExpandWidth(true));
            EditorGUILayout.LabelField("Monitor your modifier system's health and statistics", subtitleStyle, GUILayout.ExpandWidth(true));

            EditorGUILayout.Space(5);
        }

        private void DrawTabs()
        {
            selectedTab = GUILayout.Toolbar(selectedTab, tabNames);
            EditorGUILayout.Space(10);
        }

        private void DrawProjectStatusTab()
        {
            // Overall health check
            EditorGUILayout.LabelField("🏥 System Health Check", EditorStyles.boldLabel);
            EditorGUILayout.BeginVertical("box");

            bool allGood = true;

            if (statRegistry != null)
            {
                EditorGUILayout.LabelField("✅ Stat Registry: Healthy", EditorStyles.label);
                EditorGUILayout.LabelField($"   └─ {statRegistry.StatCount} stats discovered", EditorStyles.miniLabel);
                EditorGUILayout.LabelField($"   └─ {statRegistry.GetCategories().Count} categories", EditorStyles.miniLabel);
            }
            else
            {
                EditorGUILayout.LabelField("❌ Stat Registry: Missing", new GUIStyle(EditorStyles.label) { normal = { textColor = Color.red } });
                allGood = false;
            }

            // Check for generated assets
            var items = AssetBatchOperations.FindAllGeneratedItems();
            var abilities = AssetBatchOperations.FindAllGeneratedAbilities();

            if (items.Count > 0 || abilities.Count > 0)
            {
                EditorGUILayout.LabelField("✅ Generated Assets: Found", EditorStyles.label);
                EditorGUILayout.LabelField($"   └─ {items.Count} items, {abilities.Count} abilities", EditorStyles.miniLabel);
            }
            else
            {
                EditorGUILayout.LabelField("⚠️ Generated Assets: None yet", new GUIStyle(EditorStyles.label) { normal = { textColor = Color.yellow } });
                EditorGUILayout.LabelField("   └─ This is normal for new projects", EditorStyles.miniLabel);
            }

            // Check for modifier managers in scene
            var modifierManagers = FindObjectsOfType<ModifierManager>();
            if (modifierManagers.Length > 0)
            {
                EditorGUILayout.LabelField("✅ Active ModifierManagers: Found", EditorStyles.label);
                EditorGUILayout.LabelField($"   └─ {modifierManagers.Length} in current scene", EditorStyles.miniLabel);
            }
            else
            {
                EditorGUILayout.LabelField("ℹ️ Active ModifierManagers: None in scene", EditorStyles.label);
                EditorGUILayout.LabelField("   └─ Add one to test your modifier system", EditorStyles.miniLabel);
            }

            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(10);

            // Quick status summary
            string overallStatus = allGood ? "🟢 All Systems Operational" : "🟡 Setup Required";
            Color statusColor = allGood ? Color.green : Color.yellow;

            EditorGUILayout.LabelField("Overall Status:", EditorStyles.boldLabel);
            EditorGUILayout.LabelField(overallStatus, new GUIStyle(EditorStyles.boldLabel) { normal = { textColor = statusColor } });

            EditorGUILayout.Space(10);

            // Quick actions based on status
            EditorGUILayout.LabelField("🚀 Quick Actions", EditorStyles.boldLabel);

            if (statRegistry == null || statRegistry.StatCount == 0)
            {
                EditorGUILayout.HelpBox("Looks like you need to scan for stats first!", MessageType.Info);
                if (GUILayout.Button("🔍 Scan for Stats Now", GUILayout.Height(30)))
                {
                    if (statRegistry != null)
                    {
                        statRegistry.ScanForStats();
                        EditorUtility.DisplayDialog("Scan Complete", $"Found {statRegistry.StatCount} stats!", "Great!");
                    }
                }
            }
            else if (items.Count == 0 && abilities.Count == 0)
            {
                EditorGUILayout.HelpBox("Ready to create your first modifier items!", MessageType.Info);
                if (GUILayout.Button("🎨 Open Item Creator", GUILayout.Height(30)))
                {
                    FatalOddsEditor.ShowWindow();
                }
            }
            else
            {
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("🎨 Create More Items", GUILayout.Height(25)))
                {
                    FatalOddsEditor.ShowWindow();
                }
                if (GUILayout.Button("📁 View Generated Assets", GUILayout.Height(25)))
                {
                    selectedTab = 1; // Switch to statistics tab
                }
                EditorGUILayout.EndHorizontal();
            }
        }

        private void DrawStatisticsTab()
        {
            EditorGUILayout.LabelField("📈 Project Statistics", EditorStyles.boldLabel);

            // Stat breakdown
            if (statRegistry != null && statRegistry.StatCount > 0)
            {
                EditorGUILayout.LabelField("📊 Discovered Stats", EditorStyles.boldLabel);
                EditorGUILayout.BeginVertical("box");

                var categories = statRegistry.GetCategories();
                foreach (var category in categories)
                {
                    var statsInCategory = statRegistry.GetStatsByCategory(category);
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField($"📁 {category}", EditorStyles.boldLabel, GUILayout.Width(100));
                    EditorGUILayout.LabelField($"{statsInCategory.Count} stats", GUILayout.Width(60));

                    // Show some example stat names
                    string examples = string.Join(", ", statsInCategory.Take(3).Select(s => s.DisplayName).ToArray());
                    if (statsInCategory.Count > 3) examples += "...";
                    EditorGUILayout.LabelField(examples, EditorStyles.miniLabel);

                    EditorGUILayout.EndHorizontal();
                }

                EditorGUILayout.Space(5);
                EditorGUILayout.LabelField($"📅 Last scan: {statRegistry.LastScanTime}", EditorStyles.miniLabel);
                EditorGUILayout.EndVertical();
            }
            else
            {
                EditorGUILayout.HelpBox("No stats discovered yet. Add [StatTag] attributes to your fields!", MessageType.Info);
            }

            EditorGUILayout.Space(10);

            // Generated assets breakdown
            var items = AssetBatchOperations.FindAllGeneratedItems();
            var abilities = AssetBatchOperations.FindAllGeneratedAbilities();

            EditorGUILayout.LabelField("🎒 Generated Content", EditorStyles.boldLabel);
            EditorGUILayout.BeginVertical("box");

            if (items.Count > 0)
            {
                EditorGUILayout.LabelField($"Items Created: {items.Count}", EditorStyles.boldLabel);

                // Items by rarity
                var itemsByRarity = items.GroupBy(i => i.rarity).OrderBy(g => g.Key);
                foreach (var group in itemsByRarity)
                {
                    string rarityIcon = GetRarityIcon(group.Key);
                    EditorGUILayout.LabelField($"  {rarityIcon} {group.Key}: {group.Count()}", EditorStyles.label);
                }

                // Items by stacking type
                var itemsByStacking = items.GroupBy(i => i.stackingType);
                EditorGUILayout.Space(3);
                EditorGUILayout.LabelField("Stacking Types:", EditorStyles.miniLabel);
                foreach (var group in itemsByStacking)
                {
                    EditorGUILayout.LabelField($"  • {group.Key}: {group.Count()}", EditorStyles.miniLabel);
                }
            }

            if (abilities.Count > 0)
            {
                EditorGUILayout.Space(5);
                EditorGUILayout.LabelField($"Abilities Created: {abilities.Count}", EditorStyles.boldLabel);

                var abilitiesByTarget = abilities.GroupBy(a => a.targetType);
                foreach (var group in abilitiesByTarget)
                {
                    EditorGUILayout.LabelField($"  🎯 {group.Key}: {group.Count()}", EditorStyles.label);
                }
            }

            if (items.Count == 0 && abilities.Count == 0)
            {
                EditorGUILayout.LabelField("No content created yet - time to get started!", EditorStyles.label);
            }

            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(10);

            // Usage statistics
            EditorGUILayout.LabelField("🎮 Runtime Usage", EditorStyles.boldLabel);
            EditorGUILayout.BeginVertical("box");

            var modifierManagers = FindObjectsOfType<ModifierManager>();
            EditorGUILayout.LabelField($"Active ModifierManagers in scene: {modifierManagers.Length}");

            if (modifierManagers.Length > 0)
            {
                EditorGUILayout.LabelField($"GameObjects ready for modifiers: {modifierManagers.Length}");
            }
            else
            {
                EditorGUILayout.LabelField("💡 Tip: Add ModifierManager to GameObjects to use your items!", EditorStyles.miniLabel);
            }

            EditorGUILayout.EndVertical();

            // Quick asset access
            if (items.Count > 0 || abilities.Count > 0)
            {
                EditorGUILayout.Space(10);
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("📁 Open Asset Folder"))
                {
                    FatalOddsMenus.OpenGeneratedAssetsFolder();
                }
                if (GUILayout.Button("📊 Detailed Asset Statistics"))
                {
                    ShowDetailedAssetStatistics();
                }
                EditorGUILayout.EndHorizontal();
            }
        }

        private void DrawDiagnosticsTab()
        {
            EditorGUILayout.LabelField("🔧 System Diagnostics", EditorStyles.boldLabel);

            // Plugin health test
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField("Plugin Connection Test", EditorStyles.boldLabel);

            if (GUILayout.Button("🧪 Run Full System Test", GUILayout.Height(30)))
            {
                bool success = FatalOddsMenus.TestPluginConnection();
                // Results shown in the test function's dialog
            }
            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(5);

            // Quick diagnostic tools
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField("Diagnostic Tools", EditorStyles.boldLabel);

            if (GUILayout.Button("🔍 Audit Untagged Stats"))
            {
                StatDiscoveryAudit.ShowWindow();
            }

            if (GUILayout.Button("📊 Debug Stat Values"))
            {
                StatSamplingDebugWindow.ShowStatSamplingDebug();
            }

            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(5);

            // System information
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField("System Information", EditorStyles.boldLabel);
            EditorGUILayout.LabelField($"Plugin Version: {VERSION}");
            EditorGUILayout.LabelField($"Unity Version: {Application.unityVersion}");

            if (statRegistry != null)
            {
                EditorGUILayout.LabelField($"Stat Registry Path: Assets/FatalOdds/Resources/StatRegistry.asset");
                EditorGUILayout.LabelField($"Registry Status: Loaded Successfully");
            }
            else
            {
                EditorGUILayout.LabelField("Registry Status: Not Found", new GUIStyle(EditorStyles.label) { normal = { textColor = Color.red } });
            }
            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(10);

            // Maintenance tools
            EditorGUILayout.LabelField("🧹 Maintenance", EditorStyles.boldLabel);
            EditorGUILayout.BeginVertical("box");

            if (GUILayout.Button("🔄 Refresh Stat Registry"))
            {
                LoadStatRegistry();
                if (statRegistry != null)
                {
                    statRegistry.ScanForStats();
                    ShowNotification(new GUIContent("Registry refreshed!"));
                }
            }

            if (GUILayout.Button("📁 Create Missing Folders"))
            {
                FatalOddsMenus.EnsureStatRegistryExists();
                FatalOddsMenus.OpenGeneratedAssetsFolder();
                ShowNotification(new GUIContent("Folders created!"));
            }

            EditorGUILayout.EndVertical();
        }

        private string GetRarityIcon(ItemRarity rarity)
        {
            switch (rarity)
            {
                case ItemRarity.Common: return "⚪";
                case ItemRarity.Uncommon: return "🟢";
                case ItemRarity.Rare: return "🔵";
                case ItemRarity.Epic: return "🟣";
                case ItemRarity.Legendary: return "🟡";
                case ItemRarity.Artifact: return "🔴";
                default: return "⚫";
            }
        }

        private void ShowDetailedAssetStatistics()
        {
            string stats = AssetBatchOperations.GetAssetStatistics();
            EditorUtility.DisplayDialog("Detailed Asset Statistics", stats, "OK");
        }
        private void DrawFooter()
        {
            EditorGUILayout.Space(10);
            EditorGUILayout.Separator();
            EditorGUILayout.Space(5);

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("💡 Tip: This dashboard shows your modifier system's health at a glance", EditorStyles.miniLabel);
            GUILayout.FlexibleSpace();
            EditorGUILayout.LabelField($"v{VERSION}", EditorStyles.miniLabel);
            EditorGUILayout.EndHorizontal();
        }
    }
}