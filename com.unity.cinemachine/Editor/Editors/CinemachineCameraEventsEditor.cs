using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace Unity.Cinemachine.Editor
{
    [CustomEditor(typeof(CinemachineCameraEvents))]
    [CanEditMultipleObjects]
    class CinemachineCameraEventsEditor : UnityEditor.Editor
    {
        CinemachineCameraEvents Target => target as CinemachineCameraEvents;

        public override VisualElement CreateInspectorGUI()
        {
            var ux = new VisualElement();

            var wrongComponentHelp = ux.AddChild(
                new HelpBox("This behaviour will only work with the following components: "
                    + InspectorUtility.GetAssignableBehaviourNames(typeof(CinemachineVirtualCameraBase)), 
                HelpBoxMessageType.Warning));

            ux.Add(new PropertyField(serializedObject.FindProperty(() => Target.CameraActivatedEvent)));
            ux.Add(new PropertyField(serializedObject.FindProperty(() => Target.CameraDeactivatedEvent)));
            ux.Add(new PropertyField(serializedObject.FindProperty(() => Target.BlendCreatedEvent)));
            ux.Add(new PropertyField(serializedObject.FindProperty(() => Target.BlendFinishedEvent)));

            // Update state
            ux.TrackAnyUserActivity(() =>
            {
                var noCamera = false;
                for (int i = 0; i < targets.Length && !noCamera; ++i)
                {
                    var t = targets[i] as CinemachineCameraEvents;
                    if (t == null)
                        break;
                    noCamera |= !t.TryGetComponent<CinemachineVirtualCameraBase>(out _);
                }
                wrongComponentHelp?.SetVisible(noCamera);
            });

            return ux;
        }
    }
}
