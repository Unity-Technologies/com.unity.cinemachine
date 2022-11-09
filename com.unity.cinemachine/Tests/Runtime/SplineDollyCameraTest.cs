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
    public class SplineDollyCameraTest : CinemachineRuntimeFixtureBase
    {
        CmCamera m_CmCam;
        CinemachineSplineDolly m_Dolly;
        SplineContainer m_SplineContainer;

        [SetUp]
        public void Setup()
        {
            base.SetUp();
            
            m_SplineContainer = CreateGameObject("Dolly Track", typeof(SplineContainer)).GetComponent<SplineContainer>();
            m_SplineContainer.Spline = SplineFactory.CreateLinear(
                new List<float3> { new(7, 1, -6), new(13, 1, -6), new(13, 1, 1), new(7, 1, 1) }, true);
            
            m_CmCam = CreateGameObject("CM vcam", typeof(CmCamera)).GetComponent<CmCamera>();
            m_CmCam.Follow = CreatePrimitive(PrimitiveType.Cube).transform;
            m_Dolly = m_CmCam.gameObject.AddComponent<CinemachineSplineDolly>();
            m_Dolly.Spline = m_SplineContainer;
            m_Dolly.CameraUp = CinemachineSplineDolly.CameraUpMode.Default;
            m_Dolly.Damping.Enabled = false;
        }
        
        [TearDown]
        public override void TearDown()
        {
            base.TearDown();
        }

        [UnityTest]
        public IEnumerator LinearInterpolationTestDistance()
        {
            m_Dolly.PositionUnits = PathIndexUnit.Distance;
            m_Dolly.CameraPosition = 0;
            m_CmCam.InternalUpdateCameraState(Vector3.up, 0);
            yield return null;
            UnityEngine.Assertions.Assert.AreApproximatelyEqual(Vector3.Distance(m_CmCam.State.GetFinalPosition(), new Vector3(7, 1, -6)), 0, 0.1f);

            m_Dolly.CameraPosition = 0.5f;
            m_CmCam.InternalUpdateCameraState(Vector3.up, 0);
            yield return null;
            UnityEngine.Assertions.Assert.AreApproximatelyEqual(Vector3.Distance(m_CmCam.State.GetFinalPosition(), new Vector3(7.5f, 1, -6)), 0, 0.1f);

            m_Dolly.CameraPosition = 6;
            m_CmCam.InternalUpdateCameraState(Vector3.up, 0);
            yield return null;
            UnityEngine.Assertions.Assert.AreApproximatelyEqual(Vector3.Distance(m_CmCam.State.GetFinalPosition(), new Vector3(12.98846f, 1, -6)), 0, 0.1f);

            m_Dolly.CameraPosition = 10;
            m_CmCam.InternalUpdateCameraState(Vector3.up, 0);
            yield return null;
            UnityEngine.Assertions.Assert.AreApproximatelyEqual(Vector3.Distance(m_CmCam.State.GetFinalPosition(), new Vector3(13, 1, -2)), 0, 0.1f);

            m_Dolly.CameraPosition = 15;
            m_CmCam.InternalUpdateCameraState(Vector3.up, 0);
            yield return null;
            UnityEngine.Assertions.Assert.AreApproximatelyEqual(Vector3.Distance(m_CmCam.State.GetFinalPosition(), new Vector3(11, 1, 1)), 0, 0.1f);

            m_Dolly.CameraPosition = 20;
            m_CmCam.InternalUpdateCameraState(Vector3.up, 0);
            yield return null;
            UnityEngine.Assertions.Assert.AreApproximatelyEqual(Vector3.Distance(m_CmCam.State.GetFinalPosition(), new Vector3(7, 1, 0)), 0, 0.1f);

            m_Dolly.CameraPosition = 26;
            m_CmCam.InternalUpdateCameraState(Vector3.up, 0);
            yield return null;
            UnityEngine.Assertions.Assert.AreApproximatelyEqual(Vector3.Distance(m_CmCam.State.GetFinalPosition(), new Vector3(7, 1, -6)), 0, 0.1f);
        }

        [UnityTest]
        public IEnumerator LinearInterpolationTestNormalized()
        {
            m_Dolly.PositionUnits = PathIndexUnit.Normalized;
            m_Dolly.CameraPosition = 0;
            m_CmCam.InternalUpdateCameraState(Vector3.up, 0);
            yield return null;
            UnityEngine.Assertions.Assert.AreApproximatelyEqual(Vector3.Distance(m_CmCam.State.GetFinalPosition(), new Vector3(7, 1, -6)), 0, 0.1f);

            m_Dolly.CameraPosition = 0.125f;
            m_CmCam.InternalUpdateCameraState(Vector3.up, 0);
            yield return null;
            UnityEngine.Assertions.Assert.AreApproximatelyEqual(Vector3.Distance(m_CmCam.State.GetFinalPosition(), new Vector3(10.25f, 1, -6)), 0, 0.1f);

            m_Dolly.CameraPosition = 0.5f;
            m_CmCam.InternalUpdateCameraState(Vector3.up, 0);
            yield return null;
            UnityEngine.Assertions.Assert.AreApproximatelyEqual(Vector3.Distance(m_CmCam.State.GetFinalPosition(), new Vector3(13, 1, 1)), 0, 0.1f);

            m_Dolly.CameraPosition = 0.75f;
            m_CmCam.InternalUpdateCameraState(Vector3.up, 0);
            yield return null;
            UnityEngine.Assertions.Assert.AreApproximatelyEqual(Vector3.Distance(m_CmCam.State.GetFinalPosition(), new Vector3(7, 1, 0.5f)), 0, 0.1f);

            m_Dolly.CameraPosition = 1;
            m_CmCam.InternalUpdateCameraState(Vector3.up, 0);
            yield return null;
            UnityEngine.Assertions.Assert.AreApproximatelyEqual(Vector3.Distance(m_CmCam.State.GetFinalPosition(), new Vector3(7, 1, -6)), 0, 0.1f);
        }

        [UnityTest]
        public IEnumerator LinearInterpolationTestPathUnits()
        {
            m_Dolly.PositionUnits = PathIndexUnit.Knot;
            m_Dolly.CameraPosition = 0;
            m_CmCam.InternalUpdateCameraState(Vector3.up, 0);
            yield return null;
            UnityEngine.Assertions.Assert.AreApproximatelyEqual(Vector3.Distance(m_CmCam.State.GetFinalPosition(), new Vector3(7, 1, -6)), 0, 0.1f);

            m_Dolly.CameraPosition = 0.125f;
            m_CmCam.InternalUpdateCameraState(Vector3.up, 0);
            yield return null;
            UnityEngine.Assertions.Assert.AreApproximatelyEqual(Vector3.Distance(m_CmCam.State.GetFinalPosition(), new Vector3(7.75f, 1, -6)), 0, 0.1f);

            m_Dolly.CameraPosition = 0.5f;
            m_CmCam.InternalUpdateCameraState(Vector3.up, 0);
            yield return null;
            UnityEngine.Assertions.Assert.AreApproximatelyEqual(Vector3.Distance(m_CmCam.State.GetFinalPosition(), new Vector3(10, 1, -6)), 0, 0.1f);

            m_Dolly.CameraPosition = 1;
            m_CmCam.InternalUpdateCameraState(Vector3.up, 0);
            yield return null;
            UnityEngine.Assertions.Assert.AreApproximatelyEqual(Vector3.Distance(m_CmCam.State.GetFinalPosition(), new Vector3(13, 1, -6)), 0, 0.1f);

            m_Dolly.CameraPosition = 1.5f;
            m_CmCam.InternalUpdateCameraState(Vector3.up, 0);
            yield return null;
            UnityEngine.Assertions.Assert.AreApproximatelyEqual(Vector3.Distance(m_CmCam.State.GetFinalPosition(), new Vector3(13, 1, -2.5f)), 0, 0.1f);

            m_Dolly.CameraPosition = 1.75f;
            m_CmCam.InternalUpdateCameraState(Vector3.up, 0);
            yield return null;
            UnityEngine.Assertions.Assert.AreApproximatelyEqual(Vector3.Distance(m_CmCam.State.GetFinalPosition(), new Vector3(13, 1, -0.75f)), 0, 0.1f);

            m_Dolly.CameraPosition = 3;
            m_CmCam.InternalUpdateCameraState(Vector3.up, 0);
            yield return null;
            UnityEngine.Assertions.Assert.AreApproximatelyEqual(Vector3.Distance(m_CmCam.State.GetFinalPosition(), new Vector3(7, 1, 1)), 0, 0.1f);

            m_Dolly.CameraPosition = 3.5f;
            m_CmCam.InternalUpdateCameraState(Vector3.up, 0);
            yield return null;
            UnityEngine.Assertions.Assert.AreApproximatelyEqual(Vector3.Distance(m_CmCam.State.GetFinalPosition(), new Vector3(7, 1, -2.5f)), 0, 0.1f);
        }
    }
}
#endif
