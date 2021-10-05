using Cinemachine.Utility;
using UnityEngine;
using UnityEditor;
using UnityEngine.UI;

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

        bool m_SoloSetByTools;
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
                    out var shoulderPosition, out var verticalArmPosition);
                var targetForward = followTargetRotation * Vector3.forward;
                var heading =
                    tpFollow.GetHeading(targetForward, tpFollow.VirtualCamera.State.ReferenceUp);
                var cameraDistance = tpFollow.CameraDistance;
                var cameraPosition = verticalArmPosition - targetForward * cameraDistance;

                var originalColor = Handles.color;
                EditorGUI.BeginChangeCheck();

                // shoulder offset handle
                var soHandleMinId = GUIUtility.GetControlID(FocusType.Passive); // TODO: KGB workaround until id is exposed
                var newShoulderPosition = Handles.PositionHandle(shoulderPosition, heading);
                var soHandleMaxId = GUIUtility.GetControlID(FocusType.Passive); // TODO: KGB workaround until id is exposed

                // vertical arm length handle
                Handles.color = Color.cyan;
                var vaHandleId = GUIUtility.GetControlID(FocusType.Passive); 
                var newVerticalArmPosition = Handles.Slider(vaHandleId,
                    verticalArmPosition, followUp, HandleUtility.GetHandleSize(verticalArmPosition),
                    Handles.ArrowHandleCap, -1);

                // camera distance handle
                Handles.color = Color.magenta;
                var cdHandleId = GUIUtility.GetControlID(FocusType.Passive);
                var newCameraPosition = Handles.Slider(cdHandleId, cameraPosition, targetForward,
                    HandleUtility.GetHandleSize(cameraPosition), Handles.ArrowHandleCap, -1);
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(tpFollow, "Changed 3rdPersonFollow offsets using handle in Scene View.");

                    // calculate delta and discard imprecision, then update offset
                    var delta =
                        Quaternion.Inverse(heading) * (newShoulderPosition - shoulderPosition);
                    delta = new Vector3(
                        Mathf.Abs(delta.x) < UnityVectorExtensions.Epsilon ? 0 : delta.x,
                        Mathf.Abs(delta.y) < UnityVectorExtensions.Epsilon ? 0 : delta.y,
                        Mathf.Abs(delta.z) < UnityVectorExtensions.Epsilon ? 0 : delta.z);
                    tpFollow.ShoulderOffset += delta;

                    tpFollow.VerticalArmLength += 
                        CinemachineSceneToolUtility.SliderDelta(newVerticalArmPosition, verticalArmPosition, followUp);
                    tpFollow.CameraDistance -= 
                        CinemachineSceneToolUtility.SliderDelta(newCameraPosition, cameraPosition, targetForward);

                    InspectorUtility.RepaintGameView();
                }

                var shoulderOffsetHandleIsHovered = 
                    soHandleMinId < HandleUtility.nearestControl && HandleUtility.nearestControl < soHandleMaxId;
                var shoulderOffsetHandleIsDragged = 
                    soHandleMinId < GUIUtility.hotControl && GUIUtility.hotControl < soHandleMaxId;
                var shoulderOffsetHandleIsDraggedOrHovered = 
                    shoulderOffsetHandleIsHovered || shoulderOffsetHandleIsDragged;
                if (shoulderOffsetHandleIsDraggedOrHovered)
                {
                    CinemachineSceneToolUtility.DrawLabel(shoulderPosition, 
                        "Shoulder Offset " + tpFollow.ShoulderOffset.ToString("F1"));
                }
                Handles.color = shoulderOffsetHandleIsDraggedOrHovered ? 
                    Handles.selectedColor : CinemachineSettings.CinemachineCoreSettings.ActiveGizmoColour;
                Handles.DrawDottedLine(followTargetPosition, shoulderPosition, 5f);

                var verticalArmHandleIsHovered = HandleUtility.nearestControl == vaHandleId;
                var verticalArmHandleIsDragged = GUIUtility.hotControl == vaHandleId;
                var verticalArmHandleIsDraggedOrHovered = verticalArmHandleIsHovered || verticalArmHandleIsDragged;
                if (verticalArmHandleIsDraggedOrHovered)
                {
                    CinemachineSceneToolUtility.DrawLabel(verticalArmPosition, 
                        "Vertical Arm Length (" + tpFollow.VerticalArmLength.ToString("F1") + ")");
                }
                Handles.color = verticalArmHandleIsDraggedOrHovered ? 
                    Handles.selectedColor : CinemachineSettings.CinemachineCoreSettings.ActiveGizmoColour;
                Handles.DrawDottedLine(shoulderPosition, verticalArmPosition, 5f);

                var cameraDistanceHandleIsHovered = HandleUtility.nearestControl == cdHandleId;
                var cameraDistanceHandleIsDragged = GUIUtility.hotControl == cdHandleId;
                var cameraDistanceHandleIsDraggedOrHovered = 
                    cameraDistanceHandleIsHovered || cameraDistanceHandleIsDragged;
                if (cameraDistanceHandleIsHovered || cameraDistanceHandleIsDragged)
                {
                    CinemachineSceneToolUtility.DrawLabel(cameraPosition, 
                        "Camera Distance (" + cameraDistance.ToString("F1") + ")");
                }
                Handles.color = cameraDistanceHandleIsDraggedOrHovered ? 
                    Handles.selectedColor : CinemachineSettings.CinemachineCoreSettings.ActiveGizmoColour;
                Handles.DrawDottedLine(verticalArmPosition, cameraPosition, 5f);
                
                Handles.color = originalColor;
                
                CinemachineSceneToolUtility.SoloVcamOnConditions(tpFollow.VirtualCamera, ref m_SoloSetByTools,
                    shoulderOffsetHandleIsDragged || verticalArmHandleIsDragged || cameraDistanceHandleIsDragged,
                    soHandleMaxId != -1);
            }
        }
    }
}

