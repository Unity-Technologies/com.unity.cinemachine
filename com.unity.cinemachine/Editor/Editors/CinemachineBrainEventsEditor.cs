using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace Unity.Cinemachine.Editor
{
    [CustomEditor(typeof(CinemachineBrainEvents))]
    [CanEditMultipleObjects]
    class CinemachineBrainEventsEditor : UnityEditor.Editor
    {
        CinemachineBrainEvents Target => target as CinemachineBrainEvents;

        public override VisualElement CreateInspectorGUI()
        {
            var ux = new VisualElement();

            var wrongComponentHelp = ux.AddChild(
                new HelpBox("This behaviour will only work with the following components: "
                    + InspectorUtility.GetAssignableBehaviourNames(typeof(ICinemachineMixer)), 
                HelpBoxMessageType.Warning));

            ux.Add(new PropertyField(serializedObject.FindProperty(() => Target.CameraActivatedEvent)));
            ux.Add(new PropertyField(serializedObject.FindProperty(() => Target.CameraDeactivatedEvent)));
            ux.Add(new PropertyField(serializedObject.FindProperty(() => Target.CameraBlendFinishedEvent)));
            ux.Add(new PropertyField(serializedObject.FindProperty(() => Target.CameraCutEvent)));
            var brainEvent = ux.AddChild(
                new PropertyField(serializedObject.FindProperty(() => Target.BrainUpdatedEvent)));

            // Update state
            ux.TrackAnyUserActivity(() =>
            {
                var haveBrain = false;
                var noMixer = false;
                for (int i = 0; i < targets.Length; ++i)
                {
                    var t = targets[i] as CinemachineBrainEvents;
                    if (t == null)
                        break;
                    noMixer |= !t.TryGetComponent<ICinemachineMixer>(out _);
                    haveBrain |= t.TryGetComponent<CinemachineBrain>(out _);
                }
                wrongComponentHelp?.SetVisible(noMixer);
                brainEvent?.SetVisible(haveBrain);
            });

            return ux;
        }
    }
}
