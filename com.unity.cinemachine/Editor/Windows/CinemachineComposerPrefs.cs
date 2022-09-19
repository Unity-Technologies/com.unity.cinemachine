using UnityEngine;
using UnityEditor;

namespace Cinemachine.Editor
{
    [InitializeOnLoad]
    static class CinemachineComposerPrefs
    {
        static CinemachineSettings.BoolItem SettingsFoldedOut = new("CNMCN_Composer_Folded", false);

        public static CinemachineSettings.FloatItem OverlayOpacity = new("CNMCN_Overlay_Opacity", 0.15f);
        public static CinemachineSettings.ColorItem HardBoundsOverlayColour = new ("CNMCN_Composer_HardBounds_Colour", new Color32(255, 0, 72, 255));
        public static CinemachineSettings.ColorItem SoftBoundsOverlayColour = new("CNMCN_Composer_SoftBounds_Colour", new Color32(0, 194, 255, 255));
        public static CinemachineSettings.ColorItem TargetColour = new("CNMCN_Composer_Target_Colour", new Color32(255, 254, 25, 255));
        public static CinemachineSettings.FloatItem TargetSize = new("CNMCN_Composer_Target_Size", 5f);

        static readonly GUIContent sComposerOverlayOpacity = new GUIContent(
            "Overlay Opacity", "The alpha of the composer's overlay when a virtual camera is selected with composer module enabled");
        static readonly GUIContent sComposerHardBoundsOverlay = new GUIContent(
            "Hard Bounds Overlay", "The colour of the composer overlay's hard bounds region");
        static readonly GUIContent sComposerSoftBoundsOverlay = new GUIContent(
            "Soft Bounds Overlay", "The colour of the composer overlay's soft bounds region");
        static readonly GUIContent sComposerTargetOverlay = new GUIContent(
            "Composer Target", "The colour of the composer overlay's target");
        static readonly GUIContent sComposerTargetOverlayPixels = new GUIContent(
            "Target Size (px)", "The size of the composer overlay's target box in pixels");

        static CinemachineComposerPrefs() => CinemachineSettings.AdditionalCategories += DrawComposerSettings;

        static void DrawComposerSettings()
        {
            SettingsFoldedOut.Value = EditorGUILayout.Foldout(SettingsFoldedOut.Value, "Composer Settings", true);
            if (SettingsFoldedOut.Value)
            {
                EditorGUI.indentLevel++;

                EditorGUI.BeginChangeCheck();

                EditorGUILayout.BeginHorizontal();
                OverlayOpacity.Value = EditorGUILayout.Slider(sComposerOverlayOpacity, OverlayOpacity.Value, 0f, 1f);
                if (GUILayout.Button("Reset"))
                    OverlayOpacity.Reset();
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.BeginHorizontal();
                HardBoundsOverlayColour.Value = EditorGUILayout.ColorField(sComposerHardBoundsOverlay, HardBoundsOverlayColour.Value);
                if (GUILayout.Button("Reset"))
                    HardBoundsOverlayColour.Reset();
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.BeginHorizontal();
                SoftBoundsOverlayColour.Value = EditorGUILayout.ColorField(sComposerSoftBoundsOverlay, SoftBoundsOverlayColour.Value);
                if (GUILayout.Button("Reset"))
                    SoftBoundsOverlayColour.Reset();
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.BeginHorizontal();
                TargetColour.Value = EditorGUILayout.ColorField(sComposerTargetOverlay, TargetColour.Value);
                if (GUILayout.Button("Reset"))
                    TargetColour.Reset();
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.BeginHorizontal();
                TargetSize.Value = EditorGUILayout.FloatField(sComposerTargetOverlayPixels, TargetSize.Value);
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
