using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;
using Cinemachine.Utility;
using System.Reflection;
using System.Linq;

namespace Cinemachine.Editor
{
    [CustomEditor(typeof(CinemachineVirtualCamera))]
    [CanEditMultipleObjects]
    internal class CinemachineVirtualCameraEditor
        : CinemachineVirtualCameraBaseEditor<CinemachineVirtualCamera>
    {
        PipelineStageSubeditorSet m_PipelineSet = new PipelineStageSubeditorSet();
        Vector3 m_PreviousPosition;

        [MenuItem("CONTEXT/CinemachineVirtualCamera/Adopt Current Camera Settings")]
        static void AdoptCurrentCameraSettings(MenuCommand command)
        {
            var vcam = command.context as CinemachineVirtualCamera;
            var brain = CinemachineCore.Instance.FindPotentialTargetBrain(vcam);
            if (brain != null)
            {
                vcam.m_Lens = brain.CurrentCameraState.Lens;
                vcam.transform.position = brain.transform.position;
                vcam.transform.rotation = brain.transform.rotation;
            }
        }

        [MenuItem("CONTEXT/CinemachineVirtualCamera/Adopt Scene View Camera Settings")]
        static void AdoptSceneViewCameraSettings(MenuCommand command)
        {
            var vcam = command.context as CinemachineVirtualCamera;
            CinemachineMenu.SetVcamFromSceneView(vcam);
        }
        
        protected override void OnEnable()
        {
            base.OnEnable();
            m_PipelineSet.CreateSubeditors(this);
            Undo.undoRedoPerformed += ResetTargetOnUndo;
        }

        protected override void OnDisable()
        {
            Undo.undoRedoPerformed -= ResetTargetOnUndo;
            m_PipelineSet.Shutdown();
            base.OnDisable();
        }

        private void OnSceneGUI()
        {
            if (!Target.UserIsDragging)
                m_PreviousPosition = Target.transform.position;
            if (Selection.Contains(Target.gameObject) && Tools.current == Tool.Move
                && Event.current.type == EventType.MouseDrag)
            {
                // User might be dragging our position handle
                Target.UserIsDragging = true;
                Vector3 delta = Target.transform.position - m_PreviousPosition;
                if (!delta.AlmostZero())
                {
                    m_PipelineSet.OnPositionDragged(delta);
                    m_PreviousPosition = Target.transform.position;
                }
            }
            else if (GUIUtility.hotControl == 0 && Target.UserIsDragging)
            {
                // We're not dragging anything now, but we were
                InspectorUtility.RepaintGameView();
                Target.UserIsDragging = false;
            }
        }

        public override void OnInspectorGUI()
        {
            BeginInspector();
            DrawHeaderInInspector();
            DrawPropertyInInspector(FindProperty(x => x.m_Priority));
            DrawTargetsInInspector(FindProperty(x => x.m_Follow), FindProperty(x => x.m_LookAt));
            DrawPropertyInInspector(FindProperty(x => x.m_StandbyUpdate));
            DrawLensSettingsInInspector(FindProperty(x => x.m_Lens));
            DrawRemainingPropertiesInInspector();

            EditorGUILayout.Space();
            EditorGUILayout.LabelField(VcamStageEditor.ProceduralMotionLabel, EditorStyles.boldLabel);
            for (int i = 0; i < m_PipelineSet.m_subeditors.Length; ++i)
            {
                var ed = m_PipelineSet.m_subeditors[i];
                if (ed == null)
                    continue;
                if (!ed.HasImplementation)
                    continue;
                ed.OnInspectorGUI(); // may destroy component
            }

            DrawExtensionsWidgetInInspector();
        }

        void ResetTargetOnUndo() 
        {
            ResetTarget();
            for (int i = 0; i < targets.Length; ++i)
                (targets[i] as CinemachineVirtualCamera).InvalidateComponentPipeline();
        }

        /// <summary>
        /// Register with CinemachineVirtualCamera to create the pipeline in an undo-friendly manner
        /// </summary>
        [InitializeOnLoad]
        class CreatePipelineWithUndo
        {
            static CreatePipelineWithUndo()
            {
                CinemachineVirtualCamera.CreatePipelineOverride =
                    (CinemachineVirtualCamera vcam, string name, CinemachineComponentBase[] copyFrom) =>
                    {
                        // Create a new pipeline
                        GameObject go =  InspectorUtility.CreateGameObject(name);
                        Undo.RegisterCreatedObjectUndo(go, "created pipeline");
                        bool partOfPrefab = PrefabUtility.IsPartOfAnyPrefab(vcam.gameObject);
                        if (!partOfPrefab)
                            Undo.SetTransformParent(go.transform, vcam.transform, "parenting pipeline");
                        Undo.AddComponent<CinemachinePipeline>(go);

                        // If copying, transfer the components
                        if (copyFrom != null)
                        {
                            foreach (Component c in copyFrom)
                            {
                                Component copy = Undo.AddComponent(go, c.GetType());
                                Undo.RecordObject(copy, "copying pipeline");
                                ReflectionHelpers.CopyFields(c, copy);
                            }
                        }
                        return go.transform;
                    };
                CinemachineVirtualCamera.DestroyPipelineOverride = (GameObject pipeline) =>
                    {
                        Undo.DestroyObjectImmediate(pipeline);
                    };
            }
        }

        class PipelineStageSubeditorSet
        {
            public VcamStageEditor[] m_subeditors;
            UnityEditor.Editor m_ParentEditor;

            public void CreateSubeditors(UnityEditor.Editor parentEditor)
            {
                m_ParentEditor = parentEditor;
                m_subeditors = new VcamStageEditor[(int)CinemachineCore.Stage.Finalize];
                if (m_ParentEditor.target as CinemachineVirtualCamera == null)
                    return;
                for (CinemachineCore.Stage stage = CinemachineCore.Stage.Body;
                    stage < CinemachineCore.Stage.Finalize; ++stage)
                {
                    var ed = new VcamStageEditor(stage);
                    m_subeditors[(int)stage] = ed;
                    ed.GetComponent = (stage, result) =>
                    {
                        int numNullComponents = 0;
                        foreach (var obj in m_ParentEditor.targets)
                        {
                            var vcam = obj as CinemachineVirtualCamera;
                            if (vcam != null)
                            {
                                var c = vcam.GetCinemachineComponent(stage);
                                if (c != null)
                                    result.Add(c);
                                else
                                    ++numNullComponents;
                            }
                        }
                        return numNullComponents;
                    };
                    ed.SetComponent = (stage, type) => 
                    {
                        Undo.SetCurrentGroupName("Cinemachine pipeline change");
                        foreach (var obj in m_ParentEditor.targets)
                        {
                            var vcam = obj as CinemachineVirtualCamera;
                            Transform owner = vcam == null ? null : vcam.GetComponentOwner();
                            if (owner == null)
                                continue; // maybe it's a prefab
                            var c = vcam.GetCinemachineComponent(stage);
                            if (c != null && c.GetType() == type)
                                continue;
                            if (c != null)
                            {
                                Undo.DestroyObjectImmediate(c);
                                vcam.InvalidateComponentPipeline();
                            }
                            if (type != null)
                            {
                                Undo.AddComponent(owner.gameObject, type);
                                vcam.InvalidateComponentPipeline();
                            }
                        }
                    };
                }
            }

            public void Shutdown()
            {
                if (m_subeditors != null)
                {
                    for (int i = 0; i < m_subeditors.Length; ++i)
                    {
                        if (m_subeditors[i] != null)
                            m_subeditors[i].Shutdown();
                        m_subeditors[i] = null;
                    }
                    m_subeditors = null;
                }
                m_ParentEditor = null;
            }

            // Pass the dragged event down to the CM component editors
            public void OnPositionDragged(Vector3 delta)
            {
                foreach (var e in m_subeditors)
                    if (e != null)
                        e.OnPositionDragged(delta);
            }
        }

        // Because the cinemachine components are attached to hidden objects, their
        // gizmos don't get drawn by default.  We have to do it explicitly.
        [InitializeOnLoad]
        static class CollectGizmoDrawers
        {
            static CollectGizmoDrawers()
            {
                m_GizmoDrawers = new Dictionary<Type, MethodInfo>();
                string definedIn = typeof(CinemachineComponentBase).Assembly.GetName().Name;
                Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
                foreach (Assembly assembly in assemblies)
                {
                    // Note that we have to call GetName().Name.  Just GetName() will not work.
                    if ((!assembly.GlobalAssemblyCache)
                        && ((assembly.GetName().Name == definedIn)
                            || assembly.GetReferencedAssemblies().Any(a => a.Name == definedIn)))
                    {
                        try
                        {
                            foreach (var type in assembly.GetTypes())
                            {
                                try
                                {
                                    bool added = false;
                                    foreach (var method in type.GetMethods(
                                                 BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static))
                                    {
                                        if (added)
                                            break;
                                        if (!method.IsStatic)
                                            continue;
                                        var attributes = method.GetCustomAttributes(typeof(DrawGizmo), true) as DrawGizmo[];
                                        foreach (var a in attributes)
                                        {
                                            if (typeof(CinemachineComponentBase).IsAssignableFrom(a.drawnType) && !a.drawnType.IsAbstract)
                                            {
                                                m_GizmoDrawers.Add(a.drawnType, method);
                                                added = true;
                                                break;
                                            }
                                        }
                                    }
                                }
                                catch (System.Exception) {} // Just skip uncooperative types
                            }
                        }
                        catch (System.Exception) {} // Just skip uncooperative assemblies
                    }
                }
            }
            public static Dictionary<Type, MethodInfo> m_GizmoDrawers;
        }

        [DrawGizmo(GizmoType.Active | GizmoType.InSelectionHierarchy, typeof(CinemachineVirtualCamera))]
        internal static void DrawVirtualCameraGizmos(CinemachineVirtualCamera vcam, GizmoType selectionType)
        {
            var pipeline = vcam.GetComponentPipeline();
            if (pipeline != null)
            {
                foreach (var c in pipeline)
                {
                    if (c == null)
                        continue;

                    MethodInfo method;
                    if (CollectGizmoDrawers.m_GizmoDrawers.TryGetValue(c.GetType(), out method))
                    {
                        if (method != null)
                            method.Invoke(null, new object[] {c, selectionType});
                    }
                }
            }
        }
    }
}
