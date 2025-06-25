using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

using UTime = UnityEngine.Time;

namespace FatalOdds.Runtime
{
    
    /// Manages all runtime stat modifiers, item stacks, and cache.
    /// Now caches ORIGINAL base values so linear stacks work like Risk of Rain.
    
    public class ModifierManager : MonoBehaviour
    {
        // ──────── configurable ────────
        [SerializeField] private bool showDebugInfo = true;
        [SerializeField] private bool autoApplyCalculatedValues = true;

        // ──────── state ─────
        [SerializeField] private List<StatModifier> activeModifiers = new List<StatModifier>();
        [SerializeField] private Dictionary<string, int> itemStackCounts = new Dictionary<string, int>();

        private readonly Dictionary<string, float> originalBaseValues = new Dictionary<string, float>();

        private readonly Dictionary<string, float> cachedValues = new Dictionary<string, float>();
        private readonly Dictionary<string, bool> cacheDirty = new Dictionary<string, bool>();

        private readonly Dictionary<string, MonoBehaviour> componentCache = new Dictionary<string, MonoBehaviour>();

        private StatRegistry statRegistry;

        // ──────── events ────
        public event Action<StatModifier> OnModifierAdded;
        public event Action<StatModifier> OnModifierRemoved;
        public event Action<string> OnStatChanged;
        public event Action<string, int> OnItemStackChanged;

        // ──────── properties 
        public Int32 ActiveModifierCount => activeModifiers.Count;
        public IReadOnlyDictionary<string, int> ItemStacks => itemStackCounts;

        // ──────── life-cycle 
        private void Awake()
        {
            LoadStatRegistry();
            CacheOriginalBaseValues();
        }

        private void Update()
        {
            UpdateTemporaryModifiers();
        }

        // ──────── public API 
        public void AddModifier(StatModifier modifier)
        {
            if (modifier == null)
            {
                return;
            }

            activeModifiers.Add(modifier);
            InvalidateCache(modifier.StatGuid);

            OnModifierAdded?.Invoke(modifier);
            OnStatChanged?.Invoke(modifier.StatGuid);

            if (showDebugInfo)
            {
                Debug.Log($"[Fatal Odds] Added {modifier.Type} {modifier.Value} to {GetStatName(modifier.StatGuid)} from {modifier.Source}");
            }

            if (autoApplyCalculatedValues)
            {
                ApplyCalculatedValue(modifier.StatGuid);
            }
        }

        public void RemoveModifier(StatModifier modifier)
        {
            if (modifier == null || !activeModifiers.Remove(modifier))
            {
                return;
            }

            InvalidateCache(modifier.StatGuid);

            OnModifierRemoved?.Invoke(modifier);
            OnStatChanged?.Invoke(modifier.StatGuid);

            if (showDebugInfo)
            {
                Debug.Log($"[Fatal Odds] Removed {modifier.Type} from {GetStatName(modifier.StatGuid)} ({modifier.Source})");
            }

            if (autoApplyCalculatedValues)
            {
                ApplyCalculatedValue(modifier.StatGuid);
            }
        }

        public void RemoveModifiersFromSource(string source)
        {
            var toRemove = activeModifiers.Where(m => m.Source == source).ToList();
            var touchedStats = new HashSet<string>();

            foreach (StatModifier m in toRemove)
            {
                touchedStats.Add(m.StatGuid);
                activeModifiers.Remove(m);
                InvalidateCache(m.StatGuid);

                OnModifierRemoved?.Invoke(m);
            }

            foreach (string guid in touchedStats)
            {
                OnStatChanged?.Invoke(guid);
                if (autoApplyCalculatedValues)
                {
                    ApplyCalculatedValue(guid);
                }
            }
        }

        public void SetItemStackCount(string itemName, int stacks)
        {
            int previous = GetItemStackCount(itemName);
            itemStackCounts[itemName] = stacks;

            OnItemStackChanged?.Invoke(itemName, stacks);

            if (showDebugInfo)
            {
                Debug.Log($"[Fatal Odds] {itemName} stacks: {previous} -> {stacks}");
            }
        }

        public int GetItemStackCount(string itemName)
        {
            return itemStackCounts.TryGetValue(itemName, out int count) ? count : 0;
        }

        public float GetStatValue(string statGuid)
        {
            // ─── fast-path: cached & not dirty ─────
            if (cachedValues.TryGetValue(statGuid, out float cached))
            {
                bool isDirty = cacheDirty.TryGetValue(statGuid, out bool dirtyFlag) && dirtyFlag;
                if (!isDirty)
                {
                    return cached;
                }
            }

            // ─── slow-path: recalculate ─────
            float baseVal = GetBaseStatValue(statGuid);

            List<StatModifier> mods = activeModifiers
                .Where(m => m.StatGuid == statGuid)
                .ToList();

            float final = ModifierCalculator.CalculateFinalValue(baseVal, mods);

            // update cache
            cachedValues[statGuid] = final;
            cacheDirty[statGuid] = false;

            if (showDebugInfo && mods.Count > 0)
            {
                Debug.Log(
                    $"[Fatal Odds] {GetStatName(statGuid)}: {baseVal} → {final}  (mods {mods.Count})");
            }

            return final;
        }

        public void ApplyCalculatedValue(string statGuid)
        {
            if (statRegistry == null)
            {
                return;
            }

            StatInfo info = statRegistry.GetStat(statGuid);
            if (info == null || !info.IsValid())
            {
                return;
            }

            MonoBehaviour comp = FindComponentForStat(info);
            if (comp == null)
            {
                return;
            }

            float final = GetStatValue(statGuid);
            float old = info.GetValue(comp);

            if (!Mathf.Approximately(old, final))
            {
                info.SetValue(comp, final);

                if (showDebugInfo)
                {
                    Debug.Log($"[Fatal Odds] Applied {info.DisplayName}: {old} -> {final}");
                }
            }
        }

        // ──────── helpers ───
        private void LoadStatRegistry()
        {
            statRegistry = Resources.Load<StatRegistry>("StatRegistry");

            if (statRegistry == null)
            {
                Debug.LogWarning("[Fatal Odds] StatRegistry missing from Resources.");
                return;
            }

            foreach (var stat in statRegistry.RegisteredStats)
            {
                stat.RestoreFieldInfo();
            }
        }

        private void CacheOriginalBaseValues()
        {
            if (statRegistry == null)
            {
                return;
            }

            foreach (StatInfo stat in statRegistry.RegisteredStats)
            {
                MonoBehaviour comp = FindComponentForStat(stat);
                if (comp != null && !originalBaseValues.ContainsKey(stat.GUID))
                {
                    originalBaseValues[stat.GUID] = stat.GetValue(comp);
                }
            }
        }

        private float GetBaseStatValue(string statGuid)
        {
            if (originalBaseValues.TryGetValue(statGuid, out float cached))
            {
                return cached;
            }

            StatInfo info = statRegistry?.GetStat(statGuid);
            if (info == null || !info.IsValid())
            {
                return 0f;
            }

            MonoBehaviour comp = FindComponentForStat(info);
            float val = comp != null ? info.GetValue(comp) : 0f;
            originalBaseValues[statGuid] = val;
            return val;
        }

        private MonoBehaviour FindComponentForStat(StatInfo info)
        {
            if (componentCache.TryGetValue(info.DeclaringType, out MonoBehaviour cached) && cached != null)
            {
                return cached;
            }

            foreach (MonoBehaviour comp in GetComponents<MonoBehaviour>())
            {
                if (comp == null)
                {
                    continue;
                }

                Type t = comp.GetType();
                if (t.FullName == info.DeclaringType || t.AssemblyQualifiedName == info.DeclaringType)
                {
                    componentCache[info.DeclaringType] = comp;
                    return comp;
                }

                if (Type.GetType(info.DeclaringType) is Type wanted && wanted.IsAssignableFrom(t))
                {
                    componentCache[info.DeclaringType] = comp;
                    return comp;
                }
            }

            return null;
        }

        private void UpdateTemporaryModifiers()
        {
            var expired = new List<StatModifier>();

            foreach (StatModifier m in activeModifiers.Where(m => m.IsTemporary))
            {
                m.UpdateDuration(UTime.deltaTime);
                if (m.IsExpired())
                {
                    expired.Add(m);
                }
            }

            foreach (StatModifier m in expired)
            {
                RemoveModifier(m);
            }
        }

        private void InvalidateCache(string statGuid)
        {
            cacheDirty[statGuid] = true;
        }

        private string GetStatName(string statGuid)
        {
            return statRegistry?.GetStat(statGuid)?.DisplayName ?? statGuid;
        }

        // ─────────
        //  DEBUG / UTILITIES 
        // ─────────

        
        /// Return a human-readable breakdown of how the final value
        /// for this stat is produced (great for console debugging).
        
        public string GetCalculationBreakdown(string statGuid)
        {
            float baseVal = GetBaseStatValue(statGuid);
            var mods = activeModifiers.Where(m => m.StatGuid == statGuid).ToList();
            return ModifierCalculator.GetCalculationBreakdown(baseVal, mods);
        }

        
        /// Remove every modifier and item stack, then re-apply base stats.
        
        public void ClearAllModifiers()
        {
            var modsCopy = activeModifiers.ToList();
            var affectedStats = new HashSet<string>();

            foreach (StatModifier m in modsCopy)
            {
                affectedStats.Add(m.StatGuid);
                RemoveModifier(m);                 // already invalidates cache & events
            }

            itemStackCounts.Clear();

            // rebuild base values on screen if auto-apply is on
            if (autoApplyCalculatedValues)
            {
                foreach (string guid in affectedStats)
                {
                    ApplyCalculatedValue(guid);
                }
            }

            if (showDebugInfo)
            {
                Debug.Log("[Fatal Odds] Cleared ALL modifiers and item stacks.");
            }
        }

        
        /// Get a fresh list of all active modifiers that affect this stat.
        
        public List<StatModifier> GetModifiersForStat(string statGuid)
        {
            return activeModifiers.Where(m => m.StatGuid == statGuid).ToList();
        }
    }
}
