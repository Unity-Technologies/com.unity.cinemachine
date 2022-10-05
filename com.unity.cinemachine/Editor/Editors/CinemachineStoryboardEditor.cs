#if CINEMACHINE_UGUI
using UnityEngine;
using UnityEditor;

namespace Cinemachine.Editor
{
    [InitializeOnLoad]
    static class CinemachineStoryboardMute
    {
        static CinemachineStoryboardMute()
        {
            CinemachineStoryboard.s_StoryboardGlobalMute = CinemachineCorePrefs.StoryboardGlobalMute.Value;
        }
    }

    [CustomEditor(typeof(CinemachineStoryboard))]
    [CanEditMultipleObjects]
    class CinemachineStoryboardEditor : BaseEditor<CinemachineStoryboard>
    {
        const float k_FastWaveformUpdateInterval = 0.1f;
        float m_LastSplitScreenEventTime = 0;
        static bool s_AdvancedFoldout;
        
        public void OnDisable()
        {
            WaveformWindow.SetDefaultUpdateInterval();
        }

        public override void OnInspectorGUI()
        {
            float now = Time.realtimeSinceStartup;
            if (now - m_LastSplitScreenEventTime > k_FastWaveformUpdateInterval * 5)
                WaveformWindow.SetDefaultUpdateInterval();

            BeginInspector();
            CmPipelineComponentInspectorUtility.IMGUI_DrawMissingCmCameraHelpBox(this);

            CinemachineCorePrefs.StoryboardGlobalMute.Value = EditorGUILayout.Toggle(
                CinemachineCorePrefs.s_StoryboardGlobalMuteLabel, CinemachineCorePrefs.StoryboardGlobalMute.Value);

            Rect rect = EditorGUILayout.GetControlRect(true);
            EditorGUI.BeginChangeCheck();
            {
                float width = rect.width;
                rect.width = EditorGUIUtility.labelWidth + rect.height;
                EditorGUI.PropertyField(rect, FindProperty(x => x.ShowImage));

                rect.x += rect.width; rect.width = width - rect.width;
                EditorGUI.PropertyField(rect, FindProperty(x => x.Image), GUIContent.none);

                EditorGUILayout.PropertyField(FindProperty(x => x.Aspect));
                EditorGUILayout.PropertyField(FindProperty(x => x.Alpha));
                EditorGUILayout.PropertyField(FindProperty(x => x.Center));
                EditorGUILayout.PropertyField(FindProperty(x => x.Rotation));

                rect = EditorGUILayout.GetControlRect(true);
                EditorGUI.LabelField(rect, "Scale");
                rect.x += EditorGUIUtility.labelWidth; rect.width -= EditorGUIUtility.labelWidth;
                rect.width /= 3;
                serializedObject.SetIsDifferentCacheDirty(); // prop.hasMultipleDifferentValues always results in false if the SO isn't refreshed here
                var prop = FindProperty(x => x.SyncScale);
                var syncHasDifferentValues = prop.hasMultipleDifferentValues;
                GUIContent syncLabel = new GUIContent("Sync", prop.tooltip);
                EditorGUI.showMixedValue = syncHasDifferentValues;
                prop.boolValue = EditorGUI.ToggleLeft(rect, syncLabel, prop.boolValue);
                EditorGUI.showMixedValue = false;
                rect.x += rect.width;
                if (prop.boolValue || targets.Length > 1 && syncHasDifferentValues)
                {
                    prop = FindProperty(x => x.Scale);
                    float[] values = new float[1] { prop.vector2Value.x };
                    EditorGUI.showMixedValue = prop.hasMultipleDifferentValues;
                    EditorGUI.MultiFloatField(rect, new GUIContent[1] { new GUIContent("X") }, values);
                    EditorGUI.showMixedValue = false;
                    prop.vector2Value = new Vector2(values[0], values[0]);
                }
                else
                {
                    rect.width *= 2;
                    prop = FindProperty(x => x.Scale);
                    EditorGUI.showMixedValue = prop.hasMultipleDifferentValues;
                    EditorGUI.PropertyField(rect, prop, GUIContent.none);
                    EditorGUI.showMixedValue = false;
                }
                EditorGUILayout.PropertyField(FindProperty(x => x.MuteCamera));
            }
            if (EditorGUI.EndChangeCheck())
                serializedObject.ApplyModifiedProperties();

            EditorGUILayout.Space();
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(FindProperty(x => x.SplitView));
            if (EditorGUI.EndChangeCheck())
            {
                m_LastSplitScreenEventTime = now;
                WaveformWindow.UpdateInterval = k_FastWaveformUpdateInterval;
                serializedObject.ApplyModifiedProperties();
            }
            rect = EditorGUILayout.GetControlRect(true);
            GUI.Label(new Rect(rect.x, rect.y, EditorGUIUtility.labelWidth, rect.height),
                "Waveform Monitor");
            rect.width -= EditorGUIUtility.labelWidth; rect.width /= 2;
            rect.x += EditorGUIUtility.labelWidth;
            if (GUI.Button(rect, "Open"))
                WaveformWindow.OpenWindow();

            EditorGUILayout.Space();
            s_AdvancedFoldout = EditorGUILayout.Foldout(s_AdvancedFoldout, "Advanced");
            if (s_AdvancedFoldout)
            {
                ++EditorGUI.indentLevel;
                
                EditorGUI.BeginChangeCheck();
                var renderModeProperty = FindProperty(x => x.RenderMode);
                EditorGUILayout.PropertyField(renderModeProperty);
                EditorGUILayout.PropertyField(FindProperty(x => x.SortingOrder));
                if (renderModeProperty.enumValueIndex == (int) RenderMode.ScreenSpaceCamera)
                {
                    EditorGUILayout.PropertyField(FindProperty(x => x.PlaneDistance));
                }
                if (EditorGUI.EndChangeCheck())
                {
                    serializedObject.ApplyModifiedProperties();
                }
                
                --EditorGUI.indentLevel;
            }
        }
    }
}
#endif
