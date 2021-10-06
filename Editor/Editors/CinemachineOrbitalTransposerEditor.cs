using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

namespace Cinemachine.Editor
{
    [CustomEditor(typeof(CinemachineOrbitalTransposer))]
    [CanEditMultipleObjects]
    internal class CinemachineOrbitalTransposerEditor : BaseEditor<CinemachineOrbitalTransposer>
    {
        /// <summary>Get the property names to exclude in the inspector.</summary>
        /// <param name="excluded">Add the names to this list</param>
        protected override void GetExcludedPropertiesInInspector(List<string> excluded)
        {
            base.GetExcludedPropertiesInInspector(excluded);
            if (Target.m_HeadingIsSlave)
            {
                excluded.Add(FieldPath(x => x.m_BindingMode));
                excluded.Add(FieldPath(x => x.m_Heading));
                excluded.Add(FieldPath(x => x.m_XAxis));
                excluded.Add(FieldPath(x => x.m_RecenterToTargetHeading));
            }
            if (Target.m_HideOffsetInInspector)
                excluded.Add(FieldPath(x => x.m_FollowOffset));

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
                    excluded.Add(FieldPath(x => x.m_Heading));
                    excluded.Add(FieldPath(x => x.m_RecenterToTargetHeading));
                    break;
            }
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
                = (Target.m_BindingMode == CinemachineTransposer.BindingMode.SimpleFollowWithWorldUp);
            DrawRemainingPropertiesInInspector();
        }

        /// Process a position drag from the user.
        /// Called "magically" by the vcam editor, so don't change the signature.
        private void OnVcamPositionDragged(Vector3 delta)
        {
            if (Target.FollowTarget != null)
            {
                Undo.RegisterCompleteObjectUndo(Target, "Camera drag");
                Quaternion targetOrientation = Target.GetReferenceOrientation(Target.VcamState.ReferenceUp);
                targetOrientation = targetOrientation * Quaternion.Euler(0, Target.m_Heading.m_Bias, 0);
                Vector3 localOffset = Quaternion.Inverse(targetOrientation) * delta;
                localOffset.x = 0;
                Target.m_FollowOffset += localOffset;
                Target.m_FollowOffset = Target.EffectiveOffset;
            }
        }

        [DrawGizmo(GizmoType.Active | GizmoType.Selected, typeof(CinemachineOrbitalTransposer))]
        static void DrawTransposerGizmos(CinemachineOrbitalTransposer target, GizmoType selectionType)
        {
            if (target.IsValid && !target.m_HideOffsetInInspector)
            {
                Color originalGizmoColour = Gizmos.color;
                Gizmos.color = CinemachineCore.Instance.IsLive(target.VirtualCamera)
                    ? CinemachineSettings.CinemachineCoreSettings.ActiveGizmoColour
                    : CinemachineSettings.CinemachineCoreSettings.InactiveGizmoColour;

                Vector3 up = target.VirtualCamera.State.ReferenceUp;
                Vector3 pos = target.FollowTargetPosition;

                Quaternion orient = target.GetReferenceOrientation(up);
                up = orient * Vector3.up;
                DrawCircleAtPointWithRadius
                    (pos + up * target.m_FollowOffset.y, orient, target.m_FollowOffset.z);

                Gizmos.color = originalGizmoColour;
            }
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
        
        protected virtual void OnEnable()
        {
            for (int i = 0; i < targets.Length; ++i)
                (targets[i] as CinemachineOrbitalTransposer).UpdateInputAxisProvider();

#if UNITY_2021_2_OR_NEWER
            // Only register follow offset control when not part of a freelook
            if (!Target.m_HeadingIsSlave)
            {
                CinemachineSceneToolUtility.RegisterTool(typeof(FollowOffsetTool));
            }
#endif
        }

#if UNITY_2021_2_OR_NEWER
        protected virtual void OnDisable()
        {
            CinemachineSceneToolUtility.UnregisterTool(typeof(FollowOffsetTool));
        }

        protected override void DrawSceneTools()
        {
            var orbitalTransposer = Target;
            if (!orbitalTransposer.IsValid || Target.m_HideOffsetInInspector)
            {
                return;
            }
            
            if (CinemachineSceneToolUtility.IsToolActive(typeof(FollowOffsetTool)))
            {
                var brain = CinemachineCore.Instance.FindPotentialTargetBrain(orbitalTransposer.VirtualCamera);
                var up = brain != null ? brain.DefaultWorldUp : Vector3.up;
                var camPos = orbitalTransposer.GetTargetCameraPosition(up);
                var camRot = orbitalTransposer.GetReferenceOrientation(up);

                EditorGUI.BeginChangeCheck();
                var foHandleMinId = GUIUtility.GetControlID(FocusType.Passive); // TODO: KGB workaround until id is exposed
                var newPos = Handles.PositionHandle(camPos, camRot);
                var foHandleMaxId = GUIUtility.GetControlID(FocusType.Passive); // TODO: KGB workaround until id is exposed
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(orbitalTransposer, 
                        "Change Follow Offset Position using handle in Scene View.");
                    
                    orbitalTransposer.m_FollowOffset += 
                        CinemachineSceneToolHelpers.PositionHandleDelta(camRot, newPos, camPos);
                    orbitalTransposer.m_FollowOffset = orbitalTransposer.EffectiveOffset; // sanitize offset
                    
                    InspectorUtility.RepaintGameView();
                }

                var followOffsetHandleIsDragged = 
                    foHandleMinId < GUIUtility.hotControl && GUIUtility.hotControl < foHandleMaxId;
                var followOffsetHandleIsDraggedOrHovered = followOffsetHandleIsDragged || 
                    foHandleMinId < HandleUtility.nearestControl && HandleUtility.nearestControl < foHandleMaxId;
                if (followOffsetHandleIsDraggedOrHovered)
                {
                    CinemachineSceneToolHelpers.DrawLabel(camPos, 
                        "Follow offset " + orbitalTransposer.m_FollowOffset.ToString("F1"));
                }
                var originalColor = Handles.color;
                Handles.color = followOffsetHandleIsDraggedOrHovered ? 
                    Handles.selectedColor : CinemachineSettings.CinemachineCoreSettings.ActiveGizmoColour;
                Handles.DrawDottedLine(orbitalTransposer.FollowTargetPosition, camPos, 5f);
                Handles.color = originalColor;

                if (followOffsetHandleIsDragged) 
                    CinemachineBrain.SoloCamera = orbitalTransposer.VirtualCamera;
            }
        }
#endif
    }
}
