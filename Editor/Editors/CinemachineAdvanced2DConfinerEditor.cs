using UnityEditor;
using UnityEngine;

namespace Cinemachine.Editor
{
    [CustomEditor(typeof(CinemachineAdvanced2DConfiner))]
    public class CinemachineAdvanced2DConfinerEditor : BaseEditor<CinemachineAdvanced2DConfiner>
    {
        protected static bool AdvancedSettingsExpanded = true;

        private SerializedProperty maxOrthoSizeProperty;
        private GUIContent maxOrthoSizeGUIContent;
        private SerializedProperty shrinkToPointsExperimentalProperty;
        private GUIContent shrinkToPointsExperimentalGUIContent;
        
        private SerializedProperty autoBakeProperty;
        private GUIContent autoBakeGUIContent;
        
        private SerializedProperty triggerBakeProperty;
        private SerializedProperty triggerClearCacheProperty;
        
        private SerializedProperty bakeProgressProperty;
        private string[] bakeProgressPropertyEnumNames;
        
        
        void OnEnable()
        {
            maxOrthoSizeProperty = FindProperty(x => x.m_MaxOrthoSize);
            maxOrthoSizeGUIContent = new GUIContent("Max Camera Window Size", "Defines a maximum camera window size for the precalculation. Use this to optimize " +
                                  "memory usage. If 0, then this parameter is ignored.");
            shrinkToPointsExperimentalProperty = FindProperty(x => x.m_ShrinkToPointsExperimental);
            shrinkToPointsExperimentalGUIContent = new GUIContent("Shrink To Point Experimental", 
                "By default, the confiner is reduced until it has no area (e.g. lines, " +
                "or points). If this property is enabled, then the confiner will " +
                "continue reducing itself by reducing lines to points.");
            autoBakeProperty = FindProperty(x => x.m_AutoBake);
            autoBakeGUIContent = new GUIContent("Automatic Baking",
                "Automatically rebakes the confiner, if input parameters (InputCollider, Resolution, " +
                "CameraWindowRatio) change. True is on, False is off.");

            triggerBakeProperty = FindProperty(x => x.m_TriggerBake);
            triggerClearCacheProperty = FindProperty(x => x.m_TriggerClearCache);
            
            bakeProgressProperty = FindProperty(x => x.BakeProgress);
            bakeProgressPropertyEnumNames = bakeProgressProperty.enumNames;
        }
        
        public override void OnInspectorGUI()
        {
            DrawRemainingPropertiesInInspector();
            AdvancedSettingsExpanded = EditorGUILayout.Foldout(AdvancedSettingsExpanded, "Advanced Settings", true);
            if (AdvancedSettingsExpanded)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(maxOrthoSizeProperty, maxOrthoSizeGUIContent);
                EditorGUILayout.PropertyField(shrinkToPointsExperimentalProperty, shrinkToPointsExperimentalGUIContent);
                EditorGUILayout.PropertyField(autoBakeProperty, autoBakeGUIContent);

                if (!autoBakeProperty.boolValue)
                {
                    if (GUILayout.Button("Bake"))
                    {
                        triggerBakeProperty.boolValue = true;
                    }

                    if (GUILayout.Button("Clear"))
                    {
                        triggerClearCacheProperty.boolValue = true;
                    }
                    
                    float p = bakeProgressProperty.enumValueIndex == 0 ? 0 :
                        bakeProgressProperty.enumValueIndex == 1 ? 0.5f : 1f;
                    EditorGUI.ProgressBar(EditorGUILayout.GetControlRect(), p, bakeProgressPropertyEnumNames[bakeProgressProperty.enumValueIndex]);
                }
            }
            serializedObject.ApplyModifiedProperties();
        }
    }
}