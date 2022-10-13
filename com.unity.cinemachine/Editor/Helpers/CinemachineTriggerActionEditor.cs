using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using UnityEngine.Playables;

namespace Cinemachine.Editor
{
#if CINEMACHINE_PHYSICS || CINEMACHINE_PHYSICS_2D
    [CustomEditor(typeof(CinemachineTriggerAction))]
    class CinemachineTriggerActionEditor : BaseEditor<CinemachineTriggerAction>
    {
        CinemachineTriggerAction.ActionSettings m_Def = new(); // to access name strings

        static bool s_EnterExpanded;
        static bool s_ExitExpanded;

        SerializedProperty[] m_RepeatProperties = new SerializedProperty[2];
        GUIContent m_RepeatLabel;
        GUIContent[] m_RepeatSubLabels = new GUIContent[2];

        GUIStyle m_FoldoutStyle;

        void OnEnable()
        {
            m_RepeatProperties[0] = FindProperty(x => x.SkipFirst);
            m_RepeatProperties[1] = FindProperty(x => x.Repeating);
            m_RepeatLabel = new GUIContent(
                m_RepeatProperties[0].displayName, m_RepeatProperties[0].tooltip);
            m_RepeatSubLabels[0] = GUIContent.none;
            m_RepeatSubLabels[1] = new GUIContent(
                m_RepeatProperties[1].displayName, m_RepeatProperties[1].tooltip);
        }

        /// <summary>Get the property names to exclude in the inspector.</summary>
        /// <param name="excluded">Add the names to this list</param>
        protected override void GetExcludedPropertiesInInspector(List<string> excluded)
        {
            base.GetExcludedPropertiesInInspector(excluded);
            excluded.Add(FieldPath(x => x.SkipFirst));
            excluded.Add(FieldPath(x => x.Repeating));
            excluded.Add(FieldPath(x => x.OnObjectEnter));
            excluded.Add(FieldPath(x => x.OnObjectExit));
        }

        public override void OnInspectorGUI()
        {
            BeginInspector();
            DrawRemainingPropertiesInInspector();
            InspectorUtility.MultiPropertyOnLine(
                EditorGUILayout.GetControlRect(), m_RepeatLabel,
                m_RepeatProperties, m_RepeatSubLabels);
            EditorGUILayout.Space();
            s_EnterExpanded = DrawActionSettings(FindProperty(x => x.OnObjectEnter), s_EnterExpanded);
            s_ExitExpanded = DrawActionSettings(FindProperty(x => x.OnObjectExit), s_ExitExpanded);
        }

        bool DrawActionSettings(SerializedProperty property, bool expanded)
        {
            if (m_FoldoutStyle == null)
                m_FoldoutStyle = new GUIStyle(EditorStyles.foldout) { fontStyle = FontStyle.Bold };

            Rect r = EditorGUILayout.GetControlRect();
            expanded = EditorGUI.Foldout(r, expanded, property.displayName, true, m_FoldoutStyle);
            if (expanded)
            {
                SerializedProperty actionProp = property.FindPropertyRelative(() => m_Def.Action);
                EditorGUILayout.PropertyField(actionProp);

                SerializedProperty targetProp = property.FindPropertyRelative(() => m_Def.Target);
                bool isCustom = (actionProp.intValue == (int)CinemachineTriggerAction.ActionSettings.ActionModes.Custom);
                if (!isCustom)
                    EditorGUILayout.PropertyField(targetProp);

                bool isBoost = actionProp.intValue == (int)CinemachineTriggerAction.ActionSettings.ActionModes.PriorityBoost;
                if (isBoost)
                    EditorGUILayout.PropertyField(property.FindPropertyRelative(() => m_Def.BoostAmount));

#if CINEMACHINE_TIMELINE
                bool isPlay = actionProp.intValue == (int)CinemachineTriggerAction.ActionSettings.ActionModes.Play;
                if (isPlay)
                {
                    var props = new SerializedProperty[2]
                    {
                        property.FindPropertyRelative(() => m_Def.StartTime),
                        property.FindPropertyRelative(() => m_Def.Mode)
                    };
                    var sublabels = new GUIContent[2]
                    {
                        GUIContent.none, new GUIContent("s", props[1].tooltip)
                    };
                    InspectorUtility.MultiPropertyOnLine(
                        EditorGUILayout.GetControlRect(), null, props, sublabels);
                }
#endif
                if (actionProp.intValue == (int)CinemachineTriggerAction.ActionSettings.ActionModes.Custom)
                {
                    EditorGUILayout.HelpBox("Use the Event() list below to call custom methods", MessageType.Info);
                }

                if (isBoost)
                {
                    if (GetTargetComponent<CinemachineVirtualCameraBase>(targetProp.objectReferenceValue) == null)
                        EditorGUILayout.HelpBox("Target must be a CinemachineVirtualCameraBase in order to boost priority", MessageType.Warning);
                }

                bool isEnableDisable = (actionProp.intValue == (int)CinemachineTriggerAction.ActionSettings.ActionModes.Enable
                    || actionProp.intValue == (int)CinemachineTriggerAction.ActionSettings.ActionModes.Disable);
                if (isEnableDisable)
                {
                    var value = targetProp.objectReferenceValue;
                    if (value != null && (value as Behaviour) == null)
                        EditorGUILayout.HelpBox("Target must be a Behaviour in order to Enable/Disable", MessageType.Warning);
                }
#if CINEMACHINE_TIMELINE
                bool isPlayStop = isPlay
                    || actionProp.intValue == (int)CinemachineTriggerAction.ActionSettings.ActionModes.Stop;
                if (isPlayStop)
                {
                    if (GetTargetComponent<Animator>(targetProp.objectReferenceValue) == null
                        && GetTargetComponent<PlayableDirector>(targetProp.objectReferenceValue) == null)
                    {
                        EditorGUILayout.HelpBox("Target must have a PlayableDirector or Animator in order to Play/Stop", MessageType.Warning);
                    }
                }
#endif
                if (!isCustom && targetProp.objectReferenceValue == null)
                    EditorGUILayout.HelpBox("No action will be taken because target is not valid", MessageType.Info);

                EditorGUILayout.Space();
                EditorGUILayout.LabelField("This event will be invoked.  Add calls to custom methods here:");
                EditorGUILayout.PropertyField(property.FindPropertyRelative(() => m_Def.Event));
            }
            property.serializedObject.ApplyModifiedProperties();
            return expanded;
        }

        static T GetTargetComponent<T>(UnityEngine.Object obj) where T : Behaviour
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
                if (targetGameObject != null && targetGameObject.TryGetComponent<T>(out var t))
                    return t;
            }
            return null;
        }
    }
#endif
}
