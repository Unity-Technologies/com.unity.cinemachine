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

        internal override void OnSceneGUI()
        {
            Debug.Log(Target.GetType() + "Editor OnSceneGUI");
            DrawSceneTools(
                CinemachineSettings.CinemachineCoreSettings.k_vcamActiveToolColor,
                CinemachineSettings.CinemachineCoreSettings.k_vcamToolsColor);
        }

        void DrawSceneTools(Color activeColor, Color defaultColor)
        {
            var T = Target;
            if (!T.IsValid)
            {
                return;
            }
        
            if (Utility.CinemachineSceneToolUtility.FollowOffsetToolIsOn)
            {
                var up = Vector3.up;
                var brain = CinemachineCore.Instance.FindPotentialTargetBrain(T.VirtualCamera);
                if (brain != null)
                    up = brain.DefaultWorldUp;
                var followTargetPosition = T.FollowTargetPosition;
                var targetForward = T.FollowTargetRotation * Vector3.forward;
                var shoulderOffset = T.ShoulderOffset;
                var shoulderOffsetPosition = followTargetPosition + shoulderOffset;
                var verticalArmLength = T.VerticalArmLength;
                var verticalArmLengthPosition = shoulderOffsetPosition + up * verticalArmLength;
                var cameraDistance = T.CameraDistance;
                var cameraPosition = verticalArmLengthPosition - targetForward * cameraDistance;
        
                var originalColor = Handles.color;
                
                EditorGUI.BeginChangeCheck();
                var newShoulderOffsetPosition = 
                    Handles.PositionHandle(shoulderOffsetPosition, Quaternion.identity);
                
                var handleIsUsed = GUIUtility.hotControl > 0;
                Handles.color = handleIsUsed ? activeColor : Color.cyan;
                var newVerticalArmLengthPosition = Handles.Slider(verticalArmLengthPosition, up);
                Handles.color = handleIsUsed ? activeColor : Color.blue;
                var newCameraPosition = Handles.Slider(cameraPosition, targetForward);

                if (EditorGUI.EndChangeCheck())
                {
                    T.ShoulderOffset += newShoulderOffsetPosition - shoulderOffsetPosition;
                    T.VerticalArmLength += (newVerticalArmLengthPosition - verticalArmLengthPosition).y;
                    
                    var projection = Vector3.Project(newCameraPosition - cameraPosition, targetForward);
                    var isNegative = Mathf.Abs(Vector3.Dot(projection, targetForward) - projection.magnitude * targetForward.magnitude) < 0.1f;
                    T.CameraDistance += (isNegative ? -1f : 1f) * projection.magnitude;
                    
                    Undo.RecordObject(this, "Changed 3rdPersonFollow offsets using handle in Scene View.");
                    InspectorUtility.RepaintGameView();
                }
                
                var labelStyle = new GUIStyle();
                Handles.color = 
                    labelStyle.normal.textColor = handleIsUsed ? activeColor : defaultColor;
                Handles.DrawDottedLine(followTargetPosition, shoulderOffsetPosition, 5f);
                Handles.DrawDottedLine(shoulderOffsetPosition, verticalArmLengthPosition, 5f);
                Handles.DrawDottedLine(verticalArmLengthPosition, cameraPosition, 5f); 
                Handles.Label(shoulderOffsetPosition, "Should Offset " + shoulderOffset.ToString("F1"), labelStyle);
                Handles.Label(verticalArmLengthPosition, "Vertical Arm Length (" + verticalArmLength.ToString("F1") + ")", labelStyle);
                Handles.Label(cameraPosition, "Camera Distance (" + cameraDistance.ToString("F1") + ")", labelStyle);
        
                Handles.color = originalColor;
            }
        }
    }
}

