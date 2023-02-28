using UnityEngine;
using UnityEditor;
using UnityEngine.UIElements;
using UnityEditor.UIElements;

namespace Cinemachine.Editor
{
    [CustomPropertyDrawer(typeof(TagFieldAttribute))]
    class TagFieldPropertyDrawer : PropertyDrawer
    {
        readonly GUIContent m_ClearText = new ("Clear", "Set the tag to empty");

        public override void OnGUI(Rect rect, SerializedProperty property, GUIContent label)
        {
            const float hSpace = 2;
            var textDimensions = GUI.skin.button.CalcSize(m_ClearText);
            rect.width -= textDimensions.x + hSpace;
            
            var tagValue = property.stringValue;
            EditorGUI.showMixedValue = property.hasMultipleDifferentValues;
            EditorGUI.BeginChangeCheck();
            tagValue = EditorGUI.TagField(rect, EditorGUI.BeginProperty(rect, label, property), tagValue);
            if (EditorGUI.EndChangeCheck())
                property.stringValue = tagValue;
            EditorGUI.showMixedValue = false;

            rect.x += rect.width + hSpace; rect.width = textDimensions.x; rect.height -=1;
            GUI.enabled = tagValue.Length > 0;
            if (GUI.Button(rect, m_ClearText))
                property.stringValue = string.Empty;
            GUI.enabled = true;
        }

        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            var row = new InspectorUtility.LabeledRow(property.displayName, property.tooltip);

            var enabled = row.Contents.AddChild(new Toggle() { style = { marginTop = 2, marginLeft = 0 }});
            enabled.RegisterValueChangedCallback((evt) => 
            {
                property.stringValue = evt.newValue ? "Untagged" : string.Empty;
                property.serializedObject.ApplyModifiedProperties();
            });

            var tagField = row.Contents.AddChild(new TagField("", property.stringValue) 
                { tooltip = property.tooltip, style = { flexGrow = 1, marginTop = 0, marginBottom = 0 }});
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
