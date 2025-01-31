#if !CINEMACHINE_NO_CM2_SUPPORT
using UnityEngine;
using UnityEditor;

namespace Unity.Cinemachine.Editor
{
    [System.Obsolete]
    [CustomEditor(typeof(Cinemachine3rdPersonFollow))]
    [CanEditMultipleObjects]
    class Cinemachine3rdPersonFollowEditor : BaseEditor<Cinemachine3rdPersonFollow>
    {
        [DrawGizmo(GizmoType.Active | GizmoType.Selected, typeof(Cinemachine3rdPersonFollow))]
        static void Draw3rdPersonGizmos(Cinemachine3rdPersonFollow target, GizmoType selectionType)
        {
            if (target.IsValid)
            {
                var isLive = CinemachineCore.IsLive(target.VirtualCamera);
                Color originalGizmoColour = Gizmos.color;
                Gizmos.color = isLive
                    ? CinemachineCorePrefs.ActiveGizmoColour.Value
                    : CinemachineCorePrefs.InactiveGizmoColour.Value;

                target.GetRigPositions(out Vector3 root, out Vector3 shoulder, out Vector3 hand);
                Gizmos.DrawLine(root, shoulder);
                Gizmos.DrawLine(shoulder, hand);
                Gizmos.DrawSphere(root, 0.02f);
                Gizmos.DrawSphere(shoulder, 0.02f);
#if CINEMACHINE_PHYSICS
                Gizmos.DrawSphere(hand, target.CameraRadius);

                if (isLive)
                    Gizmos.color = CinemachineCorePrefs.BoundaryObjectGizmoColour.Value;

                Gizmos.DrawSphere(target.VirtualCamera.State.RawPosition, target.CameraRadius);
#endif

                Gizmos.color = originalGizmoColour;
            }
        }

        public override void OnInspectorGUI()
        {
            BeginInspector();
            this.IMGUI_DrawMissingCmCameraHelpBox();
            DrawRemainingPropertiesInInspector();
        }
    }
}
#endif
