using UnityEngine;
using UnityEditor;
using UnityEngine.UIElements;
using UnityEditor.UIElements;

namespace Cinemachine.Editor
{
    [CustomPropertyDrawer(typeof(CameraPriority))]
    class CameraPriorityPropertyDrawer : PropertyDrawer
    {
        CameraPriority def;

        // old IMGUI implementation must remain until no more IMGUI inspectors are using it
        GUIContent m_CustomPriorityLabel = new GUIContent("Value", "The custom priority value.  0 is default.");

        public override void OnGUI(Rect rect, SerializedProperty property, GUIContent label)
        {
            var w = EditorGUIUtility.labelWidth + EditorGUIUtility.singleLineHeight + 5;
            var r = new Rect(rect.x, rect.y, w, rect.height);
            var customProp = property.FindPropertyRelative(() => def.UseCustomPriority);
            EditorGUI.PropertyField(r, customProp, new GUIContent("Priority", property.tooltip));

            r.x += r.width; r.width = rect.width - w;
            if (!customProp.boolValue)
                EditorGUI.LabelField(r, "(using default)");
            else
            {
                var oldWidth = EditorGUIUtility.labelWidth;
                EditorGUIUtility.labelWidth = GUI.skin.label.CalcSize(m_CustomPriorityLabel).x + 2;
                EditorGUI.PropertyField(r, property.FindPropertyRelative(() => def.Priority), m_CustomPriorityLabel);
                EditorGUIUtility.labelWidth = oldWidth;
            }
        }

        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            var customProp = property.FindPropertyRelative(() => def.UseCustomPriority);

            var row = new InspectorUtility.LeftRightContainer();
            row.Left.Add(new Label("Priority") { tooltip = property.tooltip, style = { alignSelf = Align.Center, flexGrow = 0 }});
            row.Right.Add(new PropertyField(customProp, "") 
                { tooltip = property.tooltip, style = { alignSelf = Align.Center, flexGrow = 0, marginRight = 5 }});
            var defaultLabel = row.Right.AddChild(new Label("(using default)") 
                { style = { alignSelf = Align.Center, flexGrow = 0, unityFontStyleAndWeight = FontStyle.Italic }});
            var valueField = row.Right.AddChild(new InspectorUtility.CompactPropertyField(
                property.FindPropertyRelative(() => def.Priority), "Value") { style = { flexGrow = 1 }});

            TrackCustom(customProp);
            row.TrackPropertyValue(customProp, TrackCustom);
            void TrackCustom(SerializedProperty p)
            {
                var isCustom = p.boolValue;
                defaultLabel.SetVisible(!isCustom);
                valueField.SetVisible(isCustom);
            }

            return row;
        }
    }
}
