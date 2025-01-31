using UnityEditor;
using UnityEngine.UIElements;
using UnityEditor.UIElements;

namespace Unity.Cinemachine.Editor
{
    [CustomPropertyDrawer(typeof(TagFieldAttribute))]
    partial class TagFieldPropertyDrawer : PropertyDrawer
    {
        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            var row = new InspectorUtility.LabeledRow(property.displayName, property.tooltip);

            var enabled = row.Contents.AddChild(new Toggle() { style = { marginTop = 3, marginLeft = 2, marginBottom = 3 }});
            enabled.RegisterValueChangedCallback((evt) =>
            {
                property.stringValue = evt.newValue ? "Untagged" : string.Empty;
                property.serializedObject.ApplyModifiedProperties();
            });

            var tagField = row.Contents.AddChild(new TagField("", property.stringValue)
                { tooltip = property.tooltip, style = { flexGrow = 1, marginTop = 0, marginBottom = 0, marginLeft = 5 }});
            tagField.RegisterValueChangedCallback((evt) =>
            {
                property.stringValue = evt.newValue;
                property.serializedObject.ApplyModifiedProperties();
            });

            row.TrackPropertyWithInitialCallback(property, (p) =>
            {
                var isEmpty = string.IsNullOrEmpty(p.stringValue);
                enabled.SetValueWithoutNotify(!isEmpty);
                tagField.SetVisible(!isEmpty);
                tagField.value = p.stringValue;
            });
            return row;
        }
    }
}
