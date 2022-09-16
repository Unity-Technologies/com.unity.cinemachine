using UnityEngine;
using UnityEditor;
using UnityEngine.UIElements;
using UnityEditor.UIElements;

namespace Cinemachine.Editor
{
    [CustomEditor(typeof(CinemachineGroupFraming))]
    [CanEditMultipleObjects]
    class CinemachineGroupFramingEditor : UnityEditor.Editor
    {
        CinemachineGroupFraming Target => target as CinemachineGroupFraming;

        VisualElement m_NoTargetHelp;
        VisualElement m_GroupSizeIsZeroHelp;
        VisualElement m_PerspectiveControls;
        VisualElement m_OrthoControls;

        void OnEnable() => EditorApplication.update += UpdateVisibility;
        void OnDisable() => EditorApplication.update -= UpdateVisibility;

        public override VisualElement CreateInspectorGUI()
        {
            var serializedTarget = new SerializedObject(Target);
            var ux = new VisualElement();

            m_NoTargetHelp = ux.AddChild(new HelpBox("Tracking target must be a Target Group.", HelpBoxMessageType.Warning));
            m_GroupSizeIsZeroHelp = ux.AddChild(new HelpBox("Group size is zero, cannot frame.", HelpBoxMessageType.Warning));

            ux.Add(new PropertyField(serializedTarget.FindProperty(() => Target.FramingMode)));
            ux.Add(new PropertyField(serializedTarget.FindProperty(() => Target.FramingSize)));
            ux.Add(new PropertyField(serializedTarget.FindProperty(() => Target.Damping)));

            m_PerspectiveControls = ux.AddChild(new VisualElement());

            var sizeAdjustmentProperty = serializedTarget.FindProperty(() => Target.SizeAdjustment);
            m_PerspectiveControls.Add(new PropertyField(sizeAdjustmentProperty));
            m_PerspectiveControls.AddChild(new PropertyField(serializedTarget.FindProperty(() => Target.LateralAdjustment)));
            var fovRange = m_PerspectiveControls.AddChild(new PropertyField(serializedTarget.FindProperty(() => Target.FovRange)));
            var dollyRange = m_PerspectiveControls.AddChild(new PropertyField(serializedTarget.FindProperty(() => Target.DollyRange)));

            m_OrthoControls = ux.AddChild(new VisualElement());
            m_OrthoControls.Add(new PropertyField(serializedTarget.FindProperty(() => Target.OrthoSizeRange)));

            ux.TrackPropertyValue(sizeAdjustmentProperty, (prop) =>
            {
                bool haveDolly = prop.intValue != (int)CinemachineGroupFraming.SizeAdjustmentModes.ZoomOnly;
                bool haveZoom = prop.intValue != (int)CinemachineGroupFraming.SizeAdjustmentModes.DollyOnly;

                fovRange.SetVisible(haveZoom);
                dollyRange.SetVisible(haveDolly);
            });
            
            UpdateVisibility();
            return ux;
        }

        void UpdateVisibility()
        {
            if (target == null || m_NoTargetHelp == null)
                return; 

            ICinemachineTargetGroup group = null;
            for (int i = 0; group == null && i < targets.Length; ++i)
                group = (targets[i] as CinemachineGroupFraming).VirtualCamera.FollowTargetAsGroup;
            m_NoTargetHelp.SetVisible(group == null);
            m_GroupSizeIsZeroHelp.SetVisible(group != null && group.Sphere.radius < 0.01f);

            bool ortho = Target.VirtualCamera.State.Lens.Orthographic;
            m_PerspectiveControls.SetVisible(!ortho);
            m_OrthoControls.SetVisible(ortho);
        }

        [DrawGizmo(GizmoType.Active | GizmoType.InSelectionHierarchy, typeof(CinemachineGroupFraming))]
        static void DrawGroupComposerGizmos(CinemachineGroupFraming target, GizmoType selectionType)
        {
            // Show the group bounding box, as viewed from the camera position
            if (target.enabled && target.VirtualCamera.FollowTargetAsGroup != null)
            {
                var oldM = Gizmos.matrix;
                var oldC = Gizmos.color;

                Gizmos.matrix = target.GroupBoundsMatrix;
                Bounds b = target.GroupBounds;
                Gizmos.color = Color.yellow;
                if (target.VirtualCamera.State.Lens.Orthographic)
                    Gizmos.DrawWireCube(b.center, b.size);
                else
                {
                    var e = b.extents;
                    var z = b.center.z - e.z; // near face
                    var d = b.size.z;
                    var a = Mathf.Atan2(e.y, z) * Mathf.Rad2Deg * 2;
                    Gizmos.DrawFrustum(Vector3.zero, a, z + d, z, e.x / e.y);
                }
                Gizmos.matrix = oldM;
                Gizmos.color = oldC;
            }
        }
    }
}
