#if !CINEMACHINE_NO_CM2_SUPPORT
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

namespace Unity.Cinemachine.Editor
{
    [System.Obsolete]
    [CustomEditor(typeof(CinemachineOrbitalTransposer))]
    [CanEditMultipleObjects]
    class CinemachineOrbitalTransposerEditor : BaseEditor<CinemachineOrbitalTransposer>
    {
        /// <summary>Get the property names to exclude in the inspector.</summary>
        /// <param name="excluded">Add the names to this list</param>
        protected override void GetExcludedPropertiesInInspector(List<string> excluded)
        {
            base.GetExcludedPropertiesInInspector(excluded);
            if (Target.m_HeadingIsDriven)
            {
                excluded.Add(FieldPath(x => x.m_BindingMode));
                excluded.Add(FieldPath(x => x.m_Heading));
                excluded.Add(FieldPath(x => x.m_XAxis));
                excluded.Add(FieldPath(x => x.m_RecenterToTargetHeading));
            }
            if (Target.HideOffsetInInspector)
                excluded.Add(FieldPath(x => x.m_FollowOffset));

            switch (Target.m_BindingMode)
            {
                default:
                case TargetTracking.BindingMode.LockToTarget:
                    if (Target.m_AngularDampingMode == TargetTracking.AngularDampingMode.Euler)
                        excluded.Add(FieldPath(x => x.m_AngularDamping));
                    else
                    {
                        excluded.Add(FieldPath(x => x.m_PitchDamping));
                        excluded.Add(FieldPath(x => x.m_YawDamping));
                        excluded.Add(FieldPath(x => x.m_RollDamping));
                    }
                    break;
                case TargetTracking.BindingMode.LockToTargetNoRoll:
                    excluded.Add(FieldPath(x => x.m_RollDamping));
                    excluded.Add(FieldPath(x => x.m_AngularDamping));
                    excluded.Add(FieldPath(x => x.m_AngularDampingMode));
                    break;
                case TargetTracking.BindingMode.LockToTargetWithWorldUp:
                    excluded.Add(FieldPath(x => x.m_PitchDamping));
                    excluded.Add(FieldPath(x => x.m_RollDamping));
                    excluded.Add(FieldPath(x => x.m_AngularDamping));
                    excluded.Add(FieldPath(x => x.m_AngularDampingMode));
                    break;
                case TargetTracking.BindingMode.LockToTargetOnAssign:
                case TargetTracking.BindingMode.WorldSpace:
                    excluded.Add(FieldPath(x => x.m_PitchDamping));
                    excluded.Add(FieldPath(x => x.m_YawDamping));
                    excluded.Add(FieldPath(x => x.m_RollDamping));
                    excluded.Add(FieldPath(x => x.m_AngularDamping));
                    excluded.Add(FieldPath(x => x.m_AngularDampingMode));
                    break;
                case TargetTracking.BindingMode.LazyFollow:
                    excluded.Add(FieldPath(x => x.m_XDamping));
                    excluded.Add(FieldPath(x => x.m_PitchDamping));
                    excluded.Add(FieldPath(x => x.m_YawDamping));
                    excluded.Add(FieldPath(x => x.m_RollDamping));
                    excluded.Add(FieldPath(x => x.m_AngularDamping));
                    excluded.Add(FieldPath(x => x.m_AngularDampingMode));
                    excluded.Add(FieldPath(x => x.m_Heading));
                    excluded.Add(FieldPath(x => x.m_RecenterToTargetHeading));
                    break;
            }
        }

        protected virtual void OnEnable()
        {
            for (int i = 0; i < targets.Length; ++i)
                (targets[i] as CinemachineOrbitalTransposer).UpdateInputAxisProvider();
        }

        public override void OnInspectorGUI()
        {
            BeginInspector();
            bool needWarning = false;
            for (int i = 0; !needWarning && i < targets.Length; ++i)
                needWarning = (targets[i] as CinemachineOrbitalTransposer).FollowTarget == null;
            if (needWarning)
                EditorGUILayout.HelpBox(
                    "Orbital Transposer requires a Follow target.",
                    MessageType.Warning);
            Target.m_XAxis.ValueRangeLocked
                = (Target.m_BindingMode == TargetTracking.BindingMode.LazyFollow);
            DrawRemainingPropertiesInInspector();
        }

        public static void DrawCircleAtPointWithRadius(Vector3 point, Quaternion orient, float radius)
        {
            Matrix4x4 prevMatrix = Gizmos.matrix;
            Gizmos.matrix = Matrix4x4.TRS(point, orient, radius * Vector3.one);

            const int kNumPoints = 25;
            Vector3 currPoint = Vector3.forward;
            Quaternion rot = Quaternion.AngleAxis(360f / (float)kNumPoints, Vector3.up);
            for (int i = 0; i < kNumPoints + 1; ++i)
            {
                Vector3 nextPoint = rot * currPoint;
                Gizmos.DrawLine(currPoint, nextPoint);
                currPoint = nextPoint;
            }
            Gizmos.matrix = prevMatrix;
        }
    }
}
#endif
