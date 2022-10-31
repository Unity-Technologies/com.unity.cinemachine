using System;
using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Cinemachine;
using UnityEngine.TestTools.Utils;

#if CINEMACHINE_PHYSICS
namespace Tests.Runtime
{
    public class CmColliderTests : CinemachineRuntimeTimeInvariantFixtureBase
    {
        CmCamera m_Vcam;
        CinemachineDeoccluder m_Collider;
        GameObject m_FollowObject;

        [SetUp]
        public override void SetUp()
        {
            base.SetUp();
            
            m_Vcam = CreateGameObject("CM Vcam", typeof(CmCamera), typeof(CinemachineDeoccluder)).GetComponent<CmCamera>();
            m_Vcam.Priority = 100;
            m_Vcam.Follow = CreateGameObject("Follow Object").transform;
            var framingTransposer = m_Vcam.gameObject.AddComponent<CinemachinePositionComposer>();
            framingTransposer.CameraDistance = 5f;
            m_Collider = m_Vcam.GetComponent<CinemachineDeoccluder>();
            m_Collider.Strategy = CinemachineDeoccluder.ResolutionStrategy.PullCameraForward;
            m_Collider.CollideAgainst = 1;
            m_Collider.AvoidObstacles = true;
            m_Collider.SmoothingTime = 0;
            m_Collider.Damping = 0;
            m_Collider.DampingWhenOccluded = 0;
            m_Vcam.AddExtension(m_Collider);
        }

        [UnityTest]
        public IEnumerator CheckSmoothingTime()
        {
            m_Collider.SmoothingTime = 1;
            m_Collider.Damping = 0;
            m_Collider.DampingWhenOccluded = 0;
            var originalCamPosition = m_Vcam.State.GetFinalPosition();

            yield return null;
            UpdateCinemachine();
            Assert.That(originalCamPosition, Is.EqualTo(m_Vcam.State.GetFinalPosition()).Using(Vector3EqualityComparer.Instance));

            var obstacle = GameObject.CreatePrimitive(PrimitiveType.Cube);
            obstacle.transform.position = originalCamPosition; // place obstacle so that camera needs to move
            
            yield return WaitForOnePhysicsFrame(); // ensure that moving the collider (obstacle) takes affect
            UpdateCinemachine();
            
            // Camera moved check
            Assert.That(originalCamPosition, Is.Not.EqualTo(m_Vcam.State.GetFinalPosition()).Using(Vector3EqualityComparer.Instance));
            Assert.That(new Vector3(0, 0, -4.4f), Is.EqualTo(m_Vcam.State.GetFinalPosition()).Using(Vector3EqualityComparer.Instance));
            
            UnityEngine.Object.Destroy(obstacle);

            yield return WaitForOnePhysicsFrame(); // ensure that the obstacle's collider is removed
            yield return WaitForSeconds(m_Collider.SmoothingTime);
            UpdateCinemachine();
            
            Assert.That(originalCamPosition, Is.EqualTo(m_Vcam.State.GetFinalPosition()).Using(Vector3EqualityComparer.Instance));
        }

        [UnityTest]
        public IEnumerator CheckDampingWhenOccluded()
        {
            m_Collider.SmoothingTime = 0;
            m_Collider.Damping = 0;
            m_Collider.DampingWhenOccluded = 1;
            var originalCamPosition = m_Vcam.State.GetFinalPosition();
            
            yield return null;
            UpdateCinemachine();
            
            Assert.That(originalCamPosition, Is.EqualTo(m_Vcam.State.GetFinalPosition()).Using(Vector3EqualityComparer.Instance));

            var obstacle = GameObject.CreatePrimitive(PrimitiveType.Cube);
            obstacle.transform.position = originalCamPosition; // place obstacle so that camera needs to move

            yield return WaitForOnePhysicsFrame(); // ensure that moving the collider (obstacle) takes affect
            UpdateCinemachine();
            
            // we are pulling away from obstacle
            Assert.That(originalCamPosition, Is.Not.EqualTo(m_Vcam.State.GetFinalPosition()).Using(Vector3EqualityComparer.Instance));
            Assert.That(new Vector3(0,0,-4.778574f), Is.EqualTo(m_Vcam.State.GetFinalPosition()).Using(Vector3EqualityComparer.Instance));
            
            var previousPosition = m_Vcam.State.GetFinalPosition();
            yield return WaitForSeconds(0.5f);
            UpdateCinemachine();
            
            Assert.That(previousPosition, Is.Not.EqualTo(m_Vcam.State.GetFinalPosition()).Using(Vector3EqualityComparer.Instance));
            Assert.That(new Vector3(0,0,-4.423887f), Is.EqualTo(m_Vcam.State.GetFinalPosition()).Using(Vector3EqualityComparer.Instance));
            UnityEngine.Object.Destroy(obstacle);

            yield return WaitForOnePhysicsFrame(); // ensure that the obstacle's collider is removed
            UpdateCinemachine();
            
            Assert.That(originalCamPosition, Is.EqualTo(m_Vcam.State.GetFinalPosition()).Using(Vector3EqualityComparer.Instance));
        }
        
        [UnityTest]
        public IEnumerator CheckDamping()
        {
            m_Collider.SmoothingTime = 0;
            m_Collider.Damping = 1;
            m_Collider.DampingWhenOccluded = 0;
            var originalCamPosition = m_Vcam.State.GetFinalPosition();

            yield return null; 
            UpdateCinemachine();
            
            Assert.That(originalCamPosition, Is.EqualTo(m_Vcam.State.GetFinalPosition()).Using(Vector3EqualityComparer.Instance));

            var obstacle = GameObject.CreatePrimitive(PrimitiveType.Cube);
            obstacle.transform.position = originalCamPosition; // place obstacle so that camera needs to move

            yield return WaitForOnePhysicsFrame(); // ensure that moving the collider (obstacle) takes affect
            UpdateCinemachine();
            
            // camera moved check
            Assert.That(originalCamPosition, Is.Not.EqualTo(m_Vcam.State.GetFinalPosition()).Using(Vector3EqualityComparer.Instance));
            
            var obstructedPosition = m_Vcam.State.GetFinalPosition();
            Assert.That(originalCamPosition, !Is.EqualTo(obstructedPosition).Using(Vector3EqualityComparer.Instance));

            // wait another frame to avoid snap - we need to have a previous damp time to avoid snap
            yield return null;
            UpdateCinemachine();
            Assert.That(obstructedPosition, Is.EqualTo(m_Vcam.State.GetFinalPosition()).Using(Vector3EqualityComparer.Instance));
            
            UnityEngine.Object.Destroy(obstacle);
            
            yield return WaitForOnePhysicsFrame(); // ensure that the obstacle's collider is removed
            UpdateCinemachine();
            
            // camera has moved and it is not yet back at its original position because damping > 0.
            var finalPosition = m_Vcam.State.GetFinalPosition();
            Assert.That(obstructedPosition, Is.Not.EqualTo(finalPosition).Using(Vector3EqualityComparer.Instance));
            Assert.That(originalCamPosition, Is.Not.EqualTo(finalPosition).Using(Vector3EqualityComparer.Instance));
            Assert.That(new Vector3(0, 0, -4.71081734f), Is.EqualTo(finalPosition).Using(Vector3EqualityComparer.Instance));
        }
    }
}
#endif
