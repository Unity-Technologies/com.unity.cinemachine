using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.Splines;
using Unity.Collections;
using Unity.Mathematics;
using Cinemachine;

public class RuntimeDollyTrackTest
{
    private CinemachineDollyCart DollyCart;
    private CinemachineSplinePath path;

    [SetUp]
    public void Setup()
    {
        DollyCart = new GameObject().AddComponent<CinemachineDollyCart>();
        path = new GameObject().AddComponent<CinemachineSplinePath>();
        var array = new NativeArray<float3>(4, Allocator.Temp);
        array[0] = new float3(7, 1, -6);
        array[1] = new float3(13, 1, -6);
        array[2] = new float3(13, 1, 1);
        array[3] = new float3(7, 1, 1);
        Spline.CreateLinear(path.Spline, array, true);
        DollyCart.m_Path = path;
        DollyCart.m_UpdateMethod = CinemachineDollyCart.UpdateMethod.Update;
    }

    [TearDown]
    public void Teardown()
    {
        Object.Destroy(path.gameObject);
        Object.Destroy(DollyCart.gameObject);
    }

    [UnityTest]
    public IEnumerator LinearInterpolationTestDistance()
    {
        DollyCart.m_PositionUnits = CinemachinePathBase.PositionUnits.Distance;
        DollyCart.m_Position = 0;
        yield return null;
        UnityEngine.Assertions.Assert.AreApproximatelyEqual(Vector3.Distance(DollyCart.transform.position, new Vector3(7, 1, -6)), 0, 0.01f);
        UnityEngine.Assertions.Assert.IsTrue(DollyCart.transform.rotation.Equals(new Quaternion(0, 0.707106829f, 0, 0.707106829f)));

        DollyCart.m_Position = 0.5f;
        yield return null;
        UnityEngine.Assertions.Assert.AreApproximatelyEqual(Vector3.Distance(DollyCart.transform.position, new Vector3(7.5f, 1, -6)), 0, 0.01f);
        UnityEngine.Assertions.Assert.IsTrue(DollyCart.transform.rotation.Equals(new Quaternion(1.22928352e-08f, 0.707106709f, -1.22928343e-08f, 0.707106829f)));


        DollyCart.m_Position = 6;
        yield return null;
        UnityEngine.Assertions.Assert.AreApproximatelyEqual(Vector3.Distance(DollyCart.transform.position, new Vector3(12.98846f, 1, -6)), 0, 0.01f);
        UnityEngine.Assertions.Assert.IsTrue(DollyCart.transform.rotation.Equals(new Quaternion(-1.40489504e-08f, 0.707106829f, 1.40489504e-08f, 0.707106829f)));

        DollyCart.m_Position = 10;
        yield return null;
        UnityEngine.Assertions.Assert.AreApproximatelyEqual(Vector3.Distance(DollyCart.transform.position, new Vector3(13, 1, -2)), 0, 0.01f);
        UnityEngine.Assertions.Assert.IsTrue(DollyCart.transform.rotation.Equals(new Quaternion(8.51494963e-09f, -6.81195971e-08f, 5.80034927e-16f, 1)));

        DollyCart.m_Position = 15;
        yield return null;
        UnityEngine.Assertions.Assert.AreApproximatelyEqual(Vector3.Distance(DollyCart.transform.position, new Vector3(11, 1, 1)), 0, 0.01f);
        UnityEngine.Assertions.Assert.IsTrue(DollyCart.transform.rotation.Equals(new Quaternion(-3.51223806e-09f, -0.707106829f, -3.51223806e-09f, 0.707106829f)));

        DollyCart.m_Position = 20;
        yield return null;
        UnityEngine.Assertions.Assert.AreApproximatelyEqual(Vector3.Distance(DollyCart.transform.position, new Vector3(7, 1, 0)), 0, 0.01f);
        UnityEngine.Assertions.Assert.IsTrue(DollyCart.transform.rotation.Equals(new Quaternion(4.29928142e-16f, 1, 8.78104078e-09f, -4.89609562e-08f)));

        DollyCart.m_Position = 25;
        yield return null;
        UnityEngine.Assertions.Assert.AreApproximatelyEqual(Vector3.Distance(DollyCart.transform.position, new Vector3(7, 1, -5)), 0, 0.01f);
        UnityEngine.Assertions.Assert.IsTrue(DollyCart.transform.rotation.Equals(new Quaternion(0, 1, 0, 0)));
    }

    [UnityTest]
    public IEnumerator LinearInterpolationTestNormalized()
    {
        DollyCart.m_PositionUnits = CinemachinePathBase.PositionUnits.Normalized;
        DollyCart.m_Position = 0;
        yield return null;
        UnityEngine.Assertions.Assert.AreApproximatelyEqual(Vector3.Distance(DollyCart.transform.position, new Vector3(7, 1, -6)), 0, 0.01f);
        UnityEngine.Assertions.Assert.IsTrue(DollyCart.transform.rotation.Equals(new Quaternion(0, 0.707106829f, 0, 0.707106829f)));

        DollyCart.m_Position = 0.125f;
        yield return null;
        UnityEngine.Assertions.Assert.AreApproximatelyEqual(Vector3.Distance(DollyCart.transform.position, new Vector3(10.25f, 1, -6)), 0, 0.01f);
        UnityEngine.Assertions.Assert.IsTrue(DollyCart.transform.rotation.Equals(new Quaternion(-3.51223695e-09f, 0.707106829f, 3.51223739e-09f, 0.707106829f)));

        DollyCart.m_Position = 0.5f;
        yield return null;
        UnityEngine.Assertions.Assert.AreApproximatelyEqual(Vector3.Distance(DollyCart.transform.position, new Vector3(13, 1, 1)), 0, 0.01f);
        UnityEngine.Assertions.Assert.IsTrue(DollyCart.transform.rotation.Equals(new Quaternion(0, 2.72478388e-07f, 0, 1)));


        DollyCart.m_Position = 0.75f;
        yield return null;
        UnityEngine.Assertions.Assert.AreApproximatelyEqual(Vector3.Distance(DollyCart.transform.position, new Vector3(7, 1, 0.5f)), 0, 0.01f);
        UnityEngine.Assertions.Assert.IsTrue(DollyCart.transform.rotation.Equals(new Quaternion(-4.12793434e-17f, 1, -3.52572149e-09f, -1.17080559e-08f)));

        DollyCart.m_Position = 1;
        yield return null;
        UnityEngine.Assertions.Assert.AreApproximatelyEqual(Vector3.Distance(DollyCart.transform.position, new Vector3(7, 1, -6)), 0, 0.01f);
        UnityEngine.Assertions.Assert.IsTrue(DollyCart.transform.rotation.Equals(new Quaternion(0, 0.707106829f, 0, 0.707106829f)));
    }

    [UnityTest]
    public IEnumerator LinearInterpolationTestPathUnits()
    {
        DollyCart.m_PositionUnits = CinemachinePathBase.PositionUnits.PathUnits;
        DollyCart.m_Position = 0;
        yield return null;
        UnityEngine.Assertions.Assert.AreApproximatelyEqual(Vector3.Distance(DollyCart.transform.position, new Vector3(7, 1, -6)), 0, 0.01f);
        UnityEngine.Assertions.Assert.IsTrue(DollyCart.transform.rotation.Equals(new Quaternion(0, 0.707106829f, 0, 0.707106829f)));

        DollyCart.m_Position = 0.125f;
        yield return null;
        UnityEngine.Assertions.Assert.AreApproximatelyEqual(Vector3.Distance(DollyCart.transform.position, new Vector3(7.75f, 1, -6)), 0, 0.01f);
        UnityEngine.Assertions.Assert.IsTrue(DollyCart.transform.rotation.Equals(new Quaternion(0, 0.707106829f, 0, 0.707106829f)));

        DollyCart.m_Position = 0.5f;
        yield return null;
        UnityEngine.Assertions.Assert.AreApproximatelyEqual(Vector3.Distance(DollyCart.transform.position, new Vector3(10, 1, -6)), 0, 0.01f);
        UnityEngine.Assertions.Assert.IsTrue(DollyCart.transform.rotation.Equals(new Quaternion(0, 0.707106829f, 0, 0.707106829f)));

        DollyCart.m_Position = 1;
        yield return null;
        UnityEngine.Assertions.Assert.AreApproximatelyEqual(Vector3.Distance(DollyCart.transform.position, new Vector3(13, 1, -6)), 0, 0.01f);
        UnityEngine.Assertions.Assert.IsTrue(DollyCart.transform.rotation.Equals(new Quaternion(0, 0, 0, 1)));

        DollyCart.m_Position = 1.5f;
        yield return null;
        UnityEngine.Assertions.Assert.AreApproximatelyEqual(Vector3.Distance(DollyCart.transform.position, new Vector3(13, 1, -2.5f)), 0, 0.01f);
        UnityEngine.Assertions.Assert.IsTrue(DollyCart.transform.rotation.Equals(new Quaternion(0, 0, 0, 1)));

        DollyCart.m_Position = 1.75f;
        yield return null;
        UnityEngine.Assertions.Assert.AreApproximatelyEqual(Vector3.Distance(DollyCart.transform.position, new Vector3(13, 1, -0.75f)), 0, 0.01f);
        UnityEngine.Assertions.Assert.IsTrue(DollyCart.transform.rotation.Equals(new Quaternion(0, 0, 0, 1)));

        DollyCart.m_Position = 3;
        yield return null;
        UnityEngine.Assertions.Assert.AreApproximatelyEqual(Vector3.Distance(DollyCart.transform.position, new Vector3(7, 1, 1)), 0, 0.01f);
        UnityEngine.Assertions.Assert.IsTrue(DollyCart.transform.rotation.Equals(new Quaternion(0, 1, 0, 0)));

        DollyCart.m_Position = 3.5f;
        yield return null;
        UnityEngine.Assertions.Assert.AreApproximatelyEqual(Vector3.Distance(DollyCart.transform.position, new Vector3(7, 1, -2.5f)), 0, 0.01f);
        UnityEngine.Assertions.Assert.IsTrue(DollyCart.transform.rotation.Equals(new Quaternion(0, 1, 0, 0)));
    }
}
