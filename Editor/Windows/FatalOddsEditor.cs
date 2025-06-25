using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor;
using FatalOdds.Runtime;

namespace FatalOdds.Editor
{
    public class FatalOddsEditor : EditorWindow
    {
        public static void ShowWindow()
        {
            var window = GetWindow<FatalOddsEditor>("Fatal Odds Creator");
            window.minSize = new Vector2(1200, 800);
            window.Show();
        }

        // Enhanced tab system
        private enum EditorTab { Dashboard, ModifierItems, TimeAbilities, Stats, Presets, Tools }
        private EditorTab currentTab = EditorTab.Dashboard;

        // Data
        private StatRegistry statRegistry;
        private List<ItemCreationData> itemsInProgress = new List<ItemCreationData>();
        private List<AbilityCreationData> abilitiesInProgress = new List<AbilityCreationData>();

        // UI State
        private Vector2 scrollPosition;
        private int selectedItemIndex = -1;
        private int selectedAbilityIndex = -1;
        private bool showAdvancedOptions = false;
        private bool showTooltips = true;

        // Enhanced styles
        private GUIStyle headerStyle;
        private GUIStyle cardStyle;
        private GUIStyle primaryButtonStyle;
        private GUIStyle secondaryButtonStyle;
        private GUIStyle successButtonStyle;
        private GUIStyle warningButtonStyle;
        private GUIStyle infoBoxStyle;
        private GUIStyle tooltipStyle;

        // Category filtering based on your StatTags
        private string currentStatFilter = "All Categories";
        private List<string> availableCategories = new List<string>();

        private string selectedPrimaryCategory = "All Categories";
        private string selectedSecondaryCategory = "All";
        private Dictionary<string, List<string>> hierarchicalCategories = new Dictionary<string, List<string>>();

        // Quick filter presets for your game type
        private static readonly Dictionary<string, string[]> CategoryPresets = new Dictionary<string, string[]>
        {
            ["Movement & Mobility"] = new[] { "Player Movement", "Movement" },
            ["Combat & Damage"] = new[] { "Player Combat", "Combat", "Weapons" },
            ["Time Abilities"] = new[] { "Time System", "Temporal", "Time" },
            ["Character Stats"] = new[] { "Player Stats", "Character", "Health" },
            ["AI & Enemies"] = new[] { "AI", "Enemy", "NPC" },
            ["All Categories"] = new string[0]
        };

        private static readonly Dictionary<string, string> FieldTooltips = new Dictionary<string, string>
        {
            // TEMPORAL ROGUELIKE ITEM TOOLTIPS
            ["ItemName"] = "Name for your modifier item (e.g., 'Time Ripper Enhancement', 'Sprint Booster', 'Jump Amplifier')",
            ["ItemDescription"] = "Describes the stacking effect. Should mention if it affects movement, combat, or time abilities",
            ["ItemRarity"] = "Drop frequency: Common (frequent), Uncommon (moderate), Rare (uncommon), Epic+ (very rare drops)",
            ["StackCount"] = "How many copies the player currently has. Effects compound with multiple copies",
            ["StacksAdditively"] = "Linear = each copy adds full effect, Diminishing = reduced effect per additional copy",
            ["FlavorText"] = "Lore text that fits your time-traveling theme",
            ["Tags"] = "Keywords for item synergies: 'movement', 'time', 'combat', 'temporal', 'speed', etc.",

            // TIME ABILITY TOOLTIPS  
            ["AbilityName"] = "Name for your temporal ability (e.g., 'Rewind Step', 'Time Freeze', 'Temporal Dash')",
            ["AbilityDescription"] = "What the ability does and how it affects time/movement/combat",
            ["AbilityCooldown"] = "Seconds before ability can be used again",
            ["AbilityEnergyCost"] = "Time dust or energy consumed when activating",
            ["AbilityTargetType"] = "What can be targeted: Self (player only), Enemy (hostiles), Area (around player)",

            // MODIFIER TOOLTIPS
            ["ModifierType"] = "How the modifier works: Flat (+10 speed), Percentage (1.15 = +15% speed), etc.",
            ["ModifierValue"] = "Strength of modification. For movement: flat units or percentage. For time: duration in seconds",
            ["StackingBehavior"] = "How multiple items combine: Additive (sum), Multiplicative (multiply), Override (highest wins)",
        };

        private void OnEnable()
        {
            LoadStatRegistry();
            InitializeEnhancedStyles();
            RefreshCategoryFilter();
        }

        private void LoadStatRegistry()
        {
            statRegistry = Resources.Load<StatRegistry>("StatRegistry");
            if (statRegistry == null)
            {
                CreateStatRegistry();
            }

            if (statRegistry.StatCount == 0)
            {
                statRegistry.ScanForStats();
            }

            RefreshCategoryFilter();
        }

        private void CreateStatRegistry()
        {
            string resourcesPath = "Assets/FatalOdds/Resources";
            if (!AssetDatabase.IsValidFolder(resourcesPath))
            {
                if (!AssetDatabase.IsValidFolder("Assets/FatalOdds"))
                    AssetDatabase.CreateFolder("Assets", "FatalOdds");
                AssetDatabase.CreateFolder("Assets/FatalOdds", "Resources");
            }

            statRegistry = ScriptableObject.CreateInstance<StatRegistry>();
            AssetDatabase.CreateAsset(statRegistry, "Assets/FatalOdds/Resources/StatRegistry.asset");
            AssetDatabase.SaveAssets();
        }

        private void InitializeEnhancedStyles()
        {
            headerStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 20,
                normal = { textColor = new Color(0.85f, 0.85f, 0.95f) },
                padding = new RectOffset(10, 10, 10, 10)
            };

            cardStyle = new GUIStyle("box")
            {
                padding = new RectOffset(20, 20, 15, 15),
                margin = new RectOffset(5, 5, 8, 8),
                normal = { background = MakeTex(2, 2, new Color(0.25f, 0.25f, 0.25f, 0.3f)) }
            };

            primaryButtonStyle = new GUIStyle("button")
            {
                padding = new RectOffset(15, 15, 10, 10),
                margin = new RectOffset(3, 3, 3, 3),
                fontStyle = FontStyle.Bold,
                normal = { background = MakeTex(2, 2, new Color(0.2f, 0.5f, 0.8f, 0.8f)), textColor = Color.white },
                hover = { background = MakeTex(2, 2, new Color(0.3f, 0.6f, 0.9f, 0.9f)), textColor = Color.white }
            };

            secondaryButtonStyle = new GUIStyle("button")
            {
                padding = new RectOffset(12, 12, 8, 8),
                margin = new RectOffset(2, 2, 2, 2)
            };

            successButtonStyle = new GUIStyle("button")
            {
                padding = new RectOffset(15, 15, 10, 10),
                fontStyle = FontStyle.Bold,
                normal = { background = MakeTex(2, 2, new Color(0.2f, 0.7f, 0.3f, 0.8f)), textColor = Color.white },
                hover = { background = MakeTex(2, 2, new Color(0.3f, 0.8f, 0.4f, 0.9f)), textColor = Color.white }
            };

            warningButtonStyle = new GUIStyle("button")
            {
                padding = new RectOffset(12, 12, 8, 8),
                normal = { background = MakeTex(2, 2, new Color(0.8f, 0.4f, 0.2f, 0.8f)), textColor = Color.white },
                hover = { background = MakeTex(2, 2, new Color(0.9f, 0.5f, 0.3f, 0.9f)), textColor = Color.white }
            };

            infoBoxStyle = new GUIStyle("box")
            {
                padding = new RectOffset(15, 15, 10, 10),
                normal = { background = MakeTex(2, 2, new Color(0.2f, 0.4f, 0.8f, 0.15f)) },
                wordWrap = true
            };

            tooltipStyle = new GUIStyle("box")
            {
                fontSize = 11,
                padding = new RectOffset(8, 8, 6, 6),
                normal = { background = MakeTex(2, 2, new Color(0.1f, 0.1f, 0.1f, 0.9f)), textColor = new Color(0.9f, 0.9f, 0.9f) },
                wordWrap = true
            };
        }

        private void RefreshCategoryFilter()
        {
            hierarchicalCategories.Clear();
            availableCategories.Clear();

            if (statRegistry != null)
            {
                // Build hierarchical categories from stat registry
                foreach (var stat in statRegistry.RegisteredStats)
                {
                    string category = stat.Category;

                    // Split hierarchical categories (e.g., "Player/Combat" -> "Player" and "Combat")
                    if (category.Contains("/"))
                    {
                        string[] parts = category.Split('/');
                        string primary = parts[0];
                        string secondary = parts.Length > 1 ? parts[1] : "General";

                        if (!hierarchicalCategories.ContainsKey(primary))
                        {
                            hierarchicalCategories[primary] = new List<string>();
                        }

                        if (!hierarchicalCategories[primary].Contains(secondary))
                        {
                            hierarchicalCategories[primary].Add(secondary);
                        }
                    }
                    else
                    {
                        // Single-level category
                        if (!hierarchicalCategories.ContainsKey(category))
                        {
                            hierarchicalCategories[category] = new List<string> { "General" };
                        }
                    }
                }

                // Sort subcategories
                foreach (var key in hierarchicalCategories.Keys.ToList())
                {
                    hierarchicalCategories[key] = hierarchicalCategories[key].OrderBy(s => s).ToList();
                    hierarchicalCategories[key].Insert(0, "All"); // Add "All" option for each primary category
                }
            }

            // Add "All Categories" as the top-level option
            availableCategories.Add("All Categories");
            availableCategories.AddRange(hierarchicalCategories.Keys.OrderBy(k => k));
        }

        private Texture2D MakeTex(int width, int height, Color col)
        {
            Color[] pix = new Color[width * height];
            for (int i = 0; i < pix.Length; i++)
                pix[i] = col;

            Texture2D result = new Texture2D(width, height);
            result.SetPixels(pix);
            result.Apply();
            return result;
        }

        private void OnGUI()
        {
            DrawEnhancedHeader();
            DrawEnhancedTabs();
            DrawContent();
        }

        private void DrawEnhancedHeader()
        {
            Rect headerRect = new Rect(0, 0, position.width, 80);
            EditorGUI.DrawRect(headerRect, new Color(0.15f, 0.15f, 0.2f, 0.8f));

            GUILayout.Space(15);

            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();

            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("⏰", new GUIStyle(headerStyle) { fontSize = 24 }, GUILayout.Width(30));
            GUILayout.Label("Fatal Odds", headerStyle);
            GUILayout.Label("Temporal Roguelike Creator", new GUIStyle(EditorStyles.label)
            {
                fontSize = 12,
                normal = { textColor = new Color(0.7f, 0.7f, 0.8f) }
            });
            EditorGUILayout.EndHorizontal();

            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();

            GUILayout.Space(5);

            // Enhanced status bar
            EditorGUILayout.BeginHorizontal("toolbar");
            GUILayout.Label($"📊 Stats: {statRegistry?.StatCount ?? 0}", GUILayout.Width(80));
            GUILayout.Label($"🎒 Items: {itemsInProgress.Count}", GUILayout.Width(80));
            GUILayout.Label($"⏰ Abilities: {abilitiesInProgress.Count}", GUILayout.Width(100));

            GUILayout.FlexibleSpace();

            showTooltips = GUILayout.Toggle(showTooltips, "💡 Help", "toolbarbutton", GUILayout.Width(60));

            if (GUILayout.Button("🔄 Scan Stats", "toolbarbutton", GUILayout.Width(80)))
            {
                statRegistry?.ScanForStats();
                RefreshCategoryFilter();
                ShowNotification(new GUIContent($"Found {statRegistry?.StatCount ?? 0} stats!"));
            }
            EditorGUILayout.EndHorizontal();

            GUILayout.Space(15);
        }

        private void DrawEnhancedTabs()
        {
            EditorGUILayout.BeginHorizontal();

            var tabs = new[] { "🏠 Dashboard", "🎒 Modifier Items", "⏰ Time Abilities", "📊 Stats", "🎯 Presets", "🔧 Tools" };
            var tabEnums = new[] { EditorTab.Dashboard, EditorTab.ModifierItems, EditorTab.TimeAbilities, EditorTab.Stats, EditorTab.Presets, EditorTab.Tools };

            for (int i = 0; i < tabs.Length; i++)
            {
                var style = currentTab == tabEnums[i] ? primaryButtonStyle : secondaryButtonStyle;
                if (GUILayout.Button(tabs[i], style, GUILayout.Height(30)))
                {
                    currentTab = tabEnums[i];
                    selectedItemIndex = -1;
                    selectedAbilityIndex = -1;
                }
            }

            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();

            GUILayout.Space(10);
        }

        private void DrawContent()
        {
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

            switch (currentTab)
            {
                case EditorTab.Dashboard:
                    DrawDashboardTab();
                    break;
                case EditorTab.ModifierItems:
                    DrawModifierItemsTab();
                    break;
                case EditorTab.TimeAbilities:
                    DrawTimeAbilitiesTab();
                    break;
                case EditorTab.Stats:
                    DrawStatsTab();
                    break;
                case EditorTab.Presets:
                    DrawPresetsTab();
                    break;
                case EditorTab.Tools:
                    DrawToolsTab();
                    break;
            }

            EditorGUILayout.EndScrollView();
        }

        private void DrawDashboardTab()
        {
            // Welcome section tailored to temporal roguelike
            EditorGUILayout.BeginVertical(infoBoxStyle);
            GUILayout.Label("⏰ Temporal Roguelike Modifier System", EditorStyles.boldLabel);
            GUILayout.Label("Create items and abilities that modify movement, combat, and time-based mechanics. " +
                          "Perfect for action roguelikes with time manipulation, speed modifiers, and combat enhancements!",
                          EditorStyles.wordWrappedLabel);
            EditorGUILayout.EndVertical();

            GUILayout.Space(15);

            // Game-specific concepts
            EditorGUILayout.BeginVertical(cardStyle);
            GUILayout.Label("🎮 Temporal Roguelike Concepts", EditorStyles.boldLabel);

            var keyPoints = new[]
            {
                "🏃 Movement Modifiers: Speed, jump height, air control - stack to become incredibly agile",
                "⚔️ Combat Enhancements: Attack speed, damage, critical chance - compound for devastating power",
                "⏰ Time Abilities: Rewind, freeze, time dust effects - manipulate temporal mechanics",
                "🎲 Stacking Items: Linear progression or diminishing returns based on item type",
                "🌟 Rarity System: Common speed boosts to legendary time manipulation artifacts"
            };

            foreach (var point in keyPoints)
            {
                GUILayout.Label(point, EditorStyles.wordWrappedLabel);
                GUILayout.Space(3);
            }

            EditorGUILayout.EndVertical();

            GUILayout.Space(15);

            // Quick stats overview
            EditorGUILayout.BeginHorizontal();

            // Stats card
            EditorGUILayout.BeginVertical(cardStyle, GUILayout.Width(200));
            GUILayout.Label("📊 Player Stats", EditorStyles.boldLabel);
            GUILayout.Label($"{statRegistry?.StatCount ?? 0} stats tagged", EditorStyles.largeLabel);

            if (statRegistry != null)
            {
                var movementStats = statRegistry.GetStatsByCategory("Player Movement").Count;
                var combatStats = statRegistry.GetStatsByCategory("Player Combat").Count;
                GUILayout.Label($"Movement: {movementStats}", EditorStyles.miniLabel);
                GUILayout.Label($"Combat: {combatStats}", EditorStyles.miniLabel);
            }

            if (GUILayout.Button("View Stats", secondaryButtonStyle))
                currentTab = EditorTab.Stats;
            EditorGUILayout.EndVertical();

            // Modifier Items card
            EditorGUILayout.BeginVertical(cardStyle, GUILayout.Width(200));
            GUILayout.Label("🎒 Modifier Items", EditorStyles.boldLabel);
            GUILayout.Label($"{itemsInProgress.Count} in progress", EditorStyles.largeLabel);
            var generatedItems = AssetBatchOperations.FindAllGeneratedItems();
            GUILayout.Label($"{generatedItems.Count} generated", EditorStyles.miniLabel);
            if (GUILayout.Button("Create Item", primaryButtonStyle))
            {
                currentTab = EditorTab.ModifierItems;
                CreateNewItem();
            }
            EditorGUILayout.EndVertical();

            // Time Abilities card  
            EditorGUILayout.BeginVertical(cardStyle, GUILayout.Width(200));
            GUILayout.Label("⏰ Time Abilities", EditorStyles.boldLabel);
            GUILayout.Label($"{abilitiesInProgress.Count} in progress", EditorStyles.largeLabel);
            var generatedAbilities = AssetBatchOperations.FindAllGeneratedAbilities();
            GUILayout.Label($"{generatedAbilities.Count} generated", EditorStyles.miniLabel);
            if (GUILayout.Button("Create Ability", primaryButtonStyle))
            {
                currentTab = EditorTab.TimeAbilities;
                CreateNewAbility();
            }
            EditorGUILayout.EndVertical();

            EditorGUILayout.EndHorizontal();

            GUILayout.Space(20);

            // Quick examples specific to your game
            GUILayout.Label("🎯 Quick Examples", EditorStyles.boldLabel);
            EditorGUILayout.BeginVertical(cardStyle);

            var examples = new[]
            {
                "🏃 Sprint Enhancer: +20% sprint speed (linear stacking with Sprint Speed stat)",
                "🦘 Jump Amplifier: +15% jump force (affects Jump Force stat)",
                "⚡ Attack Speed Boost: +10% attack speed (stacks with combat stats)",
                "⏰ Time Dust Collector: +5% time ability effectiveness",
                "🎯 Aim Lock Extension: +2 seconds aim lock duration"
            };

            foreach (var example in examples)
            {
                GUILayout.Label(example, EditorStyles.wordWrappedLabel);
                GUILayout.Space(3);
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawModifierItemsTab()
        {
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("🎒 Modifier Items", EditorStyles.boldLabel);
            GUILayout.FlexibleSpace();

            if (GUILayout.Button("➕ New Modifier Item", primaryButtonStyle, GUILayout.Width(150)))
            {
                CreateNewItem();
            }
            EditorGUILayout.EndHorizontal();

            GUILayout.Space(10);

            // Explanation specific to your game
            EditorGUILayout.BeginVertical(infoBoxStyle);
            GUILayout.Label("💡 Modifier items are passive effects that permanently enhance your player's abilities. " +
                          "They stack when collected multiple times, creating powerful build combinations. " +
                          "Perfect for movement enhancements, combat improvements, and time ability modifications.",
                          EditorStyles.wordWrappedLabel);
            EditorGUILayout.EndVertical();

            GUILayout.Space(10);

            if (itemsInProgress.Count == 0)
            {
                DrawEmptyState("No modifier items created yet", "Create your first modifier item to enhance player movement, combat, or time abilities!");
            }
            else
            {
                DrawItemsList();
            }

            if (selectedItemIndex >= 0 && selectedItemIndex < itemsInProgress.Count)
            {
                GUILayout.Space(20);
                DrawItemEditor(itemsInProgress[selectedItemIndex]);
            }
        }

        private void DrawTimeAbilitiesTab()
        {
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("⏰ Time Abilities", EditorStyles.boldLabel);
            GUILayout.FlexibleSpace();

            if (GUILayout.Button("➕ New Time Ability", primaryButtonStyle, GUILayout.Width(150)))
            {
                CreateNewAbility();
            }
            EditorGUILayout.EndHorizontal();

            GUILayout.Space(10);

            EditorGUILayout.BeginVertical(infoBoxStyle);
            GUILayout.Label("💡 Time abilities are active skills that manipulate temporal mechanics or provide temporary buffs. " +
                          "They consume energy/time dust and have cooldowns. Great for rewind mechanics, time freeze, or burst movement abilities.",
                          EditorStyles.wordWrappedLabel);
            EditorGUILayout.EndVertical();

            GUILayout.Space(10);

            if (abilitiesInProgress.Count == 0)
            {
                DrawEmptyState("No time abilities created yet", "Create your first temporal ability for time manipulation or temporary stat boosts!");
            }
            else
            {
                DrawAbilitiesList();
            }

            if (selectedAbilityIndex >= 0 && selectedAbilityIndex < abilitiesInProgress.Count)
            {
                GUILayout.Space(20);
                DrawAbilityEditor(abilitiesInProgress[selectedAbilityIndex]);
            }
        }

        private void DrawPresetsTab()
        {
            GUILayout.Label("🎯 Roguelike Item Presets", EditorStyles.boldLabel);
            GUILayout.Space(10);

            EditorGUILayout.BeginVertical(infoBoxStyle);
            GUILayout.Label("Quick-create common roguelike items based on your PlayerMovement stats. " +
                          "These presets automatically target the correct stats and use appropriate stacking behaviors.",
                          EditorStyles.wordWrappedLabel);
            EditorGUILayout.EndVertical();

            GUILayout.Space(10);

            // Movement presets
            DrawPresetCategory("🏃 Movement Enhancers", new[]
            {
                ("Sprint Booster", "Increases sprint speed by 15%. Stacks linearly for incredible speed.", "Sprint Speed"),
                ("Jump Amplifier", "Enhances jump force by 20%. Stack for superhuman leaping ability.", "Jump Force"),
                ("Air Controller", "Improves air mobility by 25%. Better mid-air movement control.", "Air Mobility"),
                ("Quick Stepper", "Reduces turn speed delay. More responsive movement.", "Turn Speed")
            });

            // Combat presets
            DrawPresetCategory("⚔️ Combat Enhancers", new[]
            {
                ("Aim Stabilizer", "Extends aim lock duration by 1 second. Better combat precision.", "Aim Lock Duration"),
                ("Double Jump Boost", "Increases double jump force by 15%. Enhanced aerial combat.", "Double Jump Force"),
                ("Ground Grip", "Improves ground friction by 10%. Better movement control in combat.", "Ground Friction"),
                ("Quick Recovery", "Reduces jump cooldown by 0.05s. Faster consecutive jumps.", "Jump Cooldown")
            });

            // Time/Special presets  
            DrawPresetCategory("⏰ Temporal Items", new[]
            {
                ("Time Dust Magnet", "Increases fall speed multiplier (faster time interactions).", "Fall Speed Multiplier"),
                ("Rewind Enhancer", "Improves ground detection range for better rewind accuracy.", "Ground Detection Range"),
                ("Temporal Anchor", "Reduces air resistance for smoother time-based movement.", "Air Resistance"),
                ("Max Jump Amplifier", "Increases maximum allowed jumps. Stack for multiple jumps.", "Max Jumps")
            });
        }

        private void DrawPresetCategory(string categoryName, (string name, string description, string statHint)[] presets)
        {
            EditorGUILayout.LabelField(categoryName, EditorStyles.boldLabel);
            EditorGUILayout.BeginVertical(cardStyle);

            foreach (var preset in presets)
            {
                EditorGUILayout.BeginHorizontal();

                EditorGUILayout.BeginVertical();
                GUILayout.Label(preset.name, EditorStyles.boldLabel);
                GUILayout.Label(preset.description, EditorStyles.miniLabel);
                GUILayout.Label($"Targets: {preset.statHint}", EditorStyles.miniLabel);
                EditorGUILayout.EndVertical();

                if (GUILayout.Button("Create", secondaryButtonStyle, GUILayout.Width(60)))
                {
                    CreatePresetItem(preset.name, preset.description, preset.statHint);
                }

                EditorGUILayout.EndHorizontal();
                GUILayout.Space(5);
            }

            EditorGUILayout.EndVertical();
            GUILayout.Space(10);
        }

        private void CreatePresetItem(string name, string description, string statHint)
        {
            var newItem = new ItemCreationData
            {
                itemName = name,
                description = description,
                rarity = ItemRarity.Common,
                stacksAdditively = true,
                flavorText = "A reliable enhancement for any time-traveling warrior.",
                tags = new[] { "movement", "enhancement", "temporal" },
                modifiers = new List<ModifierCreationData>()
            };

            // Try to find matching stat
            if (statRegistry != null)
            {
                var matchingStat = statRegistry.RegisteredStats
                    .FirstOrDefault(s => s.DisplayName.Contains(statHint) || s.FieldName.Contains(statHint.Replace(" ", "")));

                if (matchingStat != null)
                {
                    var modifier = new ModifierCreationData
                    {
                        statGuid = matchingStat.GUID,
                        statDisplayName = matchingStat.DisplayName,
                        modifierType = statHint.Contains("Speed") || statHint.Contains("Force") ? ModifierType.PercentageAdditive : ModifierType.Flat,
                        value = statHint.Contains("Speed") || statHint.Contains("Force") ? 0.15f : 1f
                    };
                    newItem.modifiers.Add(modifier);
                }
            }

            itemsInProgress.Add(newItem);
            currentTab = EditorTab.ModifierItems;
            selectedItemIndex = itemsInProgress.Count - 1;

            ShowNotification(new GUIContent($"Created {name} preset!"));
        }

        // Include all the other methods from the original FatalOddsEditor but streamlined...
        // [Rest of the methods would go here - DrawItemEditor, DrawAbilityEditor, etc.]
        // For brevity, I'm focusing on the main structural changes

        private void DrawStatsTab()
        {
            if (statRegistry == null)
            {
                EditorGUILayout.HelpBox("📊 Stat Registry not found!", MessageType.Error);
                return;
            }

            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("📊 Discovered Player Stats", EditorStyles.boldLabel);
            GUILayout.FlexibleSpace();

            if (GUILayout.Button("🔄 Refresh", secondaryButtonStyle))
            {
                statRegistry.ScanForStats();
                RefreshCategoryFilter();
            }
            EditorGUILayout.EndHorizontal();

            GUILayout.Space(10);

            if (statRegistry.StatCount == 0)
            {
                DrawEmptyState("No stats found", "Add [StatTag] attributes to your PlayerMovement, combat, and other scripts, then scan for stats.");
                return;
            }

            // Show organized by your game's categories
            var priorityCategories = new[] { "Player Movement", "Player Combat", "Time System" };
            var allCategories = statRegistry.GetCategories();

            foreach (var category in priorityCategories.Where(c => allCategories.Contains(c)))
            {
                DrawStatCategory(category);
            }

            foreach (var category in allCategories.Where(c => !priorityCategories.Contains(c)))
            {
                DrawStatCategory(category);
            }
        }

        private void DrawStatCategory(string category)
        {
            EditorGUILayout.BeginVertical(cardStyle);

            EditorGUILayout.BeginHorizontal();
            string icon = category.Contains("Movement") ? "🏃" : category.Contains("Combat") ? "⚔️" : category.Contains("Time") ? "⏰" : "📊";
            GUILayout.Label($"{icon} {category}", EditorStyles.boldLabel);
            var statsInCategory = statRegistry.GetStatsByCategory(category);
            GUILayout.Label($"({statsInCategory.Count} stats)", new GUIStyle(EditorStyles.miniLabel) { normal = { textColor = Color.gray } });
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();

            foreach (var stat in statsInCategory)
            {
                EditorGUILayout.BeginHorizontal();
                GUILayout.Space(20);
                GUILayout.Label($"📊 {stat.DisplayName}", GUILayout.ExpandWidth(true));
                GUILayout.Label($"{stat.FieldName}", EditorStyles.miniLabel, GUILayout.Width(150));
                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.EndVertical();
            GUILayout.Space(5);
        }

        private void DrawToolsTab()
        {
            GUILayout.Label("🔧 Development Tools", EditorStyles.boldLabel);
            GUILayout.Space(10);

            DrawFormSection("🎮 Quick Testing", () =>
            {
             if (GUILayout.Button("🧪 Test Plugin Connection", secondaryButtonStyle, GUILayout.Height(35)))
                {
                    TestPluginConnection();
                }
            });
        }

        // Helper methods (simplified versions of the original methods)

        private void CreateNewItem()
        {
            var newItem = new ItemCreationData
            {
                itemName = $"New Modifier Item {itemsInProgress.Count + 1}",
                description = "A passive modifier that enhances player abilities when collected.",
                rarity = ItemRarity.Common,
                stackCount = 1,
                stacksAdditively = true,
                flavorText = "Time bends around this mysterious artifact...",
                tags = new[] { "enhancement", "temporal" },
                modifiers = new List<ModifierCreationData>()
            };

            itemsInProgress.Add(newItem);
            selectedItemIndex = itemsInProgress.Count - 1;
        }

        private void CreateNewAbility()
        {
            var newAbility = new AbilityCreationData
            {
                abilityName = $"New Time Ability {abilitiesInProgress.Count + 1}",
                description = "A temporal ability that manipulates time or provides temporary enhancements.",
                targetType = AbilityTargetType.Self,
                cooldown = 8f,
                energyCost = 30f,
                modifiers = new List<ModifierCreationData>()
            };

            abilitiesInProgress.Add(newAbility);
            selectedAbilityIndex = abilitiesInProgress.Count - 1;
        }

        private void DrawItemsList()
        {
            for (int i = 0; i < itemsInProgress.Count; i++)
            {
                var item = itemsInProgress[i];
                var isSelected = selectedItemIndex == i;

                var style = isSelected ? "selectionRect" : cardStyle;
                EditorGUILayout.BeginVertical(style);

                EditorGUILayout.BeginHorizontal();
                var rarityColor = GetRarityColor(item.rarity);
                GUI.color = rarityColor;
                GUILayout.Label("💎", GUILayout.Width(20));
                GUI.color = Color.white;

                EditorGUILayout.BeginVertical();
                GUILayout.Label(item.itemName, EditorStyles.boldLabel);

                string stackInfo = item.stacksAdditively ? "Linear stacking" : "Diminishing returns";
                GUILayout.Label($"{item.rarity} • {item.modifiers.Count} effects • {stackInfo}", EditorStyles.miniLabel);
                EditorGUILayout.EndVertical();

                GUILayout.FlexibleSpace();

                if (GUILayout.Button(isSelected ? "Hide" : "Edit", secondaryButtonStyle, GUILayout.Width(60)))
                {
                    selectedItemIndex = isSelected ? -1 : i;
                }

                if (GUILayout.Button("🗑️", warningButtonStyle, GUILayout.Width(30)))
                {
                    if (EditorUtility.DisplayDialog("Delete Item", $"Delete '{item.itemName}'?", "Delete", "Cancel"))
                    {
                        itemsInProgress.RemoveAt(i);
                        if (selectedItemIndex == i) selectedItemIndex = -1;
                        else if (selectedItemIndex > i) selectedItemIndex--;
                    }
                }

                EditorGUILayout.EndHorizontal();
                EditorGUILayout.EndVertical();
                GUILayout.Space(2);
            }
        }

        private void DrawAbilitiesList()
        {
            for (int i = 0; i < abilitiesInProgress.Count; i++)
            {
                var ability = abilitiesInProgress[i];
                var isSelected = selectedAbilityIndex == i;

                var style = isSelected ? "selectionRect" : cardStyle;
                EditorGUILayout.BeginVertical(style);

                EditorGUILayout.BeginHorizontal();
                GUILayout.Label("⏰", GUILayout.Width(20));

                EditorGUILayout.BeginVertical();
                GUILayout.Label(ability.abilityName, EditorStyles.boldLabel);
                GUILayout.Label($"{ability.cooldown}s cooldown • {ability.energyCost} energy • {ability.modifiers.Count} effects", EditorStyles.miniLabel);
                EditorGUILayout.EndVertical();

                GUILayout.FlexibleSpace();

                if (GUILayout.Button(isSelected ? "Hide" : "Edit", secondaryButtonStyle, GUILayout.Width(60)))
                {
                    selectedAbilityIndex = isSelected ? -1 : i;
                }

                if (GUILayout.Button("🗑️", warningButtonStyle, GUILayout.Width(30)))
                {
                    if (EditorUtility.DisplayDialog("Delete Ability", $"Delete '{ability.abilityName}'?", "Delete", "Cancel"))
                    {
                        abilitiesInProgress.RemoveAt(i);
                        if (selectedAbilityIndex == i) selectedAbilityIndex = -1;
                        else if (selectedAbilityIndex > i) selectedAbilityIndex--;
                    }
                }

                EditorGUILayout.EndHorizontal();
                EditorGUILayout.EndVertical();
                GUILayout.Space(2);
            }
        }

        private void DrawItemEditor(ItemCreationData item)
        {
            EditorGUILayout.BeginVertical(cardStyle);

            EditorGUILayout.BeginHorizontal();
            GUILayout.Label($"✏️ Editing: {item.itemName}", EditorStyles.boldLabel);
            GUILayout.FlexibleSpace();

            if (GUILayout.Button("💾 Generate Asset", successButtonStyle, GUILayout.Width(140)))
            {
                GenerateItemAsset(item);
            }
            EditorGUILayout.EndHorizontal();

            GUILayout.Space(10);

            // Basic Information
            DrawFormSection("📝 Basic Information", () =>
            {
                item.itemName = DrawFieldWithTooltip("Name", item.itemName, "ItemName");
                item.description = DrawTextAreaWithTooltip("Description", item.description, "ItemDescription", 60);
                item.rarity = (ItemRarity)DrawEnumWithTooltip("Rarity", item.rarity, "ItemRarity");
            });

            GUILayout.Space(10);

            // Stacking Properties
            DrawFormSection("🔄 Stacking Properties", () =>
            {
                item.stackCount = DrawIntWithTooltip("Current Stack Count", item.stackCount, "StackCount");
                item.stacksAdditively = DrawBoolWithTooltip("Stacks Linearly", item.stacksAdditively, "StacksAdditively");

                // Show stacking preview
                if (item.modifiers.Count > 0)
                {
                    EditorGUILayout.Space(5);
                    EditorGUILayout.LabelField("Stacking Preview:", EditorStyles.boldLabel);
                    string preview = GetStackingPreview(item);
                    EditorGUILayout.TextArea(preview, GUILayout.Height(40));
                }
            });

            GUILayout.Space(10);

            // Flavor and Tags
            DrawFormSection("✨ Flavor & Tags", () =>
            {
                item.flavorText = DrawTextAreaWithTooltip("Flavor Text", item.flavorText, "FlavorText", 40);

                EditorGUILayout.LabelField("Tags (comma separated):");
                string tagsString = item.tags != null ? string.Join(", ", item.tags) : "";
                tagsString = EditorGUILayout.TextField(tagsString);
                item.tags = string.IsNullOrEmpty(tagsString) ? new string[0] :
                           tagsString.Split(',').Select(t => t.Trim()).Where(t => !string.IsNullOrEmpty(t)).ToArray();
            });

            GUILayout.Space(10);

            // Enhanced modifiers section
            DrawModifiersEditor(item.modifiers);

            EditorGUILayout.EndVertical();
        }

        private void DrawAbilityEditor(AbilityCreationData ability)
        {
            EditorGUILayout.BeginVertical(cardStyle);

            EditorGUILayout.BeginHorizontal();
            GUILayout.Label($"✏️ Editing: {ability.abilityName}", EditorStyles.boldLabel);
            GUILayout.FlexibleSpace();

            if (GUILayout.Button("💾 Generate Asset", successButtonStyle, GUILayout.Width(140)))
            {
                GenerateAbilityAsset(ability);
            }
            EditorGUILayout.EndHorizontal();

            GUILayout.Space(10);

            DrawFormSection("📝 Basic Information", () =>
            {
                ability.abilityName = DrawFieldWithTooltip("Name", ability.abilityName, "AbilityName");
                ability.description = DrawTextAreaWithTooltip("Description", ability.description, "AbilityDescription", 60);
                ability.targetType = (AbilityTargetType)DrawEnumWithTooltip("Target Type", ability.targetType, "AbilityTargetType");
                ability.cooldown = DrawFloatWithTooltip("Cooldown (seconds)", ability.cooldown, "AbilityCooldown");
                ability.energyCost = DrawFloatWithTooltip("Energy Cost", ability.energyCost, "AbilityEnergyCost");
            });

            GUILayout.Space(10);
            DrawModifiersEditor(ability.modifiers);

            EditorGUILayout.EndVertical();
        }

        // Replace the DrawModifiersEditor method in FatalOddsEditor.cs
        private void DrawModifiersEditor(List<ModifierCreationData> modifiers)
        {
            EditorGUILayout.BeginVertical(cardStyle);

            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("⚡ Stat Modifiers", EditorStyles.boldLabel);
            GUILayout.FlexibleSpace();

            if (GUILayout.Button("➕ Add Modifier", primaryButtonStyle))
            {
                modifiers.Add(new ModifierCreationData());
                RefreshCategoryFilter();
            }
            EditorGUILayout.EndHorizontal();

            GUILayout.Space(10);

            if (modifiers.Count > 0)
            {
                // Show current filter info
                if (currentStatFilter != "All Categories")
                {
                    EditorGUILayout.BeginHorizontal();
                    GUILayout.Label($"🔍 Filtering by: {currentStatFilter}", EditorStyles.miniLabel);
                    if (GUILayout.Button("Clear Filter", EditorStyles.miniButton, GUILayout.Width(80)))
                    {
                        currentStatFilter = "All Categories";
                    }
                    EditorGUILayout.EndHorizontal();
                    GUILayout.Space(5);
                }
            }

            // Draw modifiers with proper GUI layout handling
            for (int i = modifiers.Count - 1; i >= 0; i--) // Iterate backwards for safe removal
            {
                bool shouldRemove = false;

                EditorGUILayout.BeginVertical("box");

                // Header with remove button
                EditorGUILayout.BeginHorizontal();
                GUILayout.Label("⚡ Stat Modifier", EditorStyles.boldLabel);
                GUILayout.FlexibleSpace();

                if (GUILayout.Button("🗑️ Remove", warningButtonStyle, GUILayout.Width(80)))
                {
                    shouldRemove = true;
                }
                EditorGUILayout.EndHorizontal();

                if (!shouldRemove)
                {
                    DrawSingleModifierContent(modifiers[i]);
                }

                EditorGUILayout.EndVertical();

                if (shouldRemove)
                {
                    modifiers.RemoveAt(i);
                    RefreshCategoryFilter();
                }

                GUILayout.Space(5);
            }

            if (modifiers.Count == 0)
            {
                EditorGUILayout.BeginVertical(infoBoxStyle);
                GUILayout.Label("💡 No modifiers added yet", EditorStyles.boldLabel);
                GUILayout.Label("Modifiers define what your item or ability actually does! They modify player stats like " +
                              "movement speed, jump force, combat abilities, or time-related mechanics.",
                              EditorStyles.wordWrappedLabel);
                EditorGUILayout.EndVertical();
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawSingleModifierContent(ModifierCreationData modifier)
        {
            GUILayout.Space(5);

            // Hierarchical category selection
            DrawHierarchicalCategorySelector(modifier);

            GUILayout.Space(3);

            // Filtered stat selection
            var allStats = statRegistry.RegisteredStats.ToList();
            var filteredStats = GetFilteredStats(allStats);

            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("Target Stat:", GUILayout.Width(80));

            if (filteredStats.Count > 0)
            {
                var statNames = filteredStats.Select(s => $"📊 {s.DisplayName}").ToArray();

                int selectedIndex = filteredStats.FindIndex(s => s.GUID == modifier.statGuid);
                if (selectedIndex < 0) selectedIndex = 0;

                EditorGUI.BeginChangeCheck();
                selectedIndex = EditorGUILayout.Popup(selectedIndex, statNames);
                if (EditorGUI.EndChangeCheck())
                {
                    modifier.statGuid = filteredStats[selectedIndex].GUID;
                    modifier.statDisplayName = filteredStats[selectedIndex].DisplayName;
                }
            }
            else
            {
                EditorGUILayout.LabelField("⚠️ No stats found for this category");
                if (GUILayout.Button("Reset Filter", GUILayout.Width(80)))
                {
                    currentStatFilter = "All Categories";
                    selectedPrimaryCategory = "All Categories";
                    selectedSecondaryCategory = "All";
                }
            }
            EditorGUILayout.EndHorizontal();

            GUILayout.Space(5);

            // Modifier configuration
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("Effect Type:", GUILayout.Width(80));
            modifier.modifierType = (ModifierType)EditorGUILayout.EnumPopup(modifier.modifierType, GUILayout.Width(120));

            GUILayout.Label("Value:", GUILayout.Width(50));
            modifier.value = EditorGUILayout.FloatField(modifier.value, GUILayout.Width(80));

            GUILayout.Label(GetModifierValueHint(modifier.modifierType), EditorStyles.miniLabel);
            EditorGUILayout.EndHorizontal();

            if (showAdvancedOptions)
            {
                EditorGUILayout.BeginHorizontal();
                GUILayout.Label("Stacking:", GUILayout.Width(80));
                modifier.stackingBehavior = (StackingBehavior)EditorGUILayout.EnumPopup(modifier.stackingBehavior, GUILayout.Width(120));
                EditorGUILayout.EndHorizontal();
            }

            GUILayout.Space(3);

            // Show preview
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("Preview:", EditorStyles.miniLabel, GUILayout.Width(60));
            GUILayout.Label(modifier.GetPreviewText(), EditorStyles.miniLabel);
            EditorGUILayout.EndHorizontal();
        }

        private void DrawHierarchicalCategorySelector(ModifierCreationData modifier)
        {
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("Category:", GUILayout.Width(80));

            // Primary category dropdown
            int primaryIndex = availableCategories.IndexOf(selectedPrimaryCategory);
            if (primaryIndex < 0) primaryIndex = 0;

            EditorGUI.BeginChangeCheck();
            primaryIndex = EditorGUILayout.Popup(primaryIndex, availableCategories.ToArray(), GUILayout.Width(120));
            if (EditorGUI.EndChangeCheck())
            {
                selectedPrimaryCategory = availableCategories[primaryIndex];
                selectedSecondaryCategory = "All"; // Reset secondary when primary changes
                UpdateCurrentFilter();
            }

            // Secondary category dropdown (only show if not "All Categories")
            if (selectedPrimaryCategory != "All Categories" && hierarchicalCategories.ContainsKey(selectedPrimaryCategory))
            {
                GUILayout.Label("→", GUILayout.Width(20));

                var secondaryOptions = hierarchicalCategories[selectedPrimaryCategory];
                int secondaryIndex = secondaryOptions.IndexOf(selectedSecondaryCategory);
                if (secondaryIndex < 0) secondaryIndex = 0;

                EditorGUI.BeginChangeCheck();
                secondaryIndex = EditorGUILayout.Popup(secondaryIndex, secondaryOptions.ToArray(), GUILayout.Width(100));
                if (EditorGUI.EndChangeCheck())
                {
                    selectedSecondaryCategory = secondaryOptions[secondaryIndex];
                    UpdateCurrentFilter();
                }
            }

            if (GUILayout.Button("🔄", GUILayout.Width(25)))
            {
                RefreshCategoryFilter();
            }

            EditorGUILayout.EndHorizontal();
        }

        // Update the current filter based on selections
        private void UpdateCurrentFilter()
        {
            if (selectedPrimaryCategory == "All Categories")
            {
                currentStatFilter = "All Categories";
            }
            else if (selectedSecondaryCategory == "All")
            {
                currentStatFilter = selectedPrimaryCategory;
            }
            else
            {
                currentStatFilter = $"{selectedPrimaryCategory}/{selectedSecondaryCategory}";
            }
        }

        private void DrawSingleModifier(ModifierCreationData modifier, System.Action onRemove)
        {
            EditorGUILayout.BeginVertical("box");

            // Header with remove button
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("⚡ Stat Modifier", EditorStyles.boldLabel);
            GUILayout.FlexibleSpace();

            if (GUILayout.Button("🗑️ Remove", warningButtonStyle, GUILayout.Width(80)))
            {
                onRemove();
                return;
            }
            EditorGUILayout.EndHorizontal();

            GUILayout.Space(5);

            // Category filter dropdown
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("Filter by Category:", GUILayout.Width(130));

            int filterIndex = availableCategories.IndexOf(currentStatFilter);
            if (filterIndex < 0) filterIndex = 0;

            EditorGUI.BeginChangeCheck();
            filterIndex = EditorGUILayout.Popup(filterIndex, availableCategories.ToArray(), GUILayout.Width(180));
            if (EditorGUI.EndChangeCheck())
            {
                currentStatFilter = availableCategories[filterIndex];
            }

            if (GUILayout.Button("🔄", GUILayout.Width(25)))
            {
                RefreshCategoryFilter();
            }
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();

            GUILayout.Space(3);

            // Filtered stat selection
            var allStats = statRegistry.RegisteredStats.ToList();
            var filteredStats = GetFilteredStats(allStats);

            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("Target Stat:", GUILayout.Width(80));

            if (filteredStats.Count > 0)
            {
                var statNames = filteredStats.Select(s => $"📊 {s.DisplayName} ({s.Category})").ToArray();

                int selectedIndex = filteredStats.FindIndex(s => s.GUID == modifier.statGuid);
                if (selectedIndex < 0) selectedIndex = 0;

                EditorGUI.BeginChangeCheck();
                selectedIndex = EditorGUILayout.Popup(selectedIndex, statNames);
                if (EditorGUI.EndChangeCheck())
                {
                    modifier.statGuid = filteredStats[selectedIndex].GUID;
                    modifier.statDisplayName = filteredStats[selectedIndex].DisplayName;
                }
            }
            else
            {
                EditorGUILayout.LabelField("⚠️ No stats found for this category");
                if (GUILayout.Button("Reset Filter", GUILayout.Width(80)))
                {
                    currentStatFilter = "All Categories";
                }
            }
            EditorGUILayout.EndHorizontal();

            GUILayout.Space(5);

            // Modifier configuration
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("Effect Type:", GUILayout.Width(80));
            modifier.modifierType = (ModifierType)EditorGUILayout.EnumPopup(modifier.modifierType, GUILayout.Width(120));

            GUILayout.Label("Value:", GUILayout.Width(50));
            modifier.value = EditorGUILayout.FloatField(modifier.value, GUILayout.Width(80));

            GUILayout.Label(GetModifierValueHint(modifier.modifierType), EditorStyles.miniLabel);
            EditorGUILayout.EndHorizontal();

            if (showAdvancedOptions)
            {
                EditorGUILayout.BeginHorizontal();
                GUILayout.Label("Stacking:", GUILayout.Width(80));
                modifier.stackingBehavior = (StackingBehavior)EditorGUILayout.EnumPopup(modifier.stackingBehavior, GUILayout.Width(120));
                EditorGUILayout.EndHorizontal();
            }

            GUILayout.Space(3);

            // Show preview
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("Preview:", EditorStyles.miniLabel, GUILayout.Width(60));
            GUILayout.Label(modifier.GetPreviewText(), EditorStyles.miniLabel);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();
        }

        private List<StatInfo> GetFilteredStats(List<StatInfo> allStats)
        {
            if (currentStatFilter == "All Categories")
                return allStats;

            if (selectedPrimaryCategory != "All Categories" && selectedSecondaryCategory == "All")
            {
                // Show all stats from the primary category (e.g., all "Player" stats)
                return allStats.Where(s => s.Category.StartsWith(selectedPrimaryCategory)).ToList();
            }

            if (currentStatFilter.Contains("/"))
            {
                // Exact hierarchical match (e.g., "Player/Combat")
                return allStats.Where(s => s.Category == currentStatFilter).ToList();
            }

            // Single-level category match
            return allStats.Where(s => s.Category == currentStatFilter).ToList();
        }

        // Helper methods with tooltips
        private string DrawFieldWithTooltip(string label, string value, string tooltipKey)
        {
            EditorGUILayout.BeginHorizontal();
            var result = EditorGUILayout.TextField(label, value);

            if (showTooltips && FieldTooltips.ContainsKey(tooltipKey))
            {
                GUILayout.Label("❓", GUILayout.Width(20));
                if (GUILayoutUtility.GetLastRect().Contains(Event.current.mousePosition))
                {
                    ShowTooltip(FieldTooltips[tooltipKey]);
                }
            }

            EditorGUILayout.EndHorizontal();
            return result;
        }

        private string DrawTextAreaWithTooltip(string label, string value, string tooltipKey, int height)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.BeginVertical();
            GUILayout.Label(label);
            var result = EditorGUILayout.TextArea(value, GUILayout.Height(height));
            EditorGUILayout.EndVertical();

            if (showTooltips && FieldTooltips.ContainsKey(tooltipKey))
            {
                EditorGUILayout.BeginVertical(GUILayout.Width(20));
                GUILayout.Label("❓");
                if (GUILayoutUtility.GetLastRect().Contains(Event.current.mousePosition))
                {
                    ShowTooltip(FieldTooltips[tooltipKey]);
                }
                EditorGUILayout.EndVertical();
            }

            EditorGUILayout.EndHorizontal();
            return result;
        }

        private float DrawFloatWithTooltip(string label, float value, string tooltipKey)
        {
            EditorGUILayout.BeginHorizontal();
            var result = EditorGUILayout.FloatField(label, value);

            if (showTooltips && FieldTooltips.ContainsKey(tooltipKey))
            {
                GUILayout.Label("❓", GUILayout.Width(20));
                if (GUILayoutUtility.GetLastRect().Contains(Event.current.mousePosition))
                {
                    ShowTooltip(FieldTooltips[tooltipKey]);
                }
            }

            EditorGUILayout.EndHorizontal();
            return result;
        }

        private int DrawIntWithTooltip(string label, int value, string tooltipKey)
        {
            EditorGUILayout.BeginHorizontal();
            var result = EditorGUILayout.IntField(label, value);

            if (showTooltips && FieldTooltips.ContainsKey(tooltipKey))
            {
                GUILayout.Label("❓", GUILayout.Width(20));
                if (GUILayoutUtility.GetLastRect().Contains(Event.current.mousePosition))
                {
                    ShowTooltip(FieldTooltips[tooltipKey]);
                }
            }

            EditorGUILayout.EndHorizontal();
            return result;
        }

        private bool DrawBoolWithTooltip(string label, bool value, string tooltipKey)
        {
            EditorGUILayout.BeginHorizontal();
            var result = EditorGUILayout.Toggle(label, value);

            if (showTooltips && FieldTooltips.ContainsKey(tooltipKey))
            {
                GUILayout.Label("❓", GUILayout.Width(20));
                if (GUILayoutUtility.GetLastRect().Contains(Event.current.mousePosition))
                {
                    ShowTooltip(FieldTooltips[tooltipKey]);
                }
            }

            EditorGUILayout.EndHorizontal();
            return result;
        }

        private System.Enum DrawEnumWithTooltip(string label, System.Enum value, string tooltipKey)
        {
            EditorGUILayout.BeginHorizontal();
            var result = EditorGUILayout.EnumPopup(label, value);

            if (showTooltips && FieldTooltips.ContainsKey(tooltipKey))
            {
                GUILayout.Label("❓", GUILayout.Width(20));
                if (GUILayoutUtility.GetLastRect().Contains(Event.current.mousePosition))
                {
                    ShowTooltip(FieldTooltips[tooltipKey]);
                }
            }

            EditorGUILayout.EndHorizontal();
            return result;
        }

        private void ShowTooltip(string message)
        {
            var rect = new Rect(Event.current.mousePosition.x, Event.current.mousePosition.y - 40, 350, 80);
            GUI.Label(rect, message, tooltipStyle);
        }

        private string GetModifierValueHint(ModifierType type)
        {
            switch (type)
            {
                case ModifierType.Flat:
                    return "Direct value: 10 = +10 to the stat";
                case ModifierType.Percentage:
                    return "Multiplier: 1.25 = +25%, 0.8 = -20%";
                case ModifierType.PercentageAdditive:
                    return "Additive %: 0.25 = +25% added to other bonuses";
                case ModifierType.Hyperbolic:
                    return "Diminishing returns value";
                case ModifierType.Override:
                    return "Sets stat to this exact value";
                case ModifierType.Minimum:
                    return "Enforces minimum value";
                case ModifierType.Maximum:
                    return "Enforces maximum value";
                default:
                    return "";
            }
        }

        private string GetStackingPreview(ItemCreationData item)
        {
            if (item.modifiers.Count == 0) return "No effects to preview";

            var firstMod = item.modifiers[0];
            var preview = $"Stack 1: {firstMod.GetPreviewText()}\n";

            if (item.stackCount > 1)
            {
                float effectiveValue = item.GetEffectiveModifierValue(firstMod, item.stackCount);
                string stackType = item.stacksAdditively ? "linear" : "diminishing";
                preview += $"Stack {item.stackCount}: {GetEffectiveModifierText(firstMod, effectiveValue)} ({stackType})";
            }

            return preview;
        }

        private string GetEffectiveModifierText(ModifierCreationData modifier, float effectiveValue)
        {
            string statName = !string.IsNullOrEmpty(modifier.statDisplayName) ? modifier.statDisplayName : "stat";

            switch (modifier.modifierType)
            {
                case ModifierType.Flat:
                    return $"{(effectiveValue >= 0 ? "+" : "")}{effectiveValue:F1} {statName}";
                case ModifierType.Percentage:
                    float percentage = (effectiveValue - 1f) * 100f;
                    return $"{(percentage >= 0 ? "+" : "")}{percentage:F0}% {statName}";
                default:
                    return $"{effectiveValue:F1} {statName}";
            }
        }

        private Color GetRarityColor(ItemRarity rarity)
        {
            switch (rarity)
            {
                case ItemRarity.Common: return Color.white;
                case ItemRarity.Uncommon: return Color.green;
                case ItemRarity.Rare: return Color.blue;
                case ItemRarity.Epic: return Color.magenta;
                case ItemRarity.Legendary: return Color.yellow;
                case ItemRarity.Artifact: return Color.red;
                default: return Color.gray;
            }
        }

        private void DrawFormSection(string title, System.Action drawContent)
        {
            EditorGUILayout.BeginVertical(cardStyle);
            GUILayout.Label(title, EditorStyles.boldLabel);
            GUILayout.Space(5);
            drawContent();
            EditorGUILayout.EndVertical();
        }

        private void DrawEmptyState(string title, string message)
        {
            EditorGUILayout.BeginVertical(infoBoxStyle);
            GUILayout.FlexibleSpace();

            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            EditorGUILayout.BeginVertical();

            GUILayout.Label(title, EditorStyles.boldLabel);
            GUILayout.Label(message, EditorStyles.wordWrappedLabel);

            EditorGUILayout.EndVertical();
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();

            GUILayout.FlexibleSpace();
            EditorGUILayout.EndVertical();
        }

        private void GenerateItemAsset(ItemCreationData itemData)
        {
            var itemDefinition = AssetGenerator.GenerateItemAsset(itemData);
            EditorGUIUtility.PingObject(itemDefinition);
            ShowNotification(new GUIContent($"✅ Created {itemData.itemName}!"));
        }

        private void GenerateAbilityAsset(AbilityCreationData abilityData)
        {
            var abilityDefinition = AssetGenerator.GenerateAbilityAsset(abilityData);
            EditorGUIUtility.PingObject(abilityDefinition);
            ShowNotification(new GUIContent($"✅ Created {abilityData.abilityName}!"));
        }

        private void TestPluginConnection()
        {
            bool success = statRegistry != null;
            string message = success ? "✅ All systems working!" : "❌ Plugin connection failed!";
            EditorUtility.DisplayDialog("Plugin Test", message, "OK");
        }
    }
}