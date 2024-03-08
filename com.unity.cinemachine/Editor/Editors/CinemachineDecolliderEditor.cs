#if CINEMACHINE_PHYSICS

using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace Unity.Cinemachine.Editor
{
    [CustomEditor(typeof(CinemachineDecollider))]
    [CanEditMultipleObjects]
    class CinemachineDecolliderEditor : UnityEditor.Editor
    {
        CinemachineDecollider Target => target as CinemachineDecollider;

        public override VisualElement CreateInspectorGUI()
        {
            var ux = new VisualElement();

            this.AddMissingCmCameraHelpBox(ux);
            ux.Add(new PropertyField(serializedObject.FindProperty(() => Target.CameraRadius)));
            ux.Add(new PropertyField(serializedObject.FindProperty(() => Target.Decollision)));
            ux.Add(new PropertyField(serializedObject.FindProperty(() => Target.TerrainResolution)));

            return ux;
        }
    }
}
#endif
