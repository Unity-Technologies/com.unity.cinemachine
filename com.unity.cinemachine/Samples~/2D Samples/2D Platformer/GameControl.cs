using Unity.Cinemachine;
using UnityEngine;

public class GameControl : MonoBehaviour
{
    public Transform Player;
    public Vector3 StartPosition;

    public void RestartGame()
    {
        Player.transform.SetLocalPositionAndRotation(StartPosition, Quaternion.identity);
        CinemachineCore.ResetCameraState();
    }
}
