using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace Cinemachine.Editor
{
    [CustomEditor(typeof(CinemachineSameAsFollowTarget))]
    [CanEditMultipleObjects]
    class CinemachineSameAsFollowTargetEditor : UnityEditor.Editor
    {
        CinemachineSameAsFollowTarget Target => target as CinemachineSameAsFollowTarget;

        CmPipelineComponentInspectorUtility m_PipelineUtility;

        void OnEnable() => m_PipelineUtility = new (this);
        void OnDisable() => m_PipelineUtility.OnDisable();

        public override VisualElement CreateInspectorGUI()
        {
            var ux = new VisualElement();

            m_PipelineUtility.AddMissingCmCameraHelpBox(ux, CmPipelineComponentInspectorUtility.RequiredTargets.Follow);
            ux.Add(new PropertyField(serializedObject.FindProperty(() => Target.Damping)));

            m_PipelineUtility.UpdateState();
            return ux;
        }
    }
}
