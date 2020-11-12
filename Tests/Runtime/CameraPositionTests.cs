using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Cinemachine;

public class CameraPositionTests
{
    private Camera cam;
    private CinemachineVirtualCamera vcam;
    private GameObject followPbject; 

    [SetUp]
    public void Setup()
    {
        var cameraHolder = new GameObject("MainCamera");
        cam = cameraHolder.AddComponent<Camera>();
        cameraHolder.AddComponent<CinemachineBrain>();
        var vcamHolder = new GameObject("CM Vcam");
        vcam = vcamHolder.AddComponent<CinemachineVirtualCamera>();
        vcam.Priority = 100;
        followPbject = new GameObject("Follow Object");
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
        vcam.Follow = followPbject.transform;
        Vector3 oldPos = vcam.transform.position;
        followPbject.transform.position += new Vector3(2, 2, 2);
        yield return null;
        UnityEngine.Assertions.Assert.AreApproximatelyEqual(Vector3.Distance(vcam.transform.position, oldPos), 0, 0.01f);
    }

    [UnityTest]
    public IEnumerator ThirdPerson()
    {
        vcam.AddCinemachineComponent<Cinemachine3rdPersonFollow>();
        vcam.Follow = followPbject.transform;
        Vector3 oldPos = vcam.transform.position;
        followPbject.transform.position += new Vector3(10, 0, 0);
        yield return null;
        UnityEngine.Assertions.Assert.AreApproximatelyEqual(Vector3.Distance(vcam.transform.position, oldPos), 10, 0.01f);
    }

    [UnityTest]
    public IEnumerator FramingTransposer()
    {
        CinemachineFramingTransposer component = vcam.AddCinemachineComponent<CinemachineFramingTransposer>();
        component.m_XDamping = 0;
        component.m_YDamping = 0;
        component.m_ZDamping = 0;
        component.m_CameraDistance = 0;
        vcam.Follow = followPbject.transform;
        Vector3 oldPos = vcam.transform.position;
        followPbject.transform.position += new Vector3(10, 0, 0);
        yield return null;
        UnityEngine.Assertions.Assert.AreApproximatelyEqual(Vector3.Distance(vcam.transform.position, oldPos), 10, 0.01f);
    }

    [UnityTest]
    public IEnumerator HardLockToTarget()
    {
        vcam.AddCinemachineComponent<CinemachineHardLockToTarget>();
        vcam.Follow = followPbject.transform;
        Vector3 oldPos = vcam.transform.position;
        followPbject.transform.position += new Vector3(10, 0, 0);
        yield return null;
        UnityEngine.Assertions.Assert.AreApproximatelyEqual(Vector3.Distance(vcam.transform.position, oldPos), 10, 0.01f);
    }

    [UnityTest]
    public IEnumerator OrbTransposer()
    {
        CinemachineOrbitalTransposer component = vcam.AddCinemachineComponent<CinemachineOrbitalTransposer>();
        component.m_XDamping = 0;
        component.m_YDamping = 0;
        component.m_ZDamping = 0;
        component.m_FollowOffset = new Vector3(0, 0, 0);
        vcam.Follow = followPbject.transform;
        Vector3 oldPos = vcam.transform.position;
        followPbject.transform.position += new Vector3(10, 0, 0);
        yield return null;
        UnityEngine.Assertions.Assert.AreApproximatelyEqual(Vector3.Distance(vcam.transform.position, oldPos), 10, 0.01f);
    }

    [UnityTest]
    public IEnumerator TrackedDolly()
    {
        vcam.AddCinemachineComponent<CinemachineTrackedDolly>();
        vcam.Follow = followPbject.transform;
        Vector3 oldPos = vcam.transform.position;
        followPbject.transform.position += new Vector3(2, 2, 2);
        yield return null;
        UnityEngine.Assertions.Assert.AreApproximatelyEqual(Vector3.Distance(vcam.transform.position, oldPos), 0, 0.01f);
    }

    [UnityTest]
    public IEnumerator Transposer()
    {
        CinemachineTransposer component = vcam.AddCinemachineComponent<CinemachineTransposer>();
        component.m_XDamping = 0;
        component.m_YDamping = 0;
        component.m_ZDamping = 0;
        component.m_FollowOffset = new Vector3(0, 0, 0);
        vcam.Follow = followPbject.transform;
        Vector3 oldPos = vcam.transform.position;
        followPbject.transform.position += new Vector3(10, 0, 0);
        yield return null;
        UnityEngine.Assertions.Assert.AreApproximatelyEqual(Vector3.Distance(vcam.transform.position, oldPos), 10, 0.01f);
    }

}