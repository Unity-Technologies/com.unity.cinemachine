using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;
using Cinemachine.Utility;
using UnityEngine.UIElements;
using System.Reflection;

namespace Cinemachine.Editor
{
    /// <summary>
    /// Helpers for drawing CmCamera inspectors.
    /// </summary>
    class CmCameraInspectorUtility
    {
        public UnityEngine.Object[] Targets { get; private set; }
        CinemachineVirtualCameraBase Target => Targets.Length == 0 ? null : Targets[0] as CinemachineVirtualCameraBase;

        public bool IsPrefab { get; private set; }

        /// <summary>Call from Inspector's OnEnsable</summary>
        public void OnEnable(UnityEngine.Object[] targets)
        {
            Targets = targets;
            Undo.undoRedoPerformed += UpdateCameraState;
            IsPrefab = Target != null && Target.gameObject.scene.name == null; // causes a small GC alloc
        }

        /// <summary>Call from Inspector's OnDisable</summary>
        public void OnDisable()
        {
            Undo.undoRedoPerformed -= UpdateCameraState;
            if (Target != null && CinemachineBrain.SoloCamera == (ICinemachineCamera)Target)
            {
                CinemachineBrain.SoloCamera = null;
                InspectorUtility.RepaintGameView();
            }
        }
        
        void UpdateCameraState() { if (Target != null) Target.InternalUpdateCameraState(Vector3.up, -1); }


        public void AddCameraStatus(VisualElement ux)
        {
#if false
            // Experimenting with UI Elements layout.  How the !@#$% is this supposed to work?
            var row = new VisualElement();
            row.AddToClassList("unity-property-field__label");
            row.style.flexDirection = FlexDirection.Row;
            var left = new Label("Hello");
            left.AddToClassList("unity-property-field__label");
            //left.style.flexGrow = 1;
            //left.style.flexBasis = 0; // default is auto and uses the text size
            var right = new VisualElement();

            right.Add(new Button() { text = "foofoo" });
            right.Add(new Label("There"));
            right.style.flexDirection = FlexDirection.Row;
            right.style.flexGrow = 1;
            row.Add(left);
            row.Add(right);
            ux.Add(row);

#endif
            // Temporary IMGUI implementation.  GML TODO: how to reimplement this?
            var container = new IMGUIContainer();
            ux.Add(container);
            container.onGUIHandler = () =>
            {
                // No status and Solo for prefabs or multi-select
                if (Selection.objects.Length > 1 || IsPrefab)
                    return;

                // Is the camera navel-gazing?
                CameraState state = Target.State;
                if (state.HasLookAt && (state.ReferenceLookAt - state.CorrectedPosition).AlmostZero())
                    EditorGUILayout.HelpBox(
                        "The camera is positioned on the same point at which it is trying to look.",
                        MessageType.Warning);

                // Active status and Solo button
                Rect rect = EditorGUILayout.GetControlRect(true);
                Rect rectLabel = new Rect(rect.x, rect.y, EditorGUIUtility.labelWidth, rect.height);
                rect.width -= rectLabel.width;
                rect.x += rectLabel.width;

                Color color = GUI.color;
                bool isSolo = (CinemachineBrain.SoloCamera == (ICinemachineCamera)Target);
                if (isSolo)
                    GUI.color = CinemachineBrain.GetSoloGUIColor();

                bool isLive = CinemachineCore.Instance.IsLive(Target);
                GUI.enabled = isLive;
                GUI.Label(rectLabel, isLive ? "Status: Live"
                    : (Target.isActiveAndEnabled ? "Status: Standby" : "Status: Disabled"));
                GUI.enabled = true;

                float labelWidth = 0;
                GUIContent updateText = GUIContent.none;
                UpdateTracker.UpdateClock updateMode = CinemachineCore.Instance.GetVcamUpdateStatus(Target);
                if (Application.isPlaying)
                {
                    updateText = new GUIContent(
                        updateMode == UpdateTracker.UpdateClock.Fixed ? " Fixed Update" : " Late Update");
                    var textDimensions = GUI.skin.label.CalcSize(updateText);
                    labelWidth = textDimensions.x;
                }
                rect.width -= labelWidth;
                if (GUI.Button(rect, "Solo", "Button"))
                {
                    isSolo = !isSolo;
                    CinemachineBrain.SoloCamera = isSolo ? Target : null;
                    InspectorUtility.RepaintGameView();
                }
                GUI.color = color;
                if (isSolo && !Application.isPlaying)
                    InspectorUtility.RepaintGameView();

                if (labelWidth > 0)
                {
                    GUI.enabled = false;
                    rect.x += rect.width; rect.width = labelWidth;
                    GUI.Label(rect, updateText);
                    GUI.enabled = true;
                }
            };
        }

        public void AddPipelineDropdowns(VisualElement ux)
        {
            var cmCam = Target as CmCamera;
            if (cmCam == null)
                return;

            var targets = Targets; // capture for lambda

            // Add a dropdown for each pipeline stage
            for (int i = 0; i < PipelineStageMenu.s_StageData.Length; ++i)
            {
                // Skip empty categories
                if (PipelineStageMenu.s_StageData[i].Types.Count < 2)
                    continue;

                int currentSelection = PipelineStageMenu.GetSelectedComponent(
                    i, cmCam.GetCinemachineComponent((CinemachineCore.Stage)i));
                var stage = i; // capture for lambda
                var dropdown = new DropdownField
                {
                    name = PipelineStageMenu.s_StageData[stage].Name + " selector",
                    label = PipelineStageMenu.s_StageData[stage].Name,
                    choices = PipelineStageMenu.s_StageData[stage].Choices,
                    index = currentSelection
                };
                dropdown.RegisterValueChangedCallback((evt) => 
                {
                    var newType = PipelineStageMenu.s_StageData[stage].Types[GetTypeIndexFromSelection(evt.newValue, stage)];
                    for (int i = 0; i < targets.Length; i++)
                    {
                        var t = targets[i] as CmCamera;
                        if (t == null)
                            continue;
                        var oldComponent = t.GetCinemachineComponent((CinemachineCore.Stage)stage);
                        var oldType = oldComponent == null ? null : oldComponent.GetType();
                        if (newType != oldType)
                        {
                            t.PipelineChanged();
                            if (oldComponent != null)
                                Undo.DestroyObjectImmediate(oldComponent);
                            if (newType != null)
                                Undo.AddComponent(t.gameObject, newType);
                        }
                    }            
                    int GetTypeIndexFromSelection(string selection, int stage)
                    {
                        for (var j = 0; j < PipelineStageMenu.s_StageData[stage].Choices.Count; ++j)
                            if (PipelineStageMenu.s_StageData[stage].Choices[j].Equals(selection))
                                return j;
                        return 0;
                    }
                });
                ux.Add(dropdown);
            }
        }

        /// <summary>Draw the Extensions dropdown in the inspector</summary>
        public void AddExtensionsDropdown(VisualElement ux)
        {
            var cmCam = Target;
            var dropdown = new DropdownField
            {
                name = "extensions selector",
                label = "Extensions",
                choices = PipelineStageMenu.s_ExtentionNames,
                index = 0
            };
            dropdown.RegisterValueChangedCallback((evt) => 
            {
                Type extType = PipelineStageMenu.s_ExtentionTypes[GetTypeIndexFromSelection(evt.newValue)];
                for (int i = 0; i < Targets.Length; i++)
                {
                    var targetGO = (Targets[i] as CinemachineVirtualCameraBase).gameObject;
                    if (targetGO != null && targetGO.GetComponent(extType) == null)
                        Undo.AddComponent(targetGO, extType);
                }
            
                int GetTypeIndexFromSelection(string selection)
                {
                    for (var j = 0; j < PipelineStageMenu.s_ExtentionNames.Count; ++j)
                        if (PipelineStageMenu.s_ExtentionNames[j].Equals(selection))
                            return j;
                    return 0;
                }
            });
            ux.Add(dropdown);
        }
        
        [InitializeOnLoad]
        static class PipelineStageMenu
        {
            // Pipeline stages
            public struct StageData
            {
                public CinemachineCore.Stage Stage;
                public string Name;
                public List<Type> Types;   // first entry is null - this array is synched with PopupOptions
                public List<string> Choices;
            }
            public static StageData[] s_StageData = null;
            
            // Extensions
            public static List<Type> s_ExtentionTypes;
            public static List<string> s_ExtentionNames;
        
            public static int GetSelectedComponent(int stage, CinemachineComponentBase component)
            {
                if (component != null)
                    for (int j = 0; j < s_StageData[stage].Choices.Count; ++j)
                        if (s_StageData[stage].Types[j] == component.GetType())
                            return j;
                return 0;
            }

            // This code dynamically discovers eligible classes and builds the menu
            // data for the various component pipeline stages.
            static PipelineStageMenu()
            {
                s_StageData = new StageData[Enum.GetValues(typeof(CinemachineCore.Stage)).Length];
                for (int i = 0; i < s_StageData.Length; ++i)
                {
                    var stage = (CinemachineCore.Stage)i;
                    s_StageData[i] = new StageData
                    {
                        Stage = stage,
                        Name = stage.ToString(),
                        Types = new List<Type>() { null }, // first item is "none"
                        Choices = new List<string>() 
                            { (stage == CinemachineCore.Stage.Aim || stage == CinemachineCore.Stage.Body) ? "Do nothing" : "none" }
                    };
                }

                // Get all CinemachineComponentBase
                var allTypes = ReflectionHelpers.GetTypesInAllDependentAssemblies((Type t) => 
                    typeof(CinemachineComponentBase).IsAssignableFrom(t) && !t.IsAbstract 
                    && t.GetCustomAttribute<CameraPipelineAttribute>() != null
                    && t.GetCustomAttribute<ObsoleteAttribute>() == null);

                foreach (var t in allTypes)
                {
                    var stage = (int)t.GetCustomAttribute<CameraPipelineAttribute>().Stage;
                    s_StageData[stage].Types.Add(t);
                    s_StageData[stage].Choices.Add(InspectorUtility.NicifyClassName(t.Name));
                }

                // Populate the extension list
                s_ExtentionTypes = new List<Type>();
                s_ExtentionNames = new List<string>();
                s_ExtentionTypes.Add(null);
                s_ExtentionNames.Add("(select)");
                var allExtensions
                    = ReflectionHelpers.GetTypesInAllDependentAssemblies(
                            (Type t) => typeof(CinemachineExtension).IsAssignableFrom(t) && !t.IsAbstract);
                foreach (Type t in allExtensions)
                {
                    s_ExtentionTypes.Add(t);
                    s_ExtentionNames.Add(t.Name);
                }
            }
        }
        
        
        static List<MonoBehaviour> s_componentCache = new List<MonoBehaviour>();
        enum SortOrder { None, Camera, Pipeline, Extensions = CinemachineCore.Stage.Finalize + 1, Other };

        /// <summary>
        /// This is only for aesthetics, sort order does not affect camera logic.
        /// Behaviours should be sorted like this:
        /// CmCamera, Body, Aim, Noise, Finalize, Extensions, everything else.
        /// </summary>
        public void SortComponents()
        {
            SortOrder lastItem = SortOrder.None;
            bool sortNeeded = false;
            Target.gameObject.GetComponents(s_componentCache);
            for (int i = 0; i < s_componentCache.Count && !sortNeeded; ++i)
            {
                var current = GetSortOrderForComponent(s_componentCache[i]);
                if (current < lastItem)
                    sortNeeded = true;
                lastItem = current;
            }
            if (sortNeeded)
            {
                // This is painful, but it won't happen too often
                var pos = 0;
                if (MoveComponentToPosition(pos, SortOrder.Camera, s_componentCache)) ++pos;
                if (MoveComponentToPosition(pos, SortOrder.Pipeline + (int)CinemachineCore.Stage.Body, s_componentCache)) ++pos;
                if (MoveComponentToPosition(pos, SortOrder.Pipeline + (int)CinemachineCore.Stage.Aim, s_componentCache)) ++pos;
                if (MoveComponentToPosition(pos, SortOrder.Pipeline + (int)CinemachineCore.Stage.Noise, s_componentCache)) ++pos;
                MoveComponentToPosition(pos, SortOrder.Pipeline + (int)CinemachineCore.Stage.Finalize, s_componentCache);
                // leave everything else where it is
            }

            SortOrder GetSortOrderForComponent(MonoBehaviour component)
            {
                if (component is CinemachineVirtualCameraBase)
                    return SortOrder.Camera;
                if (component is CinemachineExtension)
                    return SortOrder.Extensions;
                if (component is CinemachineComponentBase)
                    return SortOrder.Pipeline + (int)(component as CinemachineComponentBase).Stage;
                return SortOrder.Other;
            }
        
            // Returns true if item exists.  Will re-sort components if something changed.
            bool MoveComponentToPosition(int pos, SortOrder item, List<MonoBehaviour> components)
            {
                for (int i = pos; i < components.Count; ++i)
                {
                    var component = components[i];
                    if (GetSortOrderForComponent(component) == item)
                    {
                        for (int j = i; j > pos; --j)
                            UnityEditorInternal.ComponentUtility.MoveComponentUp(component);
                        if (i > pos)
                            component.gameObject.GetComponents(components);
                        return true;
                    }
                }
                return false;
            }
        }
        
        /// <summary>
        /// Draw the global settings controls in the inspector
        /// </summary>
        public void AddSaveDuringPlayToggle(VisualElement ux)
        {
            var helpBox = new HelpBox(" Virtual Camera settings changes made during Play Mode will be "
                    + "propagated back to the scene when Play Mode is exited.", 
                HelpBoxMessageType.Info);
            helpBox.style.display = (SaveDuringPlay.SaveDuringPlay.Enabled && Application.isPlaying) 
                ? DisplayStyle.Flex : DisplayStyle.None;

            var toggle = new Toggle("Save During Play");
            toggle.tooltip = "If checked, Virtual Camera settings changes made during Play Mode "
                + "will be propagated back to the scene when Play Mode is exited.";
            toggle.value = SaveDuringPlay.SaveDuringPlay.Enabled;
            toggle.RegisterValueChangedCallback((evt) => 
            {
                SaveDuringPlay.SaveDuringPlay.Enabled = evt.newValue;
                helpBox.style.display = (evt.newValue && Application.isPlaying) 
                    ? DisplayStyle.Flex : DisplayStyle.None;
            });
            ux.Add(toggle);
            ux.Add(helpBox);
        }

        /// <summary>
        /// Draw the global settings controls in the inspector
        /// </summary>
        public void AddGameViewGuidesToggle(VisualElement ux)
        {
            var toggle = new Toggle("Game View Guides");
            toggle.tooltip = "Enable the display of overlays in the Game window.  "
                + "You can adjust colours and opacity in Cinemachine Preferences.";
            toggle.value = CinemachineSettings.CinemachineCoreSettings.ShowInGameGuides;
            toggle.RegisterValueChangedCallback((evt) => CinemachineSettings.CinemachineCoreSettings.ShowInGameGuides = evt.newValue);
            ux.Add(toggle);
        }

#if false
        LensSettingsInspectorHelper m_LensSettingsInspectorHelper;
        internal bool IsHorizontalFOVUsed() => 
            m_LensSettingsInspectorHelper != null && m_LensSettingsInspectorHelper.UseHorizontalFOV;

        /// <summary>
        /// Draw the Lens Settings controls in the inspector
        /// </summary>
        /// <param name="property">The SerializedProperty for the field of type LensSettings field</param>
        protected void DrawLensSettingsInInspector(SerializedProperty property)
        {
            if (IsPropertyExcluded(property.name))
                return;
            if (m_LensSettingsInspectorHelper == null)
                m_LensSettingsInspectorHelper = new LensSettingsInspectorHelper();

            // This should be a global UX setting, but the best we can do now is to query 
            // the associated camera (if any) for the lens FOV display mode
            Camera camera = null;
            var brain = CinemachineCore.Instance.FindPotentialTargetBrain(Target);
            if (brain != null)
                camera = brain.OutputCamera;
            
            m_LensSettingsInspectorHelper.SnapshotCameraShadowValues(property, camera);

            m_LensSettingsInspectorHelper.DrawLensSettingsInInspector(property);
            ExcludeProperty(property.name);
        }
    }

    // Helper for drawing lensSettings in inspector
    class LensSettingsInspectorHelper
    {
        GUIContent[] m_PresetOptions;
        GUIContent[] m_PhysicalPresetOptions;
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
        internal bool UseHorizontalFOV;
        static bool s_AdvancedExpanded;
        SerializedProperty ModeOverrideProperty;

    #if CINEMACHINE_HDRP
        GUIContent PhysicalPropertiesLabel;
        static bool mPhysicalExapnded;
    #endif

        public LensSettingsInspectorHelper() 
        {
    #if CINEMACHINE_HDRP
            PhysicalPropertiesLabel = new GUIContent("Physical Properties", "Physical properties of the lens");
    #endif

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

        public void SnapshotCameraShadowValues(SerializedProperty property, Camera camera)
        {
            ModeOverrideProperty = property.FindPropertyRelative(() => m_LensSettingsDef.ModeOverride);

            // Assume lens is up-to-date
            UseHorizontalFOV = false;
            var lensObject = SerializedPropertyHelper.GetPropertyValue(property);
            IsOrtho = AccessProperty<bool>(typeof(LensSettings), lensObject, "Orthographic");
            IsPhysical = AccessProperty<bool>(typeof(LensSettings), lensObject, "IsPhysicalCamera");
            SensorSize = AccessProperty<Vector2>(typeof(LensSettings), lensObject, "SensorSize");

            // Then pull from actual camera if appropriate
            if (camera != null)
            {
#if UNITY_2019_1_OR_NEWER
                // This should really be a global setting, but for now there is no better way than this!
                var p = new SerializedObject(camera).FindProperty("m_FOVAxisMode");
                if (p != null && p.intValue == (int)Camera.FieldOfViewAxis.Horizontal)
                    UseHorizontalFOV = true;
#endif
                // It's possible that the lens isn't synched with its camera - fix that here
                if (ModeOverrideProperty.intValue == (int)LensSettings.OverrideModes.None)
                {
                    IsOrtho = camera.orthographic;
                    IsPhysical = camera.usePhysicalProperties;
                    SensorSize = IsPhysical ? camera.sensorSize : new Vector2(camera.aspect, 1f);
                }
            }
            
            var nearClipPlaneProperty = property.FindPropertyRelative("NearClipPlane");
            if (!IsOrtho)
            {
                nearClipPlaneProperty.floatValue = Mathf.Max(nearClipPlaneProperty.floatValue, 0.01f);
                property.serializedObject.ApplyModifiedPropertiesWithoutUndo();
            }
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

            System.Reflection.PropertyInfo pi = type.GetProperty(memberName, bindingFlags);
            if ((pi != null) && (pi.PropertyType == typeof(T)))
                return (T)pi.GetValue(obj, null);
            else
                return default(T);
        }
        
        public void DrawLensSettingsInInspector(SerializedProperty property)
        {
            Rect rect = EditorGUILayout.GetControlRect(true);

            var lensLabelWidth = GUI.skin.label.CalcSize(LensLabel).x;
            var foldoutLabelWidth = lensLabelWidth;

            property.isExpanded = EditorGUI.Foldout(
                new Rect(rect.x, rect.y, foldoutLabelWidth, rect.height),
                property.isExpanded, LensLabel, true);

            if (!property.isExpanded)
            {
                // Put the FOV on the same line
                var oldLabelWidth = EditorGUIUtility.labelWidth;
                EditorGUIUtility.labelWidth -= foldoutLabelWidth;
                rect.x += foldoutLabelWidth; rect.width -= foldoutLabelWidth; 
                DrawLensFocusInInspector(rect, property);
                EditorGUIUtility.labelWidth = oldLabelWidth;
            }
            else
            {
                ++EditorGUI.indentLevel;

                rect = EditorGUILayout.GetControlRect(true);
                DrawLensFocusInInspector(rect, property);

                EditorGUILayout.PropertyField(property.FindPropertyRelative(() => m_LensSettingsDef.NearClipPlane));
                EditorGUILayout.PropertyField(property.FindPropertyRelative(() => m_LensSettingsDef.FarClipPlane));

                if (IsPhysical)
                {
#if CINEMACHINE_HDRP
                    mPhysicalExapnded = EditorGUILayout.Foldout(mPhysicalExapnded, PhysicalPropertiesLabel, true);
                    if (mPhysicalExapnded)
                    {
                        ++EditorGUI.indentLevel;
                        EditorGUILayout.PropertyField(property.FindPropertyRelative(() => m_LensSettingsDef.Aperture));
                        EditorGUILayout.PropertyField(property.FindPropertyRelative(() => m_LensSettingsDef.Iso));
                        EditorGUILayout.PropertyField(property.FindPropertyRelative(() => m_LensSettingsDef.ShutterSpeed));
                        EditorGUILayout.PropertyField(property.FindPropertyRelative(() => m_LensSettingsDef.BladeCount));

                        rect = EditorGUILayout.GetControlRect(true);
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

                        EditorGUILayout.PropertyField(property.FindPropertyRelative(() => m_LensSettingsDef.BarrelClipping));
                        EditorGUILayout.PropertyField(property.FindPropertyRelative(() => m_LensSettingsDef.Anamorphism));

                        DrawSensorSizeInInspector(property);
                        EditorGUILayout.PropertyField(property.FindPropertyRelative(() => m_LensSettingsDef.LensShift));
                        if (ModeOverrideProperty.intValue != (int)LensSettings.OverrideModes.None)
                            EditorGUILayout.PropertyField(property.FindPropertyRelative(() => m_LensSettingsDef.GateFit));

                        --EditorGUI.indentLevel;
                    }
#else
                    DrawSensorSizeInInspector(property);
                    EditorGUILayout.PropertyField(property.FindPropertyRelative(() => m_LensSettingsDef.LensShift));
                    if (ModeOverrideProperty.intValue != (int)LensSettings.OverrideModes.None)
                        EditorGUILayout.PropertyField(property.FindPropertyRelative(() => m_LensSettingsDef.GateFit));
#endif
                }
                EditorGUILayout.PropertyField(property.FindPropertyRelative(() => m_LensSettingsDef.Dutch));
                s_AdvancedExpanded = EditorGUILayout.Foldout(s_AdvancedExpanded, AdvancedLabel);
                if (s_AdvancedExpanded)
                {
                    ++EditorGUI.indentLevel;
                    EditorGUILayout.HelpBox("Setting a mode override here implies changes to the Camera component when "
                        + "Cinemachine activates this Virtual Camera, and the changes will remain after the Virtual "
                        + "Camera deactivation. If you set a mode override in any Virtual Camera, you should set "
                        + "one in all Virtual Cameras.", MessageType.Info);
                    EditorGUILayout.PropertyField(ModeOverrideProperty);
                    --EditorGUI.indentLevel;
                }
                --EditorGUI.indentLevel;
            }
            property.serializedObject.ApplyModifiedProperties();
        }

        static float ExtraSpaceHackWTF() { return EditorGUI.indentLevel * (EditorGUIUtility.singleLineHeight - 3); }

        void DrawSensorSizeInInspector(SerializedProperty property)
        {
            if (ModeOverrideProperty.intValue != (int)LensSettings.OverrideModes.None)
            {
                property = property.FindPropertyRelative("m_SensorSize");
                var rect = EditorGUILayout.GetControlRect(true);
                EditorGUI.BeginProperty(rect, SensorSizeLabel, property);
                var v = EditorGUI.Vector2Field(rect, SensorSizeLabel, property.vector2Value);
                v.x = Mathf.Max(v.x, 0.1f);
                v.y = Mathf.Max(v.y, 0.1f);
                property.vector2Value = v;
                EditorGUI.EndProperty();
            }
        }

        void DrawLensFocusInInspector(Rect rect, SerializedProperty property)
        {
            if (IsOrtho)
                EditorGUI.PropertyField(
                    rect, property.FindPropertyRelative(() => m_LensSettingsDef.OrthographicSize), OrthoSizeLabel);
            else if (IsPhysical)
                DrawFocalLengthControl(rect, property, FocalLengthLabel);
            else
                DrawFOVControl(rect, property);
        }

        void DrawFOVControl(Rect rect, SerializedProperty property)
        {
            const float hSpace = 2;
            var label = UseHorizontalFOV ? HFOVLabel : VFOVLabel;

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
#endif
    }
}

