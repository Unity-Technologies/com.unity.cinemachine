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
        
#if UNITY_2021_2_OR_NEWER
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
            var tpFollow = Target;
            if (!tpFollow.IsValid)
            {
                return;
            }

            if (CinemachineSceneToolUtility.IsToolActive(typeof(FollowOffsetTool)))
            {
                var followTargetRotation = tpFollow.FollowTargetRotation;
                var followUp = followTargetRotation * Vector3.up;
                tpFollow.GetRigPositions(out var followTargetPosition, out var shoulderPosition, 
                    out var armPosition);
                var targetForward = followTargetRotation * Vector3.forward;
                var heading = tpFollow.GetHeading(targetForward, tpFollow.VirtualCamera.State.ReferenceUp);
                var camDistance = tpFollow.CameraDistance;
                var camPos = armPosition - targetForward * camDistance;

                var originalColor = Handles.color;
                EditorGUI.BeginChangeCheck();
                // shoulder handle
                var soHandleMinId = GUIUtility.GetControlID(FocusType.Passive); // TODO: KGB workaround until id is exposed
                var newShoulderPosition = Handles.PositionHandle(shoulderPosition, heading);
                var soHandleMaxId = GUIUtility.GetControlID(FocusType.Passive); // TODO: KGB workaround until id is exposed
                
                Handles.color = CinemachineSettings.CinemachineCoreSettings.ActiveGizmoColour;
                // arm handle
                var aHandleId = GUIUtility.GetControlID(FocusType.Passive); 
                var newArmPosition = Handles.Slider(aHandleId, armPosition, followUp, 
                    HandleUtility.GetHandleSize(armPosition) / 10f, Handles.CubeHandleCap, 0.5f);

                // cam distance handle
                var cdHandleId = GUIUtility.GetControlID(FocusType.Passive);
                var newCamPos = Handles.Slider(cdHandleId, camPos, targetForward, 
                    HandleUtility.GetHandleSize(camPos) / 10f, Handles.CubeHandleCap, 0.5f);
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(tpFollow, "Changed 3rdPersonFollow offsets using handle in Scene View.");

                    tpFollow.ShoulderOffset += 
                        CinemachineSceneToolHelpers.PositionHandleDelta(heading, newShoulderPosition, shoulderPosition);
                    tpFollow.VerticalArmLength += 
                        CinemachineSceneToolHelpers.SliderHandleDelta(newArmPosition, armPosition, followUp);
                    tpFollow.CameraDistance -= 
                        CinemachineSceneToolHelpers.SliderHandleDelta(newCamPos, camPos, targetForward);

                    InspectorUtility.RepaintGameView();
                }

                var isDragged = HandleOnDragOrHover(soHandleMinId, soHandleMaxId, shoulderPosition, "Shoulder Offset " 
                    + tpFollow.ShoulderOffset.ToString("F1"), followTargetPosition, shoulderPosition);
                isDragged |= HandleOnDragOrHover(aHandleId, aHandleId, armPosition, "Vertical Arm Length (" 
                    + tpFollow.VerticalArmLength.ToString("F1") + ")", shoulderPosition, armPosition);
                isDragged |= HandleOnDragOrHover(cdHandleId, cdHandleId, camPos, "Camera Distance (" 
                    + camDistance.ToString("F1") + ")", armPosition, camPos);
                Handles.color = originalColor;

                if (isDragged) 
                    CinemachineBrain.SoloCamera = tpFollow.VirtualCamera;

                static bool HandleOnDragOrHover
                    (int handleMinId, int handleMaxId, Vector3 labelPos, string text, Vector3 lineStart, Vector3 lineEnd)
                {
                    bool handleIsDragged;
                    bool handleIsDraggedOrHovered;
                    if (handleMinId == handleMaxId) {
                        handleIsDragged = GUIUtility.hotControl == handleMinId; 
                        handleIsDraggedOrHovered = handleIsDragged || HandleUtility.nearestControl == handleMinId;
                    }
                    else
                    {
                        handleIsDragged = handleMinId < GUIUtility.hotControl && GUIUtility.hotControl < handleMaxId;
                        handleIsDraggedOrHovered = handleIsDragged ||
                            (handleMinId < HandleUtility.nearestControl && HandleUtility.nearestControl < handleMaxId);
                    }

                    if (handleIsDraggedOrHovered)
                        CinemachineSceneToolHelpers.DrawLabel(labelPos, text);
                    
                    Handles.color = handleIsDraggedOrHovered ? 
                        Handles.selectedColor : CinemachineSettings.CinemachineCoreSettings.ActiveGizmoColour;
                    Handles.DrawDottedLine(lineStart, lineEnd, CinemachineSceneToolHelpers.lineSpacing);
                    
                    return handleIsDragged;
                }
            }
        }
#endif
    }
}

