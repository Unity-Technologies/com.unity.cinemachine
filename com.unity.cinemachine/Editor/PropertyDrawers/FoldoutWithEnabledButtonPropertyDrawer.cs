using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.Cinemachine.Editor
{
    [CustomPropertyDrawer(typeof(FoldoutWithEnabledButtonAttribute))]
    class FoldoutWithEnabledButtonPropertyDrawer : PropertyDrawer
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
    class EnabledPropertyPropertyDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect rect, SerializedProperty property, GUIContent label)
        {
            var a = (EnabledPropertyAttribute)attribute;
            InspectorUtility.EnabledFoldoutSingleLine(rect, property, a.EnabledPropertyName, a.ToggleDisabledText);
        }

        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            var a = (EnabledPropertyAttribute)attribute;
            var enabledProp = property.FindPropertyRelative(a.EnabledPropertyName);
            if (enabledProp == null)
                return new PropertyField(property);

            var row = InspectorUtility.PropertyRow(enabledProp, out var enabledField, property.displayName);
            enabledField.style.flexBasis = InspectorUtility.SingleLineHeight;
            enabledField.style.flexGrow = 0;
            enabledField.style.alignSelf = Align.Center;
            enabledField.style.marginTop = 2;
            Label disabledText = null;
            if (!string.IsNullOrEmpty(a.ToggleDisabledText))
                disabledText = row.Contents.AddChild(new Label(a.ToggleDisabledText)
                    { style = { flexGrow = 0, flexBasis = 0, marginLeft = 3, alignSelf = Align.Center, opacity = 0.5f }});

            // Draw the first property in the struct
            VisualElement childField = null;
            var childProperty = property.Copy();
            var endProperty = childProperty.GetEndProperty();
            childProperty.NextVisible(true);
            while (childField == null && !SerializedProperty.EqualContents(childProperty, endProperty))
            {
                if (!SerializedProperty.EqualContents(childProperty, enabledProp))
                {
                    childField = row.Contents.AddChild(
                        new InspectorUtility.CompactPropertyField(childProperty, "") 
                        { style = { flexGrow = 1, marginTop = 0, marginBottom = 0 }});
                    row.Label.AddPropertyDragger(childProperty, childField);
                }
                childProperty.NextVisible(false);
            }

            row.TrackPropertyWithInitialCallback(enabledProp, (p) => 
            {
                childField?.SetVisible(p.boolValue);
                disabledText?.SetVisible(!p.boolValue);
            });

            return row;
        }
    }
}
