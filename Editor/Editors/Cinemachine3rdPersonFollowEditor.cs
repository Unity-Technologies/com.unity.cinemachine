using Cinemachine.Utility;
using UnityEngine;
using UnityEditor;

namespace Cinemachine.Editor
{
    [CustomEditor(typeof(Cinemachine3rdPersonFollow))]
    [CanEditMultipleObjects]
    internal class Cinemachine3rdPersonFollowEditor : BaseEditor<Cinemachine3rdPersonFollow>
    {
        [DrawGizmo(GizmoType.Active | GizmoType.Selected, typeof(Cinemachine3rdPersonFollow))]
        static void Draw3rdPersonGizmos(Cinemachine3rdPersonFollow target, GizmoType selectionType)
        {
            if (target.IsValid)
            {
                var isLive = CinemachineCore.Instance.IsLive(target.VirtualCamera);
                Color originalGizmoColour = Gizmos.color;
                Gizmos.color = isLive
                    ? CinemachineSettings.CinemachineCoreSettings.ActiveGizmoColour
                    : CinemachineSettings.CinemachineCoreSettings.InactiveGizmoColour;

                target.GetRigPositions(out Vector3 root, out Vector3 shoulder, out Vector3 hand);
                Gizmos.DrawLine(root, shoulder);
                Gizmos.DrawLine(shoulder, hand);
                Gizmos.DrawSphere(root, 0.02f);
                Gizmos.DrawSphere(shoulder, 0.02f);
                Gizmos.DrawSphere(hand, target.CameraRadius);

                if (isLive)
                    Gizmos.color = CinemachineSettings.CinemachineCoreSettings.BoundaryObjectGizmoColour;
                Gizmos.DrawSphere(target.VirtualCamera.State.RawPosition, target.CameraRadius);

                Gizmos.color = originalGizmoColour;
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
            var thirdPersonFollow = Target;
            if (!thirdPersonFollow.IsValid)
            {
                return;
            }
        
            if (CinemachineSceneToolUtility.IsToolActive(typeof(FollowOffsetTool)))
            {
                var followTargetRotation= thirdPersonFollow.FollowTargetRotation;
                var followUp = followTargetRotation * Vector3.up;
                thirdPersonFollow.GetRigPositions(out var followTargetPosition, 
                    out var shoulderOffsetPosition, out var verticalArmLengthPosition);
                var targetForward = followTargetRotation * Vector3.forward;
                var heading = 
                    thirdPersonFollow.GetHeading(targetForward, thirdPersonFollow.VirtualCamera.State.ReferenceUp);
                var cameraDistance = thirdPersonFollow.CameraDistance;
                var cameraPosition = verticalArmLengthPosition - targetForward * cameraDistance;

                var originalColor = Handles.color;
                EditorGUI.BeginChangeCheck();
                var shoulderOffsetHandleMinId = GUIUtility.GetControlID(FocusType.Passive);
                var newShoulderOffsetPosition = Handles.PositionHandle(shoulderOffsetPosition, heading);
                var shoulderOffsetHandleMaxId = GUIUtility.GetControlID(FocusType.Passive);
                Handles.color = Color.cyan;
                var verticalArmHandleId = GUIUtility.GetControlID(FocusType.Passive);
                var newVerticalArmLengthPosition = Handles.Slider(verticalArmHandleId, 
                    verticalArmLengthPosition, followUp, HandleUtility.GetHandleSize(verticalArmLengthPosition), 
                    Handles.ArrowHandleCap, -1);
                Handles.color = Color.magenta;
                var cameraDistanceHandleId = GUIUtility.GetControlID(FocusType.Passive);
                var newCameraPosition = Handles.Slider(cameraDistanceHandleId, cameraPosition, targetForward,
                    HandleUtility.GetHandleSize(cameraPosition), 
                    Handles.ArrowHandleCap, -1);
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(thirdPersonFollow, "Changed 3rdPersonFollow offsets using handle in Scene View.");

                    // calculate delta and discard imprecision, then update offset
                    var delta =
                        Quaternion.Inverse(heading) * (newShoulderOffsetPosition - shoulderOffsetPosition);
                    delta = new Vector3(
                        Mathf.Abs(delta.x) < UnityVectorExtensions.Epsilon ? 0 : delta.x,
                        Mathf.Abs(delta.y) < UnityVectorExtensions.Epsilon ? 0 : delta.y,
                        Mathf.Abs(delta.z) < UnityVectorExtensions.Epsilon ? 0 : delta.z);
                    thirdPersonFollow.ShoulderOffset += delta;

                    var diffPos = newVerticalArmLengthPosition - verticalArmLengthPosition;
                    var sameDirection = Vector3.Dot(diffPos.normalized, followUp) > 0;
                    thirdPersonFollow.VerticalArmLength += (sameDirection ? 1f : -1f) * diffPos.magnitude;
                    
                    diffPos = newCameraPosition - cameraPosition;
                    sameDirection = Vector3.Dot(diffPos.normalized, targetForward) > 0;
                    thirdPersonFollow.CameraDistance -= (sameDirection ? 1f : -1f) * diffPos.magnitude;

                    InspectorUtility.RepaintGameView();
                }

                var shoulderOffsetHandleIsUsed = shoulderOffsetHandleMinId < GUIUtility.hotControl 
                    && GUIUtility.hotControl < shoulderOffsetHandleMaxId;
                var verticalArmHandleIsUsed = GUIUtility.hotControl == verticalArmHandleId;
                var cameraDistanceHandleIsUsed = GUIUtility.hotControl == cameraDistanceHandleId;
                if (shoulderOffsetHandleIsUsed)
                {
                    var labelStyle = new GUIStyle { normal = { textColor = Handles.selectedColor } };
                    Handles.Label(shoulderOffsetPosition, "Shoulder Offset " + 
                        thirdPersonFollow.ShoulderOffset.ToString("F1"), labelStyle);
                }
                else if (verticalArmHandleIsUsed)
                {
                    var labelStyle = new GUIStyle { normal = { textColor = Handles.selectedColor } };
                    Handles.Label(verticalArmLengthPosition, "Vertical Arm Length (" + 
                        thirdPersonFollow.VerticalArmLength.ToString("F1") + ")", labelStyle);
                }
                else if (cameraDistanceHandleIsUsed)
                {
                    var labelStyle = new GUIStyle { normal = { textColor = Handles.selectedColor } };
                    Handles.Label(cameraPosition, "Camera Distance (" + 
                        cameraDistance.ToString("F1") + ")", labelStyle);
                }

                Handles.color = shoulderOffsetHandleIsUsed ? 
                    Handles.selectedColor : CinemachineSettings.CinemachineCoreSettings.ActiveGizmoColour;
                Handles.DrawDottedLine(followTargetPosition, shoulderOffsetPosition, 5f);
                
                Handles.color = verticalArmHandleIsUsed ? 
                    Handles.selectedColor : CinemachineSettings.CinemachineCoreSettings.ActiveGizmoColour;
                Handles.DrawDottedLine(shoulderOffsetPosition, verticalArmLengthPosition, 5f);
                
                Handles.color = cameraDistanceHandleIsUsed ? 
                    Handles.selectedColor : CinemachineSettings.CinemachineCoreSettings.ActiveGizmoColour;
                Handles.DrawDottedLine(verticalArmLengthPosition, cameraPosition, 5f);
                
                Handles.color = originalColor;
            }
        }
    }
}

