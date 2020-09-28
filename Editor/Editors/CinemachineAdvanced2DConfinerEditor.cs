using UnityEditor;
using UnityEngine;

namespace Cinemachine.Editor
{
    [CustomEditor(typeof(CinemachineAdvanced2DConfiner))]
    public class CinemachineAdvanced2DConfinerEditor : BaseEditor<CinemachineAdvanced2DConfiner>
    {
        private static bool m_advancedSettingsExpanded = true;

        private SerializedProperty m_maxOrthoSizeProperty;
        private GUIContent m_maxOrthoSizeGUIContent;
        private SerializedProperty m_shrinkToPointsExperimentalProperty;
        private GUIContent m_shrinkToPointsExperimentalGUIContent;
        
        private SerializedProperty m_autoBakeProperty;
        private GUIContent m_autoBakeGUIContent;
        
        private SerializedProperty m_triggerBakeProperty;
        private SerializedProperty m_triggerClearCacheProperty;
        
        private SerializedProperty m_bakeProgressProperty;
        private string[] m_bakeProgressPropertyEnumNames;
        
        
        void OnEnable()
        {
            m_maxOrthoSizeProperty = FindProperty(x => x.m_MaxOrthoSize);
            m_maxOrthoSizeGUIContent = new GUIContent("Max Camera Window Size", 
                "Defines a maximum camera window size for the precalculation. Use this to optimize memory usage. " +
                "If 0, then this parameter is ignored.  " +
                "Can be also used to allow the camera to look outside the map a bit. For example, if you are using a " +
                "camera with orthographic size of 2, then you could set this value to 1.8. This will allow the camera " +
                "to look outside by some amount depending on your window ratio.");
            m_shrinkToPointsExperimentalProperty = FindProperty(x => x.m_ShrinkToPointsExperimental);
            m_shrinkToPointsExperimentalGUIContent = new GUIContent("Shrink Sub-Polygons To Point Experimental", 
                "By default, the confiner is reduced to its skeleton. If this property is enabled, then the confiner will " +
                "continue reducing the skeletons by reducing bones (line segments) to points.");
            m_autoBakeProperty = FindProperty(x => x.m_AutoBake);
            m_autoBakeGUIContent = new GUIContent("Automatic Baking",
                "Automatically rebakes the confiner, if input parameters (InputCollider, Resolution, " +
                "CameraWindowRatio) change. True is on, False is off.");

            m_triggerBakeProperty = FindProperty(x => x.m_TriggerBake);
            m_triggerClearCacheProperty = FindProperty(x => x.m_TriggerClearCache);
            
            m_bakeProgressProperty = FindProperty(x => x.BakeProgress);
            m_bakeProgressPropertyEnumNames = m_bakeProgressProperty.enumNames;
        }
        
        public override void OnInspectorGUI()
        {
            DrawRemainingPropertiesInInspector();
            m_advancedSettingsExpanded = EditorGUILayout.Foldout(m_advancedSettingsExpanded, "Advanced Settings", true);
            if (m_advancedSettingsExpanded)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(m_maxOrthoSizeProperty, m_maxOrthoSizeGUIContent);
                EditorGUILayout.PropertyField(m_shrinkToPointsExperimentalProperty, m_shrinkToPointsExperimentalGUIContent);
                EditorGUILayout.PropertyField(m_autoBakeProperty, m_autoBakeGUIContent);

                if (!m_autoBakeProperty.boolValue)
                {
                    if (GUILayout.Button("Bake"))
                    {
                        m_triggerBakeProperty.boolValue = true;
                    }

                    if (GUILayout.Button("Clear"))
                    {
                        m_triggerClearCacheProperty.boolValue = true;
                    }
                    
                    float p = m_bakeProgressProperty.enumValueIndex == 0 ? 0 :
                        m_bakeProgressProperty.enumValueIndex == 1 ? 0.5f : 1f;
                    EditorGUI.ProgressBar(EditorGUILayout.GetControlRect(), p, m_bakeProgressPropertyEnumNames[m_bakeProgressProperty.enumValueIndex]);
                }
            }
            serializedObject.ApplyModifiedProperties();
        }
    }
}