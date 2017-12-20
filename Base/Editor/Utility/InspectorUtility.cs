using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;

namespace Cinemachine.Editor
{
    public class InspectorUtility
    {
        /// <summary>Put multiple properties on a single inspector line, with 
        /// optional label overrides.  Passing null as a label (or sublabel) override will 
        /// cause the property's displayName to be used as a label.  For no label at all,
        /// pass GUIContent.none.</summary>
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
                    totalSubLabelWidth += rect.height;
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
                    rect.width = EditorGUIUtility.labelWidth + rect.height;
                    props[i].boolValue = EditorGUI.ToggleLeft(rect, actualLabels[i], props[i].boolValue);
                }
                else
                {
                    rect.width = EditorGUIUtility.labelWidth + colWidth;
                    EditorGUI.PropertyField(rect, props[i], actualLabels[i]);
                }
                rect.x += rect.width + hSpace;
            }

            EditorGUIUtility.labelWidth = labelWidth;
            EditorGUI.indentLevel = indentLevel;
        }
    }
}
