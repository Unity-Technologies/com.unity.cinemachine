#if CINEMACHINE_TIMELINE

using UnityEngine;
using UnityEditor;

namespace Unity.Cinemachine.Editor
{
    static class CinemachineTimelinePrefs
    {
        static CinemachineSettings.BoolItem s_SettingsFoldedOut = new("CNMCN_Timeline_Folded", true);

        public static CinemachineSettings.BoolItem AutoCreateShotFromSceneView = new ("CM_Timeline_AutoCreateShotFromSceneView", false);

        public class ScrubbingCacheItem : CinemachineSettings.BoolItem
        {
            public ScrubbingCacheItem(string key, bool defaultValue) : base(key, defaultValue) {}
            protected override void WritePrefs(bool value)
            {
                base.WritePrefs(value);
                TargetPositionCache.UseCache = value;
            }
        };
        public static ScrubbingCacheItem UseScrubbingCache = new ("CNMCN_Timeline_CachedScrubbing", false);

        public static readonly GUIContent s_AutoCreateLabel = new GUIContent(
            "Auto-create new shots",  "When enabled, new clips will be "
                + "automatically populated to match the scene view camera.  "
                + "This is a global setting");
        public static readonly GUIContent s_ScrubbingCacheLabel = new GUIContent(
            "Cached Scrubbing",
            "For preview scrubbing, caches target positions and pre-simulates each frame to "
                + "approximate damping and noise playback.  Target position cache is built when timeline is "
                + "played forward, and used when timeline is scrubbed within the indicated zone. "
                + "This is a global setting.");

        static CinemachineTimelinePrefs() => CinemachineSettings.AdditionalCategories += DrawTimelineSettings;

        static void DrawTimelineSettings()
        {
            s_SettingsFoldedOut.Value = EditorGUILayout.Foldout(s_SettingsFoldedOut.Value, "Timeline Settings", true);
            if (s_SettingsFoldedOut.Value)
            {
                EditorGUI.indentLevel++;
                EditorGUI.BeginChangeCheck();

                AutoCreateShotFromSceneView.Value = EditorGUILayout.Toggle(s_AutoCreateLabel, AutoCreateShotFromSceneView.Value);
                UseScrubbingCache.Value = EditorGUILayout.Toggle(s_ScrubbingCacheLabel, UseScrubbingCache.Value);

                if (EditorGUI.EndChangeCheck())
                    UnityEditorInternal.InternalEditorUtility.RepaintAllViews();
                EditorGUI.indentLevel--;
            }
        }
    }
}
#endif
