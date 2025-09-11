#if !CINEMACHINE_NO_CM2_SUPPORT
using UnityEngine;
using UnityEditor;
using System;

namespace Unity.Cinemachine.Editor
{
    partial class BlendDefinitionPropertyDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect rect, SerializedProperty property, GUIContent label)
        {
            CinemachineBlendDefinition def = new(); // to access name strings

            float vSpace = 0;
            float floatFieldWidth = EditorGUIUtility.singleLineHeight * 2.5f;

            SerializedProperty timeProp = property.FindPropertyRelative(() => def.Time);
            GUIContent timeText = new GUIContent(" s", timeProp.tooltip);
            var textDimensions = GUI.skin.label.CalcSize(timeText);

            rect = EditorGUI.PrefixLabel(rect, EditorGUI.BeginProperty(rect, label, property));

            rect.y += vSpace; rect.height = EditorGUIUtility.singleLineHeight;
            rect.width -= floatFieldWidth + textDimensions.x;

            SerializedProperty styleProp = property.FindPropertyRelative(() => def.Style);
            bool isCustom = styleProp.enumValueIndex == (int)CinemachineBlendDefinition.Styles.Custom;
            var r = rect;
            if (isCustom)
                r.width -= 2 * r.height;
            EditorGUI.PropertyField(r, styleProp, GUIContent.none);
            if (isCustom)
            {
                SerializedProperty curveProp = property.FindPropertyRelative(() => def.CustomCurve);
                r.x += r.width;
                r.width = 2 * rect.height;
                EditorGUI.BeginChangeCheck();
                EditorGUI.PropertyField(r, curveProp, GUIContent.none);
                if (EditorGUI.EndChangeCheck())
                {
                    curveProp.animationCurveValue = InspectorUtility.NormalizeCurve(curveProp.animationCurveValue);
                    curveProp.serializedObject.ApplyModifiedProperties();
                }
            }
            if (styleProp.intValue != (int)CinemachineBlendDefinition.Styles.Cut)
            {
                float oldWidth = EditorGUIUtility.labelWidth;
                EditorGUIUtility.labelWidth = textDimensions.x;
                rect.x += rect.width; rect.width = floatFieldWidth + EditorGUIUtility.labelWidth;
                EditorGUI.BeginChangeCheck();
                EditorGUI.PropertyField(rect, timeProp, timeText);
                if (EditorGUI.EndChangeCheck())
                    timeProp.floatValue = Mathf.Max(timeProp.floatValue, 0);
                EditorGUIUtility.labelWidth = oldWidth;
            }
        }
    }

    partial class FoldoutWithEnabledButtonPropertyDrawer : PropertyDrawer
    {
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            var a = (FoldoutWithEnabledButtonAttribute)attribute;
            return InspectorUtility.EnabledFoldoutHeight(property, a.EnabledPropertyName);
        }

        public override void OnGUI(Rect rect, SerializedProperty property, GUIContent label)
        {
            var a = (FoldoutWithEnabledButtonAttribute)attribute;
            InspectorUtility.EnabledFoldout(rect, property, a.EnabledPropertyName);
        }
    }

    partial class EnabledPropertyPropertyDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect rect, SerializedProperty property, GUIContent label)
        {
            var a = (EnabledPropertyAttribute)attribute;
            InspectorUtility.EnabledFoldoutSingleLine(rect, property, a.EnabledPropertyName, a.ToggleDisabledText);
        }
    }

    partial class GroupTargetPropertyDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect rect, SerializedProperty property, GUIContent label)
        {
            float floatFieldWidth = EditorGUIUtility.singleLineHeight * 3.5f;

            EditorGUI.BeginProperty(rect, GUIContent.none, property);

            float oldWidth = EditorGUIUtility.labelWidth;
            EditorGUIUtility.labelWidth = 1;

            rect.width -= 2 * (floatFieldWidth + EditorGUIUtility.singleLineHeight);
            var p = property.FindPropertyRelative(() => def.Object);
            EditorGUI.PropertyField(rect, p, new GUIContent(" ", p.tooltip));

            EditorGUIUtility.labelWidth = EditorGUIUtility.singleLineHeight;
            rect.x += rect.width; rect.width = floatFieldWidth + EditorGUIUtility.singleLineHeight;
            p = property.FindPropertyRelative(() => def.Weight);
            EditorGUI.PropertyField(rect, p, new GUIContent(" ", p.tooltip));

            rect.x += rect.width;
            p = property.FindPropertyRelative(() => def.Radius);
            EditorGUI.PropertyField(rect, p, new GUIContent(" ", p.tooltip));

            EditorGUIUtility.labelWidth = oldWidth;
            EditorGUI.EndProperty();
        }
    }

    partial class HideFoldoutPropertyDrawer : PropertyDrawer
    {
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return InspectorUtility.PropertyHeightOfChidren(property);
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            InspectorUtility.DrawChildProperties(position, property);
        }
    }

    partial class InputAxisNamePropertyDrawer : PropertyDrawer
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

    partial class InputAxisPropertyDrawer : PropertyDrawer
    {
        InputAxis def = new (); // to access name strings

        public override void OnGUI(Rect rect, SerializedProperty property, GUIContent label)
        {
            float height = EditorGUIUtility.singleLineHeight;
            rect.height = height;

            property.isExpanded = EditorGUI.Foldout(
                new Rect(rect.x, rect.y, EditorGUIUtility.labelWidth - 2 * height, rect.height),
                property.isExpanded, label, true);

            if (property.isExpanded)
            {
                ++EditorGUI.indentLevel;

                rect.y += height + EditorGUIUtility.standardVerticalSpacing;
                EditorGUI.PropertyField(rect, property.FindPropertyRelative(() => def.Value));

                var flags = property.FindPropertyRelative(() => def.Restrictions).intValue;

                var enabled = GUI.enabled;
                GUI.enabled = (flags & (int)InputAxis.RestrictionFlags.RangeIsDriven) == 0;

                rect.y += height + EditorGUIUtility.standardVerticalSpacing;
                EditorGUI.PropertyField(rect, property.FindPropertyRelative(() => def.Center));

                rect.y += height + EditorGUIUtility.standardVerticalSpacing;
                if ((flags & (int)InputAxis.RestrictionFlags.Momentary) != 0)
                    EditorGUI.PropertyField(rect, property.FindPropertyRelative(() => def.Range));
                else
                {
                    InspectorUtility.MultiPropertyOnLine(
                        rect, null,
                        new [] {
                                property.FindPropertyRelative(() => def.Range),
                                property.FindPropertyRelative(() => def.Wrap)},
                        new [] { GUIContent.none, null });
                }

                rect.y += height + EditorGUIUtility.standardVerticalSpacing;
                if ((flags & (int)(InputAxis.RestrictionFlags.NoRecentering | InputAxis.RestrictionFlags.Momentary)) == 0)
                    EditorGUI.PropertyField(rect, property.FindPropertyRelative(() => def.Recentering));

                GUI.enabled = enabled;
                --EditorGUI.indentLevel;
            }
            else
            {
                // Draw the input value on the same line as the foldout, for convenience
                var valueProp = property.FindPropertyRelative(() => def.Value);

                int oldIndent = EditorGUI.indentLevel;
                float oldLabelWidth = EditorGUIUtility.labelWidth;

                rect.x += EditorGUIUtility.labelWidth - 2 * EditorGUIUtility.singleLineHeight;
                rect.width -= EditorGUIUtility.labelWidth - 2 * EditorGUIUtility.singleLineHeight;

                EditorGUI.indentLevel = 0;
                EditorGUIUtility.labelWidth = 2 * EditorGUIUtility.singleLineHeight;
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
                height += 3 * lineHeight;
                var flags = property.FindPropertyRelative(() => def.Restrictions).intValue;
                if ((flags & (int)(InputAxis.RestrictionFlags.NoRecentering | InputAxis.RestrictionFlags.Momentary)) == 0)
                    height += EditorGUI.GetPropertyHeight(property.FindPropertyRelative(() => def.Recentering));
            }
            return height - EditorGUIUtility.standardVerticalSpacing;
        }
    }

    partial class MinMaxRangeSliderPropertyDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect rect, SerializedProperty property, GUIContent label)
        {
            var a = attribute as MinMaxRangeSliderAttribute;
            EditorGUI.BeginProperty(rect, label, property);
            {
                var v = property.vector2Value;

                // The layout system breaks alignment when mixing inspector fields with custom layout'd
                // fields as soon as a scrollbar is needed in the inspector, so we'll do the layout
                // manually instead
                const int kFloatFieldWidth = 50;
                const int kSeparatorWidth = 5;
                float indentOffset = EditorGUI.indentLevel * 15f;
                var labelRect = new Rect(rect.x, rect.y, EditorGUIUtility.labelWidth - indentOffset, rect.height);
                var floatFieldLeft = new Rect(labelRect.xMax, rect.y, kFloatFieldWidth + indentOffset, rect.height);
                var sliderRect = new Rect(floatFieldLeft.xMax + kSeparatorWidth - indentOffset, rect.y, rect.width - labelRect.width - kFloatFieldWidth * 2 - kSeparatorWidth * 2, rect.height);
                var floatFieldRight = new Rect(sliderRect.xMax + kSeparatorWidth - indentOffset, rect.y, kFloatFieldWidth + indentOffset, rect.height);

                EditorGUI.PrefixLabel(labelRect, label);
                v.x = EditorGUI.FloatField(floatFieldLeft, v.x);
                EditorGUI.MinMaxSlider(sliderRect, ref v.x, ref v.y, a.Min, a.Max);
                v.y = EditorGUI.FloatField(floatFieldRight, v.y);

                property.vector2Value = v;
            }
            EditorGUI.EndProperty();
        }
    }

    partial class SensorSizePropertyDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect rect, SerializedProperty property, GUIContent label)
        {
            var v = EditorGUI.Vector2Field(rect, property.displayName, property.vector2Value);
            v.x = Mathf.Max(v.x, 0.1f);
            v.y = Mathf.Max(v.y, 0.1f);
            property.vector2Value = v;
            property.serializedObject.ApplyModifiedProperties();
        }
    }

    partial class TagFieldPropertyDrawer : PropertyDrawer
    {
        readonly GUIContent m_ClearText = new ("Clear", "Set the tag to empty");

        public override void OnGUI(Rect rect, SerializedProperty property, GUIContent label)
        {
            const float hSpace = 2;
            var textDimensions = GUI.skin.button.CalcSize(m_ClearText);
            rect.width -= textDimensions.x + hSpace;

            var tagValue = property.stringValue;
            EditorGUI.showMixedValue = property.hasMultipleDifferentValues;
            EditorGUI.BeginChangeCheck();
            tagValue = EditorGUI.TagField(rect, EditorGUI.BeginProperty(rect, label, property), tagValue);
            if (EditorGUI.EndChangeCheck())
                property.stringValue = tagValue;
            EditorGUI.showMixedValue = false;

            rect.x += rect.width + hSpace; rect.width = textDimensions.x; rect.height -=1;
            GUI.enabled = tagValue.Length > 0;
            if (GUI.Button(rect, m_ClearText))
                property.stringValue = string.Empty;
            GUI.enabled = true;
        }
    }

    partial class Vector2AsRangePropertyDrawer : PropertyDrawer
    {
        const int hSpace = 2;
        GUIContent m_ToLabel =  new GUIContent("...");

        public override void OnGUI(Rect rect, SerializedProperty property, GUIContent label)
        {
            float toLabelSize =  GUI.skin.label.CalcSize(m_ToLabel).x + hSpace;

            float w = rect.width - EditorGUIUtility.labelWidth;
            w = (w - toLabelSize - hSpace) / 2;
            if (w > 0)
            {
                EditorGUI.BeginProperty(rect, GUIContent.none, property);

                var oldIndent = EditorGUI.indentLevel;

                var xProp = property.FindPropertyRelative("x");
                var yProp = property.FindPropertyRelative("y");

                rect.width -= w + toLabelSize + hSpace;
                float x = EditorGUI.DelayedFloatField(rect, label, xProp.floatValue);

                rect.x += rect.width + hSpace; rect.width = w + toLabelSize;
                EditorGUI.indentLevel = 0;
                EditorGUIUtility.labelWidth = toLabelSize;
                float y = EditorGUI.DelayedFloatField(rect, m_ToLabel, yProp.floatValue);

                if (xProp.floatValue != x)
                    y = Mathf.Max(x, y);
                else if (yProp.floatValue != y)
                    x = Mathf.Min(x, y);

                xProp.floatValue = x;
                yProp.floatValue = y;

                EditorGUI.indentLevel = oldIndent;
                EditorGUI.EndProperty();
            }
        }
    }

    partial class OutputChannelsPropertyDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect rect, SerializedProperty property, GUIContent label)
        {
            EditorGUI.PropertyField(rect, property, label);
        }
    }
}
#endif
