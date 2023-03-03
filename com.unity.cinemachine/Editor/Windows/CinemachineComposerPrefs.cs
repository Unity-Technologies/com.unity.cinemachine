using UnityEngine;
using UnityEditor;

namespace Unity.Cinemachine.Editor
{
    [InitializeOnLoad]
    static class CinemachineComposerPrefs
    {
        static CinemachineSettings.BoolItem s_SettingsFoldedOut = new("CNMCN_Composer_Folded", false);

        public static CinemachineSettings.FloatItem OverlayOpacity = new("CNMCN_Overlay_Opacity", 0.15f);
        public static CinemachineSettings.ColorItem HardBoundsOverlayColour = new ("CNMCN_Composer_HardBounds_Colour", new Color32(255, 0, 72, 255));
        public static CinemachineSettings.ColorItem SoftBoundsOverlayColour = new("CNMCN_Composer_SoftBounds_Colour", new Color32(0, 194, 255, 255));
        public static CinemachineSettings.ColorItem TargetColour = new("CNMCN_Composer_Target_Colour", new Color32(255, 254, 25, 255));
        public static CinemachineSettings.FloatItem TargetSize = new("CNMCN_Composer_Target_Size", 5f);

        static readonly GUIContent k_ComposerOverlayOpacity = new(
            "Overlay Opacity", "The alpha of the composer's overlay when a virtual camera is selected with composer module enabled");
        static readonly GUIContent k_ComposerHardBoundsOverlay = new(
            "Hard Bounds Overlay", "The colour of the composer overlay's hard bounds region");
        static readonly GUIContent k_ComposerSoftBoundsOverlay = new(
            "Soft Bounds Overlay", "The colour of the composer overlay's soft bounds region");
        static readonly GUIContent k_ComposerTargetOverlay = new(
            "Composer Target", "The colour of the composer overlay's target");
        static readonly GUIContent k_ComposerTargetOverlayPixels = new(
            "Target Size (px)", "The size of the composer overlay's target box in pixels");

        static CinemachineComposerPrefs() => CinemachineSettings.AdditionalCategories += DrawComposerSettings;

        static void DrawComposerSettings()
        {
            s_SettingsFoldedOut.Value = EditorGUILayout.Foldout(s_SettingsFoldedOut.Value, "Composer Settings", true);
            if (s_SettingsFoldedOut.Value)
            {
                EditorGUI.indentLevel++;

                EditorGUI.BeginChangeCheck();

                EditorGUILayout.BeginHorizontal();
                OverlayOpacity.Value = EditorGUILayout.Slider(k_ComposerOverlayOpacity, OverlayOpacity.Value, 0f, 1f);
                if (GUILayout.Button("Reset"))
                    OverlayOpacity.Reset();
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.BeginHorizontal();
                HardBoundsOverlayColour.Value = EditorGUILayout.ColorField(k_ComposerHardBoundsOverlay, HardBoundsOverlayColour.Value);
                if (GUILayout.Button("Reset"))
                    HardBoundsOverlayColour.Reset();
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.BeginHorizontal();
                SoftBoundsOverlayColour.Value = EditorGUILayout.ColorField(k_ComposerSoftBoundsOverlay, SoftBoundsOverlayColour.Value);
                if (GUILayout.Button("Reset"))
                    SoftBoundsOverlayColour.Reset();
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.BeginHorizontal();
                TargetColour.Value = EditorGUILayout.ColorField(k_ComposerTargetOverlay, TargetColour.Value);
                if (GUILayout.Button("Reset"))
                    TargetColour.Reset();
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.BeginHorizontal();
                TargetSize.Value = EditorGUILayout.FloatField(k_ComposerTargetOverlayPixels, TargetSize.Value);
                if (GUILayout.Button("Reset"))
                    TargetSize.Reset();
                EditorGUILayout.EndHorizontal();

                if (EditorGUI.EndChangeCheck())
                    UnityEditorInternal.InternalEditorUtility.RepaintAllViews();

                EditorGUI.indentLevel--;
            }




        }
    }
}
