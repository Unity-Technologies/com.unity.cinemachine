using Unity.Cinemachine;
using UnityEngine;

public class GameControl : MonoBehaviour
{
    public CinemachineVirtualCameraBase InitialCamera;
    public Transform Player;
    public Vector3 StartPosition;

    public void RestartGame()
    {
        Player.transform.SetLocalPositionAndRotation(StartPosition, Quaternion.identity);
        CinemachineCore.ResetCameraState();

        // Activate the initial camera, deactivate the rest
        for (int i = 0; i < CinemachineCore.VirtualCameraCount; ++i)
            CinemachineCore.GetVirtualCamera(i).gameObject.SetActive(
                CinemachineCore.GetVirtualCamera(i) == InitialCamera);
    }
}
