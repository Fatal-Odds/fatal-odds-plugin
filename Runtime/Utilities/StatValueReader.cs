using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif
using System.Reflection;

namespace FatalOdds.Runtime
{
    
    /// Smart system to read actual stat values from prefabs and scene objects,
    /// preferring inspector-modified values over code defaults
    
    public static class StatValueReader
    {
        private static Dictionary<string, float> cachedValues = new Dictionary<string, float>();
        private static Dictionary<string, List<float>> sampledValues = new Dictionary<string, List<float>>();

        
        /// Gets the most appropriate base value for a stat by sampling from actual game objects
        
        public static float GetSmartBaseValue(StatInfo statInfo)
        {
            // Check cache first
            if (cachedValues.TryGetValue(statInfo.GUID, out float cachedValue))
            {
                return cachedValue;
            }

            float result = 1f;

            // Try different methods in order of preference
            result = TryGetFromPrefabs(statInfo) ??
                    TryGetFromSceneObjects(statInfo) ??
                    TryGetFromCodeDefault(statInfo) ??
                    GetPatternBasedDefault(statInfo.FieldName);

            // Cache the result
            cachedValues[statInfo.GUID] = result;
            return result;
        }

        
        /// Sample values from all prefabs that have this component type
        /// This captures inspector-modified values!
        
        private static float? TryGetFromPrefabs(StatInfo statInfo)
        {
#if UNITY_EDITOR
            try
            {
                List<float> values = new List<float>();

                // Find all prefabs in the project
                string[] prefabGuids = AssetDatabase.FindAssets("t:Prefab");

                foreach (string guid in prefabGuids)
                {
                    string path = AssetDatabase.GUIDToAssetPath(guid);
                    GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);

                    if (prefab != null)
                    {
                        // Look for components of the right type on the prefab and its children
                        Component[] components = prefab.GetComponentsInChildren<MonoBehaviour>(true);

                        foreach (var component in components)
                        {
                            if (component != null && component.GetType().FullName == statInfo.DeclaringType)
                            {
                                // Get the SERIALIZED (inspector) value, not the code default
                                float value = GetSerializedFieldValue(component, statInfo.FieldInfo);

                                if (IsValidStatValue(value, statInfo.FieldName))
                                {
                                    values.Add(value);
                                }
                            }
                        }
                    }
                }

                if (values.Count > 0)
                {
                    // Store all sampled values for analysis
                    sampledValues[statInfo.GUID] = values;

                    // Return the most common value, or median if no clear winner
                    return GetBestRepresentativeValue(values);
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[SmartStatValueReader] Error sampling from prefabs for {statInfo.DisplayName}: {e.Message}");
            }
#endif
            return null;
        }

        
        /// Gets the serialized (inspector) value of a field, not the code default
        
        private static float GetSerializedFieldValue(Component component, FieldInfo fieldInfo)
        {
#if UNITY_EDITOR
            try
            {
                // Use SerializedObject to get the actual inspector value
                SerializedObject serializedObject = new SerializedObject(component);
                SerializedProperty property = serializedObject.FindProperty(fieldInfo.Name);

                if (property != null)
                {
                    switch (property.propertyType)
                    {
                        case SerializedPropertyType.Float:
                            return property.floatValue;
                        case SerializedPropertyType.Integer:
                            return property.intValue;
                        case SerializedPropertyType.Boolean:
                            return property.boolValue ? 1f : 0f;
                        default:
                            break;
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[SmartStatValueReader] Error reading serialized value: {e.Message}");
            }
#endif

            // Fallback to reflection (gets current runtime value)
            try
            {
                object value = fieldInfo.GetValue(component);
                return Convert.ToSingle(value);
            }
            catch
            {
                return 1f;
            }
        }

        
        /// Sample from objects currently in the scene
        
        private static float? TryGetFromSceneObjects(StatInfo statInfo)
        {
            try
            {
                List<float> values = new List<float>();

                // Find all MonoBehaviours in the scene
                MonoBehaviour[] allComponents = UnityEngine.Object.FindObjectsOfType<MonoBehaviour>(true);

                foreach (var component in allComponents)
                {
                    if (component != null && component.GetType().FullName == statInfo.DeclaringType)
                    {
                        float value = GetSerializedFieldValue(component, statInfo.FieldInfo);

                        if (IsValidStatValue(value, statInfo.FieldName))
                        {
                            values.Add(value);
                        }
                    }
                }

                if (values.Count > 0)
                {
                    return GetBestRepresentativeValue(values);
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[SmartStatValueReader] Error sampling from scene for {statInfo.DisplayName}: {e.Message}");
            }

            return null;
        }

        
        /// Try to read the code default value from a new instance
        
        private static float? TryGetFromCodeDefault(StatInfo statInfo)
        {
            try
            {
                // Create a temporary instance to read the default field value
                Type componentType = Type.GetType(statInfo.DeclaringType);
                if (componentType != null && componentType.IsSubclassOf(typeof(Component)))
                {
                    // We can't easily instantiate MonoBehaviours, so skip this approach
                    // The inspector values should be sufficient
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[SmartStatValueReader] Error getting code default for {statInfo.DisplayName}: {e.Message}");
            }

            return null;
        }

        
        /// Determines if a value is reasonable for a given stat
        
        private static bool IsValidStatValue(float value, string fieldName)
        {
            // Filter out obviously invalid values
            if (float.IsNaN(value) || float.IsInfinity(value))
                return false;

            string lower = fieldName.ToLower();

            // Health should be positive
            if (lower.Contains("health") && value <= 0)
                return false;

            // Speed should be positive
            if (lower.Contains("speed") && value < 0)
                return false;

            // Damage should be positive
            if (lower.Contains("damage") && value <= 0)
                return false;

            // Most stats shouldn't be extremely large
            if (value > 10000f)
                return false;

            return true;
        }

        
        /// From a list of values, pick the most representative one
        
        private static float GetBestRepresentativeValue(List<float> values)
        {
            if (values.Count == 0) return 1f;
            if (values.Count == 1) return values[0];

            // Group by value and find the most common
            var groups = values.GroupBy(v => Mathf.Round(v * 100f) / 100f) // Round to 2 decimal places for grouping
                              .OrderByDescending(g => g.Count())
                              .ToList();

            var mostCommon = groups.First();

            // If the most common value appears significantly more than others, use it
            if (mostCommon.Count() > values.Count * 0.4f) // More than 40% of samples
            {
                return mostCommon.Key;
            }

            // Otherwise, use median for robustness
            var sorted = values.OrderBy(v => v).ToList();
            int middle = sorted.Count / 2;

            if (sorted.Count % 2 == 0)
                return (sorted[middle - 1] + sorted[middle]) / 2f;
            else
                return sorted[middle];
        }

        
        /// Fallback pattern-based defaults (your existing system)
        
        private static float GetPatternBasedDefault(string fieldName)
        {
            string lower = fieldName.ToLower();

            // Health-related
            if (lower.Contains("health") || lower.Contains("hp"))
                return 100f;

            // Damage-related
            if (lower.Contains("damage") || lower.Contains("attack") || lower.Contains("power"))
                return 20f;

            // Speed-related
            if (lower.Contains("speed") || lower.Contains("velocity") || lower.Contains("movement"))
                return 5f;

            // Jump-related
            if (lower.Contains("jump"))
                return lower.Contains("height") ? 2f : 2f; // height or count

            // Time-related
            if (lower.Contains("time") || lower.Contains("duration") || lower.Contains("delay"))
                return 1f;

            // Cooldown
            if (lower.Contains("cooldown"))
                return 3f;

            // Range/Distance
            if (lower.Contains("range") || lower.Contains("distance") || lower.Contains("radius"))
                return 10f;

            // Percentage/Chance (0-1)
            if (lower.Contains("chance") || lower.Contains("percent") || lower.Contains("probability"))
                return 0.1f;

            // Multipliers
            if (lower.Contains("multiplier") || lower.Contains("factor") || lower.Contains("scale"))
                return 1f;

            // Defense/Armor
            if (lower.Contains("defense") || lower.Contains("armor") || lower.Contains("resistance"))
                return 10f;

            // Resources
            if (lower.Contains("mana") || lower.Contains("energy") || lower.Contains("stamina"))
                return 50f;

            return 1f; // Ultimate fallback
        }

        
        /// Get detailed sampling information for debugging
        
        public static string GetSamplingInfo(string statGuid)
        {
            if (!sampledValues.ContainsKey(statGuid))
                return "No sampling data available";

            var values = sampledValues[statGuid];
            var groups = values.GroupBy(v => v).OrderByDescending(g => g.Count()).ToList();

            string info = $"Sampled {values.Count} values:\n";
            foreach (var group in groups.Take(5)) // Show top 5 most common
            {
                info += $"  {group.Key:F2} ({group.Count()} times)\n";
            }

            return info;
        }

        
        /// Clear the cache (useful when prefabs change)
        
        public static void ClearCache()
        {
            cachedValues.Clear();
            sampledValues.Clear();
        }

        
        /// Get all unique values found for a stat (for debugging/validation)
        
        public static List<float> GetAllSampledValues(string statGuid)
        {
            return sampledValues.ContainsKey(statGuid) ?
                   new List<float>(sampledValues[statGuid]) :
                   new List<float>();
        }
    }
}