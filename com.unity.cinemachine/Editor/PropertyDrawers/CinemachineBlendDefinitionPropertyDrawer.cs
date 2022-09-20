using UnityEngine;
using UnityEditor;
using UnityEngine.UIElements;
using UnityEditor.UIElements;

namespace Cinemachine.Editor
{
    [CustomPropertyDrawer(typeof(CinemachineBlendDefinition))]
    class CinemachineBlendDefinitionPropertyDrawer : PropertyDrawer
    {
        CinemachineBlendDefinition myClass = new CinemachineBlendDefinition(); // to access name strings
        public override void OnGUI(Rect rect, SerializedProperty property, GUIContent label)
        {
            float vSpace = 0;
            float floatFieldWidth = EditorGUIUtility.singleLineHeight * 2.5f;

            SerializedProperty timeProp = property.FindPropertyRelative(() => myClass.m_Time);
            GUIContent timeText = new GUIContent(" s", timeProp.tooltip);
            var textDimensions = GUI.skin.label.CalcSize(timeText);

            rect = EditorGUI.PrefixLabel(rect, EditorGUI.BeginProperty(rect, label, property));

            rect.y += vSpace; rect.height = EditorGUIUtility.singleLineHeight;
            rect.width -= floatFieldWidth + textDimensions.x;

            SerializedProperty styleProp = property.FindPropertyRelative(() => myClass.m_Style);
            bool isCustom = styleProp.enumValueIndex == (int)CinemachineBlendDefinition.Style.Custom;
            var r = rect;
            if (isCustom)
                r.width -= 2 * r.height;
            EditorGUI.PropertyField(r, styleProp, GUIContent.none);
            if (isCustom)
            {
                SerializedProperty curveProp = property.FindPropertyRelative(() => myClass.m_CustomCurve);
                r.x += r.width;
                r.width = 2 * rect.height;
                EditorGUI.BeginChangeCheck();
                EditorGUI.PropertyField(r, curveProp, GUIContent.none);
                if (EditorGUI.EndChangeCheck())
                {
                    curveProp.animationCurveValue = InspectorUtility.NormalizeCurve(curveProp.animationCurveValue);
                    curveProp.serializedObject.ApplyModifiedProperties();
                }
            }
            if (styleProp.intValue != (int)CinemachineBlendDefinition.Style.Cut)
            {
                float oldWidth = EditorGUIUtility.labelWidth;
                EditorGUIUtility.labelWidth = textDimensions.x;
                rect.x += rect.width; rect.width = floatFieldWidth + EditorGUIUtility.labelWidth;
                EditorGUI.BeginChangeCheck();
                EditorGUI.PropertyField(rect, timeProp, timeText);
                if (EditorGUI.EndChangeCheck())
                    timeProp.floatValue = Mathf.Max(timeProp.floatValue, 0);
                EditorGUIUtility.labelWidth = oldWidth;
            }
        }

        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            float floatFieldWidth = EditorGUIUtility.singleLineHeight * 2.5f;

            var row = new InspectorUtility.LeftRightContainer();
            row.Left.Add(new Label(property.displayName)
                { tooltip = property.tooltip, style = { alignSelf = Align.Center, flexGrow = 1 }});

            var styleProp = property.FindPropertyRelative(() => myClass.m_Style);
            row.Right.Add(new PropertyField(styleProp, "")
                { style = { flexGrow = 1, flexBasis = floatFieldWidth }});

            var curveProp = property.FindPropertyRelative(() => myClass.m_CustomCurve);
            var curveWidget = row.Right.AddChild(new PropertyField(curveProp, "")
                { style = { flexGrow = 0, flexBasis = floatFieldWidth }});

            var timeProp = property.FindPropertyRelative(() => myClass.m_Time);
            var timeWidget = row.Right.AddChild(new InspectorUtility.CompactPropertyField(timeProp, "s")
                { style = { flexGrow = 0, flexBasis = floatFieldWidth, marginLeft = 5 }});

            OnStyleChanged(styleProp);
            row.TrackPropertyValue(styleProp, OnStyleChanged);
            void OnStyleChanged(SerializedProperty p)
            {
                curveWidget.SetVisible(p.intValue == (int)CinemachineBlendDefinition.Style.Custom);
                timeWidget.SetVisible(p.intValue != (int)CinemachineBlendDefinition.Style.Cut);
            }

            return row;
        }
    }
}
