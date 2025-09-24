using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace Unity.Cinemachine.Editor
{
    [CustomPropertyDrawer(typeof(FoldoutWithEnabledButtonAttribute))]
    partial class FoldoutWithEnabledButtonPropertyDrawer : PropertyDrawer
    {
        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            var a = (FoldoutWithEnabledButtonAttribute)attribute;
            var enabledProp = property.FindPropertyRelative(a.EnabledPropertyName);
            if (enabledProp == null)
                return new PropertyField(property);

            // Don't set the text of the Foldout as this will set the Toggle.text,
            // which is on the right side of the checkbox
            var foldout = new Foldout(); // { style = { marginTop = 2 }}; // GML this hack would compensate for the current uneven line spacing
            const string kToggleClassName = "unity-foldout__toggle";
            var foldoutToggle = foldout.Q<Toggle>(className: kToggleClassName);

            // This is to counter the auto-adding of the "unity-foldout__toggle--inspector" class that
            // adds -12px margin-left. Not ideal. I raised this as an issue.
            foldoutToggle.style.marginLeft = 3;

            // This does the magic nested alignment if this Foldout is inside another Foldout
            foldoutToggle.AddToClassList(Toggle.alignedFieldUssClassName);

            // Change from arrow to checkbox
            foldoutToggle.RemoveFromClassList(kToggleClassName);

            // Bind toggle to the enabled property while displaying the main property text
            foldoutToggle.label = property.displayName;
            foldoutToggle.tooltip = property.tooltip;
            foldoutToggle.BindProperty(enabledProp);

            // Add children
            var childProperty = property.Copy();
            var endProperty = childProperty.GetEndProperty();
            childProperty.NextVisible(true);
            while (!SerializedProperty.EqualContents(childProperty, endProperty))
            {
                if (!SerializedProperty.EqualContents(childProperty, enabledProp))
                    foldout.Add(new PropertyField(childProperty));
                childProperty.NextVisible(false);
            }
            return foldout;
        }
    }


    [CustomPropertyDrawer(typeof(EnabledPropertyAttribute))]
    partial class EnabledPropertyPropertyDrawer : PropertyDrawer
    {
        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            var a = (EnabledPropertyAttribute)attribute;
            var enabledProp = property.FindPropertyRelative(a.EnabledPropertyName);

            // Use the first child field found
            var childProperty = property.Copy();
            var endProperty = childProperty.GetEndProperty();
            childProperty.NextVisible(true);
            while (!SerializedProperty.EqualContents(childProperty, endProperty))
            {
                if (!SerializedProperty.EqualContents(childProperty, enabledProp))
                    break;
                childProperty.NextVisible(false);
            }
            if (SerializedProperty.EqualContents(childProperty, endProperty))
                childProperty = null;
            if (enabledProp == null || childProperty == null)
                return new PropertyField(property);

            var row = new InspectorUtility.LabeledRow(preferredLabel, enabledProp.tooltip);
            var toggle = row.Contents.AddChild(new Toggle("")
                { style = { flexGrow = 0, marginTop = 3, marginBottom = 3, marginLeft = 3, alignSelf = Align.Center }});
            toggle.BindProperty(enabledProp);

            Label disabledText = null;
            if (!string.IsNullOrEmpty(a.ToggleDisabledText))
                disabledText = row.Contents.AddChild(new Label(a.ToggleDisabledText)
                    { style = { flexGrow = 0, flexBasis = 0, marginLeft = 8, alignSelf = Align.Center, opacity = 0.5f }});

            var childLabel = row.Contents.AddChild(new Label(childProperty.displayName)
                { style = { flexGrow = 0, marginLeft = 8, alignSelf = Align.Center }, tooltip = childProperty.tooltip});
            var childField = row.Contents.AddChild(new PropertyField(childProperty, "")
                { style = { flexGrow = 1, marginTop = -1, marginLeft = 5, marginBottom = -1, marginRight = 1 }});
            childLabel.AddDelayedFriendlyPropertyDragger(childProperty, childField, (d) => d.CancelDelayedWhenDragging = true);
            childField.RemoveFromClassList(InspectorUtility.AlignFieldClassName);

            row.TrackPropertyWithInitialCallback(enabledProp, (p) =>
            {
                if (p.IsDeletedObject())
                    return;
                childField?.SetVisible(p.boolValue);
                childLabel?.SetVisible(p.boolValue);
                disabledText?.SetVisible(!p.boolValue);
            });

            return row;
        }
    }
}
