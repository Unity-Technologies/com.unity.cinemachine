using UnityEngine;

namespace Cinemachine.Examples
{
    public class PositionAimTargetAndReticle : MonoBehaviour
    {
        [Tooltip("This canvas will be enabled when there is a 3rdPersoAim camera active")]
        public Canvas ReticleCanvas;

        [Tooltip("If non-null, this target will pe positioned on the screen over the actual aim target")]
        public RectTransform AimTargetIndicator;

        // We add a CameraUpdatedEvent listener so that we are guaranteed to update after the
        // Brain has positioned the camera
        void OnEnable()
        {
            CinemachineCore.CameraUpdatedEvent.AddListener(SetAimTarget);
        }

        void OnDisable()
        {
            CinemachineCore.CameraUpdatedEvent.RemoveListener(SetAimTarget);
        }

        void SetAimTarget(CinemachineBrain brain)
        {
            var enableReticle = false;
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
                        enableReticle = true;
                        transform.position = aim.AimTarget;

                        // Set the screen-space hit target indicator position
                        if (AimTargetIndicator != null)
                            AimTargetIndicator.position = brain.OutputCamera.WorldToScreenPoint(transform.position);
                    }
                    else
                    {
                        // Emulate unstable aim spread
                        var randomXYRotation = 
                            Quaternion.Euler(10f * Random.value, 10f * Random.value, 0);
                        var randomForward = liveCam.transform.localToWorldMatrix * (randomXYRotation * Vector3.forward);
                        transform.position = randomForward * 200f;
                    }
                }
            }
            if (ReticleCanvas != null)
                ReticleCanvas.enabled = enableReticle;
        }
    }
}
