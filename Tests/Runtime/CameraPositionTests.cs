using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Cinemachine;

public class CameraPositionTests
{
    private Camera cam;
    private CinemachineVirtualCamera vcam;
    private GameObject followObject; 

    [SetUp]
    public void Setup()
    {
        var cameraHolder = new GameObject("MainCamera");
        cam = cameraHolder.AddComponent<Camera>();
        cameraHolder.AddComponent<CinemachineBrain>();
        var vcamHolder = new GameObject("CM Vcam");
        vcam = vcamHolder.AddComponent<CinemachineVirtualCamera>();
        vcam.Priority = 100;
        followObject = new GameObject("Follow Object");
    }

    [TearDown]
    public void Teardown()
    {
        Object.Destroy(cam.gameObject);
        Object.Destroy(vcam.gameObject);
    }


    [UnityTest]
    public IEnumerator DoNothing()
    {
        vcam.Follow = followObject.transform;
        Vector3 oldPos = vcam.transform.position;
        followObject.transform.position += new Vector3(2, 2, 2);
        yield return null;
        Assert.IsTrue(vcam.State.FinalPosition == oldPos);
    }

    [UnityTest]
    public IEnumerator ThirdPerson()
    {
        vcam.AddCinemachineComponent<Cinemachine3rdPersonFollow>();
        vcam.Follow = followObject.transform;
        followObject.transform.position += new Vector3(10, 0, 0);
        yield return null; 
        Assert.IsTrue(vcam.State.FinalPosition == followObject.transform.position);
    }

    [UnityTest]
    public IEnumerator FramingTransposer()
    {
        CinemachineFramingTransposer component = vcam.AddCinemachineComponent<CinemachineFramingTransposer>();
        component.m_XDamping = 0;
        component.m_YDamping = 0;
        component.m_ZDamping = 0;
        component.m_CameraDistance = 1f;
        vcam.Follow = followObject.transform;
        followObject.transform.position += new Vector3(10, 0, 0);
        yield return null;
        Assert.IsTrue(vcam.State.FinalPosition == new Vector3(10, 0, -component.m_CameraDistance));
    }

    [UnityTest]
    public IEnumerator HardLockToTarget()
    {
        vcam.AddCinemachineComponent<CinemachineHardLockToTarget>();
        vcam.Follow = followObject.transform;
        followObject.transform.position += new Vector3(10, 0, 0);
        yield return null;
        Assert.IsTrue(vcam.State.FinalPosition == followObject.transform.position);
    }

    [UnityTest]
    public IEnumerator OrbTransposer()
    {
        CinemachineOrbitalTransposer component = vcam.AddCinemachineComponent<CinemachineOrbitalTransposer>();
        component.m_XDamping = 0;
        component.m_YDamping = 0;
        component.m_ZDamping = 0;
        component.m_FollowOffset = new Vector3(0, 0, 0);
        vcam.Follow = followObject.transform;
        followObject.transform.position += new Vector3(10, 0, 0);
        yield return null;
        Assert.IsTrue(vcam.State.FinalPosition == followObject.transform.position);
    }

    [UnityTest]
    public IEnumerator TrackedDolly()
    {
        vcam.AddCinemachineComponent<CinemachineTrackedDolly>();
        vcam.Follow = followObject.transform;
        Vector3 oldPos = vcam.transform.position;
        followObject.transform.position += new Vector3(2, 2, 2);
        yield return null;
        Assert.IsTrue(vcam.State.FinalPosition == oldPos);
    }

    [UnityTest]
    public IEnumerator Transposer()
    {
        CinemachineTransposer component = vcam.AddCinemachineComponent<CinemachineTransposer>();
        component.m_XDamping = 0;
        component.m_YDamping = 0;
        component.m_ZDamping = 0;
        component.m_FollowOffset = new Vector3(0, 0, 0);
        vcam.Follow = followObject.transform;
        followObject.transform.position += new Vector3(10, 0, 0);
        yield return null;
        Assert.IsTrue(vcam.State.FinalPosition == followObject.transform.position);
    }

}