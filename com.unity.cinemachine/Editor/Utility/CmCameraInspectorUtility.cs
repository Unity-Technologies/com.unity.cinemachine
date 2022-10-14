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

        VisualElement m_NavelGazeMessage;
        Label m_StatusText;
        Button m_SoloButton;
        Label m_UpdateMode;

        public bool IsPrefab { get; private set; }

        /// <summary>Call from Inspector's OnEnable</summary>
        public void OnEnable(UnityEngine.Object[] targets)
        {
            Targets = targets;
            Undo.undoRedoPerformed += UpdateCameraState;
            IsPrefab = Target != null && Target.gameObject.scene.name == null; // causes a small GC alloc
        }

        /// <summary>Call from Inspector's OnDisable</summary>
        public void OnDisable()
        {
            EditorApplication.update -= UpdateCameraStatus;
            EditorApplication.update -= RefreshPipelinDropdowns;
            Undo.undoRedoPerformed -= UpdateCameraState;
            if (Target != null && CinemachineBrain.SoloCamera == Target)
            {
                CinemachineBrain.SoloCamera = null;
                InspectorUtility.RepaintGameView();
            }
        }
        
        public void AddCameraStatus(VisualElement ux)
        {
            // No status and Solo for prefabs or multi-select
            if (Selection.objects.Length > 1 || IsPrefab)
                return;

            EditorApplication.update -= UpdateCameraStatus;
            EditorApplication.update += UpdateCameraStatus;

            m_NavelGazeMessage = ux.AddChild(new HelpBox("The camera is trying to look at itself.", HelpBoxMessageType.Warning));

            var row = new InspectorUtility.LabeledContainer("Status");
            m_StatusText = row.labelElement;
            m_SoloButton = row.AddInput(new Button() 
            { 
                text = "Solo", 
                style = { flexGrow = 1, paddingLeft = 0, paddingRight = 0, 
                    marginLeft = 0, marginRight = 0, borderLeftWidth = 0, borderRightWidth = 0 } 
            });
            m_UpdateMode = row.AddInput(new Label("(Update Mode)") { style = { flexGrow = 0, alignSelf = Align.Center }});
            m_UpdateMode.SetEnabled(false);
            m_UpdateMode.style.display = DisplayStyle.None;
            ux.Add(row);

            var target = Target; // capture for lambda
            m_SoloButton.RegisterCallback<ClickEvent>((evt) => 
            {
                var isSolo = CinemachineBrain.SoloCamera != target;
                CinemachineBrain.SoloCamera = isSolo ? Target : null;
                InspectorUtility.RepaintGameView();
            });

            UpdateCameraStatus(); // avoid initial flicker
        }

        void UpdateCameraState() { if (Target != null) Target.InternalUpdateCameraState(Vector3.up, -1); }

        void UpdateCameraStatus() 
        { 
            if (Target == null)
                return;

            // Is the camera navel-gazing?
            if (m_NavelGazeMessage != null)
            {
                CameraState state = Target.State;
                bool isNavelGazing = Target.PreviousStateIsValid 
                    && state.HasLookAt() && (state.ReferenceLookAt - state.GetCorrectedPosition()).AlmostZero();
                m_NavelGazeMessage.style.display = isNavelGazing ? DisplayStyle.Flex : DisplayStyle.None;
            }

            bool isSolo = CinemachineBrain.SoloCamera == Target;
            var color = isSolo ? CinemachineBrain.GetSoloGUIColor() : GUI.color; // GML fixme: what is the right way to get default color?
            if (m_StatusText != null)
            {
                bool isLive = CinemachineCore.Instance.IsLive(Target);
                m_StatusText.text = isLive ? "Status: Live"
                    : (Target.isActiveAndEnabled ? "Status: Standby" : "Status: Disabled");
                m_StatusText.SetEnabled(isLive);
                m_StatusText.style.color = color;
            }

            if (m_UpdateMode != null)
            {
                if (!Application.isPlaying)
                    m_UpdateMode.style.display = DisplayStyle.None;
                else
                {
                    UpdateTracker.UpdateClock updateMode = CinemachineCore.Instance.GetVcamUpdateStatus(Target);
                    m_UpdateMode.text = updateMode == UpdateTracker.UpdateClock.Fixed ? " Fixed Update" : " Late Update";
                    m_UpdateMode.style.display = DisplayStyle.Flex;
                }
            }

            if (m_SoloButton != null)
                m_SoloButton.style.color = color;

            // Refresh the game view if solo and not playing
            if (isSolo && !Application.isPlaying)
                InspectorUtility.RepaintGameView();
        }
        
        public void AddPipelineDropdowns(VisualElement ux)
        {
            var cmCam = Target as CmCamera;
            if (cmCam == null)
                return;

            var targets = Targets; // capture for lambda

            // Add a dropdown for each pipeline stage
            m_PipelineItems = new List<PipelineStageItem>();
            for (int i = 0; i < PipelineStageMenu.s_StageData.Length; ++i)
            {
                // Skip empty categories
                if (PipelineStageMenu.s_StageData[i].Types.Count < 2)
                    continue;

                var row = ux.AddChild(new InspectorUtility.LeftRightContainer());
                int currentSelection = PipelineStageMenu.GetSelectedComponent(
                    i, cmCam.GetCinemachineComponent((CinemachineCore.Stage)i));
                var stage = i; // capture for lambda
                var dropdown = new DropdownField
                {
                    name = PipelineStageMenu.s_StageData[stage].Name + " selector",
                    label = "",
                    choices = PipelineStageMenu.s_StageData[stage].Choices,
                    index = currentSelection,
                    style = { flexGrow = 1 }
                };
                dropdown.AddToClassList(InspectorUtility.kAlignFieldClass);
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
                            t.InvalidatePipelineCache();
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
                row.Left.Add(new Label(PipelineStageMenu.s_StageData[stage].Name) 
                    { style = { flexGrow = 1, alignSelf = Align.Center }});
                var warningIcon = row.Left.AddChild(new Label 
                { 
                    tooltip = "Component is disabled or has a problem",
                    style = 
                    { 
                        flexGrow = 0,
                        backgroundImage = (StyleBackground)EditorGUIUtility.IconContent("console.warnicon.sml").image,
                        width = InspectorUtility.SingleLineHeight, height = InspectorUtility.SingleLineHeight,
                        alignSelf = Align.Center
                    }
                });
                warningIcon.SetVisible(false);
                row.Right.Add(dropdown);

                m_PipelineItems.Add(new PipelineStageItem
                {
                    Stage = (CinemachineCore.Stage)i,
                    Dropdown = dropdown,
                    WarningIcon = warningIcon
                });
            }
            EditorApplication.update += RefreshPipelinDropdowns;
        }

        struct PipelineStageItem
        {
            public CinemachineCore.Stage Stage;
            public DropdownField Dropdown;
            public Label WarningIcon;
        }
        List<PipelineStageItem> m_PipelineItems;

        void RefreshPipelinDropdowns()
        {
            var cmCam = Target as CmCamera;
            if (cmCam == null)
                return;
            for (int i = 0; i < m_PipelineItems.Count; ++i)
            {
                var item = m_PipelineItems[i];
                var c = cmCam.GetCinemachineComponent(item.Stage);
                int selection = PipelineStageMenu.GetSelectedComponent((int)item.Stage, c);
                item.Dropdown.value = PipelineStageMenu.s_StageData[(int)item.Stage].Choices[selection];
                
                item.WarningIcon.SetVisible(c != null && !c.IsValid);
            }
        }

        /// <summary>Draw the Extensions dropdown in the inspector</summary>
        public void AddExtensionsDropdown(VisualElement ux)
        {
            var cmCam = Target;
            var dropdown = new DropdownField
            {
                name = "extensions selector",
                label = "Add Extension",
                choices = PipelineStageMenu.s_ExtentionNames,
                index = 0,
            };
            dropdown.AddToClassList(InspectorUtility.kAlignFieldClass);
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
                        Name = stage == CinemachineCore.Stage.Body ? "Position Control" 
                            : stage == CinemachineCore.Stage.Aim ? "Rotation Control"
                            : ObjectNames.NicifyVariableName(stage.ToString()),
                        Types = new List<Type>() { null }, // first item is "none"
                        Choices = new List<string>() { "none" }
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
                    s_StageData[stage].Choices.Add(InspectorUtility.NicifyClassName(t));
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
        
        /// <summary>
        /// Draw the global settings controls in the inspector
        /// </summary>
        public void AddSaveDuringPlayToggle(VisualElement ux)
        {
            var toggle = ux.AddChild(new Toggle("Save During Play") { style = { height = InspectorUtility.SingleLineHeight }});
            toggle.AddToClassList(InspectorUtility.kAlignFieldClass);
            toggle.tooltip = "If checked, CmCamera settings changes made during Play Mode "
                + "will be propagated back to the scene when Play Mode is exited.";
            toggle.value = SaveDuringPlay.SaveDuringPlay.Enabled;

            var helpBox = ux.AddChild(new HelpBox("CmCamera settings changes made during Play Mode will be "
                    + "propagated back to the scene when Play Mode is exited.", 
                HelpBoxMessageType.Info));
            helpBox.SetVisible(SaveDuringPlay.SaveDuringPlay.Enabled && Application.isPlaying);

            toggle.RegisterValueChangedCallback((evt) => 
            {
                SaveDuringPlay.SaveDuringPlay.Enabled = evt.newValue;
                helpBox.SetVisible(evt.newValue && Application.isPlaying);
            });
        }

        /// <summary>
        /// Draw the global settings controls in the inspector
        /// </summary>
        public void AddGameViewGuidesToggle(VisualElement ux)
        {
            var row = new InspectorUtility.LeftRightContainer();

            const string tooltip = "Enable the display of overlays in the Game window.  "
                    + "You can adjust colours and opacity in Cinemachine Preferences.";
            var label = row.Left.AddChild(new Label("Game View Guides") 
            { 
                tooltip = tooltip, 
                style = { alignSelf = Align.Center, flexGrow = 1, marginBottom = 1 }
            });
            var toggle = row.Right.AddChild(new Toggle("") { tooltip = tooltip, style = { height = InspectorUtility.SingleLineHeight }});
            toggle.value = CinemachineCorePrefs.ShowInGameGuides.Value;

            const string interactiveTooltip = "Enable this to make the gave view giudes draggable with the mouse.";
            var interactiveToggle = row.Right.AddChild(new Toggle("") 
            { 
                tooltip = interactiveTooltip, 
                style = { height = InspectorUtility.SingleLineHeight, marginLeft = 8, marginRight = 4 }
            });

            var interactiveLabel = row.Right.AddChild(new Label("Draggable")
            {
                tooltip = interactiveTooltip, 
                style = { alignSelf = Align.Center, flexGrow = 1, marginBottom = 1 }
            });

            interactiveToggle.RegisterValueChangedCallback((evt) => 
            {
                CinemachineCorePrefs.DraggableComposerGuides.Value = evt.newValue;
                UpdateVisibility(interactiveLabel, interactiveToggle);
            });

            toggle.RegisterValueChangedCallback((evt) => 
            {
                CinemachineCorePrefs.ShowInGameGuides.Value = evt.newValue;
                UpdateVisibility(interactiveLabel, interactiveToggle);
            });

            UpdateVisibility(interactiveLabel, interactiveToggle);
            static void UpdateVisibility(Label label, Toggle toggle)
            {
                toggle.SetVisible(CinemachineCorePrefs.ShowInGameGuides.Value);
                label.style.opacity = CinemachineCorePrefs.DraggableComposerGuides.Value ? 1 : 0.5f;
                label.SetVisible(CinemachineCorePrefs.ShowInGameGuides.Value);
            }

            ux.Add(row);
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
            if (Target == null || PrefabUtility.IsPartOfNonAssetPrefabInstance(Target))
                return; // target was deleted or is part of a prefab instance

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
    }
}

