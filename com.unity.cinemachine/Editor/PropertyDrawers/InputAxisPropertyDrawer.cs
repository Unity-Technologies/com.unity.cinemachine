using UnityEditor;
using UnityEngine.UIElements;
using UnityEditor.UIElements;

namespace Unity.Cinemachine.Editor
{
    [CustomPropertyDrawer(typeof(InputAxis))]
    partial class InputAxisPropertyDrawer : PropertyDrawer
    {
        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            // When foldout is closed, we display the axis value on the same line, for convenience
            var foldout = new Foldout { text = property.displayName, tooltip = property.tooltip };
            foldout.BindProperty(property);

            var valueProp = property.FindPropertyRelative(nameof(InputAxis.Value));
            var valueLabel = new Label(" ") { style = { minWidth = InspectorUtility.SingleLineHeight * 2 }};
            var valueField =  new PropertyField(valueProp, "") { style = { flexGrow = 1, marginLeft = 2 }};
            valueField.OnInitialGeometry(() => valueField.SafeSetIsDelayed());
            valueLabel.AddDelayedFriendlyPropertyDragger(valueProp, valueField, (d) => d.CancelDelayedWhenDragging = true);

            var ux = new InspectorUtility.FoldoutWithOverlay(foldout, valueField, valueLabel);

            // We want dynamic dragging on the value, even if isDelayed is set. PropertyRow puts a delayed-friendly dragger.
            foldout.AddChild(InspectorUtility.PropertyRow(valueProp, out var valueField2));
            valueField2.OnInitialGeometry(() => valueField2.SafeSetIsDelayed());

            var centerProp = property.FindPropertyRelative(nameof(InputAxis.Center));
            var centerRow = foldout.AddChild(InspectorUtility.PropertyRow(centerProp, out var centerField));
            centerField.OnInitialGeometry(() => centerField.SafeSetIsDelayed());
            centerRow.Contents.Add(new Button(() =>
            {
                valueProp.floatValue = centerProp.floatValue;
                valueProp.serializedObject.ApplyModifiedProperties();
            })
            { 
                text = "Center Now", 
                tooltip = "Reset the axis value to the center",
                style = { marginLeft = 4, marginRight = 0, marginTop = 0, marginBottom = 0 }
            });

            var rangeContainer = foldout.AddChild(new VisualElement() { style = { flexDirection = FlexDirection.Row }});
            rangeContainer.Add(new PropertyField(property.FindPropertyRelative(nameof(InputAxis.Range))) { style = { flexGrow = 1 }});
            var wrapProp = property.FindPropertyRelative(nameof(InputAxis.Wrap));
            var wrap = rangeContainer.AddChild(new PropertyField(wrapProp, "")
                { style = { alignSelf = Align.Center, marginLeft = 5, marginRight = 5, marginTop = 2 }});
            var wrapLabel = rangeContainer.AddChild(new Label(wrapProp.displayName)
                { tooltip = wrapProp.tooltip, style = { alignSelf = Align.Center }});
            var recentering = foldout.AddChild(new PropertyField(property.FindPropertyRelative(nameof(InputAxis.Recentering))));

            var flagsProp = property.FindPropertyRelative(nameof(InputAxis.Restrictions));
            ux.TrackPropertyWithInitialCallback(flagsProp, (prop) =>
            {
                if (prop.serializedObject == null)
                    return; // object deleted
                var flags = prop.intValue;
                var rangeDisabled = (flags & (int)InputAxis.RestrictionFlags.RangeIsDriven) != 0;
                centerField.SetEnabled(!rangeDisabled);
                rangeContainer.SetEnabled(!rangeDisabled);
                recentering.SetVisible((flags & (int)(InputAxis.RestrictionFlags.NoRecentering | InputAxis.RestrictionFlags.Momentary)) == 0);
                wrap.SetVisible((flags & (int)InputAxis.RestrictionFlags.Momentary) == 0);
                wrapLabel.SetVisible((flags & (int)InputAxis.RestrictionFlags.Momentary) == 0);
            });

            return ux;
        }
    }
}
