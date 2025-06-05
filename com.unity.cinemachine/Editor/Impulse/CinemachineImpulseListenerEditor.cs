using UnityEditor;
using UnityEngine.UIElements;

namespace Unity.Cinemachine.Editor
{
    [CustomEditor(typeof(CinemachineImpulseListener))]
    [CanEditMultipleObjects]
    class CinemachineImpulseListenerEditor : UnityEditor.Editor
    {
        public override VisualElement CreateInspectorGUI()
        {
            var ux = new VisualElement();

            this.AddMissingCmCameraHelpBox(ux);

            if (!(target as CinemachineImpulseListener).TryGetComponent<CinemachineVirtualCameraBase>(out _))
            {
                ux.Add(new HelpBox(
                    "To add impulse listening capabilities to GameObjects that are not CinemachineCameras, "
                        + "you can use the <b>Cinemachine External Impulse Listener</b> behaviour instead.",
                    HelpBoxMessageType.Info));
            }
            else
            {
                ux.Add(new HelpBox(
                    "The Impulse Listener will respond to signals broadcast by any CinemachineImpulseSource.", 
                    HelpBoxMessageType.Info));
            }
            var prop = serializedObject.GetIterator();
            if (prop.NextVisible(true))
                InspectorUtility.AddRemainingProperties(ux, prop);
            return ux;
        }
    }
}
