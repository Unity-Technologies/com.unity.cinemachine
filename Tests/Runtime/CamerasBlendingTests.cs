using System.Collections;
using UnityEngine;
using Cinemachine;
using NUnit.Framework;
using UnityEngine.TestTools;

[TestFixture]
[Ignore("Instability in blending tests")]
public class CamerasBlendingTests
{
    private Camera cam;
    private CinemachineVirtualCamera sourceVCam;
    private CinemachineVirtualCamera targetVCam;
    private CinemachineBrain brain;
    private GameObject followObject;

    private const float BlendingTime = 1;

    [SetUp]
    public void Setup()
    {
        // Camera
        var cameraHolder = new GameObject("MainCamera");
        cam = cameraHolder.AddComponent<Camera>();
        brain = cameraHolder.AddComponent<CinemachineBrain>();

        // Blending
        brain.m_DefaultBlend = new CinemachineBlendDefinition(
            CinemachineBlendDefinition.Style.Linear, 
            BlendingTime);
        
        followObject = new GameObject("Follow Object");
        
        // Source vcam
        var sourceVCamHolder = new GameObject("Source CM Vcam");
        sourceVCam = sourceVCamHolder.AddComponent<CinemachineVirtualCamera>();
        sourceVCam.Priority = 2;
        sourceVCam.Follow = followObject.transform;

        // target vcam
        var targetVCamHolder = new GameObject("Target CM Vcam");
        targetVCam = targetVCamHolder.AddComponent<CinemachineVirtualCamera>();
        targetVCam.Priority = 1;
        targetVCam.Follow = followObject.transform;
        
        CinemachineCore.UniformDeltaTimeOverride = 0.1f;
    }

    [TearDown]
    public void Teardown()
    {
        Object.Destroy(cam.gameObject);
        Object.Destroy(sourceVCam.gameObject);
        Object.Destroy(targetVCam.gameObject);
        Object.Destroy(followObject);
        
        CinemachineCore.UniformDeltaTimeOverride = -1f;
    }


    [UnityTest]
    public IEnumerator BlendingBetweenCameras()
    {
        
        targetVCam.Priority = 3;
        yield return null;

        yield return new WaitForSeconds(BlendingTime + 0.1f);
        Assert.IsTrue(!brain.IsBlending);
    }
    
    [UnityTest]
    public IEnumerator InterruptedBlendingBetweenCameras()
    {
        // Start blending
        targetVCam.Priority = 3;
        yield return null;

        // Wait for 90% of blending duration
        yield return new WaitForSeconds(BlendingTime * 0.9f);
        
        // Blend back to source
        targetVCam.Priority = 1;
        yield return null;
        yield return new WaitForSeconds(BlendingTime * 0.1f);
        
        // Quickly blend to target again
        targetVCam.Priority = 3;
        yield return null;
        
        // We went 90%, then got 10% back, it means we are 20% away from the target
        yield return new WaitForSeconds(BlendingTime * 0.21f);

        Assert.IsTrue(!brain.IsBlending);

        // Start blending
        targetVCam.Priority = 3;
        yield return null;

        // Wait for 90% of blending duration
        yield return new WaitForSeconds(BlendingTime * 0.9f);
        
        // Blend back to source
        targetVCam.Priority = 1;
        yield return null;
        yield return new WaitForSeconds(BlendingTime * 0.1f);
        
        // Quickly blend to target again
        targetVCam.Priority = 3;
        yield return null;
        
        // We went 90%, then got 10% back, it means we are 20% away from the target - wait only 10% worth
        yield return new WaitForSeconds(BlendingTime * 0.1f);

        // Blend back to source
        targetVCam.Priority = 1;
        yield return null;
        yield return new WaitForSeconds(BlendingTime * 0.1f);
        
        // Quickly blend to target again
        targetVCam.Priority = 3;
        yield return null;
        
        // We went 90%, then got 10% back, it means we are 20% away from the target
        yield return new WaitForSeconds(BlendingTime * 0.21f);

        Assert.IsTrue(!brain.IsBlending);
    }
    
    [UnityTest]
    public IEnumerator DoesInterruptedBlendingBetweenCamerasTakesDoubleTime()
    {
        // Start blending
        targetVCam.Priority = 3;
        yield return null;

        // Wait for 90% of blending duration
        yield return new WaitForSeconds(BlendingTime * 0.9f);
        
        // Blend back to source
        targetVCam.Priority = 1;
        yield return null;
        yield return new WaitForSeconds(BlendingTime * 0.1f);
        
        // Quickly blend to target again
        targetVCam.Priority = 3;
        yield return null;
        
        // We went 90%, then got 10% back, it means we are 20% away from the target
        yield return new WaitForSeconds(BlendingTime + 0.01f);

        Assert.IsTrue(!brain.IsBlending);
    }
    
}
