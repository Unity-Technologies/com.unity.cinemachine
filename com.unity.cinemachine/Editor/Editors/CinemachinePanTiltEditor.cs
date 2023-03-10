using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace Unity.Cinemachine.Editor
{
    [CustomEditor(typeof(CinemachinePanTilt))]
    [CanEditMultipleObjects]
    class CinemachinePanTiltEditor : UnityEditor.Editor
    {
        CinemachinePanTilt Target => target as CinemachinePanTilt;

        public override VisualElement CreateInspectorGUI()
        {
            var ux = new VisualElement();
            this.AddMissingCmCameraHelpBox(ux);
            ux.Add(new PropertyField(serializedObject.FindProperty(() => Target.ReferenceFrame)));
            ux.Add(new PropertyField(serializedObject.FindProperty(() => Target.PanAxis)));
            ux.Add(new PropertyField(serializedObject.FindProperty(() => Target.TiltAxis)));
            ux.AddSpace();
            this.AddInputControllerHelp(ux, "PanTilt has no input axis controller behaviour.");
            return ux;
        }
    }
}
