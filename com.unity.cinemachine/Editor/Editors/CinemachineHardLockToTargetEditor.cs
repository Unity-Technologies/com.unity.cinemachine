using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace Cinemachine.Editor
{
    [CustomEditor(typeof(CinemachineHardLockToTarget))]
    [CanEditMultipleObjects]
    class CinemachineHardLockToTargetEditor : UnityEditor.Editor
    {
        CinemachineHardLockToTarget Target => target as CinemachineHardLockToTarget;

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
