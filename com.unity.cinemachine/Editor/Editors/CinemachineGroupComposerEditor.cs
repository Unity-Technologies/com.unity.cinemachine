using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.UIElements;

namespace Cinemachine.Editor
{
    [CustomEditor(typeof(CinemachineGroupComposer))]
    internal class CinemachineGroupComposerEditor : CinemachineRotationComposerEditor
    {
        CinemachineGroupComposer Target => target as CinemachineGroupComposer;

        public override VisualElement CreateInspectorGUI()
        {
            var ux = base.CreateInspectorGUI();

            ux.AddSpace();
            var notGroupHelp = ux.AddChild(new HelpBox(
                "The Framing settings will be ignored because the LookAt target is not a kind of ICinemachineTargetGroup.", 
                HelpBoxMessageType.Info));

            ux.Add(new PropertyField(serializedObject.FindProperty(() => Target.m_FramingMode)));
            ux.Add(new PropertyField(serializedObject.FindProperty(() => Target.m_GroupFramingSize)));
            ux.Add(new PropertyField(serializedObject.FindProperty(() => Target.m_FrameDamping)));

            var nonOrthoControls = ux.AddChild(new VisualElement());

            var adjustmentModeProperty = serializedObject.FindProperty(() => Target.m_AdjustmentMode);
            nonOrthoControls.Add(new PropertyField(adjustmentModeProperty));

            var maxDollyIn = nonOrthoControls.AddChild(new PropertyField(serializedObject.FindProperty(() => Target.m_MaxDollyIn)));
            var maxDollyOut = nonOrthoControls.AddChild(new PropertyField(serializedObject.FindProperty(() => Target.m_MaxDollyOut)));
            var minDistance = nonOrthoControls.AddChild(new PropertyField(serializedObject.FindProperty(() => Target.m_MinimumDistance)));
            var maxDistance = nonOrthoControls.AddChild(new PropertyField(serializedObject.FindProperty(() => Target.m_MaximumDistance)));
            var minFov = nonOrthoControls.AddChild(new PropertyField(serializedObject.FindProperty(() => Target.m_MinimumFOV)));
            var maxFov = nonOrthoControls.AddChild(new PropertyField(serializedObject.FindProperty(() => Target.m_MaximumFOV)));

            var orthoControls = ux.AddChild(new VisualElement());
            orthoControls.Add(new PropertyField(serializedObject.FindProperty(() => Target.m_MinimumOrthoSize)));
            orthoControls.Add(new PropertyField(serializedObject.FindProperty(() => Target.m_MaximumOrthoSize)));

            nonOrthoControls.TrackPropertyValue(adjustmentModeProperty, (prop) =>
            {
                bool haveDolly = prop.intValue != (int)CinemachineGroupComposer.AdjustmentMode.ZoomOnly;
                bool haveZoom = prop.intValue != (int)CinemachineGroupComposer.AdjustmentMode.DollyOnly;

                minFov.SetVisible(haveZoom);
                maxFov.SetVisible(haveZoom);
                maxDollyIn.SetVisible(haveDolly);
                maxDollyOut.SetVisible(haveDolly);
                minDistance.SetVisible(haveDolly);
                maxDistance.SetVisible(haveDolly);
            });
            
            // GML: This is rather evil.  Is there a better (event-driven) way?
            UpdateVisibility();
            ux.schedule.Execute(UpdateVisibility).Every(250);

            void UpdateVisibility()
            {
                bool ortho = Target.VcamState.Lens.Orthographic;
                nonOrthoControls.SetVisible(!ortho);
                orthoControls.SetVisible(ortho);

                bool noTarget = false;
                for (int i = 0; i < targets.Length; ++i)
                    noTarget |= targets[i] != null && (targets[i] as CinemachineGroupComposer).LookAtTargetAsGroup == null;
                if (notGroupHelp != null)
                    notGroupHelp.SetVisible(noTarget);
            }

            return ux;
        }

        [DrawGizmo(GizmoType.Active | GizmoType.InSelectionHierarchy, typeof(CinemachineGroupComposer))]
        private static void DrawGroupComposerGizmos(CinemachineGroupComposer target, GizmoType selectionType)
        {
            // Show the group bounding box, as viewed from the camera position
            if (target.LookAtTargetAsGroup != null)
            {
                Matrix4x4 m = Gizmos.matrix;
                Bounds b = target.LastBounds;
                Gizmos.matrix = target.LastBoundsMatrix;
                Gizmos.color = Color.yellow;

                if (target.VcamState.Lens.Orthographic)
                    Gizmos.DrawWireCube(b.center, b.size);
                else
                {
                    float z = b.center.z;
                    Vector3 e = b.extents;
                    Gizmos.DrawFrustum(
                        Vector3.zero,
                        Mathf.Atan2(e.y, z) * Mathf.Rad2Deg * 2,
                        z + e.z, z - e.z, e.x / e.y);
                }
                Gizmos.matrix = m;
            }
        }
    }
}
