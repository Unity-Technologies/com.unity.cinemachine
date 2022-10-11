using Cinemachine;
using UnityEngine;

public class SetAimTarget : MonoBehaviour
{
    [Tooltip("A transform to hold the position the camera is aiming at in world space")]
    public Transform AimTarget;
    
    [Tooltip("Cinemachine camera that defines the aim target")]
    public CinemachineVirtualCameraBase CmCamera;

    void LateUpdate()
    {
        if (AimTarget != null && CmCamera != null && CinemachineCore.Instance.IsLive(CmCamera))
        {
            if (CmCamera.TryGetComponent<Cinemachine3rdPersonAim>(out var aim) && aim.enabled)
            {
                AimTarget.position = aim.AimTarget;
            }
            else
            {
                var lookAt = CmCamera.ResolveLookAt(CmCamera.LookAt);
                if (lookAt != null)
                    AimTarget.position = lookAt.position;
            }
        }
    }
}
