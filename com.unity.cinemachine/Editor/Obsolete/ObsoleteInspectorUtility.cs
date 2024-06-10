#if !CINEMACHINE_NO_CM2_SUPPORT
using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;

namespace Unity.Cinemachine.Editor
{
    static partial class InspectorUtility
    {
        /// <summary>Put multiple properties on a single inspector line, with
        /// optional label overrides.  Passing null as a label (or sublabel) override will
        /// cause the property's displayName to be used as a label.  For no label at all,
        /// pass GUIContent.none.</summary>
        /// <param name="rect">Rect in which to draw</param>
        /// <param name="label">Main label</param>
        /// <param name="props">Properties to place on the line</param>
        /// <param name="subLabels">Sublabels for the properties</param>
        public static void MultiPropertyOnLine(
            Rect rect,
            GUIContent label,
            SerializedProperty[] props, GUIContent[] subLabels)
        {
            if (props == null || props.Length == 0)
                return;

            const int hSpace = 2;
            int indentLevel = EditorGUI.indentLevel;
            float labelWidth = EditorGUIUtility.labelWidth;

            float totalSubLabelWidth = 0;
            int numBoolColumns = 0;
            List<GUIContent> actualLabels = new List<GUIContent>();
            for (int i = 0; i < props.Length; ++i)
            {
                GUIContent sublabel = new GUIContent(props[i].displayName, props[i].tooltip);
                if (subLabels != null && subLabels.Length > i && subLabels[i] != null)
                    sublabel = subLabels[i];
                actualLabels.Add(sublabel);
                totalSubLabelWidth += GUI.skin.label.CalcSize(sublabel).x;
                if (i > 0)
                    totalSubLabelWidth += hSpace;
                // Special handling for toggles, or it looks stupid
                if (props[i].propertyType == SerializedPropertyType.Boolean)
                {
                    totalSubLabelWidth += rect.height + hSpace;
                    ++numBoolColumns;
                }
            }

            float subFieldWidth = rect.width - labelWidth - totalSubLabelWidth;
            float numCols = props.Length - numBoolColumns;
            float colWidth = numCols == 0 ? 0 : subFieldWidth / numCols;

            // Main label.  If no first sublabel, then main label must take on that
            // role, for mouse dragging value-scrolling support
            int subfieldStartIndex = 0;
            if (label == null)
                label = new GUIContent(props[0].displayName, props[0].tooltip);
            if (actualLabels[0] != GUIContent.none)
                rect = EditorGUI.PrefixLabel(rect, label);
            else
            {
                rect.width = labelWidth + colWidth;
                EditorGUI.PropertyField(rect, props[0], label);
                rect.x += rect.width + hSpace;
                subfieldStartIndex = 1;
            }

            for (int i = subfieldStartIndex; i < props.Length; ++i)
            {
                EditorGUI.indentLevel = 0;
                EditorGUIUtility.labelWidth = GUI.skin.label.CalcSize(actualLabels[i]).x;
                if (props[i].propertyType == SerializedPropertyType.Boolean)
                {
                    rect.x += hSpace;
                    rect.width = EditorGUIUtility.labelWidth + rect.height;
                    EditorGUI.BeginProperty(rect, actualLabels[i], props[i]);
                    props[i].boolValue = EditorGUI.ToggleLeft(rect, actualLabels[i], props[i].boolValue);
                }
                else
                {
                    rect.width = EditorGUIUtility.labelWidth + colWidth;
                    EditorGUI.BeginProperty(rect, actualLabels[i], props[i]);
                    EditorGUI.PropertyField(rect, props[i], actualLabels[i]);
                }
                EditorGUI.EndProperty();
                rect.x += rect.width + hSpace;
            }

            EditorGUIUtility.labelWidth = labelWidth;
            EditorGUI.indentLevel = indentLevel;
        }

        public static float PropertyHeightOfChidren(SerializedProperty property)
        {
            float height = 0;
            var childProperty = property.Copy();
            var endProperty = childProperty.GetEndProperty();
            childProperty.NextVisible(true);
            while (!SerializedProperty.EqualContents(childProperty, endProperty))
            {
                height += EditorGUI.GetPropertyHeight(childProperty)
                    + EditorGUIUtility.standardVerticalSpacing;
                childProperty.NextVisible(false);
            }
            return height - EditorGUIUtility.standardVerticalSpacing;
        }

        public static void DrawChildProperties(Rect position, SerializedProperty property)
        {
            var childProperty = property.Copy();
            var endProperty = childProperty.GetEndProperty();
            childProperty.NextVisible(true);
            while (!SerializedProperty.EqualContents(childProperty, endProperty))
            {
                position.height = EditorGUI.GetPropertyHeight(childProperty);
                EditorGUI.PropertyField(position, childProperty, true);
                position.y += position.height + EditorGUIUtility.standardVerticalSpacing;
                childProperty.NextVisible(false);
            }
        }

        public static void HelpBoxWithButton(
            string message, MessageType messageType,
            GUIContent buttonContent, Action onClicked)
        {
            EditorGUILayout.HelpBox(message + "\n\n", messageType, true);
            var rect = EditorGUI.IndentedRect(EditorGUILayout.GetControlRect(false, 2));

            float lineHeight = EditorGUIUtility.singleLineHeight;
            var buttonSize = GUI.skin.label.CalcSize(buttonContent);
            buttonSize.x += lineHeight;

            rect.x += rect.width - buttonSize.x - 6; rect.width = buttonSize.x;
            rect.y += rect.height - buttonSize.y - 12; rect.height = buttonSize.y + 3;
            if (GUI.Button(rect, buttonContent))
                onClicked();
        }

        public static float EnabledFoldoutHeight(SerializedProperty property, string enabledPropertyName)
        {
            var enabledProp = property.FindPropertyRelative(enabledPropertyName);
            if (enabledProp == null)
                return EditorGUI.GetPropertyHeight(property);
            if (!enabledProp.boolValue)
                return EditorGUIUtility.singleLineHeight;
            return PropertyHeightOfChidren(property);
        }

        public static bool EnabledFoldout(
            Rect rect, SerializedProperty property, string enabledPropertyName,
            GUIContent label = null)
        {
            var enabledProp = property.FindPropertyRelative(enabledPropertyName);
            if (enabledProp == null)
            {
                EditorGUI.PropertyField(rect, property, true);
                rect.x += EditorGUIUtility.labelWidth;
                EditorGUI.LabelField(rect, new GUIContent($"unknown field `{enabledPropertyName}`"));
                return property.isExpanded;
            }
            rect.height = EditorGUIUtility.singleLineHeight;
            label ??= new GUIContent(property.displayName, enabledProp.tooltip);
            EditorGUI.PropertyField(rect, enabledProp, label);
            if (enabledProp.boolValue)
            {
                ++EditorGUI.indentLevel;
                var childProperty = property.Copy();
                var endProperty = childProperty.GetEndProperty();
                childProperty.NextVisible(true);
                while (!SerializedProperty.EqualContents(childProperty, endProperty))
                {
                    if (!SerializedProperty.EqualContents(childProperty, enabledProp))
                    {
                        rect.y += rect.height + EditorGUIUtility.standardVerticalSpacing;
                        rect.height = EditorGUI.GetPropertyHeight(childProperty);
                        EditorGUI.PropertyField(rect, childProperty, true);
                    }
                    childProperty.NextVisible(false);
                }
                --EditorGUI.indentLevel;
            }
            return enabledProp.boolValue;
        }

        public static bool EnabledFoldoutSingleLine(
            Rect rect, SerializedProperty property,
            string enabledPropertyName, string disabledToggleLabel,
            GUIContent label = null)
        {
            var enabledProp = property.FindPropertyRelative(enabledPropertyName);
            if (enabledProp == null)
            {
                EditorGUI.PropertyField(rect, property, true);
                rect.x += EditorGUIUtility.labelWidth;
                EditorGUI.LabelField(rect, new GUIContent($"unknown field `{enabledPropertyName}`"));
                return property.isExpanded;
            }
            rect.height = EditorGUIUtility.singleLineHeight;
            label ??= new GUIContent(property.displayName, enabledProp.tooltip);
            EditorGUI.PropertyField(rect, enabledProp, label);
            if (!enabledProp.boolValue)
            {
                if (!string.IsNullOrEmpty(disabledToggleLabel))
                {
                    var w = EditorGUIUtility.labelWidth + EditorGUIUtility.singleLineHeight + 3;
                    var r = rect; r.x += w; r.width -= w;
                    var oldColor = GUI.color;
                    GUI.color = new(oldColor.r, oldColor.g, oldColor.g, 0.5f);
                    EditorGUI.LabelField(r, disabledToggleLabel);
                    GUI.color = oldColor;
                }
            }
            else
            {
                rect.width -= EditorGUIUtility.labelWidth + EditorGUIUtility.singleLineHeight;
                rect.x += EditorGUIUtility.labelWidth + EditorGUIUtility.singleLineHeight;

                var childProperty = property.Copy();
                var endProperty = childProperty.GetEndProperty();
                childProperty.NextVisible(true);
                while (!SerializedProperty.EqualContents(childProperty, endProperty))
                {
                    if (!SerializedProperty.EqualContents(childProperty, enabledProp))
                    {
                        var oldWidth = EditorGUIUtility.labelWidth;
                        EditorGUIUtility.labelWidth = 6; // for dragging
                        EditorGUI.PropertyField(rect, childProperty, new GUIContent(" "));
                        EditorGUIUtility.labelWidth = oldWidth;
                        break; // Draw only the first property
                    }
                    childProperty.NextVisible(false);
                }
            }
            return enabledProp.boolValue;
        }
    }
}
#endif
