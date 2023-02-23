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

        public override VisualElement CreateInspectorGUI()
        {
            var ux = new VisualElement();

            this.AddMissingCmCameraHelpBox(ux, CmPipelineComponentInspectorUtility.RequiredTargets.Tracking);
            ux.Add(new PropertyField(serializedObject.FindProperty(() => Target.Damping)));

            return ux;
        }
    }
}
