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
        static LensSettings s_Def = new (); // to access name strings

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
        const string k_AddPresetsLabel = "New Preset with these Settings...";
        const string k_EditPresetsLabel = "Edit Presets...";
        float m_PreviousAspect;

        protected bool HideModeOverride { get; set; }

        void InitPresetOptions()
        {
            m_PresetOptions ??= new List<string>();
            m_PresetOptions.Clear();
            var presets = CinemachineLensPresets.InstanceIfExists;
            for (int i = 0; presets != null && i < presets.Presets.Count; ++i)
                m_PresetOptions.Add(presets.Presets[i].Name);
            m_PresetOptions.Add("");
            m_PresetOptions.Add(k_AddPresetsLabel);
            m_PresetOptions.Add(k_EditPresetsLabel);

            var physicalPresets = CinemachinePhysicalLensPresets.InstanceIfExists;
            m_PhysicalPresetOptions ??= new List<string>();
            m_PhysicalPresetOptions.Clear();
            for (int i = 0; physicalPresets != null && i < physicalPresets.Presets.Count; ++i)
                m_PhysicalPresetOptions.Add(physicalPresets.Presets[i].Name);
            m_PhysicalPresetOptions.Add("");
            m_PhysicalPresetOptions.Add(k_AddPresetsLabel);
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

            var nearClip = property.FindPropertyRelative(() => s_Def.NearClipPlane);
            foldout.AddChild(new PropertyField(nearClip)).RegisterValueChangeCallback((evt) =>
            {
                if (!IsOrtho(property) && nearClip.floatValue < 0.01f)
                {
                    nearClip.floatValue = 0.01f;
                    property.serializedObject.ApplyModifiedPropertiesWithoutUndo();
                }
            });
            foldout.Add(new PropertyField(property.FindPropertyRelative(() => s_Def.FarClipPlane)));
            foldout.Add(new PropertyField(property.FindPropertyRelative(() => s_Def.Dutch)));

            var physical = foldout.AddChild(new PropertyField(property.FindPropertyRelative(() => s_Def.PhysicalProperties)));

            SerializedProperty modeOverrideProperty = null;
            VisualElement modeHelp = null;
            if (!HideModeOverride)
            {
                modeOverrideProperty = property.FindPropertyRelative(() => s_Def.ModeOverride);
                modeHelp = foldout.AddChild(
                    new HelpBox("Lens Mode Override must be enabled in the CM Brain for Mode Override to take effect", 
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
                    var brainHasModeOverride = CinemachineCore.BrainCount > 0 
                        && CinemachineCore.GetActiveBrain(0).LensModeOverride.Enabled;
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
                style.flexDirection = FlexDirection.Row;

                m_LensProperty = property;
                var physicalProp = property.FindPropertyRelative(() => s_Def.PhysicalProperties);
                m_SensorSizeProperty = physicalProp.FindPropertyRelative(() => s_Def.PhysicalProperties.SensorSize);

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
                        var orthoProp = m_LensProperty.FindPropertyRelative(() => s_Def.OrthographicSize);
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
                        var fovProp = m_LensProperty.FindPropertyRelative(() => s_Def.FieldOfView);
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
                        var fovProp = m_LensProperty.FindPropertyRelative(() => s_Def.FieldOfView);
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
                        var orthoProp = m_LensProperty.FindPropertyRelative(() => s_Def.OrthographicSize);
                        m_Control.SetValueWithoutNotify(orthoProp.floatValue);
                        break;
                    }
                    case Modes.Physical:
                    {
                        // Convert to display FolcalLength units
                        var fovProp = m_LensProperty.FindPropertyRelative(() => s_Def.FieldOfView);
                        var v = FovToFocalLength(fovProp.floatValue);
                        m_Control.SetValueWithoutNotify(v);

                        // Sync the presets
                        var presets = CinemachinePhysicalLensPresets.InstanceIfExists;
                        var index = presets == null ? -1 : presets.GetMatchingPreset(new ()
                        {
                            FocalLength = v,
                            PhysicalProperties = ReadPhysicalSettings()
                        });
                        m_Presets.SetValueWithoutNotify(index < 0 ? string.Empty : presets.Presets[index].Name);
                        break;
                    }
                    case Modes.VFOV:
                    case Modes.HFOV:
                    {
                        // Convert to display FOV units
                        var fovProp = m_LensProperty.FindPropertyRelative(() => s_Def.FieldOfView);
                        var v = fovProp.floatValue;
                        if (mode == Modes.HFOV)
                            v = Camera.VerticalToHorizontalFieldOfView(v, Aspect(m_LensProperty));
                        m_Control.SetValueWithoutNotify(v);

                        // Sync the presets
                        var presets = CinemachineLensPresets.InstanceIfExists;
                        var index = presets == null ? -1 : presets.GetMatchingPreset(fovProp.floatValue);
                        m_Presets.SetValueWithoutNotify(index < 0 ? string.Empty : presets.Presets[index].Name);
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
                            var orthoProp = m_LensProperty.FindPropertyRelative(() => s_Def.OrthographicSize);
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
                            var fovProp = m_LensProperty.FindPropertyRelative(() => s_Def.FieldOfView);
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
                            var fovProp = m_LensProperty.FindPropertyRelative(() => s_Def.FieldOfView);
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
                            var fovProp = m_LensProperty.FindPropertyRelative(() => s_Def.FieldOfView);
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
                    var presets = CinemachinePhysicalLensPresets.Instance;
                    if (presets != null)
                    {
                        // Edit the presets assets if desired
                        if (evt.newValue == k_EditPresetsLabel)
                            Selection.activeObject = presets;
                        else if (evt.newValue == k_AddPresetsLabel)
                        {
                            Selection.activeObject = presets;
                            Undo.RecordObject(presets, "add preset");
                            presets.Presets.Add(new ()
                            {
                                Name = $"{m_Control.value}mm preset {presets.Presets.Count + 1}",
                                FocalLength = m_Control.value,
                                PhysicalProperties = ReadPhysicalSettings()
                            });
                        }
                        else 
                        {
                            // Apply the preset
                            var index = presets.GetPresetIndex(evt.newValue);
                            if (index >= 0)
                            {
                                var v = presets.Presets[index];
                                m_LensProperty.FindPropertyRelative(() => s_Def.FieldOfView).floatValue = FocalLengthToFov(v.FocalLength);
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
                    var presets = CinemachineLensPresets.Instance;
                    if (presets != null)
                    {
                        var fovProp = m_LensProperty.FindPropertyRelative(() => s_Def.FieldOfView);

                        // Edit the presets assets if desired
                        if (evt.newValue == k_EditPresetsLabel)
                            Selection.activeObject = presets;
                        else if (evt.newValue == k_AddPresetsLabel)
                        {
                            Selection.activeObject = presets;
                            Undo.RecordObject(presets, "add preset");
                            presets.Presets.Add(new ()
                            {
                                Name = $"{fovProp.floatValue} preset {presets.Presets.Count + 1}",
                                VerticalFOV = fovProp.floatValue,
                            });
                        }
                        else 
                        {
                            // Apply the preset
                            var index = presets.GetPresetIndex(evt.newValue);
                            if (index >= 0)
                                fovProp.floatValue = presets.Presets[index].VerticalFOV;
                            m_LensProperty.serializedObject.ApplyModifiedProperties();
                        }
                    }
                }
                m_Presets.SetValueWithoutNotify(string.Empty);
            }

            LensSettings.PhysicalSettings ReadPhysicalSettings()
            {
                var p = m_LensProperty.FindPropertyRelative(() => s_Def.PhysicalProperties);
                return new ()
                {
                    GateFit = (Camera.GateFitMode)p.FindPropertyRelative(() => s_Def.PhysicalProperties.GateFit).intValue,
                    SensorSize = p.FindPropertyRelative(() => s_Def.PhysicalProperties.SensorSize).vector2Value,
                    LensShift = p.FindPropertyRelative(() => s_Def.PhysicalProperties.LensShift).vector2Value,
                    Iso = p.FindPropertyRelative(() => s_Def.PhysicalProperties.Iso).intValue,
                    ShutterSpeed = p.FindPropertyRelative(() => s_Def.PhysicalProperties.ShutterSpeed).floatValue,
                    Aperture = p.FindPropertyRelative(() => s_Def.PhysicalProperties.Aperture).floatValue,
                    BladeCount = p.FindPropertyRelative(() => s_Def.PhysicalProperties.BladeCount).intValue,
                    Curvature = p.FindPropertyRelative(() => s_Def.PhysicalProperties.Curvature).vector2Value,
                    BarrelClipping = p.FindPropertyRelative(() => s_Def.PhysicalProperties.BarrelClipping).floatValue,
                    Anamorphism =p.FindPropertyRelative(() => s_Def.PhysicalProperties.Anamorphism).floatValue
                };
            }

            void WritePhysicalSettings(in LensSettings.PhysicalSettings s)
            {
                var p = m_LensProperty.FindPropertyRelative(() => s_Def.PhysicalProperties);
                p.FindPropertyRelative(() => s_Def.PhysicalProperties.GateFit).intValue = (int)s.GateFit;
                p.FindPropertyRelative(() => s_Def.PhysicalProperties.SensorSize).vector2Value = s.SensorSize;
                p.FindPropertyRelative(() => s_Def.PhysicalProperties.LensShift).vector2Value = s.LensShift;
                p.FindPropertyRelative(() => s_Def.PhysicalProperties.Iso).intValue = s.Iso;
                p.FindPropertyRelative(() => s_Def.PhysicalProperties.ShutterSpeed).floatValue = s.ShutterSpeed;
                p.FindPropertyRelative(() => s_Def.PhysicalProperties.Aperture).floatValue = s.Aperture;
                p.FindPropertyRelative(() => s_Def.PhysicalProperties.BladeCount).intValue = s.BladeCount;
                p.FindPropertyRelative(() => s_Def.PhysicalProperties.Curvature).vector2Value = s.Curvature;
                p.FindPropertyRelative(() => s_Def.PhysicalProperties.BarrelClipping).floatValue = s.BarrelClipping;
                p.FindPropertyRelative(() => s_Def.PhysicalProperties.Anamorphism).floatValue = s.Anamorphism;
            }
        }

#if true
        ///===========================================================================
        /// IMGUI IMPLEMENTATION (to be removed) 
        ///===========================================================================

        static readonly GUIContent EditPresetsLabel = new ("Edit Presets...");
        static readonly GUIContent HFOVLabel = new ("Horizontal FOV", "Horizontal Field of View");
        static readonly GUIContent VFOVLabel = new ("Vertical FOV", "Vertical Field of View");
        static readonly GUIContent FocalLengthLabel = new ("Focal Length", "The length of the lens (in mm)");
        static readonly GUIContent OrthoSizeLabel = new ("Ortho Size", "When using an orthographic camera, "
            + "this defines the half-height, in world coordinates, of the camera view.");
        static readonly GUIContent s_EmptyContent = new (" ");
        static readonly GUIContent AdvancedLabel = new ("Advanced");
        static readonly string AdvancedHelpboxMessage = "Lens Mode Override must be enabled in the CM Brain for Mode Override to take effect";

        static bool s_AdvancedLensExpanded;

        struct Snapshot
        {
            public bool IsOrtho;
            public bool IsPhysical;
            public float Aspect;
            public bool UseHorizontalFOV;
        }
        Snapshot m_Snapshot;

        const float vSpace= 2;
        const float hSpace = 2;

        void SnapshotCameraShadowValues(SerializedProperty property)
        {
            // Assume lens is up-to-date
            m_Snapshot.UseHorizontalFOV = UseHorizontalFOV(property);
            m_Snapshot.IsOrtho = IsOrtho(property);
            m_Snapshot.IsPhysical = IsPhysical(property);
            m_Snapshot.Aspect = Aspect(property);
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
                    rect, property.FindPropertyRelative(() => s_Def.OrthographicSize), label);
            else if (m_Snapshot.IsPhysical)
                DrawFocalLengthControl(rect, property, label);
            else
            {
                var FOVProperty = property.FindPropertyRelative(() => s_Def.FieldOfView);
                float aspect = m_Snapshot.Aspect;
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
            }
        }

        void DrawFocalLengthControl(Rect rect, SerializedProperty property, GUIContent label)
        {
            var FOVProperty = property.FindPropertyRelative(() => s_Def.FieldOfView);
            var physicalProp = property.FindPropertyRelative(() => s_Def.PhysicalProperties);
            var sensorSizeProp = physicalProp.FindPropertyRelative(() => s_Def.PhysicalProperties.SensorSize);

            float f = Camera.FieldOfViewToFocalLength(FOVProperty.floatValue, sensorSizeProp.vector2Value.y);
            EditorGUI.BeginProperty(rect, label, FOVProperty);
            f = EditorGUI.FloatField(rect, label, f);
            f = Camera.FocalLengthToFieldOfView(Mathf.Max(0.01f, f), sensorSizeProp.vector2Value.y);
            if (!Mathf.Approximately(FOVProperty.floatValue, f))
                FOVProperty.floatValue = Mathf.Clamp(f, 1, 179);
            EditorGUI.EndProperty();
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
                var nearClip = property.FindPropertyRelative(() => s_Def.NearClipPlane);
                EditorGUI.PropertyField(rect, nearClip);
                if (!m_Snapshot.IsOrtho && nearClip.floatValue < 0.01f)
                {
                    nearClip.floatValue = 0.01f;
                    property.serializedObject.ApplyModifiedPropertiesWithoutUndo();
                }
                rect.y += rect.height + vSpace;
                EditorGUI.PropertyField(rect, property.FindPropertyRelative(() => s_Def.FarClipPlane));

                rect.y += rect.height + vSpace;
                EditorGUI.PropertyField(rect, property.FindPropertyRelative(() => s_Def.Dutch));

                if (m_Snapshot.IsPhysical)
                {
                    var physicalProp = property.FindPropertyRelative(() => s_Def.PhysicalProperties);
                    rect.y += rect.height + vSpace;
                    physicalProp.isExpanded = EditorGUI.Foldout(rect, physicalProp.isExpanded, physicalProp.displayName, true);
                    if (physicalProp.isExpanded)
                    {
                        ++EditorGUI.indentLevel;
                        rect.y += rect.height + vSpace;
                        EditorGUI.PropertyField(rect, physicalProp.FindPropertyRelative(() => s_Def.PhysicalProperties.GateFit));
                        rect.y += rect.height + vSpace;
                        EditorGUI.PropertyField(rect, physicalProp.FindPropertyRelative(() => s_Def.PhysicalProperties.SensorSize));
                        rect.y += rect.height + vSpace;
                        EditorGUI.PropertyField(rect, physicalProp.FindPropertyRelative(() => s_Def.PhysicalProperties.LensShift));
                        rect.y += rect.height + vSpace;
                        EditorGUI.PropertyField(rect, physicalProp.FindPropertyRelative(() => s_Def.PhysicalProperties.Iso));
                        rect.y += rect.height + vSpace;
                        EditorGUI.PropertyField(rect, physicalProp.FindPropertyRelative(() => s_Def.PhysicalProperties.ShutterSpeed));
                        rect.y += rect.height + vSpace;
                        EditorGUI.PropertyField(rect, physicalProp.FindPropertyRelative(() => s_Def.PhysicalProperties.Aperture));
                        rect.y += rect.height + vSpace;
                        EditorGUI.PropertyField(rect, physicalProp.FindPropertyRelative(() => s_Def.PhysicalProperties.BladeCount));
                        rect.y += rect.height + vSpace;
                        EditorGUI.PropertyField(rect, physicalProp.FindPropertyRelative(() => s_Def.PhysicalProperties.Curvature));
                        rect.y += rect.height + vSpace;
                        EditorGUI.PropertyField(rect, physicalProp.FindPropertyRelative(() => s_Def.PhysicalProperties.BarrelClipping));
                        rect.y += rect.height + vSpace;
                        EditorGUI.PropertyField(rect, physicalProp.FindPropertyRelative(() => s_Def.PhysicalProperties.Anamorphism));
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
                        EditorGUI.PropertyField(rect, property.FindPropertyRelative(() => s_Def.ModeOverride));
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
                var physicalProp = property.FindPropertyRelative(() => s_Def.PhysicalProperties);
                if (physicalProp.isExpanded)
                    numLines += 10;
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
#endif
    }
}
