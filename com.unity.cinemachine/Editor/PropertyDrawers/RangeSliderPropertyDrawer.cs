using System;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Cinemachine.Editor
{
    [CustomPropertyDrawer(typeof(RangeSliderAttribute))]
    sealed class RangeSliderPropertyDrawer : PropertyDrawer
    {
        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            var row = new VisualElement { style = { flexDirection = FlexDirection.Row }};

            var a = attribute as RangeSliderAttribute;
            var slider = new Slider
            {
                label = property.displayName,
                tooltip = property.tooltip,
                highValue = a.Max,
                lowValue = a.Min,
                style = { flexGrow = 4, paddingRight = 4 }
            };
            // note: slider can only be bound to float properties, so we sync manually
            slider.AddToClassList(InspectorUtility.kAlignFieldClass);
            row.Add(slider);
            switch (property.propertyType)
            {
                case SerializedPropertyType.Float:
                {
                    var field = new FloatField { label = string.Empty, tooltip = property.tooltip, style = {flexGrow = 1, flexBasis = 0} }; 
                    field.BindProperty(property);
                    row.Add(field);
                    field.RegisterValueChangedCallback((evt) => slider.value = evt.newValue);
                    slider.RegisterValueChangedCallback((evt) => 
                    {
                        property.floatValue = evt.newValue;
                        property.serializedObject.ApplyModifiedProperties();
                    });
                    break;
                }
                case SerializedPropertyType.Integer:
                {
                    var field = new IntegerField { label = string.Empty, tooltip = property.tooltip, style = {flexGrow = 1, flexBasis = 0} }; 
                    field.BindProperty(property);
                    row.Add(field);
                    field.RegisterValueChangedCallback((evt) => slider.value = evt.newValue);
                    slider.RegisterValueChangedCallback((evt) => 
                    {
                        property.intValue = Mathf.RoundToInt(evt.newValue);
                        property.serializedObject.ApplyModifiedProperties();
                    });
                    break;
                }
                default:
                {
                    Debug.LogError("Use RangeSlider with float or int properties.");
                    break;
                }
            }
            return row;
        }
    }
}
