using System;
using System.Collections.Generic;
using UnityEngine;
using System.Reflection;

namespace FatalOdds.Runtime
{
    
    /// ScriptableObject definition for items generated from the graph editor
    /// Updated for Risk of Rain style modifier items
    
    [CreateAssetMenu(fileName = "New Item", menuName = "Fatal Odds/Item Definition")]
    public class ItemDefinition : ScriptableObject
    {
        [Header("Basic Information")]
        public string itemName = "New Item";
        [TextArea(3, 5)]
        public string description = "A mysterious item...";
        public Texture2D icon;

        [Header("Item Properties")]
        public ItemRarity rarity = ItemRarity.Common;
        public int stackSize = 1;
        public float value = 0f; // Economic value
        public bool isConsumable = false;
        public bool isEquippable = true;

        [Header("Risk of Rain Properties")]
        public ItemStackingType stackingType = ItemStackingType.Linear; // How this item stacks when collected multiple times
        public string flavorText = ""; // Lore text shown at bottom of description

        [Header("Modifiers")]
        public List<StatModifierData> modifiers = new List<StatModifierData>();

        [Header("Advanced")]
        public string[] tags = new string[0];
        public ItemDefinition[] requiredItems; // For crafting/combination
        public bool isUnique = false; // Only one can be owned at a time

        [Header("Visual Effects")]
        public GameObject pickupEffect;
        public GameObject equipEffect;
        public AudioClip pickupSound;
        public AudioClip equipSound;

        
        /// Apply this item's modifiers to a target GameObject
        /// Updated for Risk of Rain style stacking
        
        public void ApplyToTarget(GameObject target, int stackCount = 1)
        {
            var modifierManager = target.GetComponent<ModifierManager>();
            if (modifierManager == null)
            {
                Debug.LogWarning($"No ModifierManager found on {target.name} when applying item {itemName}");
                return;
            }

            // Get current stack count for this item
            int currentStacks = modifierManager.GetItemStackCount(itemName);
            int newTotalStacks = currentStacks + stackCount;

            Debug.Log($"[Fatal Odds] Applying {itemName}: Current stacks: {currentStacks}, Adding: {stackCount}, New total: {newTotalStacks}");

            // Remove existing modifiers from this item (we'll recalculate with new stack count)
            modifierManager.RemoveModifiersFromSource(itemName);

            // Apply modifiers with the new total stack count
            foreach (var modifierData in modifiers)
            {
                // Calculate effective value based on stacking
                float effectiveValue = CalculateStackedValue(modifierData.value, newTotalStacks);

                // Create a new modifier with the stacked value
                var modifier = StatModifier.Create(modifierData.statGuid, modifierData.modifierType, effectiveValue)
                    .WithSource(itemName)
                    .WithStacking(modifierData.stackingBehavior)
                    .WithPriority(modifierData.priority)
                    .Build();

                modifierManager.AddModifier(modifier);

                Debug.Log($"[Fatal Odds] Applied modifier: {modifierData.modifierType} {effectiveValue} to {modifierData.statDisplayName} from {itemName} (x{newTotalStacks})");
            }

            // Update the stack count in the modifier manager
            modifierManager.SetItemStackCount(itemName, newTotalStacks);

            // Force apply calculated values to actual stat fields
            foreach (var modifierData in modifiers)
            {
                modifierManager.ApplyCalculatedValue(modifierData.statGuid);
            }
        }

        
        /// Calculate the effective value based on item stacking type and count
        
        private float CalculateStackedValue(float baseValue, int stackCount)
        {
            if (stackCount <= 1) return baseValue;

            switch (stackingType)
            {
                case ItemStackingType.Linear:
                    // Linear stacking: each stack adds full effect
                    return baseValue * stackCount;

                case ItemStackingType.Diminishing:
                    // Diminishing returns: first stack = 100%, additional stacks = 50%
                    return baseValue + (baseValue * 0.5f * (stackCount - 1));

                default:
                    return baseValue * stackCount;
            }
        }

        
        /// Remove this item's modifiers from a target GameObject
        
        public void RemoveFromTarget(GameObject target)
        {
            var modifierManager = target.GetComponent<ModifierManager>();
            if (modifierManager == null) return;

            modifierManager.RemoveModifiersFromSource(itemName);
        }

        
        /// Get the total modifier value for a specific stat
        
        public float GetModifierForStat(string statGuid)
        {
            float total = 0f;
            foreach (var modifier in modifiers)
            {
                if (modifier.statGuid == statGuid)
                {
                    total += modifier.value;
                }
            }
            return total;
        }

        
        /// Check if this item affects a specific stat
        
        public bool AffectsStat(string statGuid)
        {
            return modifiers.Exists(m => m.statGuid == statGuid);
        }

        
        /// Get a formatted description including all modifiers and Risk of Rain style info
        
        public string GetFullDescription()
        {
            var fullDesc = description;

            if (modifiers.Count > 0)
            {
                fullDesc += "\n\nEffects:";
                foreach (var modifier in modifiers)
                {
                    fullDesc += $"\n• {modifier.GetDisplayText()}";
                }

                // Add stacking info
                string stackingInfo = stackingType == ItemStackingType.Linear
                    ? "Stacks linearly (+100% per stack)"
                    : "Stacks with diminishing returns (+50% per additional stack)";
                fullDesc += $"\n\n{stackingInfo}";
            }

            if (!string.IsNullOrEmpty(flavorText))
            {
                fullDesc += $"\n\n\"{flavorText}\"";
            }

            if (tags.Length > 0)
            {
                fullDesc += $"\n\nTags: {string.Join(", ", tags)}";
            }

            return fullDesc;
        }
    }

    
    /// ScriptableObject definition for abilities generated from the graph editor
    
    [CreateAssetMenu(fileName = "New Ability", menuName = "Fatal Odds/Ability Definition")]
    public class AbilityDefinition : ScriptableObject
    {
        [Header("Basic Information")]
        public string abilityName = "New Ability";
        [TextArea(3, 5)]
        public string description = "A powerful ability...";
        public Texture2D icon;

        [Header("Ability Properties")]
        public AbilityTargetType targetType = AbilityTargetType.Self;
        public float cooldown = 5f;
        public float energyCost = 25f;
        public float castTime = 0f;
        public float range = 0f; // 0 = infinite/no range limit

        [Header("Effects")]
        public List<StatModifierData> modifiers = new List<StatModifierData>();
        public AbilityEffectType effectType = AbilityEffectType.Buff;
        public float effectDuration = 0f; // 0 = permanent

        [Header("Advanced")]
        public bool canBeInterrupted = true;
        public bool requiresLineOfSight = false;
        public int maxTargets = 1;
        public string[] requiredTags = new string[0];
        public AbilityDefinition[] prerequisites; // Required abilities

        [Header("Visual & Audio")]
        public GameObject castEffect;
        public GameObject impactEffect;
        public GameObject channelEffect;
        public AudioClip castSound;
        public AudioClip impactSound;

        
        /// Check if this ability can be used on the specified target
        
        public bool CanUseOnTarget(GameObject caster, GameObject target)
        {
            if (target == null && targetType != AbilityTargetType.Ground)
                return false;

            switch (targetType)
            {
                case AbilityTargetType.Self:
                    return target == caster;

                case AbilityTargetType.Enemy:
                    return IsEnemyTarget(target);

                case AbilityTargetType.Ally:
                    return IsAllyTarget(target, caster);

                case AbilityTargetType.Ground:
                    return true; // Ground targeting always valid

                case AbilityTargetType.Area:
                    return true; // Area effects don't need specific targets

                default:
                    return false;
            }
        }

        
        /// Generic enemy detection using multiple fallback methods
        
        private bool IsEnemyTarget(GameObject target)
        {
            // Method 1: Check for Enemy tag
            if (target.CompareTag("Enemy"))
                return true;

            // Method 2: Check if name contains "enemy" (case insensitive)
            if (target.name.ToLower().Contains("enemy"))
                return true;

            // Method 3: Look for any component with "Enemy" in its type name
            var components = target.GetComponents<MonoBehaviour>();
            foreach (var component in components)
            {
                if (component != null && component.GetType().Name.ToLower().Contains("enemy"))
                    return true;
            }

            // Method 4: Check for common enemy-related interfaces using reflection
            var interfaces = target.GetComponents<MonoBehaviour>();
            foreach (var component in interfaces)
            {
                if (component != null)
                {
                    var type = component.GetType();
                    // Check if it implements any interface with "Enemy" in the name
                    foreach (var interfaceType in type.GetInterfaces())
                    {
                        if (interfaceType.Name.ToLower().Contains("enemy"))
                            return true;
                    }

                    // Check if it inherits from any class with "Enemy" in the name
                    var baseType = type.BaseType;
                    while (baseType != null && baseType != typeof(MonoBehaviour))
                    {
                        if (baseType.Name.ToLower().Contains("enemy"))
                            return true;
                        baseType = baseType.BaseType;
                    }
                }
            }

            // Method 5: Check for specific method signatures that enemies typically have
            foreach (var component in components)
            {
                if (component != null)
                {
                    var type = component.GetType();
                    // Look for common enemy methods
                    if (type.GetMethod("TakeDamage") != null ||
                        type.GetMethod("Die") != null ||
                        type.GetMethod("IsDead") != null)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        
        /// Generic ally detection
        
        private bool IsAllyTarget(GameObject target, GameObject caster)
        {
            // Not self and not enemy = ally
            return target != caster && !IsEnemyTarget(target);
        }

        
        /// Check if the caster is in range of the target
        
        public bool IsInRange(Vector3 casterPosition, Vector3 targetPosition)
        {
            if (range <= 0f) return true; // No range limit

            float distance = Vector3.Distance(casterPosition, targetPosition);
            return distance <= range;
        }

        
        /// Apply this ability's effects to a target
        
        public void ApplyToTarget(GameObject caster, GameObject target, Vector3 position)
        {
            if (target != null)
            {
                var modifierManager = target.GetComponent<ModifierManager>();
                if (modifierManager != null)
                {
                    foreach (var modifierData in modifiers)
                    {
                        var modifier = modifierData.CreateModifier($"{abilityName} (from {caster.name})");

                        // Override duration if ability has a specific duration
                        if (effectDuration > 0f)
                        {
                            modifier = StatModifier.Create(modifier.StatGuid, modifier.Type, modifier.Value)
                                .WithSource(modifier.Source)
                                .WithDuration(effectDuration)
                                .Build();
                        }

                        modifierManager.AddModifier(modifier);
                    }
                }
            }

            // Spawn visual effects
            if (impactEffect != null)
            {
                Vector3 effectPosition = target != null ? target.transform.position : position;
                Instantiate(impactEffect, effectPosition, Quaternion.identity);
            }

            // Play impact sound
            if (impactSound != null && caster != null)
            {
                AudioSource.PlayClipAtPoint(impactSound, caster.transform.position);
            }
        }

        
        /// Get a formatted description including all effects
        
        public string GetFullDescription()
        {
            var fullDesc = description;

            fullDesc += $"\n\nCooldown: {cooldown}s";
            fullDesc += $"\nEnergy Cost: {energyCost}";

            if (castTime > 0f)
                fullDesc += $"\nCast Time: {castTime}s";

            if (range > 0f)
                fullDesc += $"\nRange: {range}m";

            if (modifiers.Count > 0)
            {
                fullDesc += "\n\nEffects:";
                foreach (var modifier in modifiers)
                {
                    fullDesc += $"\n• {modifier.GetDisplayText()}";
                    if (effectDuration > 0f)
                        fullDesc += $" (for {effectDuration}s)";
                }
            }

            return fullDesc;
        }
    }

    
    /// Serializable data structure for stat modifiers in ScriptableObjects
    
    [Serializable]
    public class StatModifierData
    {
        public string statGuid;
        public string statDisplayName; // For editor display
        public ModifierType modifierType;
        public float value;
        public StackingBehavior stackingBehavior;
        public int priority;

        // Special parameters for complex modifier types
        public float hyperbolicConstant = 100f;
        public float exponentialBase = 2f;
        public string conditionExpression;

        public StatModifierData()
        {
            modifierType = ModifierType.Flat;
            value = 0f;
            stackingBehavior = StackingBehavior.Additive;
            priority = 500;
        }

        public StatModifierData(string statGuid, ModifierType type, float value, string displayName = "")
        {
            this.statGuid = statGuid;
            this.modifierType = type;
            this.value = value;
            this.statDisplayName = displayName;
            this.stackingBehavior = StatModifier.GetDefaultStackingBehavior(type);
            this.priority = StatModifier.GetDefaultPriority(type);
        }

        
        /// Create a runtime StatModifier from this data
        
        public StatModifier CreateModifier(string source)
        {
            var modifier = StatModifier.Create(statGuid, modifierType, value)
                .WithSource(source)
                .WithStacking(stackingBehavior)
                .WithPriority(priority);

            if (modifierType == ModifierType.Hyperbolic)
                modifier.WithHyperbolicConstant(hyperbolicConstant);

            if (modifierType == ModifierType.Exponential)
                modifier.WithExponentialBase(exponentialBase);

            if (!string.IsNullOrEmpty(conditionExpression))
                modifier.WithCondition(conditionExpression);

            return modifier.Build();
        }

        
        /// Get a human-readable description of this modifier
        
        public string GetDisplayText()
        {
            string statName = !string.IsNullOrEmpty(statDisplayName) ? statDisplayName : statGuid;

            switch (modifierType)
            {
                case ModifierType.Flat:
                    return $"{(value >= 0 ? "+" : "")}{value:F1} {statName}";

                case ModifierType.Percentage:
                case ModifierType.PercentageMultiplicative:
                    {
                        float pct = (value - 1f) * 100f;          // multiplier -> %
                        return $"{(pct >= 0 ? "+" : "")}{pct:F0}% {statName}";
                    }

                case ModifierType.PercentageAdditive:
                    {
                        float pct = value * 100f;                 // fraction -> %
                        return $"{(pct >= 0 ? "+" : "")}{pct:F0}% {statName}";
                    }

                case ModifierType.Override:
                    return $"Set {statName} to {value:F1}";

                case ModifierType.Minimum:
                    return $"Minimum {statName}: {value:F1}";

                case ModifierType.Maximum:
                    return $"Maximum {statName}: {value:F1}";

                case ModifierType.Hyperbolic:
                    return $"{value:F1} {statName} (diminishing returns)";

                case ModifierType.Exponential:
                    return $"{value:F1}× {statName} (exponential)";

                case ModifierType.Logarithmic:
                    return $"{value:F1} {statName} (logarithmic)";

                default:
                    return $"{value:F1} {statName}";
            }
        }
    }

    
    /// Enums for item and ability properties
    
    public enum ItemRarity
    {
        Common,
        Uncommon,
        Rare,
        Epic,
        Legendary,
        Artifact
    }

    
    /// Risk of Rain style item stacking types
    
    public enum ItemStackingType
    {
        Linear,        // Each stack adds full effect (linear stacking)
        Diminishing    // Diminishing returns per additional stack
    }

    public enum AbilityTargetType
    {
        Self,        // Can only target the caster
        Enemy,       // Can only target enemies
        Ally,        // Can only target allies
        Ground,      // Target a position on the ground
        Area         // Area of effect around caster
    }

    public enum AbilityEffectType
    {
        Buff,        // Positive effect
        Debuff,      // Negative effect
        Damage,      // Direct damage
        Heal,        // Direct healing
        Utility      // Other effects
    }
}