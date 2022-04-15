using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;

#if CINEMACHINE_HDRP || CINEMACHINE_LWRP_7_3_1
    #if CINEMACHINE_HDRP_7_3_1
        using UnityEngine.Rendering.HighDefinition;
    #else
        #if CINEMACHINE_LWRP_7_3_1
            using UnityEngine.Rendering.Universal;
        #else
            using UnityEngine.Experimental.Rendering.HDPipeline;
        #endif
    #endif
#endif

#if UNITY_2019_1_OR_NEWER
    using CameraExtensions = UnityEngine.Camera;
#else
    // Needed only for Unity pre-2019.1 because Camera doesn't have these methods
    static class CameraExtensions
    {
        public static float HorizontalToVerticalFieldOfView(float f, float aspect)
        {
            return Mathf.Rad2Deg * 2 * Mathf.Atan(Mathf.Tan(f * Mathf.Deg2Rad * 0.5f) / aspect);
        }

        public static float VerticalToHorizontalFieldOfView(float f, float aspect)
        {
            return Mathf.Rad2Deg * 2 * Mathf.Atan(Mathf.Tan(f * Mathf.Deg2Rad * 0.5f) * aspect);
        }

        public static float FieldOfViewToFocalLength(float fov, float sensorHeight)
        {
            return sensorHeight * 0.5f / Mathf.Tan(Mathf.Deg2Rad * fov * 0.5f);
        }

        public static float FocalLengthToFieldOfView(float focalLength, float sensorHeight)
        {
            if (focalLength < UnityVectorExtensions.Epsilon)
                return 180f;
            return Mathf.Rad2Deg * 2.0f * Mathf.Atan(sensorHeight * 0.5f / focalLength);
        }
    }
#endif

namespace Cinemachine.Editor
{
    // GML TODO: figure out how to do this weird stuff with UI Elements
    [CustomPropertyDrawer(typeof(LensSettings))]
    internal sealed class LensSettingsPropertyDrawer : PropertyDrawer
    {
        LensSettings m_LensSettingsDef = new LensSettings(); // to access name strings

        static readonly GUIContent EditPresetsLabel = new GUIContent("Edit Presets...");
        static readonly GUIContent LensLabel = new GUIContent("Lens", "Lens settings to apply to the camera");
        static readonly GUIContent HFOVLabel = new GUIContent("Horizontal FOV", "Horizontal Field of View");
        static readonly GUIContent VFOVLabel = new GUIContent("Vertical FOV", "Vertical Field of View");
        static readonly GUIContent FocalLengthLabel = new GUIContent("Focal Length", "The length of the lens (in mm)");
        static readonly GUIContent OrthoSizeLabel = new GUIContent("Ortho Size", "When using an orthographic camera, "
            + "this defines the half-height, in world coordinates, of the camera view.");
        static readonly GUIContent SensorSizeLabel = new GUIContent("Sensor Size", 
            "Actual size of the image sensor (in mm), used to "
            + "convert between focal length and field of vue.");
        static readonly GUIContent AdvancedLabel = new GUIContent("Advanced");

        bool IsOrtho;
        bool IsPhysical;
        Vector2 SensorSize;
        bool UseHorizontalFOV;

        static bool s_AdvancedExpanded;

    #if CINEMACHINE_HDRP
        static readonly GUIContent PhysicalPropertiesLabel = new GUIContent("Physical Properties", "Physical properties of the lens");
        static bool m_PhysicalExapnded;
    #endif

        GUIContent[] m_PresetOptions;
        GUIContent[] m_PhysicalPresetOptions;

        const float vSpace= 2;
        const float hSpace = 2;

        void InitPresetOptions()
        {
            if (m_PresetOptions != null)
                return;

            var options = new List<GUIContent>();
            CinemachineLensPresets presets = CinemachineLensPresets.InstanceIfExists;
            for (int i = 0; presets != null && i < presets.m_Presets.Length; ++i)
                options.Add(new GUIContent(presets.m_Presets[i].m_Name));
            options.Add(EditPresetsLabel);
            m_PresetOptions = options.ToArray();

            options.Clear();
            for (int i = 0; presets != null && i < presets.m_PhysicalPresets.Length; ++i)
                options.Add(new GUIContent(presets.m_PhysicalPresets[i].m_Name));
            options.Add(EditPresetsLabel);
            m_PhysicalPresetOptions = options.ToArray();
        }

        void SnapshotCameraShadowValues(SerializedProperty property)
        {
            // Assume lens is up-to-date
            UseHorizontalFOV = false;
            var lensObject = SerializedPropertyHelper.GetPropertyValue(property);
            UseHorizontalFOV = AccessProperty<bool>(typeof(LensSettings), lensObject, "UseHorizontalFOV");
            IsOrtho = AccessProperty<bool>(typeof(LensSettings), lensObject, "Orthographic");
            IsPhysical = AccessProperty<bool>(typeof(LensSettings), lensObject, "IsPhysicalCamera");
            SensorSize = AccessProperty<Vector2>(typeof(LensSettings), lensObject, "SensorSize");
        }

        static T AccessProperty<T>(Type type, object obj, string memberName)
        {
            if (string.IsNullOrEmpty(memberName) || (type == null))
                return default(T);

            System.Reflection.BindingFlags bindingFlags 
                = System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic;
            if (obj != null)
                bindingFlags |= System.Reflection.BindingFlags.Instance;
            else
                bindingFlags |= System.Reflection.BindingFlags.Static;

            System.Reflection.PropertyInfo pi = type.GetProperty(memberName, bindingFlags);
            if ((pi != null) && (pi.PropertyType == typeof(T)))
                return (T)pi.GetValue(obj, null);
            else
                return default(T);
        }
        
        static float ExtraSpaceHackWTF() { return EditorGUI.indentLevel * (EditorGUIUtility.singleLineHeight - 3); }

        GUIContent GetFOVLabel()
        {
            if (IsOrtho) return OrthoSizeLabel;
            if (IsPhysical) return FocalLengthLabel;
            return UseHorizontalFOV ? HFOVLabel : VFOVLabel;
        }

        void DrawFOVControl(Rect rect, SerializedProperty property, GUIContent label)
        {
            if (IsOrtho)
                EditorGUI.PropertyField(
                    rect, property.FindPropertyRelative(() => m_LensSettingsDef.OrthographicSize), label);
            else if (IsPhysical)
                DrawFocalLengthControl(rect, property, label);
            else
            {
                var FOVProperty = property.FindPropertyRelative(() => m_LensSettingsDef.FieldOfView);
                float aspect = SensorSize.x / SensorSize.y;

                float dropdownWidth = (rect.width - EditorGUIUtility.labelWidth) / 4;
                rect.width -= dropdownWidth + hSpace;

                float f = FOVProperty.floatValue;
                if (UseHorizontalFOV)
                    f = CameraExtensions.VerticalToHorizontalFieldOfView(f, aspect);
                EditorGUI.BeginProperty(rect, label, FOVProperty);
                f = EditorGUI.FloatField(rect, label, f);
                if (UseHorizontalFOV)
                    f = CameraExtensions.HorizontalToVerticalFieldOfView(Mathf.Clamp(f, 1, 179), aspect);
                if (!Mathf.Approximately(FOVProperty.floatValue, f))
                    FOVProperty.floatValue = Mathf.Clamp(f, 1, 179);
                EditorGUI.EndProperty();
                rect.x += rect.width + hSpace; rect.width = dropdownWidth;

                CinemachineLensPresets presets = CinemachineLensPresets.InstanceIfExists;
                int preset = (presets == null) ? -1 : presets.GetMatchingPreset(FOVProperty.floatValue);
                rect.x -= ExtraSpaceHackWTF(); rect.width += ExtraSpaceHackWTF();
                int selection = EditorGUI.Popup(rect, GUIContent.none, preset, m_PresetOptions);
                if (selection == m_PresetOptions.Length-1 && CinemachineLensPresets.Instance != null)
                    Selection.activeObject = presets = CinemachineLensPresets.Instance;
                else if (selection >= 0 && selection < m_PresetOptions.Length-1)
                {
                    var vfov = presets.m_Presets[selection].m_FieldOfView;
                    FOVProperty.floatValue = vfov;
                    property.serializedObject.ApplyModifiedProperties();
                }
            }
        }

        void DrawFocalLengthControl(Rect rect, SerializedProperty property, GUIContent label)
        {
            const float hSpace = 2;
            var FOVProperty = property.FindPropertyRelative(() => m_LensSettingsDef.FieldOfView);
            float dropdownWidth = (rect.width - EditorGUIUtility.labelWidth) / 4;
            rect.width -= dropdownWidth + hSpace;

            float f = CameraExtensions.FieldOfViewToFocalLength(FOVProperty.floatValue, SensorSize.y);
            EditorGUI.BeginProperty(rect, label, FOVProperty);
            f = EditorGUI.FloatField(rect, label, f);
            f = CameraExtensions.FocalLengthToFieldOfView(Mathf.Max(f, 0.0001f), SensorSize.y);
            if (!Mathf.Approximately(FOVProperty.floatValue, f))
                FOVProperty.floatValue = Mathf.Clamp(f, 1, 179);
            EditorGUI.EndProperty();

            rect.x += rect.width + hSpace; rect.width = dropdownWidth;

#if CINEMACHINE_HDRP
            CinemachineLensPresets presets = CinemachineLensPresets.InstanceIfExists;
            int preset = -1;
            if (presets != null)
            {
                var focalLength = CameraExtensions.FieldOfViewToFocalLength(FOVProperty.floatValue, SensorSize.y);
                var aperture = property.FindPropertyRelative(() => m_LensSettingsDef.Aperture).floatValue;
                var iso = property.FindPropertyRelative(() => m_LensSettingsDef.Iso).intValue;
                var shutterSpeed = property.FindPropertyRelative(() => m_LensSettingsDef.ShutterSpeed).floatValue;
                var bladeCount = property.FindPropertyRelative(() => m_LensSettingsDef.BladeCount).intValue;
                var curvature = property.FindPropertyRelative(() => m_LensSettingsDef.Curvature).vector2Value;
                var barrelClipping = property.FindPropertyRelative(() => m_LensSettingsDef.BarrelClipping).floatValue;
                var anamprphism = property.FindPropertyRelative(() => m_LensSettingsDef.Anamorphism).floatValue;
                var lensShift = property.FindPropertyRelative(() => m_LensSettingsDef.LensShift).vector2Value;

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
                FOVProperty.floatValue = CameraExtensions.FocalLengthToFieldOfView(v.m_FocalLength, SensorSize.y);
                property.FindPropertyRelative(() => m_LensSettingsDef.Aperture).floatValue = v.Aperture;
                property.FindPropertyRelative(() => m_LensSettingsDef.Iso).intValue = v.Iso;
                property.FindPropertyRelative(() => m_LensSettingsDef.ShutterSpeed).floatValue = v.ShutterSpeed;
                property.FindPropertyRelative(() => m_LensSettingsDef.BladeCount).intValue = v.BladeCount;
                property.FindPropertyRelative(() => m_LensSettingsDef.Curvature).vector2Value = v.Curvature;
                property.FindPropertyRelative(() => m_LensSettingsDef.BarrelClipping).floatValue = v.BarrelClipping;
                property.FindPropertyRelative(() => m_LensSettingsDef.Anamorphism).floatValue = v.Anamorphism;
                property.FindPropertyRelative(() => m_LensSettingsDef.LensShift).vector2Value = v.LensShift;
                property.serializedObject.ApplyModifiedProperties();
            }
#else
            CinemachineLensPresets presets = CinemachineLensPresets.InstanceIfExists;
            int preset = (presets == null) ? -1 : presets.GetMatchingPhysicalPreset(
                CameraExtensions.FieldOfViewToFocalLength(FOVProperty.floatValue, SensorSize.y));
            rect.x -= ExtraSpaceHackWTF(); rect.width += ExtraSpaceHackWTF();
            int selection = EditorGUI.Popup(rect, GUIContent.none, preset, m_PhysicalPresetOptions);
            if (selection == m_PhysicalPresetOptions.Length-1 && CinemachineLensPresets.Instance != null)
                Selection.activeObject = presets = CinemachineLensPresets.Instance;
            else if (selection >= 0 && selection < m_PhysicalPresetOptions.Length-1)
            {
                FOVProperty.floatValue = CameraExtensions.FocalLengthToFieldOfView(
                    presets.m_PhysicalPresets[selection].m_FocalLength, SensorSize.y);
                property.serializedObject.ApplyModifiedProperties();
            }
#endif
        }

        public override void OnGUI(Rect rect, SerializedProperty property, GUIContent label)
        {
            InitPresetOptions();

            rect.height = EditorGUIUtility.singleLineHeight; // draw one line at a time

            var fovLabel = GetFOVLabel();
            var fovLabelWidth = GUI.skin.label.CalcSize(fovLabel).x;

            property.isExpanded = EditorGUI.Foldout(
                new Rect(rect.x, rect.y, EditorGUIUtility.labelWidth - fovLabelWidth, rect.height),
                property.isExpanded, label, true);

            if (!property.isExpanded)
            {
                // Put the FOV on the same line
                var oldIndent = EditorGUI.indentLevel;
                EditorGUI.indentLevel = 0;
                var oldLabelWidth = EditorGUIUtility.labelWidth;
                var delta = EditorGUIUtility.labelWidth - fovLabelWidth;
                EditorGUIUtility.labelWidth = fovLabelWidth;
                DrawFOVControl(
                    new Rect(rect.x + delta, rect.y, rect.width - delta, rect.height), 
                    property, fovLabel);
                EditorGUIUtility.labelWidth = oldLabelWidth;
                EditorGUI.indentLevel = oldIndent;
            }
            else
            {
                ++EditorGUI.indentLevel;

                rect.y += rect.height + vSpace;
                DrawFOVControl(rect, property, fovLabel);

                rect.y += rect.height + vSpace;
                var nearClip = property.FindPropertyRelative(() => m_LensSettingsDef.NearClipPlane);
                EditorGUI.PropertyField(rect, nearClip);
                if (!IsOrtho && nearClip.floatValue < 0.01f)
                {
                    nearClip.floatValue = 0.01f;
                    property.serializedObject.ApplyModifiedPropertiesWithoutUndo();
                }
                rect.y += rect.height + vSpace;
                EditorGUI.PropertyField(rect, property.FindPropertyRelative(() => m_LensSettingsDef.FarClipPlane));

                var modeOverrideProperty = property.FindPropertyRelative(() => m_LensSettingsDef.ModeOverride);
                if (IsPhysical)
                {
#if CINEMACHINE_HDRP
                    m_PhysicalExapnded = EditorGUILayout.Foldout(m_PhysicalExapnded, PhysicalPropertiesLabel, true);
                    if (m_PhysicalExapnded)
                    {
                        ++EditorGUI.indentLevel;
                        rect.y += rect.height + vSpace;
                        EditorGUI.PropertyField(rect, property.FindPropertyRelative(() => m_LensSettingsDef.Aperture));
                        rect.y += rect.height + vSpace;
                        EditorGUI.PropertyField(rect, property.FindPropertyRelative(() => m_LensSettingsDef.Iso));
                        rect.y += rect.height + vSpace;
                        EditorGUI.PropertyField(rect, property.FindPropertyRelative(() => m_LensSettingsDef.ShutterSpeed));
                        rect.y += rect.height + vSpace;
                        EditorGUI.PropertyField(rect, property.FindPropertyRelative(() => m_LensSettingsDef.BladeCount));

                        rect.y += rect.height + vSpace;
                        var curvature = property.FindPropertyRelative(() => m_LensSettingsDef.Curvature);
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

                        rect.y += rect.height + vSpace;
                        EditorGUI.PropertyField(rect, property.FindPropertyRelative(() => m_LensSettingsDef.BarrelClipping));
                        rect.y += rect.height + vSpace;
                        EditorGUI.PropertyField(rect, property.FindPropertyRelative(() => m_LensSettingsDef.Anamorphism));

                        --EditorGUI.indentLevel;
                    }
#endif
                    rect.y += rect.height + vSpace;
                    EditorGUI.PropertyField(rect, property.FindPropertyRelative(() => m_LensSettingsDef.LensShift));
                    if (modeOverrideProperty.intValue != (int)LensSettings.OverrideModes.None)
                    {
                        rect.y += rect.height + vSpace;

                        var ssProp = property.FindPropertyRelative("m_SensorSize");
                        EditorGUI.BeginProperty(rect, SensorSizeLabel, ssProp);
                        var v = EditorGUI.Vector2Field(rect, SensorSizeLabel, ssProp.vector2Value);
                        v.x = Mathf.Max(v.x, 0.1f);
                        v.y = Mathf.Max(v.y, 0.1f);
                        ssProp.vector2Value = v;
                        EditorGUI.EndProperty();

                        rect.y += rect.height + vSpace;
                        EditorGUI.PropertyField(rect, property.FindPropertyRelative(() => m_LensSettingsDef.GateFit));
                    }
                }
                rect.y += rect.height + vSpace;
                EditorGUI.PropertyField(rect, property.FindPropertyRelative(() => m_LensSettingsDef.Dutch));
                rect.y += rect.height + vSpace;
                s_AdvancedExpanded = EditorGUI.Foldout(rect, s_AdvancedExpanded, AdvancedLabel, true);
                if (s_AdvancedExpanded)
                {
                    ++EditorGUI.indentLevel;
                    rect.y += rect.height + vSpace;
                    EditorGUI.PropertyField(rect, modeOverrideProperty);
                    --EditorGUI.indentLevel;
                }
                --EditorGUI.indentLevel;
            }
            property.serializedObject.ApplyModifiedProperties();
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            SnapshotCameraShadowValues(property);

            var lineHeight = EditorGUIUtility.singleLineHeight;
            if (!property.isExpanded)
                return lineHeight;

            int numLines = 3;
            if (IsPhysical)
            {
#if CINEMACHINE_HDRP
                if (m_PhysicalExapnded)
                    numLines += 7;
#endif
                numLines += 1;
                var modeOverrideProperty = property.FindPropertyRelative(() => m_LensSettingsDef.ModeOverride);
                if (modeOverrideProperty.intValue != (int)LensSettings.OverrideModes.None)
                    numLines += 2;
            }
            numLines += 2;
            if (s_AdvancedExpanded)
                numLines += 1;

            return lineHeight + numLines * (lineHeight + vSpace);
        }
    }
}
