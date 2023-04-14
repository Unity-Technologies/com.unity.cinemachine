using Unity.Cinemachine;
using UnityEngine;

public class GameControl : MonoBehaviour
{
    public Transform Player;
    public Vector3 StartPosition;

    public void RestartGame()
    {
        var delta = StartPosition - Player.position;
        Player.position = StartPosition;
        CinemachineCore.OnTargetObjectWarped(Player, delta);
        for (int i = 0; i < CinemachineCore.VirtualCameraCount; ++i)
            CinemachineCore.GetVirtualCamera(i).PreviousStateIsValid = false;
    }
}
