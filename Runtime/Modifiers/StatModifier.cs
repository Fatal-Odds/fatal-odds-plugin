using System;
using UnityEngine;

namespace FatalOdds.Runtime
{
    
    /// Base class for all stat modifiers
    
    [Serializable]
    public class StatModifier
    {
        [SerializeField] private string id;
        [SerializeField] private string statGuid;
        [SerializeField] private ModifierType type;
        [SerializeField] private float value;
        [SerializeField] private StackingBehavior stackingBehavior;
        [SerializeField] internal int priority;
        [SerializeField] private string source; // Item/Ability that created this modifier
        [SerializeField] private bool isTemporary;
        [SerializeField] private float duration;
        [SerializeField] private float remainingDuration;

        // For hyperbolic calculations
        [SerializeField] internal float hyperbolicConstant = 100f;

        // For exponential calculations
        [SerializeField] internal float exponentialBase = 2f;

        // For conditional modifiers
        [SerializeField] internal string conditionExpression;

        public string ID => id;
        public string StatGuid => statGuid;
        public ModifierType Type => type;
        public float Value => value;
        public StackingBehavior Stacking => stackingBehavior;
        public int Priority => priority;
        public string Source => source;
        public bool IsTemporary => isTemporary;
        public float Duration => duration;
        public float RemainingDuration => remainingDuration;

        public StatModifier(string statGuid, ModifierType type, float value)
        {
            this.id = Guid.NewGuid().ToString();
            this.statGuid = statGuid;
            this.type = type;
            this.value = value;
            this.stackingBehavior = GetDefaultStackingBehavior(type);
            this.priority = GetDefaultPriority(type);
            this.isTemporary = false;
        }

        
        /// Full constructor with all options
        
        public StatModifier(string statGuid, ModifierType type, float value,
            StackingBehavior stacking, string source = null, float duration = 0f)
        {
            this.id = Guid.NewGuid().ToString();
            this.statGuid = statGuid;
            this.type = type;
            this.value = value;
            this.stackingBehavior = stacking;
            this.priority = GetDefaultPriority(type);
            this.source = source;
            this.isTemporary = duration > 0;
            this.duration = duration;
            this.remainingDuration = duration;
        }

        
        /// Apply this modifier to a base value
        
        public virtual float Apply(float baseValue, float currentValue)
        {
            switch (type)
            {
                case ModifierType.Flat:
                    return currentValue + value;

                case ModifierType.Percentage:
                case ModifierType.PercentageMultiplicative:
                    // expects a multiplier: 1.25 = +25 %
                    return currentValue * value;

                case ModifierType.PercentageAdditive:
                    // expects a FRACTION: 0.25 = +25 %
                    return currentValue * (1f + value);

                case ModifierType.Override:
                    return value;

                case ModifierType.Minimum:
                    return Mathf.Max(currentValue, value);

                case ModifierType.Maximum:
                    return Mathf.Min(currentValue, value);

                case ModifierType.Hyperbolic:
                    {
                        float k = hyperbolicConstant;
                        float factor = value / (value + k);
                        return currentValue * (1f + factor);
                    }

                case ModifierType.Exponential:
                    return currentValue * Mathf.Pow(exponentialBase, value);

                case ModifierType.Logarithmic:
                    {
                        float ln = value > 0 ? Mathf.Log(1f + value) : 0f;
                        return currentValue * (1f + ln);
                    }

                // Conditional branches unchanged
                case ModifierType.ConditionalFlat:
                case ModifierType.ConditionalPercentage:
                    return EvaluateCondition()
                         ? (type == ModifierType.ConditionalFlat
                             ? currentValue + value
                             : currentValue * value)
                         : currentValue;

                default:
                    return currentValue;
            }
        }

        
        /// Update temporary modifier duration
        
        public void UpdateDuration(float deltaTime)
        {
            if (isTemporary)
            {
                remainingDuration -= deltaTime;
            }
        }

        
        /// Check if this modifier has expired
        
        public bool IsExpired()
        {
            return isTemporary && remainingDuration <= 0;
        }

        
        /// Get default stacking behavior for a modifier type
        /// CHANGED: Made public instead of internal
        
        public static StackingBehavior GetDefaultStackingBehavior(ModifierType type)
        {
            switch (type)
            {
                case ModifierType.Flat:
                case ModifierType.PercentageAdditive:
                    return StackingBehavior.Additive;

                case ModifierType.Percentage:
                case ModifierType.PercentageMultiplicative:
                case ModifierType.Hyperbolic:
                case ModifierType.Exponential:
                    return StackingBehavior.Multiplicative;

                case ModifierType.Override:
                case ModifierType.Minimum:
                case ModifierType.Maximum:
                    return StackingBehavior.Override;

                default:
                    return StackingBehavior.Additive;
            }
        }

        
        /// Get default priority for a modifier type
        /// CHANGED: Made public instead of internal
        
        public static int GetDefaultPriority(ModifierType type)
        {
            switch (type)
            {
                case ModifierType.Override:
                    return (int)CalculationPriority.Override;
                case ModifierType.Minimum:
                    return (int)CalculationPriority.Minimum;
                case ModifierType.Maximum:
                    return (int)CalculationPriority.Maximum;
                case ModifierType.Flat:
                    return (int)CalculationPriority.FlatAdditive;
                case ModifierType.PercentageAdditive:
                    return (int)CalculationPriority.PercentageAdditive;
                case ModifierType.Percentage:
                case ModifierType.PercentageMultiplicative:
                    return (int)CalculationPriority.PercentageMultiplicative;
                case ModifierType.Hyperbolic:
                    return (int)CalculationPriority.Hyperbolic;
                case ModifierType.Exponential:
                    return (int)CalculationPriority.Exponential;
                case ModifierType.Logarithmic:
                    return (int)CalculationPriority.Logarithmic;
                case ModifierType.ConditionalFlat:
                case ModifierType.ConditionalPercentage:
                    return (int)CalculationPriority.Conditional;
                default:
                    return 500;
            }
        }

        
        /// Evaluate condition for conditional modifiers
        
        private bool EvaluateCondition()
        {
            // This is a placeholder - in a full implementation, you'd have
            // a condition evaluation system (e.g., checking player state)
            // For now, always return true
            return true;
        }

        
        /// Create a builder for fluent modifier creation
        
        public static ModifierBuilder Create(string statGuid, ModifierType type, float value)
        {
            return new ModifierBuilder(statGuid, type, value);
        }
    }

    
    /// Builder pattern for creating modifiers with fluent syntax
    
    public class ModifierBuilder
    {
        private string statGuid;
        private ModifierType type;
        private float value;
        private StackingBehavior stackingBehavior;
        private string source;
        private bool isTemporary;
        private float duration;
        private float remainingDuration;
        private int priority;
        private float hyperbolicConstant = 100f;
        private float exponentialBase = 2f;
        private string conditionExpression;

        public ModifierBuilder(string statGuid, ModifierType type, float value)
        {
            this.statGuid = statGuid;
            this.type = type;
            this.value = value;
            this.stackingBehavior = StatModifier.GetDefaultStackingBehavior(type);
            this.priority = StatModifier.GetDefaultPriority(type);
            this.isTemporary = false;
        }

        public ModifierBuilder WithStacking(StackingBehavior stacking)
        {
            this.stackingBehavior = stacking;
            return this;
        }

        public ModifierBuilder WithSource(string source)
        {
            this.source = source;
            return this;
        }

        public ModifierBuilder WithDuration(float duration)
        {
            this.isTemporary = duration > 0;
            this.duration = duration;
            this.remainingDuration = duration;
            return this;
        }

        public ModifierBuilder WithPriority(int priority)
        {
            this.priority = priority;
            return this;
        }

        public ModifierBuilder WithHyperbolicConstant(float constant)
        {
            this.hyperbolicConstant = constant;
            return this;
        }

        public ModifierBuilder WithExponentialBase(float baseValue)
        {
            this.exponentialBase = baseValue;
            return this;
        }

        public ModifierBuilder WithCondition(string condition)
        {
            this.conditionExpression = condition;
            return this;
        }

        public StatModifier Build()
        {
            var modifier = new StatModifier(statGuid, type, value, stackingBehavior, source, duration);
            modifier.priority = priority;
            modifier.hyperbolicConstant = hyperbolicConstant;
            modifier.exponentialBase = exponentialBase;
            modifier.conditionExpression = conditionExpression;
            return modifier;
        }
    }
}