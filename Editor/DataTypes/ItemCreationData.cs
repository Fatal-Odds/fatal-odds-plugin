using System;
using System.Collections.Generic;
using System.Linq;
using FatalOdds.Runtime;

namespace FatalOdds.Editor
{
    
    /// Enhanced template data for creating items tailored to your time-based action roguelike
    /// Based on your actual game categories and inspired by Risk of Rain, Dead Cells, etc.
    
    [Serializable]
    public class ItemCreationData
    {
        public string itemName = "New Item";
        public string description = "A mysterious item...";
        public ItemRarity rarity = ItemRarity.Common;
        public int stackSize = 1;
        public float value = 0f;
        public List<ModifierCreationData> modifiers = new List<ModifierCreationData>();

        // Action roguelike specific properties
        public int stackCount = 1; // How many copies the player currently has
        public bool stacksAdditively = true; // Linear vs diminishing returns stacking
        public string flavorText = ""; // Lore text at bottom of description
        public string[] tags = new string[0]; // Keywords for synergies/filtering

        // Enhanced categorization for your game
        public ItemCategory category = ItemCategory.Movement;
        public ItemType itemType = ItemType.Passive;
        public bool isTimeRelated = false; // Special flag for time-manipulation items

        
        /// Calculate effective modifier value based on stacking behavior
        
        public float GetEffectiveModifierValue(ModifierCreationData modifier, int currentStacks)
        {
            if (currentStacks <= 1) return modifier.value;

            if (stacksAdditively)
            {
                // Linear stacking: each stack adds full effect
                return modifier.value * currentStacks;
            }
            else
            {
                // Diminishing returns: first stack = 100%, additional stacks = 50%
                return modifier.value + (modifier.value * 0.5f * (currentStacks - 1));
            }
        }

        
        /// Get suggested rarity based on modifier power level
        
        public ItemRarity GetSuggestedRarity()
        {
            if (modifiers.Count == 0) return ItemRarity.Common;

            float totalPower = 0f;
            foreach (var mod in modifiers)
            {
                totalPower += CalculateModifierPower(mod);
            }

            // Suggest rarity based on total power
            if (totalPower >= 3.0f) return ItemRarity.Legendary;
            if (totalPower >= 2.0f) return ItemRarity.Epic;
            if (totalPower >= 1.5f) return ItemRarity.Rare;
            if (totalPower >= 1.0f) return ItemRarity.Uncommon;
            return ItemRarity.Common;
        }

        private float CalculateModifierPower(ModifierCreationData modifier)
        {
            // Simple power calculation based on modifier type and value
            switch (modifier.modifierType)
            {
                case ModifierType.Flat:
                    return Math.Abs(modifier.value) / 10f; // Normalize flat values
                case ModifierType.Percentage:
                case ModifierType.PercentageAdditive:
                    return Math.Abs(modifier.value - 1f) * 2f; // 0.5 = 1.0 power
                default:
                    return 1f;
            }
        }

        
        /// Create a copy of this item data
        
        public ItemCreationData Clone()
        {
            var clone = new ItemCreationData
            {
                itemName = this.itemName,
                description = this.description,
                rarity = this.rarity,
                stackSize = this.stackSize,
                value = this.value,
                stackCount = this.stackCount,
                stacksAdditively = this.stacksAdditively,
                flavorText = this.flavorText,
                tags = this.tags?.ToArray() ?? new string[0],
                category = this.category,
                itemType = this.itemType,
                isTimeRelated = this.isTimeRelated,
                modifiers = new List<ModifierCreationData>()
            };

            foreach (var modifier in this.modifiers)
            {
                clone.modifiers.Add(modifier.Clone());
            }

            return clone;
        }

        
        /// Validate this item data before asset generation
        
        public bool IsValid(out string errorMessage)
        {
            errorMessage = "";

            if (string.IsNullOrEmpty(itemName.Trim()))
            {
                errorMessage = "Item name cannot be empty";
                return false;
            }

            if (stackSize < 1)
            {
                errorMessage = "Stack size must be at least 1";
                return false;
            }

            if (stackCount < 1)
            {
                errorMessage = "Stack count must be at least 1";
                return false;
            }

            if (value < 0)
            {
                errorMessage = "Value cannot be negative";
                return false;
            }

            if (modifiers.Count == 0)
            {
                errorMessage = "Item must have at least one modifier";
                return false;
            }

            foreach (var modifier in modifiers)
            {
                if (!modifier.IsValid(out string modifierError))
                {
                    errorMessage = $"Modifier error: {modifierError}";
                    return false;
                }
            }

            return true;
        }

        
        /// Get auto-generated tags based on item properties
        
        public string[] GetAutoTags()
        {
            var autoTags = new List<string>();

            // Add category-based tags
            autoTags.Add(category.ToString().ToLower());

            // Add type-based tags
            autoTags.Add(itemType.ToString().ToLower());

            // Add time-related tag if applicable
            if (isTimeRelated) autoTags.Add("time");

            // Add rarity tag
            autoTags.Add(rarity.ToString().ToLower());

            // Add modifier-based tags
            foreach (var modifier in modifiers)
            {
                if (modifier.statDisplayName.ToLower().Contains("speed"))
                    autoTags.Add("speed");
                if (modifier.statDisplayName.ToLower().Contains("jump"))
                    autoTags.Add("mobility");
                if (modifier.statDisplayName.ToLower().Contains("health"))
                    autoTags.Add("survivability");
                if (modifier.statDisplayName.ToLower().Contains("damage"))
                    autoTags.Add("damage");
            }

            return autoTags.Distinct().ToArray();
        }
    }

    
    /// Enhanced ability creation data for time-based action roguelike
    
    [Serializable]
    public class AbilityCreationData
    {
        public string abilityName = "New Ability";
        public string description = "A powerful ability...";
        public AbilityTargetType targetType = AbilityTargetType.Self;
        public float cooldown = 5f;
        public float energyCost = 25f;
        public List<ModifierCreationData> modifiers = new List<ModifierCreationData>();

        // Enhanced ability properties
        public AbilityCategory category = AbilityCategory.Utility;
        public bool isTimeRelated = false; // Special flag for time abilities (rewind, freeze, etc.)
        public float castTime = 0f; // Time to cast the ability
        public float duration = 0f; // How long effects last (0 = instant)
        public int maxCharges = 1; // Number of charges before cooldown
        public bool isChanneled = false; // Whether ability requires continuous input

        
        /// Create a copy of this ability data
        
        public AbilityCreationData Clone()
        {
            var clone = new AbilityCreationData
            {
                abilityName = this.abilityName,
                description = this.description,
                targetType = this.targetType,
                cooldown = this.cooldown,
                energyCost = this.energyCost,
                category = this.category,
                isTimeRelated = this.isTimeRelated,
                castTime = this.castTime,
                duration = this.duration,
                maxCharges = this.maxCharges,
                isChanneled = this.isChanneled,
                modifiers = new List<ModifierCreationData>()
            };

            foreach (var modifier in this.modifiers)
            {
                clone.modifiers.Add(modifier.Clone());
            }

            return clone;
        }

        
        /// Validate this ability data before asset generation
        
        public bool IsValid(out string errorMessage)
        {
            errorMessage = "";

            if (string.IsNullOrEmpty(abilityName.Trim()))
            {
                errorMessage = "Ability name cannot be empty";
                return false;
            }

            if (cooldown < 0)
            {
                errorMessage = "Cooldown cannot be negative";
                return false;
            }

            if (energyCost < 0)
            {
                errorMessage = "Energy cost cannot be negative";
                return false;
            }

            if (castTime < 0)
            {
                errorMessage = "Cast time cannot be negative";
                return false;
            }

            if (maxCharges < 1)
            {
                errorMessage = "Max charges must be at least 1";
                return false;
            }

            if (modifiers.Count == 0)
            {
                errorMessage = "Ability must have at least one effect modifier";
                return false;
            }

            foreach (var modifier in modifiers)
            {
                if (!modifier.IsValid(out string modifierError))
                {
                    errorMessage = $"Modifier error: {modifierError}";
                    return false;
                }
            }

            return true;
        }
    }

    
    /// Enhanced modifier creation data with better categorization
    
    [Serializable]
    public class ModifierCreationData
    {
        public string statGuid = "";
        public string statDisplayName = "";
        public ModifierType modifierType = ModifierType.Flat;
        public float value = 10f;
        public StackingBehavior stackingBehavior = StackingBehavior.Additive;

        // Enhanced properties
        public bool isConditional = false; // Whether this modifier has conditions
        public string condition = ""; // Condition for when this modifier applies
        public float duration = 0f; // Duration for temporary modifiers (0 = permanent)

        
        /// Create a copy of this modifier data
        
        public ModifierCreationData Clone()
        {
            return new ModifierCreationData
            {
                statGuid = this.statGuid,
                statDisplayName = this.statDisplayName,
                modifierType = this.modifierType,
                value = this.value,
                stackingBehavior = this.stackingBehavior,
                isConditional = this.isConditional,
                condition = this.condition,
                duration = this.duration
            };
        }

        
        /// Validate this modifier data
        
        public bool IsValid(out string errorMessage)
        {
            errorMessage = "";

            if (string.IsNullOrEmpty(statGuid))
            {
                errorMessage = "Modifier must have a stat selected";
                return false;
            }

            // Check for reasonable value ranges based on modifier type
            switch (modifierType)
            {
                case ModifierType.Percentage:
                case ModifierType.PercentageAdditive:
                case ModifierType.PercentageMultiplicative:
                    if (value <= 0)
                    {
                        errorMessage = "Percentage modifiers should be positive (1.0 = 100%, 1.25 = +25%)";
                        return false;
                    }
                    break;

                case ModifierType.Hyperbolic:
                    if (value < 0)
                    {
                        errorMessage = "Hyperbolic modifiers should be positive";
                        return false;
                    }
                    break;
            }

            if (isConditional && string.IsNullOrEmpty(condition))
            {
                errorMessage = "Conditional modifiers must have a condition specified";
                return false;
            }

            if (duration < 0)
            {
                errorMessage = "Duration cannot be negative";
                return false;
            }

            return true;
        }

        
        /// Get a human-readable description of this modifier
        
        public string GetPreviewText()
        {
            string statName = !string.IsNullOrEmpty(statDisplayName) ? statDisplayName : "Unknown Stat";
            string preview = "";

            switch (modifierType)
            {
                case ModifierType.Flat:
                    preview = $"{(value >= 0 ? "+" : "")}{value:F1} {statName}";
                    break;

                case ModifierType.Percentage:
                case ModifierType.PercentageAdditive:
                case ModifierType.PercentageMultiplicative:
                    float percentage = (value - 1f) * 100f;
                    preview = $"{(percentage >= 0 ? "+" : "")}{percentage:F0}% {statName}";
                    break;

                case ModifierType.Override:
                    preview = $"Set {statName} to {value:F1}";
                    break;

                case ModifierType.Minimum:
                    preview = $"Minimum {statName}: {value:F1}";
                    break;

                case ModifierType.Maximum:
                    preview = $"Maximum {statName}: {value:F1}";
                    break;

                case ModifierType.Hyperbolic:
                    preview = $"{value:F1} {statName} (diminishing returns)";
                    break;

                case ModifierType.Exponential:
                    preview = $"{value:F1}x {statName} (exponential)";
                    break;

                case ModifierType.Logarithmic:
                    preview = $"{value:F1} {statName} (logarithmic)";
                    break;

                default:
                    preview = $"{value:F1} {statName} ({modifierType})";
                    break;
            }

            // Add conditional info
            if (isConditional && !string.IsNullOrEmpty(condition))
            {
                preview += $" (when {condition})";
            }

            // Add duration info
            if (duration > 0)
            {
                preview += $" (for {duration:F1}s)";
            }

            return preview;
        }
    }

    
    /// Item categories based on your game's stat categories
    
    public enum ItemCategory
    {
        Movement,       // Walk Speed, Sprint Speed, Jump Force, etc.
        Combat,         // Damage, Attack Speed, Critical Chance
        Defense,        // Health, Armor, Block Chance
        Utility,        // Cooldown Reduction, Luck, Resource Management
        Time,           // Time-related abilities and modifiers
        Hybrid          // Items that affect multiple categories
    }

    
    /// Item types for better organization
    
    public enum ItemType
    {
        Passive,        // Always-on effects (most roguelike items)
        Active,         // Triggered abilities with cooldowns
        Consumable,     // One-time use items
        Equipment,      // Equippable gear (if your game has this)
        Upgrade         // Permanent character improvements
    }

    
    /// Ability categories for better organization
    
    public enum AbilityCategory
    {
        Movement,       // Dash, teleport, speed boosts
        Combat,         // Attack abilities, damage spells
        Defensive,      // Shields, healing, damage reduction
        Utility,        // Buffs, debuffs, environment interaction
        Time,           // Rewind, freeze, time manipulation
        Ultimate        // Powerful abilities with long cooldowns
    }

    
    /// Template data for creating items based on your game's actual stats
    
    public static class FatalOddsItemTemplates
    {
        public static ItemCreationData CreateSpeedEnhancer()
        {
            return new ItemCreationData
            {
                itemName = "Momentum Boots",
                description = "Increases movement speed. Speed builds momentum over time.",
                rarity = ItemRarity.Common,
                category = ItemCategory.Movement,
                itemType = ItemType.Passive,
                stacksAdditively = true,
                flavorText = "Every step builds upon the last.",
                tags = new[] { "movement", "speed", "boots" },
                modifiers = new List<ModifierCreationData>
                {
                    new ModifierCreationData
                    {
                        statDisplayName = "Walk Speed",
                        modifierType = ModifierType.PercentageAdditive,
                        value = 0.15f // +15% walk speed
                    }
                }
            };
        }

        public static ItemCreationData CreateJumpEnhancer()
        {
            return new ItemCreationData
            {
                itemName = "Gravity Defiant",
                description = "Increases jump force and adds a third jump. Stacks add more jumps.",
                rarity = ItemRarity.Uncommon,
                category = ItemCategory.Movement,
                itemType = ItemType.Passive,
                stacksAdditively = false, // Diminishing returns for additional jumps
                flavorText = "Defying gravity, one leap at a time.",
                tags = new[] { "movement", "jump", "aerial" },
                modifiers = new List<ModifierCreationData>
                {
                    new ModifierCreationData
                    {
                        statDisplayName = "Jump Force",
                        modifierType = ModifierType.PercentageAdditive,
                        value = 0.2f // +20% jump force
                    },
                    new ModifierCreationData
                    {
                        statDisplayName = "Max Jumps",
                        modifierType = ModifierType.Flat,
                        value = 1f // +1 extra jump
                    }
                }
            };
        }

        public static ItemCreationData CreateTimeDistorter()
        {
            return new ItemCreationData
            {
                itemName = "Temporal Accelerator",
                description = "Reduces ability cooldowns and increases attack speed. Time flows differently around you.",
                rarity = ItemRarity.Rare,
                category = ItemCategory.Time,
                itemType = ItemType.Passive,
                isTimeRelated = true,
                stacksAdditively = true,
                flavorText = "Time bends to your will.",
                tags = new[] { "time", "cooldown", "attack" },
                modifiers = new List<ModifierCreationData>
                {
                    new ModifierCreationData
                    {
                        statDisplayName = "Jump Cooldown",
                        modifierType = ModifierType.PercentageMultiplicative,
                        value = 0.8f // -20% cooldown
                    },
                    new ModifierCreationData
                    {
                        statDisplayName = "Aim Lock Duration",
                        modifierType = ModifierType.PercentageMultiplicative,
                        value = 1.25f // +25% aim lock duration
                    }
                }
            };
        }

        public static ItemCreationData CreateRewindCore()
        {
            return new ItemCreationData
            {
                itemName = "Paradox Core",
                description = "Grants immunity to temporal effects and enhances rewind recovery speed.",
                rarity = ItemRarity.Epic,
                category = ItemCategory.Time,
                itemType = ItemType.Passive,
                isTimeRelated = true,
                stacksAdditively = false, // Diminishing returns
                flavorText = "A fragment of time itself, crystallized.",
                tags = new[] { "time", "rewind", "immunity", "rare" },
                modifiers = new List<ModifierCreationData>
                {
                    new ModifierCreationData
                    {
                        statDisplayName = "Turn Speed",
                        modifierType = ModifierType.PercentageAdditive,
                        value = 0.3f, // +30% turn speed for better control
                        isConditional = true,
                        condition = "while recovering from rewind"
                    }
                }
            };
        }

        public static AbilityCreationData CreateTemporalDash()
        {
            return new AbilityCreationData
            {
                abilityName = "Temporal Dash",
                description = "Instantly teleport forward while briefly slowing time around you.",
                category = AbilityCategory.Movement,
                targetType = AbilityTargetType.Ground,
                isTimeRelated = true,
                cooldown = 8f,
                energyCost = 30f,
                castTime = 0.2f,
                duration = 2f, // Time slow lasts 2 seconds
                maxCharges = 2,
                modifiers = new List<ModifierCreationData>
                {
                    new ModifierCreationData
                    {
                        statDisplayName = "Movement Speed",
                        modifierType = ModifierType.PercentageMultiplicative,
                        value = 1.5f, // +50% movement speed during effect
                        duration = 2f
                    }
                }
            };
        }

        public static AbilityCreationData CreateTimeFreeze()
        {
            return new AbilityCreationData
            {
                abilityName = "Temporal Stasis",
                description = "Freeze time around you, allowing free movement while everything else stops.",
                category = AbilityCategory.Time,
                targetType = AbilityTargetType.Area,
                isTimeRelated = true,
                cooldown = 20f,
                energyCost = 60f,
                castTime = 0.5f,
                duration = 3f,
                maxCharges = 1,
                isChanneled = true,
                modifiers = new List<ModifierCreationData>
                {
                    new ModifierCreationData
                    {
                        statDisplayName = "Air Mobility",
                        modifierType = ModifierType.PercentageMultiplicative,
                        value = 2f, // Can move freely in air during time freeze
                        duration = 3f,
                        isConditional = true,
                        condition = "during time freeze"
                    }
                }
            };
        }
    }
}