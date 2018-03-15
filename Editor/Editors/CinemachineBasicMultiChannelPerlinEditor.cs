using UnityEngine;
using UnityEditor;

namespace Cinemachine.Editor
{
    [CustomEditor(typeof(CinemachineBasicMultiChannelPerlin))]
    internal sealed class CinemachineBasicMultiChannelPerlinEditor 
        : BaseEditor<CinemachineBasicMultiChannelPerlin>
    {
        private void OnEnable()
        {
            NoiseSettingsPropertyDrawer.InvalidateProfileList();
        }

        public override void OnInspectorGUI()
        {
            BeginInspector();
            if (FindProperty(x => x.m_NoiseProfile).objectReferenceValue == null)
                EditorGUILayout.HelpBox(
                    "A Noise Profile is required.  You may choose from among the NoiseSettings assets defined in the project.",
                    MessageType.Warning);
            DrawRemainingPropertiesInInspector();

            Rect rect = EditorGUILayout.GetControlRect(true);
            rect.width -= EditorGUIUtility.labelWidth; rect.x += EditorGUIUtility.labelWidth;
            if (GUI.Button(rect, "New random seed"))
                Target.ReSeed();
        }
    }
}
