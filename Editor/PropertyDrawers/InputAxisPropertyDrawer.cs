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

                rect.y += height + EditorGUIUtility.standardVerticalSpacing;
                EditorGUI.PropertyField(rect, property.FindPropertyRelative(() => def.Center));

                rect.y += height + EditorGUIUtility.standardVerticalSpacing;
                InspectorUtility.MultiPropertyOnLine(
                    rect, null,
                    new [] {
                            property.FindPropertyRelative(() => def.Range),
                            property.FindPropertyRelative(() => def.Wrap)}, 
                    new [] { GUIContent.none, null });

                rect.y += height + EditorGUIUtility.standardVerticalSpacing;
                var recenter = property.FindPropertyRelative(() => def.Recentering);
                var enabledProp = recenter.FindPropertyRelative(() => def.Recentering.Enabled);
                EditorGUI.PropertyField(rect, enabledProp, new GUIContent(recenter.displayName, enabledProp.tooltip));
                if (enabledProp.boolValue)
                {
                    ++EditorGUI.indentLevel;
                    rect.y += height + EditorGUIUtility.standardVerticalSpacing;
                    EditorGUI.PropertyField(rect, recenter.FindPropertyRelative(() => def.Recentering.Wait));
                    rect.y += height + EditorGUIUtility.standardVerticalSpacing;
                    EditorGUI.PropertyField(rect, recenter.FindPropertyRelative(() => def.Recentering.Time));
                    --EditorGUI.indentLevel;
                }
                --EditorGUI.indentLevel;
            }
            else
            {
                rect.x += EditorGUIUtility.labelWidth;
                rect.width -= EditorGUIUtility.labelWidth;
    
                // Draw the input value on the same line as the foldout, for convenience
                var valueProp = property.FindPropertyRelative(() => def.Value);
                var valueLabel = new GUIContent(valueProp.displayName, valueProp.tooltip);

                int oldIndent = EditorGUI.indentLevel;
                float oldLabelWidth = EditorGUIUtility.labelWidth;

                EditorGUI.indentLevel = 0;
                EditorGUIUtility.labelWidth = GUI.skin.label.CalcSize(valueLabel).x;
                EditorGUI.PropertyField(rect, valueProp, valueLabel);

                EditorGUI.indentLevel = oldIndent;
                EditorGUIUtility.labelWidth = oldLabelWidth;
            }
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            int lines = 1;
            if (property != null && property.isExpanded)
            {
                lines += 4;
                var recenter = property.FindPropertyRelative(() => def.Recentering);
                if (recenter.FindPropertyRelative(() => def.Recentering.Enabled).boolValue)
                    lines += 2;
            }
            return lines * (EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing);
        }
    }
}
