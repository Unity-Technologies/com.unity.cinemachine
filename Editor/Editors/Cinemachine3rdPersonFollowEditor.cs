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
        
        protected override void DrawSceneTools(Color guideLinesColor, Color defaultColor)
        {
            var T = Target;
            if (!T.IsValid)
            {
                return;
            }
        
            if (CinemachineSceneToolUtility.IsToolActive(CinemachineSceneTool.FollowOffset))
            {
                var up = T.FollowTargetRotation * Vector3.up;
                //var followTargetPosition = T.FollowTargetPosition;
                T.GetRigPositions(out var followTargetPosition, 
                    out var shoulderOffsetPosition, out var verticalArmLengthPosition);
                var targetForward = T.FollowTargetRotation * Vector3.forward;
                var heading = T.GetHeading(targetForward, T.VirtualCamera.State.ReferenceUp);
                var cameraDistance = T.CameraDistance;
                var cameraPosition = verticalArmLengthPosition - targetForward * cameraDistance;
        
                var originalColor = Handles.color;
                
                EditorGUI.BeginChangeCheck();
                var newShoulderOffsetPosition = Handles.PositionHandle(shoulderOffsetPosition, heading);
                
                //TODO: Set up Undos for each handle separately
                Handles.color = Color.cyan;
                var newVerticalArmLengthPosition = Handles.Slider(verticalArmLengthPosition, up);
                Handles.color = Handles.zAxisColor; // TODO: KGB set this to the correct axis color, lerp inbetween?
                var newCameraPosition = Handles.Slider(cameraPosition, targetForward);

                if (EditorGUI.EndChangeCheck())
                {
                    T.ShoulderOffset += Quaternion.Inverse(heading) * (newShoulderOffsetPosition - shoulderOffsetPosition);
                    T.VerticalArmLength += (newVerticalArmLengthPosition - verticalArmLengthPosition).y;

                    var diffCameraPos = newCameraPosition - cameraPosition;
                    var sameDirection = Vector3.Dot(diffCameraPos.normalized, targetForward) > 0;
                    T.CameraDistance += (sameDirection ? -1f : 1f) * diffCameraPos.magnitude;
                    
                    Undo.RecordObject(this, "Changed 3rdPersonFollow offsets using handle in Scene View.");
                    InspectorUtility.RepaintGameView();
                }

                var handleIsUsed = GUIUtility.hotControl > 0;
                var labelStyle = new GUIStyle();
                Handles.color = labelStyle.normal.textColor = handleIsUsed ? Handles.selectedColor : guideLinesColor;
                if (handleIsUsed)
                {
                    Handles.Label(shoulderOffsetPosition, "Should Offset " + T.ShoulderOffset.ToString("F1"), labelStyle);
                    Handles.Label(verticalArmLengthPosition, "Vertical Arm Length (" + T.VerticalArmLength.ToString("F1") + ")", labelStyle);
                    Handles.Label(cameraPosition, "Camera Distance (" + cameraDistance.ToString("F1") + ")", labelStyle);
                }
                Handles.DrawDottedLine(followTargetPosition, shoulderOffsetPosition, 5f);
                Handles.DrawDottedLine(shoulderOffsetPosition, verticalArmLengthPosition, 5f);
                Handles.DrawDottedLine(verticalArmLengthPosition, cameraPosition, 5f); 
                
                Handles.color = originalColor;
            }
        }
    }
}

