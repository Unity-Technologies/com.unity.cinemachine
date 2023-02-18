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
    /// Helpers for drawing CinemachineCamera inspectors.
    /// </summary>
    static class CmCameraInspectorUtility
    {
        struct PipelineStageItem
        {
            public CinemachineCore.Stage Stage;
            public DropdownField Dropdown;
            public Label WarningIcon;
        }

        static bool IsPrefab(UnityEngine.Object target)
        {
            var t = target as CinemachineVirtualCameraBase;
            return t != null && t.gameObject.scene.name == null; // causes a small GC alloc
        }

        /// <summary>Add the camera ststos controls and indicators in the inspector</summary>
        public static void AddCameraStatus(this UnityEditor.Editor editor, VisualElement ux)
        {
            // No status and Solo for prefabs or multi-select
            if (Selection.objects.Length > 1 || IsPrefab(editor.target))
                return;

            var navelGazeMessage = ux.AddChild(new HelpBox("The camera is trying to look at itself.", HelpBoxMessageType.Warning));

            var row = ux.AddChild(new InspectorUtility.LabeledContainer("Status"));
            var statusText = row.labelElement;
            var soloButton = row.AddInput(new Button() 
            { 
                text = "Solo", 
                style = { flexGrow = 1, paddingLeft = 0, paddingRight = 0, 
                    marginLeft = 0, marginRight = 0, borderLeftWidth = 1, borderRightWidth = 1 } 
            });
            var updateMode = row.AddInput(new Label("(Update Mode)") { style = { flexGrow = 0, alignSelf = Align.Center }});
            updateMode.SetEnabled(false);
            updateMode.style.display = DisplayStyle.None;

            var target = editor.target as CinemachineVirtualCameraBase; // capture for lambda
            soloButton.RegisterCallback<ClickEvent>((evt) => 
            {
                var isSolo = CinemachineBrain.SoloCamera != target;
                CinemachineBrain.SoloCamera = isSolo ? target : null;
                InspectorUtility.RepaintGameView();
            });

            ux.TrackAnyUserActivity(() =>
            { 
                if (target == null)
                    return;

                // Is the camera navel-gazing?
                if (navelGazeMessage != null)
                {
                    CameraState state = target.State;
                    bool isNavelGazing = target.PreviousStateIsValid && state.HasLookAt() &&
                        (state.ReferenceLookAt - state.GetCorrectedPosition()).AlmostZero() &&
                        target.GetCinemachineComponent(CinemachineCore.Stage.Aim) != null;
                    navelGazeMessage.SetVisible(isNavelGazing);
                }
            });

            // Capture "normal" colors
            ux.OnInitialGeometryChanged(() =>
            {
                var normalColor = statusText.resolvedStyle.color;
                var normalBkgColor = soloButton.resolvedStyle.backgroundColor;

                // Refresh camera state
                ux.ContinuousUpdate(() =>
                { 
                    if (target == null)
                        return;

                    bool isSolo = CinemachineBrain.SoloCamera == target;
                    var color = isSolo ? Color.Lerp(normalColor, CinemachineBrain.GetSoloGUIColor(), 0.5f) : normalColor;

                    bool isLive = CinemachineCore.Instance.IsLive(target);
                    statusText.text = isLive ? "Status: Live"
                        : (target.isActiveAndEnabled ? "Status: Standby" : "Status: Disabled");
                    statusText.SetEnabled(isLive);
                    statusText.style.color = color;

                    if (!Application.isPlaying)
                        updateMode.SetVisible(false);
                    else
                    {
                        var mode = CinemachineCore.Instance.GetVcamUpdateStatus(target);
                        updateMode.text = mode == UpdateTracker.UpdateClock.Fixed ? " Fixed Update" : " Late Update";
                        updateMode.SetVisible(true);
                    }

                    soloButton.style.color = color;
                    soloButton.style.backgroundColor = isSolo 
                        ? Color.Lerp(normalBkgColor, CinemachineBrain.GetSoloGUIColor(), 0.2f) : normalBkgColor;

                    // Refresh the game view if solo and not playing
                    if (isSolo && !Application.isPlaying)
                    {
                        target.InternalUpdateCameraState(Vector3.up, -1);
                        InspectorUtility.RepaintGameView();
                    }
                });
            });

            // Kill solo when inspector shuts down
            ux.RegisterCallback<DetachFromPanelEvent>((e) => 
            {
                if (target != null && CinemachineBrain.SoloCamera == target)
                {
                    CinemachineBrain.SoloCamera = null;
                    InspectorUtility.RepaintGameView();
                }
            });
        }

        /// <summary>Add the pipeline control dropdowns in the inspector</summary>
        public static void AddPipelineDropdowns(this UnityEditor.Editor editor, VisualElement ux)
        {
            var target = editor.target as CinemachineCamera;
            if (target == null)
                return;

            var targets = editor.targets; // capture for lambda

            // Add a dropdown for each pipeline stage
            var pipelineItems = new List<PipelineStageItem>();
            for (int i = 0; i < PipelineStageMenu.s_StageData.Length; ++i)
            {
                // Skip empty categories
                if (PipelineStageMenu.s_StageData[i].Types.Count < 2)
                    continue;

                var row = ux.AddChild(new InspectorUtility.LeftRightContainer());
                int currentSelection = PipelineStageMenu.GetSelectedComponent(
                    i, target.GetCinemachineComponent((CinemachineCore.Stage)i));
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
                        var t = targets[i] as CinemachineCamera;
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

                    static int GetTypeIndexFromSelection(string selection, int stage)
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

                pipelineItems.Add(new PipelineStageItem
                {
                    Stage = (CinemachineCore.Stage)i,
                    Dropdown = dropdown,
                    WarningIcon = warningIcon
                });
            }

            ux.TrackAnyUserActivity(() =>
            {
                if (target == null)
                    return; // deleted
                for (int i = 0; i < pipelineItems.Count; ++i)
                {
                    var item = pipelineItems[i];
                    var c = target.GetCinemachineComponent(item.Stage);
                    int selection = PipelineStageMenu.GetSelectedComponent((int)item.Stage, c);
                    item.Dropdown.value = PipelineStageMenu.s_StageData[(int)item.Stage].Choices[selection];
                    item.WarningIcon.SetVisible(c != null && !c.IsValid);
                }
            });
        }

        /// <summary>Draw the Extensions dropdown in the inspector</summary>
        public static void AddExtensionsDropdown(this UnityEditor.Editor editor, VisualElement ux)
        {
            var targets = editor.targets;
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
                for (int i = 0; i < targets.Length; i++)
                {
                    var targetGO = (targets[i] as CinemachineVirtualCameraBase).gameObject;
                    if (targetGO != null && targetGO.GetComponent(extType) == null)
                        Undo.AddComponent(targetGO, extType);
                }
            
                static int GetTypeIndexFromSelection(string selection)
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
                            (Type t) => typeof(CinemachineExtension).IsAssignableFrom(t) 
                                && !t.IsAbstract && t.GetCustomAttribute<ObsoleteAttribute>() == null);
                foreach (Type t in allExtensions)
                {
                    s_ExtentionTypes.Add(t);
                    s_ExtentionNames.Add(t.Name);
                }
            }
        }
        
        /// <summary>Draw the global settings controls in the inspector</summary>
        public static void AddGlobalControls(this UnityEditor.Editor editor, VisualElement ux)
        {
            var helpBox = ux.AddChild(new HelpBox("CinemachineCamera settings changes made during Play Mode will be "
                    + "propagated back to the scene when Play Mode is exited.", 
                HelpBoxMessageType.Info));
            helpBox.SetVisible(SaveDuringPlay.Enabled && Application.isPlaying);

            var toggle = ux.AddChild(new Toggle(CinemachineCorePrefs.s_SaveDuringPlayLabel.text) 
            { 
                tooltip = CinemachineCorePrefs.s_SaveDuringPlayLabel.tooltip,
                value = SaveDuringPlay.Enabled
            });
            toggle.AddToClassList(InspectorUtility.kAlignFieldClass);
            toggle.RegisterValueChangedCallback((evt) => 
            {
                SaveDuringPlay.Enabled = evt.newValue;
                helpBox.SetVisible(evt.newValue && Application.isPlaying);
            });

            var choices = new List<string>() { "Disabled", "Passive", "Interactive" };
            int index = CinemachineCorePrefs.ShowInGameGuides.Value 
                ? (CinemachineCorePrefs.DraggableComposerGuides.Value ? 2 : 1) : 0;
            var dropdown = ux.AddChild(new DropdownField("Game View Guides")
            {
                tooltip = CinemachineCorePrefs.s_ShowInGameGuidesLabel.tooltip,
                choices = choices,
                index = index,
                style = { flexGrow = 1 }
            });
            dropdown.AddToClassList(InspectorUtility.kAlignFieldClass);
            dropdown.RegisterValueChangedCallback((evt) => 
            {
                CinemachineCorePrefs.ShowInGameGuides.Value = evt.newValue != choices[0];
                CinemachineCorePrefs.DraggableComposerGuides.Value = evt.newValue == choices[2];
                InspectorUtility.RepaintGameView();
            });
        }

        static List<MonoBehaviour> s_componentCache = new ();
        enum SortOrder { None, Camera, Pipeline, Extensions = CinemachineCore.Stage.Finalize + 1, Other };

        /// <summary>
        /// This is only for aesthetics, sort order does not affect camera logic.
        /// Behaviours should be sorted like this:
        /// CinemachineCamera, Body, Aim, Noise, Finalize, Extensions, everything else.
        /// </summary>
        public static void SortComponents(CinemachineVirtualCameraBase target)
        {
            if (target == null || PrefabUtility.IsPartOfNonAssetPrefabInstance(target))
                return; // target was deleted or is part of a prefab instance

            SortOrder lastItem = SortOrder.None;
            bool sortNeeded = false;
            target.gameObject.GetComponents(s_componentCache);
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

