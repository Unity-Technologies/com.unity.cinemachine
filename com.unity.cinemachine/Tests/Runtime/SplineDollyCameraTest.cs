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
        CmCamera m_CmCam;
        CinemachineSplineDolly m_Dolly;
        SplineContainer m_SplineContainer;

        [SetUp]
        public void Setup()
        {
            m_SplineContainer = CreateGameObject("Dolly Track", typeof(SplineContainer)).GetComponent<SplineContainer>();
            m_SplineContainer.Spline = SplineFactory.CreateLinear(
                new List<float3> { new float3(7, 1, -6), new float3(13, 1, -6), new float3(13, 1, 1), new float3(7, 1, 1) },
                true);
            
            m_CmCam = CreateGameObject("CM vcam", typeof(CmCamera)).GetComponent<CmCamera>();
            m_CmCam.Follow = CreatePrimitive(PrimitiveType.Cube).transform;
            m_Dolly = m_CmCam.gameObject.AddComponent<CinemachineSplineDolly>();
            m_Dolly.AddSplineContainer(m_SplineContainer);
            m_Dolly.cameraUp = CinemachineSplineDolly.CameraUpMode.Default;
            m_Dolly.dampingEnabled = false;
        }

        [UnityTest]
        public IEnumerator LinearInterpolationTestDistance()
        {
            m_Dolly.positionUnits = PathIndexUnit.Distance;
            m_Dolly.cameraPosition = 0;
            m_CmCam.InternalUpdateCameraState(Vector3.up, 0);
            yield return null;
            UnityEngine.Assertions.Assert.AreApproximatelyEqual(Vector3.Distance(m_CmCam.State.FinalPosition, new Vector3(7, 1, -6)), 0, 0.1f);

            m_Dolly.cameraPosition = 0.5f;
            m_CmCam.InternalUpdateCameraState(Vector3.up, 0);
            yield return null;
            UnityEngine.Assertions.Assert.AreApproximatelyEqual(Vector3.Distance(m_CmCam.State.FinalPosition, new Vector3(7.5f, 1, -6)), 0, 0.1f);

            m_Dolly.cameraPosition = 6;
            m_CmCam.InternalUpdateCameraState(Vector3.up, 0);
            yield return null;
            UnityEngine.Assertions.Assert.AreApproximatelyEqual(Vector3.Distance(m_CmCam.State.FinalPosition, new Vector3(12.98846f, 1, -6)), 0, 0.1f);

            m_Dolly.cameraPosition = 10;
            m_CmCam.InternalUpdateCameraState(Vector3.up, 0);
            yield return null;
            UnityEngine.Assertions.Assert.AreApproximatelyEqual(Vector3.Distance(m_CmCam.State.FinalPosition, new Vector3(13, 1, -2)), 0, 0.1f);

            m_Dolly.cameraPosition = 15;
            m_CmCam.InternalUpdateCameraState(Vector3.up, 0);
            yield return null;
            UnityEngine.Assertions.Assert.AreApproximatelyEqual(Vector3.Distance(m_CmCam.State.FinalPosition, new Vector3(11, 1, 1)), 0, 0.1f);

            m_Dolly.cameraPosition = 20;
            m_CmCam.InternalUpdateCameraState(Vector3.up, 0);
            yield return null;
            UnityEngine.Assertions.Assert.AreApproximatelyEqual(Vector3.Distance(m_CmCam.State.FinalPosition, new Vector3(7, 1, 0)), 0, 0.1f);

            m_Dolly.cameraPosition = 26;
            m_CmCam.InternalUpdateCameraState(Vector3.up, 0);
            yield return null;
            UnityEngine.Assertions.Assert.AreApproximatelyEqual(Vector3.Distance(m_CmCam.State.FinalPosition, new Vector3(7, 1, -6)), 0, 0.1f);
        }

        [UnityTest]
        public IEnumerator LinearInterpolationTestNormalized()
        {
            m_Dolly.positionUnits = PathIndexUnit.Normalized;
            m_Dolly.cameraPosition = 0;
            m_CmCam.InternalUpdateCameraState(Vector3.up, 0);
            yield return null;
            UnityEngine.Assertions.Assert.AreApproximatelyEqual(Vector3.Distance(m_CmCam.State.FinalPosition, new Vector3(7, 1, -6)), 0, 0.1f);

            m_Dolly.cameraPosition = 0.125f;
            m_CmCam.InternalUpdateCameraState(Vector3.up, 0);
            yield return null;
            UnityEngine.Assertions.Assert.AreApproximatelyEqual(Vector3.Distance(m_CmCam.State.FinalPosition, new Vector3(10.25f, 1, -6)), 0, 0.1f);

            m_Dolly.cameraPosition = 0.5f;
            m_CmCam.InternalUpdateCameraState(Vector3.up, 0);
            yield return null;
            UnityEngine.Assertions.Assert.AreApproximatelyEqual(Vector3.Distance(m_CmCam.State.FinalPosition, new Vector3(13, 1, 1)), 0, 0.1f);

            m_Dolly.cameraPosition = 0.75f;
            m_CmCam.InternalUpdateCameraState(Vector3.up, 0);
            yield return null;
            UnityEngine.Assertions.Assert.AreApproximatelyEqual(Vector3.Distance(m_CmCam.State.FinalPosition, new Vector3(7, 1, 0.5f)), 0, 0.1f);

            m_Dolly.cameraPosition = 1;
            m_CmCam.InternalUpdateCameraState(Vector3.up, 0);
            yield return null;
            UnityEngine.Assertions.Assert.AreApproximatelyEqual(Vector3.Distance(m_CmCam.State.FinalPosition, new Vector3(7, 1, -6)), 0, 0.1f);
        }

        [UnityTest]
        public IEnumerator LinearInterpolationTestPathUnits()
        {
            m_Dolly.positionUnits = PathIndexUnit.Knot;
            m_Dolly.cameraPosition = 0;
            m_CmCam.InternalUpdateCameraState(Vector3.up, 0);
            yield return null;
            UnityEngine.Assertions.Assert.AreApproximatelyEqual(Vector3.Distance(m_CmCam.State.FinalPosition, new Vector3(7, 1, -6)), 0, 0.1f);

            m_Dolly.cameraPosition = 0.125f;
            m_CmCam.InternalUpdateCameraState(Vector3.up, 0);
            yield return null;
            UnityEngine.Assertions.Assert.AreApproximatelyEqual(Vector3.Distance(m_CmCam.State.FinalPosition, new Vector3(7.75f, 1, -6)), 0, 0.1f);

            m_Dolly.cameraPosition = 0.5f;
            m_CmCam.InternalUpdateCameraState(Vector3.up, 0);
            yield return null;
            UnityEngine.Assertions.Assert.AreApproximatelyEqual(Vector3.Distance(m_CmCam.State.FinalPosition, new Vector3(10, 1, -6)), 0, 0.1f);

            m_Dolly.cameraPosition = 1;
            m_CmCam.InternalUpdateCameraState(Vector3.up, 0);
            yield return null;
            UnityEngine.Assertions.Assert.AreApproximatelyEqual(Vector3.Distance(m_CmCam.State.FinalPosition, new Vector3(13, 1, -6)), 0, 0.1f);

            m_Dolly.cameraPosition = 1.5f;
            m_CmCam.InternalUpdateCameraState(Vector3.up, 0);
            yield return null;
            UnityEngine.Assertions.Assert.AreApproximatelyEqual(Vector3.Distance(m_CmCam.State.FinalPosition, new Vector3(13, 1, -2.5f)), 0, 0.1f);

            m_Dolly.cameraPosition = 1.75f;
            m_CmCam.InternalUpdateCameraState(Vector3.up, 0);
            yield return null;
            UnityEngine.Assertions.Assert.AreApproximatelyEqual(Vector3.Distance(m_CmCam.State.FinalPosition, new Vector3(13, 1, -0.75f)), 0, 0.1f);

            m_Dolly.cameraPosition = 3;
            m_CmCam.InternalUpdateCameraState(Vector3.up, 0);
            yield return null;
            UnityEngine.Assertions.Assert.AreApproximatelyEqual(Vector3.Distance(m_CmCam.State.FinalPosition, new Vector3(7, 1, 1)), 0, 0.1f);

            m_Dolly.cameraPosition = 3.5f;
            m_CmCam.InternalUpdateCameraState(Vector3.up, 0);
            yield return null;
            UnityEngine.Assertions.Assert.AreApproximatelyEqual(Vector3.Distance(m_CmCam.State.FinalPosition, new Vector3(7, 1, -2.5f)), 0, 0.1f);
        }
    }
}
#endif
