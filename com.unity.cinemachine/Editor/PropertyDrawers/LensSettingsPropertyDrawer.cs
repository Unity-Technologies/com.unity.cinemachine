using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;
using UnityEngine.UIElements;
using UnityEditor.UIElements;

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


namespace Cinemachine.Editor
{
    [CustomPropertyDrawer(typeof(LensSettings))]
    internal sealed class LensSettingsPropertyDrawer : PropertyDrawer
    {
        static LensSettings m_LensSettingsDef = new LensSettings(); // to access name strings

        static bool IsOrtho(SerializedProperty property) => AccessProperty<bool>(
            typeof(LensSettings), SerializedPropertyHelper.GetPropertyValue(property), "Orthographic");

        static bool IsPhysical(SerializedProperty property) => AccessProperty<bool>(
            typeof(LensSettings), SerializedPropertyHelper.GetPropertyValue(property), "IsPhysicalCamera");

        static Vector2 SensorSize(SerializedProperty property) => AccessProperty<Vector2>(
            typeof(LensSettings), SerializedPropertyHelper.GetPropertyValue(property), "SensorSize");

        static bool UseHorizontalFOV(SerializedProperty property) => AccessProperty<bool>(
            typeof(LensSettings), SerializedPropertyHelper.GetPropertyValue(property), "UseHorizontalFOV");

        static bool s_AdvancedExpanded;

    #if CINEMACHINE_HDRP
        static bool s_PhysicalExapnded;
    #endif

        static List<string> m_PresetOptions;
        static List<string> m_PhysicalPresetOptions;
        static string m_EditPresetsLabel = "Edit Presets...";

        void InitPresetOptions()
        {
            if (m_PresetOptions == null)
                m_PresetOptions = new List<string>();
            m_PresetOptions.Clear();
            m_PresetOptions.Add("");
            var presets = CinemachineLensPresets.InstanceIfExists;
            for (int i = 0; presets != null && i < presets.m_Presets.Length; ++i)
                m_PresetOptions.Add(presets.m_Presets[i].m_Name);
            m_PresetOptions.Add(m_EditPresetsLabel);

            if (m_PhysicalPresetOptions == null)
                m_PhysicalPresetOptions = new List<string>();
            m_PhysicalPresetOptions.Clear();
            m_PhysicalPresetOptions.Add("");
            for (int i = 0; presets != null && i < presets.m_PhysicalPresets.Length; ++i)
                m_PhysicalPresetOptions.Add(presets.m_PhysicalPresets[i].m_Name);
            m_PhysicalPresetOptions.Add(m_EditPresetsLabel);
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
        
        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            InitPresetOptions();

            var ux = new VisualElement();

            // When foldout is closed, we display the FOV on the same line, for convenience
            var foldout = new Foldout { text = property.displayName, tooltip = property.tooltip, value = property.isExpanded };
            foldout.RegisterValueChangedCallback((evt) => 
            {
                property.isExpanded = evt.newValue;
                property.serializedObject.ApplyModifiedProperties();
                evt.StopPropagation();
            });

            var fovControl = new FovPropertyControl(property, true) { style = { flexGrow = 1 }};
            ux.Add(new InspectorUtility.FoldoutWithOverlay(
                foldout, fovControl, fovControl.ShortLabel) { style = { flexGrow = 1 }});

            // Populate the foldout
            var fovControl2 = new FovPropertyControl(property, false) { style = { flexGrow = 1 }};
            foldout.Add(fovControl2);

            var nearClip = property.FindPropertyRelative(() => m_LensSettingsDef.NearClipPlane);
            var nearClipField = new PropertyField(nearClip);
            foldout.Add(nearClipField);
            nearClipField.RegisterValueChangeCallback((evt) =>
            {
                if (!IsOrtho(property) && nearClip.floatValue < 0.01f)
                {
                    nearClip.floatValue = 0.01f;
                    property.serializedObject.ApplyModifiedPropertiesWithoutUndo();
                }
                evt.StopPropagation();
            });
            foldout.Add(new PropertyField(property.FindPropertyRelative(() => m_LensSettingsDef.FarClipPlane)));

            var modeOverrideProperty = property.FindPropertyRelative(() => m_LensSettingsDef.ModeOverride);
            var physical = new VisualElement();
            foldout.Add(physical);

#if CINEMACHINE_HDRP
            var physicalFoldout = new Foldout() 
                { text = "Physical Properties", tooltip = "Physical properties of the lens", value = s_PhysicalExapnded };
            physical.Add(physicalFoldout);
            physicalFoldout.RegisterValueChangedCallback((evt) => 
            {
                s_PhysicalExapnded = evt.newValue;
                evt.StopPropagation();
            });

            physicalFoldout.Add(new PropertyField(property.FindPropertyRelative(() => m_LensSettingsDef.Aperture)));
            physicalFoldout.Add(new PropertyField(property.FindPropertyRelative(() => m_LensSettingsDef.Iso)));
            physicalFoldout.Add(new PropertyField(property.FindPropertyRelative(() => m_LensSettingsDef.ShutterSpeed)));
            physicalFoldout.Add(new PropertyField(property.FindPropertyRelative(() => m_LensSettingsDef.BladeCount)));

            var curveProp = property.FindPropertyRelative(() => m_LensSettingsDef.Curvature); 
            var curveMin = new FloatField { value = curveProp.vector2Value.x, style = { flexGrow = 1, flexBasis = 0 }};
            curveMin.AddToClassList(InspectorUtility.kAlignFieldClass);
            curveMin.TrackPropertyValue(curveProp, (evt) => curveMin.value = evt.vector2Value.x);
            curveMin.RegisterValueChangedCallback((evt) =>
            {
                var v = curveProp.vector2Value;
                v.x = Mathf.Max(evt.newValue, HDPhysicalCamera.kMinAperture);
                curveProp.vector2Value = v;
                curveProp.serializedObject.ApplyModifiedProperties();
            });

            var slider = new MinMaxSlider()
            { 
                lowLimit = HDPhysicalCamera.kMinAperture, highLimit = HDPhysicalCamera.kMaxAperture,
                style = { flexGrow = 3, flexBasis = 0, paddingLeft = 5, paddingRight = 5 }
            };
            slider.BindProperty(curveProp);

            var curveMax = new FloatField() { value = curveProp.vector2Value.y, style = { flexGrow = 1, flexBasis = 0 } };
            curveMax.TrackPropertyValue(curveProp, (evt) => curveMax.value = evt.vector2Value.y);
            curveMax.RegisterValueChangedCallback((evt) =>
            {
                var v = curveProp.vector2Value;
                v.y = Mathf.Min(evt.newValue, HDPhysicalCamera.kMaxAperture);
                curveProp.vector2Value = v;
                curveProp.serializedObject.ApplyModifiedProperties();
            });

            var curveContainer = new InspectorUtility.LeftRightContainer { style = { flexGrow = 1 }};
            physicalFoldout.Add(curveContainer);
            curveContainer.Left.Add(new Label 
                { text = curveProp.displayName, tooltip = curveProp.tooltip, style = { alignSelf = Align.Center }});
            curveContainer.Right.Add(curveMin);
            curveContainer.Right.Add(slider);
            curveContainer.Right.Add(curveMax);

            physicalFoldout.Add(new PropertyField(property.FindPropertyRelative(() => m_LensSettingsDef.BarrelClipping)));
            physicalFoldout.Add(new PropertyField(property.FindPropertyRelative(() => m_LensSettingsDef.Anamorphism)));
#endif

            physical.Add(new PropertyField(property.FindPropertyRelative(() => m_LensSettingsDef.LensShift)));
            var ssProp = property.FindPropertyRelative("m_SensorSize");
            var sensorSizeField = new PropertyField(ssProp);
            physical.Add(sensorSizeField);
            sensorSizeField.RegisterValueChangeCallback((evt) =>
            {
                var v = ssProp.vector2Value;
                v.x = Mathf.Max(v.x, 0.1f);
                v.y = Mathf.Max(v.y, 0.1f);
                ssProp.vector2Value = v;
                property.serializedObject.ApplyModifiedPropertiesWithoutUndo();
                evt.StopPropagation();
            });
            physical.Add(new PropertyField(property.FindPropertyRelative(() => m_LensSettingsDef.GateFit)));

            foldout.Add(new PropertyField(property.FindPropertyRelative(() => m_LensSettingsDef.Dutch)));
            var advanced = new Foldout() { text = "Advanced", value = s_AdvancedExpanded };
            foldout.Add(advanced);
            advanced.RegisterValueChangedCallback((evt) => 
            {
                s_AdvancedExpanded = evt.newValue;
                evt.StopPropagation();
            });

            advanced.Add(new HelpBox("Setting a mode override here implies changes to the Camera component when "
                + "Cinemachine activates this CM Camera, and the changes will remain after the CM "
                + "Camera deactivation. If you set a mode override in any CM Camera, you should set "
                + "one in all CM Cameras.", HelpBoxMessageType.Info));
            advanced.Add(new PropertyField(modeOverrideProperty));

            // GML: This is rather evil.  Is there a better (event-driven) way?
            ux.schedule.Execute(() => 
            {
                physical.SetVisible(IsPhysical(property));
                sensorSizeField.SetVisible(modeOverrideProperty.intValue != (int)LensSettings.OverrideModes.None);
                fovControl.Update(true);
                fovControl2.Update(false);
            }).Every(250);

            return ux;
        }


        /// <summary>
        /// Make the complicated FOV widget which works in 3 modes, with preset popups, 
        /// and optional weird small label display
        /// </summary>
        class FovPropertyControl : VisualElement
        {
            public VisualElement OrthoControl;
            public VisualElement FovControl;
            public VisualElement FocalLengthControl;
            public Label ShortLabel;

            SerializedProperty m_Property;
            FieldMouseDragger<float> m_DraggerOrtho;
            FieldMouseDragger<float> m_DraggerFov;
            FieldMouseDragger<float> m_DraggerFocal;

            public FovPropertyControl(SerializedProperty property, bool hideLabel) 
            {
                m_Property = property;
                ShortLabel = new Label("(fov)") { style = { alignSelf = Align.Center }};

                var orthoProp = property.FindPropertyRelative(() => m_LensSettingsDef.OrthographicSize);
                OrthoControl = new PropertyField(orthoProp, hideLabel ? "" : orthoProp.displayName)
                    { tooltip = orthoProp.tooltip, style = {flexGrow = 1}};
                Add(OrthoControl);

                FovControl = CreateFovControl(property, hideLabel);
                Add(FovControl);

                FocalLengthControl = CreateFocalLengthControl(property, hideLabel);
                Add(FocalLengthControl);
            }

            VisualElement CreateFovControl(SerializedProperty property, bool hideLabel)
            {
                var fovControl = new VisualElement { style = { flexDirection = FlexDirection.Row, flexGrow = 1 }};
                var fovProperty = property.FindPropertyRelative(() => m_LensSettingsDef.FieldOfView);
                var fovField = new FloatField 
                { 
                    label = hideLabel ? "" : fovProperty.displayName, 
                    tooltip = fovProperty.tooltip, 
                    style = { flexGrow = 1 },
                    value = GetDisplayFOV(fovProperty.floatValue)
                };
                fovField.AddToClassList(InspectorUtility.kAlignFieldClass);

                fovField.RegisterValueChangedCallback((evt) =>
                {
                    var newValue = Mathf.Clamp(evt.newValue, 1, 179);
                    fovField.SetValueWithoutNotify(newValue);

                    // Convert from display units
                    if (UseHorizontalFOV(property))
                    {
                        var sensorSize = SensorSize(property);
                        float aspect = sensorSize.x / sensorSize.y;
                        newValue = Camera.HorizontalToVerticalFieldOfView(newValue, aspect);
                    }
                    // Push to property
                    if (!Mathf.Approximately(fovProperty.floatValue, newValue))
                    {
                        fovProperty.floatValue = newValue;
                        fovProperty.serializedObject.ApplyModifiedProperties();
                    }
                });

                // Presets menu
                var initialIndex = GetMatchingPreset(fovProperty);
                var fovPresets = new PopupField<string>(
                    m_PresetOptions, initialIndex < 0 ? "" : CinemachineLensPresets.InstanceIfExists.m_Presets[initialIndex].m_Name) 
                    { tooltip = "Custom Lens Presets", style = 
                { 
                    height = InspectorUtility.SingleLineHeight,
                    alignSelf = Align.Center, flexGrow = 2, flexBasis = 0,
                    marginLeft = 5, paddingRight = 0, borderRightWidth = 0, marginRight = 0
                }};
                fovPresets.RegisterValueChangedCallback((evt) => 
                {
                    // Edit the presets assets if desired
                    if (evt.newValue == m_EditPresetsLabel)
                        Selection.activeObject = CinemachineLensPresets.Instance;
                    else
                    {
                        // Apply the preset
                        var index = CinemachineLensPresets.Instance.GetPresetIndex(evt.newValue);
                        if (index >= 0)
                        {
                            fovProperty.floatValue = CinemachineLensPresets.Instance.m_Presets[index].m_FieldOfView;
                            fovProperty.serializedObject.ApplyModifiedProperties();
                        }
                    }
                });

                // Convert to display units, and track the presets
                fovField.TrackPropertyValue(fovProperty, (prop) => 
                {
                    // Select the matching preset, if any
                    int index = GetMatchingPreset(fovProperty);
                    fovPresets.SetValueWithoutNotify(
                        index >= 0 ? CinemachineLensPresets.Instance.m_Presets[index].m_Name : "");

                    // Convert to display FOV units
                    fovField.SetValueWithoutNotify(GetDisplayFOV(prop.floatValue));
                });

                fovControl.Add(fovField);
                fovControl.Add(fovPresets);
                return fovControl;

                int GetMatchingPreset(SerializedProperty p)
                {
                    var presets = CinemachineLensPresets.InstanceIfExists;
                    return (presets == null) ? -1 : presets.GetMatchingPreset(p.floatValue);
                }
            }

            VisualElement CreateFocalLengthControl(SerializedProperty property, bool hideLabel)
            {
                FocalLengthControl = new VisualElement { style = { flexDirection = FlexDirection.Row, flexGrow = 1 }};
                var focalProperty = property.FindPropertyRelative(() => m_LensSettingsDef.FieldOfView);
                var focalField = new FloatField 
                { 
                    label = hideLabel ? "" : focalProperty.displayName, 
                    tooltip = focalProperty.tooltip, 
                    style = { flexGrow = 1 },
                    value = GetDisplayFocalLength(focalProperty.floatValue)
                };
                focalField.AddToClassList(InspectorUtility.kAlignFieldClass);
                focalField.RegisterValueChangedCallback((evt) =>
                {
                    // Convert and clamp
                    var sensorHeight = SensorSize(property).y;
                    var vfov =Camera.FocalLengthToFieldOfView(evt.newValue, sensorHeight);
                    if (vfov < 1 || vfov > 179)
                    {
                        vfov = Mathf.Clamp(vfov, 1, 179);
                        focalField.SetValueWithoutNotify(Camera.FieldOfViewToFocalLength(vfov, sensorHeight));
                    }
                    // Push to property
                    if (!Mathf.Approximately(focalProperty.floatValue, vfov))
                    {
                        focalProperty.floatValue = vfov;
                        focalProperty.serializedObject.ApplyModifiedProperties();
                    }
                });

                // Presets menu
                var initialIndex = GetMatchingPhysicalPreset(focalProperty);
                var focalPresets = new PopupField<string>(m_PhysicalPresetOptions, 
                    initialIndex < 0 ? "" : CinemachineLensPresets.InstanceIfExists.m_PhysicalPresets[initialIndex].m_Name) 
                    { tooltip = "Custom Lens Presets", style = 
                { 
                    height = InspectorUtility.SingleLineHeight,
                    alignSelf = Align.Center, flexGrow = 2, flexBasis = 0,
                    marginLeft = 5, paddingRight = 0, borderRightWidth = 0, marginRight = 0
                }};
                focalPresets.RegisterValueChangedCallback((evt) => 
                {
                    // Edit the presets assets if desired
                    if (evt.newValue == m_EditPresetsLabel)
                        Selection.activeObject = CinemachineLensPresets.Instance;
                    else 
                    {
                        // Apply the preset
                        var index = CinemachineLensPresets.Instance.GetPhysicalPresetIndex(evt.newValue);
                        if (index >= 0)
                        {
                            var v = CinemachineLensPresets.Instance.m_PhysicalPresets[index];
                            focalProperty.floatValue = Camera.FocalLengthToFieldOfView(v.m_FocalLength, SensorSize(property).y);
#if CINEMACHINE_HDRP
                            property.FindPropertyRelative(() => m_LensSettingsDef.Aperture).floatValue = v.Aperture;
                            property.FindPropertyRelative(() => m_LensSettingsDef.Iso).intValue = v.Iso;
                            property.FindPropertyRelative(() => m_LensSettingsDef.ShutterSpeed).floatValue = v.ShutterSpeed;
                            property.FindPropertyRelative(() => m_LensSettingsDef.BladeCount).intValue = v.BladeCount;
                            property.FindPropertyRelative(() => m_LensSettingsDef.Curvature).vector2Value = v.Curvature;
                            property.FindPropertyRelative(() => m_LensSettingsDef.BarrelClipping).floatValue = v.BarrelClipping;
                            property.FindPropertyRelative(() => m_LensSettingsDef.Anamorphism).floatValue = v.Anamorphism;
                            property.FindPropertyRelative(() => m_LensSettingsDef.LensShift).vector2Value = v.LensShift;
#endif
                            focalProperty.serializedObject.ApplyModifiedProperties();
                        }
                    }
                });

                // Convert to display units, and track the presets
                focalField.TrackPropertyValue(focalProperty, (prop) => 
                {
                    // Select the matching preset, if any
                    int index = GetMatchingPhysicalPreset(prop);
                    focalPresets.SetValueWithoutNotify(
                        index >= 0 ? CinemachineLensPresets.Instance.m_PhysicalPresets[index].m_Name : "");

                    // Convert to display FOV units
                    focalField.SetValueWithoutNotify(GetDisplayFocalLength(prop.floatValue));
                });
                
                int GetMatchingPhysicalPreset(SerializedProperty p)
                {
                    var presets = CinemachineLensPresets.InstanceIfExists;
                    if (presets == null)
                        return -1;
#if CINEMACHINE_HDRP
                    var focalLength = Camera.FieldOfViewToFocalLength(p.floatValue, SensorSize(p).y);
                    var aperture = property.FindPropertyRelative(() => m_LensSettingsDef.Aperture).floatValue;
                    var iso = property.FindPropertyRelative(() => m_LensSettingsDef.Iso).intValue;
                    var shutterSpeed = property.FindPropertyRelative(() => m_LensSettingsDef.ShutterSpeed).floatValue;
                    var bladeCount = property.FindPropertyRelative(() => m_LensSettingsDef.BladeCount).intValue;
                    var curvature = property.FindPropertyRelative(() => m_LensSettingsDef.Curvature).vector2Value;
                    var barrelClipping = property.FindPropertyRelative(() => m_LensSettingsDef.BarrelClipping).floatValue;
                    var anamprphism = property.FindPropertyRelative(() => m_LensSettingsDef.Anamorphism).floatValue;
                    var lensShift = property.FindPropertyRelative(() => m_LensSettingsDef.LensShift).vector2Value;

                    return presets.GetMatchingPhysicalPreset(
                        focalLength, iso, shutterSpeed, aperture, bladeCount,
                        curvature, barrelClipping, anamprphism, lensShift);
#else
                    return presets.GetMatchingPhysicalPreset(
                        Camera.FieldOfViewToFocalLength(p.floatValue, SensorSize(p).y));
#endif
                }
                
                FocalLengthControl.Add(focalField);
                FocalLengthControl.Add(focalPresets);
                return FocalLengthControl;
            }

            // Convert to display FOV units
            float GetDisplayFOV(float vfov)
            {
                if (UseHorizontalFOV(m_Property))
                {
                    var sensorSize = SensorSize(m_Property);
                    float aspect = sensorSize.x / sensorSize.y;
                    vfov = Camera.VerticalToHorizontalFieldOfView(vfov, aspect);
                }
                return vfov;
            }

            // Convert to display FOV units
            float GetDisplayFocalLength(float vfov) => Camera.FieldOfViewToFocalLength(vfov, SensorSize(m_Property).y);

            public void Update(bool shortLabel)
            {
                if (IsOrtho(m_Property))
                {
                    OrthoControl.SetVisible(true);
                    FovControl.SetVisible(false);
                    FocalLengthControl.SetVisible(false);
                    if (shortLabel && ShortLabel.text != "Ortho")
                    {
                        ShortLabel.text = "Ortho";
                        if (m_DraggerOrtho == null)
                            m_DraggerOrtho = new FieldMouseDragger<float>(OrthoControl.Q<FloatField>());
                        m_DraggerOrtho.SetDragZone(ShortLabel);
                        if (m_DraggerFov != null)
                            m_DraggerFov.SetDragZone(null);
                        if (m_DraggerFocal != null)
                            m_DraggerFocal.SetDragZone(null);
                    }
                }
                else if (IsPhysical(m_Property))
                {
                    OrthoControl.SetVisible(false);
                    FovControl.SetVisible(false);
                    FocalLengthControl.SetVisible(true);
                    if (!shortLabel)
                    {
                        var e = FocalLengthControl.Q<Label>();
                        if (e != null)
                            e.text = "Focal Length";
                    }
                    else if (ShortLabel.text != "Focal")
                    {
                        ShortLabel.text = "Focal";
                        if (m_DraggerOrtho != null)
                            m_DraggerOrtho.SetDragZone(null);
                        if (m_DraggerFov != null)
                            m_DraggerFov.SetDragZone(null);
                        if (m_DraggerFocal == null)
                            m_DraggerFocal = new FieldMouseDragger<float>(FocalLengthControl.Q<FloatField>());
                        m_DraggerFocal.SetDragZone(ShortLabel);
                    }
                }
                else
                {
                    OrthoControl.SetVisible(false);
                    FovControl.SetVisible(true);
                    FocalLengthControl.SetVisible(false);
                    var isHov = UseHorizontalFOV(m_Property);
                    if (!shortLabel)
                    {
                        var e = FovControl.Q<Label>();
                        if (e != null)
                            e.text = UseHorizontalFOV(m_Property) ? "Horizontal FOV" : "Vertical FOV";
                    }
                    else
                    {
                        var text = isHov ? "HFOV" : "FOV";
                        if (ShortLabel.text != text)
                        {
                            ShortLabel.text = text;
                            if (m_DraggerOrtho != null)
                                m_DraggerOrtho.SetDragZone(null);
                            if (m_DraggerFov == null)
                                m_DraggerFov = new FieldMouseDragger<float>(FovControl.Q<FloatField>());
                            m_DraggerFov.SetDragZone(ShortLabel);
                            if (m_DraggerFocal != null)
                                m_DraggerFocal.SetDragZone(null);
                        }
                    }
                }
            }
        }
    }
}
