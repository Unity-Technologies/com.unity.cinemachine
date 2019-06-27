using UnityEngine;

namespace Cinemachine
{
    public class TransformFromVcamState : MonoBehaviour
    {
        CinemachineVirtualCameraBase vcam;

        void Start()
        {
            vcam = GetComponentInParent<CinemachineVirtualCameraBase>();
        }

        void LateUpdate()
        {
            if (vcam != null)
            {
                var state = vcam.State;
                transform.SetPositionAndRotation(state.FinalPosition, state.FinalOrientation);
            }
        }
    }
}

