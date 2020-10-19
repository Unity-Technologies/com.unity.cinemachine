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
        private SerializedProperty m_shrinkToPointsExperimentalProperty;
        private GUIContent m_shrinkToPointsExperimentalGUIContent;

        private SerializedProperty m_bakeProgressProperty;
        private string[] m_bakeProgressPropertyEnumNames;
        
        
        void OnEnable()
        {
            m_maxOrthoSizeProperty = FindProperty(x => x.m_MaxOrthoSize);
            m_maxOrthoSizeGUIContent = new GUIContent("Max Camera Window Size", 
                "Defines a maximum camera window size for baking. The 2DConfiner will clamp values bigger than this. " +
                "Use this to optimize memory usage.  If set to 0, then this parameter is ignored.  " +
                "Can be also used to allow the camera to look outside the map a bit. For example, if you are using a " +
                "camera with orthographic size of 2, then you could set this value to 1.8. This will allow the camera " +
                "to look outside by some amount depending on your window ratio.");
            
            m_shrinkToPointsExperimentalProperty = FindProperty(x => x.m_ShrinkToPointsExperimental);
            m_shrinkToPointsExperimentalGUIContent = new GUIContent("Shrink Sub-Polygons To Point Experimental", 
                "By default, the confiner is reduced to its skeleton. If this property is enabled, then the confiner " +
                "will continue reducing the skeletons by reducing bones (line segments) to points.");

            m_bakeProgressProperty = FindProperty(x => x.BakeProgress);
            m_bakeProgressPropertyEnumNames = m_bakeProgressProperty.enumNames;
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
                EditorGUILayout.PropertyField(m_shrinkToPointsExperimentalProperty, 
                    m_shrinkToPointsExperimentalGUIContent);

                float p = m_bakeProgressProperty.enumValueIndex == 0 ? 0 :
                    m_bakeProgressProperty.enumValueIndex == 1 ? 0.5f : 1f;
                EditorGUI.ProgressBar(EditorGUILayout.GetControlRect(), p, 
                    m_bakeProgressPropertyEnumNames[m_bakeProgressProperty.enumValueIndex]);
                
            }
            serializedObject.ApplyModifiedProperties();
        }
    }
}