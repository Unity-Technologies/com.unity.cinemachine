using UnityEngine;
using UnityEditor;
using UnityEngine.UIElements;
using UnityEditor.UIElements;

namespace Unity.Cinemachine.Editor
{
    [CustomPropertyDrawer(typeof(Vector2AsRangeAttribute))]
    class Vector2AsRangePropertyDrawer : PropertyDrawer
    {
        const int hSpace = 2;
        GUIContent m_ToLabel =  new GUIContent("...");

        public override void OnGUI(Rect rect, SerializedProperty property, GUIContent label)
        {
            float toLabelSize =  GUI.skin.label.CalcSize(m_ToLabel).x + hSpace;

            float w = rect.width - EditorGUIUtility.labelWidth;
            w = (w - toLabelSize - hSpace) / 2;
            if (w > 0)
            {
                EditorGUI.BeginProperty(rect, GUIContent.none, property);

                var oldIndent = EditorGUI.indentLevel;

                var xProp = property.FindPropertyRelative("x");
                var yProp = property.FindPropertyRelative("y");

                rect.width -= w + toLabelSize + hSpace;
                float x = EditorGUI.DelayedFloatField(rect, label, xProp.floatValue);

                rect.x += rect.width + hSpace; rect.width = w + toLabelSize;
                EditorGUI.indentLevel = 0;
                EditorGUIUtility.labelWidth = toLabelSize;
                float y = EditorGUI.DelayedFloatField(rect, m_ToLabel, yProp.floatValue);

                if (xProp.floatValue != x)
                    y = Mathf.Max(x, y);
                else if (yProp.floatValue != y)
                    x = Mathf.Min(x, y);

                xProp.floatValue = x;
                yProp.floatValue = y;

                EditorGUI.indentLevel = oldIndent;
                EditorGUI.EndProperty();
            }
        }

        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            var xProp = property.FindPropertyRelative("x");
            var yProp = property.FindPropertyRelative("y");

            var ux = new InspectorUtility.LeftRightRow();
            var label = new Label(property.displayName) 
                { tooltip = property.tooltip, style = { alignSelf = Align.Center, flexGrow = 1 }};
            var minField = new PropertyField(xProp, "") { style = { flexBasis = 0, flexGrow = 1}};
            var maxField = new InspectorUtility.CompactPropertyField(yProp, "...") 
                { tooltip = property.tooltip, style = { flexBasis = 10, flexGrow = 1, marginLeft = 5 }};

            ux.OnInitialGeometry(() =>
            {
                minField.SafeSetIsDelayed();
                maxField.SafeSetIsDelayed();
            });

            label.AddPropertyDragger(xProp, minField);
            ux.Left.Add(label);
            ux.Right.Add(minField);
            ux.Right.Add(maxField);
            return ux;
        }
    }
}
