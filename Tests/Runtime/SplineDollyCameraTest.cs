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
        CinemachineVirtualCamera m_Vcam;
        CinemachineSplineDolly m_Dolly;
        SplineContainer m_SplineContainer;

        [SetUp]
        public void Setup()
        {
            m_SplineContainer = CreateGameObject("Dolly Track", typeof(SplineContainer)).GetComponent<SplineContainer>();
            m_SplineContainer.Spline = SplineFactory.CreateLinear(
                new List<float3> { new float3(7, 1, -6), new float3(13, 1, -6), new float3(13, 1, 1), new float3(7, 1, 1) },
                true);
            
            m_Vcam = CreateGameObject("CM vcam", typeof(CinemachineVirtualCamera)).GetComponent<CinemachineVirtualCamera>();
            m_Vcam.Follow = CreatePrimitive(PrimitiveType.Cube).transform;
            m_Dolly = m_Vcam.AddCinemachineComponent<CinemachineSplineDolly>();
            m_Dolly.m_Spline = m_SplineContainer;
            m_Dolly.m_CameraUp = CinemachineSplineDolly.CameraUpMode.Default;
            m_Dolly.m_DampingEnabled = false;
        }

        [UnityTest]
        public IEnumerator LinearInterpolationTestDistance()
        {
            m_Dolly.m_PositionUnits = PathIndexUnit.Distance;
            m_Dolly.m_CameraPosition = 0;
            m_Vcam.InternalUpdateCameraState(Vector3.up, 0);
            yield return null;
            UnityEngine.Assertions.Assert.AreApproximatelyEqual(Vector3.Distance(m_Vcam.State.FinalPosition, new Vector3(7, 1, -6)), 0, 0.1f);

            m_Dolly.m_CameraPosition = 0.5f;
            m_Vcam.InternalUpdateCameraState(Vector3.up, 0);
            yield return null;
            UnityEngine.Assertions.Assert.AreApproximatelyEqual(Vector3.Distance(m_Vcam.State.FinalPosition, new Vector3(7.5f, 1, -6)), 0, 0.1f);

            m_Dolly.m_CameraPosition = 6;
            m_Vcam.InternalUpdateCameraState(Vector3.up, 0);
            yield return null;
            UnityEngine.Assertions.Assert.AreApproximatelyEqual(Vector3.Distance(m_Vcam.State.FinalPosition, new Vector3(12.98846f, 1, -6)), 0, 0.1f);

            m_Dolly.m_CameraPosition = 10;
            m_Vcam.InternalUpdateCameraState(Vector3.up, 0);
            yield return null;
            UnityEngine.Assertions.Assert.AreApproximatelyEqual(Vector3.Distance(m_Vcam.State.FinalPosition, new Vector3(13, 1, -2)), 0, 0.1f);

            m_Dolly.m_CameraPosition = 15;
            m_Vcam.InternalUpdateCameraState(Vector3.up, 0);
            yield return null;
            UnityEngine.Assertions.Assert.AreApproximatelyEqual(Vector3.Distance(m_Vcam.State.FinalPosition, new Vector3(11, 1, 1)), 0, 0.1f);

            m_Dolly.m_CameraPosition = 20;
            m_Vcam.InternalUpdateCameraState(Vector3.up, 0);
            yield return null;
            UnityEngine.Assertions.Assert.AreApproximatelyEqual(Vector3.Distance(m_Vcam.State.FinalPosition, new Vector3(7, 1, 0)), 0, 0.1f);

            m_Dolly.m_CameraPosition = 26;
            m_Vcam.InternalUpdateCameraState(Vector3.up, 0);
            yield return null;
            UnityEngine.Assertions.Assert.AreApproximatelyEqual(Vector3.Distance(m_Vcam.State.FinalPosition, new Vector3(7, 1, -6)), 0, 0.1f);
        }

        [UnityTest]
        public IEnumerator LinearInterpolationTestNormalized()
        {
            m_Dolly.m_PositionUnits = PathIndexUnit.Normalized;
            m_Dolly.m_CameraPosition = 0;
            m_Vcam.InternalUpdateCameraState(Vector3.up, 0);
            yield return null;
            UnityEngine.Assertions.Assert.AreApproximatelyEqual(Vector3.Distance(m_Vcam.State.FinalPosition, new Vector3(7, 1, -6)), 0, 0.1f);

            m_Dolly.m_CameraPosition = 0.125f;
            m_Vcam.InternalUpdateCameraState(Vector3.up, 0);
            yield return null;
            UnityEngine.Assertions.Assert.AreApproximatelyEqual(Vector3.Distance(m_Vcam.State.FinalPosition, new Vector3(10.25f, 1, -6)), 0, 0.1f);

            m_Dolly.m_CameraPosition = 0.5f;
            m_Vcam.InternalUpdateCameraState(Vector3.up, 0);
            yield return null;
            UnityEngine.Assertions.Assert.AreApproximatelyEqual(Vector3.Distance(m_Vcam.State.FinalPosition, new Vector3(13, 1, 1)), 0, 0.1f);

            m_Dolly.m_CameraPosition = 0.75f;
            m_Vcam.InternalUpdateCameraState(Vector3.up, 0);
            yield return null;
            UnityEngine.Assertions.Assert.AreApproximatelyEqual(Vector3.Distance(m_Vcam.State.FinalPosition, new Vector3(7, 1, 0.5f)), 0, 0.1f);

            m_Dolly.m_CameraPosition = 1;
            m_Vcam.InternalUpdateCameraState(Vector3.up, 0);
            yield return null;
            UnityEngine.Assertions.Assert.AreApproximatelyEqual(Vector3.Distance(m_Vcam.State.FinalPosition, new Vector3(7, 1, -6)), 0, 0.1f);
        }

        [UnityTest]
        public IEnumerator LinearInterpolationTestPathUnits()
        {
            m_Dolly.m_PositionUnits = PathIndexUnit.Knot;
            m_Dolly.m_CameraPosition = 0;
            m_Vcam.InternalUpdateCameraState(Vector3.up, 0);
            yield return null;
            UnityEngine.Assertions.Assert.AreApproximatelyEqual(Vector3.Distance(m_Vcam.State.FinalPosition, new Vector3(7, 1, -6)), 0, 0.1f);

            m_Dolly.m_CameraPosition = 0.125f;
            m_Vcam.InternalUpdateCameraState(Vector3.up, 0);
            yield return null;
            UnityEngine.Assertions.Assert.AreApproximatelyEqual(Vector3.Distance(m_Vcam.State.FinalPosition, new Vector3(7.75f, 1, -6)), 0, 0.1f);

            m_Dolly.m_CameraPosition = 0.5f;
            m_Vcam.InternalUpdateCameraState(Vector3.up, 0);
            yield return null;
            UnityEngine.Assertions.Assert.AreApproximatelyEqual(Vector3.Distance(m_Vcam.State.FinalPosition, new Vector3(10, 1, -6)), 0, 0.1f);

            m_Dolly.m_CameraPosition = 1;
            m_Vcam.InternalUpdateCameraState(Vector3.up, 0);
            yield return null;
            UnityEngine.Assertions.Assert.AreApproximatelyEqual(Vector3.Distance(m_Vcam.State.FinalPosition, new Vector3(13, 1, -6)), 0, 0.1f);

            m_Dolly.m_CameraPosition = 1.5f;
            m_Vcam.InternalUpdateCameraState(Vector3.up, 0);
            yield return null;
            UnityEngine.Assertions.Assert.AreApproximatelyEqual(Vector3.Distance(m_Vcam.State.FinalPosition, new Vector3(13, 1, -2.5f)), 0, 0.1f);

            m_Dolly.m_CameraPosition = 1.75f;
            m_Vcam.InternalUpdateCameraState(Vector3.up, 0);
            yield return null;
            UnityEngine.Assertions.Assert.AreApproximatelyEqual(Vector3.Distance(m_Vcam.State.FinalPosition, new Vector3(13, 1, -0.75f)), 0, 0.1f);

            m_Dolly.m_CameraPosition = 3;
            m_Vcam.InternalUpdateCameraState(Vector3.up, 0);
            yield return null;
            UnityEngine.Assertions.Assert.AreApproximatelyEqual(Vector3.Distance(m_Vcam.State.FinalPosition, new Vector3(7, 1, 1)), 0, 0.1f);

            m_Dolly.m_CameraPosition = 3.5f;
            m_Vcam.InternalUpdateCameraState(Vector3.up, 0);
            yield return null;
            UnityEngine.Assertions.Assert.AreApproximatelyEqual(Vector3.Distance(m_Vcam.State.FinalPosition, new Vector3(7, 1, -2.5f)), 0, 0.1f);
        }
    }
}
#endif
