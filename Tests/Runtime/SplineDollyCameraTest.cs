#if CINEMACHINE_UNITY_SPLINES
using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.Splines;
using Unity.Mathematics;
using Cinemachine;

namespace Tests.Runtime
{
    public class SplineDollyCameraTest : CinemachineFixtureBase
    {
        CinemachineVirtualCamera m_vcam;
        CinemachineSplineDolly m_Dolly;
        SplineContainer m_SplineContainer;

        [SetUp]
        public void Setup()
        {
            m_SplineContainer = CreateGameObject("Dolly Track", typeof(SplineContainer)).GetComponent<SplineContainer>();
            m_SplineContainer.Spline = SplineFactory.CreateLinear(
                new List<float3> { new float3(7, 1, -6), new float3(13, 1, -6), new float3(13, 1, 1), new float3(7, 1, 1) },
                true);
            
            m_vcam = CreateGameObject("CM vcam", typeof(CinemachineVirtualCamera)).GetComponent<CinemachineVirtualCamera>();
            m_Dolly = m_vcam.AddCinemachineComponent<CinemachineSplineDolly>();
            m_vcam.AddCinemachineComponent<CinemachineComposer>();
            m_Dolly.m_Spline = m_SplineContainer;
            m_Dolly.m_Damping = Vector3.zero;
        }

        [UnityTest]
        public IEnumerator LinearInterpolationTestDistance()
        {
            m_Dolly.m_PositionUnits = PathIndexUnit.Distance;
            m_Dolly.m_SplinePosition = 0;
            m_vcam.InternalUpdateCameraState(Vector3.up, 0);
            yield return null;
            UnityEngine.Assertions.Assert.AreApproximatelyEqual(Vector3.Distance(m_vcam.State.FinalPosition, new Vector3(7, 1, -6)), 0, 0.1f);

            m_Dolly.m_SplinePosition = 0.5f;
            m_vcam.InternalUpdateCameraState(Vector3.up, 0);
            yield return null;
            UnityEngine.Assertions.Assert.AreApproximatelyEqual(Vector3.Distance(m_vcam.State.FinalPosition, new Vector3(7.5f, 1, -6)), 0, 0.1f);

            m_Dolly.m_SplinePosition = 6;
            m_vcam.InternalUpdateCameraState(Vector3.up, 0);
            yield return null;
            UnityEngine.Assertions.Assert.AreApproximatelyEqual(Vector3.Distance(m_vcam.State.FinalPosition, new Vector3(12.98846f, 1, -6)), 0, 0.1f);

            m_Dolly.m_SplinePosition = 10;
            m_vcam.InternalUpdateCameraState(Vector3.up, 0);
            yield return null;
            UnityEngine.Assertions.Assert.AreApproximatelyEqual(Vector3.Distance(m_vcam.State.FinalPosition, new Vector3(13, 1, -2)), 0, 0.1f);

            m_Dolly.m_SplinePosition = 15;
            m_vcam.InternalUpdateCameraState(Vector3.up, 0);
            yield return null;
            UnityEngine.Assertions.Assert.AreApproximatelyEqual(Vector3.Distance(m_vcam.State.FinalPosition, new Vector3(11, 1, 1)), 0, 0.1f);

            m_Dolly.m_SplinePosition = 20;
            m_vcam.InternalUpdateCameraState(Vector3.up, 0);
            yield return null;
            UnityEngine.Assertions.Assert.AreApproximatelyEqual(Vector3.Distance(m_vcam.State.FinalPosition, new Vector3(7, 1, 0)), 0, 0.1f);

            m_Dolly.m_SplinePosition = 26;
            m_vcam.InternalUpdateCameraState(Vector3.up, 0);
            yield return null;
            UnityEngine.Assertions.Assert.AreApproximatelyEqual(Vector3.Distance(m_vcam.State.FinalPosition, new Vector3(7, 1, -6)), 0, 0.1f);
        }

        [UnityTest]
        public IEnumerator LinearInterpolationTestNormalized()
        {
            m_Dolly.m_PositionUnits = PathIndexUnit.Normalized;
            m_Dolly.m_SplinePosition = 0;
            m_vcam.InternalUpdateCameraState(Vector3.up, 0);
            yield return null;
            UnityEngine.Assertions.Assert.AreApproximatelyEqual(Vector3.Distance(m_vcam.State.FinalPosition, new Vector3(7, 1, -6)), 0, 0.1f);

            m_Dolly.m_SplinePosition = 0.125f;
            m_vcam.InternalUpdateCameraState(Vector3.up, 0);
            yield return null;
            UnityEngine.Assertions.Assert.AreApproximatelyEqual(Vector3.Distance(m_vcam.State.FinalPosition, new Vector3(10.25f, 1, -6)), 0, 0.1f);

            m_Dolly.m_SplinePosition = 0.5f;
            m_vcam.InternalUpdateCameraState(Vector3.up, 0);
            yield return null;
            UnityEngine.Assertions.Assert.AreApproximatelyEqual(Vector3.Distance(m_vcam.State.FinalPosition, new Vector3(13, 1, 1)), 0, 0.1f);

            m_Dolly.m_SplinePosition = 0.75f;
            m_vcam.InternalUpdateCameraState(Vector3.up, 0);
            yield return null;
            UnityEngine.Assertions.Assert.AreApproximatelyEqual(Vector3.Distance(m_vcam.State.FinalPosition, new Vector3(7, 1, 0.5f)), 0, 0.1f);

            m_Dolly.m_SplinePosition = 1;
            m_vcam.InternalUpdateCameraState(Vector3.up, 0);
            yield return null;
            UnityEngine.Assertions.Assert.AreApproximatelyEqual(Vector3.Distance(m_vcam.State.FinalPosition, new Vector3(7, 1, -6)), 0, 0.1f);
        }

        [UnityTest]
        public IEnumerator LinearInterpolationTestPathUnits()
        {
            m_Dolly.m_PositionUnits = PathIndexUnit.Knot;
            m_Dolly.m_SplinePosition = 0;
            m_vcam.InternalUpdateCameraState(Vector3.up, 0);
            yield return null;
            UnityEngine.Assertions.Assert.AreApproximatelyEqual(Vector3.Distance(m_vcam.State.FinalPosition, new Vector3(7, 1, -6)), 0, 0.1f);

            m_Dolly.m_SplinePosition = 0.125f;
            m_vcam.InternalUpdateCameraState(Vector3.up, 0);
            yield return null;
            UnityEngine.Assertions.Assert.AreApproximatelyEqual(Vector3.Distance(m_vcam.State.FinalPosition, new Vector3(7.75f, 1, -6)), 0, 0.1f);

            m_Dolly.m_SplinePosition = 0.5f;
            m_vcam.InternalUpdateCameraState(Vector3.up, 0);
            yield return null;
            UnityEngine.Assertions.Assert.AreApproximatelyEqual(Vector3.Distance(m_vcam.State.FinalPosition, new Vector3(10, 1, -6)), 0, 0.1f);

            m_Dolly.m_SplinePosition = 1;
            m_vcam.InternalUpdateCameraState(Vector3.up, 0);
            yield return null;
            UnityEngine.Assertions.Assert.AreApproximatelyEqual(Vector3.Distance(m_vcam.State.FinalPosition, new Vector3(13, 1, -6)), 0, 0.1f);

            m_Dolly.m_SplinePosition = 1.5f;
            m_vcam.InternalUpdateCameraState(Vector3.up, 0);
            yield return null;
            UnityEngine.Assertions.Assert.AreApproximatelyEqual(Vector3.Distance(m_vcam.State.FinalPosition, new Vector3(13, 1, -2.5f)), 0, 0.1f);

            m_Dolly.m_SplinePosition = 1.75f;
            m_vcam.InternalUpdateCameraState(Vector3.up, 0);
            yield return null;
            UnityEngine.Assertions.Assert.AreApproximatelyEqual(Vector3.Distance(m_vcam.State.FinalPosition, new Vector3(13, 1, -0.75f)), 0, 0.1f);

            m_Dolly.m_SplinePosition = 3;
            m_vcam.InternalUpdateCameraState(Vector3.up, 0);
            yield return null;
            UnityEngine.Assertions.Assert.AreApproximatelyEqual(Vector3.Distance(m_vcam.State.FinalPosition, new Vector3(7, 1, 1)), 0, 0.1f);

            m_Dolly.m_SplinePosition = 3.5f;
            m_vcam.InternalUpdateCameraState(Vector3.up, 0);
            yield return null;
            UnityEngine.Assertions.Assert.AreApproximatelyEqual(Vector3.Distance(m_vcam.State.FinalPosition, new Vector3(7, 1, -2.5f)), 0, 0.1f);
        }
    }
}
#endif
