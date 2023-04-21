using Unity.Cinemachine;
using Unity.Cinemachine.Samples;
using UnityEngine;

public class SwapShoulders : MonoBehaviour
{
    public GameObject CinemachineCameraGameObject;
    public SimplePlayerControllerBase PlayerController;

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
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
