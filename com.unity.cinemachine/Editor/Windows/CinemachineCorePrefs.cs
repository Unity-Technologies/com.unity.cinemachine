using UnityEngine;
using UnityEditor;

namespace Unity.Cinemachine.Editor
{
    static class CinemachineCorePrefs
    {
        // Specialization for game view guides setting: keep a shadow copy of the value in CinemachineDebug
        public class GameViewGuidesItem : CinemachineSettings.BoolItem
        {
            public GameViewGuidesItem(string key, bool defaultValue) : base(key, defaultValue) {}
            protected override bool ReadPrefs()
            {
                var value = base.ReadPrefs();
                CinemachineDebug.GameViewGuidesEnabled = value;
                return value;
            }
            protected override void WritePrefs(bool value)
            {
                base.WritePrefs(value);
                CinemachineDebug.GameViewGuidesEnabled = value;
            }
        }

        static CinemachineSettings.BoolItem s_SettingsFoldedOut = new("CNMCN_Core_Folded", true);

        public static CinemachineSettings.BoolItem StoryboardGlobalMute = new ("CNMCN_StoryboardMute_Enabled", false);
        public static CinemachineSettings.BoolItem DraggableComposerGuides = new ("CNMCN_DraggableComposerGuides_Enabled", true);
        public static CinemachineSettings.BoolItem ShowBrainIconInHierarchy = new ("CNMCN_Core_Brain_icon_in_hierarchy", true);
        public static GameViewGuidesItem ShowInGameGuides = new ("CNMCN_Core_ShowInGameGuides", true);
        public static CinemachineSettings.ColorItem ActiveGizmoColour = new ("CNMCN_Core_Active_Gizmo_Colour", new Color32(255, 0, 0, 100));
        public static CinemachineSettings.ColorItem InactiveGizmoColour = new ("CNMCN_Core_Inactive_Gizmo_Colour", new Color32(9, 54, 87, 100));
        public static CinemachineSettings.ColorItem BoundaryObjectGizmoColour = new ("CNMCN_Core_BoundaryObject_Gizmo_Colour", Color.yellow);

        public static readonly GUIContent s_StoryboardGlobalMuteLabel = new(
            "Storyboard Global Mute",
            "If checked, all storyboards are globally muted.");
        public static readonly GUIContent s_ShowInGameGuidesLabel = new(
            "Show Game View Guides",
            "Enable the display of overlays in the Game View.  You can adjust colours and opacity in Cinemachine Preferences.");
        public static readonly GUIContent s_SaveDuringPlayLabel = new(
            "Save During Play",
            "If checked, Virtual Camera settings changes made during Play Mode "
                + "will be propagated back to the scene when Play Mode is exited.");

        static readonly GUIContent k_CoreActiveGizmosColour = new(
            "Active Virtual Camera", "The colour for the active virtual camera's gizmos");
        static readonly GUIContent k_CoreInactiveGizmosColour = new(
            "Inactive Virtual Camera", "The colour for all inactive virtual camera gizmos");
        static readonly GUIContent k_CoreBoundaryObjectGizmosColour = new(
            "Boundary Object", "The colour to indicate secondary objects like group boundaries and confiner shapes");
        static readonly GUIContent k_DraggableGuidesLabel = new(
            "Draggable Game View Guides", "If checked, game view guides are draggable in play mode. If false, game view guides are only for visualization");
        static readonly GUIContent k_ShowBrainIconsLabel = new(
            "Show Hierarchy Icon", "If checked, an icon will be added next to the CM Brain in the scene hierarchy");

        // Core settings is special - it is displayed first therefore this method is exposed
        public static void DrawCoreSettings()
        {
            // set label width, so text is not cut for Toggles
            float originalLabelWidth = EditorGUIUtility.labelWidth;
            EditorGUIUtility.labelWidth = GUI.skin.label.CalcSize(k_DraggableGuidesLabel).x;
            {
                EditorGUI.BeginChangeCheck();

                ShowInGameGuides.Value = EditorGUILayout.Toggle(s_ShowInGameGuidesLabel, ShowInGameGuides.Value);
                DraggableComposerGuides.Value = EditorGUILayout.Toggle(k_DraggableGuidesLabel, DraggableComposerGuides.Value);
                ShowBrainIconInHierarchy.Value = EditorGUILayout.Toggle(k_ShowBrainIconsLabel, ShowBrainIconInHierarchy.Value);
                SaveDuringPlay.Enabled = EditorGUILayout.Toggle(s_SaveDuringPlayLabel, SaveDuringPlay.Enabled);
#if CINEMACHINE_UGUI
                StoryboardGlobalMute.Value = EditorGUILayout.Toggle(s_StoryboardGlobalMuteLabel, StoryboardGlobalMute.Value);
#endif
                if (EditorGUI.EndChangeCheck())
                    UnityEditorInternal.InternalEditorUtility.RepaintAllViews();
            }
            EditorGUIUtility.labelWidth = originalLabelWidth;

            s_SettingsFoldedOut.Value = EditorGUILayout.Foldout(s_SettingsFoldedOut.Value, "Core Settings", true);
            if (s_SettingsFoldedOut.Value)
            {
                EditorGUI.indentLevel++;
                EditorGUI.BeginChangeCheck();

                EditorGUILayout.BeginHorizontal();
                ActiveGizmoColour.Value = EditorGUILayout.ColorField(k_CoreActiveGizmosColour, ActiveGizmoColour.Value);
                if (GUILayout.Button("Reset"))
                    ActiveGizmoColour.Reset();
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.BeginHorizontal();
                InactiveGizmoColour.Value = EditorGUILayout.ColorField(k_CoreInactiveGizmosColour, InactiveGizmoColour.Value);
                if (GUILayout.Button("Reset"))
                    InactiveGizmoColour.Reset();
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.BeginHorizontal();
                EditorGUI.BeginChangeCheck();
                BoundaryObjectGizmoColour.Value = EditorGUILayout.ColorField(k_CoreBoundaryObjectGizmosColour, BoundaryObjectGizmoColour.Value);
                if (GUILayout.Button("Reset"))
                    BoundaryObjectGizmoColour.Reset();
                EditorGUILayout.EndHorizontal();

                if (EditorGUI.EndChangeCheck())
                    UnityEditorInternal.InternalEditorUtility.RepaintAllViews();
                EditorGUI.indentLevel--;
            }
        }
    }
}