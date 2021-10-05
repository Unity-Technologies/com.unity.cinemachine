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
        
#else
        // Keep here for code simplicity
        internal float CameraRadius;
#endif

        // State info
        Vector3 m_PreviousFollowTargetPosition;
        Vector3 m_DampingCorrection; // this is in local rig space

        void OnValidate()
        {
            CameraSide = Mathf.Clamp(CameraSide, -1.0f, 1.0f);
            Damping.x = Mathf.Max(0, Damping.x);
            Damping.y = Mathf.Max(0, Damping.y);
            Damping.z = Mathf.Max(0, Damping.z);
            CameraRadius = Mathf.Max(0.001f, CameraRadius);
        }

        void Reset()
        {
            ShoulderOffset = new Vector3(0.5f, -0.4f, 0.0f);
            VerticalArmLength = 0.4f;
            CameraSide = 1.0f;
            CameraDistance = 2.0f;
            Damping = new Vector3(0.1f, 0.5f, 0.3f);
#if CINEMACHINE_PHYSICS
            CameraCollisionFilter = 0;
            CameraRadius = 0.2f;
#else
            CameraRadius = 0.02f;
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
            return Mathf.Max(Damping.x, Mathf.Max(Damping.y, Damping.z)); 
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

        /// <summary>This is called to notify the us that a target got warped,
        /// so that we can update its internal state to make the camera
        /// also warp seamlessy.</summary>
        /// <param name="target">The object that was warped</param>
        /// <param name="positionDelta">The amount the target's position changed</param>
        public override void OnTargetObjectWarped(Transform target, Vector3 positionDelta)
        {
            base.OnTargetObjectWarped(target, positionDelta);
            if (target == FollowTarget)
            {
                m_PreviousFollowTargetPosition += positionDelta;
            }
        }
        
        void PositionCamera(ref CameraState curState, float deltaTime)
        {
            var up = curState.ReferenceUp;
            var targetPos = FollowTargetPosition;
            var targetRot = FollowTargetRotation;
            var targetForward = targetRot * Vector3.forward;
            var heading = GetHeading(targetForward, up);

            if (deltaTime < 0)
            {
                // No damping - reset damping state info
                m_DampingCorrection = Vector3.zero;
            }
            else
            {
                // Damping correction is applied to the shoulder offset - stretching the rig
                m_DampingCorrection += Quaternion.Inverse(heading) * (m_PreviousFollowTargetPosition - targetPos);
                m_DampingCorrection -= VirtualCamera.DetachedFollowTargetDamp(m_DampingCorrection, Damping, deltaTime);
            }

            m_PreviousFollowTargetPosition = targetPos;
            var root = targetPos;
            GetRawRigPositions(root, targetRot, heading, out _, out Vector3 hand);

            // Check if hand is colliding with something, if yes, then move the hand 
            // closer to the player. The radius is slightly enlarged, to avoid problems 
            // next to walls
            var collidedHand = ResolveCollisions(root, hand, CameraRadius * 1.05f);

            // Place the camera at the correct distance from the hand
            Vector3 camPos = hand - (targetForward * (CameraDistance - m_DampingCorrection.z));
            camPos = ResolveCollisions(collidedHand, camPos, CameraRadius);

            // Set state
            curState.RawPosition = camPos;
            curState.RawOrientation = targetRot; // not necessary, but left in to avoid breaking scenes that depend on this
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
            var up = VirtualCamera.State.ReferenceUp;
            var targetRot = FollowTargetRotation;
            var targetForward = targetRot * Vector3.forward;
            var heading = GetHeading(targetForward, up);
            root = m_PreviousFollowTargetPosition;
            GetRawRigPositions(root, targetRot, heading, out shoulder, out hand);
            hand = ResolveCollisions(root, hand, CameraRadius * 1.05f);
        }

        Quaternion GetHeading(Vector3 targetForward, Vector3 up)
        {
            var planeForward = targetForward.ProjectOntoPlane(up);
            planeForward = Vector3.Cross(up, Vector3.Cross(planeForward, up));
            return Quaternion.LookRotation(planeForward, up);
        }

        void GetRawRigPositions(
            Vector3 root, Quaternion targetRot, Quaternion heading, 
            out Vector3 shoulder, out Vector3 hand)
        {
            var shoulderPivotReflected = Vector3.Reflect(ShoulderOffset, Vector3.right);
            var shoulderOffset = Vector3.Lerp(shoulderPivotReflected, ShoulderOffset, CameraSide);
            shoulderOffset.x += m_DampingCorrection.x;
            shoulderOffset.y += m_DampingCorrection.y;
            shoulder = root + heading * shoulderOffset;
            hand = shoulder + targetRot * new Vector3(0, VerticalArmLength, 0);   
        }

        Vector3 ResolveCollisions(Vector3 root, Vector3 tip, float cameraRadius)
        {
#if CINEMACHINE_PHYSICS
            if (CameraCollisionFilter.value == 0)
            {
                return tip;
            }
            
            var dir = tip - root;
            var len = dir.magnitude;
            dir /= len;

            var result = tip;
            float desiredCorrection = 0;

            if (RuntimeUtility.SphereCastIgnoreTag(
                root, cameraRadius, dir, out RaycastHit hitInfo, 
                len, CameraCollisionFilter, IgnoreTag))
            {
                var desiredResult = hitInfo.point + hitInfo.normal * cameraRadius;
                desiredCorrection = (desiredResult - tip).magnitude;
            }

            // Apply the correction
            if (desiredCorrection > Epsilon)
                result -= dir * desiredCorrection;

            return result;
#else
            return tip;
#endif
        }
    }
}
