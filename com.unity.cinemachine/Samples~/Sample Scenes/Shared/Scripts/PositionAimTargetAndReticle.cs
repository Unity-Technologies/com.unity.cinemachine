using System;
using Cinemachine;
using UnityEngine;

public class PositionAimTargetAndReticle : MonoBehaviour
{
    [Tooltip("AimTarget Reticle")]
    public RectTransform Reticle;

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
        if (brain.OutputCamera == null)
            CinemachineCore.CameraUpdatedEvent.RemoveListener(SetAimTarget);

        var liveCam = brain.ActiveVirtualCamera as CinemachineVirtualCameraBase;
        if (liveCam != null)
        {
            if (liveCam.TryGetComponent<Cinemachine3rdPersonAim>(out var aim) && aim.enabled)
            {
                transform.position = aim.AimTarget;
            }
            else
            {
                var lookAt = liveCam.ResolveLookAt(liveCam.LookAt);
                if (lookAt != null)
                    transform.position = lookAt.position;
            }

            if (brain != null && Reticle != null)
                Reticle.position = brain.OutputCamera.WorldToScreenPoint(transform.position);
        }
    }
}
