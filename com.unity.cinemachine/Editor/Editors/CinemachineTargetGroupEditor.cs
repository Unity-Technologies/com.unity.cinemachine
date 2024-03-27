using UnityEditor;
using UnityEngine.UIElements;

namespace Unity.Cinemachine.Editor
{
    [CustomEditor(typeof(CinemachineTargetGroup))]
    class CinemachineTargetGroupEditor : UnityEditor.Editor
    {
        public override VisualElement CreateInspectorGUI()
        {
            var ux = new VisualElement();
            var prop = serializedObject.GetIterator();
            if (prop.NextVisible(true))
                InspectorUtility.AddRemainingProperties(ux, prop);
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
