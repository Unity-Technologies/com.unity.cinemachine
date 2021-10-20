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
        
        void OnSceneGUI()
        {
            DrawSceneTools();
        }

        bool m_SoloSetByTools;
        void DrawSceneTools()
        {
            var thirdPerson = Target;
            if (thirdPerson == null || !thirdPerson.IsValid)
            {
                return;
            }

            if (CinemachineSceneToolUtility.IsToolActive(typeof(FollowOffsetTool)))
            {
                var originalColor = Handles.color;
                
                thirdPerson.GetRigPositions(out var followTargetPosition, out var shoulderPosition, 
                    out var armPosition);
                var followTargetRotation = thirdPerson.FollowTargetRotation;
                var targetForward = followTargetRotation * Vector3.forward;

                EditorGUI.BeginChangeCheck();
                // shoulder handle
                var heading = Cinemachine3rdPersonFollow.GetHeading(
                    targetForward, thirdPerson.VirtualCamera.State.ReferenceUp);
                var sHandleMinId = GUIUtility.GetControlID(FocusType.Passive); // TODO: KGB workaround until id is exposed
                var newShoulderPosition = Handles.PositionHandle(shoulderPosition, heading);
                var sHandleMaxId = GUIUtility.GetControlID(FocusType.Passive); // TODO: KGB workaround until id is exposed

                Handles.color = Handles.preselectionColor;
                // arm handle
                var followUp = followTargetRotation * Vector3.up;
                var aHandleId = GUIUtility.GetControlID(FocusType.Passive); 
                var newArmPosition = Handles.Slider(aHandleId, armPosition, followUp, 
                    HandleUtility.GetHandleSize(armPosition) / 10f, Handles.CubeHandleCap, 0.5f);

                // cam distance handle
                var camDistance = thirdPerson.CameraDistance;
                var camPos = armPosition - targetForward * camDistance;
                var cdHandleId = GUIUtility.GetControlID(FocusType.Passive);
                var newCamPos = Handles.Slider(cdHandleId, camPos, targetForward, 
                    HandleUtility.GetHandleSize(camPos) / 10f, Handles.CubeHandleCap, 0.5f);
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(thirdPerson, "Changed 3rdPersonFollow offsets using handle in Scene View.");

                    thirdPerson.ShoulderOffset += 
                        CinemachineSceneToolHelpers.PositionHandleDelta(heading, newShoulderPosition, shoulderPosition);
                    thirdPerson.VerticalArmLength += 
                        CinemachineSceneToolHelpers.SliderHandleDelta(newArmPosition, armPosition, followUp);
                    thirdPerson.CameraDistance -= 
                        CinemachineSceneToolHelpers.SliderHandleDelta(newCamPos, camPos, targetForward);

                    InspectorUtility.RepaintGameView();
                }

                var isDragged = IsHandleDragged(sHandleMinId, sHandleMaxId, shoulderPosition, "Shoulder Offset " 
                    + thirdPerson.ShoulderOffset.ToString("F1"), followTargetPosition, shoulderPosition);
                isDragged |= IsHandleDragged(aHandleId, aHandleId, armPosition, "Vertical Arm Length (" 
                    + thirdPerson.VerticalArmLength.ToString("F1") + ")", shoulderPosition, armPosition);
                isDragged |= IsHandleDragged(cdHandleId, cdHandleId, camPos, "Camera Distance (" 
                    + camDistance.ToString("F1") + ")", armPosition, camPos);

                CinemachineSceneToolHelpers.SoloOnDrag(
                    isDragged, thirdPerson.VirtualCamera, sHandleMaxId, ref m_SoloSetByTools);
                
                Handles.color = originalColor;
            }
            
            // local function that draws label and guide lines, and returns true if a handle has been dragged
            static bool IsHandleDragged
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
                Handles.DrawLine(lineStart, lineEnd, CinemachineSceneToolHelpers.lineThickness);
                    
                return handleIsDragged;
            }
        }
#endif
    }
}

