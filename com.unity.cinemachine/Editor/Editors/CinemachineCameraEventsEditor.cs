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
            this.AddMissingCmCameraHelpBox(ux);
            ux.Add(new PropertyField(serializedObject.FindProperty(() => Target.CameraActivatedEvent)));
            ux.Add(new PropertyField(serializedObject.FindProperty(() => Target.CameraDeactivatedEvent)));
            ux.Add(new PropertyField(serializedObject.FindProperty(() => Target.CameraBlendFinishedEvent)));
            return ux;
        }
    }
}
