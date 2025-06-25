#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using FatalOdds.Runtime;

namespace FatalOdds.Editor
{
    public class StatSamplingDebugWindow : EditorWindow
    {
        private StatRegistry registry;
        private Vector2 scrollPos;

        public static void ShowStatSamplingDebug()
        {
            var window = GetWindow<StatSamplingDebugWindow>("Stat Sampling");
            window.Show();
        }

        private void OnGUI()
        {
            if (registry == null)
            {
                registry = Resources.Load<StatRegistry>("StatRegistry");
            }

            if (registry == null)
            {
                EditorGUILayout.HelpBox("StatRegistry not found!", MessageType.Error);
                return;
            }

            EditorGUILayout.LabelField("Stat Base Value Sampling", EditorStyles.boldLabel);

            if (GUILayout.Button("Clear Cache & Resample"))
            {
                StatValueReader.ClearCache();
                Repaint();
            }

            scrollPos = EditorGUILayout.BeginScrollView(scrollPos);

            foreach (var stat in registry.RegisteredStats)
            {
                EditorGUILayout.BeginVertical("box");

                EditorGUILayout.LabelField($"{stat.DisplayName} ({stat.Category})", EditorStyles.boldLabel);

                float baseValue = StatValueReader.GetSmartBaseValue(stat);
                EditorGUILayout.LabelField($"Computed Base Value: {baseValue:F2}");

                string samplingInfo = StatValueReader.GetSamplingInfo(stat.GUID);
                EditorGUILayout.LabelField("Sampling Data:", EditorStyles.miniLabel);
                EditorGUILayout.TextArea(samplingInfo, GUILayout.Height(60));

                EditorGUILayout.EndVertical();
                EditorGUILayout.Space(5);
            }

            EditorGUILayout.EndScrollView();
        }
    }
}
#endif