using UnityEngine;
using UnityEditor;
using System;

namespace Cinemachine.Editor
{
    [CustomPropertyDrawer(typeof(InputAxisNamePropertyAttribute))]
    internal sealed class InputAxisNamePropertyDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect rect, SerializedProperty property, GUIContent label)
        {
            EditorGUI.PropertyField(rect, property, label);

            // Is the axis name valid?
            var nameError = string.Empty;
            var nameValue = property.stringValue;
            if (nameValue.Length > 0)
                try { CinemachineCore.GetInputAxis(nameValue); }
                catch (ArgumentException e) { nameError = e.Message; }

            // Show an error icon if there's a problem
            if (nameError.Length > 0)
            {
                int oldIndent = EditorGUI.indentLevel;
                float oldLabelWidth = EditorGUIUtility.labelWidth;

                EditorGUI.indentLevel = 0;
                EditorGUIUtility.labelWidth = 1;

                var w = rect.height;
                rect.x += rect.width - w; rect.width = w;
                EditorGUI.LabelField(rect, new GUIContent(
                    EditorGUIUtility.IconContent("console.erroricon.sml").image,
                    nameError));

                EditorGUI.indentLevel = oldIndent;
                EditorGUIUtility.labelWidth = oldLabelWidth;
            }
        }
    }
}
