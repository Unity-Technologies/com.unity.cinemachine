using UnityEngine;
using UnityEditor;
using System.Reflection;
using System.Collections.Generic;

namespace Cinemachine.Editor
{
    [CustomPropertyDrawer(typeof(LensSettingsPropertyAttribute))]
    public sealed class LensSettingsPropertyDrawer : PropertyDrawer
    {
        const int vSpace = 2;
        static bool mExpanded = true;
        LensSettings def = new LensSettings(); // to access name strings

        public override void OnGUI(Rect rect, SerializedProperty property, GUIContent label)
        {
            float height = EditorGUIUtility.singleLineHeight;
            rect.height = height;
            mExpanded = EditorGUI.Foldout(rect, mExpanded, label);
            if (mExpanded)
            {
                ++EditorGUI.indentLevel;
                rect.y += height + vSpace;
                if (IsOrtho(property))
                    EditorGUI.PropertyField(rect, property.FindPropertyRelative(() => def.OrthographicSize));
                else
                    DrawFOVControl(rect, property);
                rect.y += height + vSpace;
                EditorGUI.PropertyField(rect, property.FindPropertyRelative(() => def.NearClipPlane));
                rect.y += height + vSpace;
                EditorGUI.PropertyField(rect, property.FindPropertyRelative(() => def.FarClipPlane));
                rect.y += height + vSpace;
                EditorGUI.PropertyField(rect, property.FindPropertyRelative(() => def.Dutch));
                --EditorGUI.indentLevel;
            }
        }

        void DrawFOVControl(Rect rect, SerializedProperty property)
        {
            var FOVProperty = property.FindPropertyRelative(() => def.FieldOfView);
            float dropdownWidth = (rect.width - EditorGUIUtility.labelWidth) / 3;
            rect.width -= dropdownWidth;
            EditorGUI.PropertyField(rect, FOVProperty);
            rect.x += rect.width; rect.width = dropdownWidth;

            CinemachineLensPresets presets = CinemachineLensPresets.Instance;
            int preset = presets.GetMatchingPreset(FOVProperty.floatValue);
            int numPresets = presets.m_Presets.Length; // this value represents Edit option

            List<GUIContent> options = new List<GUIContent>();
            for (int i = 0; i < numPresets; ++i)
                options.Add(new GUIContent(presets.m_Presets[i].m_Name));
            options.Add(new GUIContent("Edit Presets..."));

            //Handles.DrawSolidRectangleWithOutline(rect, Color.green, Color.red);
            float extraSpace = EditorGUIUtility.singleLineHeight - 2;
            rect.x -= extraSpace; rect.width += extraSpace;
            int selection = EditorGUI.Popup(rect, GUIContent.none, preset, options.ToArray());
            if (selection == numPresets)
                Selection.activeObject = presets;
            else if (selection >= 0 && selection < numPresets)
            {
                FOVProperty.floatValue = presets.m_Presets[selection].m_FieldOfView;
                property.serializedObject.ApplyModifiedProperties();
            }
        }

        bool IsOrtho(SerializedProperty property)
        {
            PropertyInfo pi = typeof(LensSettings).GetProperty(
                "Orthographic", BindingFlags.NonPublic | BindingFlags.Instance);
            if (pi == null)
                return false;
            return bool.Equals(
                true, pi.GetValue(SerializedPropertyHelper.GetPropertyValue(property), null));
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            float height = EditorGUIUtility.singleLineHeight + vSpace;
            if (mExpanded)
                height *= 5;
            return height;
        }
    }
}
