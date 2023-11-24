using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace Unity.Cinemachine.Editor
{
    [CustomEditor(typeof(CinemachineHardLookAt))]
    [CanEditMultipleObjects]
    class CinemachineHardLookAtEditor : UnityEditor.Editor
    {
        CinemachineHardLookAt Target => target as CinemachineHardLookAt;

        public override VisualElement CreateInspectorGUI()
        {
            var ux = new VisualElement();
            this.AddMissingCmCameraHelpBox(ux, CmPipelineComponentInspectorUtility.RequiredTargets.LookAt);
            ux.Add(new PropertyField(serializedObject.FindProperty(() => Target.LookAtOffset)));
            return ux;
        }
    }
}
