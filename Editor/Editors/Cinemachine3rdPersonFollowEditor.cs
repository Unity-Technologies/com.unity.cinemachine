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

        bool m_SoloSetByMe;
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
                tpFollow.GetRigPositions(out var followTargetPosition, 
                    out var shoulderPosition, out var armPosition);
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

                // arm handle
                Handles.color = Color.cyan;
                var vaHandleId = GUIUtility.GetControlID(FocusType.Passive); 
                var newArmPosition = Handles.Slider(vaHandleId, armPosition, followUp, 
                    HandleUtility.GetHandleSize(armPosition), Handles.ArrowHandleCap, -1);

                // cam distance handle
                Handles.color = Color.magenta;
                var cdHandleId = GUIUtility.GetControlID(FocusType.Passive);
                var newCamPos = Handles.Slider(cdHandleId, camPos, targetForward, 
                    HandleUtility.GetHandleSize(camPos), Handles.ArrowHandleCap, -1);
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(tpFollow, "Changed 3rdPersonFollow offsets using handle in Scene View.");

                    tpFollow.ShoulderOffset += 
                        CinemachineSceneToolUtility.PositionHandleDelta(heading, newShoulderPosition, shoulderPosition);
                    tpFollow.VerticalArmLength += 
                        CinemachineSceneToolUtility.SliderHandleDelta(newArmPosition, armPosition, followUp);
                    tpFollow.CameraDistance -= 
                        CinemachineSceneToolUtility.SliderHandleDelta(newCamPos, camPos, targetForward);

                    InspectorUtility.RepaintGameView();
                }

                var shoulderHandleIsDragged = 
                    soHandleMinId < GUIUtility.hotControl && GUIUtility.hotControl < soHandleMaxId;
                var shoulderHandleIsDraggedOrHovered = shoulderHandleIsDragged ||
                    (soHandleMinId < HandleUtility.nearestControl && HandleUtility.nearestControl < soHandleMaxId);
                if (shoulderHandleIsDraggedOrHovered)
                {
                    CinemachineSceneToolUtility.DrawLabel(shoulderPosition, 
                        "Shoulder Offset " + tpFollow.ShoulderOffset.ToString("F1"));
                }
                Handles.color = shoulderHandleIsDraggedOrHovered ? 
                    Handles.selectedColor : CinemachineSettings.CinemachineCoreSettings.ActiveGizmoColour;
                Handles.DrawDottedLine(followTargetPosition, shoulderPosition, 5f);

                var armHandleIsDragged = GUIUtility.hotControl == vaHandleId;
                var armHandleIsDraggedOrHovered = armHandleIsDragged || HandleUtility.nearestControl == vaHandleId;
                if (armHandleIsDraggedOrHovered)
                {
                    CinemachineSceneToolUtility.DrawLabel(armPosition, 
                        "Vertical Arm Length (" + tpFollow.VerticalArmLength.ToString("F1") + ")");
                }
                Handles.color = armHandleIsDraggedOrHovered ? 
                    Handles.selectedColor : CinemachineSettings.CinemachineCoreSettings.ActiveGizmoColour;
                Handles.DrawDottedLine(shoulderPosition, armPosition, 5f);

                var camDistanceHandleIsDragged = GUIUtility.hotControl == cdHandleId;
                var camDistanceHandleIsDraggedOrHovered = camDistanceHandleIsDragged || 
                    HandleUtility.nearestControl == cdHandleId;
                if (camDistanceHandleIsDraggedOrHovered)
                {
                    CinemachineSceneToolUtility.DrawLabel(camPos, 
                        "Camera Distance (" + camDistance.ToString("F1") + ")");
                }
                Handles.color = camDistanceHandleIsDraggedOrHovered ? 
                    Handles.selectedColor : CinemachineSettings.CinemachineCoreSettings.ActiveGizmoColour;
                Handles.DrawDottedLine(armPosition, camPos, 5f);
                
                Handles.color = originalColor;
                
                CinemachineSceneToolUtility.SoloVcamOnConditions(tpFollow.VirtualCamera, ref m_SoloSetByMe,
                    shoulderHandleIsDragged || armHandleIsDragged || camDistanceHandleIsDragged, soHandleMaxId != -1);
            }
        }
#endif
    }
}

