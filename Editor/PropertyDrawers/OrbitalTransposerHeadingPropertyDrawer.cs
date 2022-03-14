using UnityEngine;
using UnityEditor;

namespace Cinemachine.Editor
{
    [CustomPropertyDrawer(typeof(OrbitalTransposerHeadingPropertyAttribute))]
    internal sealed class OrbitalTransposerHeadingPropertyDrawer : PropertyDrawer
    {
        const int vSpace = 2;
        bool mExpanded = true;
        CmOrbitalTransposer.Heading def = new CmOrbitalTransposer.Heading(); // to access name strings

        public override void OnGUI(Rect rect, SerializedProperty property, GUIContent label)
        {
            float height = EditorGUIUtility.singleLineHeight;
            rect.height = height;
            mExpanded = EditorGUI.Foldout(rect, mExpanded, EditorGUI.BeginProperty(rect, label, property), true);
            if (mExpanded)
            {
                ++EditorGUI.indentLevel;

                rect.y += height + vSpace;
                EditorGUI.PropertyField(rect, property.FindPropertyRelative(() => def.m_Definition));

                if (IsVelocityMode(property))
                {
                    rect.y += height + vSpace;
                    EditorGUI.PropertyField(rect, property.FindPropertyRelative(() => def.m_VelocityFilterStrength));
                }

                rect.y += height + vSpace;
                EditorGUI.PropertyField(rect, property.FindPropertyRelative(() => def.m_Bias));

                --EditorGUI.indentLevel;
            }
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            float height = EditorGUIUtility.singleLineHeight + vSpace;
            if (mExpanded)
                height *= IsVelocityMode(property) ? 4 : 3;
            return height;
        }

        bool IsVelocityMode(SerializedProperty property)
        {
            var mode = property.FindPropertyRelative(() => def.m_Definition);
            var value = (CmOrbitalTransposer.Heading.HeadingDefinition)
                (System.Enum.GetValues(typeof(CmOrbitalTransposer.Heading.HeadingDefinition))).GetValue(mode.enumValueIndex);
            return value == CmOrbitalTransposer.Heading.HeadingDefinition.Velocity
                || value == CmOrbitalTransposer.Heading.HeadingDefinition.PositionDelta;
        }
    }
}
