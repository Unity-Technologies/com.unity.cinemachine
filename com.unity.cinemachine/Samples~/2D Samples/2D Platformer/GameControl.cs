using UnityEngine;

namespace Unity.Cinemachine.Samples
{
    public class GameControl : MonoBehaviour
    {
        public CinemachineVirtualCameraBase InitialCamera;
        public SimplePlayerController2D Player;
        public Vector3 StartPosition;

        public void RestartGame()
        {
            // Move the plyer to its start position
            Player.transform.position = StartPosition;
            Player.PlayerGeometry.rotation = Quaternion.Euler(0, 90, 0);

            // Reset the camera state, to cancel damping
            CinemachineCore.ResetCameraState();

            // Activate the initial camera, deactivate the rest
            for (int i = 0; i < CinemachineCore.VirtualCameraCount; ++i)
                CinemachineCore.GetVirtualCamera(i).gameObject.SetActive(
                    CinemachineCore.GetVirtualCamera(i) == InitialCamera);
        }
    }
}
