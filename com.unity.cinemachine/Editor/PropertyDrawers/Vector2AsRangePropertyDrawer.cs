using UnityEditor;
using UnityEngine.UIElements;
using UnityEditor.UIElements;

namespace Unity.Cinemachine.Editor
{
    [CustomPropertyDrawer(typeof(Vector2AsRangeAttribute))]
    partial class Vector2AsRangePropertyDrawer : PropertyDrawer
    {
        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            var xProp = property.FindPropertyRelative("x");
            var yProp = property.FindPropertyRelative("y");

            var ux = new InspectorUtility.LeftRightRow();
            var label = new Label(property.displayName) 
                { tooltip = property.tooltip, style = { alignSelf = Align.Center, flexGrow = 1 }};
            var minField = new PropertyField(xProp, "") { style = { flexBasis = 0, flexGrow = 1 }};
            var maxField = new InspectorUtility.CompactPropertyField(yProp, "...") 
                { tooltip = property.tooltip, style = { flexBasis = 10, flexGrow = 1, marginLeft = 5 }};

            ux.OnInitialGeometry(() =>
            {
                minField.SafeSetIsDelayed();
                maxField.SafeSetIsDelayed();
            });

            label.AddDelayedFriendlyPropertyDragger(xProp, minField, (d) => d.CancelDelayedWhenDragging = true);
            ux.Left.Add(label);
            ux.Right.Add(minField);
            ux.Right.Add(maxField);
            return ux;
        }
    }
}
