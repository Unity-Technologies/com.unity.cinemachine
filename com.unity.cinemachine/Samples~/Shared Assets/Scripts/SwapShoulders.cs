using UnityEngine;
#if CINEMACHINE_UNITY_INPUTSYSTEM
using UnityEngine.InputSystem;
#endif

namespace Unity.Cinemachine.Samples
{
    public class SwapShoulders : MonoBehaviour
    {
        public GameObject CinemachineCameraGameObject;
        public SimplePlayerControllerBase PlayerController;

        void Update()
        {
#if CINEMACHINE_UNITY_INPUTSYSTEM
            if (Keyboard.current[Key.Escape].wasPressedThisFrame)
#else
            if (Input.GetKeyDown(KeyCode.Escape))
#endif
                PlayerController.EnableLockCursor(false);
        }

        public void Swap()
        {
            var thirdPersonFollows =
                CinemachineCameraGameObject.GetComponentsInChildren<CinemachineThirdPersonFollow>(true);
            foreach (var tpf in thirdPersonFollows)
                tpf.CameraSide = Mathf.Abs(tpf.CameraSide - 1);
        }
    }
}