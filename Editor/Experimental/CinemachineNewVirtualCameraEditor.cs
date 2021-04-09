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
        PipelineStageSubeditorSet m_PipelineSet = new PipelineStageSubeditorSet();

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

        void ResetTargetOnUndo() 
        {
            ResetTarget();
        }

        public override void OnInspectorGUI()
        {
            // Ordinary properties
            BeginInspector();
            DrawHeaderInInspector();
            DrawPropertyInInspector(FindProperty(x => x.m_Priority));
            DrawTargetsInInspector(FindProperty(x => x.m_Follow), FindProperty(x => x.m_LookAt));
            DrawPropertyInInspector(FindProperty(x => x.m_StandbyUpdate));
            DrawLensSettingsInInspector(FindProperty(x => x.m_Lens));
            DrawRemainingPropertiesInInspector();

            // Pipeline Stages
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

            // Extensions
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

        internal class PipelineStageSubeditorSet
        {
            public VcamStageEditor[] m_subeditors;
            UnityEditor.Editor m_ParentEditor;

            public void CreateSubeditors(UnityEditor.Editor parentEditor)
            {
                m_ParentEditor = parentEditor;
                m_subeditors = new VcamStageEditor[(int)CinemachineCore.Stage.Finalize];
                if (m_ParentEditor.target as CinemachineNewVirtualCamera == null)
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
                    };
                    ed.SetComponent = (stage, type) => 
                    {
                        Undo.SetCurrentGroupName("Cinemachine pipeline change");
                        foreach (var obj in m_ParentEditor.targets)
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
    }
}
#endif
