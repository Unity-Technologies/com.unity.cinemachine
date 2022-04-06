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
            EditorGUILayout.PropertyField(serializedObject.FindProperty(() => Target.Orbits));
            EditorGUILayout.Space();
            EditorGUILayout.PropertyField(serializedObject.FindProperty(() => def.Tilt));
            EditorGUILayout.PropertyField(serializedObject.FindProperty(() => def.Noise));
            EditorGUILayout.PropertyField(serializedObject.FindProperty(() => def.Lens));

            if (EditorGUI.EndChangeCheck())
                serializedObject.ApplyModifiedProperties();
        }

        [DrawGizmo(GizmoType.Active | GizmoType.Selected, typeof(CinemachineFreeLookModifier))]
        private static void DrawFreeLookGizmos(CinemachineFreeLookModifier freelook, GizmoType selectionType)
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

                    CinemachineOrbitalTransposerEditor.DrawCircleAtPointWithRadius(
                        pos + up * freelook.Orbits.Top.m_Height * scale, 
                        orient, freelook.Orbits.Top.m_Radius * scale);
                    CinemachineOrbitalTransposerEditor.DrawCircleAtPointWithRadius(
                        pos + up * freelook.Orbits.Center.m_Height * scale, orient, 
                        freelook.Orbits.Center.m_Radius * scale);
                    CinemachineOrbitalTransposerEditor.DrawCircleAtPointWithRadius(
                        pos + up * freelook.Orbits.Bottom.m_Height * scale, 
                        orient, freelook.Orbits.Bottom.m_Radius * scale);

                    DrawCameraPath(pos, orient, scale, freelook);

                    Gizmos.color = prevColor;
                }
            }
        }

        private static void DrawCameraPath(
            Vector3 pos, Quaternion orient, float scale, CinemachineFreeLookModifier freelook)
        {
            var prevMatrix = Gizmos.matrix;
            Gizmos.matrix = Matrix4x4.TRS(pos, orient, scale * Vector3.one);

            const int kNumSteps = CinemachineFreeLookModifier.kPositionLookupSize / 2;
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
