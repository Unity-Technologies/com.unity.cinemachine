#if CINEMACHINE_PHYSICS
using System;
using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Unity.Cinemachine.Tests
{
    [TestFixture]
    public class CmDeoccluderTests : CinemachineRuntimeTimeInvariantFixtureBase
    {
        CinemachineCamera m_Vcam;
        CinemachineDeoccluder m_Deoccluder;
        GameObject m_FollowObject;

        [SetUp]
        public override void SetUp()
        {
            base.SetUp();
            
            m_Vcam = CreateGameObject("CM Vcam", typeof(CinemachineCamera), typeof(CinemachineDeoccluder)).GetComponent<CinemachineCamera>();
            m_Vcam.Priority = 100;
            m_Vcam.Follow = CreateGameObject("Follow Object").transform;
            var positionComposer = m_Vcam.gameObject.AddComponent<CinemachinePositionComposer>();
            positionComposer.CameraDistance = 5f;
            m_Deoccluder = m_Vcam.GetComponent<CinemachineDeoccluder>();
            m_Deoccluder.CollideAgainst = 1;
            m_Deoccluder.AvoidObstacles.Strategy = CinemachineDeoccluder.ObstacleAvoidance.ResolutionStrategy.PullCameraForward;
            m_Deoccluder.AvoidObstacles.Enabled = true;
            m_Deoccluder.AvoidObstacles.SmoothingTime = 0;
            m_Deoccluder.AvoidObstacles.Damping = 0;
            m_Deoccluder.AvoidObstacles.DampingWhenOccluded = 0;
            m_Vcam.AddExtension(m_Deoccluder);
        }

        [TearDown]
        public override void TearDown()
        {
            base.TearDown();
        }

        [UnityTest]
        public IEnumerator CheckSmoothingTime()
        {
            m_Deoccluder.AvoidObstacles.SmoothingTime = 1;
            m_Deoccluder.AvoidObstacles.Damping = 0;
            m_Deoccluder.AvoidObstacles.DampingWhenOccluded = 0;
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
            yield return WaitForSmoothingTimeWhileCheckingMovement();
            Assert.That(originalCamPosition, Is.EqualTo(m_Vcam.State.GetFinalPosition()).Using(m_Vector3EqualityComparer));

            // local function
            IEnumerator WaitForSmoothingTimeWhileCheckingMovement()
            {
                var startTime = CinemachineCore.CurrentTimeOverride;
                var initialPosition = m_Vcam.State.GetFinalPosition();
                while (CinemachineCore.CurrentTimeOverride - startTime <= m_Deoccluder.AvoidObstacles.SmoothingTime)
                {
                    Assert.That(m_Vcam.State.GetFinalPosition(), Is.EqualTo(initialPosition));
                    yield return UpdateCinemachine();
                }
            }
        }
        
        [UnityTest]
        public IEnumerator CheckDampingWhenOccluded()
        {
            m_Deoccluder.AvoidObstacles.SmoothingTime = 0;
            m_Deoccluder.AvoidObstacles.Damping = 0;
            m_Deoccluder.AvoidObstacles.DampingWhenOccluded = 1;
            var originalCamPosition = m_Vcam.State.GetFinalPosition();
            yield return UpdateCinemachine();
            Assert.That(originalCamPosition, Is.EqualTo(m_Vcam.State.GetFinalPosition()).Using(m_Vector3EqualityComparer));
            
            // 1. Place an obstacle where the camera is currently to force the camera to move.
            var obstacle = CreatePrimitive(PrimitiveType.Cube);
            obstacle.transform.SetPositionAndRotation(originalCamPosition, Quaternion.identity);
            yield return WaitForOnePhysicsFrame(); // ensure that moving the collider (obstacle) takes effect

            // 2. Wait for correction damping as the camera is positioned in front of the obstacle and test damping.
            yield return WaitForDamping();
            
            // 3. Remove obstacle 
            yield return PhysicsDestroy(obstacle);
            
            // 4. Check if cameras has snapped back to its original location
            yield return UpdateCinemachine();
            Assert.That(originalCamPosition, Is.EqualTo(m_Vcam.State.GetFinalPosition()).Using(m_Vector3EqualityComparer));
        }
        
        [UnityTest]
        public IEnumerator CheckDamping()
        {
            m_Deoccluder.AvoidObstacles.SmoothingTime = 0;
            m_Deoccluder.AvoidObstacles.Damping = 1;
            m_Deoccluder.AvoidObstacles.DampingWhenOccluded = 0;
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
            
            // 5. Wait for correction damping as the camera is returning to the starting position and test damping.
            yield return WaitForDamping();

            // 6. Check that camera is back to original position
            yield return UpdateCinemachine();
            Assert.That(originalCamPosition, Is.EqualTo(m_Vcam.State.GetFinalPosition()).Using(m_Vector3EqualityComparer));
        }
        
        IEnumerator WaitForDamping()
        {
            var previousDeltaMagnitude = -1f;
            var previousDelta = Vector3.zero;
            
            while (true)
            {
                var startPosition = m_Vcam.State.GetFinalPosition();
                yield return UpdateCinemachine();
                var delta = (m_Vcam.State.GetFinalPosition() - startPosition);
                var deltaMagnitude = delta.magnitude;
                if (deltaMagnitude < UnityVectorExtensions.Epsilon)
                    break; // stop when delta is small enough
                
                if (previousDeltaMagnitude >= 0)
                {
                    Assert.That(delta.normalized, 
                        Is.EqualTo(previousDelta.normalized).Using(m_Vector3EqualityComparer)); // monotonic 
                    Assert.That(deltaMagnitude, Is.LessThan(previousDeltaMagnitude)); // strictly decreasing
                }
                
                previousDeltaMagnitude = deltaMagnitude;
                previousDelta = delta;
            } 
        }
    }
}
#endif
