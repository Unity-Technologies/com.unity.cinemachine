using System;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Cinemachine.Editor
{
    [CustomPropertyDrawer(typeof(CmRangeAttribute))]
    sealed class CmRangeDrawer : PropertyDrawer
    {
        Label m_Label;
        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            var cmRangeAttribute = (CmRangeAttribute) attribute;
            var inspector = new Slider
            {
                label = ObjectNames.NicifyVariableName(property.name),
                highValue = cmRangeAttribute.max,
                lowValue = cmRangeAttribute.min,
            };
            inspector.BindProperty(property);
            inspector.AddToClassList(InspectorUtility.alignFieldClass);
            switch (property.propertyType)
            {
                case SerializedPropertyType.Float:
                {
                    var field = new FloatField
                    {
                        label = string.Empty
                    };
                    field.BindProperty(property);
                    inspector.Add(field);
                    break;
                }
                case SerializedPropertyType.Integer:
                {
                    var field = new IntegerField
                    {
                        label = string.Empty
                    };
                    field.BindProperty(property);
                    inspector.Add(field);
                    break;
                }
                default:
                {
                    inspector.Add(new Label("Use CmRange with float or int."));
                    break;
                }
            }
            return inspector;
        }
    }
}
