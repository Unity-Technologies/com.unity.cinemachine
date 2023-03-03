using UnityEditor;
using UnityEngine.UIElements;

namespace Unity.Cinemachine.Editor
{
    [CustomEditor(typeof(CinemachineHardLookAt))]
    [CanEditMultipleObjects]
    class CinemachineHardLookAtEditor : UnityEditor.Editor
    {
        public override VisualElement CreateInspectorGUI()
        {
            var ux = new VisualElement();
            this.AddMissingCmCameraHelpBox(ux, CmPipelineComponentInspectorUtility.RequiredTargets.LookAt);
            return ux;
        }
    }
}
