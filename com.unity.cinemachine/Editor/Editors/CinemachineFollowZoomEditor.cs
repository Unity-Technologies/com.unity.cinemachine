using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace Cinemachine.Editor
{
    [CustomEditor(typeof(CinemachineFollowZoom))]
    [CanEditMultipleObjects]
    class CinemachineFollowZoomEditor : UnityEditor.Editor
    {
        CinemachineFollowZoom Target => target as CinemachineFollowZoom;

        CmPipelineComponentInspectorUtility m_PipelineUtility;

        void OnEnable() => m_PipelineUtility = new (this);
        void OnDisable() => m_PipelineUtility.OnDisable();

        public override VisualElement CreateInspectorGUI()
        {
            var ux = new VisualElement();

            m_PipelineUtility.AddMissingCmCameraHelpBox(ux);
            ux.Add(new PropertyField(serializedObject.FindProperty(() => Target.Width)));
            ux.Add(new PropertyField(serializedObject.FindProperty(() => Target.Damping)));
            ux.Add(new PropertyField(serializedObject.FindProperty(() => Target.FovRange)));

            m_PipelineUtility.UpdateState();
            return ux;
        }
    }
}
