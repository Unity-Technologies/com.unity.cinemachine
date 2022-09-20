using System;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Cinemachine.Editor
{
    [CustomPropertyDrawer(typeof(RangeSliderAttribute))]
    class RangeSliderPropertyDrawer : PropertyDrawer
    {
        // old IMGUI implementation must remain until no more IMGUI inspectors are using it
        public override void OnGUI(Rect rect, SerializedProperty property, GUIContent label)
        {
            var a = attribute as RangeSliderAttribute;
            EditorGUI.BeginProperty(rect, label, property);
            switch (property.propertyType)
            {
                case SerializedPropertyType.Float:
                {
                    property.floatValue = EditorGUI.Slider(rect, label, property.floatValue, a.Min, a.Max);
                    break;
                }
                case SerializedPropertyType.Integer:
                {
                    property.intValue = EditorGUI.IntSlider(rect, label, property.intValue, (int)a.Min, (int)a.Max);
                    break;
                }
                default:
                {
                    Debug.LogError("Use RangeSlider with float or int properties.");
                    break;
                }
            }
            EditorGUI.EndProperty();
        }

        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            var a = attribute as RangeSliderAttribute;
            var row = new VisualElement { style = { flexDirection = FlexDirection.Row }};
            switch (property.propertyType)
            {
                case SerializedPropertyType.Float:
                {
                    var slider = new Slider
                    {
                        label = property.displayName,
                        tooltip = property.tooltip,
                        highValue = a.Max,
                        lowValue = a.Min,
                        style = { flexGrow = 4, paddingRight = 4 }
                    };
                    slider.AddToClassList(InspectorUtility.kAlignFieldClass);
                    slider.BindProperty(property);
                    row.Add(slider);
                    var field = new FloatField { label = string.Empty, tooltip = property.tooltip, style = {flexGrow = 1, flexBasis = 0} }; 
                    field.BindProperty(property);
                    row.Add(field);
                    break;
                }
                case SerializedPropertyType.Integer:
                {
                    var slider = new SliderInt
                    {
                        label = property.displayName,
                        tooltip = property.tooltip,
                        highValue = Mathf.RoundToInt(a.Max),
                        lowValue = Mathf.RoundToInt(a.Min),
                        style = { flexGrow = 4, paddingRight = 4 }
                    };
                    slider.AddToClassList(InspectorUtility.kAlignFieldClass);
                    slider.BindProperty(property);
                    row.Add(slider);
                    var field = new IntegerField { label = string.Empty, tooltip = property.tooltip, style = {flexGrow = 1, flexBasis = 0} }; 
                    field.BindProperty(property);
                    row.Add(field);
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
