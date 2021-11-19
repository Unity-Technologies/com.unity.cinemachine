using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Cinemachine;

namespace Tests.Runtime
{
    public class CmColliderTests : CinemachineFixtureBase
    {
        GameObject m_CameraHolderWithBrain;
        CinemachineVirtualCamera m_Vcam;
        CinemachineCollider m_Collider;
        GameObject m_FollowObject;

        [SetUp]
        public override void SetUp()
        {
            m_CameraHolderWithBrain = CreateGameObject("MainCamera", typeof(Camera), typeof(CinemachineBrain));

            m_Vcam = CreateGameObject("CM Vcam", typeof(CinemachineVirtualCamera), typeof(CinemachineCollider)).GetComponent<CinemachineVirtualCamera>();
            m_Vcam.Priority = 100;
            m_FollowObject = CreateGameObject("Follow Object");
            var framingTransposer = m_Vcam.AddCinemachineComponent<CinemachineFramingTransposer>();
            framingTransposer.m_CameraDistance = 5f;
            m_Collider = m_Vcam.GetComponent<CinemachineCollider>();
            m_Collider.m_Strategy = CinemachineCollider.ResolutionStrategy.PullCameraForward;
            m_Collider.m_CollideAgainst = 1;
            m_Collider.m_AvoidObstacles = true;
            m_Collider.m_SmoothingTime = 0;
            m_Collider.m_Damping = 0;
            m_Collider.m_DampingWhenOccluded = 0;
            m_Vcam.AddExtension(m_Collider);
            
            base.SetUp();
        }

        [UnityTest]
        public IEnumerator CheckSmoothingTime()
        {
            m_Collider.m_SmoothingTime = 1;
            var originalCamPosition = m_Vcam.State.FinalPosition;

            yield return null;
            Assert.That(originalCamPosition == m_Vcam.State.FinalPosition, Is.True);

            var obstacle = GameObject.CreatePrimitive(PrimitiveType.Cube);
            obstacle.transform.position = originalCamPosition; // put obstacle in the way

            yield return null;
            Assert.That(originalCamPosition == m_Vcam.State.FinalPosition, Is.False);
            
            obstacle.transform.position = originalCamPosition + Vector3.left * 100f; // move obstacle out of the way

            yield return new WaitForSeconds(1);
            Assert.That(originalCamPosition == m_Vcam.State.FinalPosition, Is.True);
        }
        
        [UnityTest]
        public IEnumerator CheckDampingWhenOccluded()
        {
            m_Collider.m_DampingWhenOccluded = 10;
            var originalCamPosition = m_Vcam.State.FinalPosition;

            yield return null;
            Assert.That(originalCamPosition == m_Vcam.State.FinalPosition, Is.True);

            var obstacle = GameObject.CreatePrimitive(PrimitiveType.Cube);
            obstacle.transform.position = originalCamPosition; // put obstacle in the way

            yield return null;
            Assert.That(originalCamPosition == m_Vcam.State.FinalPosition, Is.False);
            var pos1 = m_Vcam.State.FinalPosition;

            yield return null;
            Assert.That(pos1 == m_Vcam.State.FinalPosition, Is.False);
        }
        
        [UnityTest]
        public IEnumerator CheckDamping()
        {
            m_Collider.m_Damping = 10;
            var originalCamPosition = m_Vcam.State.FinalPosition;

            yield return null;
            Assert.That(originalCamPosition == m_Vcam.State.FinalPosition, Is.True);

            var obstacle = GameObject.CreatePrimitive(PrimitiveType.Cube);
            obstacle.transform.position = originalCamPosition; // put obstacle in the way

            yield return null;
            Assert.That(originalCamPosition == m_Vcam.State.FinalPosition, Is.False);
            
            obstacle.transform.position = originalCamPosition + Vector3.left * 100f; // move obstacle out of the way
            var pos1 = m_Vcam.State.FinalPosition;

            yield return null;
            Assert.That(pos1 == m_Vcam.State.FinalPosition, Is.False);
            Assert.That(originalCamPosition == m_Vcam.State.FinalPosition, Is.False);
        }
    }
}