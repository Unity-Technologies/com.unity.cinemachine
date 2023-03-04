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
        static LensSettings s_LensSettingsDef = new (); // to access name strings

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
                return default;

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
                return default;
        }

        static bool s_PhysicalExapnded;

        static List<string> m_PresetOptions;
        static List<string> m_PhysicalPresetOptions;
        const string k_EditPresetsLabel = "Edit Presets...";

        protected bool HideModeOverride { get; set; }

        void InitPresetOptions()
        {
            m_PresetOptions ??= new List<string>();
            m_PresetOptions.Clear();
            var presets = CinemachineLensPresets.InstanceIfExists;
            for (int i = 0; presets != null && i < presets.Presets.Count; ++i)
                m_PresetOptions.Add(presets.Presets[i].Name);
            m_PresetOptions.Add("");
            m_PresetOptions.Add(k_EditPresetsLabel);

            m_PhysicalPresetOptions ??= new List<string>();
            m_PhysicalPresetOptions.Clear();
            for (int i = 0; presets != null && i < presets.PhysicalPresets.Count; ++i)
                m_PhysicalPresetOptions.Add(presets.PhysicalPresets[i].Name);
            m_PhysicalPresetOptions.Add("");
            m_PhysicalPresetOptions.Add(k_EditPresetsLabel);
        }

        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            InitPresetOptions();

            var ux = new VisualElement();

            // When foldout is closed, we display the FOV on the same line, for convenience
            var foldout = new Foldout { text = property.displayName, tooltip = property.tooltip, value = property.isExpanded };
            foldout.BindProperty(property);

            var outerFovControl = new FovPropertyControl(property, true) { style = { flexGrow = 1 }};
            ux.Add(new InspectorUtility.FoldoutWithOverlay(
                foldout, outerFovControl, outerFovControl.ShortLabel) { style = { flexGrow = 1 }});

            // Populate the foldout
            var innerFovControl = foldout.AddChild(new FovPropertyControl(property, false) { style = { flexGrow = 1 }});

            var nearClip = property.FindPropertyRelative(() => s_LensSettingsDef.NearClipPlane);
            foldout.AddChild(new PropertyField(nearClip)).RegisterValueChangeCallback((evt) =>
            {
                if (!IsOrtho(property) && nearClip.floatValue < 0.01f)
                {
                    nearClip.floatValue = 0.01f;
                    property.serializedObject.ApplyModifiedPropertiesWithoutUndo();
                }
            });
            foldout.Add(new PropertyField(property.FindPropertyRelative(() => s_LensSettingsDef.FarClipPlane)));
            foldout.Add(new PropertyField(property.FindPropertyRelative(() => s_LensSettingsDef.Dutch)));

            var physical = foldout.AddChild(new Foldout() { text = "Physical Properties", value = s_PhysicalExapnded });
            physical.RegisterValueChangedCallback((evt) => 
            {
                if (evt.target == physical)
                    s_PhysicalExapnded = evt.newValue;
            });

            var physicalProp = property.FindPropertyRelative(() => s_LensSettingsDef.PhysicalProperties);
            physical.Add(new PropertyField(physicalProp.FindPropertyRelative(() => s_LensSettingsDef.PhysicalProperties.GateFit)));
            physical.Add(new PropertyField(physicalProp.FindPropertyRelative(() => s_LensSettingsDef.PhysicalProperties.SensorSize)));
            physical.Add(new PropertyField(physicalProp.FindPropertyRelative(() => s_LensSettingsDef.PhysicalProperties.LensShift)));
            physical.Add(new PropertyField(physicalProp.FindPropertyRelative(() => s_LensSettingsDef.PhysicalProperties.FocusDistance)));

            physical.Add(new PropertyField(physicalProp.FindPropertyRelative(() => s_LensSettingsDef.PhysicalProperties.Iso)));
            physical.Add(new PropertyField(physicalProp.FindPropertyRelative(() => s_LensSettingsDef.PhysicalProperties.ShutterSpeed)));
            physical.Add(new PropertyField(physicalProp.FindPropertyRelative(() => s_LensSettingsDef.PhysicalProperties.Aperture)));
            physical.Add(new PropertyField(physicalProp.FindPropertyRelative(() => s_LensSettingsDef.PhysicalProperties.BladeCount)));
            physical.Add(new PropertyField(physicalProp.FindPropertyRelative(() => s_LensSettingsDef.PhysicalProperties.Curvature)));
            physical.Add(new PropertyField(physicalProp.FindPropertyRelative(() => s_LensSettingsDef.PhysicalProperties.BarrelClipping)));
            physical.Add(new PropertyField(physicalProp.FindPropertyRelative(() => s_LensSettingsDef.PhysicalProperties.Anamorphism)));

            SerializedProperty modeOverrideProperty = null;
            VisualElement modeHelp = null;
            if (!HideModeOverride)
            {
                modeOverrideProperty = property.FindPropertyRelative(() => s_LensSettingsDef.ModeOverride);
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
                outerFovControl.TimedUpdate();
                innerFovControl.TimedUpdate();

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
        /// Make the complicated FOV widget which works in 4 modes, with preset popups, 
        /// and optional weird small label display
        /// </summary>
        class FovPropertyControl : InspectorUtility.LabeledRow
        {
            public readonly Label ShortLabel;
            readonly SerializedProperty m_LensProperty;
            readonly FloatField m_Control;
            readonly PopupField<string> m_Presets;

            enum Modes { Ortho, Physical, VFOV, HFOV };
            Modes GetLensMode()
            {
                if (IsOrtho(m_LensProperty)) 
                    return Modes.Ortho;
                if (IsPhysical(m_LensProperty))
                    return Modes.Physical;
                return UseHorizontalFOV(m_LensProperty) ? Modes.HFOV : Modes.VFOV;
            }

            public FovPropertyControl(SerializedProperty property, bool hideLabel) : base(hideLabel ? "" : "(fov)")
            {
                style.flexDirection = FlexDirection.Row;
                m_LensProperty = property;
                m_Control = Contents.AddChild(new FloatField("") { style = {flexBasis = 20, flexGrow = 2, marginLeft = 0}});
                m_Control.RegisterValueChangedCallback(OnControlValueChanged);
                Label.SetVisible(!hideLabel);
                Label.AddToClassList("unity-base-field__label--with-dragger");
                new FieldMouseDragger<float>(m_Control).SetDragZone(Label);

                m_Presets = Contents.AddChild(new PopupField<string>
                    { tooltip = "Custom Lens Presets", style = {flexBasis = 20, flexGrow = 1}});
                m_Presets.RegisterValueChangedCallback(OnPresetValueChanged);

                ShortLabel = new Label("X") { style = { alignSelf = Align.Center, opacity = 0.5f }};
                ShortLabel.AddToClassList("unity-base-field__label--with-dragger");
                new FieldMouseDragger<float>(m_Control).SetDragZone(ShortLabel);

                this.TrackPropertyWithInitialCallback(property, OnLensPropertyChanged);
            }

            void OnControlValueChanged(ChangeEvent<float> evt)
            {
                var mode = GetLensMode();
                switch (mode)
                {
                    case Modes.Ortho:
                    {
                        var orthoProp = m_LensProperty.FindPropertyRelative(() => s_LensSettingsDef.OrthographicSize);
                        orthoProp.floatValue = evt.newValue;
                        orthoProp.serializedObject.ApplyModifiedProperties();
                        break;
                    }
                    case Modes.Physical:
                    {
                        // Convert and clamp
                        var sensorHeight = SensorSize(m_LensProperty).y;
                        var vfov = Camera.FocalLengthToFieldOfView(Mathf.Max(0.01f, evt.newValue), sensorHeight);
                        vfov = Mathf.Clamp(vfov, 1, 179);
                        m_Control.SetValueWithoutNotify(Camera.FieldOfViewToFocalLength(vfov, sensorHeight));

                        // Push to property
                        var fovProp = m_LensProperty.FindPropertyRelative(() => s_LensSettingsDef.FieldOfView);
                        fovProp.floatValue = vfov;
                        fovProp.serializedObject.ApplyModifiedProperties();
                        break;
                    }
                    case Modes.VFOV:
                    case Modes.HFOV:
                    {
                        var newValue = Mathf.Clamp(evt.newValue, 1, 179);
                        m_Control.SetValueWithoutNotify(newValue);

                        // Convert from display units
                        if (mode == Modes.HFOV)
                        {
                            var sensorSize = SensorSize(m_LensProperty);
                            newValue = Camera.HorizontalToVerticalFieldOfView(newValue, sensorSize.x / sensorSize.y);
                        }
                        // Push to property
                        var fovProp = m_LensProperty.FindPropertyRelative(() => s_LensSettingsDef.FieldOfView);
                        fovProp.floatValue = newValue;
                        fovProp.serializedObject.ApplyModifiedProperties();
                        break;
                    }
                }
            }

            void OnLensPropertyChanged(SerializedProperty p)
            {
                var mode = GetLensMode();
                switch (mode)
                {
                    case Modes.Ortho:
                    {
                        var orthoProp = m_LensProperty.FindPropertyRelative(() => s_LensSettingsDef.OrthographicSize);
                        m_Control.SetValueWithoutNotify(orthoProp.floatValue);
                        break;
                    }
                    case Modes.Physical:
                    {
                        // Convert to display FolcalLength units
                        var fovProp = m_LensProperty.FindPropertyRelative(() => s_LensSettingsDef.FieldOfView);
                        var v = Camera.FieldOfViewToFocalLength(fovProp.floatValue, SensorSize(m_LensProperty).y);
                        m_Control.SetValueWithoutNotify(v);
                        SyncPhysicalPreset();
                        break;
                    }
                    case Modes.VFOV:
                    case Modes.HFOV:
                    {
                        // Convert to display FOV units
                        var fovProp = m_LensProperty.FindPropertyRelative(() => s_LensSettingsDef.FieldOfView);
                        var v = fovProp.floatValue;
                        if (mode == Modes.HFOV)
                        {
                            var sensorSize = SensorSize(m_LensProperty);
                            v = Camera.VerticalToHorizontalFieldOfView(v, sensorSize.x / sensorSize.y);
                        }
                        m_Control.SetValueWithoutNotify(v);

                        // Sync the presets
                        var presets = CinemachineLensPresets.InstanceIfExists;
                        var index = presets == null ? -1 : presets.GetMatchingPreset(m_Control.value);
                        m_Presets.SetValueWithoutNotify(index < 0 ? string.Empty : presets.Presets[index].Name);
                        break;
                    }
                }
                TimedUpdate();
            }

            public void TimedUpdate()
            {
                switch (GetLensMode())
                {
                    case Modes.Ortho:
                    {
                        var text = "O";
                        if (ShortLabel.text != text)
                        {
                            var orthoProp = m_LensProperty.FindPropertyRelative(() => s_LensSettingsDef.OrthographicSize);
                            ShortLabel.text = text;
                            Label.text = orthoProp.displayName;
                            Label.tooltip = m_Control.tooltip = ShortLabel.tooltip = orthoProp.tooltip;
                            m_Presets.SetVisible(false);
                        }
                        break;
                    }
                    case Modes.Physical:
                    {
                        var text = "F";
                        if (ShortLabel.text != text)
                        {
                            var fovProp = m_LensProperty.FindPropertyRelative(() => s_LensSettingsDef.FieldOfView);
                            ShortLabel.text = text;
                            Label.text = ShortLabel.tooltip = "Focal Length";
                            Label.tooltip = m_Control.tooltip = fovProp.tooltip;
                            m_Presets.choices = m_PhysicalPresetOptions;
                            m_Presets.SetVisible(true);
                        }
                        break;
                    }
                    case Modes.HFOV:
                    {
                        var text = "H";
                        if (ShortLabel.text != text)
                        {
                            var fovProp = m_LensProperty.FindPropertyRelative(() => s_LensSettingsDef.FieldOfView);
                            ShortLabel.text = text;
                            Label.text = ShortLabel.tooltip = "Horizontal FOV";
                            Label.tooltip = m_Control.tooltip = fovProp.tooltip;
                            m_Presets.choices = m_PresetOptions;
                            m_Presets.SetVisible(true);
                        }
                        break;
                    }
                    case Modes.VFOV:
                    {
                        var text = "V";
                        if (ShortLabel.text != text)
                        {
                            var fovProp = m_LensProperty.FindPropertyRelative(() => s_LensSettingsDef.FieldOfView);
                            ShortLabel.text = text;
                            Label.text = ShortLabel.tooltip = "Vertical FOV";
                            Label.tooltip = m_Control.tooltip = fovProp.tooltip;
                            m_Presets.choices = m_PresetOptions;
                            m_Presets.SetVisible(true);
                        }
                        break;
                    }
                }
            }

            void OnPresetValueChanged(ChangeEvent<string> evt)
            {
                // Edit the presets assets if desired
                if (evt.newValue == k_EditPresetsLabel)
                    Selection.activeObject = CinemachineLensPresets.Instance;
                else 
                {
                    // Apply the preset
                    var fovProp = m_LensProperty.FindPropertyRelative(() => s_LensSettingsDef.FieldOfView);
                    if (GetLensMode() == Modes.Physical)
                    {
                        var index = CinemachineLensPresets.Instance.GetPhysicalPresetIndex(evt.newValue);
                        if (index >= 0)
                        {
                            var v = CinemachineLensPresets.Instance.PhysicalPresets[index];
                            fovProp.floatValue = Camera.FocalLengthToFieldOfView(
                                Mathf.Max(0.01f, v.FocalLength), SensorSize(m_LensProperty).y);
                            var physicalProp = m_LensProperty.FindPropertyRelative(() => s_LensSettingsDef.PhysicalProperties);
                            physicalProp.FindPropertyRelative(() => s_LensSettingsDef.PhysicalProperties.GateFit).intValue = (int)v.GateFit;
                            physicalProp.FindPropertyRelative(() => s_LensSettingsDef.PhysicalProperties.SensorSize).vector2Value = v.SensorSize;
                            physicalProp.FindPropertyRelative(() => s_LensSettingsDef.PhysicalProperties.LensShift).vector2Value = v.LensShift;
                            physicalProp.FindPropertyRelative(() => s_LensSettingsDef.PhysicalProperties.Iso).intValue = v.Iso;
                            physicalProp.FindPropertyRelative(() => s_LensSettingsDef.PhysicalProperties.ShutterSpeed).floatValue = v.ShutterSpeed;
                            physicalProp.FindPropertyRelative(() => s_LensSettingsDef.PhysicalProperties.Aperture).floatValue = v.Aperture;
                            physicalProp.FindPropertyRelative(() => s_LensSettingsDef.PhysicalProperties.BladeCount).intValue = v.BladeCount;
                            physicalProp.FindPropertyRelative(() => s_LensSettingsDef.PhysicalProperties.Curvature).vector2Value = v.Curvature;
                            physicalProp.FindPropertyRelative(() => s_LensSettingsDef.PhysicalProperties.BarrelClipping).floatValue = v.BarrelClipping;
                            physicalProp.FindPropertyRelative(() => s_LensSettingsDef.PhysicalProperties.Anamorphism).floatValue = v.Anamorphism;
                            m_LensProperty.serializedObject.ApplyModifiedProperties();
                        }
                    }
                    else
                    {
                        var index = CinemachineLensPresets.Instance.GetPresetIndex(evt.newValue);
                        if (index >= 0)
                        {
                            fovProp.floatValue = CinemachineLensPresets.Instance.Presets[index].FieldOfView;
                            fovProp.serializedObject.ApplyModifiedProperties();
                        }
                    }
                }
            }

            void SyncPhysicalPreset()
            {
                var v = string.Empty;
                var presets = CinemachineLensPresets.InstanceIfExists;
                if (presets != null)
                {
                    var physicalProp = m_LensProperty.FindPropertyRelative(() => s_LensSettingsDef.PhysicalProperties);
                    CinemachineLensPresets.PhysicalPreset p = new ()
                    {
                        FocalLength = m_Control.value,
                        GateFit = (Camera.GateFitMode)physicalProp.FindPropertyRelative(() => s_LensSettingsDef.PhysicalProperties.GateFit).intValue,
                        SensorSize = physicalProp.FindPropertyRelative(() => s_LensSettingsDef.PhysicalProperties.SensorSize).vector2Value,
                        LensShift = physicalProp.FindPropertyRelative(() => s_LensSettingsDef.PhysicalProperties.LensShift).vector2Value,
                        Iso = physicalProp.FindPropertyRelative(() => s_LensSettingsDef.PhysicalProperties.Iso).intValue,
                        ShutterSpeed = physicalProp.FindPropertyRelative(() => s_LensSettingsDef.PhysicalProperties.ShutterSpeed).floatValue,
                        Aperture = physicalProp.FindPropertyRelative(() => s_LensSettingsDef.PhysicalProperties.Aperture).floatValue,
                        BladeCount = physicalProp.FindPropertyRelative(() => s_LensSettingsDef.PhysicalProperties.BladeCount).intValue,
                        Curvature = physicalProp.FindPropertyRelative(() => s_LensSettingsDef.PhysicalProperties.Curvature).vector2Value,
                        BarrelClipping = physicalProp.FindPropertyRelative(() => s_LensSettingsDef.PhysicalProperties.BarrelClipping).floatValue,
                        Anamorphism =physicalProp.FindPropertyRelative(() => s_LensSettingsDef.PhysicalProperties.Anamorphism).floatValue
                    };
                    var index = presets.GetMatchingPhysicalPreset(p);
                    if (index >= 0)
                        v = CinemachineLensPresets.Instance.PhysicalPresets[index].Name;
                }
                m_Presets.SetValueWithoutNotify(v);
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
                for (int i = 0; presets != null && i < presets.Presets.Count; ++i)
                    options.Add(new GUIContent(presets.Presets[i].Name));
                options.Add(EditPresetsLabel);
                m_Snapshot.m_PresetOptions = options.ToArray();

                options.Clear();
                for (int i = 0; presets != null && i < presets.PhysicalPresets.Count; ++i)
                    options.Add(new GUIContent(presets.PhysicalPresets[i].Name));
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
                    rect, property.FindPropertyRelative(() => s_LensSettingsDef.OrthographicSize), label);
            else if (m_Snapshot.IsPhysical)
                DrawFocalLengthControl(rect, property, label);
            else
            {
                var FOVProperty = property.FindPropertyRelative(() => s_LensSettingsDef.FieldOfView);
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
                    var vfov = presets.Presets[selection].FieldOfView;
                    FOVProperty.floatValue = vfov;
                    property.serializedObject.ApplyModifiedProperties();
                }
            }
        }

        void DrawFocalLengthControl(Rect rect, SerializedProperty property, GUIContent label)
        {
            const float hSpace = 2;
            var FOVProperty = property.FindPropertyRelative(() => s_LensSettingsDef.FieldOfView);
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

            var physicalProp = property.FindPropertyRelative(() => s_LensSettingsDef.PhysicalProperties);
            int preset = -1;
            CinemachineLensPresets presets = CinemachineLensPresets.InstanceIfExists;
            if (presets != null)
            {
                CinemachineLensPresets.PhysicalPreset p = new ()
                {
                    FocalLength = Camera.FieldOfViewToFocalLength(FOVProperty.floatValue, m_Snapshot.SensorSize.y),
                    GateFit = (Camera.GateFitMode)physicalProp.FindPropertyRelative(() => s_LensSettingsDef.PhysicalProperties.GateFit).intValue,
                    SensorSize = physicalProp.FindPropertyRelative(() => s_LensSettingsDef.PhysicalProperties.SensorSize).vector2Value,
                    LensShift = physicalProp.FindPropertyRelative(() => s_LensSettingsDef.PhysicalProperties.LensShift).vector2Value,
                    Iso = physicalProp.FindPropertyRelative(() => s_LensSettingsDef.PhysicalProperties.Iso).intValue,
                    ShutterSpeed = physicalProp.FindPropertyRelative(() => s_LensSettingsDef.PhysicalProperties.ShutterSpeed).floatValue,
                    Aperture = physicalProp.FindPropertyRelative(() => s_LensSettingsDef.PhysicalProperties.Aperture).floatValue,
                    BladeCount = physicalProp.FindPropertyRelative(() => s_LensSettingsDef.PhysicalProperties.BladeCount).intValue,
                    Curvature = physicalProp.FindPropertyRelative(() => s_LensSettingsDef.PhysicalProperties.Curvature).vector2Value,
                    BarrelClipping = physicalProp.FindPropertyRelative(() => s_LensSettingsDef.PhysicalProperties.BarrelClipping).floatValue,
                    Anamorphism =physicalProp.FindPropertyRelative(() => s_LensSettingsDef.PhysicalProperties.Anamorphism).floatValue
                };
                preset = presets.GetMatchingPhysicalPreset(p);
            }

            var oldLabelWidth = EditorGUIUtility.labelWidth;
            EditorGUIUtility.labelWidth = 1;
            int selection = EditorGUI.Popup(rect, s_EmptyContent, preset, m_Snapshot.m_PhysicalPresetOptions);
            EditorGUIUtility.labelWidth = oldLabelWidth;
            if (selection == m_Snapshot.m_PhysicalPresetOptions.Length-1 && CinemachineLensPresets.Instance != null)
                Selection.activeObject = presets = CinemachineLensPresets.Instance;
            else if (selection >= 0 && selection < m_Snapshot.m_PhysicalPresetOptions.Length-1)
            {
                var v = presets.PhysicalPresets[selection];
                FOVProperty.floatValue = Camera.FocalLengthToFieldOfView(Mathf.Max(0.01f, v.FocalLength), m_Snapshot.SensorSize.y);
                physicalProp.FindPropertyRelative(() => s_LensSettingsDef.PhysicalProperties.GateFit).intValue = (int)v.GateFit;
                physicalProp.FindPropertyRelative(() => s_LensSettingsDef.PhysicalProperties.SensorSize).vector2Value = v.SensorSize;
                physicalProp.FindPropertyRelative(() => s_LensSettingsDef.PhysicalProperties.LensShift).vector2Value = v.LensShift;
                physicalProp.FindPropertyRelative(() => s_LensSettingsDef.PhysicalProperties.Iso).intValue = v.Iso;
                physicalProp.FindPropertyRelative(() => s_LensSettingsDef.PhysicalProperties.ShutterSpeed).floatValue = v.ShutterSpeed;
                physicalProp.FindPropertyRelative(() => s_LensSettingsDef.PhysicalProperties.Aperture).floatValue = v.Aperture;
                physicalProp.FindPropertyRelative(() => s_LensSettingsDef.PhysicalProperties.BladeCount).intValue = v.BladeCount;
                physicalProp.FindPropertyRelative(() => s_LensSettingsDef.PhysicalProperties.Curvature).vector2Value = v.Curvature;
                physicalProp.FindPropertyRelative(() => s_LensSettingsDef.PhysicalProperties.BarrelClipping).floatValue = v.BarrelClipping;
                physicalProp.FindPropertyRelative(() => s_LensSettingsDef.PhysicalProperties.Anamorphism).floatValue = v.Anamorphism;
                property.serializedObject.ApplyModifiedProperties();
            }
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
                var nearClip = property.FindPropertyRelative(() => s_LensSettingsDef.NearClipPlane);
                EditorGUI.PropertyField(rect, nearClip);
                if (!m_Snapshot.IsOrtho && nearClip.floatValue < 0.01f)
                {
                    nearClip.floatValue = 0.01f;
                    property.serializedObject.ApplyModifiedPropertiesWithoutUndo();
                }
                rect.y += rect.height + vSpace;
                EditorGUI.PropertyField(rect, property.FindPropertyRelative(() => s_LensSettingsDef.FarClipPlane));

                rect.y += rect.height + vSpace;
                EditorGUI.PropertyField(rect, property.FindPropertyRelative(() => s_LensSettingsDef.Dutch));

                if (m_Snapshot.IsPhysical)
                {
                    var physicalProp = property.FindPropertyRelative(() => s_LensSettingsDef.PhysicalProperties);
                    rect.y += rect.height + vSpace;
                    s_PhysicalExapnded = EditorGUI.Foldout(rect, s_PhysicalExapnded, PhysicalPropertiesLabel, true);

                    if (s_PhysicalExapnded)
                    {
                        ++EditorGUI.indentLevel;

                        rect.y += rect.height + vSpace;
                        EditorGUI.PropertyField(rect, physicalProp.FindPropertyRelative(() => s_LensSettingsDef.PhysicalProperties.GateFit));

                        rect.y += rect.height + vSpace;
                        EditorGUI.PropertyField(rect, physicalProp.FindPropertyRelative(() => s_LensSettingsDef.PhysicalProperties.SensorSize));

                        rect.y += rect.height + vSpace;
                        EditorGUI.PropertyField(rect, physicalProp.FindPropertyRelative(() => s_LensSettingsDef.PhysicalProperties.LensShift));
#if CINEMACHINE_HDRP
                        rect.y += rect.height + vSpace;
                        EditorGUI.PropertyField(rect, physicalProp.FindPropertyRelative(() => s_LensSettingsDef.PhysicalProperties.Iso));
                        rect.y += rect.height + vSpace;
                        EditorGUI.PropertyField(rect, physicalProp.FindPropertyRelative(() => s_LensSettingsDef.PhysicalProperties.ShutterSpeed));
                        rect.y += rect.height + vSpace;
                        EditorGUI.PropertyField(rect, physicalProp.FindPropertyRelative(() => s_LensSettingsDef.PhysicalProperties.Aperture));
                        rect.y += rect.height + vSpace;
                        EditorGUI.PropertyField(rect, physicalProp.FindPropertyRelative(() => s_LensSettingsDef.PhysicalProperties.BladeCount));
                        rect.y += rect.height + vSpace;
                        EditorGUI.PropertyField(rect, physicalProp.FindPropertyRelative(() => s_LensSettingsDef.PhysicalProperties.Curvature));
                        rect.y += rect.height + vSpace;
                        EditorGUI.PropertyField(rect, physicalProp.FindPropertyRelative(() => s_LensSettingsDef.PhysicalProperties.BarrelClipping));
                        rect.y += rect.height + vSpace;
                        EditorGUI.PropertyField(rect, physicalProp.FindPropertyRelative(() => s_LensSettingsDef.PhysicalProperties.Anamorphism));
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
                        EditorGUI.PropertyField(rect, property.FindPropertyRelative(() => s_LensSettingsDef.ModeOverride));
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
