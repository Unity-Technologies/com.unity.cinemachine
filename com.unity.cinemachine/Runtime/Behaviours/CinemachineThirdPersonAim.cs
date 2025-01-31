#if CINEMACHINE_PHYSICS
using UnityEngine;

namespace Unity.Cinemachine
{
    /// <summary>
    /// An add-on module for CinemachineCamera that forces the LookAt
    /// point to the center of the screen, based on the Follow target's orientation,
    /// cancelling noise and other corrections.
    /// This is useful for third-person style aim cameras that want a dead-accurate
    /// aim at all times, even in the presence of positional or rotational noise.
    /// </summary>
    [AddComponentMenu("Cinemachine/Procedural/Rotation Control/Cinemachine Third Person Aim")]
    [ExecuteAlways]
    [SaveDuringPlay]
    [DisallowMultipleComponent]
    [HelpURL(Documentation.BaseURL + "manual/CinemachineThirdPersonAim.html")]
    public class CinemachineThirdPersonAim : CinemachineExtension
    {
        /// <summary>Objects on these layers will be detected.</summary>
        [Header("Aim Target Detection")]
        [Tooltip("Objects on these layers will be detected")]
        public LayerMask AimCollisionFilter;

        /// <summary>Objects with this tag will be ignored.
        /// It is a good idea to set this field to the target's tag.</summary>
        [TagField]
        [Tooltip("Objects with this tag will be ignored.  "
            + "It is a good idea to set this field to the target's tag")]
        public string IgnoreTag = string.Empty;

        /// <summary>How far to project the object detection ray.</summary>
        [Tooltip("How far to project the object detection ray")]
        public float AimDistance;

        /// <summary>If set, camera noise will be adjusted to stabilize target on screen.</summary>
        [Tooltip("If set, camera noise will be adjusted to stabilize target on screen")]
        public bool NoiseCancellation = true;

        /// <summary>World space position of where the player would hit if a projectile were to
        /// be fired from the player origin.  This may be different
        /// from state.ReferenceLookAt due to camera offset from player origin.</summary>
        public Vector3 AimTarget { get; private set; }

        void OnValidate()
        {
            AimDistance = Mathf.Max(1, AimDistance);
        }

        void Reset()
        {
            AimCollisionFilter = 1;
            IgnoreTag = string.Empty;
            AimDistance = 200.0f;
            NoiseCancellation = true;
        }

        /// <summary>
        /// Sets the ReferenceLookAt to be the result of a raycast in the direction of camera forward.
        /// If an object is hit, point is placed there, else it is placed at AimDistance along the ray.
        /// </summary>
        /// <param name="vcam">The virtual camera being processed</param>
        /// <param name="stage">The current pipeline stage</param>
        /// <param name="state">The current virtual camera state</param>
        /// <param name="deltaTime">The current applicable deltaTime</param>
        protected override void PostPipelineStageCallback(
            CinemachineVirtualCameraBase vcam,
            CinemachineCore.Stage stage, ref CameraState state, float deltaTime)
        {
            switch (stage)
            {
                case CinemachineCore.Stage.Body:
                {
                    if (NoiseCancellation)
                    {
                        // Raycast to establish what we're actually aiming at
                        var player = vcam.Follow;
                        if (player != null)
                        {
                            state.ReferenceLookAt = ComputeLookAtPoint(state.GetCorrectedPosition(), player, player.forward);
                            AimTarget = ComputeAimTarget(state.ReferenceLookAt, player);
                        }
                    }
                    break;
                }
                case CinemachineCore.Stage.Finalize:
                {
                    if (NoiseCancellation)
                    {
                        // Stabilize the LookAt point in the center of the screen
                        var dir = state.ReferenceLookAt - state.GetFinalPosition();
                        if (dir.sqrMagnitude > 0.01f)
                        {
                            state.RawOrientation = Quaternion.LookRotation(dir, state.ReferenceUp);
                            state.OrientationCorrection = Quaternion.identity;
                        }
                    }
                    else
                    {
                        // Raycast to establish what we're actually aiming at.
                        // In this case we do it without cancelling the noise.
                        var player = vcam.Follow;
                        if (player != null)
                        {
                            state.ReferenceLookAt = ComputeLookAtPoint(
                                state.GetCorrectedPosition(), player, state.GetCorrectedOrientation() * Vector3.forward);
                            AimTarget = ComputeAimTarget(state.ReferenceLookAt, player);
                        }
                    }
                    break;
                }
            }
        }

        Vector3 ComputeLookAtPoint(Vector3 camPos, Transform player, Vector3 fwd)
        {
            // We don't want to hit targets behind the player
            var aimDistance = AimDistance;
            var playerOrientation = player.rotation;
            var playerPosLocal = Quaternion.Inverse(playerOrientation) * (player.position - camPos);
            if (playerPosLocal.z > 0)
            {
                camPos += fwd * playerPosLocal.z;
                aimDistance -= playerPosLocal.z;
            }

            aimDistance = Mathf.Max(1, aimDistance);
            bool hasHit = RuntimeUtility.RaycastIgnoreTag(new Ray(camPos, fwd),
                out RaycastHit hitInfo, aimDistance, AimCollisionFilter, IgnoreTag);
            return hasHit ? hitInfo.point : camPos + fwd * aimDistance;
        }

        Vector3 ComputeAimTarget(Vector3 cameraLookAt, Transform player)
        {
            // Adjust for actual player aim target (may be different due to offset)
            var playerPos = player.position;
            var dir = cameraLookAt - playerPos;
            if (RuntimeUtility.RaycastIgnoreTag(new Ray(playerPos, dir),
                out RaycastHit hitInfo, dir.magnitude, AimCollisionFilter, IgnoreTag))
                return hitInfo.point;
            return cameraLookAt;
        }
    }
}
#endif
