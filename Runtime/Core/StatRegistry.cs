using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using System.Linq;

namespace FatalOdds.Runtime
{
    [CreateAssetMenu(fileName = "StatRegistry", menuName = "Fatal Odds/Stat Registry")]
    public class StatRegistry : ScriptableObject
    {
        [SerializeField] private List<StatInfo> registeredStats = new List<StatInfo>();
        [SerializeField] private string lastScanTime;
        [SerializeField] private int statCount;

        private Dictionary<string, StatInfo> statLookup = new Dictionary<string, StatInfo>();
        private bool lookupBuilt = false;

        public IReadOnlyList<StatInfo> RegisteredStats => registeredStats;
        public int StatCount => statCount;
        public string LastScanTime => lastScanTime;

        private void OnEnable()
        {
            RebuildLookup();
            RestoreFieldInfos();
        }

        private void Awake()
        {
            RebuildLookup();
            RestoreFieldInfos();
        }
        public void ScanForStats()
        {
            Debug.Log("[Fatal Odds] Starting stat scan...");

            registeredStats.Clear();
            statLookup.Clear();
            lookupBuilt = false;

            int totalTypesScanned = 0;
            int totalStatsFound = 0;

            Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();

            foreach (var assembly in assemblies)
            {
                // Skip Unity internal assemblies to improve performance
                if (ShouldSkipAssembly(assembly.FullName))
                    continue;

                try
                {
                    Type[] types = assembly.GetTypes();
                    foreach (var type in types)
                    {
                        // Only scan MonoBehaviour-derived classes and ScriptableObjects
                        if (!type.IsSubclassOf(typeof(MonoBehaviour)) &&
                            !type.IsSubclassOf(typeof(ScriptableObject)))
                            continue;

                        totalTypesScanned++;
                        int statsFoundInType = ScanType(type);
                        totalStatsFound += statsFoundInType;

                        if (statsFoundInType > 0)
                        {
                            //Debug.Log($"[Fatal Odds] Found {statsFoundInType} stats in {type.FullName}");
                        }
                    }
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"[Fatal Odds] Failed to scan assembly {assembly.FullName}: {e.Message}");
                }
            }

            statCount = registeredStats.Count;
            lastScanTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

            RebuildLookup();
            RestoreFieldInfos();

            Debug.Log($"[Fatal Odds] Stat scan complete. Scanned {totalTypesScanned} types and found {statCount} stats.");

#if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(this);
#endif
        }
        private bool ShouldSkipAssembly(string assemblyName)
        {
            if (string.IsNullOrEmpty(assemblyName))
                return true;

            // Skip Unity internal assemblies
            if (assemblyName.StartsWith("Unity") ||
                assemblyName.StartsWith("System") ||
                assemblyName.StartsWith("mscorlib") ||
                assemblyName.StartsWith("netstandard") ||
                assemblyName.StartsWith("Microsoft") ||
                assemblyName.StartsWith("Mono.") ||
                assemblyName.StartsWith("nunit") ||
                assemblyName.StartsWith("ReportGeneratorMerged"))
                return true;

            return false;
        }
        private int ScanType(Type type)
        {
            int statsFound = 0;

            try
            {
                FieldInfo[] fields = type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

                foreach (var field in fields)
                {
                    var attributes = field.GetCustomAttributes(typeof(StatTagAttribute), true);
                    if (attributes.Length > 0)
                    {
                        var statTag = (StatTagAttribute)attributes[0];
                        var statInfo = new StatInfo(field, statTag, type);

                        // Check for duplicates
                        if (!statLookup.ContainsKey(statInfo.GUID))
                        {
                            registeredStats.Add(statInfo);
                            statLookup[statInfo.GUID] = statInfo;
                            statsFound++;

                            //Debug.Log($"[Fatal Odds] Discovered stat: {statInfo.DisplayName} ({statInfo.GUID})");
                        }
                        else
                        {
                            Debug.LogWarning($"[Fatal Odds] Duplicate stat GUID found: {statInfo.GUID} in {type.FullName}");
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[Fatal Odds] Error scanning type {type.FullName}: {e.Message}");
            }

            return statsFound;
        }
        public StatInfo GetStat(string guid)
        {
            if (!lookupBuilt)
            {
                RebuildLookup();
            }

            statLookup.TryGetValue(guid, out StatInfo stat);
            return stat;
        }
        public List<StatInfo> GetStatsByCategory(string category)
        {
            return registeredStats.Where(s => s.Category == category).ToList();
        }
        public List<string> GetCategories()
        {
            return registeredStats.Select(s => s.Category).Distinct().OrderBy(c => c).ToList();
        }

        private void RebuildLookup()
        {
            statLookup.Clear();

            if (registeredStats != null)
            {
                foreach (var stat in registeredStats)
                {
                    if (stat != null && !string.IsNullOrEmpty(stat.GUID))
                    {
                        statLookup[stat.GUID] = stat;
                    }
                }
            }

            lookupBuilt = true;
        }
        private void RestoreFieldInfos()
        {
            if (registeredStats == null) return;

            int restoredCount = 0;
            int failedCount = 0;

            foreach (var stat in registeredStats)
            {
                if (stat != null)
                {
                    try
                    {
                        stat.RestoreFieldInfo();
                        if (stat.FieldInfo != null)
                            restoredCount++;
                        else
                            failedCount++;
                    }
                    catch (Exception e)
                    {
                        Debug.LogError($"[Fatal Odds] Failed to restore FieldInfo for {stat.DisplayName}: {e.Message}");
                        failedCount++;
                    }
                }
            }

            if (failedCount > 0)
            {
                Debug.LogWarning($"[Fatal Odds] StatRegistry: Restored {restoredCount}/{registeredStats.Count} FieldInfos. {failedCount} failed.");
            }
            else if (restoredCount > 0)
            {
                //Debug.Log($"[Fatal Odds] StatRegistry: Successfully restored {restoredCount} FieldInfos.");
            }
        }
        [ContextMenu("Force Refresh FieldInfos")]
        public void ForceRefreshFieldInfos()
        {
            Debug.Log("[Fatal Odds] Force refreshing all FieldInfos...");
            RestoreFieldInfos();
        }

        [ContextMenu("Validate All Stats")]
        public void ValidateAllStats()
        {
            if (registeredStats == null || registeredStats.Count == 0)
            {
                Debug.LogWarning("[Fatal Odds] No stats to validate. Run a scan first.");
                return;
            }

            int validStats = 0;
            int invalidStats = 0;

            Debug.Log("[Fatal Odds] Validating all registered stats...");

            foreach (var stat in registeredStats)
            {
                if (stat == null)
                {
                    Debug.LogError("[Fatal Odds] Found null StatInfo in registry");
                    invalidStats++;
                    continue;
                }

                if (stat.FieldInfo == null)
                {
                    Debug.LogWarning($"[Fatal Odds] StatInfo {stat.DisplayName} has null FieldInfo");
                    invalidStats++;
                }
                else
                {
                    validStats++;
                }
            }

            Debug.Log($"[Fatal Odds] Validation complete: {validStats} valid, {invalidStats} invalid stats");
        }
        public string GetDebugInfo()
        {
            var info = $"StatRegistry Debug Info:\n";
            info += $"  Total Stats: {StatCount}\n";
            info += $"  Last Scan: {LastScanTime}\n";
            info += $"  Lookup Built: {lookupBuilt}\n";
            info += $"  Categories: {GetCategories().Count}\n";

            var categories = GetCategories();
            foreach (var category in categories)
            {
                var statsInCategory = GetStatsByCategory(category);
                info += $"    {category}: {statsInCategory.Count} stats\n";
            }

            return info;
        }
    }
}