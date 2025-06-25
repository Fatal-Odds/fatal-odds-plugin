using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEditor;
using FatalOdds.Runtime;
using System.Linq;
using System.IO;
using System.Text.RegularExpressions;

namespace FatalOdds.Editor
{
    
    /// Advanced tool to analyze all potential stat candidates in your project
    /// and identify which ones should have StatTag attributes
    
    public class StatDiscoveryAudit : EditorWindow
    {
        public static void ShowWindow()
        {
            GetWindow<StatDiscoveryAudit>("Stat Discovery Audit");
        }

        private Vector2 scrollPosition;
        private List<StatCandidate> allCandidates = new List<StatCandidate>();
        private List<StatCandidate> filteredCandidates = new List<StatCandidate>();

        // Filters
        private bool showTagged = true;
        private bool showUntagged = true;
        private bool showGameplayRelevant = true;
        private bool showSystemFields = false;
        private string typeFilter = "";
        private StatRelevance relevanceFilter = StatRelevance.All;

        private enum StatRelevance
        {
            All,
            HighlyRelevant,    // Combat stats, movement, health
            ModeratelyRelevant, // UI settings, delays, ranges
            LowRelevant,       // System values, internal counters
            NotRelevant        // Unity internals, temp values
        }

        private class StatCandidate
        {
            public FieldInfo fieldInfo;
            public Type declaringType;
            public string displayName;
            public bool hasStatTag;
            public StatRelevance relevance;
            public string reason;
            public string suggestedCategory;
            public bool isNumeric;
            public object sampleValue;
            public string scriptPath;  // Path to the script file
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField("Stat Discovery Audit", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            // Scan button
            if (GUILayout.Button("Scan All MonoBehaviours for Potential Stats", GUILayout.Height(30)))
            {
                ScanForStatCandidates();
            }

            if (allCandidates.Count == 0)
            {
                EditorGUILayout.HelpBox("Click 'Scan' to analyze all MonoBehaviours in your project for potential stat fields.", MessageType.Info);
                return;
            }

            EditorGUILayout.Space();

            // Filters
            EditorGUILayout.LabelField("Filters", EditorStyles.boldLabel);
            EditorGUI.BeginChangeCheck();

            EditorGUILayout.BeginHorizontal();
            showTagged = EditorGUILayout.Toggle("Show Tagged", showTagged, GUILayout.Width(100));
            showUntagged = EditorGUILayout.Toggle("Show Untagged", showUntagged, GUILayout.Width(120));
            showGameplayRelevant = EditorGUILayout.Toggle("Gameplay Only", showGameplayRelevant, GUILayout.Width(120));
            showSystemFields = EditorGUILayout.Toggle("System Fields", showSystemFields, GUILayout.Width(100));
            EditorGUILayout.EndHorizontal();

            relevanceFilter = (StatRelevance)EditorGUILayout.EnumPopup("Relevance Filter", relevanceFilter);
            typeFilter = EditorGUILayout.TextField("Type Filter", typeFilter);

            if (EditorGUI.EndChangeCheck())
            {
                ApplyFilters();
            }

            EditorGUILayout.Space();

            // Bulk operations
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Add StatTag to All Highly Relevant"))
            {
                AddStatTagToRelevantFields(StatRelevance.HighlyRelevant);
            }
            if (GUILayout.Button("Add StatTag to All Moderately Relevant"))
            {
                AddStatTagToRelevantFields(StatRelevance.ModeratelyRelevant);
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space();

            // Summary
            EditorGUILayout.LabelField($"Showing {filteredCandidates.Count} of {allCandidates.Count} candidates", EditorStyles.miniLabel);

            // Results
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

            foreach (var candidate in filteredCandidates)
            {
                DrawStatCandidate(candidate);
            }

            EditorGUILayout.EndScrollView();
        }

        private void ScanForStatCandidates()
        {
            allCandidates.Clear();

            Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();

            foreach (var assembly in assemblies)
            {
                // Skip Unity internal assemblies and system assemblies
                if (assembly.FullName.StartsWith("Unity") ||
                    assembly.FullName.StartsWith("System") ||
                    assembly.FullName.StartsWith("mscorlib") ||
                    assembly.FullName.StartsWith("netstandard") ||
                    assembly.FullName.StartsWith("Microsoft"))
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

                        ScanTypeForStats(type);
                    }
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"Failed to scan assembly {assembly.FullName}: {e.Message}");
                }
            }

            ApplyFilters();
            Debug.Log($"Stat Discovery Audit found {allCandidates.Count} potential stat candidates");
        }

        private void ScanTypeForStats(Type type)
        {
            FieldInfo[] fields = type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

            foreach (var field in fields)
            {
                // Check if it's a numeric type
                if (!IsNumericType(field.FieldType))
                    continue;

                // Skip compiler-generated fields
                if (field.Name.Contains("<") || field.Name.Contains("k__BackingField"))
                    continue;

                var candidate = new StatCandidate
                {
                    fieldInfo = field,
                    declaringType = type,
                    displayName = $"{type.Name}.{field.Name}",
                    hasStatTag = field.GetCustomAttribute<StatTagAttribute>() != null,
                    isNumeric = true,
                    scriptPath = FindScriptPath(type)
                };

                // Analyze relevance
                AnalyzeStatRelevance(candidate);

                allCandidates.Add(candidate);
            }
        }

        private string FindScriptPath(Type type)
        {
            // Find the script file for this type
            MonoScript script = null;
            string[] guids = AssetDatabase.FindAssets($"{type.Name} t:MonoScript");

            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                MonoScript candidate = AssetDatabase.LoadAssetAtPath<MonoScript>(path);
                if (candidate != null && candidate.GetClass() == type)
                {
                    script = candidate;
                    break;
                }
            }

            return script != null ? AssetDatabase.GetAssetPath(script) : null;
        }

        private void AnalyzeStatRelevance(StatCandidate candidate)
        {
            string fieldName = candidate.fieldInfo.Name.ToLower();
            string typeName = candidate.declaringType.Name.ToLower();

            // High relevance - combat, movement, core gameplay
            if (IsHighRelevanceField(fieldName, typeName))
            {
                candidate.relevance = StatRelevance.HighlyRelevant;
                candidate.reason = "Core gameplay stat";
                candidate.suggestedCategory = GetSuggestedCategory(fieldName, typeName);
            }
            // Moderate relevance - settings, timers, ranges
            else if (IsModerateRelevanceField(fieldName, typeName))
            {
                candidate.relevance = StatRelevance.ModeratelyRelevant;
                candidate.reason = "Gameplay parameter";
                candidate.suggestedCategory = GetSuggestedCategory(fieldName, typeName);
            }
            // Low relevance - internal counters, debug values
            else if (IsLowRelevanceField(fieldName, typeName))
            {
                candidate.relevance = StatRelevance.LowRelevant;
                candidate.reason = "Internal/debug value";
                candidate.suggestedCategory = "Debug";
            }
            // Not relevant - Unity internals, temp values
            else
            {
                candidate.relevance = StatRelevance.NotRelevant;
                candidate.reason = "System/temporary value";
                candidate.suggestedCategory = "System";
            }
        }

        private bool IsHighRelevanceField(string fieldName, string typeName)
        {
            // Combat-related
            if (fieldName.Contains("health") || fieldName.Contains("damage") || fieldName.Contains("attack") ||
                fieldName.Contains("defense") || fieldName.Contains("armor") || fieldName.Contains("critical") ||
                fieldName.Contains("crit") || fieldName.Contains("power") || fieldName.Contains("strength"))
                return true;

            // Movement-related
            if (fieldName.Contains("speed") || fieldName.Contains("velocity") || fieldName.Contains("acceleration") ||
                fieldName.Contains("jump") || fieldName.Contains("movement") || fieldName.Contains("walk") ||
                fieldName.Contains("sprint") || fieldName.Contains("run"))
                return true;

            // Resource-related
            if (fieldName.Contains("mana") || fieldName.Contains("energy") || fieldName.Contains("stamina") ||
                fieldName.Contains("ammo") || fieldName.Contains("resource") || fieldName.Contains("fuel"))
                return true;

            // Character stats
            if (fieldName.Contains("level") || fieldName.Contains("experience") || fieldName.Contains("exp") ||
                fieldName.Contains("skill") || fieldName.Contains("stat") || fieldName.Contains("attribute"))
                return true;

            return false;
        }

        private bool IsModerateRelevanceField(string fieldName, string typeName)
        {
            // Timing and delays
            if (fieldName.Contains("time") || fieldName.Contains("delay") || fieldName.Contains("duration") ||
                fieldName.Contains("cooldown") || fieldName.Contains("interval") || fieldName.Contains("rate"))
                return true;

            // Ranges and distances
            if (fieldName.Contains("range") || fieldName.Contains("distance") || fieldName.Contains("radius") ||
                fieldName.Contains("size") || fieldName.Contains("scale") || fieldName.Contains("multiplier"))
                return true;

            // Settings and configurations
            if (fieldName.Contains("threshold") || fieldName.Contains("limit") || fieldName.Contains("max") ||
                fieldName.Contains("min") || fieldName.Contains("base") || fieldName.Contains("factor"))
                return true;

            // Visual/audio settings
            if (fieldName.Contains("volume") || fieldName.Contains("intensity") || fieldName.Contains("brightness") ||
                fieldName.Contains("opacity") || fieldName.Contains("alpha") || fieldName.Contains("fade"))
                return true;

            return false;
        }

        private bool IsLowRelevanceField(string fieldName, string typeName)
        {
            // Counters and indices
            if (fieldName.Contains("count") || fieldName.Contains("index") || fieldName.Contains("current") ||
                fieldName.Contains("last") || fieldName.Contains("previous") || fieldName.Contains("next"))
                return true;

            // Debug and development
            if (fieldName.Contains("debug") || fieldName.Contains("test") || fieldName.Contains("temp") ||
                fieldName.Contains("sample") || fieldName.Contains("example"))
                return true;

            return false;
        }

        private string GetSuggestedCategory(string fieldName, string typeName)
        {
            if (fieldName.Contains("health") || fieldName.Contains("damage") || fieldName.Contains("attack") ||
                fieldName.Contains("defense") || fieldName.Contains("armor") || fieldName.Contains("critical"))
                return "Combat";

            if (fieldName.Contains("speed") || fieldName.Contains("movement") || fieldName.Contains("jump") ||
                fieldName.Contains("walk") || fieldName.Contains("sprint"))
                return "Movement";

            if (fieldName.Contains("mana") || fieldName.Contains("energy") || fieldName.Contains("stamina") ||
                fieldName.Contains("ammo") || fieldName.Contains("resource"))
                return "Resources";

            if (fieldName.Contains("time") || fieldName.Contains("delay") || fieldName.Contains("duration") ||
                fieldName.Contains("cooldown"))
                return "Timing";

            if (typeName.Contains("weapon") || typeName.Contains("gun") || typeName.Contains("projectile"))
                return "Weapons";

            if (typeName.Contains("enemy") || typeName.Contains("ai"))
                return "AI";

            if (typeName.Contains("player"))
                return "Player";

            return "General";
        }

        private void ApplyFilters()
        {
            filteredCandidates.Clear();

            foreach (var candidate in allCandidates)
            {
                // Tag filter
                if (!showTagged && candidate.hasStatTag) continue;
                if (!showUntagged && !candidate.hasStatTag) continue;

                // Relevance filter
                if (relevanceFilter != StatRelevance.All && candidate.relevance != relevanceFilter) continue;

                // Gameplay filter
                if (showGameplayRelevant && candidate.relevance == StatRelevance.NotRelevant) continue;
                if (!showSystemFields && candidate.relevance == StatRelevance.NotRelevant) continue;

                // Type filter
                if (!string.IsNullOrEmpty(typeFilter) &&
                    !candidate.declaringType.Name.ToLower().Contains(typeFilter.ToLower())) continue;

                filteredCandidates.Add(candidate);
            }
        }

        private void DrawStatCandidate(StatCandidate candidate)
        {
            EditorGUILayout.BeginVertical("box");

            EditorGUILayout.BeginHorizontal();

            // Status icon
            string statusIcon = candidate.hasStatTag ? "✓" : "○";
            Color statusColor = candidate.hasStatTag ? Color.green : Color.yellow;

            GUIStyle iconStyle = new GUIStyle(EditorStyles.label);
            iconStyle.normal.textColor = statusColor;
            iconStyle.fontStyle = FontStyle.Bold;

            EditorGUILayout.LabelField(statusIcon, iconStyle, GUILayout.Width(20));

            // Field info
            EditorGUILayout.LabelField(candidate.displayName, EditorStyles.boldLabel);

            // Relevance badge
            Color relevanceColor = GetRelevanceColor(candidate.relevance);
            GUIStyle relevanceStyle = new GUIStyle(EditorStyles.miniButton);
            relevanceStyle.normal.textColor = relevanceColor;
            EditorGUILayout.LabelField(candidate.relevance.ToString(), relevanceStyle, GUILayout.Width(100));

            // Add StatTag button
            if (!candidate.hasStatTag && candidate.relevance != StatRelevance.NotRelevant)
            {
                if (GUILayout.Button("Add StatTag", GUILayout.Width(80)))
                {
                    AddStatTagToField(candidate);
                }
            }

            EditorGUILayout.EndHorizontal();

            // Details
            EditorGUILayout.LabelField($"Type: {candidate.fieldInfo.FieldType.Name}", EditorStyles.miniLabel);
            EditorGUILayout.LabelField($"Reason: {candidate.reason}", EditorStyles.miniLabel);
            if (!string.IsNullOrEmpty(candidate.suggestedCategory))
            {
                EditorGUILayout.LabelField($"Suggested Category: {candidate.suggestedCategory}", EditorStyles.miniLabel);
            }

            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(2);
        }

        private Color GetRelevanceColor(StatRelevance relevance)
        {
            switch (relevance)
            {
                case StatRelevance.HighlyRelevant: return Color.green;
                case StatRelevance.ModeratelyRelevant: return Color.yellow;
                case StatRelevance.LowRelevant: return Color.gray;
                case StatRelevance.NotRelevant: return Color.red;
                default: return Color.white;
            }
        }

        private void AddStatTagToField(StatCandidate candidate)
        {
            if (string.IsNullOrEmpty(candidate.scriptPath))
            {
                Debug.LogWarning($"Could not find script file for {candidate.declaringType.Name}");
                return;
            }

            try
            {
                string scriptContent = File.ReadAllText(candidate.scriptPath);

                // Find the field declaration
                string fieldPattern = $@"\b{candidate.fieldInfo.FieldType.Name}\s+{candidate.fieldInfo.Name}\b";
                Match fieldMatch = Regex.Match(scriptContent, fieldPattern);

                if (!fieldMatch.Success)
                {
                    Debug.LogWarning($"Could not find field declaration for {candidate.declaringType.Name}.{candidate.fieldInfo.Name} in {candidate.scriptPath}");
                    return;
                }

                // Check if StatTag already exists
                string lineContent = GetLineContaining(scriptContent, fieldMatch.Index);
                string previousLine = GetPreviousLine(scriptContent, fieldMatch.Index);

                if (previousLine.Contains("[StatTag") || lineContent.Contains("[StatTag"))
                {
                    Debug.Log($"StatTag already exists for {candidate.fieldInfo.Name}");
                    return;
                }

                // Insert StatTag attribute
                string statTagAttribute = $"    [StatTag(\"{candidate.fieldInfo.Name}\", \"{candidate.suggestedCategory}\")]\n";

                // Find the line start
                int lineStart = scriptContent.LastIndexOf('\n', fieldMatch.Index) + 1;

                // Insert the attribute
                string newContent = scriptContent.Insert(lineStart, statTagAttribute);

                // Write back to file
                File.WriteAllText(candidate.scriptPath, newContent);

                // Refresh AssetDatabase
                AssetDatabase.Refresh();

                // Mark as having StatTag
                candidate.hasStatTag = true;

                Debug.Log($"Added StatTag to {candidate.declaringType.Name}.{candidate.fieldInfo.Name}");

                // Show success message
                EditorUtility.DisplayDialog("StatTag Added",
                    $"Successfully added StatTag to {candidate.fieldInfo.Name}!\n\nThe script has been modified and will recompile.",
                    "OK");
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to add StatTag to {candidate.fieldInfo.Name}: {e.Message}");
                EditorUtility.DisplayDialog("Error",
                    $"Failed to add StatTag: {e.Message}",
                    "OK");
            }
        }

        private void AddStatTagToRelevantFields(StatRelevance relevance)
        {
            var candidates = allCandidates.Where(c =>
                !c.hasStatTag &&
                c.relevance == relevance &&
                !string.IsNullOrEmpty(c.scriptPath)
            ).ToList();

            if (candidates.Count == 0)
            {
                EditorUtility.DisplayDialog("No Fields Found",
                    $"No {relevance} fields found that need StatTag attributes.",
                    "OK");
                return;
            }

            bool confirm = EditorUtility.DisplayDialog("Bulk Add StatTags",
                $"This will add StatTag attributes to {candidates.Count} {relevance} fields. Continue?",
                "Yes", "Cancel");

            if (!confirm) return;

            int successCount = 0;
            foreach (var candidate in candidates)
            {
                try
                {
                    AddStatTagToField(candidate);
                    successCount++;
                }
                catch
                {
                    // Error already logged in AddStatTagToField
                }
            }

            EditorUtility.DisplayDialog("Bulk Operation Complete",
                $"Successfully added StatTag to {successCount} out of {candidates.Count} fields.",
                "OK");

            // Refresh the scan
            ScanForStatCandidates();
        }

        private string GetLineContaining(string content, int index)
        {
            int lineStart = content.LastIndexOf('\n', index) + 1;
            int lineEnd = content.IndexOf('\n', index);
            if (lineEnd == -1) lineEnd = content.Length;

            return content.Substring(lineStart, lineEnd - lineStart);
        }

        private string GetPreviousLine(string content, int index)
        {
            int currentLineStart = content.LastIndexOf('\n', index) + 1;
            if (currentLineStart <= 1) return "";

            int previousLineEnd = currentLineStart - 2; // -1 for the \n, -1 more to get to previous char
            int previousLineStart = content.LastIndexOf('\n', previousLineEnd) + 1;

            return content.Substring(previousLineStart, previousLineEnd - previousLineStart + 1);
        }

        private bool IsNumericType(Type type)
        {
            return type == typeof(int) || type == typeof(float) || type == typeof(double) ||
                   type == typeof(long) || type == typeof(short) || type == typeof(byte) ||
                   type == typeof(uint) || type == typeof(ulong) || type == typeof(ushort) ||
                   type == typeof(sbyte) || type == typeof(decimal);
        }
    }
}