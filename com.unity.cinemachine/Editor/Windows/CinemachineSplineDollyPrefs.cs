using UnityEngine;
using UnityEditor;

namespace Unity.Cinemachine.Editor
{
    [InitializeOnLoad]
    static class CinemachineSplineDollyPrefs
    {
        static CinemachineSettings.BoolItem s_SettingsFoldedOut = new("CNMCN_Spline_Foldout", false);
        public static CinemachineSettings.ColorItem SplineRollColor = new ("CNMCN_Spline_Roll_Colour", Color.green);
        public static CinemachineSettings.FloatItem SplineWidth = new("CNMCN_Spline_width", 0.5f);
        public static CinemachineSettings.IntItem SplineResolution = new("CNMCN_Spline_resolution", 10);

        static readonly GUIContent k_SplineRollColorGUIContent = new("Spline Track", 
            "The color with which the spline is drawn, when a " + nameof(CinemachineSplineRoll) + " is attached.");
        static readonly GUIContent k_SplineWidthGUIContent = new("Width", 
            "The width of the spline");
        static readonly GUIContent k_SplineResolutionGUIContent = new("Resolution",
            "The resolution with which the spline is drawn. Change this if performance in the editor is a concern.");

        static CinemachineSplineDollyPrefs() => CinemachineSettings.AdditionalCategories += DrawSplineSettings;

        static void DrawSplineSettings()
        {
            s_SettingsFoldedOut.Value = EditorGUILayout.Foldout(s_SettingsFoldedOut.Value, "Spline Settings", true);
            if (s_SettingsFoldedOut.Value)
            {
                EditorGUI.indentLevel++;
                EditorGUI.BeginChangeCheck();

                EditorGUILayout.BeginHorizontal();
                SplineRollColor.Value = EditorGUILayout.ColorField(k_SplineRollColorGUIContent, SplineRollColor.Value);
                if (GUILayout.Button("Reset"))
                    SplineRollColor.Reset();
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.BeginHorizontal();
                SplineWidth.Value = Mathf.Max(0.01f, EditorGUILayout.FloatField(k_SplineWidthGUIContent, SplineWidth.Value));
                if (GUILayout.Button("Reset"))
                    SplineWidth.Reset();
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.BeginHorizontal();
                SplineResolution.Value = Mathf.Clamp(EditorGUILayout.IntField(k_SplineResolutionGUIContent, SplineResolution.Value), 3, 100);
                if (GUILayout.Button("Reset"))
                    SplineResolution.Reset();
                EditorGUILayout.EndHorizontal();

                if (EditorGUI.EndChangeCheck())
                    UnityEditorInternal.InternalEditorUtility.RepaintAllViews();

                EditorGUI.indentLevel--;
            }
        }
    }
}
