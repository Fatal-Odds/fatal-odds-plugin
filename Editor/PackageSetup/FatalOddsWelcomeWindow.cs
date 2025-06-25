using UnityEngine;
using UnityEditor;

namespace FatalOdds.Editor
{
    // Welcome window shown on first installation
    public class FatalOddsWelcomeWindow : EditorWindow
    {
        private Vector2 scrollPosition;

        public static void ShowWindow()
        {
            var window = GetWindow<FatalOddsWelcomeWindow>("Welcome to Fatal Odds");
            window.minSize = new Vector2(500, 600);
            window.maxSize = new Vector2(500, 600);
            window.ShowModal();
        }

        private void OnGUI()
        {
            // Header
            EditorGUILayout.Space(20);

            var titleStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 20,
                alignment = TextAnchor.MiddleCenter
            };

            GUILayout.Label("⏰ Fatal Odds", titleStyle);
            GUILayout.Label("Modifier System for Unity", new GUIStyle(EditorStyles.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontStyle = FontStyle.Italic
            });

            EditorGUILayout.Space(20);

            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

            // Welcome message
            EditorGUILayout.BeginVertical("box");
            GUILayout.Label("🎉 Welcome!", EditorStyles.boldLabel);
            GUILayout.Label(
                "Thank you for using Fatal Odds! This plugin helps you create stacking modifier items " +
                "and abilities perfect for action roguelike games.",
                EditorStyles.wordWrappedLabel
            );
            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(10);

            // Quick start steps
            EditorGUILayout.BeginVertical("box");
            GUILayout.Label("🚀 Quick Start Guide", EditorStyles.boldLabel);

            var steps = new[]
            {
                "1. Add [StatTag] attributes to your numeric fields",
                "2. Open the Item & Ability Creator from the Window menu",
                "3. Scan for stats to discover your tagged fields",
                "4. Create your first modifier item or ability",
                "5. Generate the asset and test in your game!"
            };

            foreach (string step in steps)
            {
                GUILayout.Label(step, EditorStyles.wordWrappedLabel);
                GUILayout.Space(3);
            }
            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(10);

            // Actions
            EditorGUILayout.BeginVertical("box");
            GUILayout.Label("🎯 What would you like to do?", EditorStyles.boldLabel);

            if (GUILayout.Button("📝 Open Item & Ability Creator", GUILayout.Height(35)))
            {
                FatalOddsEditor.ShowWindow();
                Close();
            }

            if (GUILayout.Button("📊 View Project Overview", GUILayout.Height(25)))
            {
                FatalOddsPluginManager.ShowWindow();
                Close();
            }

            if (GUILayout.Button("📚 Open Help & Documentation", GUILayout.Height(25)))
            {
                FatalOddsHelpWindow.ShowWindow();
                Close();
            }
            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(10);

            // Sample content
            EditorGUILayout.BeginVertical("box");
            GUILayout.Label("📦 Sample Content", EditorStyles.boldLabel);
            GUILayout.Label(
                "Want to see Fatal Odds in action? Import the sample content to get example " +
                "player scripts with StatTag attributes and test components.",
                EditorStyles.wordWrappedLabel
            );

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Import Basic Samples"))
            {
                ImportSampleContent();
            }
            if (GUILayout.Button("Maybe Later"))
            {
                Close();
            }
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();

            EditorGUILayout.EndScrollView();

            EditorGUILayout.Space(10);

            // Footer
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Close"))
            {
                Close();
            }
            GUILayout.FlexibleSpace();
            GUILayout.Label("v0.1.0", EditorStyles.miniLabel);
            EditorGUILayout.EndHorizontal();
        }

        private void ImportSampleContent()
        {
            // This would import sample content if available
            EditorUtility.DisplayDialog(
                "Sample Content",
                "Sample content import is not yet implemented in this version. " +
                "Check the documentation for manual setup examples!",
                "OK"
            );
            Close();
        }
    }
}
