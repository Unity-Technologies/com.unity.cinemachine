using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace Unity.Cinemachine.Editor
{
    [CustomEditor(typeof(CinemachineCameraManagerEvents))]
    [CanEditMultipleObjects]
    class CinemachineCameraManagerEventsEditor : UnityEditor.Editor
    {
        CinemachineCameraManagerEvents Target => target as CinemachineCameraManagerEvents;

        public override VisualElement CreateInspectorGUI()
        {
            var ux = new VisualElement();
            ux.Add(new PropertyField(serializedObject.FindProperty(() => Target.CameraManager)));
            ux.Add(new PropertyField(serializedObject.FindProperty(() => Target.CameraActivatedEvent)));
            ux.Add(new PropertyField(serializedObject.FindProperty(() => Target.CameraDeactivatedEvent)));
            ux.Add(new PropertyField(serializedObject.FindProperty(() => Target.BlendCreatedEvent)));
            ux.Add(new PropertyField(serializedObject.FindProperty(() => Target.BlendFinishedEvent)));
            ux.Add(new PropertyField(serializedObject.FindProperty(() => Target.CameraCutEvent)));
            return ux;
        }
    }
}
