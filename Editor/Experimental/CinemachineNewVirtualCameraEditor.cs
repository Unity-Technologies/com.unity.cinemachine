#if CINEMACHINE_EXPERIMENTAL_VCAM
using UnityEngine;
using UnityEditor;
using Cinemachine.Editor;
using Cinemachine.Utility;

namespace Cinemachine
{
    [CustomEditor(typeof(CinemachineNewVirtualCamera))]
    [CanEditMultipleObjects]
    internal sealed class CinemachineNewVirtualCameraEditor
        : CinemachineVirtualCameraBaseEditor<CinemachineNewVirtualCamera>
    {
        VcamStageEditorPipeline m_PipelineSet = new VcamStageEditorPipeline();

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
                        var vcam = obj as CinemachineNewVirtualCamera;
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
                        var vcam = obj as CinemachineNewVirtualCamera;
                        if (vcam != null)
                        {
                            Component c = vcam.GetCinemachineComponent(stage);
                            if (c != null && c.GetType() == type)
                                continue;
                            if (c != null)
                            {
                                Undo.DestroyObjectImmediate(c);
                                vcam.InvalidateComponentCache();
                            }
                            if (type != null)
                            {
                                Undo.AddComponent(vcam.gameObject, type);
                                vcam.InvalidateComponentCache();
                            }
                        }
                    }
                });
        }

        protected override void OnDisable()
        {
            Undo.undoRedoPerformed -= ResetTargetOnUndo;
            m_PipelineSet.Shutdown();
            base.OnDisable();
        }

        void ResetTargetOnUndo() 
        {
            ResetTarget();
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
            m_PipelineSet.OnInspectorGUI(true);
            DrawExtensionsWidgetInInspector();
        }

        Vector3 m_PreviousPosition; // for position dragging
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

    }
}
#endif
