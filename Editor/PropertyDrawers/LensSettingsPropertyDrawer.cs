using UnityEngine;
using UnityEditor;
using System.Reflection;
using System.Collections.Generic;
using Cinemachine.Utility;
using System;

namespace Cinemachine.Editor
{
    [CustomPropertyDrawer(typeof(LensSettingsPropertyAttribute))]
    internal sealed class LensSettingsPropertyDrawer : PropertyDrawer
    {
        const int vSpace = 2;
        LensSettings def = new LensSettings(); // to access name strings
        GUIContent FocalLengthLabel = new GUIContent("Focal Length", "The length of the lens (in mm)");

        public override void OnGUI(Rect rect, SerializedProperty property, GUIContent label)
        {
            float height = EditorGUIUtility.singleLineHeight;
            rect.height = height;
            property.isExpanded = EditorGUI.Foldout(
                new Rect(rect.x, rect.y, EditorGUIUtility.labelWidth, rect.height), 
                property.isExpanded, label, true);
            if (property.isExpanded)
            {
                ++EditorGUI.indentLevel;
                rect.y += height + vSpace;
                if (IsOrtho)
                    EditorGUI.PropertyField(rect, property.FindPropertyRelative(() => def.OrthographicSize));
                else
                {
                    if (IsPhysical)
                        DrawFocalLengthControl(rect, property);
                    else
                        DrawFOVControl(rect, property);
                }
                rect.y += height + vSpace;
                EditorGUI.PropertyField(rect, property.FindPropertyRelative(() => def.NearClipPlane));
                rect.y += height + vSpace;
                EditorGUI.PropertyField(rect, property.FindPropertyRelative(() => def.FarClipPlane));
                if (IsPhysical)
                {
                    rect.y += height + vSpace;
                    EditorGUI.PropertyField(rect, property.FindPropertyRelative(() => def.LensShift));
                }
                rect.y += height + vSpace;
                EditorGUI.PropertyField(rect, property.FindPropertyRelative(() => def.Dutch));
                --EditorGUI.indentLevel;
            }
        }

        static float ExtraSpaceHackWTF() { return EditorGUIUtility.singleLineHeight - 2; }

        void DrawFOVControl(Rect rect, SerializedProperty property)
        {
            var FOVProperty = property.FindPropertyRelative(() => def.FieldOfView);
            float dropdownWidth = (rect.width - EditorGUIUtility.labelWidth) / 3;
            rect.width -= dropdownWidth;
            EditorGUI.PropertyField(rect, FOVProperty);
            rect.x += rect.width; rect.width = dropdownWidth;

            CinemachineLensPresets presets = CinemachineLensPresets.InstanceIfExists;
            int preset = (presets == null) ? -1 : presets.GetMatchingPreset(FOVProperty.floatValue);
            rect.x -= ExtraSpaceHackWTF(); rect.width += ExtraSpaceHackWTF();
            int selection = EditorGUI.Popup(rect, GUIContent.none, preset, m_PresetOptions);
            if (selection == m_PresetOptions.Length-1 && CinemachineLensPresets.Instance != null)
                Selection.activeObject = presets = CinemachineLensPresets.Instance;
            else if (selection >= 0 && selection < m_PresetOptions.Length-1)
            {
                FOVProperty.floatValue = presets.m_Presets[selection].m_FieldOfView;
                property.serializedObject.ApplyModifiedProperties();
            }
        }

        void DrawFocalLengthControl(Rect rect, SerializedProperty property)
        {
            var FOVProperty = property.FindPropertyRelative(() => def.FieldOfView);
            float dropdownWidth = (rect.width - EditorGUIUtility.labelWidth) / 3;
            rect.width -= dropdownWidth;
            float f = VerticalFOVToFocalLength(FOVProperty.floatValue);
            f = EditorGUI.FloatField(rect, FocalLengthLabel, f);
            f = FocalLengthToVerticalFOV(f);
            if (!Mathf.Approximately(FOVProperty.floatValue, f))
            {
                FOVProperty.floatValue = Mathf.Clamp(f, 1, 179);
                property.serializedObject.ApplyModifiedProperties();
            }
            rect.x += rect.width; rect.width = dropdownWidth;

            CinemachineLensPresets presets = CinemachineLensPresets.InstanceIfExists;
            int preset = (presets == null) ? -1 : presets.GetMatchingPhysicalPreset(VerticalFOVToFocalLength(FOVProperty.floatValue));
            rect.x -= ExtraSpaceHackWTF(); rect.width += ExtraSpaceHackWTF();
            int selection = EditorGUI.Popup(rect, GUIContent.none, preset, m_PhysicalPresetOptions);
            if (selection == m_PhysicalPresetOptions.Length-1 && CinemachineLensPresets.Instance != null)
                Selection.activeObject = presets = CinemachineLensPresets.Instance;
            else if (selection >= 0 && selection < m_PhysicalPresetOptions.Length-1)
            {
                FOVProperty.floatValue = FocalLengthToVerticalFOV(
                    presets.m_PhysicalPresets[selection].m_FocalLength);
                property.serializedObject.ApplyModifiedProperties();
            }
        }

        float VerticalFOVToFocalLength(float fov)
        {
            return SensorSize.y * 0.5f / Mathf.Tan(Mathf.Deg2Rad * fov * 0.5f);
        }

        float FocalLengthToVerticalFOV(float focalLength)
        {
            if (focalLength < UnityVectorExtensions.Epsilon)
                return 180f;
            return Mathf.Rad2Deg * 2.0f * Mathf.Atan(SensorSize.y * 0.5f / focalLength);
        }

        bool IsOrtho { get; set; }
        bool IsPhysical { get; set; }
        Vector2 SensorSize { get; set; }

        GUIContent[] m_PresetOptions = new GUIContent[0];
        GUIContent[] m_PhysicalPresetOptions = new GUIContent[0];

        void CacheABunchOfStuff(SerializedProperty property)
        {
            object lens = SerializedPropertyHelper.GetPropertyValue(property);
            IsOrtho = AccessProperty<bool>(typeof(LensSettings), lens, "Orthographic");
            IsPhysical = AccessProperty<bool>(typeof(LensSettings), lens, "IsPhysicalCamera");
            SensorSize = AccessProperty<Vector2>(typeof(LensSettings), lens, "SensorSize");

            List<GUIContent> options = new List<GUIContent>();
            CinemachineLensPresets presets = CinemachineLensPresets.InstanceIfExists;
            if (presets != null)
                for (int i = 0; i < presets.m_Presets.Length; ++i)
                    options.Add(new GUIContent(presets.m_Presets[i].m_Name));
            options.Add(new GUIContent("Edit Presets..."));
            m_PresetOptions = options.ToArray();

            options.Clear();
            if (presets != null)
                for (int i = 0; i < presets.m_PhysicalPresets.Length; ++i)
                    options.Add(new GUIContent(presets.m_PhysicalPresets[i].m_FocalLength.ToString() + "mm"));
            options.Add(new GUIContent("Edit Presets..."));
            m_PhysicalPresetOptions = options.ToArray();
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            // Cache it here because it's called less often - less garbage
            CacheABunchOfStuff(property);

            float height = EditorGUIUtility.singleLineHeight + vSpace;
            if (property.isExpanded)
                height *= IsPhysical ? 6 : 5;
            return height - vSpace;
        }

        static T AccessProperty<T>(Type type, object obj, string memberName)
        {
            if (string.IsNullOrEmpty(memberName) || (type == null))
                return default(T);

            System.Reflection.BindingFlags bindingFlags = System.Reflection.BindingFlags.Public;
            if (obj != null)
                bindingFlags |= System.Reflection.BindingFlags.Instance;
            else
                bindingFlags |= System.Reflection.BindingFlags.Static;

            PropertyInfo pi = type.GetProperty(memberName, bindingFlags);
            if ((pi != null) && (pi.PropertyType == typeof(T)))
                return (T)pi.GetValue(obj, null);
            else
                return default(T);
        }
    }
}
