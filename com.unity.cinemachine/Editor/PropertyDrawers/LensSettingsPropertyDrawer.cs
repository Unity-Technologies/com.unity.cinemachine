using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;
using UnityEngine.UIElements;
using UnityEditor.UIElements;

#if CINEMACHINE_HDRP
    using UnityEngine.Rendering.HighDefinition;
#elif CINEMACHINE_URP
    using UnityEngine.Rendering.Universal;
#endif

namespace Cinemachine.Editor
{
    [CustomPropertyDrawer(typeof(LensSettingsHideModeOverridePropertyAttribute))]
    class LensSettingsHideModeOverridePropertyDrawer : LensSettingsPropertyDrawer
    {
        public LensSettingsHideModeOverridePropertyDrawer() => HideModeOverride = true;
    }

    [CustomPropertyDrawer(typeof(LensSettings))]
    class LensSettingsPropertyDrawer : PropertyDrawer
    {
        static LensSettings m_LensSettingsDef = new LensSettings(); // to access name strings

        static bool IsOrtho(SerializedProperty property) => AccessProperty<bool>(
            typeof(LensSettings), SerializedPropertyHelper.GetPropertyValue(property), "Orthographic");

        static bool IsPhysical(SerializedProperty property) => AccessProperty<bool>(
            typeof(LensSettings), SerializedPropertyHelper.GetPropertyValue(property), "IsPhysicalCamera");

        static Vector2 SensorSize(SerializedProperty property) => AccessProperty<Vector2>(
            typeof(LensSettings), SerializedPropertyHelper.GetPropertyValue(property), "SensorSize");

        static bool UseHorizontalFOV(SerializedProperty property) => 
            InspectorUtility.GetUseHorizontalFOV(AccessProperty<Camera>(
                typeof(LensSettings), SerializedPropertyHelper.GetPropertyValue(property), "SourceCamera"));

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

        static bool s_PhysicalExapnded;

        static List<string> m_PresetOptions;
        static List<string> m_PhysicalPresetOptions;
        static string m_EditPresetsLabel = "Edit Presets...";

        protected bool HideModeOverride { get; set; }

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

        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            InitPresetOptions();

            var ux = new VisualElement();

            // When foldout is closed, we display the FOV on the same line, for convenience
            var foldout = new Foldout { text = property.displayName, tooltip = property.tooltip, value = property.isExpanded };
            foldout.RegisterValueChangedCallback((evt) => 
            {
                if (evt.target == foldout)
                {
                    property.isExpanded = evt.newValue;
                    property.serializedObject.ApplyModifiedPropertiesWithoutUndo();
                }
            });

            var outerFovControl = new FovPropertyControl(property, true) { style = { flexGrow = 1 }};
            ux.Add(new InspectorUtility.FoldoutWithOverlay(
                foldout, outerFovControl, outerFovControl.ShortLabel) { style = { flexGrow = 1 }});

            // Populate the foldout
            var innerFovControl = foldout.AddChild(new FovPropertyControl(property, false) { style = { flexGrow = 1 }});

            var nearClip = property.FindPropertyRelative(() => m_LensSettingsDef.NearClipPlane);
            foldout.AddChild(new PropertyField(nearClip)).RegisterValueChangeCallback((evt) =>
            {
                if (!IsOrtho(property) && nearClip.floatValue < 0.01f)
                {
                    nearClip.floatValue = 0.01f;
                    property.serializedObject.ApplyModifiedPropertiesWithoutUndo();
                }
            });
            foldout.Add(new PropertyField(property.FindPropertyRelative(() => m_LensSettingsDef.FarClipPlane)));
            foldout.Add(new PropertyField(property.FindPropertyRelative(() => m_LensSettingsDef.Dutch)));

            var physical = foldout.AddChild(new Foldout() { text = "Physical Properties", value = s_PhysicalExapnded });
            physical.RegisterValueChangedCallback((evt) => 
            {
                if (evt.target == physical)
                    s_PhysicalExapnded = evt.newValue;
            });

            physical.Add(new PropertyField(property.FindPropertyRelative(() => m_LensSettingsDef.GateFit)));

            var ssProp = property.FindPropertyRelative(() => m_LensSettingsDef.m_SensorSize);
            var sensorSizeField = physical.AddChild(new PropertyField(ssProp));
            sensorSizeField.RegisterValueChangeCallback((evt) =>
            {
                var v = ssProp.vector2Value;
                v.x = Mathf.Max(v.x, 0.1f);
                v.y = Mathf.Max(v.y, 0.1f);
                ssProp.vector2Value = v;
                property.serializedObject.ApplyModifiedPropertiesWithoutUndo();
            });

#if CINEMACHINE_HDRP
            physical.Add(new PropertyField(property.FindPropertyRelative(() => m_LensSettingsDef.Iso)));
            physical.Add(new PropertyField(property.FindPropertyRelative(() => m_LensSettingsDef.ShutterSpeed)));
#endif
            physical.Add(new PropertyField(property.FindPropertyRelative(() => m_LensSettingsDef.LensShift)));
            physical.Add(new PropertyField(property.FindPropertyRelative(() => m_LensSettingsDef.FocusDistance)));
#if CINEMACHINE_HDRP
            physical.Add(new PropertyField(property.FindPropertyRelative(() => m_LensSettingsDef.Aperture)));
            physical.Add(new PropertyField(property.FindPropertyRelative(() => m_LensSettingsDef.BladeCount)));
            physical.Add(new PropertyField(property.FindPropertyRelative(() => m_LensSettingsDef.Curvature)));
            physical.Add(new PropertyField(property.FindPropertyRelative(() => m_LensSettingsDef.BarrelClipping)));
            physical.Add(new PropertyField(property.FindPropertyRelative(() => m_LensSettingsDef.Anamorphism)));
#endif

            SerializedProperty modeOverrideProperty = null;
            VisualElement modeHelp = null;
            if (!HideModeOverride)
            {
                modeOverrideProperty = property.FindPropertyRelative(() => m_LensSettingsDef.ModeOverride);
                modeHelp = foldout.AddChild(
                    new HelpBox("Lens Mode Override must be enabled in the CM Brain for Mode Override to take effect", 
                        HelpBoxMessageType.Warning));
                foldout.Add(new PropertyField(modeOverrideProperty));
            }

            // GML: This is rather evil.  Is there a better (event-driven) way?
            DoUpdate();
            ux.schedule.Execute(DoUpdate).Every(250);
            void DoUpdate()
            {
                if (property.serializedObject.targetObject == null)
                    return; // target deleted

                bool isPhysical = IsPhysical(property);
                physical.SetVisible(isPhysical);
                outerFovControl.Update(true);
                innerFovControl.Update(false);

                if (!HideModeOverride)
                {
                    var brainHasModeOverride = CinemachineCore.Instance.BrainCount > 0 
                        && CinemachineCore.Instance.GetActiveBrain(0).LensModeOverride.Enabled;
                    modeHelp.SetVisible(!brainHasModeOverride
                        && modeOverrideProperty.intValue != (int)LensSettings.OverrideModes.None);
                }
            };

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
                ShortLabel = new Label("(fov)") { style = { alignSelf = Align.Center, opacity = 0.5f }};
                ShortLabel.AddToClassList("unity-base-field__label--with-dragger");

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
                    style = { flexGrow = 2, flexBasis = 0 },
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
                    { 
                        tooltip = "Custom Lens Presets", style = 
                        { 
                            height = InspectorUtility.SingleLineHeight,
                            alignSelf = Align.Center, flexGrow = 1, flexBasis = 0, minWidth = 0,
                            marginLeft = 5, paddingRight = 0, borderRightWidth = 0, marginRight = 0
                        }
                    };
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
                    style = { flexGrow = 2, flexBasis = 0 },
                    value = GetDisplayFocalLength(focalProperty.floatValue)
                };
                focalField.AddToClassList(InspectorUtility.kAlignFieldClass);
                focalField.RegisterValueChangedCallback((evt) =>
                {
                    // Convert and clamp
                    var sensorHeight = SensorSize(property).y;
                    var vfov = Camera.FocalLengthToFieldOfView(Mathf.Max(0.01f, evt.newValue), sensorHeight);
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
                    { 
                        tooltip = "Custom Lens Presets", style = 
                        { 
                            height = InspectorUtility.SingleLineHeight,
                            alignSelf = Align.Center, flexGrow = 1, flexBasis = 0, minWidth = 0,
                            marginLeft = 5, paddingRight = 0, borderRightWidth = 0, marginRight = 0
                        }
                    };
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
                            focalProperty.floatValue = Camera.FocalLengthToFieldOfView(Mathf.Max(0.01f, v.m_FocalLength), SensorSize(property).y);
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
                    var focalLength = Camera.FieldOfViewToFocalLength(p.floatValue, SensorSize(property).y);
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
                    if (shortLabel && ShortLabel.text != "Ortho" && OrthoControl.Q<FloatField>() != null)
                    {
                        ShortLabel.text = "Ortho";
                        m_DraggerOrtho ??= new (OrthoControl.Q<FloatField>());
                        m_DraggerOrtho.SetDragZone(ShortLabel);
                        m_DraggerFov?.SetDragZone(null);
                        m_DraggerFocal?.SetDragZone(null);
                    }
                }
                else if (IsPhysical(m_Property))
                {
                    OrthoControl.SetVisible(false);
                    FovControl.SetVisible(false);
                    FocalLengthControl.SetVisible(true);
                    var text = "Focal Length";
                    if (!shortLabel)
                    {
                        var e = FocalLengthControl.Q<Label>();
                        if (e != null)
                            e.text = text;
                    }
                    else if (ShortLabel.text != text && FocalLengthControl.Q<FloatField>() != null)
                    {
                        ShortLabel.text = text;
                        m_DraggerOrtho?.SetDragZone(null);
                        m_DraggerFov?.SetDragZone(null);
                        m_DraggerFocal ??= new (FocalLengthControl.Q<FloatField>());
                        m_DraggerFocal.SetDragZone(ShortLabel);
                    }
                }
                else
                {
                    OrthoControl.SetVisible(false);
                    FovControl.SetVisible(true);
                    FocalLengthControl.SetVisible(false);
                    var text = UseHorizontalFOV(m_Property) ? "Horiz. FOV" : "Vertical FOV";
                    if (!shortLabel)
                    {
                        var e = FovControl.Q<Label>();
                        if (e != null)
                            e.text = text;
                    }
                    else if (ShortLabel.text != text && FovControl.Q<FloatField>() != null)
                    {
                        ShortLabel.text = text;
                        m_DraggerOrtho?.SetDragZone(null);
                        m_DraggerFov ??= new (FovControl.Q<FloatField>());
                        m_DraggerFov.SetDragZone(ShortLabel);
                        m_DraggerFocal?.SetDragZone(null);
                    }
                }
            }
        }

        ///===========================================================================
        /// IMGUI IMPLEMENTATION (to be removed) 
        ///===========================================================================

        static readonly GUIContent EditPresetsLabel = new GUIContent("Edit Presets...");
        static readonly GUIContent HFOVLabel = new GUIContent("Horizontal FOV", "Horizontal Field of View");
        static readonly GUIContent VFOVLabel = new GUIContent("Vertical FOV", "Vertical Field of View");
        static readonly GUIContent FocalLengthLabel = new GUIContent("Focal Length", "The length of the lens (in mm)");
        static readonly GUIContent OrthoSizeLabel = new GUIContent("Ortho Size", "When using an orthographic camera, "
            + "this defines the half-height, in world coordinates, of the camera view.");
        static readonly GUIContent SensorSizeLabel = new GUIContent("Sensor Size", 
            "Actual size of the image sensor (in mm), used to "
            + "convert between focal length and field of view.");
        static readonly GUIContent s_EmptyContent = new GUIContent(" ");
        static readonly GUIContent PhysicalPropertiesLabel = new GUIContent("Physical Properties", "Physical properties of the lens");
        static readonly GUIContent AdvancedLabel = new GUIContent("Advanced");
        static readonly string AdvancedHelpboxMessage = "Lens Mode Override must be enabled in the CM Brain for Mode Override to take effect";

        static bool s_AdvancedLensExpanded;

        struct Snapshot
        {
            public bool IsOrtho;
            public bool IsPhysical;
            public Vector2 SensorSize;
            public bool UseHorizontalFOV;

            public GUIContent[] m_PresetOptions;
            public GUIContent[] m_PhysicalPresetOptions;
        }
        Snapshot m_Snapshot;

        const float vSpace= 2;
        const float hSpace = 2;

        void SnapshotCameraShadowValues(SerializedProperty property)
        {
            if (m_Snapshot.m_PresetOptions == null)
            {
                var options = new List<GUIContent>();
                CinemachineLensPresets presets = CinemachineLensPresets.InstanceIfExists;
                for (int i = 0; presets != null && i < presets.m_Presets.Length; ++i)
                    options.Add(new GUIContent(presets.m_Presets[i].m_Name));
                options.Add(EditPresetsLabel);
                m_Snapshot.m_PresetOptions = options.ToArray();

                options.Clear();
                for (int i = 0; presets != null && i < presets.m_PhysicalPresets.Length; ++i)
                    options.Add(new GUIContent(presets.m_PhysicalPresets[i].m_Name));
                options.Add(EditPresetsLabel);
                m_Snapshot.m_PhysicalPresetOptions = options.ToArray();
            }
            // Assume lens is up-to-date
            m_Snapshot.UseHorizontalFOV = UseHorizontalFOV(property);
            m_Snapshot.IsOrtho = IsOrtho(property);
            m_Snapshot.IsPhysical = IsPhysical(property);
            m_Snapshot.SensorSize = SensorSize(property);
        }

        GUIContent GetFOVLabel()
        {
            if (m_Snapshot.IsOrtho) return OrthoSizeLabel;
            if (m_Snapshot.IsPhysical) return FocalLengthLabel;
            return m_Snapshot.UseHorizontalFOV ? HFOVLabel : VFOVLabel;
        }

        void DrawFOVControl(Rect rect, SerializedProperty property, GUIContent label)
        {
            if (m_Snapshot.IsOrtho)
                EditorGUI.PropertyField(
                    rect, property.FindPropertyRelative(() => m_LensSettingsDef.OrthographicSize), label);
            else if (m_Snapshot.IsPhysical)
                DrawFocalLengthControl(rect, property, label);
            else
            {
                var FOVProperty = property.FindPropertyRelative(() => m_LensSettingsDef.FieldOfView);
                float aspect = m_Snapshot.SensorSize.x / m_Snapshot.SensorSize.y;

                float dropdownWidth = (rect.width - EditorGUIUtility.labelWidth) / 3;
                rect.width -= dropdownWidth + hSpace;

                float f = FOVProperty.floatValue;
                if (m_Snapshot.UseHorizontalFOV)
                    f = Camera.VerticalToHorizontalFieldOfView(f, aspect);
                EditorGUI.BeginProperty(rect, label, FOVProperty);
                f = EditorGUI.FloatField(rect, label, f);
                if (m_Snapshot.UseHorizontalFOV)
                    f = Camera.HorizontalToVerticalFieldOfView(Mathf.Clamp(f, 1, 179), aspect);
                if (!Mathf.Approximately(FOVProperty.floatValue, f))
                    FOVProperty.floatValue = Mathf.Clamp(f, 1, 179);
                EditorGUI.EndProperty();
                rect.x += rect.width + hSpace; rect.width = dropdownWidth;

                CinemachineLensPresets presets = CinemachineLensPresets.InstanceIfExists;
                int preset = (presets == null) ? -1 : presets.GetMatchingPreset(FOVProperty.floatValue);

                var oldLabelWidth = EditorGUIUtility.labelWidth;
                EditorGUIUtility.labelWidth = 1;
                int selection = EditorGUI.Popup(rect, s_EmptyContent, preset, m_Snapshot.m_PresetOptions);
                EditorGUIUtility.labelWidth = oldLabelWidth;
                if (selection == m_Snapshot.m_PresetOptions.Length-1 && CinemachineLensPresets.Instance != null)
                    Selection.activeObject = presets = CinemachineLensPresets.Instance;
                else if (selection >= 0 && selection < m_Snapshot.m_PresetOptions.Length-1)
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

            float f = Camera.FieldOfViewToFocalLength(FOVProperty.floatValue, m_Snapshot.SensorSize.y);
            EditorGUI.BeginProperty(rect, label, FOVProperty);
            f = EditorGUI.FloatField(rect, label, f);
            f = Camera.FocalLengthToFieldOfView(Mathf.Max(0.01f, f), m_Snapshot.SensorSize.y);
            if (!Mathf.Approximately(FOVProperty.floatValue, f))
                FOVProperty.floatValue = Mathf.Clamp(f, 1, 179);
            EditorGUI.EndProperty();

            rect.x += rect.width + hSpace; rect.width = dropdownWidth;

#if CINEMACHINE_HDRP
            CinemachineLensPresets presets = CinemachineLensPresets.InstanceIfExists;
            int preset = -1;
            if (presets != null)
            {
                var focalLength = Camera.FieldOfViewToFocalLength(FOVProperty.floatValue, m_Snapshot.SensorSize.y);
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

            var oldLabelWidth = EditorGUIUtility.labelWidth;
            EditorGUIUtility.labelWidth = 1;
            int selection = EditorGUI.Popup(rect, s_EmptyContent, preset, m_Snapshot.m_PhysicalPresetOptions);
            EditorGUIUtility.labelWidth = oldLabelWidth;
            if (selection == m_Snapshot.m_PhysicalPresetOptions.Length-1 && CinemachineLensPresets.Instance != null)
                Selection.activeObject = presets = CinemachineLensPresets.Instance;
            else if (selection >= 0 && selection < m_Snapshot.m_PhysicalPresetOptions.Length-1)
            {
                var v = presets.m_PhysicalPresets[selection];
                FOVProperty.floatValue = Camera.FocalLengthToFieldOfView(Mathf.Max(0.01f, v.m_FocalLength), m_Snapshot.SensorSize.y);
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
                Camera.FieldOfViewToFocalLength(FOVProperty.floatValue, m_Snapshot.SensorSize.y));
            var oldLabelWidth = EditorGUIUtility.labelWidth;
            EditorGUIUtility.labelWidth = 1;
            int selection = EditorGUI.Popup(rect, s_EmptyContent, preset, m_Snapshot.m_PhysicalPresetOptions);
            EditorGUIUtility.labelWidth = oldLabelWidth;
            if (selection == m_Snapshot.m_PhysicalPresetOptions.Length-1 && CinemachineLensPresets.Instance != null)
                Selection.activeObject = presets = CinemachineLensPresets.Instance;
            else if (selection >= 0 && selection < m_Snapshot.m_PhysicalPresetOptions.Length-1)
            {
                FOVProperty.floatValue = Camera.FocalLengthToFieldOfView(
                    Mathf.Max(0.01f, presets.m_PhysicalPresets[selection].m_FocalLength), m_Snapshot.SensorSize.y);
                property.serializedObject.ApplyModifiedProperties();
            }
#endif
        }

        public override void OnGUI(Rect rect, SerializedProperty property, GUIContent label)
        {
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
                if (!m_Snapshot.IsOrtho && nearClip.floatValue < 0.01f)
                {
                    nearClip.floatValue = 0.01f;
                    property.serializedObject.ApplyModifiedPropertiesWithoutUndo();
                }
                rect.y += rect.height + vSpace;
                EditorGUI.PropertyField(rect, property.FindPropertyRelative(() => m_LensSettingsDef.FarClipPlane));

                rect.y += rect.height + vSpace;
                EditorGUI.PropertyField(rect, property.FindPropertyRelative(() => m_LensSettingsDef.Dutch));

                if (m_Snapshot.IsPhysical)
                {
                    rect.y += rect.height + vSpace;
                    s_PhysicalExapnded = EditorGUI.Foldout(rect, s_PhysicalExapnded, PhysicalPropertiesLabel, true);

                    if (s_PhysicalExapnded)
                    {
                        ++EditorGUI.indentLevel;

                        rect.y += rect.height + vSpace;
                        EditorGUI.PropertyField(rect, property.FindPropertyRelative(() => m_LensSettingsDef.GateFit));

                        rect.y += rect.height + vSpace;
                        var ssProp = property.FindPropertyRelative(() => m_LensSettingsDef.m_SensorSize);
                        EditorGUI.BeginProperty(rect, SensorSizeLabel, ssProp);
                        var v = EditorGUI.Vector2Field(rect, SensorSizeLabel, ssProp.vector2Value);
                        v.x = Mathf.Max(v.x, 0.1f);
                        v.y = Mathf.Max(v.y, 0.1f);
                        ssProp.vector2Value = v;
                        EditorGUI.EndProperty();

                        rect.y += rect.height + vSpace;
                        EditorGUI.PropertyField(rect, property.FindPropertyRelative(() => m_LensSettingsDef.LensShift));
#if CINEMACHINE_HDRP
                        rect.y += rect.height + vSpace;
                        EditorGUI.PropertyField(rect, property.FindPropertyRelative(() => m_LensSettingsDef.Aperture));
                        rect.y += rect.height + vSpace;
                        EditorGUI.PropertyField(rect, property.FindPropertyRelative(() => m_LensSettingsDef.Iso));
                        rect.y += rect.height + vSpace;
                        EditorGUI.PropertyField(rect, property.FindPropertyRelative(() => m_LensSettingsDef.ShutterSpeed));
                        rect.y += rect.height + vSpace;
                        EditorGUI.PropertyField(rect, property.FindPropertyRelative(() => m_LensSettingsDef.BladeCount));
                        rect.y += rect.height + vSpace;
                        EditorGUI.PropertyField(rect, property.FindPropertyRelative(() => m_LensSettingsDef.Curvature));
                        rect.y += rect.height + vSpace;
                        EditorGUI.PropertyField(rect, property.FindPropertyRelative(() => m_LensSettingsDef.BarrelClipping));
                        rect.y += rect.height + vSpace;
                        EditorGUI.PropertyField(rect, property.FindPropertyRelative(() => m_LensSettingsDef.Anamorphism));
#endif
                        --EditorGUI.indentLevel;
                    }
                }
                if (!HideModeOverride)
                {
                    rect.y += rect.height + vSpace;
                    s_AdvancedLensExpanded = EditorGUI.Foldout(rect, s_AdvancedLensExpanded, AdvancedLabel);
                    if (s_AdvancedLensExpanded)
                    {
                        ++EditorGUI.indentLevel;
                        rect.y += rect.height + vSpace;
                        var r = EditorGUI.IndentedRect(rect); r.height *= 2;
                        EditorGUI.HelpBox(r, AdvancedHelpboxMessage, MessageType.Info);

                        rect.y += r.height + vSpace;
                        EditorGUI.PropertyField(rect, property.FindPropertyRelative(() => m_LensSettingsDef.ModeOverride));
                        --EditorGUI.indentLevel;
                    }
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

            int numLines = 4;
            if (m_Snapshot.IsPhysical)
            {
                numLines += 1;
                if (s_PhysicalExapnded)
                {
#if CINEMACHINE_HDRP
                    numLines += 10;
#else
                    numLines += 3;
#endif
                }
            }
            if (!HideModeOverride)
            {
                // Advanced section
                numLines += 1;
                if (s_AdvancedLensExpanded)
                    numLines += 3;  // not correct but try to make it big enough to hold the help box
            }
            return lineHeight + numLines * (lineHeight + vSpace);
        }
    }
}
