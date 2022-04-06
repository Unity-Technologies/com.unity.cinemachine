using UnityEngine;
using UnityEditor;
using Cinemachine.Editor;

namespace Cinemachine
{
    [CustomEditor(typeof(CinemachineFreeLookModifier))]
    [CanEditMultipleObjects]
    internal sealed class CinemachineFreeLookModifierEditor : UnityEditor.Editor
    {
        CinemachineFreeLookModifier Target => target as CinemachineFreeLookModifier;

        public override void OnInspectorGUI()
        {
            var def = Target;

            EditorGUI.BeginChangeCheck();

            EditorGUILayout.LabelField("Orbits", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(serializedObject.FindProperty(() => def.Orbits));
            EditorGUILayout.Space();
            EditorGUILayout.PropertyField(serializedObject.FindProperty(() => def.Tilt));
            EditorGUILayout.PropertyField(serializedObject.FindProperty(() => def.Noise));
            EditorGUILayout.PropertyField(serializedObject.FindProperty(() => def.Lens));

            if (EditorGUI.EndChangeCheck())
                serializedObject.ApplyModifiedProperties();
        }

        [DrawGizmo(GizmoType.Active | GizmoType.Selected, typeof(CinemachineFreeLookModifier))]
        static void DrawFreeLookGizmos(CinemachineFreeLookModifier freelook, GizmoType selectionType)
        {
            var vcam = freelook.VirtualCamera as CinemachineVirtualCamera;
            if (vcam != null && vcam.Follow != null)
            {
                var orbital = vcam.GetCinemachineComponent<CinemachineOrbitalFollow>();
                if (orbital != null)
                {
                    var prevColor = Gizmos.color;
                    Gizmos.color = CinemachineCore.Instance.IsLive(vcam)
                        ? CinemachineSettings.CinemachineCoreSettings.BoundaryObjectGizmoColour
                        : CinemachineSettings.CinemachineCoreSettings.InactiveGizmoColour;

                    var orient = orbital.GetReferenceOrientation();
                    var up = orient * Vector3.up;
                    var rotation = orbital.HorizontalAxis.Value;
                    orient = Quaternion.AngleAxis(rotation, up);
                    var pos = orbital.FollowTargetPosition;
                    var scale = orbital.RadialAxis.Value;

                    DrawCircleAtPointWithRadius(
                        pos + up * freelook.Orbits.Top.Height * scale, 
                        orient, freelook.Orbits.Top.Radius * scale);
                    DrawCircleAtPointWithRadius(
                        pos + up * freelook.Orbits.Center.Height * scale, orient, 
                        freelook.Orbits.Center.Radius * scale);
                    DrawCircleAtPointWithRadius(
                        pos + up * freelook.Orbits.Bottom.Height * scale, 
                        orient, freelook.Orbits.Bottom.Radius * scale);

                    DrawCameraPath(pos, orient, scale, freelook);

                    Gizmos.color = prevColor;
                }
            }
        }

        static void DrawCircleAtPointWithRadius(Vector3 point, Quaternion orient, float radius)
        {
            var prevMatrix = Gizmos.matrix;
            Gizmos.matrix = Matrix4x4.TRS(point, orient, radius * Vector3.one);

            const int kNumPoints = 25;
            var currPoint = Vector3.forward;
            var rot = Quaternion.AngleAxis(360f / (float)kNumPoints, Vector3.up);
            for (int i = 0; i < kNumPoints + 1; ++i)
            {
                var nextPoint = rot * currPoint;
                Gizmos.DrawLine(currPoint, nextPoint);
                currPoint = nextPoint;
            }
            Gizmos.matrix = prevMatrix;
        }
        
        static void DrawCameraPath(
            Vector3 pos, Quaternion orient, float scale, CinemachineFreeLookModifier freelook)
        {
            var prevMatrix = Gizmos.matrix;
            Gizmos.matrix = Matrix4x4.TRS(pos, orient, scale * Vector3.one);

            const int kNumSteps = Cinemachine3OrbitRig.OrbitSplineCache.kResolution / 2;
            var stepSize = 1.0f / kNumSteps;
            var lastPos = freelook.GetCameraOffsetForNormalizedAxisValue(0);
            for (int i = 1; i <= kNumSteps; ++i)
            {
                var p = freelook.GetCameraOffsetForNormalizedAxisValue(i * stepSize);
                Gizmos.DrawLine(lastPos, p);
                lastPos = p;
            }
            Gizmos.matrix = prevMatrix;
        }
    }
}
