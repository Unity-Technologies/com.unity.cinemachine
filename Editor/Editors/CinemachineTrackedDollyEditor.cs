using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Cinemachine.Editor
{
    [CustomEditor(typeof(CinemachineTrackedDolly))]
    internal sealed class CinemachineTrackedDollyEditor : BaseEditor<CinemachineTrackedDolly>
    {
        protected override List<string> GetExcludedPropertiesInInspector()
        {
            List<string> excluded = base.GetExcludedPropertiesInInspector();
            switch (Target.m_CameraUp)
            {
                default:
                    break;
                case CinemachineTrackedDolly.CameraUpMode.PathNoRoll:
                case CinemachineTrackedDolly.CameraUpMode.FollowTargetNoRoll:
                    excluded.Add(FieldPath(x => x.m_RollDamping));
                    break;
                case CinemachineTrackedDolly.CameraUpMode.Default:
                    excluded.Add(FieldPath(x => x.m_PitchDamping));
                    excluded.Add(FieldPath(x => x.m_YawDamping));
                    excluded.Add(FieldPath(x => x.m_RollDamping));
                    break;
            }
            return excluded;
        }

        public override void OnInspectorGUI()
        {
            BeginInspector();
            if (Target.m_Path == null)
                EditorGUILayout.HelpBox("A Path is required", MessageType.Warning);
            if (Target.m_AutoDolly.m_Enabled && Target.FollowTarget == null)
                EditorGUILayout.HelpBox("AutoDolly requires a Follow Target", MessageType.Warning);
            DrawRemainingPropertiesInInspector();
        }

        /// Process a position drag from the user.
        /// Called "magically" by the vcam editor, so don't change the signature.
        public void OnVcamPositionDragged(Vector3 delta)
        {
            Undo.RegisterCompleteObjectUndo(Target, "Camera drag"); // GML do we need this?
            Quaternion targetOrientation = Target.m_Path.EvaluateOrientationAtUnit(
                Target.m_PathPosition, Target.m_PositionUnits);
            Vector3 localOffset = Quaternion.Inverse(targetOrientation) * delta;
            FindProperty(x => x.m_PathOffset).vector3Value += localOffset;
            serializedObject.ApplyModifiedProperties();
        }
        
        [DrawGizmo(GizmoType.Active | GizmoType.InSelectionHierarchy, typeof(CinemachineTrackedDolly))]
        private static void DrawTrackeDollyGizmos(CinemachineTrackedDolly target, GizmoType selectionType)
        {
            if (target.IsValid)
            {
                CinemachinePathBase path = target.m_Path;
                if (path != null)
                {
                    CinemachinePathEditor.DrawPathGizmo(path, path.m_Appearance.pathColor);
                    Vector3 pos = path.EvaluatePositionAtUnit(target.m_PathPosition, target.m_PositionUnits);
                    Color oldColor = Gizmos.color;
                    Gizmos.color = CinemachineCore.Instance.IsLive(target.VirtualCamera)
                        ? CinemachineSettings.CinemachineCoreSettings.ActiveGizmoColour
                        : CinemachineSettings.CinemachineCoreSettings.InactiveGizmoColour;
                    Gizmos.DrawLine(pos, target.VirtualCamera.State.RawPosition);
                    Gizmos.color = oldColor;
                }
            }
        }
    }
}
