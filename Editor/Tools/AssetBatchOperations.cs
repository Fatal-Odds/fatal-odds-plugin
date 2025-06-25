using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
using FatalOdds.Runtime;

namespace FatalOdds.Editor
{
    public static class AssetBatchOperations
    {
        public static List<ItemDefinition> FindAllGeneratedItems()
        {
            var guids = AssetDatabase.FindAssets("t:ItemDefinition", new[] { "Assets/FatalOdds/Generated/Items" });
            var items = new List<ItemDefinition>();

            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var item = AssetDatabase.LoadAssetAtPath<ItemDefinition>(path);
                if (item != null)
                {
                    items.Add(item);
                }
            }

            return items;
        }
        public static List<AbilityDefinition> FindAllGeneratedAbilities()
        {
            var guids = AssetDatabase.FindAssets("t:AbilityDefinition", new[] { "Assets/FatalOdds/Generated/Abilities" });
            var abilities = new List<AbilityDefinition>();

            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var ability = AssetDatabase.LoadAssetAtPath<AbilityDefinition>(path);
                if (ability != null)
                {
                    abilities.Add(ability);
                }
            }

            return abilities;
        }
        public static void CleanupGeneratedAssets()
        {
            string generatedPath = "Assets/FatalOdds/Generated";

            if (Directory.Exists(generatedPath))
            {
                bool confirm = EditorUtility.DisplayDialog(
                    "Cleanup Generated Assets",
                    "This will delete all generated modifier items and abilities. Are you sure?",
                    "Yes, Delete All",
                    "Cancel"
                );

                if (confirm)
                {
                    AssetDatabase.DeleteAsset(generatedPath);
                    AssetDatabase.Refresh();
                    Debug.Log("[Fatal Odds] Cleaned up all generated assets");
                }
            }
        }
        public static void ExportGeneratedAssets()
        {
            var items = FindAllGeneratedItems();
            var abilities = FindAllGeneratedAbilities();

            if (items.Count == 0 && abilities.Count == 0)
            {
                EditorUtility.DisplayDialog("Export Assets", "No generated assets found to export.", "OK");
                return;
            }

            string exportPath = EditorUtility.SaveFilePanel(
                "Export Generated Assets",
                "",
                "FatalOddsModifierAssets.unitypackage",
                "unitypackage"
            );

            if (!string.IsNullOrEmpty(exportPath))
            {
                var assetPaths = new List<string>();

                foreach (var item in items)
                {
                    assetPaths.Add(AssetDatabase.GetAssetPath(item));
                }

                foreach (var ability in abilities)
                {
                    assetPaths.Add(AssetDatabase.GetAssetPath(ability));
                }

                AssetDatabase.ExportPackage(assetPaths.ToArray(), exportPath, ExportPackageOptions.Default);
                Debug.Log($"[Fatal Odds] Exported {assetPaths.Count} modifier assets to {exportPath}");
                EditorUtility.DisplayDialog("Export Complete", $"Exported {assetPaths.Count} assets successfully!", "OK");
            }
        }
        public static string GetAssetStatistics()
        {
            var items = FindAllGeneratedItems();
            var abilities = FindAllGeneratedAbilities();

            var stats = $"Modifier Item System Statistics:\n";
            stats += $"Modifier Items: {items.Count}\n";
            stats += $"Active Abilities: {abilities.Count}\n";
            stats += $"Total: {items.Count + abilities.Count}\n\n";

            if (items.Count > 0)
            {
                var rarityGroups = items.GroupBy(i => i.rarity);
                stats += "Items by Rarity:\n";
                foreach (var group in rarityGroups)
                {
                    stats += $"  {group.Key}: {group.Count()}\n";
                }

                var stackingGroups = items.GroupBy(i => i.stackingType);
                stats += "\nItems by Stacking Type:\n";
                foreach (var group in stackingGroups)
                {
                    stats += $"  {group.Key}: {group.Count()}\n";
                }
                stats += "\n";
            }

            if (abilities.Count > 0)
            {
                var targetGroups = abilities.GroupBy(a => a.targetType);
                stats += "Abilities by Target Type:\n";
                foreach (var group in targetGroups)
                {
                    stats += $"  {group.Key}: {group.Count()}\n";
                }
            }

            return stats;
        }
    }
}