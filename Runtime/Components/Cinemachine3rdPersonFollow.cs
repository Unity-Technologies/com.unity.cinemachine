#if !UNITY_2019_3_OR_NEWER
#define CINEMACHINE_PHYSICS
#endif

using UnityEngine;
using Cinemachine.Utility;

namespace Cinemachine
{
#if CINEMACHINE_PHYSICS
    /// <summary>
    /// Third-person follower, with complex pivoting: horizontal about the origin, 
    /// vertical about the shoulder.  
    /// </summary>
    [AddComponentMenu("")] // Don't display in add component menu
    [SaveDuringPlay]
    public class Cinemachine3rdPersonFollow : CinemachineComponentBase
    {
        /// <summary>How responsively the camera tracks the target.  Each axis (camera-local) 
        /// can have its own setting.  Value is the approximate time it takes the camera 
        /// to catch up to the target's new position.  Smaller values give a more rigid 
        /// effect, larger values give a squishier one.</summary>
        [Tooltip("How responsively the camera tracks the target.  Each axis (camera-local) "
           + "can have its own setting.  Value is the approximate time it takes the camera "
           + "to catch up to the target's new position.  Smaller values give a more "
           + "rigid effect, larger values give a squishier one")]
        public Vector3 Damping;

        /// <summary>Position of the shoulder pivot relative to the Follow target origin.  
        /// This offset is in target-local space.</summary>
        [Header("Rig")]
        [Tooltip("Position of the shoulder pivot relative to the Follow target origin.  "
            + "This offset is in target-local space")]
        public Vector3 ShoulderOffset;

        /// <summary>Vertical offset of the hand in relation to the shoulder.  
        /// Arm length will affect the follow target's screen position 
        /// when the camera rotates vertically.</summary>
        [Tooltip("Vertical offset of the hand in relation to the shoulder.  "
            + "Arm length will affect the follow target's screen position when "
            + "the camera rotates vertically")]
        public float VerticalArmLength;

        /// <summary>Specifies which shoulder (left, right, or in-between) the camera is on.</summary>
        [Tooltip("Specifies which shoulder (left, right, or in-between) the camera is on")]
        [Range(0, 1)]
        public float CameraSide;

        /// <summary>How far baehind the hand the camera will be placed.</summary>
        [Tooltip("How far baehind the hand the camera will be placed")]
        public float CameraDistance;

        /// <summary>Camera will avoid obstacles on these layers.</summary>
        [Header("Obstacles")]
        [Tooltip("Camera will avoid obstacles on these layers")]
        public LayerMask CameraCollisionFilter;

        /// <summary>
        /// Obstacles with this tag will be ignored.  It is a good idea 
        /// to set this field to the target's tag
        /// </summary>
        [TagField]
        [Tooltip("Obstacles with this tag will be ignored.  "
            + "It is a good idea to set this field to the target's tag")]
        public string IgnoreTag = string.Empty;

        /// <summary>
        /// Specifies how close the camera can get to obstacles
        /// </summary>
        [Tooltip("Specifies how close the camera can get to obstacles")]
        public float CameraRadius;

        // State info
        private Vector3 PreviousFollowTargetPosition;
        private float PreviousHeadingAngle;

        private void OnValidate()
        {
            CameraSide = Mathf.Clamp(CameraSide, -1.0f, 1.0f);
            Damping.x = Mathf.Max(0, Damping.x);
            Damping.y = Mathf.Max(0, Damping.y);
            Damping.z = Mathf.Max(0, Damping.z);
            CameraRadius = Mathf.Max(0.001f, CameraRadius);
        }

        private void Reset()
        {
            CameraCollisionFilter = 1;
            ShoulderOffset = new Vector3(0.5f, -0.4f, 0.0f);
            VerticalArmLength = 0.4f;
            CameraSide = 1.0f;
            CameraDistance = 2.0f;
            Damping = new Vector3(0.1f, 0.5f, 0.3f);
            CameraRadius = 0.2f;
        }

        /// <summary>True if component is enabled and has a Follow target defined</summary>
        public override bool IsValid { get { return enabled && FollowTarget != null; } }

        /// <summary>Get the Cinemachine Pipeline stage that this component implements.
        /// Always returns the Aim stage</summary>
        public override CinemachineCore.Stage Stage { get { return CinemachineCore.Stage.Body; } }

        /// <summary>
        /// Report maximum damping time needed for this component.
        /// </summary>
        /// <returns>Highest damping setting in this component</returns>
        public override float GetMaxDampTime() { return Mathf.Max(Damping.x, Mathf.Max(Damping.y, Damping.z)); }

        /// <summary>Orients the camera to match the Follow target's orientation</summary>
        /// <param name="curState">The current camera state</param>
        /// <param name="deltaTime">Not used.</param>
        public override void MutateCameraState(ref CameraState curState, float deltaTime)
        {
            if (!IsValid)
                return;
            if (!VirtualCamera.PreviousStateIsValid)
                deltaTime = -1;
            PositionCamera(ref curState, deltaTime);
        }

        void PositionCamera(ref CameraState curState, float deltaTime)
        {
            var targetPos = FollowTargetPosition;
            var prevTargetPos = deltaTime >= 0 ? PreviousFollowTargetPosition : targetPos;

            // Compute damped target pos (compute in camera space)
            var dampedTargetPos = Quaternion.Inverse(curState.RawOrientation) 
                * (targetPos - prevTargetPos);
            if (deltaTime >= 0)
                dampedTargetPos = VirtualCamera.DetachedFollowTargetDamp(
                    dampedTargetPos, Damping, deltaTime);
            dampedTargetPos = prevTargetPos + curState.RawOrientation * dampedTargetPos;

            // Get target rotation (worldspace)
            var fwd = Vector3.forward;
            var up = Vector3.up;
            var followTargetRotation = FollowTargetRotation;
            var followTargetForward = followTargetRotation * fwd;
            var angle = UnityVectorExtensions.SignedAngle(
                fwd, followTargetForward.ProjectOntoPlane(up), up);
            var previousHeadingAngle = deltaTime >= 0 ? PreviousHeadingAngle : angle;
            var deltaHeading = angle - previousHeadingAngle;
            PreviousHeadingAngle = angle;

            // Bypass user-sourced rotation
            dampedTargetPos = targetPos 
                + Quaternion.AngleAxis(deltaHeading, up) * (dampedTargetPos - targetPos);
            PreviousFollowTargetPosition = dampedTargetPos;

            GetRigPositions(out Vector3 root, out Vector3 shoulder, out Vector3 hand);

            // 1. Check if pivot itself is colliding with something, if yes, then move the pivot 
            // closer to the player. The radius is bigger here than in step 2, to avoid problems 
            // next to walls. Where the preferred distance would be pulled completely to the 
            // player, using a bigger radius, this won't happen.
            hand = PullTowardsStartOnCollision(in root, in hand, in CameraCollisionFilter, CameraRadius * 1.05f);

            // 2. Try to place the camera to the preferred distance
            var camPos = hand - (followTargetForward * CameraDistance);
            camPos = PullTowardsStartOnCollision(in hand, in camPos, in CameraCollisionFilter, CameraRadius);

            curState.RawPosition = camPos;
            curState.RawOrientation = FollowTargetRotation;
            curState.ReferenceUp = up;
        }

        /// <summary>
        /// Internal use only.  Public for the inspector gizmo
        /// </summary>
        /// <param name="root"></param>
        /// <param name="shoulder"></param>
        /// <param name="hand"></param>
        public void GetRigPositions(out Vector3 root, out Vector3 shoulder, out Vector3 hand)
        {
            root = PreviousFollowTargetPosition;
            var ShoulderPivotReflected = Vector3.Reflect(ShoulderOffset, Vector3.right);
            var shoulderOffset = Vector3.Lerp(ShoulderPivotReflected, ShoulderOffset, CameraSide);
            var handOffset = new Vector3(0, VerticalArmLength, 0);
            shoulder = root + Quaternion.AngleAxis(PreviousHeadingAngle, Vector3.up) * shoulderOffset;
            hand = shoulder + FollowTargetRotation * handOffset;
        }

        private Vector3 PullTowardsStartOnCollision(
            in Vector3 rayStart, in Vector3 rayEnd,
            in LayerMask filter, float radius)
        {
            var dir = rayEnd - rayStart;
            bool hasHit = RuntimeUtility.SphereCastIgnoreTag(
                rayStart, radius, dir, out RaycastHit hitInfo, dir.magnitude, filter, IgnoreTag);
            return hasHit ? hitInfo.point + hitInfo.normal * radius: rayEnd;
        }
    }
#endif
}
