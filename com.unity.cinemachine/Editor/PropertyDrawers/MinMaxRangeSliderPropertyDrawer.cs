using System;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Cinemachine.Editor
{
    [CustomPropertyDrawer(typeof(MinMaxRangeSliderAttribute))]
    class MinMaxRangeSliderPropertyDrawer : PropertyDrawer
    {
        // IMGUI implementation must remain until no more IMGUI inspectors are using it
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

        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            var a = attribute as MinMaxRangeSliderAttribute;

            var minField = new FloatField { value = property.vector2Value.x, style = { flexGrow = 1, flexBasis = 0 }};
            minField.AddToClassList(InspectorUtility.kAlignFieldClass);
            minField.TrackPropertyValue(property, (evt) => minField.value = evt.vector2Value.x);
            minField.RegisterValueChangedCallback((evt) =>
            {
                var v = property.vector2Value;
                v.x = Mathf.Max(evt.newValue, a.Min);
                property.vector2Value = v;
                property.serializedObject.ApplyModifiedProperties();
            });

            var slider = new MinMaxSlider()
            { 
                lowLimit = a.Min, highLimit = a.Max,
                style = { flexGrow = 3, flexBasis = 0, paddingLeft = 5, paddingRight = 5 }
            };
            slider.BindProperty(property);

            var curveMax = new FloatField() { value = property.vector2Value.y, style = { flexGrow = 1, flexBasis = 0 } };
            curveMax.TrackPropertyValue(property, (evt) => curveMax.value = evt.vector2Value.y);
            curveMax.RegisterValueChangedCallback((evt) =>
            {
                var v = property.vector2Value;
                v.y = Mathf.Min(evt.newValue, a.Max);
                property.vector2Value = v;
                property.serializedObject.ApplyModifiedProperties();
            });

            var row = new InspectorUtility.LeftRightRow { style = { flexGrow = 1 }};
            row.Left.Add(new Label { text = property.displayName, tooltip = property.tooltip, style = { alignSelf = Align.Center }});
            row.Right.Add(minField);
            row.Right.Add(slider);
            row.Right.Add(curveMax);
            return row;
        }
    }
}
