using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

namespace Cinemachine.Editor
{
    [CustomEditor(typeof(CinemachineTransposer))]
    internal sealed class CinemachineTransposerEditor : BaseEditor<CinemachineTransposer>
    {
        protected override List<string> GetExcludedPropertiesInInspector()
        {
            List<string> excluded = base.GetExcludedPropertiesInInspector();

            if (Target.m_AngularDampingMode == CinemachineTransposer.AngularDampingMode.Euler)
                excluded.Add(FieldPath(x => x.m_AngularDamping));
            else
            {
                excluded.Add(FieldPath(x => x.m_PitchDamping));
                excluded.Add(FieldPath(x => x.m_YawDamping));
                excluded.Add(FieldPath(x => x.m_RollDamping));
            }

            switch (Target.m_BindingMode)
            {
                default:
                case CinemachineTransposer.BindingMode.LockToTarget:
                    break;
                case CinemachineTransposer.BindingMode.LockToTargetNoRoll:
                    excluded.Add(FieldPath(x => x.m_RollDamping));
                    break;
                case CinemachineTransposer.BindingMode.LockToTargetWithWorldUp:
                    excluded.Add(FieldPath(x => x.m_PitchDamping));
                    excluded.Add(FieldPath(x => x.m_RollDamping));
                    break;
                case CinemachineTransposer.BindingMode.LockToTargetOnAssign:
                case CinemachineTransposer.BindingMode.WorldSpace:
                    excluded.Add(FieldPath(x => x.m_PitchDamping));
                    excluded.Add(FieldPath(x => x.m_YawDamping));
                    excluded.Add(FieldPath(x => x.m_RollDamping));
                    excluded.Add(FieldPath(x => x.m_AngularDamping));
                    excluded.Add(FieldPath(x => x.m_AngularDampingMode));
                    break;
                case CinemachineTransposer.BindingMode.SimpleFollowWithWorldUp:
                    excluded.Add(FieldPath(x => x.m_XDamping));
                    excluded.Add(FieldPath(x => x.m_PitchDamping));
                    excluded.Add(FieldPath(x => x.m_YawDamping));
                    excluded.Add(FieldPath(x => x.m_RollDamping));
                    excluded.Add(FieldPath(x => x.m_AngularDamping));
                    excluded.Add(FieldPath(x => x.m_AngularDampingMode));
                    break;
            }
            if (Target.HideOffsetInInspector)
                excluded.Add(FieldPath(x => x.m_FollowOffset));
            return excluded;
        }

        public override void OnInspectorGUI()
        {
            BeginInspector();
            if (Target.FollowTarget == null)
                EditorGUILayout.HelpBox(
                    "Transposer requires a Follow Target.  Change Body to Do Nothing if you don't want a Follow target.",
                    MessageType.Warning);
            DrawRemainingPropertiesInInspector();
        }

        /// Process a position drag from the user.
        /// Called "magically" by the vcam editor, so don't change the signature.
        public void OnVcamPositionDragged(Vector3 delta)
        {
            Undo.RegisterCompleteObjectUndo(Target, "Camera drag"); // GML do we need this?
            Quaternion targetOrientation = Target.GetReferenceOrientation(Target.VcamState.ReferenceUp);
            Vector3 localOffset = Quaternion.Inverse(targetOrientation) * delta;
            FindProperty(x => x.m_FollowOffset).vector3Value += localOffset;
            serializedObject.ApplyModifiedProperties();
            FindProperty(x => x.m_FollowOffset).vector3Value = Target.EffectiveOffset;
            serializedObject.ApplyModifiedProperties();
        }

        [DrawGizmo(GizmoType.Active | GizmoType.Selected, typeof(CinemachineTransposer))]
        static void DrawTransposerGizmos(CinemachineTransposer target, GizmoType selectionType)
        {
            if (target.IsValid  & !target.HideOffsetInInspector)
            {
                Color originalGizmoColour = Gizmos.color;
                Gizmos.color = CinemachineCore.Instance.IsLive(target.VirtualCamera)
                    ? CinemachineSettings.CinemachineCoreSettings.ActiveGizmoColour
                    : CinemachineSettings.CinemachineCoreSettings.InactiveGizmoColour;

                Vector3 up = Vector3.up;
                CinemachineBrain brain = CinemachineCore.Instance.FindPotentialTargetBrain(target.VirtualCamera);
                if (brain != null)
                    up = brain.DefaultWorldUp;
                Vector3 targetPos = target.FollowTargetPosition;
                Vector3 desiredPos = target.GetTargetCameraPosition(up);
                Gizmos.DrawLine(targetPos, desiredPos);
                //Gizmos.DrawWireSphere(desiredPos, HandleUtility.GetHandleSize(desiredPos) / 20);
                Gizmos.color = originalGizmoColour;
            }
        }
    }
}
