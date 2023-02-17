using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace Cinemachine.Editor
{
    [CustomEditor(typeof(CinemachineFollowZoom))]
    [CanEditMultipleObjects]
    class CinemachineFollowZoomEditor : UnityEditor.Editor
    {
        CinemachineFollowZoom Target => target as CinemachineFollowZoom;

        public override VisualElement CreateInspectorGUI()
        {
            var ux = new VisualElement();

            this.AddMissingCmCameraHelpBox(ux);
            ux.Add(new PropertyField(serializedObject.FindProperty(() => Target.Width)));
            ux.Add(new PropertyField(serializedObject.FindProperty(() => Target.Damping)));
            ux.Add(new PropertyField(serializedObject.FindProperty(() => Target.FovRange)));

            return ux;
        }
    }
}
