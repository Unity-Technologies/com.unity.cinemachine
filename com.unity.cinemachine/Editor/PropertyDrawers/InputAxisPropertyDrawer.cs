using UnityEngine;
using UnityEditor;
using System;

namespace Cinemachine.Editor
{
    [CustomPropertyDrawer(typeof(InputAxis))]
    internal sealed class InputAxisWithNamePropertyDrawer : PropertyDrawer
    {
        InputAxis def = new InputAxis(); // to access name strings

        public override void OnGUI(Rect rect, SerializedProperty property, GUIContent label)
        {
            float height = EditorGUIUtility.singleLineHeight;
            rect.height = height;

            property.isExpanded = EditorGUI.Foldout(
                new Rect(rect.x, rect.y, EditorGUIUtility.labelWidth, rect.height),
                property.isExpanded, label, true);

            if (property.isExpanded)
            {
                ++EditorGUI.indentLevel;

                rect.y += height + EditorGUIUtility.standardVerticalSpacing;
                EditorGUI.PropertyField(rect, property.FindPropertyRelative(() => def.Value));

                var flags = property.FindPropertyRelative(() => def.InspectorFlags).intValue;

                var enabled = GUI.enabled;
                GUI.enabled = (flags & (int)InputAxis.Flags.RangeIsDriven) == 0;

                rect.y += height + EditorGUIUtility.standardVerticalSpacing;
                EditorGUI.PropertyField(rect, property.FindPropertyRelative(() => def.Center));

                rect.y += height + EditorGUIUtility.standardVerticalSpacing;
                InspectorUtility.MultiPropertyOnLine(
                    rect, null,
                    new [] {
                            property.FindPropertyRelative(() => def.Range),
                            property.FindPropertyRelative(() => def.Wrap)}, 
                    new [] { GUIContent.none, null });

                GUI.enabled = enabled;

                if ((flags & (int)InputAxis.Flags.HideRecentering) == 0)
                {
                    rect.y += height + EditorGUIUtility.standardVerticalSpacing;
                    var recenter = property.FindPropertyRelative(() => def.Recentering);
                    EditorGUI.PropertyField(rect, recenter);
                }
                --EditorGUI.indentLevel;
            }
            else
            {
                // Draw the input value on the same line as the foldout, for convenience
                var valueProp = property.FindPropertyRelative(() => def.Value);

                int oldIndent = EditorGUI.indentLevel;
                float oldLabelWidth = EditorGUIUtility.labelWidth;

                rect.x += EditorGUIUtility.labelWidth - EditorGUIUtility.singleLineHeight;
                rect.width -= EditorGUIUtility.labelWidth - EditorGUIUtility.singleLineHeight;

                EditorGUI.indentLevel = 0;
                EditorGUIUtility.labelWidth = EditorGUIUtility.singleLineHeight;
                EditorGUI.PropertyField(rect, valueProp, new GUIContent(" ", valueProp.tooltip));
                EditorGUI.indentLevel = oldIndent;
                EditorGUIUtility.labelWidth = oldLabelWidth;
            }
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            var lineHeight = EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
            var height = lineHeight;
            if (property != null && property.isExpanded)
            {
                var flags = property.FindPropertyRelative(() => def.InspectorFlags).intValue;
                height += 3 * lineHeight;
                if ((flags & (int)InputAxis.Flags.HideRecentering) == 0)
                    height += EditorGUI.GetPropertyHeight(property.FindPropertyRelative(() => def.Recentering)) 
                        + EditorGUIUtility.standardVerticalSpacing;
            }
            return height - EditorGUIUtility.standardVerticalSpacing;
        }
    }
}
