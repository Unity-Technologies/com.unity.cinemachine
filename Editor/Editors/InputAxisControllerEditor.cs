using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

namespace Cinemachine.Editor
{
    [CustomEditor(typeof(InputAxisController))]
    [CanEditMultipleObjects]
    internal class InputAxisControllerEditor : UnityEditor.Editor
    {
        SerializedProperty m_controllers;

        void OnEnable()
        {
            m_controllers = serializedObject.FindProperty("Controllers");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            EditorGUI.BeginChangeCheck();

            int numElements = m_controllers.arraySize;
            if (numElements == 0)
                EditorGUILayout.HelpBox("No InputAxis objects found in components.", MessageType.Info);
            else for (int i = 0; i < numElements; ++i)
            {
                var element = m_controllers.GetArrayElementAtIndex(i);

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
                    rect.x += EditorGUIUtility.labelWidth - height;
                    rect.width -= EditorGUIUtility.labelWidth - height;
    
                    // Draw the input value on the same line as the foldout, for convenience
                    var valueProp = element.FindPropertyRelative("InputName");
                    var valueLabel = new GUIContent(" ", valueProp.tooltip);

                    int oldIndent = EditorGUI.indentLevel;
                    float oldLabelWidth = EditorGUIUtility.labelWidth;

                    EditorGUI.indentLevel = 0;
                    EditorGUIUtility.labelWidth = height;
                    EditorGUI.PropertyField(rect, valueProp, valueLabel);

                    EditorGUI.indentLevel = oldIndent;
                    EditorGUIUtility.labelWidth = oldLabelWidth;
                }
            }
            if (EditorGUI.EndChangeCheck())
                serializedObject.ApplyModifiedProperties();
        }
    }
}
