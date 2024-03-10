using UnityEditor;
using UnityEngine.UIElements;

namespace Unity.Cinemachine.Editor
{
    [CustomEditor(typeof(CinemachineHardLockToTarget))]
    [CanEditMultipleObjects]
    class CinemachineHardLockToTargetEditor : UnityEditor.Editor
    {
        public override VisualElement CreateInspectorGUI()
        {
            var ux = new VisualElement();
            this.AddMissingCmCameraHelpBox(ux, CmPipelineComponentInspectorUtility.RequiredTargets.Tracking);
            var prop = serializedObject.GetIterator();
            if (prop.NextVisible(true))
                InspectorUtility.AddRemainingProperties(ux, prop);
            return ux;
        }
    }
}
