using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.Splines;
using Unity.Mathematics;

namespace Unity.Cinemachine.Tests
{
    [TestFixture]
    public class SplineDollyCameraTest : CinemachineRuntimeFixtureBase
    {
        CinemachineCamera m_CmCam;
        CinemachineSplineDolly m_Dolly;
        SplineContainer m_SplineContainer;

        [SetUp]
        public override void SetUp()
        {
            base.SetUp();

            m_SplineContainer = CreateGameObject("Dolly Track", typeof(SplineContainer)).GetComponent<SplineContainer>();
            m_SplineContainer.Spline = SplineFactory.CreateLinear(
                new List<float3> { new(7, 1, -6), new(13, 1, -6), new(13, 1, 1), new(7, 1, 1) }, true);

            m_CmCam = CreateGameObject("CM vcam", typeof(CinemachineCamera)).GetComponent<CinemachineCamera>();
            m_CmCam.Follow = CreatePrimitive(PrimitiveType.Cube).transform;
            m_Dolly = m_CmCam.gameObject.AddComponent<CinemachineSplineDolly>();
            m_Dolly.Spline = m_SplineContainer;
            m_Dolly.CameraRotation = CinemachineSplineDolly.RotationMode.Default;
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

        [UnityTest]
        public IEnumerator PositionUnits_ChangesCameraPositionCorrectly_WhenPositionUnitsChanged()
        {
            //NOTE: Initial PositionUnits is Normalized, so anything other than that will be a change.
            //NOTE: For any of these the "GetFinalPosition()" output shouldn't change but "CameraPosition" should.

            //Test:
            //Normalized -> Distance
            //Distance   -> Knot
            //Knot       -> Normalized
            //
            //Normalized -> Knot
            //Knot       -> Distance
            //Distance   -> Normalized

            // Arrange
            m_Dolly.CameraPosition = 0.5f;

            // Act (Normalized -> Distance)
            m_Dolly.PositionUnits = PathIndexUnit.Distance;
            m_CmCam.InternalUpdateCameraState(Vector3.up, deltaTime: 0);
            yield return null;
            // Assert
            UnityEngine.Assertions.Assert.AreApproximatelyEqual(expected: 13, actual: m_Dolly.CameraPosition, tolerance: Mathf.Epsilon);
            UnityEngine.Assertions.Assert.AreApproximatelyEqual(expected: Vector3.Distance(m_CmCam.State.GetFinalPosition(), new Vector3(13, 1, 1)), actual: 0, tolerance: Mathf.Epsilon);

            // Act (Distance   -> Knot)
            m_Dolly.PositionUnits = PathIndexUnit.Knot;
            m_CmCam.InternalUpdateCameraState(Vector3.up, deltaTime: 0);
            yield return null;
            // Assert
            UnityEngine.Assertions.Assert.AreApproximatelyEqual(expected: 2, actual: m_Dolly.CameraPosition, tolerance: Mathf.Epsilon);
            UnityEngine.Assertions.Assert.AreApproximatelyEqual(expected: Vector3.Distance(m_CmCam.State.GetFinalPosition(), new Vector3(13, 1, 1)), actual: 0, tolerance: Mathf.Epsilon);

            // Act (Knot       -> Normalized)
            m_Dolly.PositionUnits = PathIndexUnit.Normalized;
            m_CmCam.InternalUpdateCameraState(Vector3.up, deltaTime: 0);
            yield return null;
            // Assert
            UnityEngine.Assertions.Assert.AreApproximatelyEqual(expected: 0.5f, actual: m_Dolly.CameraPosition, tolerance: Mathf.Epsilon);
            UnityEngine.Assertions.Assert.AreApproximatelyEqual(expected: Vector3.Distance(m_CmCam.State.GetFinalPosition(), new Vector3(13, 1, 1)), actual: 0, tolerance: Mathf.Epsilon);

            // Act (Normalized -> Knot)
            m_Dolly.PositionUnits = PathIndexUnit.Knot;
            m_CmCam.InternalUpdateCameraState(Vector3.up, deltaTime: 0);
            yield return null;
            // Assert
            UnityEngine.Assertions.Assert.AreApproximatelyEqual(expected: 2, actual: m_Dolly.CameraPosition, tolerance: Mathf.Epsilon);
            UnityEngine.Assertions.Assert.AreApproximatelyEqual(expected: Vector3.Distance(m_CmCam.State.GetFinalPosition(), new Vector3(13, 1, 1)), actual: 0, tolerance: Mathf.Epsilon);

            // Act (Knot       -> Distance)
            m_Dolly.PositionUnits = PathIndexUnit.Distance;
            m_CmCam.InternalUpdateCameraState(Vector3.up, deltaTime: 0);
            yield return null;
            // Assert
            UnityEngine.Assertions.Assert.AreApproximatelyEqual(expected: 13, actual: m_Dolly.CameraPosition, tolerance: Mathf.Epsilon);
            UnityEngine.Assertions.Assert.AreApproximatelyEqual(expected: Vector3.Distance(m_CmCam.State.GetFinalPosition(), new Vector3(13, 1, 1)), actual: 0, tolerance: Mathf.Epsilon);

            // Act (Distance   -> Normalized)
            m_Dolly.PositionUnits = PathIndexUnit.Normalized;
            m_CmCam.InternalUpdateCameraState(Vector3.up, deltaTime: 0);
            yield return null;
            // Assert
            UnityEngine.Assertions.Assert.AreApproximatelyEqual(expected: 0.5f, actual: m_Dolly.CameraPosition, tolerance: Mathf.Epsilon);
            UnityEngine.Assertions.Assert.AreApproximatelyEqual(expected: Vector3.Distance(m_CmCam.State.GetFinalPosition(), new Vector3(13, 1, 1)), actual: 0, tolerance: Mathf.Epsilon);
        }

        [UnityTest]
        public IEnumerator PositionUnits_DoesNotChangeCameraPosition_WhenPositionUnitsSame()
        {
            // Arrange
            m_Dolly.PositionUnits = PathIndexUnit.Normalized;
            m_Dolly.CameraPosition = 0.5f;

            m_CmCam.InternalUpdateCameraState(Vector3.up, deltaTime: 0);
            yield return null;
            Vector3 initialPosition = m_CmCam.State.GetFinalPosition();

            // Act (Normalized -> Normalized)
            m_Dolly.PositionUnits = PathIndexUnit.Normalized;
            m_CmCam.InternalUpdateCameraState(Vector3.up, deltaTime: 0);
            yield return null;
            // Assert
            UnityEngine.Assertions.Assert.AreApproximatelyEqual(expected: 0.5f, actual: m_Dolly.CameraPosition, tolerance: Mathf.Epsilon);
            UnityEngine.Assertions.Assert.AreApproximatelyEqual(expected: Vector3.Distance(m_CmCam.State.GetFinalPosition(), initialPosition), actual: 0, tolerance: Mathf.Epsilon);


            // Arrange
            m_Dolly.PositionUnits  = PathIndexUnit.Distance;
            m_Dolly.CameraPosition = 13f;

            m_CmCam.InternalUpdateCameraState(Vector3.up, deltaTime: 0);
            yield return null;
            initialPosition = m_CmCam.State.GetFinalPosition();

            // Act (Distance -> Distance)
            m_Dolly.PositionUnits = PathIndexUnit.Distance;
            m_CmCam.InternalUpdateCameraState(Vector3.up, deltaTime: 0);
            yield return null;
            // Assert
            UnityEngine.Assertions.Assert.AreApproximatelyEqual(expected: 13f, actual: m_Dolly.CameraPosition, tolerance: Mathf.Epsilon);
            UnityEngine.Assertions.Assert.AreApproximatelyEqual(expected: Vector3.Distance(m_CmCam.State.GetFinalPosition(), initialPosition), actual: 0, tolerance: Mathf.Epsilon);


            // Arrange
            m_Dolly.PositionUnits = PathIndexUnit.Knot;
            m_Dolly.CameraPosition = 2f;

            m_CmCam.InternalUpdateCameraState(Vector3.up, deltaTime: 0);
            yield return null;
            initialPosition = m_CmCam.State.GetFinalPosition();

            // Act (Knot -> Knot)
            m_Dolly.PositionUnits = PathIndexUnit.Knot;
            m_CmCam.InternalUpdateCameraState(Vector3.up, deltaTime: 0);
            yield return null;
            // Assert
            UnityEngine.Assertions.Assert.AreApproximatelyEqual(expected: 2f, actual: m_Dolly.CameraPosition, tolerance: Mathf.Epsilon);
            UnityEngine.Assertions.Assert.AreApproximatelyEqual(expected: Vector3.Distance(m_CmCam.State.GetFinalPosition(), initialPosition), actual: 0, tolerance: Mathf.Epsilon);
        }
    }
}
