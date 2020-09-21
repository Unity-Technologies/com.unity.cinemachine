using UnityEditor;
using UnityEngine;

namespace Cinemachine.Editor
{
    [CustomEditor(typeof(CinemachineAdvanced2DConfiner))]
    public class CinemachineAdvanced2DConfinerEditor : BaseEditor<CinemachineAdvanced2DConfiner>
    {
        protected static bool AdvancedSettingsExpanded = true;

        private SerializedProperty autoBakeProperty;
        private GUIContent autoBakeTooltip;
        
        private SerializedProperty triggerBakeProperty;
        private SerializedProperty triggerClearCache;
        private SerializedProperty bakeProgressProperty;
        private string[] bakeProgressPropertyEnumNames;
        
        
        void OnEnable()
        {
            autoBakeProperty = FindProperty(x => x.m_AutoBake);
            autoBakeTooltip = new GUIContent("Automatically rebakes the confiner, if input parameters " +
                                                  "(CameraWindowRatio, InputCollider, Resolution) - that affect " +
                                                  "the outcome - change. True is on, False is off.");

            triggerBakeProperty = FindProperty(x => x.m_TriggerBake);
            triggerClearCache = FindProperty(x => x.m_TriggerClearCache);
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
                EditorGUILayout.PropertyField(autoBakeProperty, autoBakeTooltip);

                if (!autoBakeProperty.boolValue)
                {
                    if (GUILayout.Button("Bake"))
                    {
                        triggerBakeProperty.boolValue = true;
                    }

                    if (GUILayout.Button("Clear"))
                    {
                        triggerClearCache.boolValue = true;
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