using UnityEngine;
using UnityEditor;

namespace Cinemachine.Editor
{
    [CustomPropertyDrawer(typeof(Vector2AsRangeAttribute))]
    internal sealed class Vector2AsRangePropertyDrawer : PropertyDrawer
    {
        const int hSpace = 2;
        public override void OnGUI(Rect rect, SerializedProperty property, GUIContent label)
        {
            var toLabel =  new GUIContent("...");
            float toLabelSize =  GUI.skin.label.CalcSize(toLabel).x + hSpace;

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
                float y = EditorGUI.DelayedFloatField(rect, toLabel, yProp.floatValue);

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
    }
}
