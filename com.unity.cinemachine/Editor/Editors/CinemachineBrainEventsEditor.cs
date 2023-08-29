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
            ux.Add(new PropertyField(serializedObject.FindProperty(() => Target.Brain)));
            ux.Add(new PropertyField(serializedObject.FindProperty(() => Target.CameraActivatedEvent)));
            ux.Add(new PropertyField(serializedObject.FindProperty(() => Target.CameraDeactivatedEvent)));
            ux.Add(new PropertyField(serializedObject.FindProperty(() => Target.BlendCreatedEvent)));
            ux.Add(new PropertyField(serializedObject.FindProperty(() => Target.BlendFinishedEvent)));
            ux.Add(new PropertyField(serializedObject.FindProperty(() => Target.CameraCutEvent)));
            ux.AddChild(new PropertyField(serializedObject.FindProperty(() => Target.BrainUpdatedEvent)));
            return ux;
        }
    }
}
