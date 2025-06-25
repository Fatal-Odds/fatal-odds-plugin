using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
using FatalOdds.Runtime;

namespace FatalOdds.Editor
{
    
    /// Asset generator for stacking modifier items and active abilities
    /// Creates items that provide permanent stat modifications with various stacking behaviors
    
    public static class AssetGenerator
    {
        private static string outputPath = "Assets/FatalOdds/Generated";

        
        /// Generate an ItemDefinition ScriptableObject from modifier item creation data
        
        public static ItemDefinition GenerateItemAsset(ItemCreationData itemData)
        {
            // Create the ItemDefinition ScriptableObject
            var itemDefinition = ScriptableObject.CreateInstance<ItemDefinition>();

            // Set basic properties
            itemDefinition.itemName = GetValidFileName(itemData.itemName);
            itemDefinition.description = itemData.description;
            itemDefinition.rarity = itemData.rarity;

            // Set stacking properties for modifier items
            itemDefinition.stackingType = itemData.stacksAdditively ? FatalOdds.Runtime.ItemStackingType.Linear : FatalOdds.Runtime.ItemStackingType.Diminishing;
            itemDefinition.flavorText = itemData.flavorText ?? "";
            itemDefinition.tags = itemData.tags ?? new string[0];

            // Convert modifiers from creation data to runtime data
            itemDefinition.modifiers = GenerateModifierData(itemData.modifiers);

            // Generate file path
            string fileName = $"{itemDefinition.itemName}.asset";
            string itemsPath = Path.Combine(outputPath, "Items");
            EnsureDirectoryExists(itemsPath);
            string fullPath = Path.Combine(itemsPath, fileName);

            // Make sure the path is unique
            fullPath = AssetDatabase.GenerateUniqueAssetPath(fullPath);

            // Create the asset
            AssetDatabase.CreateAsset(itemDefinition, fullPath);
            AssetDatabase.SaveAssets();

            Debug.Log($"[Fatal Odds] Generated modifier item: {itemDefinition.itemName} at {fullPath}");
            return itemDefinition;
        }

        
        /// Generate an AbilityDefinition ScriptableObject from creation data
        
        public static AbilityDefinition GenerateAbilityAsset(AbilityCreationData abilityData)
        {
            // Create the AbilityDefinition ScriptableObject
            var abilityDefinition = ScriptableObject.CreateInstance<AbilityDefinition>();

            // Set basic properties
            abilityDefinition.abilityName = GetValidFileName(abilityData.abilityName);
            abilityDefinition.description = abilityData.description;
            abilityDefinition.targetType = abilityData.targetType;
            abilityDefinition.cooldown = abilityData.cooldown;
            abilityDefinition.energyCost = abilityData.energyCost;

            // Set default duration for temporary effects (10 seconds)
            abilityDefinition.effectDuration = 10f;

            // Convert modifiers from creation data to runtime data
            abilityDefinition.modifiers = GenerateModifierData(abilityData.modifiers);

            // Generate file path
            string fileName = $"{abilityDefinition.abilityName}.asset";
            string abilitiesPath = Path.Combine(outputPath, "Abilities");
            EnsureDirectoryExists(abilitiesPath);
            string fullPath = Path.Combine(abilitiesPath, fileName);

            // Make sure the path is unique
            fullPath = AssetDatabase.GenerateUniqueAssetPath(fullPath);

            // Create the asset
            AssetDatabase.CreateAsset(abilityDefinition, fullPath);
            AssetDatabase.SaveAssets();

            Debug.Log($"[Fatal Odds] Generated active ability: {abilityDefinition.abilityName} at {fullPath}");
            return abilityDefinition;
        }

        
        /// Convert creation data modifiers to runtime modifier data
        
        private static List<StatModifierData> GenerateModifierData(List<ModifierCreationData> creationModifiers)
        {
            var modifierDataList = new List<StatModifierData>();

            foreach (var creationMod in creationModifiers)
            {
                if (string.IsNullOrEmpty(creationMod.statGuid))
                {
                    Debug.LogWarning($"Modifier has no stat selected, skipping");
                    continue;
                }

                var modifierData = new StatModifierData
                {
                    statGuid = creationMod.statGuid,
                    statDisplayName = creationMod.statDisplayName,
                    modifierType = creationMod.modifierType,
                    value = creationMod.value,
                    stackingBehavior = creationMod.stackingBehavior,
                    priority = StatModifier.GetDefaultPriority(creationMod.modifierType)
                };

                modifierDataList.Add(modifierData);
            }

            return modifierDataList;
        }

        
        /// Ensure a directory exists, creating it if necessary
        
        private static void EnsureDirectoryExists(string path)
        {
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
                AssetDatabase.Refresh();
            }
        }

        
        /// Convert a name into a valid filename
        
        private static string GetValidFileName(string name)
        {
            if (string.IsNullOrEmpty(name))
                name = "Unnamed";

            // Remove invalid characters
            var invalidChars = Path.GetInvalidFileNameChars();
            foreach (char c in invalidChars)
            {
                name = name.Replace(c, '_');
            }

            // Replace spaces with underscores for consistency
            name = name.Replace(' ', '_');

            return name;
        }

        
        /// Validate modifier item creation data before generation
        
        public static bool ValidateItemData(ItemCreationData itemData, out string errorMessage)
        {
            errorMessage = "";

            if (string.IsNullOrEmpty(itemData.itemName))
            {
                errorMessage = "Item name cannot be empty";
                return false;
            }

            if (itemData.stackCount < 1)
            {
                errorMessage = "Stack count must be at least 1";
                return false;
            }

            if (itemData.modifiers.Count == 0)
            {
                errorMessage = "Item must have at least one modifier effect";
                return false;
            }

            foreach (var modifier in itemData.modifiers)
            {
                if (string.IsNullOrEmpty(modifier.statGuid))
                {
                    errorMessage = "All modifiers must have a stat selected";
                    return false;
                }

                // Validate modifier values make sense
                if (!ValidateModifierValue(modifier, out string modifierError))
                {
                    errorMessage = $"Modifier error: {modifierError}";
                    return false;
                }
            }

            return true;
        }

        
        /// Validate ability creation data before generation
        
        public static bool ValidateAbilityData(AbilityCreationData abilityData, out string errorMessage)
        {
            errorMessage = "";

            if (string.IsNullOrEmpty(abilityData.abilityName))
            {
                errorMessage = "Ability name cannot be empty";
                return false;
            }

            if (abilityData.cooldown < 0)
            {
                errorMessage = "Cooldown cannot be negative";
                return false;
            }

            if (abilityData.energyCost < 0)
            {
                errorMessage = "Energy cost cannot be negative";
                return false;
            }

            if (abilityData.modifiers.Count == 0)
            {
                errorMessage = "Ability must have at least one effect";
                return false;
            }

            foreach (var modifier in abilityData.modifiers)
            {
                if (string.IsNullOrEmpty(modifier.statGuid))
                {
                    errorMessage = "All effects must have a stat selected";
                    return false;
                }

                if (!ValidateModifierValue(modifier, out string modifierError))
                {
                    errorMessage = $"Effect error: {modifierError}";
                    return false;
                }
            }

            return true;
        }

        
        /// Validate that a modifier value makes sense for its type
        
        private static bool ValidateModifierValue(ModifierCreationData modifier, out string errorMessage)
        {
            errorMessage = "";

            switch (modifier.modifierType)
            {
                case ModifierType.Percentage:
                case ModifierType.PercentageAdditive:
                case ModifierType.PercentageMultiplicative:
                    if (modifier.value <= 0)
                    {
                        errorMessage = "Percentage modifiers should be positive (1.25 = +25%, 0.8 = -20%)";
                        return false;
                    }
                    break;

                case ModifierType.Hyperbolic:
                    if (modifier.value < 0)
                    {
                        errorMessage = "Hyperbolic modifiers should be positive for diminishing returns to work correctly";
                        return false;
                    }
                    break;

                case ModifierType.Minimum:
                case ModifierType.Maximum:
                    // These can be any value, but warn if they seem unusual
                    break;

                case ModifierType.Flat:
                    // Flat modifiers can be positive or negative
                    break;

                default:
                    break;
            }

            return true;
        }

        
        /// Generate a preview of what will be created (for debugging)
        
        public static string GenerateItemPreview(ItemCreationData itemData)
        {
            var preview = $"MODIFIER ITEM: {itemData.itemName}\n";
            preview += $"Rarity: {itemData.rarity}\n";
            preview += $"Description: {itemData.description}\n";
            preview += $"Stacking: {(itemData.stacksAdditively ? "Linear" : "Diminishing Returns")}\n";
            preview += $"Current Stacks: {itemData.stackCount}\n";

            if (!string.IsNullOrEmpty(itemData.flavorText))
            {
                preview += $"Flavor: {itemData.flavorText}\n";
            }

            if (itemData.tags != null && itemData.tags.Length > 0)
            {
                preview += $"Tags: {string.Join(", ", itemData.tags)}\n";
            }

            preview += "\nEFFECTS:\n";
            foreach (var modifier in itemData.modifiers)
            {
                preview += $"• {modifier.GetPreviewText()}\n";

                // Show stacking preview
                if (itemData.stackCount > 1)
                {
                    float effectiveValue = itemData.GetEffectiveModifierValue(modifier, itemData.stackCount);
                    string stackType = itemData.stacksAdditively ? "linear" : "diminishing";
                    preview += $"  At {itemData.stackCount} stacks: {GetEffectiveValueText(modifier, effectiveValue)} ({stackType})\n";
                }
            }

            return preview;
        }

        
        /// Generate a preview of what will be created (for debugging)
        
        public static string GenerateAbilityPreview(AbilityCreationData abilityData)
        {
            var preview = $"ACTIVE ABILITY: {abilityData.abilityName}\n";
            preview += $"Target: {abilityData.targetType}\n";
            preview += $"Description: {abilityData.description}\n";
            preview += $"Cooldown: {abilityData.cooldown}s\n";
            preview += $"Energy Cost: {abilityData.energyCost}\n";

            preview += "\nTEMPORARY EFFECTS (10s duration):\n";
            foreach (var modifier in abilityData.modifiers)
            {
                preview += $"• {modifier.GetPreviewText()}\n";
            }

            return preview;
        }

        
        /// Helper to show effective modifier values for stacking preview
        
        private static string GetEffectiveValueText(ModifierCreationData modifier, float effectiveValue)
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

        
        /// Create common stacking modifier item templates
        
        public static ItemCreationData CreateStackingItemTemplate(string templateName)
        {
            switch (templateName.ToLower())
            {
                case "attack speed booster":
                    return new ItemCreationData
                    {
                        itemName = "Attack Speed Booster",
                        description = "Increases attack speed by 15%. Stacks linearly.",
                        rarity = ItemRarity.Common,
                        stacksAdditively = true,
                        flavorText = "A simple enhancement that keeps giving.",
                        tags = new[] { "attack", "speed", "common" },
                        modifiers = new List<ModifierCreationData>
                        {
                            new ModifierCreationData
                            {
                                modifierType = ModifierType.PercentageAdditive,
                                value = 0.15f, // +15%
                                statDisplayName = "Attack Speed"
                            }
                        }
                    };

                case "movement enhancer":
                    return new ItemCreationData
                    {
                        itemName = "Movement Enhancer",
                        description = "Increases movement speed by 14%. Stacks linearly.",
                        rarity = ItemRarity.Uncommon,
                        stacksAdditively = true,
                        flavorText = "Every step feels lighter.",
                        tags = new[] { "movement", "speed", "uncommon" },
                        modifiers = new List<ModifierCreationData>
                        {
                            new ModifierCreationData
                            {
                                modifierType = ModifierType.PercentageAdditive,
                                value = 0.14f, // +14%
                                statDisplayName = "Movement Speed"
                            }
                        }
                    };

                case "vitality core":
                    return new ItemCreationData
                    {
                        itemName = "Vitality Core",
                        description = "Permanently increases health. Health gained diminishes with additional cores.",
                        rarity = ItemRarity.Rare,
                        stacksAdditively = false, // Diminishing returns
                        flavorText = "A pulsing core of life energy.",
                        tags = new[] { "health", "vitality", "rare" },
                        modifiers = new List<ModifierCreationData>
                        {
                            new ModifierCreationData
                            {
                                modifierType = ModifierType.Flat,
                                value = 25f, // +25 health base
                                statDisplayName = "Max Health"
                            }
                        }
                    };

                default:
                    return new ItemCreationData
                    {
                        itemName = "New Modifier Item",
                        description = "A passive modifier that stacks when collected.",
                        rarity = ItemRarity.Common,
                        stacksAdditively = true,
                        flavorText = "Add some flavor text here...",
                        modifiers = new List<ModifierCreationData>()
                    };
            }
        }
    }
}