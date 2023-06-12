#if CINEMACHINE_PHYSICS

using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace Unity.Cinemachine.Editor
{
    [CustomEditor(typeof(CinemachineDeoccluder))]
    [CanEditMultipleObjects]
    class CinemachineDeoccluderEditor : UnityEditor.Editor
    {
        CinemachineDeoccluder Target => target as CinemachineDeoccluder;
        static List<List<Vector3>> s_pathsCache;

        public override VisualElement CreateInspectorGUI()
        {
            var ux = new VisualElement();

            this.AddMissingCmCameraHelpBox(ux);
            ux.Add(new PropertyField(serializedObject.FindProperty(() => Target.CollideAgainst)));
            ux.Add(new PropertyField(serializedObject.FindProperty(() => Target.IgnoreTag)));
            ux.Add(new PropertyField(serializedObject.FindProperty(() => Target.TransparentLayers)));
            ux.Add(new PropertyField(serializedObject.FindProperty(() => Target.MinimumDistanceFromTarget)));
            ux.Add(new PropertyField(serializedObject.FindProperty(() => Target.AvoidObstacles)));
            ux.Add(new PropertyField(serializedObject.FindProperty(() => Target.ShotQualityEvaluation)));

            return ux;
        }

        [DrawGizmo(GizmoType.Active | GizmoType.Selected, typeof(CinemachineDeoccluder))]
        static void DrawColliderGizmos(CinemachineDeoccluder collider, GizmoType type)
        {
            CinemachineVirtualCameraBase vcam = (collider != null) ? collider.ComponentOwner : null;
            if (vcam != null && collider.enabled)
            {
                Color oldColor = Gizmos.color;
                Vector3 pos = vcam.State.GetFinalPosition();
                if (collider.AvoidObstacles.Enabled && vcam.State.HasLookAt())
                {
                    Gizmos.color = CinemachineDeoccluderPrefs.CameraSphereColor.Value;
                    if (collider.AvoidObstacles.CameraRadius > 0)
                        Gizmos.DrawWireSphere(pos, collider.AvoidObstacles.CameraRadius);

                    Vector3 forwardFeelerVector = (vcam.State.ReferenceLookAt - pos).normalized;
                    float distance = collider.AvoidObstacles.DistanceLimit;
                    Gizmos.DrawLine(pos, pos + forwardFeelerVector * distance);

                    // Show the avoidance path, for debugging
                    s_pathsCache ??= new ();
                    collider.DebugCollisionPaths(s_pathsCache, null);
                    for (int i = 0; i < s_pathsCache.Count; ++i)
                    {
                        var path = s_pathsCache[i];
                        Gizmos.color = CinemachineDeoccluderPrefs.CameraPathColor.Value;
                        Vector3 p0 = vcam.State.ReferenceLookAt;
                        for (int j = 0; j < path.Count; ++j)
                        {
                            var p = path[j];
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
}
#endif
