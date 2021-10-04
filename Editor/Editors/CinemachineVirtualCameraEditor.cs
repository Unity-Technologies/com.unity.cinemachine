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
        VcamStageEditorPipeline m_PipelineSet = new VcamStageEditorPipeline();
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
            Undo.undoRedoPerformed += ResetTargetOnUndo;
            m_PipelineSet.Initialize(
                // GetComponent
                (stage, result) =>
                {
                    int numNullComponents = 0;
                    foreach (var obj in targets)
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
                },
                // SetComponent
                (stage, type) => 
                {
                    Undo.SetCurrentGroupName("Cinemachine pipeline change");
                    foreach (var obj in targets)
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
                });

            // We only look at the first target here, on purpose
            if (Target != null && Target.m_LockStageInInspector != null)
               foreach (var s in Target.m_LockStageInInspector)
                    m_PipelineSet.SetStageIsLocked(s);
            
            CinemachineSceneToolUtility.RegisterTool(typeof(FoVTool));
            CinemachineSceneToolUtility.RegisterTool(typeof(FarNearClipTool));
        }

        protected override void OnDisable()
        {
            Undo.undoRedoPerformed -= ResetTargetOnUndo;
            m_PipelineSet.Shutdown();
            base.OnDisable();
            
            CinemachineSceneToolUtility.UnregisterTool(typeof(FoVTool));
            CinemachineSceneToolUtility.UnregisterTool(typeof(FarNearClipTool));
        }

        protected override void OnSceneGUI()
        {
            if (!Target.m_UserIsDragging)
                m_PreviousPosition = Target.transform.position;
            if (Selection.Contains(Target.gameObject) && Tools.current == Tool.Move
                && Event.current.type == EventType.MouseDrag)
            {
                // User might be dragging our position handle
                Target.m_UserIsDragging = true;
                Vector3 delta = Target.transform.position - m_PreviousPosition;
                if (!delta.AlmostZero())
                {
                    m_PipelineSet.OnPositionDragged(delta);
                    m_PreviousPosition = Target.transform.position;
                }
            }
            else if (GUIUtility.hotControl == 0 && Target.m_UserIsDragging)
            {
                // We're not dragging anything now, but we were
                InspectorUtility.RepaintGameView();
                Target.m_UserIsDragging = false;
            }

            base.OnSceneGUI();
            m_PipelineSet.OnSceneGUI(); // call hidden editors
        }

        bool m_SoloSetByTools;
        protected override void DrawSceneTools()
        {
            var vcam = Target;
            if (!vcam.IsValid || vcam.m_ExcludedPropertiesInInspector.Contains("m_Lens"))
            {
                return;
            }

            var originalColor = Handles.color;
            Handles.color = Handles.preselectionColor;
            if (CinemachineSceneToolUtility.IsToolActive(typeof(FoVTool)))
            {
                var cameraPosition = vcam.State.FinalPosition;
                var cameraRotation = vcam.State.FinalOrientation;
                var cameraForward = cameraRotation * Vector3.forward;
                
                EditorGUI.BeginChangeCheck();
                var fovHandleId = GUIUtility.GetControlID(FocusType.Passive) + 1;
                var fieldOfView = Handles.ScaleSlider(vcam.m_Lens.FieldOfView, cameraPosition, cameraForward, 
                    cameraRotation, HandleUtility.GetHandleSize(cameraPosition), 0.1f);
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(vcam, "Changed FOV using handle in scene view.");
                    vcam.m_Lens.FieldOfView = fieldOfView;
                    InspectorUtility.RepaintGameView();
                }

                var fovHandleIsDragged = GUIUtility.hotControl == fovHandleId;
                if (fovHandleIsDragged || HandleUtility.nearestControl == fovHandleId)
                {
                    var labelStyle = new GUIStyle { normal = { textColor = Handles.selectedColor } };
                    if (vcam.m_Lens.IsPhysicalCamera)
                    {
                        Handles.Label(cameraPosition + 
                            cameraForward * HandleUtility.GetHandleSize(cameraPosition), "Focal Length (" + 
                            Camera.FieldOfViewToFocalLength(vcam.m_Lens.FieldOfView, vcam.m_Lens.SensorSize.y).
                                ToString("F1") + ")", labelStyle);
                    }
                    else
                    {
                        Handles.Label(cameraPosition + 
                            cameraForward * HandleUtility.GetHandleSize(cameraPosition), "FOV (" + 
                            vcam.m_Lens.FieldOfView.ToString("F1") + ")", labelStyle);
                    }
                }
                
                // solo this vcam when dragging
                if (fovHandleIsDragged)
                {
                    // if solo was activated by the user, then it was not the tool who set it to solo.
                    m_SoloSetByTools = m_SoloSetByTools || 
                        CinemachineBrain.SoloCamera != (ICinemachineCamera) vcam;
                    CinemachineBrain.SoloCamera = vcam;
                    InspectorUtility.RepaintGameView();
                }
                else if (m_SoloSetByTools)
                {
                    CinemachineBrain.SoloCamera = null;
                    m_SoloSetByTools = false;
                    InspectorUtility.RepaintGameView();
                }
            }
            else if (CinemachineSceneToolUtility.IsToolActive(typeof(FarNearClipTool)))
            {
                var cameraPosition = vcam.State.FinalPosition;
                var cameraRotation = vcam.State.FinalOrientation;
                var cameraForward = cameraRotation * Vector3.forward;
                var nearClipPos = cameraPosition + cameraForward * vcam.m_Lens.NearClipPlane;
                var farClipPos = cameraPosition + cameraForward * vcam.m_Lens.FarClipPlane;
                
                EditorGUI.BeginChangeCheck();
                var ncHandleId = GUIUtility.GetControlID(FocusType.Passive);
                var newNearClipPos = Handles.Slider(ncHandleId, nearClipPos, cameraForward, 
                    HandleUtility.GetHandleSize(nearClipPos) / 10f, Handles.CubeHandleCap, 0.5f); // division by 10, because this makes it roughly the same size as the default handles
                var fcHandleId = GUIUtility.GetControlID(FocusType.Passive);
                var newFarClipPos = Handles.Slider(fcHandleId, farClipPos, cameraForward, 
                    HandleUtility.GetHandleSize(farClipPos) / 10f, Handles.CubeHandleCap, 0.5f); // division by 10, because this makes it roughly the same size as the default handles
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(vcam, "Changed clip plane using handle in scene view.");
                    { // near clip
                        var diffNearClip = newNearClipPos - nearClipPos;
                        var sameDirection = Vector3.Dot(diffNearClip.normalized, cameraForward) > 0;
                        vcam.m_Lens.NearClipPlane += (sameDirection ? 1f : -1f) * diffNearClip.magnitude;
                    }
                    { // far clip
                        var diffFarClip = newFarClipPos - farClipPos;
                        var sameDirection = Vector3.Dot(diffFarClip.normalized, cameraForward) > 0;
                        vcam.m_Lens.FarClipPlane += (sameDirection ? 1f : -1f) * diffFarClip.magnitude;
                    }
                    InspectorUtility.RepaintGameView();
                }

                var nearFarClipHandleIsDragged = GUIUtility.hotControl == ncHandleId || GUIUtility.hotControl == fcHandleId;
                if (nearFarClipHandleIsDragged || 
                    HandleUtility.nearestControl == ncHandleId || HandleUtility.nearestControl == fcHandleId)
                {
                    var labelStyle = new GUIStyle { normal = { textColor = Handles.selectedColor } };
                    Handles.Label(nearClipPos,
                        "Near Clip Plane (" + vcam.m_Lens.NearClipPlane.ToString("F1") + ")", labelStyle);
                    Handles.Label(farClipPos,
                        "Far Clip Plane (" + vcam.m_Lens.FarClipPlane.ToString("F1") + ")", labelStyle);
                }
                
                // solo this vcam when dragging
                if (nearFarClipHandleIsDragged)
                {
                    // if solo was activated by the user, then it was not the tool who set it to solo.
                    m_SoloSetByTools = m_SoloSetByTools || 
                        CinemachineBrain.SoloCamera != (ICinemachineCamera) vcam;
                    CinemachineBrain.SoloCamera = vcam;
                    InspectorUtility.RepaintGameView();
                }
                else if (m_SoloSetByTools)
                {
                    CinemachineBrain.SoloCamera = null;
                    m_SoloSetByTools = false;
                    InspectorUtility.RepaintGameView();
                }
            }
            Handles.color = originalColor;
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
            m_PipelineSet.OnInspectorGUI(!IsPropertyExcluded("Header"));
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
                                catch (Exception) {} // Just skip uncooperative types
                            }
                        }
                        catch (Exception) {} // Just skip uncooperative assemblies
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
