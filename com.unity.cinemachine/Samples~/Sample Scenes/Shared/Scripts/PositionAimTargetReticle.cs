using Cinemachine;
using UnityEngine;

public class PositionAimTargetReticle : MonoBehaviour
{
    [Tooltip("CmBrain that defines the live camera")]
    public CinemachineBrain CmBrain;
    
    [Tooltip("AimTarget Reticle")]
    public RectTransform Reticle;
    void LateUpdate()
    {
        if (CmBrain != null && Reticle != null)
            Reticle.position = CmBrain.OutputCamera.WorldToScreenPoint(transform.position);
    }
}
