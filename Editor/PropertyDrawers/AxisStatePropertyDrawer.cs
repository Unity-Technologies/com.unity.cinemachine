using UnityEngine;
using UnityEditor;
using System.Reflection;

namespace Cinemachine.Editor
{
    [CustomPropertyDrawer(typeof(AxisStatePropertyAttribute))]
    internal sealed class AxisStatePropertyDrawer : PropertyDrawer
    {
        const int vSpace = 2;
        bool mExpanded = true;
        AxisState def = new AxisState(); // to access name strings

        public override void OnGUI(Rect rect, SerializedProperty property, GUIContent label)
        {
            float height = EditorGUIUtility.singleLineHeight;
            rect.height = height;
            mExpanded = EditorGUI.Foldout(rect, mExpanded, label);
            if (mExpanded)
            {
                ++EditorGUI.indentLevel;

                rect.y += height + vSpace;
                EditorGUI.PropertyField(rect, property.FindPropertyRelative(() => def.Value));

                if (!ValueRangeIsLocked(property))
                {
                    rect.y += height + vSpace;
                    InspectorUtility.MultiPropertyOnLine(rect, new GUIContent("Value Range"),
                        new [] { property.FindPropertyRelative(() => def.m_MinValue), 
                                property.FindPropertyRelative(() => def.m_MaxValue), 
                                property.FindPropertyRelative(() => def.m_Wrap) },
                        new [] { GUIContent.none, new GUIContent("to "), null });
                }

                rect.y += height + vSpace;
                EditorGUI.PropertyField(rect, property.FindPropertyRelative(() => def.m_MaxSpeed));

                rect.y += height + vSpace;
                EditorGUI.PropertyField(rect, property.FindPropertyRelative(() => def.m_AccelTime));

                rect.y += height + vSpace;
                EditorGUI.PropertyField(rect, property.FindPropertyRelative(() => def.m_DecelTime));

                rect.y += height + vSpace;
                EditorGUI.PropertyField(rect, property.FindPropertyRelative(() => def.m_InputAxisName));

                rect.y += height + vSpace;
                InspectorUtility.MultiPropertyOnLine(rect, null,
                    new [] { property.FindPropertyRelative(() => def.m_InputAxisValue), 
                            property.FindPropertyRelative(() => def.m_InvertInput) },
                    new [] { GUIContent.none, new GUIContent("Invert") });

                --EditorGUI.indentLevel;
            }
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            float height = EditorGUIUtility.singleLineHeight + vSpace;
            if (mExpanded)
                height *= ValueRangeIsLocked(property) ? 7 : 8;
            return height;
        }

        bool ValueRangeIsLocked(SerializedProperty property)
        {
            bool locked = false;
            PropertyInfo pi = typeof(AxisState).GetProperty(
                "ValueRangeLocked", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            if (pi != null)
                locked = bool.Equals(true, pi.GetValue(SerializedPropertyHelper.GetPropertyValue(property), null));
            return locked;
        }
    }
}
