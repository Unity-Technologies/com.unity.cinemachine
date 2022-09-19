using UnityEngine;
using UnityEditor;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
using System;

namespace Cinemachine.Editor
{
    [CustomPropertyDrawer(typeof(InputAxisNamePropertyAttribute))]
    class InputAxisNamePropertyDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect rect, SerializedProperty property, GUIContent label)
        {
            EditorGUI.PropertyField(rect, property, label);

            // Is the axis name valid?
            var nameError = string.Empty;
            var nameValue = property.stringValue;
            if (nameValue.Length > 0)
                try { CinemachineCore.GetInputAxis(nameValue); }
                catch (ArgumentException e) { nameError = e.Message; }

            // Show an error icon if there's a problem
            if (nameError.Length > 0)
            {
                int oldIndent = EditorGUI.indentLevel;
                float oldLabelWidth = EditorGUIUtility.labelWidth;

                EditorGUI.indentLevel = 0;
                EditorGUIUtility.labelWidth = 1;

                var w = rect.height;
                rect.x += rect.width - w; rect.width = w;
                EditorGUI.LabelField(rect, new GUIContent(
                    EditorGUIUtility.IconContent("console.erroricon.sml").image,
                    nameError));

                EditorGUI.indentLevel = oldIndent;
                EditorGUIUtility.labelWidth = oldLabelWidth;
            }
        }
#if false  // GML incomplete code.  This is not working yet in UITK - stay in IMGUI for now
        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            var row = new VisualElement { style = { flexDirection = FlexDirection.Row }};
            row.Add(new PropertyField(property, "") { style = { flexGrow = 1 }});
            var error = new Label 
            { 
                style = 
                { 
                    flexGrow = 0,
                    backgroundImage = (StyleBackground)EditorGUIUtility.IconContent("console.erroricon.sml").image,
                    width = InspectorUtility.SingleLineHeight, height = InspectorUtility.SingleLineHeight,
                    alignSelf = Align.Center,
                    paddingRight = 0, borderRightWidth = 0, marginRight = 0
                }
            };
            row.Add(error);
            return row;
        }
#endif
    }
}
