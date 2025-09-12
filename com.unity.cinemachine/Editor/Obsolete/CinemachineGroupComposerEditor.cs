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
    }
}
#endif
