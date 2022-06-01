using UnityEngine;

namespace Cinemachine.Examples
{

public class ScriptingExample : MonoBehaviour
{
    CmCamera vcam;
    CmCamera freelook;

    void Start()
    {
        // Create a Cinemachine brain on the main camera
        var brain = GameObject.Find("Main Camera").AddComponent<CinemachineBrain>();
        brain.m_ShowDebugText = true;
        brain.m_DefaultBlend.m_Time = 1;

        // Create a virtual camera that looks at object "Cube", and set some settings
        vcam = new GameObject("VirtualCamera").AddComponent<CmCamera>();
        vcam.Follow = GameObject.Find("Cube").transform;
        vcam.m_Priority = 10;
        vcam.gameObject.transform.position = new Vector3(0, 1, 0);

        // Install a composer.  You can install whatever CinemachineComponents you need,
        // including your own custom-authored Cinemachine components.
        var composer = vcam.gameObject.AddComponent<CinemachineComposer>();
        composer.m_ScreenX = 0.30f;
        composer.m_ScreenY = 0.35f;

        // Create a FreeLook vcam on object "Cylinder"
        freelook = new GameObject("FreeLook").AddComponent<CmCamera>();
        freelook.gameObject.AddComponent<CinemachineOrbitalFollow>();
        freelook.gameObject.AddComponent<CinemachineComposer>();
        freelook.gameObject.AddComponent<InputAxisController>();
        freelook.Follow = GameObject.Find("Cylinder").transform;
        freelook.LookAt = GameObject.Find("Cylinder/Sphere").transform;
        freelook.m_Priority = 11;
    }

    float lastSwapTime = 0;
    void Update()
    {
        // Switch cameras from time to time to show blending
        if (Time.realtimeSinceStartup - lastSwapTime > 5)
        {
            freelook.enabled = !freelook.enabled;
            lastSwapTime = Time.realtimeSinceStartup;
        }
    }
}

}
