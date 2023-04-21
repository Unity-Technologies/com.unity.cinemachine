using UnityEngine;

namespace Unity.Cinemachine.Samples
{
    public class SwapShoulders : MonoBehaviour
    {
        public GameObject CinemachineCameraGameObject;

        public void Swap()
        {
            var thirdPersonFollows =
                CinemachineCameraGameObject.GetComponentsInChildren<CinemachineThirdPersonFollow>(true);
            foreach (var tpf in thirdPersonFollows)
                tpf.CameraSide = Mathf.Abs(tpf.CameraSide - 1);
        }
    }
}
