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
            this.AddTransitionsSection(ux, new () { serializedObject.FindProperty(() => Target.TransitionHint) });
            ux.Add(new PropertyField(serializedObject.FindProperty(() => Target.LookAtTarget)));

            return ux;
        }
    }
}
