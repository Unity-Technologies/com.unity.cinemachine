using UnityEngine;
using UnityEditor;

namespace Cinemachine.Editor
{
    [InitializeOnLoad]
    static class CinemachineColliderPrefs
    {
        static CinemachineSettings.BoolItem s_SettingsFoldedOut = new("CNMCN_Collider_Foldout", false);
        public static CinemachineSettings.ColorItem CameraSphereColor = new("CNMCN_Collider_Camera_Path_Colour", Color.grey);
        public static CinemachineSettings.ColorItem CameraPathColor = new("CNMCN_Collider_Camera_Sphere_Colour", Color.yellow);

        static CinemachineColliderPrefs() => CinemachineSettings.AdditionalCategories += DrawColliderSettings;

        static void DrawColliderSettings()
        {
            s_SettingsFoldedOut.Value = EditorGUILayout.Foldout(s_SettingsFoldedOut.Value, "Collider Settings", true);
            if (s_SettingsFoldedOut.Value)
            {
                EditorGUI.indentLevel++;
                EditorGUI.BeginChangeCheck();

                EditorGUILayout.BeginHorizontal();
                CameraSphereColor.Value = EditorGUILayout.ColorField("Camera Sphere Color", CameraSphereColor.Value);
                if (GUILayout.Button("Reset"))
                    CameraSphereColor.Reset();
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.BeginHorizontal();
                CameraPathColor.Value = EditorGUILayout.ColorField("Camera Path Color", CameraPathColor.Value);
                if (GUILayout.Button("Reset"))
                    CameraPathColor.Reset();
                EditorGUILayout.EndHorizontal();

                if (EditorGUI.EndChangeCheck())
                    UnityEditorInternal.InternalEditorUtility.RepaintAllViews();
                EditorGUI.indentLevel--;
            }
        }
    }
}
