using System;
using System.Reflection;
using UnityEngine;
using System.Linq;

namespace FatalOdds.Runtime
{
    
    /// Represents information about a discovered stat
    /// Enhanced with robust cross-assembly field restoration
    
    [Serializable]
    public class StatInfo
    {
        [SerializeField] private string guid;
        [SerializeField] private string fieldName;
        [SerializeField] private string displayName;
        [SerializeField] private string category;
        [SerializeField] private string description;
        [SerializeField] private string declaringType;
        [SerializeField] private string assemblyQualifiedName; // NEW: Store full assembly info
        [SerializeField] private bool showInUI;

        // Non-serialized runtime data
        [NonSerialized] private FieldInfo fieldInfo;
        [NonSerialized] private Type cachedType;
        [NonSerialized] private bool restorationAttempted;

        public string GUID => guid;
        public string FieldName => fieldName;
        public string DisplayName => displayName;
        public string Category => category;
        public string Description => description;
        public string DeclaringType => declaringType;
        public bool ShowInUI => showInUI;
        public FieldInfo FieldInfo => fieldInfo;

        public StatInfo(FieldInfo field, StatTagAttribute attribute, Type declaringType)
        {
            this.fieldInfo = field;
            this.fieldName = field.Name;
            this.declaringType = declaringType.FullName;
            this.assemblyQualifiedName = declaringType.AssemblyQualifiedName; // Store full assembly info
            this.cachedType = declaringType;

            // Generate unique GUID for this stat
            this.guid = $"{declaringType.FullName}.{field.Name}";

            // Extract info from attribute
            this.displayName = attribute.StatName ?? field.Name;
            this.category = attribute.Category ?? "General";
            this.description = attribute.Description ?? $"Modifies {displayName}";
            this.showInUI = attribute.ShowInUI;
        }

        
        /// Get the current value of this stat from a target object
        
        public float GetValue(object target)
        {
            if (target == null)
            {
                Debug.LogWarning($"[Fatal Odds] Target is null when getting value for stat {displayName}");
                return 0f;
            }

            // Ensure field info is restored
            if (!EnsureFieldInfoRestored())
            {
                Debug.LogError($"[Fatal Odds] Could not restore field info for stat {displayName}. Component type: {target.GetType().Name}");
                return 0f;
            }

            try
            {
                object value = fieldInfo.GetValue(target);
                return Convert.ToSingle(value);
            }
            catch (Exception e)
            {
                Debug.LogError($"[Fatal Odds] Failed to get value for stat {displayName} from {target.GetType().Name}: {e.Message}");
                return 0f;
            }
        }

        
        /// Set the value of this stat on a target object
        
        public void SetValue(object target, float value)
        {
            if (target == null)
            {
                Debug.LogWarning($"[Fatal Odds] Target is null when setting value for stat {displayName}");
                return;
            }

            // Ensure field info is restored
            if (!EnsureFieldInfoRestored())
            {
                Debug.LogError($"[Fatal Odds] Could not restore field info for stat {displayName}. Component type: {target.GetType().Name}");
                return;
            }

            try
            {
                // Convert the value to the correct type
                object convertedValue = Convert.ChangeType(value, fieldInfo.FieldType);
                fieldInfo.SetValue(target, convertedValue);
            }
            catch (Exception e)
            {
                Debug.LogError($"[Fatal Odds] Failed to set value for stat {displayName} on {target.GetType().Name}: {e.Message}");
            }
        }

        
        /// Ensure the FieldInfo is properly restored with multiple fallback methods
        
        private bool EnsureFieldInfoRestored()
        {
            // If we already have fieldInfo, we're good
            if (fieldInfo != null) return true;

            // If we already tried and failed, don't keep trying
            if (restorationAttempted) return false;

            restorationAttempted = true;

            // Method 1: Try with assembly qualified name (most reliable)
            if (!string.IsNullOrEmpty(assemblyQualifiedName))
            {
                if (TryRestoreWithAssemblyQualifiedName()) return true;
            }

            // Method 2: Try with full name
            if (TryRestoreWithFullName()) return true;

            // Method 3: Search all assemblies for the type
            if (TryRestoreBySearchingAssemblies()) return true;

            Debug.LogError($"[Fatal Odds] All restoration methods failed for stat {displayName} (field: {fieldName}, type: {declaringType})");
            return false;
        }

        
        /// Try to restore using assembly qualified name
        
        private bool TryRestoreWithAssemblyQualifiedName()
        {
            try
            {
                cachedType = Type.GetType(assemblyQualifiedName);
                if (cachedType != null)
                {
                    fieldInfo = cachedType.GetField(fieldName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                    if (fieldInfo != null)
                    {
                        //Debug.Log($"[Fatal Odds] Restored field info for {displayName} using assembly qualified name");
                        return true;
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[Fatal Odds] Failed to restore with assembly qualified name for {displayName}: {e.Message}");
            }
            return false;
        }

        
        /// Try to restore using full name
        
        private bool TryRestoreWithFullName()
        {
            try
            {
                cachedType = Type.GetType(declaringType);
                if (cachedType != null)
                {
                    fieldInfo = cachedType.GetField(fieldName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                    if (fieldInfo != null)
                    {
                        Debug.Log($"[Fatal Odds] Restored field info for {displayName} using full name");
                        return true;
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[Fatal Odds] Failed to restore with full name for {displayName}: {e.Message}");
            }
            return false;
        }

        
        /// Search all loaded assemblies for the type
        
        private bool TryRestoreBySearchingAssemblies()
        {
            try
            {
                var assemblies = AppDomain.CurrentDomain.GetAssemblies();

                foreach (var assembly in assemblies)
                {
                    // Skip system assemblies for performance
                    if (assembly.FullName.StartsWith("System") ||
                        assembly.FullName.StartsWith("mscorlib") ||
                        assembly.FullName.StartsWith("Unity") ||
                        assembly.FullName.StartsWith("Microsoft"))
                        continue;

                    try
                    {
                        var types = assembly.GetTypes();
                        cachedType = types.FirstOrDefault(t => t.FullName == declaringType);

                        if (cachedType != null)
                        {
                            fieldInfo = cachedType.GetField(fieldName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                            if (fieldInfo != null)
                            {
                                Debug.Log($"[Fatal Odds] Restored field info for {displayName} by searching assemblies (found in {assembly.GetName().Name})");
                                return true;
                            }
                        }
                    }
                    catch (Exception)
                    {
                        // Some assemblies might not be accessible, skip them
                        continue;
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[Fatal Odds] Failed to search assemblies for {displayName}: {e.Message}");
            }
            return false;
        }

        
        /// Restore the FieldInfo reference after deserialization
        /// Called by StatRegistry on load
        
        public void RestoreFieldInfo()
        {
            EnsureFieldInfoRestored();
        }

        
        /// Force a fresh restoration attempt (useful for debugging)
        
        public void ForceRestoreFieldInfo()
        {
            fieldInfo = null;
            cachedType = null;
            restorationAttempted = false;
            EnsureFieldInfoRestored();
        }

        
        /// Check if this StatInfo is valid and usable
        
        public bool IsValid()
        {
            return EnsureFieldInfoRestored() && fieldInfo != null;
        }

        
        /// Get debug information about this stat
        
        public string GetDebugInfo()
        {
            var debug = $"StatInfo Debug for {displayName}:\n";
            debug += $"  GUID: {guid}\n";
            debug += $"  Field Name: {fieldName}\n";
            debug += $"  Declaring Type: {declaringType}\n";
            debug += $"  Assembly Qualified: {assemblyQualifiedName}\n";
            debug += $"  Field Info: {(fieldInfo != null ? "OK" : "NULL")}\n";
            debug += $"  Cached Type: {(cachedType != null ? cachedType.Name : "NULL")}\n";
            debug += $"  Restoration Attempted: {restorationAttempted}\n";
            debug += $"  Is Valid: {IsValid()}\n";
            return debug;
        }
    }
}