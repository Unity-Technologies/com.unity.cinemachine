using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace Unity.Cinemachine.Editor
{
    [CustomEditor(typeof(CinemachineHardLockToTarget))]
    [CanEditMultipleObjects]
    class CinemachineHardLockToTargetEditor : UnityEditor.Editor
    {
        CinemachineHardLockToTarget Target => target as CinemachineHardLockToTarget;

        public override VisualElement CreateInspectorGUI()
        {
            var ux = new VisualElement();
            this.AddMissingCmCameraHelpBox(ux, CmPipelineComponentInspectorUtility.RequiredTargets.Tracking);
            ux.Add(new PropertyField(serializedObject.FindProperty(() => Target.Damping)));
            return ux;
        }
    }
}
