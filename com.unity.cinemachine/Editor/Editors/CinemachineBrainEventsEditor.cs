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

            var noBrainHelp = ux.AddChild(
                new HelpBox("This behaviour will only work with a CinemachineBrain component.", 
                HelpBoxMessageType.Warning));

            ux.Add(new PropertyField(serializedObject.FindProperty(() => Target.CameraActivatedEvent)));
            ux.Add(new PropertyField(serializedObject.FindProperty(() => Target.CameraDeactivatedEvent)));
            ux.Add(new PropertyField(serializedObject.FindProperty(() => Target.CameraBlendFinishedEvent)));
            ux.Add(new PropertyField(serializedObject.FindProperty(() => Target.CameraCutEvent)));
            ux.Add(new PropertyField(serializedObject.FindProperty(() => Target.UpdatedEvent)));

            // Update state
            ux.TrackAnyUserActivity(() =>
            {
                var noBrain = false;
                for (int i = 0; i < targets.Length && !noBrain; ++i)
                {
                    var t = targets[i] as CinemachineBrainEvents;
                    noBrain |= !t.TryGetComponent<CinemachineBrain>(out _);
                }
                noBrainHelp?.SetVisible(noBrain);
            });

            return ux;
        }
    }
}
