using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;
using UnityEngine.UIElements;
using System.Reflection;
using UnityEditor.UIElements;

namespace Unity.Cinemachine.Editor
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
            return t != null && PrefabUtility.IsPartOfPrefabAsset(t);
        }

        static Color s_NormalColor = Color.black;
        static Color s_NormalBkgColor = Color.black;
        
        /// <summary>Add the camera status controls and indicators in the inspector</summary>
        public static void AddCameraStatus(this UnityEditor.Editor editor, VisualElement ux)
        {
            // No status and Solo for prefabs or multi-select
            if (Selection.objects.Length > 1 || IsPrefab(editor.target))
                return;
            
            var cameraParentingMessage = ux.AddChild(new HelpBox(
                $"Setup error: {editor.target.GetType().Name} should not be a child "
                + "of CinemachineCamera or CinemachineBrain.\n\n"
                + "<b>Best practice is to have CinemachineCamera, CinemachineBrain, and camera targets as "
                + "separate objects, not parented to each other.</b>", 
                HelpBoxMessageType.Error));

            var noBrainMessage = ux.AddChild(InspectorUtility.HelpBoxWithButton(
                "A CinemachineBrain is required in the scene.", 
                HelpBoxMessageType.Warning, "Add Brain", () => CinemachineMenu.GetOrCreateBrain()));

            var navelGazeMessage = ux.AddChild(new HelpBox(
                "The camera is trying to look at itself.", 
                HelpBoxMessageType.Warning));

            var row = ux.AddChild(new InspectorUtility.LabeledRow("Status"));
            var statusText = row.Label;
            var soloButton = row.Contents.AddChild(new Button() 
            { 
                text = "Solo", 
                style = { flexGrow = 1, paddingLeft = 0, paddingRight = 0, 
                    marginLeft = 0, marginRight = 0, borderLeftWidth = 1, borderRightWidth = 1 } 
            });
            var updateMode = row.Contents.AddChild(new Label("(Update Mode)") { style = { flexGrow = 0, alignSelf = Align.Center }});
            updateMode.SetEnabled(false);
            updateMode.style.display = DisplayStyle.None;

            var target = editor.target as CinemachineVirtualCameraBase; // capture for lambda
            soloButton.RegisterCallback<ClickEvent>(_ =>
            {
                var isSolo = CinemachineCore.SoloCamera != (ICinemachineCamera)target;
                CinemachineCore.SoloCamera = isSolo ? target : null;
                InspectorUtility.RepaintGameView();
            });

            ux.TrackAnyUserActivity(() =>
            { 
                if (target == null)
                    return;

                // Is the camera navel-gazing?
                CameraState state = target.State;
                bool isNavelGazing = target.PreviousStateIsValid && state.HasLookAt() &&
                    (state.ReferenceLookAt - state.GetCorrectedPosition()).AlmostZero();
                if (isNavelGazing)
                {
                    var aim = target.GetCinemachineComponent(CinemachineCore.Stage.Aim);
                    if (aim == null || !aim.CameraLooksAtTarget)
                        isNavelGazing = false;
                }
                navelGazeMessage.SetVisible(isNavelGazing);

                // Is the camera parenting incorrect?
                cameraParentingMessage.SetVisible(
                    target.GetComponentInParent<CinemachineBrain>() != null 
                    || (target.ParentCamera != null && target.ParentCamera is not CinemachineCameraManagerBase));

                // Is there a Brain?
                noBrainMessage.SetVisible(CinemachineBrain.ActiveBrainCount == 0 && !IsPrefab(target));
            });

            // Capture "normal" colors
            if (s_NormalBkgColor == Color.black)
            {
                ux.OnInitialGeometry(() =>
                {
                    s_NormalColor = statusText.resolvedStyle.color;
                    s_NormalBkgColor = soloButton.resolvedStyle.backgroundColor;
                });
            }

            // Refresh camera state
            ux.ContinuousUpdate(() =>
            { 
                if (target == null)
                    return;

                bool isSolo = CinemachineCore.SoloCamera == (ICinemachineCamera)target;
                var color = isSolo ? Color.Lerp(s_NormalColor, CinemachineCore.SoloGUIColor(), 0.5f) : s_NormalColor;

                bool isLive = CinemachineCore.IsLive(target);
                statusText.text = isLive ? "Status: Live"
                    : target.isActiveAndEnabled ? "Status: Standby" : "Status: Disabled";
                statusText.SetEnabled(isLive);
                statusText.style.color = color;

                if (!Application.isPlaying)
                    updateMode.SetVisible(false);
                else
                {
                    var mode = CameraUpdateManager.GetVcamUpdateStatus(target);
                    updateMode.text = mode == UpdateTracker.UpdateClock.Fixed ? " Fixed Update" : " Late Update";
                    updateMode.SetVisible(true);
                }

                soloButton.style.color = color;
                soloButton.style.backgroundColor = isSolo 
                    ? Color.Lerp(s_NormalBkgColor, CinemachineCore.SoloGUIColor(), 0.2f) : s_NormalBkgColor;

                // Refresh the game view if solo and not playing
                if (isSolo && !Application.isPlaying)
                    InspectorUtility.RepaintGameView();
            });

            // Kill solo when inspector shuts down
            ux.RegisterCallback<DetachFromPanelEvent>(_ =>
            {
                if (target != null && CinemachineCore.SoloCamera == (ICinemachineCamera)target)
                {
                    CinemachineCore.SoloCamera = null;
                    InspectorUtility.RepaintGameView();
                }
            });
        }

        public static void AddTransitionsSection(
            this UnityEditor.Editor editor, VisualElement ux, 
            List<SerializedProperty> otherProperties = null)
        {
            var serializedObject = editor.serializedObject;
            var target = editor.target as CinemachineVirtualCameraBase;
            ux.Add(new PropertyField(serializedObject.FindProperty(() => target.Priority)));
            ux.Add(new PropertyField(serializedObject.FindProperty(() => target.OutputChannel)));
            ux.Add(new PropertyField(serializedObject.FindProperty(() => target.StandbyUpdate)));
            for (int i = 0; otherProperties != null && i < otherProperties.Count; ++i)
                ux.Add(new PropertyField(otherProperties[i]));
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

                var stage = i; // capture for lambda
                var row = ux.AddChild(new InspectorUtility.LeftRightRow());
                row.Left.Add(new Label(PipelineStageMenu.s_StageData[stage].Name) 
                { 
                    tooltip = "Will add a Behaviour to implement this stage in the procedural pipeline", 
                    style = { flexGrow = 1, alignSelf = Align.Center }
                });
                var warningIcon = row.Left.AddChild(InspectorUtility.MiniHelpIcon("Component is disabled or has a problem"));
                warningIcon.SetVisible(false);

                int currentSelection = PipelineStageMenu.GetSelectedComponent(
                    i, target.GetCinemachineComponent((CinemachineCore.Stage)i));
                var dropdown = row.Right.AddChild(new DropdownField
                {
                    choices = PipelineStageMenu.s_StageData[stage].Choices,
                    index = currentSelection,
                    style = { flexGrow = 1 }
                });
                dropdown.RegisterValueChangedCallback(evt => 
                {
                    var newType = PipelineStageMenu.s_StageData[stage].Types[GetTypeIndexFromSelection(evt.newValue, stage)];
                    for (int j = 0; j < targets.Length; j++)
                    {
                        var t = targets[j] as CinemachineCamera;
                        if (t == null)
                            continue;
                        var oldComponents = t.GetComponents<CinemachineComponentBase>();
                        CinemachineComponentBase existingComponent = null;
                        for (int k = 0; k < oldComponents.Length; ++k)
                        {
                            if (existingComponent == null && oldComponents[k].GetType() == newType)
                                existingComponent = oldComponents[k];
                            else if (oldComponents[k].Stage == (CinemachineCore.Stage)stage)
                                Undo.DestroyObjectImmediate(oldComponents[k]);
                        }
                        if (newType != null && existingComponent == null)
                            Undo.AddComponent(t.gameObject, newType);
                        t.InvalidatePipelineCache();
                    }

                    static int GetTypeIndexFromSelection(string selection, int stage)
                    {
                        for (var j = 0; j < PipelineStageMenu.s_StageData[stage].Choices.Count; ++j)
                            if (PipelineStageMenu.s_StageData[stage].Choices[j].Equals(selection))
                                return j;
                        return 0;
                    }
                });

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
                target.InvalidatePipelineCache();
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
            var row = new InspectorUtility.LabeledRow(
                "Add Extension", "Extensions are behaviours that inject themselves into "
                + "the Cinemachine pipeline to alter the camera's behaviour.  "
                + "This dropdown will add the selected extension behaviour.");

            var menu = new ContextualMenuManipulator((evt) => 
            {
                for (int i = 0; i < PipelineStageMenu.s_ExtensionTypes.Count; ++i)
                {
                    var type = PipelineStageMenu.s_ExtensionTypes[i];
                    if (type == null)
                        continue;
                    var name = PipelineStageMenu.s_ExtensionNames[i];
                    evt.menu.AppendAction(name, 
                        (action) => 
                        {
                            var target = editor.target as CinemachineVirtualCameraBase;
                            Undo.AddComponent(target.gameObject, type);
                        }, 
                        (status) => 
                        {
                            var target = editor.target as CinemachineVirtualCameraBase;
                            var disable = target == null || target.GetComponent(type) != null;
                            return disable ? DropdownMenuAction.Status.Disabled : DropdownMenuAction.Status.Normal;
                        }
                    );
                }
            });
            var button = row.Contents.AddChild(new Button 
            { 
                text = "(select)", 
                style = 
                { 
                    flexGrow = 1, marginRight = 0, marginLeft = 3, 
                    paddingTop = 0, paddingBottom = 0, paddingLeft = 1,
                    height = InspectorUtility.SingleLineHeight + 2, 
                    unityTextAlign = TextAnchor.MiddleLeft
                }
            });
            menu.activators.Clear();
            menu.activators.Add(new ManipulatorActivationFilter { button = MouseButton.LeftMouse });
            button.AddManipulator(menu);
            ux.Add(row);
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
            public static List<Type> s_ExtensionTypes;
            public static List<string> s_ExtensionNames;

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
                        Types = new List<Type>() { null }, // first item is "None"
                        Choices = new List<string>() { "None" }
                    };
                }

                // Get all CinemachineComponentBase
                var allTypes = ReflectionHelpers.GetTypesInAllDependentAssemblies((Type t) => 
                    typeof(CinemachineComponentBase).IsAssignableFrom(t) && !t.IsAbstract 
                    && t.GetCustomAttribute<CameraPipelineAttribute>() != null
                    && t.GetCustomAttribute<ObsoleteAttribute>() == null);

                var iter = allTypes.GetEnumerator();
                while (iter.MoveNext())
                {
                    var t = iter.Current;
                    var stage = (int)t.GetCustomAttribute<CameraPipelineAttribute>().Stage;
                    s_StageData[stage].Types.Add(t);
                    s_StageData[stage].Choices.Add(InspectorUtility.NicifyClassName(t));
                }

                // Populate the extension list
                s_ExtensionTypes = new List<Type>();
                s_ExtensionNames = new List<string>();
                s_ExtensionTypes.Add(null);
                s_ExtensionNames.Add("(select)");
                var allExtensions
                    = ReflectionHelpers.GetTypesInAllDependentAssemblies(
                            (Type t) => typeof(CinemachineExtension).IsAssignableFrom(t) 
                                && !t.IsAbstract && t.GetCustomAttribute<ObsoleteAttribute>() == null);
                var iter2 = allExtensions.GetEnumerator();
                while (iter2.MoveNext())
                {
                    var t = iter2.Current;
                    s_ExtensionTypes.Add(t);
                    s_ExtensionNames.Add(t.Name);
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

        /// <summary>
        /// Use this delegate to control the display of warning icons next to the child cameras
        /// </summary>
        public delegate string GetChildWarningMessageDelegate(object childObject);

        /// <summary>If camera is a CinemachineCameraManagerBase, draw the Child camera list</summary>
        public static void AddChildCameras(
            this UnityEditor.Editor editor, VisualElement ux, 
            GetChildWarningMessageDelegate getChildWarning)
        {
            var vcam = editor.target as CinemachineCameraManagerBase;
            if (vcam == null)
                return;

            var floatFieldWidth = EditorGUIUtility.singleLineHeight * 2.5f;

            var helpBox = ux.AddChild(new HelpBox(
                "Child Cameras cannot be displayed when multiple objects are selected.", 
                HelpBoxMessageType.Info));

            var container = ux.AddChild(new VisualElement());
            
            var header = container.AddChild(new VisualElement { style = { flexDirection = FlexDirection.Row, marginBottom = -2 } });
            header.AddToClassList("unity-collection-view--with-border");
            header.AddChild(new Label("Child Cameras") { style = { marginLeft = 3, flexGrow = 1, flexBasis = 10  }});
            header.AddChild(new Label("Priority") 
                { style = { marginRight = 4, flexGrow = 1, flexBasis = floatFieldWidth, unityTextAlign = TextAnchor.MiddleRight }});

            var list = container.AddChild(new ListView()
            {
                reorderable = false,
                showAddRemoveFooter = true,
                showBorder = true,
                showBoundCollectionSize = false,
                showFoldoutHeader = false,
                style = { borderTopWidth = 0 },
            });
            list.itemsSource = vcam.ChildCameras;

            list.makeItem = () => new VisualElement { style = { flexDirection = FlexDirection.Row }};
            list.bindItem = (row, index) =>
            {
                // Remove children - items seem to get recycled
                for (int i = row.childCount - 1; i >= 0; --i)
                    row.RemoveAt(i);

                var element = list.itemsSource[index] as CinemachineVirtualCameraBase;
                row.AddChild(new ObjectField 
                { 
                    value = element,
                    objectType = typeof(CinemachineVirtualCameraBase),
                    style = { flexBasis = 20, flexGrow = 1 }
                }).SetEnabled(false);
                if (element == null)
                    return;

                var warningIcon = row.AddChild(InspectorUtility.MiniHelpIcon("Item is null"));
                var warningText = getChildWarning == null ? string.Empty : getChildWarning(element);
                warningIcon.tooltip = warningText;
                warningIcon.SetVisible(!string.IsNullOrEmpty(warningText));

                var dragger = row.AddChild(new Label(" "));
                dragger.AddToClassList("unity-base-field__label--with-dragger");

                var so = new SerializedObject(element);
                var prop = so.FindProperty("Priority");
                var enabledProp = prop.FindPropertyRelative("Enabled");
                var priorityProp = prop.FindPropertyRelative("m_Value");
                var priorityField = row.AddChild(new IntegerField
                {
                    value = enabledProp.boolValue ? priorityProp.intValue : 0,
                    style = { flexBasis = floatFieldWidth, flexGrow = 0, marginRight = 4 }
                });
                new FieldMouseDragger<int>(priorityField).SetDragZone(dragger);
                priorityField.RegisterValueChangedCallback((evt) =>
                {
                    if (evt.newValue != 0)
                        enabledProp.boolValue = true;
                    priorityProp.intValue = evt.newValue;
                    so.ApplyModifiedProperties();
                });
                priorityField.TrackPropertyValue(priorityProp, (p) => priorityField.value = p.intValue);
                priorityField.TrackPropertyValue(enabledProp, (p) => priorityField.value = p.boolValue ? priorityProp.intValue : 0);
            };

            list.itemsAdded += (added) =>
            {
                var selected = list.selectedIndex;
                var selectedCam = (selected >= 0 && selected < list.itemsSource.Count) 
                    ? list.itemsSource[selected] as CinemachineVirtualCameraBase : null;
                var name = selectedCam != null ? selectedCam.Name : "Child";
                var iter = added.GetEnumerator();
                while (iter.MoveNext())
                    CinemachineMenu.CreatePassiveCmCamera(name, vcam.gameObject);
                Selection.activeObject = vcam;
                vcam.InvalidateCameraCache();
            };

            list.itemsRemoved += (removed) =>
            {
                var iter = removed.GetEnumerator();
                while (iter.MoveNext())
                {
                    var child = list.itemsSource[iter.Current] as CinemachineVirtualCameraBase;
                    if (child != null)
                        Undo.DestroyObjectImmediate(child.gameObject);
                }
                vcam.InvalidateCameraCache();
            };

            container.TrackAnyUserActivity(() =>
            {
                if (editor == null || editor.target == null)
                    return; // object deleted
                var isMultiSelect = editor.targets.Length > 1;
                helpBox.SetVisible(isMultiSelect);
                container.SetVisible(!isMultiSelect);

                // Update child list
                if (!isMultiSelect)
                {
                    list.itemsSource = vcam.ChildCameras;
                    list.Rebuild();
                }
            });
        }
    }
}

