#if CINEMACHINE_PHYSICS
using System;
using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Cinemachine;

namespace Tests.Runtime
{
    [TestFixture]
    public class CmColliderTests : CinemachineRuntimeTimeInvariantFixtureBase
    {
        CmCamera m_Vcam;
        CinemachineDeoccluder m_Collider;
        GameObject m_FollowObject;
        const float k_WaitTime = 0.5f; // this is roughly the time it takes to damp back

        [SetUp]
        public override void SetUp()
        {
            base.SetUp();
            
            m_Vcam = CreateGameObject("CM Vcam", typeof(CmCamera), typeof(CinemachineDeoccluder)).GetComponent<CmCamera>();
            m_Vcam.Priority = 100;
            m_Vcam.Follow = CreateGameObject("Follow Object").transform;
            var positionComposer = m_Vcam.gameObject.AddComponent<CinemachinePositionComposer>();
            positionComposer.CameraDistance = 5f;
            m_Collider = m_Vcam.GetComponent<CinemachineDeoccluder>();
            m_Collider.CollideAgainst = 1;
            m_Collider.AvoidObstacles.Strategy = CinemachineDeoccluder.ObstacleAvoidance.ResolutionStrategy.PullCameraForward;
            m_Collider.AvoidObstacles.Enabled = true;
            m_Collider.AvoidObstacles.SmoothingTime = 0;
            m_Collider.AvoidObstacles.Damping = 0;
            m_Collider.AvoidObstacles.DampingWhenOccluded = 0;
            m_Vcam.AddExtension(m_Collider);
        }

        [TearDown]
        public override void TearDown()
        {
            base.TearDown();
        }

        [UnityTest]
        public IEnumerator CheckSmoothingTime()
        {
            m_Collider.AvoidObstacles.SmoothingTime = 1;
            m_Collider.AvoidObstacles.Damping = 0;
            m_Collider.AvoidObstacles.DampingWhenOccluded = 0;
            var originalCamPosition = m_Vcam.State.GetFinalPosition();
            yield return UpdateCinemachine();
            Assert.That(originalCamPosition, Is.EqualTo(m_Vcam.State.GetFinalPosition()).Using(m_Vector3EqualityComparer));

            // 1. Place an obstacle where the camera is currently to force the camera to move
            var obstacle = CreatePrimitive(PrimitiveType.Cube);
            obstacle.transform.SetPositionAndRotation(originalCamPosition, Quaternion.identity);
            yield return WaitForOnePhysicsFrame(); // ensure that moving the collider (obstacle) takes effect
            
            // 2. See if it has snapped (due to obstacle, no damping) to the appropriate location.
            yield return UpdateCinemachine();
            // Camera snapped in front of the box at position -4.5 (imprecision is due to slush in collider algorithm)
            Assert.That(new Vector3(0, 0, -4.49900007f), Is.EqualTo(m_Vcam.State.GetFinalPosition()).Using(m_Vector3EqualityComparer));

            // 3. Remove obstacle
            yield return PhysicsDestroy(obstacle);
            
            // 4. Wait and see that smoothing stops any correction applied for its duration
            yield return WaitForSecondsWhileCheckingMovement(m_Collider.AvoidObstacles.SmoothingTime);
            Assert.That(originalCamPosition, Is.EqualTo(m_Vcam.State.GetFinalPosition()).Using(m_Vector3EqualityComparer));

            // local function
            IEnumerator WaitForSecondsWhileCheckingMovement(float t)
            {
                var startTime = CinemachineCore.CurrentTimeOverride;
                var initialPosition = m_Vcam.State.GetFinalPosition();
                while (CinemachineCore.CurrentTimeOverride - startTime <= t)
                {
                    Assert.That(m_Vcam.State.GetFinalPosition(), Is.EqualTo(initialPosition));
                    yield return UpdateCinemachine();
                }
            }
        }
        
        [UnityTest]
        public IEnumerator CheckDampingWhenOccluded()
        {
            m_Collider.AvoidObstacles.SmoothingTime = 0;
            m_Collider.AvoidObstacles.Damping = 0;
            m_Collider.AvoidObstacles.DampingWhenOccluded = 1;
            var originalCamPosition = m_Vcam.State.GetFinalPosition();
            yield return UpdateCinemachine();
            Assert.That(originalCamPosition, Is.EqualTo(m_Vcam.State.GetFinalPosition()).Using(m_Vector3EqualityComparer));
            
            // 1. Place an obstacle where the camera is currently to force the camera to move.
            var obstacle = CreatePrimitive(PrimitiveType.Cube);
            obstacle.transform.SetPositionAndRotation(originalCamPosition, Quaternion.identity);
            yield return WaitForOnePhysicsFrame(); // ensure that moving the collider (obstacle) takes effect

            // 2. Wait roughly as much time as it needs to complete the correction with damping and test that damping is applied.
            yield return WaitForSecondsWhileTestingDamping(k_WaitTime);
            
            // 3. Remove obstacle 
            yield return PhysicsDestroy(obstacle);
            
            // 4. Check if cameras has snapped back to its original location
            yield return UpdateCinemachine();
            Assert.That(originalCamPosition, Is.EqualTo(m_Vcam.State.GetFinalPosition()).Using(m_Vector3EqualityComparer));
        }
        
        [UnityTest]
        public IEnumerator CheckDamping()
        {
            m_Collider.AvoidObstacles.SmoothingTime = 0;
            m_Collider.AvoidObstacles.Damping = 1;
            m_Collider.AvoidObstacles.DampingWhenOccluded = 0;
            var originalCamPosition = m_Vcam.State.GetFinalPosition();
            yield return UpdateCinemachine();
            
            // 1. Place an obstacle where the camera is currently to force the camera to move.
            Assert.That(originalCamPosition, Is.EqualTo(m_Vcam.State.GetFinalPosition()).Using(m_Vector3EqualityComparer));
            var obstacle = CreatePrimitive(PrimitiveType.Cube);
            obstacle.transform.SetPositionAndRotation(originalCamPosition, Quaternion.identity);
            yield return WaitForOnePhysicsFrame(); // ensure that moving the collider (obstacle) takes effect
            
            // 2. Check that camera moved (no damping, so it snapped).
            yield return UpdateCinemachine();
            Assert.That(originalCamPosition, Is.Not.EqualTo(m_Vcam.State.GetFinalPosition()).Using(m_Vector3EqualityComparer));
            var obstructedPosition = m_Vcam.State.GetFinalPosition();
            
            // 3. Check that camera still has not moved 
            yield return UpdateCinemachine(); // wait another frame to avoid snap - we need to have a previous damp time to avoid snap
            Assert.That(obstructedPosition, Is.EqualTo(m_Vcam.State.GetFinalPosition()).Using(m_Vector3EqualityComparer));
            
            // 4. Remove obstacle
            yield return PhysicsDestroy(obstacle);
            
            // 5. Wait roughly as much time as it needs to return to the starting position with damping and test that damping is applied.
            yield return WaitForSecondsWhileTestingDamping(k_WaitTime);

            // 6. Check that camera is back to original position
            yield return UpdateCinemachine();
            Assert.That(originalCamPosition, Is.EqualTo(m_Vcam.State.GetFinalPosition()).Using(m_Vector3EqualityComparer));
        }
        
        IEnumerator WaitForSecondsWhileTestingDamping(float t)
        {
            var previousDelta = -1f;
            var startTime = CinemachineCore.CurrentTimeOverride;
            while (CinemachineCore.CurrentTimeOverride - startTime <= t)
            {
                var startPosition = m_Vcam.State.GetFinalPosition();
                yield return UpdateCinemachine();
                var delta = (m_Vcam.State.GetFinalPosition() - startPosition).sqrMagnitude;
                Assert.That(delta, Is.Not.Negative);
                if (previousDelta >= 0)
                    Assert.That(delta, Is.LessThanOrEqualTo(previousDelta));

                previousDelta = delta;
            }
        }
    }
}
#endif
