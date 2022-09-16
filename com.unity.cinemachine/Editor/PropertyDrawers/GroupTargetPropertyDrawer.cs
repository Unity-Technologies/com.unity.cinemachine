using UnityEngine;
using UnityEditor;
using UnityEngine.UIElements;
using UnityEditor.UIElements;

namespace Cinemachine.Editor
{
    [CustomPropertyDrawer(typeof(CinemachineTargetGroup.Target))]
    class GroupTargetPropertyDrawer : PropertyDrawer
    {
        CinemachineTargetGroup.Target def = new();

        public override void OnGUI(Rect rect, SerializedProperty property, GUIContent label)
        {
            float floatFieldWidth = EditorGUIUtility.singleLineHeight * 3.5f;

            EditorGUI.BeginProperty(rect, GUIContent.none, property);

            float oldWidth = EditorGUIUtility.labelWidth;
            EditorGUIUtility.labelWidth = 1;

            rect.width -= 2 * (floatFieldWidth + EditorGUIUtility.singleLineHeight);
            var p = property.FindPropertyRelative(() => def.Object);
            EditorGUI.PropertyField(rect, p, new GUIContent(" ", p.tooltip));

            EditorGUIUtility.labelWidth = EditorGUIUtility.singleLineHeight;
            rect.x += rect.width; rect.width = floatFieldWidth + EditorGUIUtility.singleLineHeight;
            p = property.FindPropertyRelative(() => def.Weight);
            EditorGUI.PropertyField(rect, p, new GUIContent(" ", p.tooltip));

            rect.x += rect.width;
            p = property.FindPropertyRelative(() => def.Radius);
            EditorGUI.PropertyField(rect, p, new GUIContent(" ", p.tooltip));

            EditorGUIUtility.labelWidth = oldWidth;
            EditorGUI.EndProperty();
        }

        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            float floatFieldWidth = EditorGUIUtility.singleLineHeight * 3.5f;
            var ux = new VisualElement() { style = { flexDirection = FlexDirection.Row }};

            ux.Add(new PropertyField(property.FindPropertyRelative(() => def.Object), string.Empty) 
                { style = { flexGrow = 1, flexBasis = floatFieldWidth }});
            ux.Add(new InspectorUtility.CompactPropertyField(
                property.FindPropertyRelative(() => def.Radius), "R")
                { style = { flexGrow = 0, flexBasis = floatFieldWidth, marginLeft = 5 }});
            ux.Add(new InspectorUtility.CompactPropertyField(
                property.FindPropertyRelative(() => def.Weight), "W")
                { style = { flexGrow = 0, flexBasis = floatFieldWidth, marginLeft = 5 }});

            return ux;
        }
    }
}
