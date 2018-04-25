using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using UnityEngine.Playables;

namespace Cinemachine.Editor
{
    [CustomEditor(typeof(CinemachineTriggerAction))]
    internal class CinemachineTriggerActionEditor : BaseEditor<CinemachineTriggerAction>
    {
        const int vSpace = 2;
        CinemachineTriggerAction.ActionSettings def 
            = new CinemachineTriggerAction.ActionSettings(); // to access name strings

        static bool mEnterExpanded;
        static bool mExitExpanded;

        SerializedProperty[] mRepeatProperties = new SerializedProperty[2];
        GUIContent mRepeatLabel;
        GUIContent[] mRepeatSubLabels = new GUIContent[2];
        
        private void OnEnable()
        {
            mRepeatProperties[0] = FindProperty(x => x.m_SkipFirst);
            mRepeatProperties[1] = FindProperty(x => x.m_Repeating);
            mRepeatLabel = new GUIContent(
                mRepeatProperties[0].displayName, mRepeatProperties[0].tooltip);
            mRepeatSubLabels[0] = GUIContent.none;
            mRepeatSubLabels[1] = new GUIContent(
                mRepeatProperties[1].displayName, mRepeatProperties[1].tooltip);
        }

        protected override List<string> GetExcludedPropertiesInInspector()
        {
            List<string> excluded = base.GetExcludedPropertiesInInspector();
            excluded.Add(FieldPath(x => x.m_SkipFirst));
            excluded.Add(FieldPath(x => x.m_Repeating));
            excluded.Add(FieldPath(x => x.m_EnterAction));
            excluded.Add(FieldPath(x => x.m_ExitAction));
            return excluded;
        }

        public override void OnInspectorGUI()
        {
            BeginInspector();
            DrawRemainingPropertiesInInspector();
            InspectorUtility.MultiPropertyOnLine(
                EditorGUILayout.GetControlRect(), mRepeatLabel,
                mRepeatProperties, mRepeatSubLabels);
            mEnterExpanded = DrawActionSettings(FindProperty(x => x.m_EnterAction), mEnterExpanded);
            mExitExpanded = DrawActionSettings(FindProperty(x => x.m_ExitAction), mExitExpanded);
        }

        bool DrawActionSettings(SerializedProperty property, bool expanded)
        {
            Rect r = EditorGUILayout.GetControlRect();
            expanded = EditorGUI.Foldout(r, expanded, property.displayName);
            if (expanded)
            {
                SerializedProperty actionProp = property.FindPropertyRelative(() => def.m_Action);
                EditorGUILayout.PropertyField(actionProp);

                SerializedProperty targetProp = property.FindPropertyRelative(() => def.m_Target);
                EditorGUILayout.PropertyField(targetProp);

                bool isBoost = actionProp.intValue == (int)CinemachineTriggerAction.ActionSettings.Mode.PriorityBoost;
                if (isBoost)
                    EditorGUILayout.PropertyField(property.FindPropertyRelative(() => def.m_BoostAmount));

                bool isPlay = actionProp.intValue == (int)CinemachineTriggerAction.ActionSettings.Mode.Play;
                if (isPlay)
                {
                    SerializedProperty[] props = new SerializedProperty[2]
                    {
                        property.FindPropertyRelative(() => def.m_StartTime),
                        property.FindPropertyRelative(() => def.m_Mode)
                    };
                    GUIContent[] sublabels = new GUIContent[2] 
                    { 
                        GUIContent.none, new GUIContent("s", props[1].tooltip)
                    };
                    InspectorUtility.MultiPropertyOnLine(
                        EditorGUILayout.GetControlRect(), null, props, sublabels);
                }

                SerializedProperty methodProp = property.FindPropertyRelative(() => def.m_MethodName);
                bool isBroadcast = actionProp.intValue == (int)CinemachineTriggerAction.ActionSettings.Mode.Broadcast;
                if (isBroadcast)
                {
                    EditorGUILayout.PropertyField(methodProp);
                    string value = methodProp.stringValue.Trim();
                    if (value.Length == 0)
                        EditorGUILayout.HelpBox("Supply the name of a method to call. The method will be called in the target object, if it exists", MessageType.Info);
                }

                if (isBoost)
                {
                    if (GetTargetComponent<CinemachineVirtualCameraBase>(targetProp.objectReferenceValue) == null)
                        EditorGUILayout.HelpBox("Target must be a CinemachineVirtualCameraBase in order to boost priority", MessageType.Warning);
                }

                bool isEnableDisable = (actionProp.intValue == (int)CinemachineTriggerAction.ActionSettings.Mode.Enable
                    || actionProp.intValue == (int)CinemachineTriggerAction.ActionSettings.Mode.Disable);
                if (isEnableDisable)
                {
                    var value = targetProp.objectReferenceValue;
                    if (value != null && (value as Behaviour) == null)
                        EditorGUILayout.HelpBox("Target must be a Behaviour in order to Enable/Disable", MessageType.Warning);
                }

                bool isPlayStop = isPlay
                    || actionProp.intValue == (int)CinemachineTriggerAction.ActionSettings.Mode.Stop;
                if (isPlayStop)
                {
                    if (GetTargetComponent<Animator>(targetProp.objectReferenceValue) == null
                        && GetTargetComponent<PlayableDirector>(targetProp.objectReferenceValue) == null)
                    {
                        EditorGUILayout.HelpBox("Target must have a PlayableDirector or Animator in order to Play/Stop", MessageType.Warning);
                    }
                }

                if (targetProp.objectReferenceValue == null)
                    EditorGUILayout.HelpBox("No action will be taken because target is not valid", MessageType.Info);

                EditorGUILayout.Space();
                EditorGUILayout.PropertyField(property.FindPropertyRelative(() => def.m_Event));
            }
            property.serializedObject.ApplyModifiedProperties();
            return expanded;
        }

        T GetTargetComponent<T>(UnityEngine.Object obj) where T : Behaviour
        {
            UnityEngine.Object currentTarget = obj;
            if (currentTarget != null)
            {
                GameObject targetGameObject = currentTarget as GameObject;
                Behaviour targetBehaviour = currentTarget as Behaviour;
                if (targetBehaviour != null)
                    targetGameObject = targetBehaviour.gameObject;
                if (targetBehaviour is T)
                    return targetBehaviour as T;
                if (targetGameObject != null)
                    return targetGameObject.GetComponent<T>();
            }
            return null;
        }
    }
}
