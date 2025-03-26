using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Splines;
using UnityEngine.TestTools;

namespace Unity.Cinemachine.Tests
{
    public class SplineDollyLookatTarget : CinemachineRuntimeFixtureBase
    {
        CinemachineCamera m_CmCam;
        CinemachineSplineDolly m_Dolly;
        SplineContainer m_SplineContainer;
        CinemachineSplineDollyLookAtTargets m_SplineLookatTargets;
        Vector3 m_FirstLookAtPoint = new Vector3(20, 10, 0);
        Vector3 m_SecondLookAtPoint = new Vector3(-10, 0, -10);

        [SetUp]
        public override void SetUp()
        {
            base.SetUp();

            m_SplineContainer =
                CreateGameObject("Dolly Track", typeof(SplineContainer)).GetComponent<SplineContainer>();
            m_SplineContainer.Spline = SplineFactory.CreateLinear(
                new List<float3> { new(7, 1, -6), new(13, 1, -6), new(13, 1, 1), new(7, 1, 1) }, true);

            m_CmCam = CreateGameObject("CM vcam", typeof(CinemachineCamera)).GetComponent<CinemachineCamera>();
            m_CmCam.Follow = CreatePrimitive(PrimitiveType.Cube).transform;
            m_Dolly = m_CmCam.gameObject.AddComponent<CinemachineSplineDolly>();
            m_SplineLookatTargets = m_CmCam.gameObject.AddComponent<CinemachineSplineDollyLookAtTargets>();
            m_SplineLookatTargets.Targets.PathIndexUnit = PathIndexUnit.Normalized;
            m_SplineLookatTargets.Targets.Add(0.5f, new CinemachineSplineDollyLookAtTargets.Item()
            {
                LookAt = null,
                Offset = new Vector3(0, 0, 0),
                Easing = 1,
                WorldLookAt = m_FirstLookAtPoint
            });

            m_SplineLookatTargets.Targets.Add(0f, new CinemachineSplineDollyLookAtTargets.Item()
            {
                LookAt = null,
                Offset = new Vector3(0, 0, 0),
                Easing = 1,
                WorldLookAt = m_SecondLookAtPoint
            });
            m_Dolly.Spline = m_SplineContainer;
            m_Dolly.CameraRotation = CinemachineSplineDolly.RotationMode.Default;
            m_Dolly.Damping.Enabled = false;
        }

        [UnityTest]
        public IEnumerator LookatTargetIsAppliedAndInterpolatedSmoothly()
        {
            m_Dolly.PositionUnits = PathIndexUnit.Normalized;
            m_Dolly.CameraPosition = 0;
            m_CmCam.InternalUpdateCameraState(Vector3.up, 0);
            yield return null;
            UnityEngine.Assertions.Assert.AreApproximatelyEqual(
                Vector3.Dot((m_SecondLookAtPoint - m_CmCam.State.GetFinalPosition()).normalized, m_CmCam.transform.forward), 1, 0.0001f);

            m_Dolly.CameraPosition = 0.25f; // Test between two points
            m_CmCam.InternalUpdateCameraState(Vector3.up, 0);
            yield return null;
            UnityEngine.Assertions.Assert.AreApproximatelyEqual(
                Vector3.Dot((Vector3.Lerp(m_FirstLookAtPoint, m_SecondLookAtPoint, 0.5f) - m_CmCam.State.GetFinalPosition()).normalized, m_CmCam.transform.forward), 1, 0.0001f);

            m_Dolly.CameraPosition = 0.5f;
            m_CmCam.InternalUpdateCameraState(Vector3.up, 0);
            yield return null;
            UnityEngine.Assertions.Assert.AreApproximatelyEqual(
                Vector3.Dot((m_FirstLookAtPoint - m_CmCam.State.GetFinalPosition()).normalized, m_CmCam.transform.forward), 1, 0.0001f);

        }

        [TearDown]
        public override void TearDown()
        {
            base.TearDown();
        }
    }
}
