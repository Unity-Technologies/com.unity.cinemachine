using UnityEditor;
using UnityEngine.UIElements;

namespace Unity.Cinemachine.Editor
{
    [CustomEditor(typeof(CinemachinePanTilt))]
    [CanEditMultipleObjects]
    class CinemachinePanTiltEditor : UnityEditor.Editor
    {
        public override VisualElement CreateInspectorGUI()
        {
            var ux = new VisualElement();
            this.AddMissingCmCameraHelpBox(ux);
            var prop = serializedObject.GetIterator();
            if (prop.NextVisible(true))
                InspectorUtility.AddRemainingProperties(ux, prop);

            ux.AddSpace();
            this.AddInputControllerHelp(ux, "PanTilt has no input axis controller behaviour.");
            return ux;
        }
    }
}
