using UnityEngine;
using UnityEditor;

namespace Cinemachine.Editor
{
    [CustomEditor(typeof(InputAxisController))]
    internal class InputAxisControllerEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            var Target = (InputAxisController)target;
            if (Target != null && !Target.ConrollersAreValid())
            {
                Undo.RecordObject(Target, "SynchronizeControllers");
                Target.SynchronizeControllers();
            }

            serializedObject.Update();
            var controllers = serializedObject.FindProperty("Controllers");

            EditorGUI.BeginChangeCheck();

            EditorGUILayout.PropertyField(serializedObject.FindProperty("PlayerIndex"));

#if CINEMACHINE_UNITY_INPUTSYSTEM
            EditorGUILayout.PropertyField(serializedObject.FindProperty("AutoEnableInputs"));
#endif

            int numElements = controllers.arraySize;
            if (numElements == 0)
                EditorGUILayout.HelpBox("No applicable CM components found.  Must have one of: "
                    + InspectorUtility.GetAssignableBehaviourNames(typeof(IInputAxisTarget)), 
                    MessageType.Warning);
            else for (int i = 0; i < numElements; ++i)
            {
                var element = controllers.GetArrayElementAtIndex(i);

                var rect = EditorGUILayout.GetControlRect();
                float height = EditorGUIUtility.singleLineHeight;
                rect.height = height;

                element.isExpanded = EditorGUI.Foldout(
                    new Rect(rect.x, rect.y, EditorGUIUtility.labelWidth - height, rect.height),
                    element.isExpanded, new GUIContent(element.displayName, element.tooltip), true);

                if (element.isExpanded)
                {
                    ++EditorGUI.indentLevel;
                    rect = EditorGUILayout.GetControlRect(true, InspectorUtility.PropertyHeightOfChidren(element));
                    InspectorUtility.DrawChildProperties(rect, element);
                    --EditorGUI.indentLevel;
                }
                else
                {
                    // Draw the input value on the same line as the foldout, for convenience
                    SerializedProperty property = null;
#if CINEMACHINE_UNITY_INPUTSYSTEM
                    property = element.FindPropertyRelative("InputAction");
#elif ENABLE_LEGACY_INPUT_MANAGER
                    property = element.FindPropertyRelative("InputName");
#endif
                    if (property != null)
                    {
                        rect.x += EditorGUIUtility.labelWidth - height;
                        rect.width -= EditorGUIUtility.labelWidth - height;

                        int oldIndent = EditorGUI.indentLevel;
                        float oldLabelWidth = EditorGUIUtility.labelWidth;

                        EditorGUI.indentLevel = 0;
                        EditorGUIUtility.labelWidth = height;
                        EditorGUI.PropertyField(rect, property, new GUIContent(" ", property.tooltip));

                        EditorGUI.indentLevel = oldIndent;
                        EditorGUIUtility.labelWidth = oldLabelWidth;
                    }
                }
            }
            if (EditorGUI.EndChangeCheck())
                serializedObject.ApplyModifiedProperties();
        }

#if CINEMACHINE_UNITY_INPUTSYSTEM
        [InitializeOnLoad]
        class DefaultInputActionGetter
        {
            static DefaultInputActionGetter()
            {
                InputAxisController.GetDefaultInputAction = (axis) => 
                    (axis == 0 || axis == 1) ? ScriptableObjectUtility.DefaultLookAction : null;
            }
        }
#endif
    }
}
