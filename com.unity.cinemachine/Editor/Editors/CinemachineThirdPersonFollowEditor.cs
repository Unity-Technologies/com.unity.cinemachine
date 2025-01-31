using UnityEngine;
using UnityEditor;
using UnityEditor.EditorTools;

namespace Unity.Cinemachine.Editor
{
    [CustomEditor(typeof(CinemachineThirdPersonFollow))]
    [CanEditMultipleObjects]
    class CinemachineThirdPersonFollowEditor : CinemachineComponentBaseEditor
    {
        [DrawGizmo(GizmoType.Active | GizmoType.Selected, typeof(CinemachineThirdPersonFollow))]
        static void DrawThirdPersonGizmos(CinemachineThirdPersonFollow target, GizmoType selectionType)
        {
            if (ToolManager.activeToolType == typeof(ThirdPersonFollowOffsetTool))
                return; // don't draw gizmo when using handles

            if (target.IsValid)
            {
                var isLive = CinemachineCore.IsLive(target.VirtualCamera);
                Color originalGizmoColour = Gizmos.color;
                Gizmos.color = isLive
                    ? CinemachineCorePrefs.ActiveGizmoColour.Value
                    : CinemachineCorePrefs.InactiveGizmoColour.Value;

                target.GetRigPositions(out Vector3 root, out Vector3 shoulder, out Vector3 hand);
                Gizmos.DrawLine(root, shoulder);
                Gizmos.DrawLine(shoulder, hand);
                Gizmos.DrawLine(hand, target.VirtualCamera.State.RawPosition);

                var sphereRadius = 0.1f;
                Gizmos.DrawSphere(root, sphereRadius);
                Gizmos.DrawSphere(shoulder, sphereRadius);
#if CINEMACHINE_PHYSICS
                sphereRadius = target.AvoidObstacles.Enabled ? target.AvoidObstacles.CameraRadius : sphereRadius;
#endif
                Gizmos.DrawSphere(hand, sphereRadius);
                Gizmos.DrawSphere(target.VirtualCamera.State.RawPosition, sphereRadius);

                Gizmos.color = originalGizmoColour;
            }
        }

        [EditorTool("Third Person Follow Offset Tool", typeof(CinemachineThirdPersonFollow))]
        class ThirdPersonFollowOffsetTool : EditorTool
        {
            GUIContent m_IconContent;
            public override GUIContent toolbarIcon => m_IconContent;
            void OnEnable()
            {
                m_IconContent = new GUIContent
                {
                    image = AssetDatabase.LoadAssetAtPath<Texture2D>($"{CinemachineSceneToolHelpers.IconPath}/FollowOffset.png"),
                    tooltip = "Adjust the Third Person Follow Offset",
                };
            }

            public override void OnToolGUI(EditorWindow window)
            {
                var thirdPerson = target as CinemachineThirdPersonFollow;
                if (thirdPerson == null || !thirdPerson.IsValid)
                    return;

                var originalColor = Handles.color;

                thirdPerson.GetRigPositions(out var followTargetPosition, out var shoulderPosition,
                    out var armPosition);
                var followTargetRotation = thirdPerson.FollowTargetRotation;
                var targetForward = followTargetRotation * Vector3.forward;
                var heading = CinemachineThirdPersonFollow.GetHeading(
                    followTargetRotation, thirdPerson.VirtualCamera.State.ReferenceUp);

                EditorGUI.BeginChangeCheck();

                // shoulder handle
                var sHandleIds = Handles.PositionHandleIds.@default;
                var newShoulderPosition = Handles.PositionHandle(sHandleIds, shoulderPosition, heading);

                Handles.color = Handles.preselectionColor;
                // arm handle
                var followUp = followTargetRotation * Vector3.up;
                var aHandleId = GUIUtility.GetControlID(FocusType.Passive);
                var newArmPosition = Handles.Slider(aHandleId, armPosition, followUp,
                    CinemachineSceneToolHelpers.CubeHandleCapSize(armPosition), Handles.CubeHandleCap, 0.5f);

                // cam distance handle
                var camDistance = thirdPerson.CameraDistance;
                var camPos = armPosition - targetForward * camDistance;
                var cdHandleId = GUIUtility.GetControlID(FocusType.Passive);
                var newCamPos = Handles.Slider(cdHandleId, camPos, targetForward,
                    CinemachineSceneToolHelpers.CubeHandleCapSize(camPos), Handles.CubeHandleCap, 0.5f);
                if (EditorGUI.EndChangeCheck())
                {
                    // Modify via SerializedProperty for OnValidate to get called automatically, and scene repainting too
                    var so = new SerializedObject(thirdPerson);

                    var shoulderOffset = so.FindProperty(() => thirdPerson.ShoulderOffset);
                    shoulderOffset.vector3Value += CinemachineSceneToolHelpers.PositionHandleDelta(heading, newShoulderPosition, shoulderPosition);
                    var verticalArmLength = so.FindProperty(() => thirdPerson.VerticalArmLength);
                    verticalArmLength.floatValue += CinemachineSceneToolHelpers.SliderHandleDelta(newArmPosition, armPosition, followUp);
                    var cameraDistance = so.FindProperty(() => thirdPerson.CameraDistance);
                    cameraDistance.floatValue -= CinemachineSceneToolHelpers.SliderHandleDelta(newCamPos, camPos, targetForward);

                    so.ApplyModifiedProperties();
                }

                var isDragged = IsHandleDragged(sHandleIds.x, sHandleIds.xyz, shoulderPosition, "Shoulder Offset "
                    + thirdPerson.ShoulderOffset.ToString("F1"), followTargetPosition, shoulderPosition);
                isDragged |= IsHandleDragged(aHandleId, aHandleId, armPosition, "Vertical Arm Length ("
                    + thirdPerson.VerticalArmLength.ToString("F1") + ")", shoulderPosition, armPosition);
                isDragged |= IsHandleDragged(cdHandleId, cdHandleId, camPos, "Camera Distance ("
                    + camDistance.ToString("F1") + ")", armPosition, camPos);

                CinemachineSceneToolHelpers.SoloOnDrag(isDragged, thirdPerson.VirtualCamera, sHandleIds.xyz);

                Handles.color = originalColor;

                // local function that draws label and guide lines, and returns true if a handle has been dragged
                static bool IsHandleDragged
                    (int handleMinId, int handleMaxId, Vector3 labelPos, string text, Vector3 lineStart, Vector3 lineEnd)
                {
                    var handleIsDragged = handleMinId <= GUIUtility.hotControl && GUIUtility.hotControl <= handleMaxId;
                    var handleIsDraggedOrHovered = handleIsDragged ||
                        (handleMinId <= HandleUtility.nearestControl && HandleUtility.nearestControl <= handleMaxId);

                    if (handleIsDraggedOrHovered)
                        CinemachineSceneToolHelpers.DrawLabel(labelPos, text);

                    Handles.color = handleIsDraggedOrHovered ?
                        Handles.selectedColor : CinemachineSceneToolHelpers.HelperLineDefaultColor;
                        Handles.DrawLine(lineStart, lineEnd, CinemachineSceneToolHelpers.LineThickness);

                    return handleIsDragged;
                }
            }
        }
    }
}

