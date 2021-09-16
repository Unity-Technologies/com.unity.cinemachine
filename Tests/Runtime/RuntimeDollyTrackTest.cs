using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.Splines;
using Unity.Mathematics;
using Cinemachine;

public class RuntimeDollyTrackTest
{
    CinemachineDollyCart m_DollyCart;
    CinemachineSplinePath m_Path;

    [SetUp]
    public void Setup()
    {
        m_DollyCart = new GameObject().AddComponent<CinemachineDollyCart>();
        m_Path = new GameObject().AddComponent<CinemachineSplinePath>();
        m_Path.Spline = SplineFactory.CreateLinear(
            new List<float3> { new float3(7, 1, -6), new float3(13, 1, -6), new float3(13, 1, 1), new float3(7, 1, 1) }, 
            true);
        m_DollyCart.m_Path = m_Path;
        m_DollyCart.m_UpdateMethod = CinemachineDollyCart.UpdateMethod.Update;
    }

    [UnityTest]
    public IEnumerator LinearInterpolationTestDistance()
    {
        m_DollyCart.m_PositionUnits = CinemachinePathBase.PositionUnits.Distance;
        m_DollyCart.m_Position = 0;
        yield return null;
        UnityEngine.Assertions.Assert.AreApproximatelyEqual(Vector3.Distance(m_DollyCart.transform.position, new Vector3(7, 1, -6)), 0, 0.01f);
        
        m_DollyCart.m_Position = 0.5f;
        yield return null;
        UnityEngine.Assertions.Assert.AreApproximatelyEqual(Vector3.Distance(m_DollyCart.transform.position, new Vector3(7.5f, 1, -6)), 0, 0.01f);
        
        m_DollyCart.m_Position = 6;
        yield return null;
        UnityEngine.Assertions.Assert.AreApproximatelyEqual(Vector3.Distance(m_DollyCart.transform.position, new Vector3(12.98846f, 1, -6)), 0, 0.01f);
        
        m_DollyCart.m_Position = 10;
        yield return null;
        UnityEngine.Assertions.Assert.AreApproximatelyEqual(Vector3.Distance(m_DollyCart.transform.position, new Vector3(13, 1, -2)), 0, 0.01f);
        
        m_DollyCart.m_Position = 15;
        yield return null;
        UnityEngine.Assertions.Assert.AreApproximatelyEqual(Vector3.Distance(m_DollyCart.transform.position, new Vector3(11, 1, 1)), 0, 0.01f);
        
        m_DollyCart.m_Position = 20;
        yield return null;
        UnityEngine.Assertions.Assert.AreApproximatelyEqual(Vector3.Distance(m_DollyCart.transform.position, new Vector3(7, 1, 0)), 0, 0.01f);
        
        m_DollyCart.m_Position = 25;
        yield return null;
        UnityEngine.Assertions.Assert.AreApproximatelyEqual(Vector3.Distance(m_DollyCart.transform.position, new Vector3(7, 1, -5)), 0, 0.01f);
    }

    [UnityTest]
    public IEnumerator LinearInterpolationTestNormalized()
    {
        m_DollyCart.m_PositionUnits = CinemachinePathBase.PositionUnits.Normalized;
        m_DollyCart.m_Position = 0;
        yield return null;
        UnityEngine.Assertions.Assert.AreApproximatelyEqual(Vector3.Distance(m_DollyCart.transform.position, new Vector3(7, 1, -6)), 0, 0.01f);
        
        m_DollyCart.m_Position = 0.125f;
        yield return null;
        UnityEngine.Assertions.Assert.AreApproximatelyEqual(Vector3.Distance(m_DollyCart.transform.position, new Vector3(10.25f, 1, -6)), 0, 0.01f);
        
        m_DollyCart.m_Position = 0.5f;
        yield return null;
        UnityEngine.Assertions.Assert.AreApproximatelyEqual(Vector3.Distance(m_DollyCart.transform.position, new Vector3(13, 1, 1)), 0, 0.01f);
        
        m_DollyCart.m_Position = 0.75f;
        yield return null;
        UnityEngine.Assertions.Assert.AreApproximatelyEqual(Vector3.Distance(m_DollyCart.transform.position, new Vector3(7, 1, 0.5f)), 0, 0.01f);
        
        m_DollyCart.m_Position = 1;
        yield return null;
        UnityEngine.Assertions.Assert.AreApproximatelyEqual(Vector3.Distance(m_DollyCart.transform.position, new Vector3(7, 1, -6)), 0, 0.01f);
    }

    [UnityTest]
    public IEnumerator LinearInterpolationTestPathUnits()
    {
        m_DollyCart.m_PositionUnits = CinemachinePathBase.PositionUnits.PathUnits;
        m_DollyCart.m_Position = 0;
        yield return null;
        UnityEngine.Assertions.Assert.AreApproximatelyEqual(Vector3.Distance(m_DollyCart.transform.position, new Vector3(7, 1, -6)), 0, 0.01f);
        
        m_DollyCart.m_Position = 0.125f;
        yield return null;
        UnityEngine.Assertions.Assert.AreApproximatelyEqual(Vector3.Distance(m_DollyCart.transform.position, new Vector3(7.75f, 1, -6)), 0, 0.01f);
        
        m_DollyCart.m_Position = 0.5f;
        yield return null;
        UnityEngine.Assertions.Assert.AreApproximatelyEqual(Vector3.Distance(m_DollyCart.transform.position, new Vector3(10, 1, -6)), 0, 0.01f);
        
        m_DollyCart.m_Position = 1;
        yield return null;
        UnityEngine.Assertions.Assert.AreApproximatelyEqual(Vector3.Distance(m_DollyCart.transform.position, new Vector3(13, 1, -6)), 0, 0.01f);
        
        m_DollyCart.m_Position = 1.5f;
        yield return null;
        UnityEngine.Assertions.Assert.AreApproximatelyEqual(Vector3.Distance(m_DollyCart.transform.position, new Vector3(13, 1, -2.5f)), 0, 0.01f);
        
        m_DollyCart.m_Position = 1.75f;
        yield return null;
        UnityEngine.Assertions.Assert.AreApproximatelyEqual(Vector3.Distance(m_DollyCart.transform.position, new Vector3(13, 1, -0.75f)), 0, 0.01f);
        
        m_DollyCart.m_Position = 3;
        yield return null;
        UnityEngine.Assertions.Assert.AreApproximatelyEqual(Vector3.Distance(m_DollyCart.transform.position, new Vector3(7, 1, 1)), 0, 0.01f);
        
        m_DollyCart.m_Position = 3.5f;
        yield return null;
        UnityEngine.Assertions.Assert.AreApproximatelyEqual(Vector3.Distance(m_DollyCart.transform.position, new Vector3(7, 1, -2.5f)), 0, 0.01f);
    }
}
