#if !UNITY_2019_3_OR_NEWER
#define CINEMACHINE_PHYSICS
#endif

using UnityEngine;

namespace Cinemachine
{
#if CINEMACHINE_PHYSICS
    /// <summary>
    /// An add-on module for Cinemachine Virtual Camera that forces the LookAt
    /// point to the center of the screen, cancelling noise and other corrections.
    /// This is useful for third-person style aim cameras that want a dead-accurate
    /// aim at all times, even in the presence of positional or rotational noise.
    /// </summary>
    [AddComponentMenu("")] // Hide in menu
    #if UNITY_2018_3_OR_NEWER
    [ExecuteAlways]
    #else
    [ExecuteInEditMode]
    #endif
    public class Cinemachine3rdPersonAim : CinemachineExtension
    {
        /// <summary>Objects on these layers will be detected.</summary>
        [Header("Aim Target Detection")]
        [Tooltip("Objects on these layers will be detected")]
        public LayerMask AimCollisionFilter;
        [TagField]
        [Tooltip("Obstacles with this tag will be ignored.  "
            + "It is a good idea to set this field to the target's tag")]
        public string IgnoreTag = string.Empty;

        public float AimDistance;
        public RectTransform AimTargetReticle;

        private void OnValidate()
        {
            AimDistance = Mathf.Max(0, AimDistance);
        }

        private void Reset()
        {
            AimCollisionFilter = 1;
            IgnoreTag = string.Empty;
            AimDistance = 200.0f;
            AimTargetReticle = null;
        }

        public override bool OnTransitionFromCamera(
            ICinemachineCamera fromCam, Vector3 worldUp, float deltaTime) 
        { 
            CinemachineCore.CameraUpdatedEvent.RemoveListener(DrawReticle);
            CinemachineCore.CameraUpdatedEvent.AddListener(DrawReticle);
            return false; 
        }

        void DrawReticle(CinemachineBrain brain)
        {
            if (!brain.IsLive(VirtualCamera) || brain.OutputCamera == null)
                CinemachineCore.CameraUpdatedEvent.RemoveListener(DrawReticle);
            else
            {
                var player = VirtualCamera.Follow;
                if (AimTargetReticle != null && player != null)
                {
                    // Adjust for actual player aim target (may be different due to offset)
                    var playerPos = player.position;
                    var aimTarget = VirtualCamera.State.ReferenceLookAt;
                    var dir = aimTarget - playerPos;
                    if (RuntimeUtility.RaycastIgnoreTag(new Ray(playerPos, dir), 
                            out RaycastHit hitInfo, dir.magnitude, AimCollisionFilter, IgnoreTag))
                        aimTarget = hitInfo.point;
                    AimTargetReticle.position = brain.OutputCamera.WorldToScreenPoint(aimTarget);
                }
            }
        }

        Vector3 GetLookAtPoint(ref CameraState state)
        {
            var fwd = state.CorrectedOrientation * Vector3.forward;
            var camPos = state.CorrectedPosition;
            bool hasHit = RuntimeUtility.RaycastIgnoreTag(
                new Ray(camPos, fwd), 
                out RaycastHit hitInfo, AimDistance, AimCollisionFilter, IgnoreTag);
            return hasHit ? hitInfo.point : camPos + fwd * AimDistance;
        }
        
        protected override void PostPipelineStageCallback(
            CinemachineVirtualCameraBase vcam,
            CinemachineCore.Stage stage, ref CameraState state, float deltaTime)
        {
            if (stage == CinemachineCore.Stage.Body)
            {
                // Raycast to establish what we're actually aiming at
                state.ReferenceLookAt = GetLookAtPoint(ref state);
            }
            if (stage == CinemachineCore.Stage.Finalize)
            {
                var dir = state.ReferenceLookAt - state.FinalPosition;
                if (dir.sqrMagnitude > 0.01f)
                {
                    state.RawOrientation = Quaternion.LookRotation(dir, state.ReferenceUp);
                    state.OrientationCorrection = Quaternion.identity;
                }
            }
        }
    }
#endif
}
