#if CINEMACHINE_PHYSICS
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace Cinemachine.Editor
{
    [CustomEditor(typeof(CinemachineThirdPersonAim))]
    [CanEditMultipleObjects]
    class CinemachineThirdPersonAimEditor : UnityEditor.Editor
    {
        CinemachineThirdPersonAim Target => target as CinemachineThirdPersonAim;

        public override VisualElement CreateInspectorGUI()
        {
            var ux = new VisualElement();

            this.AddMissingCmCameraHelpBox(ux);
            ux.Add(new PropertyField(serializedObject.FindProperty(() => Target.AimCollisionFilter)));
            ux.Add(new PropertyField(serializedObject.FindProperty(() => Target.IgnoreTag)));
            ux.Add(new PropertyField(serializedObject.FindProperty(() => Target.AimDistance)));
            ux.Add(new PropertyField(serializedObject.FindProperty(() => Target.NoiseCancellation)));

            return ux;
        }
    }
}
#endif
