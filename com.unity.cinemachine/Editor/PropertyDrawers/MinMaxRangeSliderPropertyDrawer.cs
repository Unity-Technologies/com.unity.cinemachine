using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.Cinemachine.Editor
{
    [CustomPropertyDrawer(typeof(MinMaxRangeSliderAttribute))]
    partial class MinMaxRangeSliderPropertyDrawer : PropertyDrawer
    {
        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            var a = attribute as MinMaxRangeSliderAttribute;

            var minField = new FloatField
                { value = property.vector2Value.x, isDelayed = true, style = { flexGrow = 1, flexBasis = 0 }};
            minField.AddToClassList(InspectorUtility.AlignFieldClassName);
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
                focusable = false,
                lowLimit = a.Min, highLimit = a.Max,
                style = { flexGrow = 3, flexBasis = 0, paddingLeft = 5, paddingRight = 5 }
            };
            slider.BindProperty(property);

            var maxField = new FloatField()
                { value = property.vector2Value.y, isDelayed = true, style = { flexGrow = 1, flexBasis = 0 } };
            maxField.TrackPropertyValue(property, (evt) => maxField.value = evt.vector2Value.y);
            maxField.RegisterValueChangedCallback((evt) =>
            {
                var v = property.vector2Value;
                v.y = Mathf.Min(evt.newValue, a.Max);
                property.vector2Value = v;
                property.serializedObject.ApplyModifiedProperties();
            });

            var row = new InspectorUtility.LeftRightRow();
            row.Left.Add(new Label { text = property.displayName, tooltip = property.tooltip, style = { alignSelf = Align.Center }});
            row.Right.Add(minField);
            row.Right.Add(slider);
            row.Right.Add(maxField);
            return row;
        }
    }
}
