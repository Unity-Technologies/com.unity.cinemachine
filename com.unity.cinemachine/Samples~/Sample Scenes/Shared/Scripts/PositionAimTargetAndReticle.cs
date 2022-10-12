using Cinemachine;
using UnityEngine;

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
        if (brain == null || brain.OutputCamera == null || ReticleCanvas == null)
            CinemachineCore.CameraUpdatedEvent.RemoveListener(SetAimTarget);
        else
        {
            var liveCam = brain.ActiveVirtualCamera as CmCamera;
            if (liveCam != null)
            {
                if (liveCam.TryGetComponent<Cinemachine3rdPersonAim>(out var aim) && aim.enabled)
                {
                    // Set the worldspace aim target position so that we can know what gets hit
                    enableReticle = true;
                    transform.position = aim.AimTarget;

                    // Set the screen-space hit target indicator position
                    if (AimTargetIndicator != null)
                        AimTargetIndicator.position = brain.OutputCamera.WorldToScreenPoint(transform.position);
                }
            }
        }
        if (ReticleCanvas != null)
            ReticleCanvas.enabled = enableReticle;
    }
}
