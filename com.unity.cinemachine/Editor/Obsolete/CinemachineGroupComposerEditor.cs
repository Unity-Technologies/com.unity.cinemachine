#if !CINEMACHINE_NO_CM2_SUPPORT
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using UnityEditor;
using UnityEngine;

namespace Unity.Cinemachine.Editor
{
    [Obsolete]
    [CustomEditor(typeof(CinemachineGroupComposer))]
    class CinemachineGroupComposerEditor : CinemachineComposerEditor
    {
        // Specialization
        CinemachineGroupComposer MyTarget => target as CinemachineGroupComposer;

        protected string FieldPath<TValue>(Expression<Func<CinemachineGroupComposer, TValue>> expr)
        {
            return ReflectionHelpers.GetFieldPath(expr);
        }

        /// <summary>Get the property names to exclude in the inspector.</summary>
        /// <param name="excluded">Add the names to this list</param>
        protected override void GetExcludedPropertiesInInspector(List<string> excluded)
        {
            base.GetExcludedPropertiesInInspector(excluded);
            CinemachineBrain brain = CinemachineCore.FindPotentialTargetBrain(MyTarget.VirtualCamera);
            var ortho = brain != null && brain.OutputCamera.orthographic;
            if (ortho)
            {
                excluded.Add(FieldPath(x => x.m_AdjustmentMode));
                excluded.Add(FieldPath(x => x.m_MinimumFOV));
                excluded.Add(FieldPath(x => x.m_MaximumFOV));
                excluded.Add(FieldPath(x => x.m_MaxDollyIn));
                excluded.Add(FieldPath(x => x.m_MaxDollyOut));
                excluded.Add(FieldPath(x => x.m_MinimumDistance));
                excluded.Add(FieldPath(x => x.m_MaximumDistance));
            }
            else
            {
                excluded.Add(FieldPath(x => x.m_MinimumOrthoSize));
                excluded.Add(FieldPath(x => x.m_MaximumOrthoSize));
                switch (MyTarget.m_AdjustmentMode)
                {
                    case CinemachineGroupComposer.AdjustmentMode.DollyOnly:
                        excluded.Add(FieldPath(x => x.m_MinimumFOV));
                        excluded.Add(FieldPath(x => x.m_MaximumFOV));
                        break;
                    case CinemachineGroupComposer.AdjustmentMode.ZoomOnly:
                        excluded.Add(FieldPath(x => x.m_MaxDollyIn));
                        excluded.Add(FieldPath(x => x.m_MaxDollyOut));
                        excluded.Add(FieldPath(x => x.m_MinimumDistance));
                        excluded.Add(FieldPath(x => x.m_MaximumDistance));
                        break;
                    default:
                        break;
                }
            }
        }

        public override void OnInspectorGUI()
        {
            if (MyTarget.IsValid && (MyTarget.LookAtTargetAsGroup == null || !MyTarget.LookAtTargetAsGroup.IsValid))
                EditorGUILayout.HelpBox(
                    "The Framing settings will be ignored because the LookAt target is not a kind of ICinemachineTargetGroup",
                    MessageType.Info);

            base.OnInspectorGUI();
        }

        [DrawGizmo(GizmoType.Active | GizmoType.InSelectionHierarchy, typeof(CinemachineGroupComposer))]
        static void DrawGroupComposerGizmos(CinemachineGroupComposer target, GizmoType selectionType)
        {
            // Show the group bounding box, as viewed from the camera position
            if (target.LookAtTargetAsGroup != null && target.LookAtTargetAsGroup.IsValid)
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
#endif
