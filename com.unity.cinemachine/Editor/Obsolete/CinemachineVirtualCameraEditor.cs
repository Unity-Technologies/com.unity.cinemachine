#if !CINEMACHINE_NO_CM2_SUPPORT
using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;

namespace Unity.Cinemachine.Editor
{
    [Obsolete]
    [CustomEditor(typeof(CinemachineVirtualCamera))]
    [CanEditMultipleObjects]
    class CinemachineVirtualCameraEditor : CinemachineVirtualCameraBaseEditor<CinemachineVirtualCamera>
    {
        VcamStageEditorPipeline m_PipelineSet = new VcamStageEditorPipeline();

        [MenuItem("CONTEXT/CinemachineVirtualCamera/Adopt Game View Camera Settings")]
        static void AdoptGameViewCameraSettings(MenuCommand command)
        {
            var vcam = command.context as CinemachineVirtualCamera;
            var brain = CinemachineCore.FindPotentialTargetBrain(vcam);
            if (brain != null)
            {
                vcam.m_Lens.SetFromLensSettings(brain.State.Lens);
                vcam.transform.SetPositionAndRotation(brain.transform.position, brain.transform.rotation);
            }
        }

        [MenuItem("CONTEXT/CinemachineVirtualCamera/Adopt Scene View Camera Settings")]
        static void AdoptSceneViewCameraSettings(MenuCommand command)
        {
            var vcam = command.context as CinemachineVirtualCamera;
            vcam.m_Lens.SetFromLensSettings(CinemachineMenu.MatchSceneViewCamera(vcam.transform));
        }
        
        /// <summary>Get the property names to exclude in the inspector.  
        /// Implementation should call the base class implementation</summary>
        /// <param name="excluded">Add the names to this list</param>
        protected override void GetExcludedPropertiesInInspector(List<string> excluded)
        {
            base.GetExcludedPropertiesInInspector(excluded);
            if (Target.m_ExcludedPropertiesInInspector != null)
                excluded.AddRange(Target.m_ExcludedPropertiesInInspector);
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
        }

        protected override void OnDisable()
        {
            Undo.undoRedoPerformed -= ResetTargetOnUndo;
            m_PipelineSet.Shutdown();
            base.OnDisable();
        }

        void OnSceneGUI()
        {
            m_PipelineSet.OnSceneGUI(); // call hidden editors' OnSceneGUI
        }

        public override void OnInspectorGUI()
        {
            if (Target.GetComponentOwner() == null)
            {
                EditorGUILayout.HelpBox(
                    "It's not possible to add this component to a prefab instance.  "
                    + "Instead, you can open the Prefab in Prefab Mode or unpack "
                    + "the Prefab instance to remove the Prefab connection.",
                    MessageType.Error);
                return;
            }
            BeginInspector();
            DrawNonExcludedHeaderInInspector();
            DrawPropertyInInspector(serializedObject.FindProperty(() => Target.Priority));
            DrawPropertyInInspector(serializedObject.FindProperty(() => Target.OutputChannel));
            DrawTargetsInInspector(serializedObject.FindProperty(() => Target.m_Follow), serializedObject.FindProperty(() => Target.m_LookAt));
            DrawPropertyInInspector(serializedObject.FindProperty(() => Target.StandbyUpdate));
            DrawPropertyInInspector(serializedObject.FindProperty(() => Target.m_Lens));
            DrawRemainingPropertiesInInspector();
            m_PipelineSet.OnInspectorGUI(!IsPropertyExcluded("Header"));
            DrawNonExcludedExtensionsWidgetInInspector();
        }

        void DrawNonExcludedHeaderInInspector()
        {
            if (!IsPropertyExcluded("Header"))
            {
                UpgradeManagerInspectorHelpers.IMGUI_DrawUpgradeControls(this, "CinemachineCamera");
                DrawCameraStatusInInspector();
                DrawGlobalControlsInInspector();
                DrawInputProviderButtonInInspector();
                ExcludeProperty("Header");
            }
        }
        
        protected void DrawNonExcludedExtensionsWidgetInInspector()
        {
            if (!IsPropertyExcluded("Extensions"))
            {
                DrawExtensionsWidgetInInspector();
                ExcludeProperty("Extensions");
            }
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
                    // Recycle existing pipeline child (if any)
                    GameObject go = null;
                    foreach (Transform child in vcam.transform)
                    {
                        if (child.GetComponent<CinemachinePipeline>() != null)
                        {
                            go = child.gameObject;
                            break;
                        }
                    }
                    if (go == null)
                    {
                        // Create a new pipeline - can't do it if prefab instance
                        if (PrefabUtility.IsPartOfAnyPrefab(vcam.gameObject))
                            return null;
                        go =  ObjectFactory.CreateGameObject(name);
                        Undo.RegisterCreatedObjectUndo(go, "created pipeline");
                        Undo.SetTransformParent(go.transform, vcam.transform, "parenting pipeline");
                        Undo.AddComponent<CinemachinePipeline>(go);
                    }

                    var oldStuff = go.GetComponents<CinemachineComponentBase>();
                    foreach (var c in oldStuff)
                        Undo.DestroyObjectImmediate(c);

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
                    var oldStuff = pipeline.GetComponents<CinemachineComponentBase>();
                    foreach (var c in oldStuff)
                        Undo.DestroyObjectImmediate(c);
                    // Cannot create or destroy child objects if prefab.
                    // Just leave it there in that case, it will get discovered and recycled.
                    if (!PrefabUtility.IsPartOfAnyPrefab(pipeline))
                        Undo.DestroyObjectImmediate(pipeline);
                };
            }
        }
    }
}
#endif
