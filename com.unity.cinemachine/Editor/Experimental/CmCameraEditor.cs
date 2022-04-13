using UnityEngine;
using UnityEditor;
using Cinemachine.Editor;
using System;
using System.Collections.Generic;
using Cinemachine.Utility;
using System.Reflection;

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
        
        protected override void OnEnable()
        {
            base.OnEnable();
            Undo.undoRedoPerformed += ResetTarget;

#if UNITY_2021_2_OR_NEWER
            CinemachineSceneToolUtility.RegisterTool(typeof(FoVTool));
            CinemachineSceneToolUtility.RegisterTool(typeof(FarNearClipTool));
#endif
        }

        protected override void OnDisable()
        {
            Undo.undoRedoPerformed -= ResetTarget;
            
#if UNITY_2021_2_OR_NEWER
            CinemachineSceneToolUtility.UnregisterTool(typeof(FoVTool));
            CinemachineSceneToolUtility.UnregisterTool(typeof(FarNearClipTool));
#endif
            base.OnDisable();
        }

        public override void OnInspectorGUI()
        {
            BeginInspector();

            DrawCameraStatusInInspector();
            DrawPropertyInInspector(FindProperty(x => x.m_Priority));
            DrawPropertyInInspector(FindProperty(x => x.m_StandbyUpdate));
            DrawPropertyInInspector(FindProperty(x => x.m_Transitions));

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Camera", EditorStyles.boldLabel);
            DrawLensSettingsInInspector(FindProperty(x => x.m_Lens));

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Procedural Motion", EditorStyles.boldLabel);
            DrawGlobalControlsInInspector();
            DrawInputProviderButtonInInspector();
            DrawTargetsInInspector(FindProperty(x => x.m_Follow), FindProperty(x => x.m_LookAt));
            DrawPipelinePopups();
            DrawExtensionsWidgetInInspector();

            SortComponents();
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
        
        #if UNITY_2021_2_OR_NEWER
        void OnSceneGUI()
        {
            DrawSceneTools();
        }
#endif

#if UNITY_2021_2_OR_NEWER
        void DrawSceneTools()
        {
            var cmCam = Target;
            if (cmCam == null)
                return;

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
#endif

        [InitializeOnLoad]
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
