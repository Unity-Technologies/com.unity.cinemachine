using UnityEngine;

namespace Unity.Cinemachine.Samples
{
    /// <summary> 
    /// When there is an active ThirdPersonFollow camera with noise cancellation,
    /// the position of this object is the aim target for the ThirdPersonAim camera.
    /// </summary> 
    public class AimTargetManager : MonoBehaviour
    {
        [Tooltip("This canvas will be enabled when there is a 3rdPersoAim camera active")]
        public Canvas ReticleCanvas;

        [Tooltip("If non-null, this target will pe positioned on the screen over the actual aim target")]
        public RectTransform AimTargetIndicator;

        bool m_HaveAimTarget;

        // We add a CameraUpdatedEvent listener so that we are guaranteed to update after the
        // Brain has positioned the camera
        void OnEnable() => CinemachineCore.CameraUpdatedEvent.AddListener(SetAimTarget);
        void OnDisable() => CinemachineCore.CameraUpdatedEvent.RemoveListener(SetAimTarget);

        // This is called after the Brain has positioned the camera.  If the camera has a
        // ThirdPersonAim component with noise cancellation, then we set the aim target
        // position to be precisely what the camera is indicating onscreen.
        // Otherwise, we disable the reticle and the aim target indicator.
        void SetAimTarget(CinemachineBrain brain)
        {
            m_HaveAimTarget = false;
            if (brain == null || brain.OutputCamera == null)
                CinemachineCore.CameraUpdatedEvent.RemoveListener(SetAimTarget);
            else
            {
                CinemachineCamera liveCam;
                if (brain.ActiveVirtualCamera is CinemachineCameraManagerBase managerCam)
                    liveCam = managerCam.LiveChild as CinemachineCamera;
                else
                    liveCam = brain.ActiveVirtualCamera as CinemachineCamera;
            
                if (liveCam != null)
                {
                    if (liveCam.TryGetComponent<CinemachineThirdPersonAim>(out var aim) && aim.enabled)
                    {
                        // Set the worldspace aim target position so that we can know what gets hit
                        m_HaveAimTarget = aim.NoiseCancellation;
                        transform.position = aim.AimTarget;

                        // Set the screen-space hit target indicator position
                        if (AimTargetIndicator != null)
                            AimTargetIndicator.position = brain.OutputCamera.WorldToScreenPoint(transform.position);
                    }
                }
            }
            if (ReticleCanvas != null)
                ReticleCanvas.enabled = m_HaveAimTarget;
        }

        /// <summary>
        /// Called by the player's shooting object to get the aim direction override, in case
        /// there is an active ThirdPersonFollow camera with noise cancellation.
        /// </summary>
        /// <param name="firingOrigin">Where the firing will come from.</param>
        /// <param name="firingDirection">The intended firing direction.</param>
        /// <returns>The direction in which to fire</returns>
        public Vector3 GetAimDirection(Vector3 firingOrigin, Vector3 firingDirection)
        {
            if (m_HaveAimTarget)
            {
                var dir = transform.position - firingOrigin;
                var len = dir.magnitude;
                if (len > 0.0001f)
                    return dir / len;
            }
            return firingDirection;
        }
    }
}
