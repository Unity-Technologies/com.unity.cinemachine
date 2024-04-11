using UnityEditor;
using UnityEngine.UIElements;
using UnityEditor.UIElements;

namespace Unity.Cinemachine.Editor
{
    [CustomPropertyDrawer(typeof(InputAxis))]
    partial class InputAxisWithNamePropertyDrawer : PropertyDrawer
    {
        InputAxis def = new (); // to access name strings

        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            // When foldout is closed, we display the axis value on the same line, for convenience
            var foldout = new Foldout { text = property.displayName, tooltip = property.tooltip, value = property.isExpanded };
            foldout.RegisterValueChangedCallback((evt) => 
            {
                if (evt.target == foldout)
                {
                    property.isExpanded = evt.newValue;
                    property.serializedObject.ApplyModifiedProperties();
                    evt.StopPropagation();
                }
            });
            var valueProp = property.FindPropertyRelative(() => def.Value);
            var valueLabel = new Label(" ") { style = { minWidth = InspectorUtility.SingleLineHeight * 2}};
            var valueField =  new InspectorUtility.CompactPropertyField(valueProp, "") { style = { flexGrow = 1}};
            //valueField.OnInitialGeometry(() => valueField.SafeSetIsDelayed());
            valueLabel.AddPropertyDragger(valueProp, valueField);

            var ux = new InspectorUtility.FoldoutWithOverlay(foldout, valueField, valueLabel);

            var valueField2 = foldout.AddChild(new PropertyField(valueProp));
            //valueField2.OnInitialGeometry(() => valueField2.SafeSetIsDelayed());

            var centerField = foldout.AddChild(new PropertyField(property.FindPropertyRelative(() => def.Center)));
            var rangeContainer = foldout.AddChild(new VisualElement() { style = { flexDirection = FlexDirection.Row }});
            rangeContainer.Add(new PropertyField(property.FindPropertyRelative(() => def.Range)) { style = { flexGrow = 1 }});
            var wrapProp = property.FindPropertyRelative(() => def.Wrap);
            var wrap = rangeContainer.AddChild(new PropertyField(wrapProp, "") 
                { style = { alignSelf = Align.Center, marginLeft = 5, marginRight = 5 }});
            var wrapLabel = rangeContainer.AddChild(new Label(wrapProp.displayName) 
                { tooltip = wrapProp.tooltip, style = { alignSelf = Align.Center }});
            var recentering = foldout.AddChild(new PropertyField(property.FindPropertyRelative(() => def.Recentering)));

            var flagsProp = property.FindPropertyRelative(() => def.Restrictions);
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
