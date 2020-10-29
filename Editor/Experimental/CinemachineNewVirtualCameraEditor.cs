#if CINEMACHINE_EXPERIMENTAL_VCAM
using UnityEngine;
using UnityEditor;
using Cinemachine.Editor;
using Cinemachine.Utility;

namespace Cinemachine
{
    [CustomEditor(typeof(CinemachineNewVirtualCamera))]
    sealed class CinemachineNewVirtualCameraEditor
        : CinemachineVirtualCameraBaseEditor<CinemachineNewVirtualCamera>
    {
        VcamPipelineStageSubeditorSet mPipelineSet = new VcamPipelineStageSubeditorSet();

        protected override void OnEnable()
        {
            base.OnEnable();
            mPipelineSet.CreateSubeditors(this);
        }

        protected override void OnDisable()
        {
            mPipelineSet.Shutdown();
            base.OnDisable();
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
            var components = Target.ComponentCache;
            for (int i = 0; i < mPipelineSet.m_subeditors.Length; ++i)
            {
                var ed = mPipelineSet.m_subeditors[i];
                if (ed == null)
                    continue;
                if (!ed.HasImplementation)
                    continue;
                ed.OnInspectorGUI(components[i]); // may destroy component
            }

            // Extensions
            DrawExtensionsWidgetInInspector();
        }

        Vector3 mPreviousPosition; // for position dragging
        private void OnSceneGUI()
        {
            if (!Target.UserIsDragging)
                mPreviousPosition = Target.transform.position;
            if (Selection.Contains(Target.gameObject) && Tools.current == Tool.Move
                && Event.current.type == EventType.MouseDrag)
            {
                // User might be dragging our position handle
                Target.UserIsDragging = true;
                Vector3 delta = Target.transform.position - mPreviousPosition;
                if (!delta.AlmostZero())
                {
                    mPipelineSet.OnPositionDragged(delta);
                    mPreviousPosition = Target.transform.position;
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
