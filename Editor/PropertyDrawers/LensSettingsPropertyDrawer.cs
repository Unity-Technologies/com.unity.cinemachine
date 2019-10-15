using UnityEngine;
using UnityEditor;
using System.Reflection;
using System.Collections.Generic;
using Cinemachine.Utility;
using System;

#if CINEMACHINE_HDRP || CINEMACHINE_LWRP_7_0_0
    #if CINEMACHINE_HDRP_7_0_0
    using UnityEngine.Rendering.HighDefinition;
    #else
        #if CINEMACHINE_LWRP_7_0_0
        using UnityEngine.Rendering.Universal;
        #else
        using UnityEngine.Experimental.Rendering.HDPipeline;
        #endif
    #endif
#endif


namespace Cinemachine.Editor
{
    [CustomPropertyDrawer(typeof(LensSettingsPropertyAttribute))]
    internal sealed class LensSettingsPropertyDrawer : PropertyDrawer
    {
        const int vSpace = 2;
        LensSettings def = new LensSettings(); // to access name strings
        GUIContent FocalLengthLabel = new GUIContent("Focal Length", "The length of the lens (in mm)");

#if CINEMACHINE_HDRP
        GUIContent PhysicalPropertiesLabel = new GUIContent("Physical Properties", "Physical properties of the lens");
        static bool mPhysicalExapnded;
#endif

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
#if CINEMACHINE_HDRP
                    rect.y += height + vSpace;
                    mPhysicalExapnded = EditorGUI.Foldout(
                        new Rect(rect.x, rect.y, EditorGUIUtility.labelWidth, rect.height),
                        mPhysicalExapnded, PhysicalPropertiesLabel, true);
                    if (mPhysicalExapnded)
                    {
                        ++EditorGUI.indentLevel;
                        rect.y += height + vSpace;
                        EditorGUI.PropertyField(rect, property.FindPropertyRelative(() => def.Aperture));
                        rect.y += height + vSpace;
                        EditorGUI.PropertyField(rect, property.FindPropertyRelative(() => def.Iso));
                        rect.y += height + vSpace;
                        EditorGUI.PropertyField(rect, property.FindPropertyRelative(() => def.ShutterSpeed));
                        rect.y += height + vSpace;
                        EditorGUI.PropertyField(rect, property.FindPropertyRelative(() => def.BladeCount));

                        rect.y += height + vSpace;
                        var curvature = property.FindPropertyRelative(() => def.Curvature);
                        using (var propertyScope = new EditorGUI.PropertyScope(rect, new GUIContent("Curvature"), curvature))
                        {
                            var v = curvature.vector2Value;

                            // The layout system breaks alignment when mixing inspector fields with custom layout'd
                            // fields as soon as a scrollbar is needed in the inspector, so we'll do the layout
                            // manually instead
                            const int kFloatFieldWidth = 50;
                            const int kSeparatorWidth = 5;
                            float indentOffset = EditorGUI.indentLevel * 15f;
                            var labelRect = new Rect(rect.x, rect.y, EditorGUIUtility.labelWidth - indentOffset, rect.height);
                            var floatFieldLeft = new Rect(labelRect.xMax, rect.y, kFloatFieldWidth + indentOffset, rect.height);
                            var sliderRect = new Rect(floatFieldLeft.xMax + kSeparatorWidth - indentOffset, rect.y, rect.width - labelRect.width - kFloatFieldWidth * 2 - kSeparatorWidth * 2, rect.height);
                            var floatFieldRight = new Rect(sliderRect.xMax + kSeparatorWidth - indentOffset, rect.y, kFloatFieldWidth + indentOffset, rect.height);

                            EditorGUI.PrefixLabel(labelRect, propertyScope.content);
                            v.x = EditorGUI.FloatField(floatFieldLeft, v.x);
                            EditorGUI.MinMaxSlider(sliderRect, ref v.x, ref v.y, HDPhysicalCamera.kMinAperture, HDPhysicalCamera.kMaxAperture);
                            v.y = EditorGUI.FloatField(floatFieldRight, v.y);

                            curvature.vector2Value = v;
                        }

                        rect.y += height + vSpace;
                        EditorGUI.PropertyField(rect, property.FindPropertyRelative(() => def.BarrelClipping));
                        rect.y += height + vSpace;
                        EditorGUI.PropertyField(rect, property.FindPropertyRelative(() => def.Anamorphism));
                        rect.y += height + vSpace;
                        EditorGUI.PropertyField(rect, property.FindPropertyRelative(() => def.LensShift));
                        --EditorGUI.indentLevel;
                    }
#else
                    rect.y += height + vSpace;
                    EditorGUI.PropertyField(rect, property.FindPropertyRelative(() => def.LensShift));
#endif
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

            EditorGUI.BeginProperty(rect, FocalLengthLabel, FOVProperty);
            f = EditorGUI.FloatField(rect, FocalLengthLabel, f);
            f = FocalLengthToVerticalFOV(f);
            if (!Mathf.Approximately(FOVProperty.floatValue, f))
                FOVProperty.floatValue = Mathf.Clamp(f, 1, 179);
            EditorGUI.EndProperty();

            rect.x += rect.width; rect.width = dropdownWidth;

#if CINEMACHINE_HDRP
            CinemachineLensPresets presets = CinemachineLensPresets.InstanceIfExists;
            int preset = -1;
            if (presets != null)
            {
                var focalLength = VerticalFOVToFocalLength(FOVProperty.floatValue);
                var aperture = property.FindPropertyRelative(() => def.Aperture).floatValue;
                var iso = property.FindPropertyRelative(() => def.Iso).intValue;
                var shutterSpeed = property.FindPropertyRelative(() => def.ShutterSpeed).floatValue;
                var bladeCount = property.FindPropertyRelative(() => def.BladeCount).intValue;
                var curvature = property.FindPropertyRelative(() => def.Curvature).vector2Value;
                var barrelClipping = property.FindPropertyRelative(() => def.BarrelClipping).floatValue;
                var anamprphism = property.FindPropertyRelative(() => def.Anamorphism).floatValue;
                var lensShift = property.FindPropertyRelative(() => def.LensShift).vector2Value;

                preset = presets.GetMatchingPhysicalPreset(
                    focalLength, iso, shutterSpeed, aperture, bladeCount,
                    curvature, barrelClipping, anamprphism, lensShift);
            }
            rect.x -= ExtraSpaceHackWTF(); rect.width += ExtraSpaceHackWTF();
            int selection = EditorGUI.Popup(rect, GUIContent.none, preset, m_PhysicalPresetOptions);
            if (selection == m_PhysicalPresetOptions.Length-1 && CinemachineLensPresets.Instance != null)
                Selection.activeObject = presets = CinemachineLensPresets.Instance;
            else if (selection >= 0 && selection < m_PhysicalPresetOptions.Length-1)
            {
                var v = presets.m_PhysicalPresets[selection];
                FOVProperty.floatValue = FocalLengthToVerticalFOV(v.m_FocalLength);
                property.FindPropertyRelative(() => def.Aperture).floatValue = v.Aperture;
                property.FindPropertyRelative(() => def.Iso).intValue = v.Iso;
                property.FindPropertyRelative(() => def.ShutterSpeed).floatValue = v.ShutterSpeed;
                property.FindPropertyRelative(() => def.BladeCount).intValue = v.BladeCount;
                property.FindPropertyRelative(() => def.Curvature).vector2Value = v.Curvature;
                property.FindPropertyRelative(() => def.BarrelClipping).floatValue = v.BarrelClipping;
                property.FindPropertyRelative(() => def.Anamorphism).floatValue = v.Anamorphism;
                property.FindPropertyRelative(() => def.LensShift).vector2Value = v.LensShift;
                property.serializedObject.ApplyModifiedProperties();
            }
#else
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
#endif
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
                    options.Add(new GUIContent(presets.m_PhysicalPresets[i].m_Name));
            options.Add(new GUIContent("Edit Presets..."));
            m_PhysicalPresetOptions = options.ToArray();
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            // Cache it here because it's called less often - less garbage
            CacheABunchOfStuff(property);

            float height = EditorGUIUtility.singleLineHeight + vSpace;
            if (property.isExpanded)
            {
                if (!IsPhysical)
                    height *= 5;
                else
                {
#if CINEMACHINE_HDRP
                    height *= 5 + (mPhysicalExapnded ? 9 : 1);
#else
                    height *= 5 + 1;
#endif
                }
            }
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
