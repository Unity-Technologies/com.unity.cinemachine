#if CINEMACHINE_TIMELINE

using UnityEngine.Playables;

namespace Cinemachine
{
    internal sealed class CinemachineShotPlayable : PlayableBehaviour
    {
        public CinemachineVirtualCameraBase VirtualCamera;
        public bool IsValid => VirtualCamera != null;
    }
}
#endif
