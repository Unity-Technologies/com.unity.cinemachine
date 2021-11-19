using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Cinemachine;

namespace Tests.Runtime
{
    public class CmColliderTests : CinemachineFixtureBase
    {
        CinemachineVirtualCamera m_Vcam;
        CinemachineCollider m_Collider;
        GameObject m_FollowObject;

        [SetUp]
        public override void SetUp()
        {
            CreateGameObject("MainCamera", typeof(Camera), typeof(CinemachineBrain));

            m_Vcam = CreateGameObject("CM Vcam", typeof(CinemachineVirtualCamera), typeof(CinemachineCollider)).GetComponent<CinemachineVirtualCamera>();
            m_Vcam.Priority = 100;
            m_Vcam.Follow = CreateGameObject("Follow Object").transform;
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
            Debug.Log("Setup");
        }

        GameObject CreateObstacle(Vector3 position)
        {
            var obstacle = GameObject.CreatePrimitive(PrimitiveType.Cube);
            obstacle.transform.position = position; // put obstacle in the way
            m_GameObjectsToDestroy.Add(obstacle);
            return obstacle;
        }

        void DebugLog(Vector3 v1, Vector3 v2, string condition)
        {
            Debug.Log(v1 + " == " + v2 + " is " + condition);
        }

        [UnityTest]
        public IEnumerator CheckSmoothingTime()
        {
            m_Collider.m_SmoothingTime = 1;
            var originalCamPosition = m_Vcam.State.FinalPosition;

            yield return null;
            DebugLog(originalCamPosition, m_Vcam.State.FinalPosition, "TRUE");
            Assert.That(originalCamPosition == m_Vcam.State.FinalPosition, Is.True);

            var obstacle = CreateObstacle(originalCamPosition);

            yield return null;
            yield return null;
            yield return null;
            DebugLog(originalCamPosition, m_Vcam.State.FinalPosition, "FALSE");
            Assert.That(originalCamPosition == m_Vcam.State.FinalPosition, Is.False);
            
            obstacle.transform.position = originalCamPosition + Vector3.left * 100f; // move obstacle out of the way

            yield return null;
            yield return null;
            yield return new WaitForSeconds(1);
            DebugLog(originalCamPosition, m_Vcam.State.FinalPosition, "TRUE");
            Assert.That(originalCamPosition == m_Vcam.State.FinalPosition, Is.True);
        }
        
        [UnityTest]
        public IEnumerator CheckDampingWhenOccluded()
        {
            m_Collider.m_DampingWhenOccluded = 5;
            var originalCamPosition = m_Vcam.State.FinalPosition;

            yield return null;
            DebugLog(originalCamPosition, m_Vcam.State.FinalPosition, "TRUE");
            Assert.That(originalCamPosition == m_Vcam.State.FinalPosition, Is.True);

            var obstacle = CreateObstacle(originalCamPosition);

            yield return null;
            yield return null;
            yield return null;
            DebugLog(originalCamPosition, m_Vcam.State.FinalPosition, "FALSE");
            Assert.That(originalCamPosition == m_Vcam.State.FinalPosition, Is.False);
            var pos1 = m_Vcam.State.FinalPosition;

            yield return null;
            yield return null;
            yield return null;
            DebugLog(pos1, m_Vcam.State.FinalPosition, "FALSE");
            Assert.That(pos1 == m_Vcam.State.FinalPosition, Is.False);
            
            obstacle.transform.position = originalCamPosition + Vector3.left * 100f; // move obstacle out of the way

            yield return null;
            yield return null;
            yield return null;
            DebugLog(originalCamPosition, m_Vcam.State.FinalPosition, "TRUE");
            Assert.That(originalCamPosition == m_Vcam.State.FinalPosition, Is.True);
        }
        
        [UnityTest]
        public IEnumerator CheckDamping()
        {
            m_Collider.m_Damping = 5;
            var originalCamPosition = m_Vcam.State.FinalPosition;

            yield return null;
            DebugLog(originalCamPosition, m_Vcam.State.FinalPosition, "TRUE");
            Assert.That(originalCamPosition == m_Vcam.State.FinalPosition, Is.True);

            var obstacle = CreateObstacle(originalCamPosition);

            yield return null;
            yield return null;
            yield return null;
            DebugLog(originalCamPosition, m_Vcam.State.FinalPosition, "FALSE");
            Assert.That(originalCamPosition == m_Vcam.State.FinalPosition, Is.False);
            
            obstacle.transform.position = originalCamPosition + Vector3.left * 100f; // move obstacle out of the way
            var pos1 = m_Vcam.State.FinalPosition;

            yield return null;
            yield return null;
            yield return null;
            DebugLog(pos1, m_Vcam.State.FinalPosition, "FALSE");
            Assert.That(pos1 == m_Vcam.State.FinalPosition, Is.False);
            DebugLog(originalCamPosition, m_Vcam.State.FinalPosition, "FALSE");
            Assert.That(originalCamPosition == m_Vcam.State.FinalPosition, Is.False);
        }
    }
}