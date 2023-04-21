using UnityEngine;
#if CINEMACHINE_UNITY_INPUTSYSTEM
using UnityEngine.InputSystem;
#endif

namespace Unity.Cinemachine.Samples
{
    public class CursorUnlockListener : MonoBehaviour
    {
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
    }
}
