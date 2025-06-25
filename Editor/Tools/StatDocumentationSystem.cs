using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.Compilation;
using UnityEngine;
using FatalOdds.Runtime;
using System.Data;

namespace FatalOdds.Editor
{
    public static class StatDocumentationSystem
    {
        private const string DOCS_FOLDER = "Assets/FatalOdds/Documentation/StatDocumentation";
        private const string MARKDOWN_FILE = "Assets/FatalOdds/Documentation/StatDocumentation/StatRegistry_Documentation.md";
        private const string JSON_FILE = "Assets/FatalOdds/Documentation/StatDocumentation/StatRegistry_Data.json";
        private const string EXCEL_FILE = "Assets/FatalOdds/Documentation/StatDocumentation/StatRegistry_Data.xlsx";
        private const string CSV_FILE = "Assets/FatalOdds/Documentation/StatDocumentation/StatRegistry_Data.csv";

        // Auto-update on compilation
        [InitializeOnLoadMethod]
        public static void InitializeAutoDocumentation()
        {
            CompilationPipeline.compilationFinished += OnCompilationFinished;
        }

        private static void OnCompilationFinished(object obj)
        {
            EditorApplication.delayCall += () =>
            {
                GenerateDocumentation();
            };
        }

        [MenuItem("Window/Fatal Odds/📋 Generate Stat Documentation", priority = 25)]
        public static void GenerateDocumentationManual()
        {
            GenerateDocumentation();

            // Open the generated file
            var markdownAsset = AssetDatabase.LoadAssetAtPath<TextAsset>(MARKDOWN_FILE);
            if (markdownAsset != null)
            {
                AssetDatabase.OpenAsset(markdownAsset);
            }

            EditorUtility.DisplayDialog("Documentation Generated!",
                $"Stat documentation has been generated!\n\n" +
                $"📄 Markdown: StatRegistry_Documentation.md\n" +
                $"📊 JSON Data: StatRegistry_Data.json\n" +
                $"📋 Excel: StatRegistry_Data.xlsx\n" +
                $"📈 CSV: StatRegistry_Data.csv\n\n" +
                $"Files are in: {DOCS_FOLDER}",
                "Open Folder");

            // Open the documentation folder
            EditorUtility.RevealInFinder(DOCS_FOLDER);
        }

        public static void GenerateDocumentation()
        {
            try
            {
                var statRegistry = FatalOddsMenus.EnsureStatRegistryExists();
                if (statRegistry == null || statRegistry.StatCount == 0)
                {
                    Debug.LogWarning("[Fatal Odds] No stats found for documentation. Run a stat scan first.");
                    return;
                }

                // Ensure documentation folder exists
                EnsureDocumentationFolderExists();

                // Gather all data
                var documentationData = GatherStatDocumentationData(statRegistry);

                // Generate markdown documentation
                GenerateMarkdownDocumentation(documentationData);

                // Generate JSON data file
                GenerateJSONDataFile(documentationData);

                // Generate Excel workbook
                GenerateExcelWorkbook(documentationData);

                // Generate CSV files
                GenerateCSVFiles(documentationData);

                AssetDatabase.Refresh();

                Debug.Log($"[Fatal Odds] Documentation generated with {documentationData.AllStats.Count} stats");
            }
            catch (Exception e)
            {
                Debug.LogError($"[Fatal Odds] Failed to generate documentation: {e.Message}");
            }
        }

        private static StatDocumentationData GatherStatDocumentationData(StatRegistry statRegistry)
        {
            var data = new StatDocumentationData
            {
                GeneratedAt = DateTime.Now,
                AllStats = new List<StatDocEntry>(),
                StatsByOwner = new Dictionary<string, List<StatDocEntry>>(),
                StatsByCategory = new Dictionary<string, List<StatDocEntry>>(),
                ItemsAffectingStats = new Dictionary<string, List<string>>(),
                AbilitiesAffectingStats = new Dictionary<string, List<string>>()
            };

            // Cache items / abilities once
            var allItems = AssetBatchOperations.FindAllGeneratedItems();
            var allAbilities = AssetBatchOperations.FindAllGeneratedAbilities();

            foreach (var stat in statRegistry.RegisteredStats)
            {
                var entry = new StatDocEntry
                {
                    GUID = stat.GUID,
                    DisplayName = stat.DisplayName,
                    FieldName = stat.FieldName,
                    Category = stat.Category,
                    Description = stat.Description,
                    DeclaringType = stat.DeclaringType,
                    Owner = DetermineStatOwner(stat),
                    BaseValue = GetStatBaseValue(stat),
                    AffectedByItems = new List<string>(),
                    AffectedByAbilities = new List<string>()
                };

                // Which items touch this stat?
                foreach (var item in allItems)
                {
                    if (item.modifiers.Any(m => m.statGuid == stat.GUID))
                    {
                        entry.AffectedByItems.Add(item.itemName);
                        data.ItemsAffectingStats.TryAdd(stat.GUID, new List<string>());
                        data.ItemsAffectingStats[stat.GUID].Add(item.itemName);
                    }
                }

                // Which abilities touch this stat?
                foreach (var ability in allAbilities)
                {
                    if (ability.modifiers.Any(m => m.statGuid == stat.GUID))
                    {
                        entry.AffectedByAbilities.Add(ability.abilityName);
                        data.AbilitiesAffectingStats.TryAdd(stat.GUID, new List<string>());
                        data.AbilitiesAffectingStats[stat.GUID].Add(ability.abilityName);
                    }
                }

                data.AllStats.Add(entry);

                // Group by owner & category
                data.StatsByOwner.TryAdd(entry.Owner, new List<StatDocEntry>());
                data.StatsByOwner[entry.Owner].Add(entry);

                data.StatsByCategory.TryAdd(entry.Category, new List<StatDocEntry>());
                data.StatsByCategory[entry.Category].Add(entry);
            }

            // ── Final ordering ──
            data.AllStats = data.AllStats
                .OrderBy(s => GetOwnerPriority(s.Owner))
                .ThenBy(s => s.Owner)
                .ThenBy(s => s.Category)
                .ThenBy(s => s.DisplayName)
                .ToList();

            foreach (var owner in data.StatsByOwner.Keys.ToList())
            {
                data.StatsByOwner[owner] = data.StatsByOwner[owner]
                    .OrderBy(s => s.Category)
                    .ThenBy(s => s.DisplayName)
                    .ToList();
            }

            foreach (var cat in data.StatsByCategory.Keys.ToList())
            {
                data.StatsByCategory[cat] = data.StatsByCategory[cat]
                    .OrderBy(s => GetOwnerPriority(s.Owner))
                    .ThenBy(s => s.Owner)
                    .ThenBy(s => s.DisplayName)
                    .ToList();
            }

            return data;
        }

        // Writes out a fully-formatted Markdown report
        private static void GenerateMarkdownDocumentation(StatDocumentationData data)
        {
            var md = new StringBuilder();

            // ───── Header ─────
            md.AppendLine("# Fatal Odds – Stat Registry Documentation");
            md.AppendLine();
            md.AppendLine($"**Generated:** {data.GeneratedAt:yyyy-MM-dd HH:mm:ss}");
            md.AppendLine($"**Total Stats:** {data.AllStats.Count}");
            md.AppendLine($"**Owners:** {data.StatsByOwner.Count}");
            md.AppendLine($"**Categories:** {data.StatsByCategory.Count}");
            md.AppendLine();

            //  Table of Contents ─────────
            md.AppendLine("## 📋 Table of Contents");
            md.AppendLine();
            md.AppendLine("1. [📊 Summary Statistics](#-summary-statistics)");
            md.AppendLine("2. [🎮 Stats by Owner](#-stats-by-owner)");
            md.AppendLine("3. [📁 Stats by Category](#-stats-by-category)");
            md.AppendLine("4. [🎒 Item Impact Analysis](#-item-impact-analysis)");
            md.AppendLine("5. [⚔️ Ability Impact Analysis](#️-ability-impact-analysis)");
            md.AppendLine("6. [📈 Complete Stat Reference](#-complete-stat-reference)");
            md.AppendLine();

            // ─ Summary Block ──
            md.AppendLine("## 📊 Summary Statistics");
            md.AppendLine();
            md.AppendLine("| Metric | Count |");
            md.AppendLine("|--------|-------|");
            md.AppendLine($"| Total Stats | {data.AllStats.Count} |");
            md.AppendLine($"| Unique Owners | {data.StatsByOwner.Count} |");
            md.AppendLine($"| Categories | {data.StatsByCategory.Count} |");
            md.AppendLine($"| Stats with Items | {data.AllStats.Count(s => s.AffectedByItems.Count > 0)} |");
            md.AppendLine($"| Stats with Abilities | {data.AllStats.Count(s => s.AffectedByAbilities.Count > 0)} |");
            md.AppendLine($"| Unmodified Stats | {data.AllStats.Count(s => s.AffectedByItems.Count == 0 && s.AffectedByAbilities.Count == 0)} |");
            md.AppendLine();

            // ───────── Stats by Owner ───
            md.AppendLine("## 🎮 Stats by Owner");
            md.AppendLine();

            foreach (var ownerGroup in data.StatsByOwner
                        .OrderBy(kvp => GetOwnerPriority(kvp.Key))
                        .ThenBy(kvp => kvp.Key))
            {
                string ownerIcon = GetOwnerIcon(ownerGroup.Key);
                md.AppendLine($"### {ownerIcon} {ownerGroup.Key} ({ownerGroup.Value.Count} stats)");
                md.AppendLine();

                foreach (var categoryGroup in ownerGroup.Value.GroupBy(s => s.Category)
                            .OrderBy(g => g.Key))
                {
                    md.AppendLine($"#### 📁 {categoryGroup.Key}");
                    md.AppendLine();
                    md.AppendLine("| Stat | Field | Base Value | Items | Abilities |");
                    md.AppendLine("|------|-------|------------|-------|-----------|");

                    foreach (var stat in categoryGroup.OrderBy(s => s.DisplayName))
                    {
                        string itemsList = stat.AffectedByItems.Any() ? string.Join(", ", stat.AffectedByItems) : "*None*";
                        string abilitiesList = stat.AffectedByAbilities.Any() ? string.Join(", ", stat.AffectedByAbilities) : "*None*";

                        md.AppendLine($"| **{stat.DisplayName}** | `{stat.FieldName}` | {stat.BaseValue:F2} | {itemsList} | {abilitiesList} |");
                    }
                    md.AppendLine();
                }
            }

            // ───────── Stats by Category 
            md.AppendLine("## 📁 Stats by Category");
            md.AppendLine();

            foreach (var categoryGroup in data.StatsByCategory.OrderBy(kvp => kvp.Key))
            {
                string categoryIcon = GetCategoryIcon(categoryGroup.Key);
                md.AppendLine($"### {categoryIcon} {categoryGroup.Key} ({categoryGroup.Value.Count} stats)");
                md.AppendLine();
                md.AppendLine("| Owner | Stat | Field | Base Value | Modifications |");
                md.AppendLine("|-------|------|-------|------------|---------------|");

                foreach (var stat in categoryGroup.Value
                            .OrderBy(s => GetOwnerPriority(s.Owner))
                            .ThenBy(s => s.Owner)
                            .ThenBy(s => s.DisplayName))
                {
                    int totalMods = stat.AffectedByItems.Count + stat.AffectedByAbilities.Count;
                    string modSummary = totalMods > 0
                                      ? $"{stat.AffectedByItems.Count} items, {stat.AffectedByAbilities.Count} abilities"
                                      : "*Unmodified*";

                    md.AppendLine($"| {stat.Owner} | **{stat.DisplayName}** | `{stat.FieldName}` | {stat.BaseValue:F2} | {modSummary} |");
                }
                md.AppendLine();
            }

            // ──────── Item Impact Analysis ────────
            md.AppendLine("## 🎒 Item Impact Analysis");
            md.AppendLine();
            var allItems = AssetBatchOperations.FindAllGeneratedItems();

            if (allItems.Any())
            {
                md.AppendLine("### Items and Their Stat Modifications");
                md.AppendLine();

                foreach (var item in allItems.OrderBy(i => i.rarity).ThenBy(i => i.itemName))
                {
                    string rarityIcon = GetRarityIcon(item.rarity);
                    md.AppendLine($"#### {rarityIcon} {item.itemName} ({item.rarity})");
                    md.AppendLine();
                    md.AppendLine($"*{item.description}*");
                    md.AppendLine();

                    if (item.modifiers.Any())
                    {
                        md.AppendLine("**Modifications:**");
                        md.AppendLine();

                        foreach (var modifier in item.modifiers)
                        {
                            var stat = data.AllStats.FirstOrDefault(s => s.GUID == modifier.statGuid);
                            string statName = stat?.DisplayName ?? "Unknown Stat";
                            md.AppendLine($"- **{statName}**: {modifier.GetDisplayText()}");
                        }
                    }
                    else
                    {
                        md.AppendLine("*No stat modifications*");
                    }
                    md.AppendLine();
                }
            }
            else
            {
                md.AppendLine("*No items created yet.*");
                md.AppendLine();
            }

            // ─────── Ability Impact Analysis ──────
            md.AppendLine("## ⚔️ Ability Impact Analysis");
            md.AppendLine();
            var allAbilities = AssetBatchOperations.FindAllGeneratedAbilities();

            if (allAbilities.Any())
            {
                md.AppendLine("### Abilities and Their Stat Modifications");
                md.AppendLine();

                foreach (var ability in allAbilities.OrderBy(a => a.targetType).ThenBy(a => a.abilityName))
                {
                    md.AppendLine($"#### ⚔️ {ability.abilityName} ({ability.targetType})");
                    md.AppendLine();
                    md.AppendLine($"*{ability.description}*");
                    md.AppendLine();
                    md.AppendLine($"**Cooldown:** {ability.cooldown}s | **Energy Cost:** {ability.energyCost}");
                    md.AppendLine();

                    if (ability.modifiers.Any())
                    {
                        md.AppendLine("**Temporary Effects:**");
                        md.AppendLine();

                        foreach (var modifier in ability.modifiers)
                        {
                            var stat = data.AllStats.FirstOrDefault(s => s.GUID == modifier.statGuid);
                            string statName = stat?.DisplayName ?? "Unknown Stat";
                            md.AppendLine($"- **{statName}**: {modifier.GetDisplayText()}");
                        }
                    }
                    else
                    {
                        md.AppendLine("*No stat modifications*");
                    }
                    md.AppendLine();
                }
            }
            else
            {
                md.AppendLine("*No abilities created yet.*");
                md.AppendLine();
            }

            // ───── Complete Stat Reference ────────
            md.AppendLine("## 📈 Complete Stat Reference");
            md.AppendLine();
            md.AppendLine("| Owner | Category | Stat | Field | Type | Base Value | Description |");
            md.AppendLine("|-------|----------|------|-------|------|------------|-------------|");

            foreach (var stat in data.AllStats)
            {
                string description = !string.IsNullOrEmpty(stat.Description) ? stat.Description : "*No description*";
                string typeName = GetSimpleTypeName(stat.DeclaringType);

                md.AppendLine($"| {stat.Owner} | {stat.Category} | **{stat.DisplayName}** | `{stat.FieldName}` | {typeName} | {stat.BaseValue:F2} | {description} |");
            }

            // ───── Footer ─────
            md.AppendLine();
            md.AppendLine("---");
            md.AppendLine();
            md.AppendLine($"*Documentation auto-generated by Fatal Odds v0.1.0 on {data.GeneratedAt:yyyy-MM-dd HH:mm:ss}*");

            File.WriteAllText(MARKDOWN_FILE, md.ToString());
        }

        private static void GenerateJSONDataFile(StatDocumentationData data)
        {
            var jsonData = new
            {
                generatedAt = data.GeneratedAt.ToString("yyyy-MM-dd HH:mm:ss"),
                summary = new
                {
                    totalStats = data.AllStats.Count,
                    uniqueOwners = data.StatsByOwner.Count,
                    categories = data.StatsByCategory.Count,
                    statsWithItems = data.AllStats.Count(s => s.AffectedByItems.Count > 0),
                    statsWithAbilities = data.AllStats.Count(s => s.AffectedByAbilities.Count > 0)
                },
                statsByOwner = data.StatsByOwner.ToDictionary(
                    kvp => kvp.Key,
                    kvp => kvp.Value.Select(s => new
                    {
                        guid = s.GUID,
                        displayName = s.DisplayName,
                        fieldName = s.FieldName,
                        category = s.Category,
                        baseValue = s.BaseValue,
                        affectedByItems = s.AffectedByItems,
                        affectedByAbilities = s.AffectedByAbilities
                    })
                ),
                statsByCategory = data.StatsByCategory.ToDictionary(
                    kvp => kvp.Key,
                    kvp => kvp.Value.Select(s => new
                    {
                        guid = s.GUID,
                        displayName = s.DisplayName,
                        owner = s.Owner,
                        fieldName = s.FieldName,
                        baseValue = s.BaseValue
                    })
                ),
                allStats = data.AllStats.Select(s => new
                {
                    guid = s.GUID,
                    displayName = s.DisplayName,
                    fieldName = s.FieldName,
                    category = s.Category,
                    owner = s.Owner,
                    declaringType = s.DeclaringType,
                    baseValue = s.BaseValue,
                    description = s.Description,
                    affectedByItems = s.AffectedByItems,
                    affectedByAbilities = s.AffectedByAbilities
                })
            };

            string jsonString = JsonUtility.ToJson(jsonData, true);
            File.WriteAllText(JSON_FILE, jsonString);
        }

        private static void GenerateExcelWorkbook(StatDocumentationData data)
        {
            try
            {
                // Create a simple Excel-compatible format using CSV with tabs
                // This creates multiple "sheets" as separate CSV files that can be imported to Excel

                var excelData = new StringBuilder();

                // Summary Sheet
                excelData.AppendLine("FATAL ODDS - STAT REGISTRY REPORT");
                excelData.AppendLine($"Generated: {data.GeneratedAt:yyyy-MM-dd HH:mm:ss}");
                excelData.AppendLine();
                excelData.AppendLine("SUMMARY STATISTICS");
                excelData.AppendLine($"Total Stats,{data.AllStats.Count}");
                excelData.AppendLine($"Unique Owners,{data.StatsByOwner.Count}");
                excelData.AppendLine($"Categories,{data.StatsByCategory.Count}");
                excelData.AppendLine($"Stats with Items,{data.AllStats.Count(s => s.AffectedByItems.Count > 0)}");
                excelData.AppendLine($"Stats with Abilities,{data.AllStats.Count(s => s.AffectedByAbilities.Count > 0)}");
                excelData.AppendLine($"Unmodified Stats,{data.AllStats.Count(s => s.AffectedByItems.Count == 0 && s.AffectedByAbilities.Count == 0)}");
                excelData.AppendLine();

                // All Stats Sheet
                excelData.AppendLine("ALL STATS");
                excelData.AppendLine("Owner,Category,Stat Name,Field Name,Type,Base Value,Description,Affected By Items,Affected By Abilities,Total Modifications");

                foreach (var stat in data.AllStats)
                {
                    string description = !string.IsNullOrEmpty(stat.Description) ? stat.Description.Replace(",", ";") : "No description";
                    string typeName = GetSimpleTypeName(stat.DeclaringType);
                    string itemsList = stat.AffectedByItems.Count > 0 ? string.Join("; ", stat.AffectedByItems) : "None";
                    string abilitiesList = stat.AffectedByAbilities.Count > 0 ? string.Join("; ", stat.AffectedByAbilities) : "None";
                    int totalMods = stat.AffectedByItems.Count + stat.AffectedByAbilities.Count;

                    excelData.AppendLine($"{stat.Owner},{stat.Category},{stat.DisplayName},{stat.FieldName},{typeName},{stat.BaseValue:F2},{description},{itemsList},{abilitiesList},{totalMods}");
                }
                excelData.AppendLine();

                // Stats by Owner
                excelData.AppendLine("STATS BY OWNER");
                foreach (var ownerGroup in data.StatsByOwner.OrderBy(kvp => kvp.Key))
                {
                    excelData.AppendLine($"{ownerGroup.Key.ToUpper()} ({ownerGroup.Value.Count} stats)");
                    excelData.AppendLine("Category,Stat Name,Field Name,Base Value,Items Count,Abilities Count");

                    foreach (var stat in ownerGroup.Value)
                    {
                        excelData.AppendLine($"{stat.Category},{stat.DisplayName},{stat.FieldName},{stat.BaseValue:F2},{stat.AffectedByItems.Count},{stat.AffectedByAbilities.Count}");
                    }
                    excelData.AppendLine();
                }

                // Write the Excel-compatible format
                File.WriteAllText(EXCEL_FILE.Replace(".xlsx", "_Report.csv"), excelData.ToString());

                // Create an XML format that Excel can read as a proper workbook
                GenerateExcelXMLWorkbook(data);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[Fatal Odds] Could not generate Excel file: {e.Message}. CSV version created instead.");
            }
        }

        // Produces an Excel-compatible XML workbook (Excel 2003 XML format)
        private static void GenerateExcelXMLWorkbook(StatDocumentationData data)
        {
            var xml = new StringBuilder();

            // ─── Excel XML Header & Styles ───
            xml.AppendLine("<?xml version=\"1.0\"?>");
            xml.AppendLine("<?mso-application progid=\"Excel.Sheet\"?>");
            xml.AppendLine("<Workbook xmlns=\"urn:schemas-microsoft-com:office:spreadsheet\"");
            xml.AppendLine(" xmlns:o=\"urn:schemas-microsoft-com:office:office\"");
            xml.AppendLine(" xmlns:x=\"urn:schemas-microsoft-com:office:excel\"");
            xml.AppendLine(" xmlns:ss=\"urn:schemas-microsoft-com:office:spreadsheet\"");
            xml.AppendLine(" xmlns:html=\"http://www.w3.org/TR/REC-html40\">");

            xml.AppendLine("<Styles>");
            xml.AppendLine("<Style ss:ID=\"Header\">");
            xml.AppendLine("<Font ss:Bold=\"1\" ss:Size=\"12\"/>");
            xml.AppendLine("<Interior ss:Color=\"#4F81BD\" ss:Pattern=\"Solid\"/>");
            xml.AppendLine("<Font ss:Color=\"#FFFFFF\"/>");
            xml.AppendLine("</Style>");
            xml.AppendLine("<Style ss:ID=\"Summary\">");
            xml.AppendLine("<Font ss:Bold=\"1\" ss:Size=\"14\"/>");
            xml.AppendLine("<Interior ss:Color=\"#D9E1F2\" ss:Pattern=\"Solid\"/>");
            xml.AppendLine("</Style>");
            xml.AppendLine("</Styles>");

            // ─ Summary Sheet 
            xml.AppendLine("<Worksheet ss:Name=\"Summary\">");
            xml.AppendLine("<Table>");
            xml.AppendLine("<Row><Cell ss:StyleID=\"Summary\"><Data ss:Type=\"String\">Fatal Odds – Stat Registry Summary</Data></Cell></Row>");
            xml.AppendLine($"<Row><Cell><Data ss:Type=\"String\">Generated: {data.GeneratedAt:yyyy-MM-dd HH:mm:ss}</Data></Cell></Row>");
            xml.AppendLine("<Row/>"); // spacer
            xml.AppendLine("<Row><Cell ss:StyleID=\"Header\"><Data ss:Type=\"String\">Metric</Data></Cell><Cell ss:StyleID=\"Header\"><Data ss:Type=\"String\">Count</Data></Cell></Row>");
            xml.AppendLine($"<Row><Cell><Data ss:Type=\"String\">Total Stats</Data></Cell><Cell><Data ss:Type=\"Number\">{data.AllStats.Count}</Data></Cell></Row>");
            xml.AppendLine($"<Row><Cell><Data ss:Type=\"String\">Unique Owners</Data></Cell><Cell><Data ss:Type=\"Number\">{data.StatsByOwner.Count}</Data></Cell></Row>");
            xml.AppendLine($"<Row><Cell><Data ss:Type=\"String\">Categories</Data></Cell><Cell><Data ss:Type=\"Number\">{data.StatsByCategory.Count}</Data></Cell></Row>");
            xml.AppendLine($"<Row><Cell><Data ss:Type=\"String\">Stats with Items</Data></Cell><Cell><Data ss:Type=\"Number\">{data.AllStats.Count(s => s.AffectedByItems.Any())}</Data></Cell></Row>");
            xml.AppendLine($"<Row><Cell><Data ss:Type=\"String\">Stats with Abilities</Data></Cell><Cell><Data ss:Type=\"Number\">{data.AllStats.Count(s => s.AffectedByAbilities.Any())}</Data></Cell></Row>");
            xml.AppendLine($"<Row><Cell><Data ss:Type=\"String\">Unmodified Stats</Data></Cell><Cell><Data ss:Type=\"Number\">{data.AllStats.Count(s => !s.AffectedByItems.Any() && !s.AffectedByAbilities.Any())}</Data></Cell></Row>");
            xml.AppendLine("</Table>");
            xml.AppendLine("</Worksheet>");

            // ─ All Stats Sheet ─────────
            xml.AppendLine("<Worksheet ss:Name=\"All Stats\">");
            xml.AppendLine("<Table>");
            xml.AppendLine("<Row>");
            xml.AppendLine("<Cell ss:StyleID=\"Header\"><Data ss:Type=\"String\">Owner</Data></Cell>");
            xml.AppendLine("<Cell ss:StyleID=\"Header\"><Data ss:Type=\"String\">Category</Data></Cell>");
            xml.AppendLine("<Cell ss:StyleID=\"Header\"><Data ss:Type=\"String\">Stat Name</Data></Cell>");
            xml.AppendLine("<Cell ss:StyleID=\"Header\"><Data ss:Type=\"String\">Field Name</Data></Cell>");
            xml.AppendLine("<Cell ss:StyleID=\"Header\"><Data ss:Type=\"String\">Type</Data></Cell>");
            xml.AppendLine("<Cell ss:StyleID=\"Header\"><Data ss:Type=\"String\">Base Value</Data></Cell>");
            xml.AppendLine("<Cell ss:StyleID=\"Header\"><Data ss:Type=\"String\">Description</Data></Cell>");
            xml.AppendLine("<Cell ss:StyleID=\"Header\"><Data ss:Type=\"String\">Items Count</Data></Cell>");
            xml.AppendLine("<Cell ss:StyleID=\"Header\"><Data ss:Type=\"String\">Abilities Count</Data></Cell>");
            xml.AppendLine("<Cell ss:StyleID=\"Header\"><Data ss:Type=\"String\">Total Mods</Data></Cell>");
            xml.AppendLine("</Row>");

            foreach (var stat in data.AllStats)
            {
                string description = EscapeXml(string.IsNullOrEmpty(stat.Description) ? "No description" : stat.Description);
                string typeName = EscapeXml(GetSimpleTypeName(stat.DeclaringType));
                int totalMods = stat.AffectedByItems.Count + stat.AffectedByAbilities.Count;

                xml.AppendLine("<Row>");
                xml.AppendLine($"<Cell><Data ss:Type=\"String\">{EscapeXml(stat.Owner)}</Data></Cell>");
                xml.AppendLine($"<Cell><Data ss:Type=\"String\">{EscapeXml(stat.Category)}</Data></Cell>");
                xml.AppendLine($"<Cell><Data ss:Type=\"String\">{EscapeXml(stat.DisplayName)}</Data></Cell>");
                xml.AppendLine($"<Cell><Data ss:Type=\"String\">{EscapeXml(stat.FieldName)}</Data></Cell>");
                xml.AppendLine($"<Cell><Data ss:Type=\"String\">{typeName}</Data></Cell>");
                xml.AppendLine($"<Cell><Data ss:Type=\"Number\">{stat.BaseValue:F2}</Data></Cell>");
                xml.AppendLine($"<Cell><Data ss:Type=\"String\">{description}</Data></Cell>");
                xml.AppendLine($"<Cell><Data ss:Type=\"Number\">{stat.AffectedByItems.Count}</Data></Cell>");
                xml.AppendLine($"<Cell><Data ss:Type=\"Number\">{stat.AffectedByAbilities.Count}</Data></Cell>");
                xml.AppendLine($"<Cell><Data ss:Type=\"Number\">{totalMods}</Data></Cell>");
                xml.AppendLine("</Row>");
            }
            xml.AppendLine("</Table>");
            xml.AppendLine("</Worksheet>");

            // ───── Owner-specific Sheets ──────
            foreach (var ownerGroup in data.StatsByOwner
                        .OrderBy(kvp => GetOwnerPriority(kvp.Key))
                        .ThenBy(kvp => kvp.Key))
            {
                string sheetName = EscapeXml(ownerGroup.Key);
                xml.AppendLine($"<Worksheet ss:Name=\"{sheetName}\">");
                xml.AppendLine("<Table>");
                xml.AppendLine("<Row>");
                xml.AppendLine("<Cell ss:StyleID=\"Header\"><Data ss:Type=\"String\">Category</Data></Cell>");
                xml.AppendLine("<Cell ss:StyleID=\"Header\"><Data ss:Type=\"String\">Stat Name</Data></Cell>");
                xml.AppendLine("<Cell ss:StyleID=\"Header\"><Data ss:Type=\"String\">Field Name</Data></Cell>");
                xml.AppendLine("<Cell ss:StyleID=\"Header\"><Data ss:Type=\"String\">Base Value</Data></Cell>");
                xml.AppendLine("<Cell ss:StyleID=\"Header\"><Data ss:Type=\"String\">Items</Data></Cell>");
                xml.AppendLine("<Cell ss:StyleID=\"Header\"><Data ss:Type=\"String\">Abilities</Data></Cell>");
                xml.AppendLine("</Row>");

                foreach (var stat in ownerGroup.Value
                            .OrderBy(s => s.Category)
                            .ThenBy(s => s.DisplayName))
                {
                    string itemsList = stat.AffectedByItems.Any() ? EscapeXml(string.Join(", ", stat.AffectedByItems)) : "None";
                    string abilitiesList = stat.AffectedByAbilities.Any() ? EscapeXml(string.Join(", ", stat.AffectedByAbilities)) : "None";

                    xml.AppendLine("<Row>");
                    xml.AppendLine($"<Cell><Data ss:Type=\"String\">{EscapeXml(stat.Category)}</Data></Cell>");
                    xml.AppendLine($"<Cell><Data ss:Type=\"String\">{EscapeXml(stat.DisplayName)}</Data></Cell>");
                    xml.AppendLine($"<Cell><Data ss:Type=\"String\">{EscapeXml(stat.FieldName)}</Data></Cell>");
                    xml.AppendLine($"<Cell><Data ss:Type=\"Number\">{stat.BaseValue:F2}</Data></Cell>");
                    xml.AppendLine($"<Cell><Data ss:Type=\"String\">{itemsList}</Data></Cell>");
                    xml.AppendLine($"<Cell><Data ss:Type=\"String\">{abilitiesList}</Data></Cell>");
                    xml.AppendLine("</Row>");
                }
                xml.AppendLine("</Table>");
                xml.AppendLine("</Worksheet>");
            }

            xml.AppendLine("</Workbook>");

            File.WriteAllText(EXCEL_FILE, xml.ToString());
        }

        private static void GenerateCSVFiles(StatDocumentationData data)
        {
            // Main CSV file with all stats
            var csv = new StringBuilder();
            csv.AppendLine("Owner,Category,Stat Name,Field Name,Type,Base Value,Description,Items Count,Abilities Count,Items List,Abilities List");

            foreach (var stat in data.AllStats)
            {
                string description = !string.IsNullOrEmpty(stat.Description) ? stat.Description.Replace("\"", "\"\"").Replace(",", ";") : "No description";
                string typeName = GetSimpleTypeName(stat.DeclaringType);
                string itemsList = stat.AffectedByItems.Count > 0 ? "\"" + string.Join("; ", stat.AffectedByItems) + "\"" : "None";
                string abilitiesList = stat.AffectedByAbilities.Count > 0 ? "\"" + string.Join("; ", stat.AffectedByAbilities) + "\"" : "None";

                csv.AppendLine($"\"{stat.Owner}\",\"{stat.Category}\",\"{stat.DisplayName}\",\"{stat.FieldName}\",\"{typeName}\",{stat.BaseValue:F2},\"{description}\",{stat.AffectedByItems.Count},{stat.AffectedByAbilities.Count},{itemsList},{abilitiesList}");
            }

            File.WriteAllText(CSV_FILE, csv.ToString());
        }

        private static string EscapeXml(string text)
        {
            if (string.IsNullOrEmpty(text)) return "";

            return text.Replace("&", "&amp;")
                      .Replace("<", "&lt;")
                      .Replace(">", "&gt;")
                      .Replace("\"", "&quot;")
                      .Replace("'", "&apos;");
        }

        // Gives us a stable priority so Player is always first, Enemy always second.
        private static int GetOwnerPriority(string owner)
        {
            owner = owner?.ToLower() ?? "";
            if (owner == "player") return 0;
            if (owner == "enemy") return 1;
            return 2;           // anything else comes after
        }

        // Determines which top-level owner group a stat belongs to.
        // Priority: specific matches first, generic matches last.
        private static string DetermineStatOwner(StatInfo stat)
        {
            string typeName = stat.DeclaringType?.ToLower() ?? "";
            string displayName = stat.DisplayName?.ToLower() ?? "";
            string fieldName = stat.FieldName?.ToLower() ?? "";
            string categoryName = stat.Category?.ToLower() ?? "";
            string description = stat.Description?.ToLower() ?? "";

            // ───── 1) Hard exclusions ─────
            // Any "Director"-style class/category (AI director, SpawnDirector, etc.)
            if (typeName.Contains("director") || categoryName.Contains("director"))
                return "System";

            // ───── 2) Player checks ──────
            bool explicitPlayer =
                typeName.Contains("player") ||
                displayName.Contains("player") ||
                fieldName.Contains("player") ||
                categoryName.Contains("player");

            bool temporalPlayer =
                (typeName.Contains("temporal") || categoryName.Contains("temporal") ||
                 displayName.Contains("temporal") || fieldName.Contains("temporal")) &&
                // Only treat as player if the stat is part of something the player *owns*
                (typeName.Contains("ability") || typeName.Contains("weapon") ||
                 typeName.Contains("controller") || typeName.Contains("movement") ||
                 typeName.Contains("player"));

            if (explicitPlayer || temporalPlayer)
                return "Player";

            // ───── 3) Enemy / AI checks ───
            if (typeName.Contains("enemy") || typeName.Contains("ai") ||
                displayName.Contains("enemy") || categoryName.Contains("enemy"))
                return "Enemy";

            // ───── 4) Everything else (unchanged) ───
            if (typeName.Contains("weapon")) return "Weapon";
            if (typeName.Contains("projectile")) return "Projectile";
            if (typeName.Contains("environment") ||
                typeName.Contains("world")) return "Environment";
            if (typeName.Contains("ui") ||
                typeName.Contains("interface")) return "UI";
            if (typeName.Contains("manager") ||
                typeName.Contains("system")) return "System";

            return "Other";
        }

        private static float GetStatBaseValue(StatInfo stat)
        {
            try
            {
                return StatValueReader.GetSmartBaseValue(stat);
            }
            catch
            {
                return 0f;
            }
        }

        private static string GetOwnerIcon(string owner)
        {
            return owner.ToLower() switch
            {
                "player" => "🎮",
                "enemy" => "👹",
                "weapon" => "⚔️",
                "projectile" => "🎯",
                "environment" => "🌍",
                "ui" => "🖥️",
                "system" => "⚙️",
                _ => "📦"
            };
        }

        private static string GetCategoryIcon(string category)
        {
            string lower = category.ToLower();
            if (lower.Contains("combat")) return "⚔️";
            if (lower.Contains("movement")) return "🏃";
            if (lower.Contains("health")) return "❤️";
            if (lower.Contains("time")) return "⏰";
            if (lower.Contains("weapon")) return "🔫";
            if (lower.Contains("ui")) return "🖥️";
            return "📁";
        }

        private static string GetRarityIcon(ItemRarity rarity)
        {
            return rarity switch
            {
                ItemRarity.Common => "⚪",
                ItemRarity.Uncommon => "🟢",
                ItemRarity.Rare => "🔵",
                ItemRarity.Epic => "🟣",
                ItemRarity.Legendary => "🟡",
                ItemRarity.Artifact => "🔴",
                _ => "⚫"
            };
        }

        private static string GetSimpleTypeName(string fullTypeName)
        {
            if (string.IsNullOrEmpty(fullTypeName))
                return "Unknown";

            // Get just the class name without namespace
            var parts = fullTypeName.Split('.');
            return parts[parts.Length - 1];
        }

        private static void EnsureDocumentationFolderExists()
        {
            if (!AssetDatabase.IsValidFolder(DOCS_FOLDER))
            {
                string parentFolder = Path.GetDirectoryName(DOCS_FOLDER);
                string folderName = Path.GetFileName(DOCS_FOLDER);

                if (!string.IsNullOrEmpty(parentFolder) && !AssetDatabase.IsValidFolder(parentFolder))
                {
                    FatalOddsMenus.EnsureFolderExists(parentFolder);
                }

                AssetDatabase.CreateFolder(parentFolder, folderName);
                AssetDatabase.Refresh();
            }
        }

        // Data classes
        [Serializable]
        public class StatDocumentationData
        {
            public DateTime GeneratedAt;
            public List<StatDocEntry> AllStats;
            public Dictionary<string, List<StatDocEntry>> StatsByOwner;
            public Dictionary<string, List<StatDocEntry>> StatsByCategory;
            public Dictionary<string, List<string>> ItemsAffectingStats;
            public Dictionary<string, List<string>> AbilitiesAffectingStats;
        }

        [Serializable]
        public class StatDocEntry
        {
            public string GUID;
            public string DisplayName;
            public string FieldName;
            public string Category;
            public string Description;
            public string DeclaringType;
            public string Owner;
            public float BaseValue;
            public List<string> AffectedByItems;
            public List<string> AffectedByAbilities;
        }
    }
}