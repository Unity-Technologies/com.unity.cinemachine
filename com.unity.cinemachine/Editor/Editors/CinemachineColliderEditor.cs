using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

namespace Cinemachine.Editor
{
#if CINEMACHINE_PHYSICS
    [CustomEditor(typeof(CinemachineCollider))]
    [CanEditMultipleObjects]
    sealed class CinemachineColliderEditor : BaseEditor<CinemachineCollider>
    {
        /// <summary>Get the property names to exclude in the inspector.</summary>
        /// <param name="excluded">Add the names to this list</param>
        protected override void GetExcludedPropertiesInInspector(List<string> excluded)
        {
            base.GetExcludedPropertiesInInspector(excluded);
            if (!Target.AvoidObstacles)
            {
                excluded.Add(FieldPath(x => x.DistanceLimit));
                excluded.Add(FieldPath(x => x.CameraRadius));
                excluded.Add(FieldPath(x => x.Strategy));
                excluded.Add(FieldPath(x => x.MaximumEffort));
                excluded.Add(FieldPath(x => x.Damping));
                excluded.Add(FieldPath(x => x.DampingWhenOccluded));
                excluded.Add(FieldPath(x => x.SmoothingTime));
            }
            else if (Target.Strategy == CinemachineCollider.ResolutionStrategy.PullCameraForward)
            {
                excluded.Add(FieldPath(x => x.MaximumEffort));
            }
        }

        public override void OnInspectorGUI()
        {
            BeginInspector();

            if (Target.AvoidObstacles && Target.VirtualCamera != null
                    && !Target.VirtualCamera.State.HasLookAt())
                EditorGUILayout.HelpBox(
                    "Avoid Obstacles requires a LookAt target.",
                    MessageType.Warning);

            DrawRemainingPropertiesInInspector();
        }

        [DrawGizmo(GizmoType.Active | GizmoType.Selected, typeof(CinemachineCollider))]
        static void DrawColliderGizmos(CinemachineCollider collider, GizmoType type)
        {
            CinemachineVirtualCameraBase vcam = (collider != null) ? collider.VirtualCamera : null;
            if (vcam != null && collider.enabled)
            {
                Color oldColor = Gizmos.color;
                Vector3 pos = vcam.State.GetFinalPosition();
                if (collider.AvoidObstacles && vcam.State.HasLookAt())
                {
                    Gizmos.color = CinemachineColliderPrefs.FeelerColor;
                    if (collider.CameraRadius > 0)
                        Gizmos.DrawWireSphere(pos, collider.CameraRadius);

                    Vector3 forwardFeelerVector = (vcam.State.ReferenceLookAt - pos).normalized;
                    float distance = collider.DistanceLimit;
                    Gizmos.DrawLine(pos, pos + forwardFeelerVector * distance);

                    // Show the avoidance path, for debugging
                    List<List<Vector3>> debugPaths = collider.DebugPaths;
                    foreach (var path in debugPaths)
                    {
                        Gizmos.color = CinemachineColliderPrefs.FeelerHitColor;
                        Vector3 p0 = vcam.State.ReferenceLookAt;
                        foreach (var p in path)
                        {
                            Gizmos.DrawLine(p0, p);
                            p0 = p;
                        }
                        Gizmos.DrawLine(p0, pos);
                    }
                }
                Gizmos.color = oldColor;
            }
        }
    }
#endif
}
