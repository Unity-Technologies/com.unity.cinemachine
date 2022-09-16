using Editor.Utility;
using UnityEditor;
using UnityEngine.UIElements;
using UnityEditor.UIElements;

namespace Cinemachine.Editor
{
    [CustomEditor(typeof(CinemachineTargetGroup))]
    internal sealed class CinemachineTargetGroupEditor : EditorWithIcon
    {
        CinemachineTargetGroup Target => target as CinemachineTargetGroup;

        public override VisualElement CreateInspectorGUI()
        {
            var ux = new VisualElement();

            ux.Add(new PropertyField(serializedObject.FindProperty(() => Target.PositionMode)));
            ux.Add(new PropertyField(serializedObject.FindProperty(() => Target.RotationMode)));
            ux.Add(new PropertyField(serializedObject.FindProperty(() => Target.UpdateMethod)));
            ux.Add(new PropertyField(serializedObject.FindProperty(() => Target.Targets)));
            return ux;
        }

#if false // enable for debugging
        [DrawGizmo(GizmoType.Active | GizmoType.InSelectionHierarchy, typeof(CinemachineTargetGroup))]
        private static void DrawGroupComposerGizmos(CinemachineTargetGroup target, GizmoType selectionType)
        {
                Gizmos.color = Color.yellow;
                var sphere = target.Sphere;
                Gizmos.DrawWireSphere(sphere.position, sphere.radius);
        }
#endif
    }
}
