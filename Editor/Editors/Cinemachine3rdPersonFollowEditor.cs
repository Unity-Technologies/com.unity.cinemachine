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
                // shoulder offset handle
                var soHandleMinId = GUIUtility.GetControlID(FocusType.Passive);
                var newShoulderOffsetPosition = Handles.PositionHandle(shoulderOffsetPosition, heading);
                var soHandleMaxId = GUIUtility.GetControlID(FocusType.Passive);
                
                // vertical arm length handle
                Handles.color = Color.cyan;
                var vaHandleId = GUIUtility.GetControlID(FocusType.Passive);
                var newVerticalArmLengthPosition = Handles.Slider(vaHandleId, 
                    verticalArmLengthPosition, followUp, HandleUtility.GetHandleSize(verticalArmLengthPosition), 
                    Handles.ArrowHandleCap, -1);
                
                // camera distance handle
                Handles.color = Color.magenta;
                var cdHandleId = GUIUtility.GetControlID(FocusType.Passive);
                var newCameraPosition = Handles.Slider(cdHandleId, cameraPosition, targetForward,
                    HandleUtility.GetHandleSize(cameraPosition), Handles.ArrowHandleCap, -1);
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

                var shoulderOffsetHandleIsUsedOrHovered = 
                    soHandleMinId < GUIUtility.hotControl && GUIUtility.hotControl < soHandleMaxId || 
                    soHandleMinId < HandleUtility.nearestControl && HandleUtility.nearestControl < soHandleMaxId;
                if (shoulderOffsetHandleIsUsedOrHovered)
                {
                    var labelStyle = new GUIStyle { normal = { textColor = Handles.selectedColor } };
                    Handles.Label(shoulderOffsetPosition, "Shoulder Offset " + 
                        thirdPersonFollow.ShoulderOffset.ToString("F1"), labelStyle);
                }
                
                var verticalArmHandleIsUsedOrHovered = 
                    GUIUtility.hotControl == vaHandleId || HandleUtility.nearestControl == vaHandleId;
                if (verticalArmHandleIsUsedOrHovered)
                {
                    var labelStyle = new GUIStyle { normal = { textColor = Handles.selectedColor } };
                    Handles.Label(verticalArmLengthPosition, "Vertical Arm Length (" + 
                        thirdPersonFollow.VerticalArmLength.ToString("F1") + ")", labelStyle);
                }
                
                var cameraDistanceHandleIsUsedOrHovered = 
                    GUIUtility.hotControl == cdHandleId || HandleUtility.nearestControl == cdHandleId;
                if (cameraDistanceHandleIsUsedOrHovered)
                {
                    var labelStyle = new GUIStyle { normal = { textColor = Handles.selectedColor } };
                    Handles.Label(cameraPosition, "Camera Distance (" + 
                        cameraDistance.ToString("F1") + ")", labelStyle);
                }

                Handles.color = shoulderOffsetHandleIsUsedOrHovered ? 
                    Handles.selectedColor : CinemachineSettings.CinemachineCoreSettings.ActiveGizmoColour;
                Handles.DrawDottedLine(followTargetPosition, shoulderOffsetPosition, 5f);
                
                Handles.color = verticalArmHandleIsUsedOrHovered ? 
                    Handles.selectedColor : CinemachineSettings.CinemachineCoreSettings.ActiveGizmoColour;
                Handles.DrawDottedLine(shoulderOffsetPosition, verticalArmLengthPosition, 5f);
                
                Handles.color = cameraDistanceHandleIsUsedOrHovered ? 
                    Handles.selectedColor : CinemachineSettings.CinemachineCoreSettings.ActiveGizmoColour;
                Handles.DrawDottedLine(verticalArmLengthPosition, cameraPosition, 5f);
                
                Handles.color = originalColor;
            }
        }
    }
}

