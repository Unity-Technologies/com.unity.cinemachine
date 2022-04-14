using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEditor;
using Cinemachine.Editor;
using Cinemachine.Utility;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace Cinemachine
{
    [CustomEditor(typeof(CmCamera))]
    [CanEditMultipleObjects]
    sealed class CmCameraEditor : CinemachineVirtualCameraBaseEditor<CmCamera>
    {
        [MenuItem("CONTEXT/CmCamera/Adopt Game View Camera Settings")]
        static void AdoptGameViewCameraSettings(MenuCommand command)
        {
            var vcam = command.context as CmCamera;
            var brain = CinemachineCore.Instance.FindPotentialTargetBrain(vcam);
            if (brain != null)
            {
                vcam.m_Lens = brain.CurrentCameraState.Lens;
                vcam.transform.position = brain.transform.position;
                vcam.transform.rotation = brain.transform.rotation;
            }
        }
        
        [MenuItem("CONTEXT/CmCamera/Adopt Scene View Camera Settings")]
        static void AdoptSceneViewCameraSettings(MenuCommand command)
        {
            var vcam = command.context as CmCamera;
            vcam.m_Lens = CinemachineMenu.MatchSceneViewCamera(vcam.transform);
        }
        
        public override VisualElement CreateInspectorGUI()
        {
            // Create a new VisualElement to be the root of our inspector UI
            var myInspector = new VisualElement();
            // inspectorXML.CloneTree(myInspector);
            var soloHolder = new IMGUIContainer();
            myInspector.Add(soloHolder);
            
            var serializedTarget = new SerializedObject(Target);
            m_PriorityField = new PropertyField(serializedTarget.FindProperty(() => Target.m_Priority));
            myInspector.Add(m_PriorityField);
            myInspector.Add(new PropertyField(serializedTarget.FindProperty(() => Target.m_Follow)));
            myInspector.Add(new PropertyField(serializedTarget.FindProperty(() => Target.m_LookAt)));
            myInspector.Add(new PropertyField(serializedTarget.FindProperty(() => Target.m_StandbyUpdate)));
            myInspector.Add(new PropertyField(serializedTarget.FindProperty(() => Target.m_Lens)));
            myInspector.Add(new PropertyField(serializedTarget.FindProperty(() => Target.m_Transitions)));

            // // Inject Status
            soloHolder.onGUIHandler = DrawCameraStatusInInspector;

            // Inject procedural behaviour dropdowns into myInspector
            // TODO: replace this with Gregory's DrawPipelinePopups
            // TODO: DrawPipelinePopups and SortComponents
            
            var cmCamera = Target;
            FindStages(cmCamera);
            for (var i = 0; i < s_StageData.Length; ++i)
            {
                if (s_StageData[i].type.Count <= 1) continue;
                
                var stage = i; // need local copy for lambda expression
                var dropdown = new DropdownField
                {
                    name = s_StageData[stage].name + " Component Selection list",
                    label = s_StageData[stage].name,
                    choices = s_StageData[stage].typeName,
                    index = s_StageData[stage].selection,
                };
                dropdown.AddToClassList(InspectorUtility.alignFieldClass);
                
                dropdown.RegisterValueChangedCallback(
                    evt => HandleDropdownSelection(evt.newValue, evt.previousValue, stage, cmCamera));
                myInspector.Add(dropdown);
            }
            
            // Return the finished inspector UI
            return myInspector;
        }

        static void HandleDropdownSelection(string selection, string previousSelection, int stage, CmCamera cmCam)
        {
            if (previousSelection.Equals(selection)) return; // no change
            
            int index = GetTypeIndexFromSelection(selection, stage);
            // set vcam according to selected component
            var type = s_StageData[stage].type[index];

            if (cmCam.m_Pipeline[stage] != null)
            {
                Undo.DestroyObjectImmediate(cmCam.m_Pipeline[stage]);
                cmCam.m_Pipeline[stage] = null;
            }
            
            if (type != null)
            {
                var component = (CinemachineComponentBase) Undo.AddComponent(cmCam.gameObject, type);
                cmCam.m_Pipeline[stage] = component;
            }
            
            static int GetTypeIndexFromSelection(string selection, int stage)
            {
                for (var j = 0; j < s_StageData[stage].typeName.Count; ++j)
                    if (s_StageData[stage].typeName[j].Equals(selection))
                        return j;
                return 0;
            }
        }
        
        void OnEnable()
        {
            base.OnEnable();
            Undo.undoRedoPerformed += ResetTarget;

#if UNITY_2021_2_OR_NEWER
            CinemachineSceneToolUtility.RegisterTool(typeof(FoVTool));
            CinemachineSceneToolUtility.RegisterTool(typeof(FarNearClipTool));
#endif
        }

        void OnDisable()
        {
            Undo.undoRedoPerformed -= ResetTarget;
            
#if UNITY_2021_2_OR_NEWER
            CinemachineSceneToolUtility.UnregisterTool(typeof(FoVTool));
            CinemachineSceneToolUtility.UnregisterTool(typeof(FarNearClipTool));
#endif
            base.OnDisable();
        }

        void OnSceneGUI()
        {
#if UNITY_2021_2_OR_NEWER
            DrawSceneTools();
#endif
        }

#if UNITY_2021_2_OR_NEWER
        void DrawSceneTools()
        {
            var cmCam = Target;
            if (cmCam == null)
            {
                return;
            }

            var originalColor = Handles.color;
            Handles.color = Handles.preselectionColor;
            if (CinemachineSceneToolUtility.IsToolActive(typeof(FoVTool)))
            {
                CinemachineSceneToolHelpers.FovToolHandle(cmCam, 
                    new SerializedObject(cmCam).FindProperty(() => cmCam.m_Lens), 
                    cmCam.m_Lens, IsHorizontalFOVUsed());
            }
            else if (CinemachineSceneToolUtility.IsToolActive(typeof(FarNearClipTool)))
            {
                CinemachineSceneToolHelpers.NearFarClipHandle(cmCam,
                    new SerializedObject(cmCam).FindProperty(() => cmCam.m_Lens));
            }
            Handles.color = originalColor;
        }

        // TODO: LensSettingsInspectorHelper does much more than we need!
        LensSettingsInspectorHelper m_LensSettingsInspectorHelper;
        bool IsHorizontalFOVUsed()
        {
            if (m_LensSettingsInspectorHelper == null)
                m_LensSettingsInspectorHelper = new LensSettingsInspectorHelper();
            return m_LensSettingsInspectorHelper.UseHorizontalFOV;
        }

        VisualElement m_PriorityField;
        void DrawCameraStatusInInspector()
        {
            if (Selection.objects.Length > 1)
                return;
            
            // Is the camera navel-gazing?
            CameraState state = Target.State;
            if (state.HasLookAt && (state.ReferenceLookAt - state.CorrectedPosition).AlmostZero())
                EditorGUILayout.HelpBox(
                    "The camera is positioned on the same point at which it is trying to look.",
                    MessageType.Warning);

            // TODO: No status and Solo for prefabs
            // if (IsPrefabBase)
            //     return;

            // Active status and Solo button
            Rect rect = EditorGUILayout.GetControlRect(true);
            Rect rectLabel = new Rect(rect.x+1f, rect.y, EditorGUIUtility.labelWidth, rect.height);
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
            if (m_PriorityField.Children() != null)
            {
                // correcting the rect so it is aligned correctly with UI toolkit standard
                var correction = m_PriorityField.Children().ToArray()[0].Children().ToArray()[1];
                rect.width -= (correction.layout.x - rect.x);
                rect.x = correction.layout.x + 3;
            }

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
        }

#endif

        struct StageData
        {
            public string name;
            public List<string> typeName;
            public List<Type> type;
            public int selection;
        }

        static StageData[] s_StageData;
        static void FindStages(CmCamera cmCamera)
        {
            var names = Enum.GetNames(typeof(CinemachineCore.Stage));
            s_StageData = new StageData[names.Length];
            for (int stage = 0; stage < s_StageData.Length; ++stage)
            {
                s_StageData[stage].name = names[stage];
                s_StageData[stage].typeName = new List<string> { "None" };
                s_StageData[stage].type = new List<Type> { null };
                s_StageData[stage].selection = 0;
            }

            // Get all ICinemachineComponents
            var allTypes
                = ReflectionHelpers.GetTypesInAllDependentAssemblies((Type t) => 
                    typeof(CinemachineComponentBase).IsAssignableFrom(t) 
                    && !t.IsAbstract && t.GetCustomAttribute<ObsoleteAttribute>() == null);
            
            foreach (var t in allTypes)
            {
                var pipelineAttribute = t.GetCustomAttribute<CameraPipelineAttribute>();
                var stage = (int)pipelineAttribute.Stage;
                s_StageData[stage].type.Add(t);
                s_StageData[stage].typeName.Add(InspectorUtility.NicifyClassName(t.Name));
            }

            for (int stage = 0; stage < s_StageData.Length; ++stage)
            {
                var component = cmCamera.GetCinemachineComponent((CinemachineCore.Stage)stage);
                s_StageData[stage].selection = component == null ? 0 : GetTypeIndexFromSelection(component.GetType(), stage);
            }
            
            static int GetTypeIndexFromSelection(Type type, int stage)
            {
                for (var j = 0; j < s_StageData[stage].type.Count; ++j)
                    if (s_StageData[stage].type[j] == type)
                        return j;
                return 0;
            }
        }
        
        void DrawPipelinePopups()
        {
            var cmCam = Target;
            for (int i = 0; i < PipelineStageMenu.s_StageData.Length; ++i)
            {
                // Skip empty categories
                if (PipelineStageMenu.s_StageData[i].Types.Length < 2)
                    continue;

                var stage = PipelineStageMenu.s_StageData[i].Stage;
                var oldComponent = cmCam.GetCinemachineComponent(stage);
                int selected = PipelineStageMenu.GetSelectedComponent(i, oldComponent);
                int newSelection = EditorGUILayout.Popup(
                    PipelineStageMenu.s_StageData[i].Label, selected, PipelineStageMenu.s_StageData[i].PopupOptions);
                if (selected != newSelection)
                {
                    if (oldComponent != null)
                        Undo.DestroyObjectImmediate(oldComponent);
                    if (newSelection != 0)
                        Undo.AddComponent(cmCam.gameObject, PipelineStageMenu.s_StageData[i].Types[newSelection]);
                }
            }
        }
        
        List<MonoBehaviour> s_componentCache = new List<MonoBehaviour>();
        enum SortOrder { None, Camera, Pipeline, Extensions = CinemachineCore.Stage.Finalize + 1, Other };

        void SortComponents()
        {
            // This is only for aesthetics, sort order does not affect camera logic.
            // Behaviours should be sorted like this:
            // CmCamera, Body, Aim, Noise, Finalize, Extensions, everything else
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
        }

        static SortOrder GetSortOrderForComponent(MonoBehaviour component)
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
        static bool MoveComponentToPosition(int pos, SortOrder item, List<MonoBehaviour> components)
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
        
        static class PipelineStageMenu
        {
            // Static state and caches
            public struct StageData
            {
                public CinemachineCore.Stage Stage;
                public GUIContent Label;
                public Type[] Types;   // first entry is null - this array is synched with PopupOptions
                public GUIContent[] PopupOptions;
            }
            public static StageData[] s_StageData = null;
        
            public static int GetSelectedComponent(int stage, CinemachineComponentBase component)
            {
                if (component != null)
                    for (int j = 0; j < s_StageData[stage].PopupOptions.Length; ++j)
                        if (s_StageData[stage].Types[j] == component.GetType())
                            return j;
                return 0;
            }

            // This code dynamically discovers eligible classes and builds the menu
            // data for the various component pipeline stages.
            static PipelineStageMenu()
            {
                s_StageData = new StageData[Enum.GetValues(typeof(CinemachineCore.Stage)).Length];
                
                var stageTypes = new List<Type>[Enum.GetValues(typeof(CinemachineCore.Stage)).Length];
                for (int i = 0; i < stageTypes.Length; ++i)
                {
                    s_StageData[i].Label = new GUIContent(((CinemachineCore.Stage)i).ToString());
                    stageTypes[i] = new List<Type> { null };  // first item is "none"
                }

                // Get all CinemachineComponentBase
                var allTypes = ReflectionHelpers.GetTypesInAllDependentAssemblies((Type t) => 
                    typeof(CinemachineComponentBase).IsAssignableFrom(t) && !t.IsAbstract 
                    && t.GetCustomAttribute<CameraPipelineAttribute>() != null
                    && t.GetCustomAttribute<ObsoleteAttribute>() == null);

                foreach (var t in allTypes)
                {
                    var pipelineAttribute = t.GetCustomAttribute<CameraPipelineAttribute>();
                    stageTypes[(int)pipelineAttribute.Stage].Add(t);
                }

                // Create the static lists
                for (int i = 0; i < stageTypes.Length; ++i)
                {
                    s_StageData[i].Stage = (CinemachineCore.Stage)i;
                    s_StageData[i].Types = stageTypes[i].ToArray();

                    GUIContent[] names = new GUIContent[s_StageData[i].Types.Length];
                    names[0] = new GUIContent(
                        (i == (int)CinemachineCore.Stage.Aim) || (i == (int)CinemachineCore.Stage.Body) ? "Do nothing" : "none");
                    for (int n = 1; n < names.Length; ++n)
                        names[n] = new GUIContent(InspectorUtility.NicifyClassName(s_StageData[i].Types[n].Name));
                    s_StageData[i].PopupOptions = names;
                }
            }
        }
    }
}
