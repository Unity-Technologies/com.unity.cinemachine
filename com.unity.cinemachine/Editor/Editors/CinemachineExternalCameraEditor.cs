using UnityEditor;
using UnityEngine.UIElements;
using UnityEditor.UIElements;

namespace Cinemachine.Editor
{
    [CustomEditor(typeof(CinemachineExternalCamera))]
    [CanEditMultipleObjects]
    class CinemachineExternalCameraEditor : UnityEditor.Editor
    {
        CinemachineExternalCamera Target => target as CinemachineExternalCamera;

        public override VisualElement CreateInspectorGUI()
        {
            var ux = new VisualElement();

            this.AddCameraStatus(ux);
            ux.Add(new PropertyField(serializedObject.FindProperty(() => Target.StandbyUpdate)));
            ux.Add(new PropertyField(serializedObject.FindProperty(() => Target.PriorityAndChannel)));
            ux.Add(new PropertyField(serializedObject.FindProperty(() => Target.TransitionHint)));
            ux.Add(new PropertyField(serializedObject.FindProperty(() => Target.LookAtTarget)));

            return ux;
        }
    }
}
