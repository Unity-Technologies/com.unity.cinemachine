using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

namespace Cinemachine.Editor
{
    [CustomEditor(typeof(CinemachineTransposer))]
    [CanEditMultipleObjects]
    internal sealed class CinemachineTransposerEditor : BaseEditor<CinemachineTransposer>
    {
        /// <summary>Get the property names to exclude in the inspector.</summary>
        /// <param name="excluded">Add the names to this list</param>
        protected override void GetExcludedPropertiesInInspector(List<string> excluded)
        {
            base.GetExcludedPropertiesInInspector(excluded);

            switch (Target.m_BindingMode)
            {
                default:
                case CinemachineTransposer.BindingMode.LockToTarget:
                    if (Target.m_AngularDampingMode == CinemachineTransposer.AngularDampingMode.Euler)
                        excluded.Add(FieldPath(x => x.m_AngularDamping));
                    else
                    {
                        excluded.Add(FieldPath(x => x.m_PitchDamping));
                        excluded.Add(FieldPath(x => x.m_YawDamping));
                        excluded.Add(FieldPath(x => x.m_RollDamping));
                    }
                    break;
                case CinemachineTransposer.BindingMode.LockToTargetNoRoll:
                    excluded.Add(FieldPath(x => x.m_RollDamping));
                    excluded.Add(FieldPath(x => x.m_AngularDamping));
                    excluded.Add(FieldPath(x => x.m_AngularDampingMode));
                    break;
                case CinemachineTransposer.BindingMode.LockToTargetWithWorldUp:
                    excluded.Add(FieldPath(x => x.m_PitchDamping));
                    excluded.Add(FieldPath(x => x.m_RollDamping));
                    excluded.Add(FieldPath(x => x.m_AngularDamping));
                    excluded.Add(FieldPath(x => x.m_AngularDampingMode));
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
            if (Target.m_HideOffsetInInspector)
                excluded.Add(FieldPath(x => x.m_FollowOffset));
        }

        public override void OnInspectorGUI()
        {
            BeginInspector();
            bool needWarning = false;
            for (int i = 0; !needWarning && i < targets.Length; ++i)
                needWarning = (targets[i] as CinemachineTransposer).FollowTarget == null;
            if (needWarning)
                EditorGUILayout.HelpBox(
                    "Transposer requires a Follow Target.  Change Body to Do Nothing if you don't want a Follow target.",
                    MessageType.Warning);
            DrawRemainingPropertiesInInspector();
        }

        /// Process a position drag from the user.
        /// Called "magically" by the vcam editor, so don't change the signature.
        public void OnVcamPositionDragged(Vector3 delta)
        {
            if (Target.FollowTarget != null)
            {
                Undo.RegisterCompleteObjectUndo(Target, "Camera drag");
                Quaternion targetOrientation = Target.GetReferenceOrientation(Target.VcamState.ReferenceUp);
                Vector3 localOffset = Quaternion.Inverse(targetOrientation) * delta;
                Target.m_FollowOffset += localOffset;
                Target.m_FollowOffset = Target.EffectiveOffset;
            }
        }

        bool m_HandleIsBeingDragged;
        private void DrawHandlesForSceneTools(CinemachineTransposer transposer)
        {
            Debug.Log("TransposerEditor - DrawHandlesForSceneTools");
            if (transposer == null || !transposer.IsValid)
            {
                return;
            }

            if (CinemachineVirtualCameraToolbarUtility.FollowOffsetToolIsOn)
            {
                var up = Vector3.up;
                var brain = CinemachineCore.Instance.FindPotentialTargetBrain(transposer.VirtualCamera);
                if (brain != null)
                    up = brain.DefaultWorldUp;
                var followTargetPosition = transposer.FollowTargetPosition;
                var cameraPosition = transposer.GetTargetCameraPosition(up);

                var originalColor = Handles.color;
                var labelStyle = new GUIStyle();
                Handles.color = labelStyle.normal.textColor = m_HandleIsBeingDragged
                    ? CinemachineSettings.CinemachineCoreSettings.k_vcamActiveToolColor
                    : CinemachineSettings.CinemachineCoreSettings.k_vcamToolsColor;


                EditorGUI.BeginChangeCheck();
                var newPos = Handles.PositionHandle(cameraPosition, Quaternion.identity);
                if (EditorGUI.EndChangeCheck())
                {
                    m_HandleIsBeingDragged = true;
                    var delta = newPos - transposer.m_FollowOffset;
                    //transposer.m_FollowOffset = newPos;
                    //transposer.ProcessSceneToolEvent();
                    InspectorUtility.RepaintGameView();

                    Undo.RecordObject(transposer, "Change Follow Offset Position using handle in scene view.");
                }
                else
                {
                    m_HandleIsBeingDragged = false;
                }


                Handles.DrawDottedLine(followTargetPosition, cameraPosition, 5f);
                Handles.Label(cameraPosition, "Follow offset " + transposer.m_FollowOffset.ToString("F1"), labelStyle);

                Handles.color = originalColor;
            }
        }
    }
}
