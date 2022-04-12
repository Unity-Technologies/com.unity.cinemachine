using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

namespace Cinemachine.Editor
{
    [CustomEditor(typeof(CinemachineOrbitalFollow))]
    [CanEditMultipleObjects]
    internal class CinemachineOrbitalFollowEditor : BaseEditor<CinemachineOrbitalFollow>
    {
        /// <summary>Get the property names to exclude in the inspector.</summary>
        /// <param name="excluded">Add the names to this list</param>
        protected override void GetExcludedPropertiesInInspector(List<string> excluded)
        {
            base.GetExcludedPropertiesInInspector(excluded);
            switch (Target.BindingMode)
            {
                default:
                case CinemachineTransposer.BindingMode.LockToTarget:
                    excluded.Add(Target.RotationDampingMode == CinemachineTransposer.AngularDampingMode.Euler 
                        ? FieldPath(x => x.QuaternionDamping) 
                        : FieldPath(x => x.RotationDamping));
                    break;
                case CinemachineTransposer.BindingMode.WorldSpace:
                    excluded.Add(FieldPath(x => x.RotationDampingMode));
                    excluded.Add(FieldPath(x => x.RotationDamping));
                    excluded.Add(FieldPath(x => x.QuaternionDamping));
                    break;
                case CinemachineTransposer.BindingMode.SimpleFollowWithWorldUp:
                    excluded.Add(FieldPath(x => x.RotationDampingMode));
                    excluded.Add(FieldPath(x => x.RotationDamping));
                    excluded.Add(FieldPath(x => x.QuaternionDamping));
                    break;
            }

            excluded.Add(Target.OrbitStyle == CinemachineOrbitalFollow.OrbitMode.Sphere 
                ? FieldPath(x => x.Orbits) 
                : FieldPath(x => x.Radius));
        }

        public override void OnInspectorGUI()
        {
            BeginInspector();
            bool noFollow = false;
            bool noHandler = false;
            for (int i = 0; i < targets.Length; ++i)
            {
                noFollow |= (targets[i] as CinemachineOrbitalFollow).FollowTarget == null;
                noHandler |= !(targets[i] as CinemachineOrbitalFollow).HasInputHandler;
            }

            if (noFollow)
            {
                EditorGUILayout.Space();
                EditorGUILayout.HelpBox(
                    "Orbital Follow requires a Follow target.",
                    MessageType.Warning);
            }
            if (noHandler)
            {
                InspectorUtility.HelpBoxWithButton(
                    "Orbital Follow has no input axis controller behaviour.", MessageType.Info,
                    new GUIContent("Add Input\nController"), () =>
                    {
                        Undo.SetCurrentGroupName("Add Input Axis Controller");
                        for (int i = 0; i < targets.Length; ++i)
                        {
                            var t = targets[i] as CinemachineOrbitalFollow;
                            if (!t.HasInputHandler)
                            {
                                var controller = t.VirtualCamera.GetComponent<InputAxisController>();
                                if (controller == null)
                                    Undo.AddComponent<InputAxisController>(t.VirtualCamera.gameObject);
                                else if (!controller.enabled)
                                {
                                    Undo.RecordObject(controller, "enable controller");
                                    controller.enabled = true;
                                }
                            }
                        }
                    });
            }

            int flags = 0;
            if (Target.BindingMode == CinemachineTransposer.BindingMode.SimpleFollowWithWorldUp)
                flags |= (int)InputAxis.Flags.HideRecentering | (int)InputAxis.Flags.RangeIsDriven;
            var flagsProp = FindProperty(x => x.HorizontalAxis).FindPropertyRelative("InspectorFlags");
            if (flagsProp.intValue != flags)
            {
                flagsProp.intValue = flags;
                serializedObject.ApplyModifiedProperties();
            }
            DrawRemainingPropertiesInInspector();
        }

        [DrawGizmo(GizmoType.Active | GizmoType.Selected, typeof(CinemachineOrbitalFollow))]
        static void DrawOrbitalGizmos(CinemachineOrbitalFollow orbital, GizmoType selectionType)
        {
            var vcam = orbital.VirtualCamera;
            if (vcam != null && vcam.Follow != null)
            {
                if (orbital.OrbitStyle == CinemachineOrbitalFollow.OrbitMode.ThreeRing)
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
                        pos + up * orbital.Orbits.Top.Height * scale, 
                        orient, orbital.Orbits.Top.Radius * scale);
                    DrawCircleAtPointWithRadius(
                        pos + up * orbital.Orbits.Center.Height * scale, orient, 
                        orbital.Orbits.Center.Radius * scale);
                    DrawCircleAtPointWithRadius(
                        pos + up * orbital.Orbits.Bottom.Height * scale, 
                        orient, orbital.Orbits.Bottom.Radius * scale);

                    DrawCameraPath(pos, orient, scale, orbital);

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
            Vector3 pos, Quaternion orient, float scale, CinemachineOrbitalFollow freelook)
        {
            var prevMatrix = Gizmos.matrix;
            Gizmos.matrix = Matrix4x4.TRS(pos, orient, scale * Vector3.one);

            const float stepSize = 0.1f;
            var lastPos = freelook.GetCameraOffsetForNormalizedAxisValue(-1);
            var max = 1 + stepSize/2;
            for (float t = -1 + stepSize; t < max; t += stepSize)
            {
                var p = freelook.GetCameraOffsetForNormalizedAxisValue(t);
                Gizmos.DrawLine(lastPos, p);
                lastPos = p;
            }
            Gizmos.matrix = prevMatrix;
        }
    }
}
