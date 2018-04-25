using UnityEngine;
using UnityEditor;
using System.Reflection;
using System.Collections.Generic;
using Cinemachine.Utility;

namespace Cinemachine.Editor
{
    [CustomPropertyDrawer(typeof(LensSettingsPropertyAttribute))]
    internal sealed class LensSettingsPropertyDrawer : PropertyDrawer
    {
        const int vSpace = 2;
        static bool mExpanded = true;
        LensSettings def = new LensSettings(); // to access name strings
        GUIContent FocalLengthLabel = new GUIContent("Focal Length", "The length of the lens (in mm)");

        public override void OnGUI(Rect rect, SerializedProperty property, GUIContent label)
        {
            float height = EditorGUIUtility.singleLineHeight;
            rect.height = height;
            mExpanded = EditorGUI.Foldout(rect, mExpanded, label);
            if (mExpanded)
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

            CinemachineLensPresets presets = CinemachineLensPresets.Instance;
            int preset = presets.GetMatchingPreset(FOVProperty.floatValue);
            rect.x -= ExtraSpaceHackWTF(); rect.width += ExtraSpaceHackWTF();
            int selection = EditorGUI.Popup(rect, GUIContent.none, preset, m_PresetOptions);
            if (selection == m_PresetOptions.Length-1)
                Selection.activeObject = presets;
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

            CinemachineLensPresets presets = CinemachineLensPresets.Instance;
            int preset = presets.GetMatchingPhysicalPreset(VerticalFOVToFocalLength(FOVProperty.floatValue));
            rect.x -= ExtraSpaceHackWTF(); rect.width += ExtraSpaceHackWTF();
            int selection = EditorGUI.Popup(rect, GUIContent.none, preset, m_PhysicalPresetOptions);
            if (selection == m_PhysicalPresetOptions.Length-1)
                Selection.activeObject = presets;
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
            IsOrtho = typeof(LensSettings).AccessInternalProperty<bool>(lens, "Orthographic");
            IsPhysical = typeof(LensSettings).AccessInternalProperty<bool>(lens, "IsPhysicalCamera");
            SensorSize = typeof(LensSettings).AccessInternalProperty<Vector2>(lens, "SensorSize");

            CinemachineLensPresets presets = CinemachineLensPresets.Instance;
            List<GUIContent> options = new List<GUIContent>();
            for (int i = 0; i < presets.m_Presets.Length; ++i)
                options.Add(new GUIContent(presets.m_Presets[i].m_Name));
            options.Add(new GUIContent("Edit Presets..."));
            m_PresetOptions = options.ToArray();

            options.Clear();
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
            if (mExpanded)
                height *= IsPhysical ? 6 : 5;
            return height;
        }
    }
}
