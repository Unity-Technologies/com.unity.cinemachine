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
        const float m_WaitTime = 0.5f; // this is roughly the time it takes to damp back

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

            var obstacle = CreatePrimitive(PrimitiveType.Cube);
            obstacle.transform.SetPositionAndRotation(originalCamPosition, Quaternion.identity); // place obstacle so that camera needs to move
            yield return WaitForOnePhysicsFrame(); // ensure that moving the collider (obstacle) takes effect
            yield return UpdateCinemachine();
            // Camera snapped in front of the box at position -4.5 (imprecision is due to slush in collider algorithm)
            var camPos = m_Vcam.State.GetFinalPosition();
            Assert.That(originalCamPosition, Is.Not.EqualTo(camPos).Using(m_Vector3EqualityComparer));
            Assert.That(new Vector3(0, 0, -4.49900007f), Is.EqualTo(camPos).Using(m_Vector3EqualityComparer));

            yield return PhysicsDestroy(obstacle);
            yield return WaitForSeconds(m_Collider.AvoidObstacles.SmoothingTime);
            yield return UpdateCinemachine();
            Assert.That(originalCamPosition, Is.EqualTo(m_Vcam.State.GetFinalPosition()).Using(m_Vector3EqualityComparer));
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
            var obstacle = CreatePrimitive(PrimitiveType.Cube);
            obstacle.transform.SetPositionAndRotation(originalCamPosition, Quaternion.identity); // place obstacle so that camera needs to move
            yield return WaitForOnePhysicsFrame(); // ensure that moving the collider (obstacle) takes effect

            yield return WaitForSecondsWhileTestingDamping(m_WaitTime); // damped correction in front of obstacle
            
            // Remove obstacle and check if cameras has snapped back to its original location
            yield return PhysicsDestroy(obstacle);
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
            
            Assert.That(originalCamPosition, Is.EqualTo(m_Vcam.State.GetFinalPosition()).Using(m_Vector3EqualityComparer));
            var obstacle = CreatePrimitive(PrimitiveType.Cube);
            obstacle.transform.SetPositionAndRotation(originalCamPosition, Quaternion.identity); // place obstacle so that camera needs to move
            yield return WaitForOnePhysicsFrame(); // ensure that moving the collider (obstacle) takes effect
            yield return UpdateCinemachine();
            
            // camera moved check
            Assert.That(originalCamPosition, Is.Not.EqualTo(m_Vcam.State.GetFinalPosition()).Using(m_Vector3EqualityComparer));
            var obstructedPosition = m_Vcam.State.GetFinalPosition();
            Assert.That(originalCamPosition, !Is.EqualTo(obstructedPosition).Using(m_Vector3EqualityComparer));
            // wait another frame to avoid snap - we need to have a previous damp time to avoid snap
            yield return UpdateCinemachine();
            
            Assert.That(obstructedPosition, Is.EqualTo(m_Vcam.State.GetFinalPosition()).Using(m_Vector3EqualityComparer));
            yield return PhysicsDestroy(obstacle);
            
            yield return WaitForSecondsWhileTestingDamping(m_WaitTime); // damped return to original pos
            
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

        static IEnumerator PhysicsDestroy(GameObject go)
        {
            UnityEngine.Object.Destroy(go);
            yield return WaitForOnePhysicsFrame();
        }
    }
}
#endif
