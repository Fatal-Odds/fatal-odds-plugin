using UnityEngine;
using UnityEditor;
using FatalOdds.Runtime;
using System.Linq;

namespace FatalOdds.Editor
{
    [CustomEditor(typeof(StatRegistry))]
    public class StatRegistryInspector : UnityEditor.Editor
    {
        private string searchFilter = "";
        private string selectedCategory = "All";
        private Vector2 scrollPosition;

        public override void OnInspectorGUI()
        {
            StatRegistry registry = (StatRegistry)target;

            // Header
            EditorGUILayout.Space(10);
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Stat Registry", EditorStyles.boldLabel);
            EditorGUILayout.LabelField($"Stats: {registry.StatCount}", GUILayout.Width(80));
            EditorGUILayout.EndHorizontal();

            if (!string.IsNullOrEmpty(registry.LastScanTime))
            {
                EditorGUILayout.LabelField($"Last scan: {registry.LastScanTime}", EditorStyles.miniLabel);
            }

            EditorGUILayout.Space(5);

            // Scan button
            if (GUILayout.Button("Scan for Stats", GUILayout.Height(30)))
            {
                registry.ScanForStats();
            }

            EditorGUILayout.Space(10);
            EditorGUILayout.Separator();
            EditorGUILayout.Space(10);

            // Filters
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Search:", GUILayout.Width(50));
            searchFilter = EditorGUILayout.TextField(searchFilter);

            EditorGUILayout.LabelField("Category:", GUILayout.Width(60));
            var categories = registry.GetCategories();
            categories.Insert(0, "All");

            int categoryIndex = categories.IndexOf(selectedCategory);
            if (categoryIndex < 0) categoryIndex = 0;

            categoryIndex = EditorGUILayout.Popup(categoryIndex, categories.ToArray());
            selectedCategory = categories[categoryIndex];
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(10);

            // Stats list
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

            var stats = registry.RegisteredStats
                .Where(s => selectedCategory == "All" || s.Category == selectedCategory)
                .Where(s => string.IsNullOrEmpty(searchFilter) ||
                           s.DisplayName.ToLower().Contains(searchFilter.ToLower()) ||
                           s.FieldName.ToLower().Contains(searchFilter.ToLower()))
                .ToList();

            foreach (var stat in stats)
            {
                DrawStatInfo(stat);
            }

            EditorGUILayout.EndScrollView();
        }

        private void DrawStatInfo(StatInfo stat)
        {
            EditorGUILayout.BeginVertical("box");

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(stat.DisplayName, EditorStyles.boldLabel);
            EditorGUILayout.LabelField($"[{stat.Category}]", GUILayout.Width(100));
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.LabelField($"Field: {stat.FieldName}", EditorStyles.miniLabel);
            EditorGUILayout.LabelField($"Type: {stat.DeclaringType}", EditorStyles.miniLabel);

            if (!string.IsNullOrEmpty(stat.Description))
            {
                EditorGUILayout.LabelField(stat.Description, EditorStyles.wordWrappedMiniLabel);
            }

            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(5);
        }
    }
}