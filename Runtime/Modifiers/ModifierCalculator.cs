using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace FatalOdds.Runtime
{
    
    /// Centralised stat‑math.  Handles:
    /// • Flat adds
    /// • % additive (fractions: 0.15 = +15 %)
    /// • % multiplicative (multipliers: 1.25 = +25 %)
    /// • Special curves (hyperbolic etc.)
    /// 
    /// Design goals  🠖 Risk‑of‑Rain / LoL feel:
    ///  1. 100 % reproducible order – lowest priority first.
    ///  2. Additive % buffs never explode: they are *fractional* and sum
    ///     before 1 + x is applied.
    ///  3. Multiplicative buffs compound, but you can cap the final value
    ///     with FinalClampMax if you truly need to.
    
    public static class ModifierCalculator
    {
        
        /// Optional hard‑cap on the resulting value.
        /// Set to <c>Mathf.Infinity</c> to disable.
        
        public const float FinalClampMax = Mathf.Infinity;

        private struct GroupKey : IEquatable<GroupKey>
        {
            internal readonly ModifierType Type;
            internal readonly StackingBehavior Stacking;

            internal GroupKey(ModifierType type, StackingBehavior stacking)
            {
                Type = type;
                Stacking = stacking;
            }

            public Boolean Equals(GroupKey other)
            {
                return Type == other.Type && Stacking == other.Stacking;
            }

            public override Int32 GetHashCode()
            {
                return ((Int32)Type * 397) ^ (Int32)Stacking;
            }
        }

        
        /// Calculate the final stat after all modifiers.
        
        public static Single CalculateFinalValue(Single baseValue, IReadOnlyList<StatModifier> modifiers)
        {
            if (modifiers == null || modifiers.Count == 0)
            {
                return baseValue;
            }

            // Sort by explicit priority once – this is the golden order.
            List<StatModifier> sorted = modifiers.OrderBy(m => m.Priority).ToList();

            // Bucket by (Type, Stacking) so we can process additive groups together
            // but still respect overall priority.
            Dictionary<GroupKey, List<StatModifier>> buckets = new Dictionary<GroupKey, List<StatModifier>>();
            Dictionary<GroupKey, Int32> bucketMinPriority = new Dictionary<GroupKey, Int32>();

            foreach (StatModifier mod in sorted)
            {
                GroupKey key = new GroupKey(mod.Type, mod.Stacking);

                if (!buckets.TryGetValue(key, out List<StatModifier> list))
                {
                    list = new List<StatModifier>();
                    buckets[key] = list;
                    bucketMinPriority[key] = mod.Priority;
                }

                list.Add(mod);
            }

            // Now process groups in ascending min‑priority order.
            Single current = baseValue;
            foreach (GroupKey key in bucketMinPriority
                                         .OrderBy(kvp => kvp.Value)
                                         .Select(kvp => kvp.Key))
            {
                List<StatModifier> group = buckets[key];

                switch (key.Stacking)
                {
                    case StackingBehavior.Additive:
                        current = ApplyAdditive(current, group);
                        break;

                    case StackingBehavior.Multiplicative:
                        current = ApplyMultiplicative(current, group);
                        break;

                    case StackingBehavior.Override:
                        current = ApplyOverride(current, group);
                        break;

                    case StackingBehavior.Average:
                        current = ApplyAverage(current, group);
                        break;

                    case StackingBehavior.Highest:
                        current = ApplyHighest(current, group);
                        break;

                    case StackingBehavior.Lowest:
                        current = ApplyLowest(current, group);
                        break;
                }
            }

            return Mathf.Min(current, FinalClampMax);
        }

        // ---------- STACKING METHODS ----------

        
        /// Additive. Handles two sub‑cases:
        /// • Flat – just sum.
        /// • PercentageAdditive – FRACTIONS that sum then (1 + Σ) * current.
        
        private static Single ApplyAdditive(Single current, IList<StatModifier> mods)
        {
            if (mods.Count == 0) return current;

            if (mods[0].Type == ModifierType.PercentageAdditive)
            {
                Single fractionSum = 0f;
                foreach (StatModifier m in mods)
                {
                    fractionSum += m.Value;          // 0.15 = 15 %
                }

                return current * (1f + fractionSum);
            }

            // Flat / other additive types
            Single addSum = 0f;
            foreach (StatModifier m in mods)
            {
                addSum += m.Apply(current, current) - current;
            }

            return current + addSum;
        }

        
        /// Multiplicative – multiply all multipliers, apply special curves inline.
        
        private static Single ApplyMultiplicative(Single current, IList<StatModifier> mods)
        {
            if (mods.Count == 0) return current;

            Single totalMultiplier = 1f;

            foreach (StatModifier m in mods)
            {
                switch (m.Type)
                {
                    case ModifierType.Percentage:
                    case ModifierType.PercentageMultiplicative:
                        totalMultiplier *= m.Value;   // actual multiplier
                        break;

                    case ModifierType.Hyperbolic:
                    case ModifierType.Exponential:
                    case ModifierType.Logarithmic:
                        current = m.Apply(current, current);
                        break;

                    default:
                        // Defensive fallback – treat as inline.
                        current = m.Apply(current, current);
                        break;
                }
            }

            return current * totalMultiplier;
        }

        private static Single ApplyOverride(Single current, IList<StatModifier> mods)
        {
            return mods.Count > 0 ? mods.Last().Apply(current, current) : current;
        }

        private static Single ApplyAverage(Single current, IList<StatModifier> mods)
        {
            if (mods.Count == 0) return current;

            Single sum = 0f;
            foreach (StatModifier m in mods)
            {
                sum += m.Apply(current, current);
            }

            return sum / mods.Count;
        }

        private static Single ApplyHighest(Single current, IList<StatModifier> mods)
        {
            Single highest = current;
            foreach (StatModifier m in mods)
            {
                Single modified = m.Apply(current, current);
                if (modified > highest) highest = modified;
            }
            return highest;
        }

        private static Single ApplyLowest(Single current, IList<StatModifier> mods)
        {
            Single lowest = current;
            foreach (StatModifier m in mods)
            {
                Single modified = m.Apply(current, current);
                if (modified < lowest) lowest = modified;
            }
            return lowest;
        }

        // ---------- DEBUG HELPERS ----------

        public static String GetCalculationBreakdown(Single baseValue, IReadOnlyList<StatModifier> modifiers)
        {
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            sb.AppendLine($"Base Value: {baseValue:F2}");

            if (modifiers == null || modifiers.Count == 0)
            {
                sb.AppendLine("No modifiers.");
                return sb.ToString();
            }

            // Use the same bucket logic so the breakdown order matches runtime order.
            List<StatModifier> sorted = modifiers.OrderBy(m => m.Priority).ToList();
            Dictionary<GroupKey, List<StatModifier>> buckets = new Dictionary<GroupKey, List<StatModifier>>();
            Dictionary<GroupKey, Int32> bucketMinPriority = new Dictionary<GroupKey, Int32>();

            foreach (StatModifier mod in sorted)
            {
                GroupKey key = new GroupKey(mod.Type, mod.Stacking);

                if (!buckets.TryGetValue(key, out List<StatModifier> list))
                {
                    list = new List<StatModifier>();
                    buckets[key] = list;
                    bucketMinPriority[key] = mod.Priority;
                }

                list.Add(mod);
            }

            Single current = baseValue;
            foreach (GroupKey key in bucketMinPriority.OrderBy(kvp => kvp.Value).Select(kvp => kvp.Key))
            {
                List<StatModifier> group = buckets[key];
                Single before = current;

                if (key.Type == ModifierType.PercentageAdditive && key.Stacking == StackingBehavior.Additive)
                {
                    Single fracSum = 0f;
                    foreach (StatModifier m in group)
                    {
                        Single pct = m.Value * 100f;    // 0.15 -> 15 %
                        fracSum += pct;
                        sb.AppendLine($"  +{pct:F1}% from {m.Source ?? "Unknown"}");
                    }

                    current = ApplyAdditive(current, group);
                    sb.AppendLine($"      Σ %Additive: +{fracSum:F1}% -> {before:F2} -> {current:F2}");
                }
                else
                {
                    switch (key.Stacking)
                    {
                        case StackingBehavior.Additive: current = ApplyAdditive(current, group); break;
                        case StackingBehavior.Multiplicative: current = ApplyMultiplicative(current, group); break;
                        case StackingBehavior.Override: current = ApplyOverride(current, group); break;
                        case StackingBehavior.Average: current = ApplyAverage(current, group); break;
                        case StackingBehavior.Highest: current = ApplyHighest(current, group); break;
                        case StackingBehavior.Lowest: current = ApplyLowest(current, group); break;
                    }

                    sb.AppendLine($"  {key.Type} ({key.Stacking}) : {before:F2} -> {current:F2}");
                }
            }

            sb.AppendLine($"Final: {current:F2}  (Δ {(current - baseValue):+0.##;-0.##} | {(current / baseValue - 1f) * 100f:+0.##;-0.##}%)");
            return sb.ToString();
        }
    }
}
