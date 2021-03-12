#if !UNITY_2019_3_OR_NEWER
#define CINEMACHINE_PHYSICS
#endif

using UnityEngine;
using Cinemachine.Utility;

namespace Cinemachine
{
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

#if CINEMACHINE_PHYSICS
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
        [Range(0, 1)]
        public float CameraRadius;
        
        /// <summary>
        /// How gradually the camera returns to its normal position after having been corrected by the built-in
        /// collision resolution system. Higher numbers will move the camera more gradually back to normal.
        /// </summary>
        [Range(0, 10)]
        [Tooltip("How gradually the camera returns to its normal position after having been corrected by the built-in " +
            "collision resolution system.  Higher numbers will move the camera more gradually back to normal.")]
        public float CollisionDamping;
#else
        // Keep here for code simplicity
        internal float CameraRadius;
        internal float CollisionDamping;
#endif

        // State info
        Vector3 m_PreviousFollowTargetPosition;
        float m_PreviousHeadingAngle;
        float m_HandCollisionCorrection;
        float m_CamPosCollisionCorrection;

        void OnValidate()
        {
            CameraSide = Mathf.Clamp(CameraSide, -1.0f, 1.0f);
            Damping.x = Mathf.Max(0, Damping.x);
            Damping.y = Mathf.Max(0, Damping.y);
            Damping.z = Mathf.Max(0, Damping.z);
            CameraRadius = Mathf.Max(0.001f, CameraRadius);
            CollisionDamping = Mathf.Max(0, CollisionDamping);
        }

        void Reset()
        {
            ShoulderOffset = new Vector3(0.5f, -0.4f, 0.0f);
            VerticalArmLength = 0.4f;
            CameraSide = 1.0f;
            CameraDistance = 2.0f;
            Damping = new Vector3(0.1f, 0.5f, 0.3f);
#if CINEMACHINE_PHYSICS
            CameraCollisionFilter = 1;
            CameraRadius = 0.2f;
            CollisionDamping = 2f;
#else
            CameraRadius = 0.02f;
            CollisionDamping = 0;
#endif
        }

#if CINEMACHINE_PHYSICS
        void OnDestroy()
        {
            RuntimeUtility.DestroyScratchCollider();
        }
#endif
        
        /// <summary>True if component is enabled and has a Follow target defined</summary>
        public override bool IsValid => enabled && FollowTarget != null;

        /// <summary>Get the Cinemachine Pipeline stage that this component implements.
        /// Always returns the Aim stage</summary>
        public override CinemachineCore.Stage Stage { get { return CinemachineCore.Stage.Body; } }

        /// <summary>
        /// Report maximum damping time needed for this component.
        /// </summary>
        /// <returns>Highest damping setting in this component</returns>
        public override float GetMaxDampTime() 
        { 
            return Mathf.Max(CollisionDamping, Mathf.Max(Damping.x, Mathf.Max(Damping.y, Damping.z))); 
        }

        /// <summary>Orients the camera to match the Follow target's orientation</summary>
        /// <param name="curState">The current camera state</param>
        /// <param name="deltaTime">Elapsed time since last frame, for damping calculations.  
        /// If negative, previous state is reset.</param>
        public override void MutateCameraState(ref CameraState curState, float deltaTime)
        {
            if (IsValid)
            {
                if (!VirtualCamera.PreviousStateIsValid)
                    deltaTime = -1;
                PositionCamera(ref curState, deltaTime);
            }
        }

        void PositionCamera(ref CameraState curState, float deltaTime)
        {
            var targetPos = FollowTargetPosition;
            if (deltaTime < 0)
            {
                // No damping - reset all state info
                m_PreviousFollowTargetPosition = targetPos;
                m_HandCollisionCorrection = m_CamPosCollisionCorrection = 0;
            }
            var prevTargetPos = m_PreviousFollowTargetPosition;

            // Compute damped target pos (compute in camera space)
            var targetRot = FollowTargetRotation;
            var dampedTargetPos = Quaternion.Inverse(targetRot) * (targetPos - prevTargetPos);
            if (deltaTime >= 0)
                dampedTargetPos = VirtualCamera.DetachedFollowTargetDamp(
                    dampedTargetPos, Damping, deltaTime);
            dampedTargetPos = prevTargetPos + targetRot * dampedTargetPos;

            // Get target rotation (worldspace)
            var fwd = Vector3.forward;
            var up = Vector3.up;
            var targetForward = targetRot * fwd;
            var angle = UnityVectorExtensions.SignedAngle(
                fwd, targetForward.ProjectOntoPlane(up), up);
            if (deltaTime < 0)
                m_PreviousHeadingAngle = angle; // no damping
            var deltaHeading = angle - m_PreviousHeadingAngle;
            m_PreviousHeadingAngle = angle;

            // Bypass user-sourced rotation
            dampedTargetPos = targetPos 
                + Quaternion.AngleAxis(deltaHeading, up) * (dampedTargetPos - targetPos);
            m_PreviousFollowTargetPosition = dampedTargetPos;

            GetRawRigPositions(out Vector3 root, out _, out Vector3 hand);

            // Check if hand is colliding with something, if yes, then move the hand 
            // closer to the player. The radius is bigger here than in step 2, to avoid problems 
            // next to walls. Where the preferred distance would be pulled completely to the 
            // player, using a bigger radius, this won't happen.
            var collidedHand = ResolveCollisions(
                root, hand, deltaTime, CameraRadius * 1.05f, ref m_HandCollisionCorrection);

            // Place the camera at the correct distance from the hand
            Vector3 camPos = hand - (targetForward * CameraDistance);
            camPos = ResolveCollisions(
                collidedHand, camPos, deltaTime, CameraRadius, ref m_CamPosCollisionCorrection);

            // Set state
            curState.RawPosition = camPos;
            curState.RawOrientation = targetRot;
            curState.ReferenceUp = up;
        }

        /// <summary>
        /// Internal use only.  Public for the inspector gizmo
        /// </summary>
        /// <param name="root">Root of the rig.</param>
        /// <param name="shoulder">Shoulder of the rig.</param>
        /// <param name="hand">Hand of the rig.</param>
        public void GetRigPositions(out Vector3 root, out Vector3 shoulder, out Vector3 hand)
        {
            GetRawRigPositions(out root, out shoulder, out hand);
            float correction = 0;
            hand = ResolveCollisions(root, hand, -1, CameraRadius * 1.05f, ref correction);
        }

        void GetRawRigPositions(out Vector3 root, out Vector3 shoulder, out Vector3 hand)
        {
            root = m_PreviousFollowTargetPosition;
            var shoulderPivotReflected = Vector3.Reflect(ShoulderOffset, Vector3.right);
            var shoulderOffset = Vector3.Lerp(shoulderPivotReflected, ShoulderOffset, CameraSide);
            shoulder = root + Quaternion.AngleAxis(m_PreviousHeadingAngle, Vector3.up) * shoulderOffset;
            hand = shoulder + FollowTargetRotation * new Vector3(0, VerticalArmLength, 0);   
        }

        Vector3 ResolveCollisions(
            Vector3 root, Vector3 tip, float deltaTime, 
            float cameraRadius, ref float collisionCorrection)
        {
#if CINEMACHINE_PHYSICS
            var dir = tip - root;
            var len = dir.magnitude;
            dir /= len;

            bool hasHit = RuntimeUtility.SphereCastIgnoreTag(
                root, cameraRadius, dir, out RaycastHit hitInfo, 
                len, CameraCollisionFilter, IgnoreTag);

            var result = tip;
            if (hasHit)
            {
                // We have a collision - store the delta for damping later
                result = hitInfo.point + hitInfo.normal * cameraRadius;
                collisionCorrection = (result - tip).magnitude;
            }
            else if (deltaTime >= 0)
            {
                // Post collision damping - ease out of last collision
                collisionCorrection -= Damper.Damp(
                    collisionCorrection, CollisionDamping, deltaTime);
                if (collisionCorrection > Epsilon)
                    result -= dir * collisionCorrection;
            }
            return result;
#else
            return tip;
#endif
        }
    }
}
