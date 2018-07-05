using UnityEngine;
using UnityEditor;
using Cinemachine.Editor;
using Cinemachine.Utility;
using System.Reflection;

namespace Cinemachine
{
    [CustomEditor(typeof(CinemachineNewVcam))]
    sealed class CinemachineNewVcamEditor 
        : CinemachineVirtualCameraBaseEditor<CinemachineNewVcam>
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
                    OnPositionDragged(delta);
                    mPreviousPosition = Target.transform.position;
                }
            }
            else if (GUIUtility.hotControl == 0 && Target.UserIsDragging)
            {
                // We're not dragging anything now, but we were
                UnityEditorInternal.InternalEditorUtility.RepaintAllViews();
                Target.UserIsDragging = false;
            }
        }

        // Pass the dragged event down to the CM component editors
        void OnPositionDragged(Vector3 delta)
        {
            foreach (var e in mPipelineSet.m_subeditors)
            {
                if (e != null && e.ComponentEditor != null)
                {
                    MethodInfo mi = e.ComponentEditor.GetType().GetMethod("OnVcamPositionDragged"
                        , BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
                    if (mi != null && e.ComponentEditor.target != null)
                    {
                        mi.Invoke(e.ComponentEditor, new object[] { delta } );
                    }
                }
            }
        }
    }
}