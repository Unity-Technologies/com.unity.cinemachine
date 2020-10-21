using UnityEditor;
using UnityEngine;

namespace Cinemachine.Editor
{
    [CustomEditor(typeof(CinemachineConfiner2D))]
    public class CinemachineConfiner2DEditor : BaseEditor<CinemachineConfiner2D>
    {
        private static bool m_advancedSettingsExpanded = true;

        private SerializedProperty m_maxOrthoSizeProperty;
        private GUIContent m_maxOrthoSizeGUIContent;
        
        protected virtual void OnEnable()
        {
            m_maxOrthoSizeProperty = FindProperty(x => x.m_MaxOrthoSize);
            m_maxOrthoSizeGUIContent = new GUIContent("Max Camera Window Size", 
                "The confiner will correctly confine up to this maximum orthographic size. " +
                "If set to 0, then this parameter is ignored and all camera sizes are supported. " +
                "Use it to optimize computation and memory costs.");
        }
        
        public override void OnInspectorGUI()
        {
            BeginInspector();
            DrawRemainingPropertiesInInspector();
            m_advancedSettingsExpanded = EditorGUILayout.Foldout(m_advancedSettingsExpanded, "Advanced Settings", true);
            if (m_advancedSettingsExpanded)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(m_maxOrthoSizeProperty, m_maxOrthoSizeGUIContent);
            }
            serializedObject.ApplyModifiedProperties();
        }
    }
}