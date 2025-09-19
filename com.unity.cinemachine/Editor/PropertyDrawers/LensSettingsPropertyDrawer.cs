using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;
using UnityEngine.UIElements;
using UnityEditor.UIElements;

namespace Unity.Cinemachine.Editor
{
    [CustomPropertyDrawer(typeof(LensSettingsHideModeOverridePropertyAttribute))]
    class LensSettingsHideModeOverridePropertyDrawer : LensSettingsPropertyDrawer
    {
        public LensSettingsHideModeOverridePropertyDrawer() => HideModeOverride = true;
    }

    [CustomPropertyDrawer(typeof(LensSettings))]
    class LensSettingsPropertyDrawer : PropertyDrawer
    {
        static bool IsOrtho(SerializedProperty property) => AccessProperty<bool>(
            typeof(LensSettings), SerializedPropertyHelper.GetPropertyValue(property), "Orthographic");

        static bool IsPhysical(SerializedProperty property) => AccessProperty<bool>(
            typeof(LensSettings), SerializedPropertyHelper.GetPropertyValue(property), "IsPhysicalCamera");

        static float Aspect(SerializedProperty property) => AccessProperty<float>(
            typeof(LensSettings), SerializedPropertyHelper.GetPropertyValue(property), "Aspect");

        static bool UseHorizontalFOV(SerializedProperty property) => AccessProperty<bool>(
            typeof(LensSettings), SerializedPropertyHelper.GetPropertyValue(property), "UseHorizontalFOV");

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

        static List<string> m_PresetOptions;
        static List<string> m_PhysicalPresetOptions;
        const string k_AddPresetsLabel = "New Palette entry with these Settings...";
        const string k_EditPresetsLabel = "Edit Palette...";
        const string k_PaletteLabel = "Palette...";
        float m_PreviousAspect;

        protected bool HideModeOverride { get; set; }

        void InitPresetOptions()
        {
            m_PresetOptions ??= new List<string>();
            m_PresetOptions.Clear();
            var palette = CinemachineLensPalette.InstanceIfExists;
            for (int i = 0; palette != null && i < palette.Presets.Count; ++i)
                m_PresetOptions.Add(palette.Presets[i].Name);
            if (palette != null && palette.Presets.Count > 0)
                m_PresetOptions.Add("");
            m_PresetOptions.Add(k_AddPresetsLabel);
            m_PresetOptions.Add(k_EditPresetsLabel);

            var physicalPresets = CinemachinePhysicalLensPalette.InstanceIfExists;
            m_PhysicalPresetOptions ??= new List<string>();
            m_PhysicalPresetOptions.Clear();
            for (int i = 0; physicalPresets != null && i < physicalPresets.Presets.Count; ++i)
                m_PhysicalPresetOptions.Add(physicalPresets.Presets[i].Name);
            if (physicalPresets != null && physicalPresets.Presets.Count > 0)
                m_PhysicalPresetOptions.Add("");
            m_PhysicalPresetOptions.Add(k_AddPresetsLabel);
            m_PhysicalPresetOptions.Add(k_EditPresetsLabel);
        }

        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            InitPresetOptions();

            var ux = new VisualElement();

            // When foldout is closed, we display the FOV on the same line, for convenience
            var foldout = new Foldout { text = property.displayName, tooltip = property.tooltip };
            foldout.BindProperty(property);

            var outerFovControl = new FovPropertyControl(property, true);
            ux.Add(new InspectorUtility.FoldoutWithOverlay(
                foldout, outerFovControl, outerFovControl.ShortLabel) { style = { flexGrow = 1 }});

            // Populate the foldout
            var innerFovControl = foldout.AddChild(new FovPropertyControl(property, false));

            var nearClip = property.FindPropertyRelative(nameof(LensSettings.NearClipPlane));
            foldout.AddChild(InspectorUtility.PropertyRow(nearClip, out var nearClipField)); // for friendly drag
            nearClipField.RegisterValueChangeCallback((evt) =>
            {
                if (!IsOrtho(property) && nearClip.floatValue < 0.01f)
                {
                    nearClip.floatValue = 0.01f;
                    property.serializedObject.ApplyModifiedPropertiesWithoutUndo();
                }
            });
            foldout.AddChild(InspectorUtility.PropertyRow(
                property.FindPropertyRelative(nameof(LensSettings.FarClipPlane)), out var farClipField)); // for friendly drag
            farClipField.OnInitialGeometry(() => farClipField.SafeSetIsDelayed());
            foldout.Add(new PropertyField(property.FindPropertyRelative(nameof(LensSettings.Dutch))));

            var physical = foldout.AddChild(new PropertyField(property.FindPropertyRelative(nameof(LensSettings.PhysicalProperties))));

            SerializedProperty modeOverrideProperty = null;
            VisualElement modeHelp = null;
            if (!HideModeOverride)
            {
                modeOverrideProperty = property.FindPropertyRelative(nameof(LensSettings.ModeOverride));
                modeHelp = foldout.AddChild(
                    new HelpBox("Lens Mode Override must be enabled in the Cinemachine Brain for Mode Override to take effect",
                        HelpBoxMessageType.Warning));
                foldout.AddChild(new PropertyField(modeOverrideProperty)).TrackPropertyValue(
                    modeOverrideProperty, (p) => InspectorUtility.RepaintGameView());
            }

            // GML: This is rather evil.  Is there a better (event-driven) way?
            DoUpdate();
            ux.schedule.Execute(DoUpdate).Every(250);
            void DoUpdate()
            {
                if (property.serializedObject.targetObject == null)
                    return; // target deleted

                // We need to track the aspect ratio because HFOV display is dependent on it
                var isHorizontal = UseHorizontalFOV(property);
                var aspect = Aspect(property);
                bool aspectChanged = isHorizontal && m_PreviousAspect != aspect;
                m_PreviousAspect = aspect;

                bool isPhysical = IsPhysical(property);
                physical.SetVisible(isPhysical);
                outerFovControl.TimedUpdate(aspectChanged);
                innerFovControl.TimedUpdate(aspectChanged);

                if (!HideModeOverride)
                {
                    var brainHasModeOverride = CinemachineBrain.ActiveBrainCount > 0
                        && CinemachineBrain.GetActiveBrain(0).LensModeOverride.Enabled;
                    modeHelp.SetVisible(!brainHasModeOverride
                        && modeOverrideProperty.intValue != (int)LensSettings.OverrideModes.None);
                }
            };
            // In case presets asset gets deleted or modified externally
            ux.TrackAnyUserActivity(InitPresetOptions);

            return ux;
        }

        /// <summary>
        /// Make the complicated FOV widget which works in 4 modes, with preset popups
        /// and optional weird small label display
        /// </summary>
        class FovPropertyControl : InspectorUtility.LabeledRow
        {
            public readonly Label ShortLabel;
            readonly SerializedProperty m_LensProperty;
            readonly SerializedProperty m_SensorSizeProperty;
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
                style.flexGrow = 1;

                m_LensProperty = property;
                var physicalProp = property.FindPropertyRelative(nameof(LensSettings.PhysicalProperties));
                m_SensorSizeProperty = physicalProp.FindPropertyRelative(nameof(LensSettings.PhysicalProperties.SensorSize));

                m_Control = Contents.AddChild(new FloatField("") { style = { flexBasis = 20, flexGrow = 2, marginLeft = 2 }});
                m_Control.RegisterValueChangedCallback(OnControlValueChanged);
                Label.SetVisible(!hideLabel);
                Label.AddToClassList("unity-base-field__label--with-dragger");
                new DelayedFriendlyFieldDragger<float>(m_Control) { CancelDelayedWhenDragging = true }.SetDragZone(Label);
                m_Control.OnInitialGeometry(() => m_Control.SafeSetIsDelayed());

                m_Presets = Contents.AddChild(new PopupField<string>
                    { tooltip = "Customizable Lens Palette", style = { flexBasis = 20, flexGrow = 1 }});
                m_Presets.RegisterValueChangedCallback(OnPresetValueChanged);

                ShortLabel = new Label("X") { style = { alignSelf = Align.Center, opacity = 0.5f }};
                ShortLabel.AddToClassList("unity-base-field__label--with-dragger");
                new DelayedFriendlyFieldDragger<float>(m_Control) { CancelDelayedWhenDragging = true }.SetDragZone(ShortLabel);

                this.TrackPropertyWithInitialCallback(property, OnLensPropertyChanged);
            }

            void OnControlValueChanged(ChangeEvent<float> evt)
            {
                var mode = GetLensMode();
                switch (mode)
                {
                    case Modes.Ortho:
                    {
                        var orthoProp = m_LensProperty.FindPropertyRelative(nameof(LensSettings.OrthographicSize));
                        orthoProp.floatValue = evt.newValue;
                        orthoProp.serializedObject.ApplyModifiedProperties();
                        break;
                    }
                    case Modes.Physical:
                    {
                        // Convert and clamp
                        var vfov = FocalLengthToFov(evt.newValue);
                        vfov = Mathf.Clamp(vfov, 1, 179);
                        m_Control.SetValueWithoutNotify(FovToFocalLength(vfov));

                        // Push to property
                        var fovProp = m_LensProperty.FindPropertyRelative(nameof(LensSettings.FieldOfView));
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
                            newValue = Camera.HorizontalToVerticalFieldOfView(newValue, Aspect(m_LensProperty));

                        // Push to property
                        var fovProp = m_LensProperty.FindPropertyRelative(nameof(LensSettings.FieldOfView));
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
                        var orthoProp = m_LensProperty.FindPropertyRelative(nameof(LensSettings.OrthographicSize));
                        m_Control.SetValueWithoutNotify(orthoProp.floatValue);
                        break;
                    }
                    case Modes.Physical:
                    {
                        // Convert to display FolcalLength units
                        var fovProp = m_LensProperty.FindPropertyRelative(nameof(LensSettings.FieldOfView));
                        var v = FovToFocalLength(fovProp.floatValue);
                        m_Control.SetValueWithoutNotify(v);

                        // Sync the presets
                        var presets = CinemachinePhysicalLensPalette.InstanceIfExists;
                        var index = presets == null ? -1 : presets.GetMatchingPreset(new ()
                        {
                            FocalLength = v,
                            PhysicalProperties = ReadPhysicalSettings()
                        });
                        m_Presets.SetValueWithoutNotify(index < 0 ? k_PaletteLabel : presets.Presets[index].Name);
                        break;
                    }
                    case Modes.VFOV:
                    case Modes.HFOV:
                    {
                        // Convert to display FOV units
                        var fovProp = m_LensProperty.FindPropertyRelative(nameof(LensSettings.FieldOfView));
                        var v = fovProp.floatValue;
                        if (mode == Modes.HFOV)
                            v = Camera.VerticalToHorizontalFieldOfView(v, Aspect(m_LensProperty));
                        m_Control.SetValueWithoutNotify(v);

                        // Sync the presets
                        var presets = CinemachineLensPalette.InstanceIfExists;
                        var index = presets == null ? -1 : presets.GetMatchingPreset(fovProp.floatValue);
                        m_Presets.SetValueWithoutNotify(index < 0 ? k_PaletteLabel : presets.Presets[index].Name);
                        break;
                    }
                }
                TimedUpdate(false);
            }

            public void TimedUpdate(bool aspectChanged)
            {
                switch (GetLensMode())
                {
                    case Modes.Ortho:
                    {
                        var text = "O";
                        if (ShortLabel.text != text)
                        {
                            var orthoProp = m_LensProperty.FindPropertyRelative(nameof(LensSettings.OrthographicSize));
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
                            var fovProp = m_LensProperty.FindPropertyRelative(nameof(LensSettings.FieldOfView));
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
                            var fovProp = m_LensProperty.FindPropertyRelative(nameof(LensSettings.FieldOfView));
                            ShortLabel.text = text;
                            Label.text = ShortLabel.tooltip = "Horizontal FOV";
                            Label.tooltip = m_Control.tooltip = fovProp.tooltip;
                            m_Presets.choices = m_PresetOptions;
                            m_Presets.SetVisible(true);
                        }
                        // No event-driven way to detect this.  Our display depends on aspect
                        if (aspectChanged)
                            OnLensPropertyChanged(m_LensProperty);
                        break;
                    }
                    case Modes.VFOV:
                    {
                        var text = "V";
                        if (ShortLabel.text != text)
                        {
                            var fovProp = m_LensProperty.FindPropertyRelative(nameof(LensSettings.FieldOfView));
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

            float FocalLengthToFov(float focal) => Camera.FocalLengthToFieldOfView(Mathf.Max(0.01f, focal), m_SensorSizeProperty.vector2Value.y);
            float FovToFocalLength(float vfov) => Camera.FieldOfViewToFocalLength(vfov, m_SensorSizeProperty.vector2Value.y);

            void OnPresetValueChanged(ChangeEvent<string> evt)
            {
                if (GetLensMode() == Modes.Physical)
                {
                    // Physical presets
                    var palette = CinemachinePhysicalLensPalette.Instance;
                    if (palette != null)
                    {
                        // Edit the presets assets if desired
                        if (evt.newValue == k_EditPresetsLabel)
                            Selection.activeObject = palette;
                        else if (evt.newValue == k_AddPresetsLabel)
                        {
                            Selection.activeObject = palette;
                            Undo.RecordObject(palette, "add palette entry");
                            palette.Presets.Add(new ()
                            {
                                Name = $"{m_Control.value}mm preset {palette.Presets.Count + 1}",
                                FocalLength = m_Control.value,
                                PhysicalProperties = ReadPhysicalSettings()
                            });
                        }
                        else
                        {
                            // Apply the preset
                            var index = palette.GetPresetIndex(evt.newValue);
                            if (index >= 0)
                            {
                                var v = palette.Presets[index];
                                m_LensProperty.FindPropertyRelative(nameof(LensSettings.FieldOfView)).floatValue = FocalLengthToFov(v.FocalLength);
                                WritePhysicalSettings(v.PhysicalProperties);
                                m_LensProperty.serializedObject.ApplyModifiedProperties();
                                return;
                            }
                        }
                    }
                }
                else
                {
                    // Nonphysical Presets
                    var palette = CinemachineLensPalette.Instance;
                    if (palette != null)
                    {
                        var fovProp = m_LensProperty.FindPropertyRelative(nameof(LensSettings.FieldOfView));

                        // Edit the presets assets if desired
                        if (evt.newValue == k_EditPresetsLabel)
                            Selection.activeObject = palette;
                        else if (evt.newValue == k_AddPresetsLabel)
                        {
                            Selection.activeObject = palette;
                            Undo.RecordObject(palette, "add palette entry");
                            palette.Presets.Add(new ()
                            {
                                Name = $"{fovProp.floatValue} preset {palette.Presets.Count + 1}",
                                VerticalFOV = fovProp.floatValue,
                            });
                        }
                        else
                        {
                            // Apply the preset
                            var index = palette.GetPresetIndex(evt.newValue);
                            if (index >= 0)
                                fovProp.floatValue = palette.Presets[index].VerticalFOV;
                            m_LensProperty.serializedObject.ApplyModifiedProperties();
                        }
                    }
                }
                m_Presets.SetValueWithoutNotify(k_PaletteLabel);
            }

            LensSettings.PhysicalSettings ReadPhysicalSettings()
            {
                var p = m_LensProperty.FindPropertyRelative(nameof(LensSettings.PhysicalProperties));
                return new ()
                {
                    GateFit = (Camera.GateFitMode)p.FindPropertyRelative(nameof(LensSettings.PhysicalProperties.GateFit)).intValue,
                    SensorSize = p.FindPropertyRelative(nameof(LensSettings.PhysicalProperties.SensorSize)).vector2Value,
                    LensShift = p.FindPropertyRelative(nameof(LensSettings.PhysicalProperties.LensShift)).vector2Value,
                    Iso = p.FindPropertyRelative(nameof(LensSettings.PhysicalProperties.Iso)).intValue,
                    ShutterSpeed = p.FindPropertyRelative(nameof(LensSettings.PhysicalProperties.ShutterSpeed)).floatValue,
                    Aperture = p.FindPropertyRelative(nameof(LensSettings.PhysicalProperties.Aperture)).floatValue,
                    BladeCount = p.FindPropertyRelative(nameof(LensSettings.PhysicalProperties.BladeCount)).intValue,
                    Curvature = p.FindPropertyRelative(nameof(LensSettings.PhysicalProperties.Curvature)).vector2Value,
                    BarrelClipping = p.FindPropertyRelative(nameof(LensSettings.PhysicalProperties.BarrelClipping)).floatValue,
                    Anamorphism =p.FindPropertyRelative(nameof(LensSettings.PhysicalProperties.Anamorphism)).floatValue
                };
            }

            void WritePhysicalSettings(in LensSettings.PhysicalSettings s)
            {
                var p = m_LensProperty.FindPropertyRelative(nameof(LensSettings.PhysicalProperties));
                p.FindPropertyRelative(nameof(LensSettings.PhysicalProperties.GateFit)).intValue = (int)s.GateFit;
                p.FindPropertyRelative(nameof(LensSettings.PhysicalProperties.SensorSize)).vector2Value = s.SensorSize;
                p.FindPropertyRelative(nameof(LensSettings.PhysicalProperties.LensShift)).vector2Value = s.LensShift;
                p.FindPropertyRelative(nameof(LensSettings.PhysicalProperties.Iso)).intValue = s.Iso;
                p.FindPropertyRelative(nameof(LensSettings.PhysicalProperties.ShutterSpeed)).floatValue = s.ShutterSpeed;
                p.FindPropertyRelative(nameof(LensSettings.PhysicalProperties.Aperture)).floatValue = s.Aperture;
                p.FindPropertyRelative(nameof(LensSettings.PhysicalProperties.BladeCount)).intValue = s.BladeCount;
                p.FindPropertyRelative(nameof(LensSettings.PhysicalProperties.Curvature)).vector2Value = s.Curvature;
                p.FindPropertyRelative(nameof(LensSettings.PhysicalProperties.BarrelClipping)).floatValue = s.BarrelClipping;
                p.FindPropertyRelative(nameof(LensSettings.PhysicalProperties.Anamorphism)).floatValue = s.Anamorphism;
            }
        }
    }
}
