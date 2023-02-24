using UnityEngine;
using UnityEditor;

namespace Cinemachine.Editor
{
    [CustomEditor(typeof(CinemachineBasicMultiChannelPerlin))]
    [CanEditMultipleObjects]
    class CinemachineBasicMultiChannelPerlinEditor : UnityEditor.Editor
    {
        CinemachineBasicMultiChannelPerlin Target => target as CinemachineBasicMultiChannelPerlin;

        void OnEnable()
        {
            NoiseSettingsPropertyDrawer.InvalidateProfileList();
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            this.IMGUI_DrawMissingCmCameraHelpBox();
            bool needWarning = false;
            for (int i = 0; !needWarning && i < targets.Length; ++i)
                needWarning = (targets[i] as CinemachineBasicMultiChannelPerlin).NoiseProfile == null;
            if (needWarning)
                EditorGUILayout.HelpBox(
                    "A Noise Profile is required.  You may choose from among the NoiseSettings assets defined in the project.",
                    MessageType.Warning);

            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(serializedObject.FindProperty(() => Target.NoiseProfile));
            EditorGUILayout.PropertyField(serializedObject.FindProperty(() => Target.PivotOffset));
            EditorGUILayout.PropertyField(serializedObject.FindProperty(() => Target.AmplitudeGain));
            EditorGUILayout.PropertyField(serializedObject.FindProperty(() => Target.FrequencyGain));
            if (EditorGUI.EndChangeCheck())
                serializedObject.ApplyModifiedProperties();

            var rect = EditorGUILayout.GetControlRect(true);
            rect.width -= EditorGUIUtility.labelWidth; rect.x += EditorGUIUtility.labelWidth;
            if (GUI.Button(rect, "New random seed"))
            {
                for (int i = 0; i < targets.Length; ++i)
                    (targets[i] as CinemachineBasicMultiChannelPerlin).ReSeed();
            }
        }
    }
}
