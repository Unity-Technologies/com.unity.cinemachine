using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace Unity.Cinemachine.Editor
{
    [CustomEditor(typeof(CinemachineCameraOffset))]
    [CanEditMultipleObjects]
    class CinemachineCameraOffsetEditor : UnityEditor.Editor
    {
        CinemachineCameraOffset Target => target as CinemachineCameraOffset;

        public override VisualElement CreateInspectorGUI()
        {
            var ux = new VisualElement();
            this.AddMissingCmCameraHelpBox(ux);
            ux.Add(new PropertyField(serializedObject.FindProperty(() => Target.Offset)));
            ux.Add(new PropertyField(serializedObject.FindProperty(() => Target.ApplyAfter)));
            ux.Add(new PropertyField(serializedObject.FindProperty(() => Target.PreserveComposition)));

            return ux;
        }
    }
}
