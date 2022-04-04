#if CINEMACHINE_UNITY_SPLINES
using UnityEngine;
using UnityEditor;
using Cinemachine.Editor;

namespace Cinemachine
{
    [InitializeOnLoad]
    static class CinemachineSplineDollyPrefs
    {
        static bool SettingsFoldedOut
        {
            get => EditorPrefs.GetBool(k_SplineSettingsFoldoutKey, false);
            set
            {
                if (value != SettingsFoldedOut)
                {
                    EditorPrefs.SetBool(k_SplineSettingsFoldoutKey, value);
                }
            }
        }

        public static Color SplineRollColor
        {
            get => CinemachineSettings.UnpackColour(
                EditorPrefs.GetString(k_SplineRollColorKey, CinemachineSettings.PackColor(Color.green)));

            set
            {
                if (value != SplineRollColor)
                {
                    EditorPrefs.SetString(k_SplineRollColorKey, CinemachineSettings.PackColor(value));
                }
            }
        }

        public static float SplineWidth = 0.5f;
        public static int SplineResolution = 10;

        const string k_SplineSettingsFoldoutKey = "CNMCN_Spline_Foldout";
        const string k_SplineRollColorKey = "CNMCN_Spline_Roll_Colour";

        static GUIContent s_SplineRollColorGUIContent, s_SplineWidthGUIContent, s_SplineResolutionGUIContent;

        static CinemachineSplineDollyPrefs()
        {
            CinemachineSettings.AdditionalCategories += DrawSplineSettings;
            
            s_SplineRollColorGUIContent = new GUIContent
            {
                text = "Spline Track",
                tooltip = "The color with which the spline is drawn, when a " + nameof(CinemachineSplineRoll) + " is attached."
            };
            s_SplineWidthGUIContent = new GUIContent
            {
                text = "Width",
                tooltip = "The width of the spline"
            };
            s_SplineResolutionGUIContent = new GUIContent
            {
                text = "Resolution",
                tooltip = "The resolution with which the spline is drawn. Change this, if performance in the editor is a concern."
            };
        }

        static void DrawSplineSettings()
        {
            SettingsFoldedOut = EditorGUILayout.Foldout(SettingsFoldedOut, "Spline Settings", true);
            if (SettingsFoldedOut)
            {
                EditorGUI.indentLevel++;
                EditorGUI.BeginChangeCheck();

                SplineRollColor = EditorGUILayout.ColorField(s_SplineRollColorGUIContent, SplineRollColor);
                SplineWidth = Mathf.Max(0.01f, EditorGUILayout.FloatField(s_SplineWidthGUIContent, SplineWidth));
                SplineResolution = Mathf.Clamp(EditorGUILayout.IntField(s_SplineResolutionGUIContent, SplineResolution), 3, 100);

                if (EditorGUI.EndChangeCheck())
                {
                    UnityEditorInternal.InternalEditorUtility.RepaintAllViews();
                }

                EditorGUI.indentLevel--;
            }
        }
    }
}
#endif