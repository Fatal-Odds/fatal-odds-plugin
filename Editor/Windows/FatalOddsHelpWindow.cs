using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

namespace FatalOdds.Editor
{
    
    /// Comprehensive help system for Fatal Odds with examples and tutorials
    
    public class FatalOddsHelpWindow : EditorWindow
    {
        public static void ShowWindow()
        {
            var window = GetWindow<FatalOddsHelpWindow>("Fatal Odds Help");
            window.minSize = new Vector2(800, 600);
            window.Show();
        }

        private enum HelpTab { Overview, StatTags, Items, Abilities, Modifiers, Examples, FAQ }
        private HelpTab currentTab = HelpTab.Overview;
        private Vector2 scrollPosition;

        private void OnGUI()
        {
            DrawHeader();
            DrawTabs();
            DrawContent();
        }

        private void DrawHeader()
        {
            EditorGUILayout.Space(10);
            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            GUILayout.Label("📚 Fatal Odds Help & Documentation", EditorStyles.boldLabel);
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space(10);
        }

        private void DrawTabs()
        {
            var tabs = new[] { "🏠 Overview", "🏷️ Stat Tags", "🎒 Items", "⚔️ Abilities", "⚡ Modifiers", "💡 Examples", "❓ FAQ" };
            var tabEnums = new[] { HelpTab.Overview, HelpTab.StatTags, HelpTab.Items, HelpTab.Abilities, HelpTab.Modifiers, HelpTab.Examples, HelpTab.FAQ };

            EditorGUILayout.BeginHorizontal();
            for (int i = 0; i < tabs.Length; i++)
            {
                if (GUILayout.Button(tabs[i], currentTab == tabEnums[i] ? "toolbarbutton" : "toolbarbutton"))
                {
                    currentTab = tabEnums[i];
                }
            }
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space(10);
        }

        private void DrawContent()
        {
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

            switch (currentTab)
            {
                case HelpTab.Overview:
                    DrawOverviewHelp();
                    break;
                case HelpTab.StatTags:
                    DrawStatTagsHelp();
                    break;
                case HelpTab.Items:
                    DrawItemsHelp();
                    break;
                case HelpTab.Abilities:
                    DrawAbilitiesHelp();
                    break;
                case HelpTab.Modifiers:
                    DrawModifiersHelp();
                    break;
                case HelpTab.Examples:
                    DrawExamplesHelp();
                    break;
                case HelpTab.FAQ:
                    DrawFAQHelp();
                    break;
            }

            EditorGUILayout.EndScrollView();
        }

        private void DrawOverviewHelp()
        {
            DrawHelpSection("🎮 What is Fatal Odds?",
                "Fatal Odds is a Unity plugin that streamlines the creation of items and abilities for your game. " +
                "It uses a tag-based system to discover stats in your code and provides an intuitive editor to create modifiers.\n\n" +
                "Key features:\n" +
                "• Automatic stat discovery through code reflection\n" +
                "• Visual item and ability creation with forms and dropdowns\n" +
                "• Powerful modifier system with multiple calculation types\n" +
                "• ScriptableObject generation for easy integration\n" +
                "• Real-time testing and debugging tools");

            DrawHelpSection("🚀 Quick Start Guide",
                "1. **Tag your stats**: Add [StatTag(\"Name\", \"Category\")] to numeric fields\n" +
                "2. **Scan for stats**: Open the Item & Ability Creator and scan for stats\n" +
                "3. **Create items**: Use the Items tab to build items with stat modifiers\n" +
                "4. **Create abilities**: Use the Abilities tab to design spells and skills\n" +
                "5. **Generate assets**: Click 'Generate Asset' to create ScriptableObjects\n" +
                "6. **Use in game**: Apply items/abilities to GameObjects with ModifierManager");

            DrawHelpSection("📋 Workflow Tips",
                "• Start with a few basic stats (health, damage, speed)\n" +
                "• Use meaningful categories to organize your stats\n" +
                "• Test modifiers with the ModifierExample component\n" +
                "• Use the Stat Discovery Audit to find untagged stats\n" +
                "• Enable tooltips for detailed field explanations");
        }

        private void DrawStatTagsHelp()
        {
            DrawHelpSection("🏷️ What are Stat Tags?",
                "Stat Tags are attributes you add to numeric fields in your MonoBehaviour classes. " +
                "They tell Fatal Odds which fields represent game stats that can be modified by items and abilities.");

            DrawCodeExample("Basic Stat Tag Example", @"
using FatalOdds.Runtime;

public class PlayerStats : MonoBehaviour 
{
    [StatTag(""Max Health"", ""Combat"")]
    public float maxHealth = 100f;
    
    [StatTag(""Attack Power"", ""Combat"")]
    public float attackPower = 20f;
    
    [StatTag(""Movement Speed"", ""Movement"")]
    public float moveSpeed = 5f;
}");

            DrawHelpSection("📝 Stat Tag Parameters",
                "• **Name**: Display name shown in the editor (optional, uses field name if not provided)\n" +
                "• **Category**: Groups related stats together (e.g., \"Combat\", \"Movement\", \"Magic\")\n" +
                "• **Description**: Detailed explanation of what the stat does (optional)\n" +
                "• **ShowInUI**: Whether to show this stat in user interfaces (default: true)");

            DrawCodeExample("Advanced Stat Tag Example", @"
[StatTag(""Critical Hit Chance"", ""Combat"", 
         ""Chance to deal double damage on attack"", true)]
public float criticalChance = 0.1f; // 10%

// You can also use simple syntax:
[StatTag(""Jump Height"", ""Movement"")]
public float jumpHeight = 2f;");

            DrawHelpSection("✅ Best Practices",
                "• Use descriptive names that players will understand\n" +
                "• Group related stats with consistent categories\n" +
                "• Only tag stats that should be modifiable by items/abilities\n" +
                "• Use float or int types for numeric stats\n" +
                "• Consider the base values when designing modifiers");
        }

        private void DrawItemsHelp()
        {
            DrawHelpSection("🎒 What are Items?",
                "Items are objects that players can collect, equip, or use. In Fatal Odds, items primarily work by " +
                "applying stat modifiers to the player or target when equipped or used.");

            DrawHelpSection("📋 Item Properties Explained",
                "• **Name**: The display name players see\n" +
                "• **Description**: Detailed text explaining the item's effects and lore\n" +
                "• **Economic Value**: How much the item costs or sells for (used by shops)\n" +
                "• **Stack Size**: Maximum number that can be grouped in one inventory slot\n" +
                "• **Rarity**: Affects drop rates, visual effects, and player excitement\n" +
                "• **Modifiers**: The actual stat changes the item provides");

            DrawHelpSection("💎 Rarity System",
                "• **Common** (White): Basic items, frequently found\n" +
                "• **Uncommon** (Green): Slightly better, less common\n" +
                "• **Rare** (Blue): Significantly powerful, rare drops\n" +
                "• **Epic** (Purple): Very powerful, very rare\n" +
                "• **Legendary** (Yellow): Extremely powerful, legendary status\n" +
                "• **Artifact** (Red): Unique, game-changing items");

            DrawHelpSection("⚡ Item Design Tips",
                "• Start with simple flat bonuses (+10 Health)\n" +
                "• Use percentage bonuses for scaling (+25% Damage)\n" +
                "• Consider the item's theme when choosing modifiers\n" +
                "• Balance power with rarity - legendary items should feel special\n" +
                "• Use stack size of 1 for equipment, higher for consumables");
        }

        private void DrawAbilitiesHelp()
        {
            DrawHelpSection("⚔️ What are Abilities?",
                "Abilities are active skills that players can use during gameplay. They typically have cooldowns, " +
                "energy costs, and apply temporary or permanent effects to targets.");

            DrawHelpSection("🎯 Target Types Explained",
                "• **Self**: Only affects the caster\n" +
                "• **Enemy**: Can only target hostile entities\n" +
                "• **Ally**: Can only target friendly entities (not including self)\n" +
                "• **Ground**: Target a specific position or area\n" +
                "• **Area**: Affects an area around the caster");

            DrawHelpSection("⏱️ Timing Properties",
                "• **Cooldown**: Time before the ability can be used again\n" +
                "• **Energy Cost**: Resources consumed when using the ability\n" +
                "• **Cast Time**: How long it takes to activate (0 = instant)\n" +
                "• **Effect Duration**: How long temporary effects last (0 = permanent)");

            DrawHelpSection("🎨 Ability Design Patterns",
                "• **Buffs**: Temporary positive effects on allies\n" +
                "• **Debuffs**: Temporary negative effects on enemies\n" +
                "• **Heals**: Restore health or other resources\n" +
                "• **Damage**: Direct harm to targets\n" +
                "• **Utility**: Movement, protection, or tactical effects");
        }

        private void DrawModifiersHelp()
        {
            DrawHelpSection("⚡ Understanding Modifiers",
                "Modifiers are the core of the Fatal Odds system. They define how items and abilities change stats. " +
                "Each modifier targets a specific stat and applies a mathematical operation.");

            DrawHelpSection("🔢 Modifier Types",
                "• **Flat**: Adds or subtracts a fixed amount (+10 Health)\n" +
                "• **Percentage**: Multiplies by a percentage (1.25 = +25%)\n" +
                "• **Percentage Additive**: Adds percentages before applying (+25% + +15% = +40%)\n" +
                "• **Override**: Sets the stat to a specific value\n" +
                "• **Minimum/Maximum**: Enforces limits on the stat\n" +
                "• **Hyperbolic**: Diminishing returns for balance\n" +
                "• **Exponential**: Exponential scaling for powerful effects");

            DrawHelpSection("📚 Stacking Behaviors",
                "• **Additive**: Multiple modifiers add together\n" +
                "• **Multiplicative**: Multiple modifiers multiply together\n" +
                "• **Override**: Only the latest/highest modifier applies\n" +
                "• **Average**: Takes the average of all modifiers\n" +
                "• **Highest/Lowest**: Uses only the highest or lowest value");

            DrawHelpSection("🎯 Modifier Examples",
                "• Health Potion: +50 flat Health (temporary)\n" +
                "• Sword of Power: +25% Attack Damage (while equipped)\n" +
                "• Speed Boots: +2.5 Movement Speed (flat bonus)\n" +
                "• Berserker Rage: +100% Attack Speed for 10 seconds\n" +
                "• Shield Spell: Set Defense to 50 (override)");
        }

        private void DrawExamplesHelp()
        {
            DrawHelpSection("💡 Complete Item Example",
                "Let's create a \"Sword of Flames\" that increases attack power and adds fire damage:");

            DrawCodeExample("Step 1: Tag your stats", @"
public class PlayerCombat : MonoBehaviour 
{
    [StatTag(""Attack Power"", ""Combat"")]
    public float attackPower = 20f;
    
    [StatTag(""Fire Damage"", ""Combat"")]
    public float fireDamage = 0f;
}");

            DrawHelpSection("Step 2: Create the item",
                "1. Open Fatal Odds Item & Ability Creator\n" +
                "2. Go to Items tab and click '+ New Item'\n" +
                "3. Set name to 'Sword of Flames'\n" +
                "4. Set rarity to 'Rare'\n" +
                "5. Add modifier: +15 Attack Power (Flat)\n" +
                "6. Add modifier: +10 Fire Damage (Flat)\n" +
                "7. Click 'Generate Asset'");

            DrawCodeExample("Step 3: Use in your game", @"
// Apply item to player
public void EquipItem(ItemDefinition item, GameObject target)
{
    item.ApplyToTarget(target);
}

// Remove item from player  
public void UnequipItem(ItemDefinition item, GameObject target)
{
    item.RemoveFromTarget(target);
}");

            DrawHelpSection("🎮 Testing Your Creation",
                "1. Add ModifierExample component to a GameObject\n" +
                "2. Run the scene and press number keys to test\n" +
                "3. Watch the stats change in real-time\n" +
                "4. Use the debug console to see calculation breakdowns");
        }

        private void DrawFAQHelp()
        {
            DrawHelpSection("❓ Frequently Asked Questions", "");

            DrawFAQItem("Q: My stats aren't being discovered. What's wrong?",
                "A: Make sure you've added [StatTag] attributes to your fields and clicked 'Scan for Stats'. " +
                "The fields must be in MonoBehaviour classes and be numeric types (float, int, etc.).");

            DrawFAQItem("Q: How do percentage modifiers work?",
                "A: For percentage modifiers, 1.0 = 100%. So 1.25 = +25% and 0.75 = -25%. " +
                "The original value is multiplied by this number.");

            DrawFAQItem("Q: Can I have multiple modifiers on one item?",
                "A: Yes! Items can have as many modifiers as you want. Each modifier can target " +
                "different stats or multiple modifiers can affect the same stat.");

            DrawFAQItem("Q: What's the difference between items and abilities?",
                "A: Items are typically passive (equipped gear, consumables) while abilities are " +
                "active skills with cooldowns and energy costs. Both use the same modifier system.");

            DrawFAQItem("Q: How do I use generated assets in my game?",
                "A: The generated ScriptableObjects have built-in methods like ApplyToTarget() and " +
                "RemoveFromTarget(). Your GameObject needs a ModifierManager component.");

            DrawFAQItem("Q: Can modifiers stack?",
                "A: Yes, the stacking behavior is configurable per modifier. You can choose additive, " +
                "multiplicative, override, or other stacking types.");

            DrawFAQItem("Q: My modifiers aren't working at runtime. Help!",
                "A: Make sure your GameObject has a ModifierManager component and that you're calling " +
                "ApplyToTarget() on your items/abilities. Check the console for error messages.");
        }

        private void DrawHelpSection(string title, string content)
        {
            EditorGUILayout.BeginVertical("box");
            GUILayout.Label(title, EditorStyles.boldLabel);
            if (!string.IsNullOrEmpty(content))
            {
                GUILayout.Label(content, EditorStyles.wordWrappedLabel);
            }
            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(10);
        }

        private void DrawCodeExample(string title, string code)
        {
            EditorGUILayout.BeginVertical("box");
            GUILayout.Label(title, EditorStyles.boldLabel);

            var codeStyle = new GUIStyle(EditorStyles.textArea)
            {
                fontStyle = FontStyle.Normal,
                fontSize = 11,
                wordWrap = false
            };

            EditorGUILayout.TextArea(code.Trim(), codeStyle, GUILayout.Height(code.Split('\n').Length * 16 + 10));
            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(10);
        }

        private void DrawFAQItem(string question, string answer)
        {
            EditorGUILayout.BeginVertical("box");
            GUILayout.Label(question, EditorStyles.boldLabel);
            GUILayout.Label(answer, EditorStyles.wordWrappedLabel);
            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(5);
        }
    }
}