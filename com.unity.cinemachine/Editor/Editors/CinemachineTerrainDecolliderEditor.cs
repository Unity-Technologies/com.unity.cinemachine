#if CINEMACHINE_PHYSICS

using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace Unity.Cinemachine.Editor
{
    [CustomEditor(typeof(CinemachineTerrainDecollider))]
    [CanEditMultipleObjects]
    class CinemachineTerrainDecolliderEditor : UnityEditor.Editor
    {
        CinemachineTerrainDecollider Target => target as CinemachineTerrainDecollider;

        public override VisualElement CreateInspectorGUI()
        {
            var ux = new VisualElement();

            this.AddMissingCmCameraHelpBox(ux);
            ux.Add(new PropertyField(serializedObject.FindProperty(() => Target.TerrainLayers)));
            ux.Add(new PropertyField(serializedObject.FindProperty(() => Target.MaximumRaycast)));
            ux.Add(new PropertyField(serializedObject.FindProperty(() => Target.CameraRadius)));
            ux.Add(new PropertyField(serializedObject.FindProperty(() => Target.SmoothingTime)));
            ux.Add(new PropertyField(serializedObject.FindProperty(() => Target.Damping)));

            return ux;
        }
    }
}
#endif
