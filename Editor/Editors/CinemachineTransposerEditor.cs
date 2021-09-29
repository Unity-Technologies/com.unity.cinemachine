using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using Cinemachine.Utility;

namespace Cinemachine.Editor
{
    [CustomEditor(typeof(CinemachineTransposer))]
    [CanEditMultipleObjects]
    internal class CinemachineTransposerEditor : BaseEditor<CinemachineTransposer>
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

        protected virtual void OnEnable()
        {
            CinemachineSceneToolUtility.RegisterTool(typeof(FollowOffsetTool));
        }

        protected virtual void OnDisable()
        {
            CinemachineSceneToolUtility.UnregisterTool(typeof(FollowOffsetTool));
        }
        
        protected override void DrawSceneTools()
        {
            var transposer = Target;
            if (!transposer.IsValid && Tools.current != Tool.Move)
            {
                return;
            }

            if (CinemachineSceneToolUtility.IsToolActive(typeof(FollowOffsetTool)))
            {
                var brain = CinemachineCore.Instance.FindPotentialTargetBrain(transposer.VirtualCamera);
                var up = brain != null ? brain.DefaultWorldUp : Vector3.up;
                var cameraPosition = transposer.GetTargetCameraPosition(up);
                var cameraRotation = transposer.GetReferenceOrientation(up);

                EditorGUI.BeginChangeCheck();
                var newPos = Handles.PositionHandle(cameraPosition, cameraRotation);
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(transposer, "Change Follow Offset Position using handle in Scene View.");
                    
                    // calculate delta and discard imprecision, then update offset
                    var delta = Quaternion.Inverse(cameraRotation) * (newPos - cameraPosition);
                    delta = new Vector3(
                        Mathf.Abs(delta.x) < UnityVectorExtensions.Epsilon ? 0 : delta.x,
                        Mathf.Abs(delta.y) < UnityVectorExtensions.Epsilon ? 0 : delta.y,
                        Mathf.Abs(delta.z) < UnityVectorExtensions.Epsilon ? 0 : delta.z);
                    transposer.m_FollowOffset += delta;
                    transposer.m_FollowOffset = transposer.EffectiveOffset; // sanitize offset
                    
                    InspectorUtility.RepaintGameView();
                }

                var handleIsUsed = GUIUtility.hotControl > 0;
                if (handleIsUsed)
                {
                    var labelStyle = new GUIStyle { normal = { textColor = Handles.selectedColor } };
                    Handles.Label(cameraPosition, "Follow offset " + 
                        transposer.m_FollowOffset.ToString("F1"), labelStyle);
                }
                var originalColor = Handles.color;
                Handles.color = handleIsUsed ? 
                    Handles.selectedColor : CinemachineSettings.CinemachineCoreSettings.ActiveGizmoColour;
                Handles.DrawDottedLine(transposer.FollowTargetPosition, cameraPosition, 5f);
                Handles.color = originalColor;
            }
        }
    }
}
