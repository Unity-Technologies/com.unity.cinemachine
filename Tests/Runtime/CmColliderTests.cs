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
        }

        GameObject CreateObstacle(Vector3 position)
        {
            var obstacle = GameObject.CreatePrimitive(PrimitiveType.Cube);
            obstacle.transform.position = position; // put obstacle in the way
            m_GameObjectsToDestroy.Add(obstacle);
            return obstacle;
        }

        [UnityTest]
        public IEnumerator CheckSmoothingTime()
        {
            m_Collider.m_SmoothingTime = 1;
            var originalCamPosition = m_Vcam.State.FinalPosition;

            yield return WaitForXFullFrames(1);
            Assert.That(originalCamPosition == m_Vcam.State.FinalPosition, Is.True);

            var obstacle = CreateObstacle(originalCamPosition);

            yield return WaitForXFullFrames(2);
            Assert.That(originalCamPosition == m_Vcam.State.FinalPosition, Is.False);
            
            obstacle.transform.position = originalCamPosition + Vector3.left * 100f; // move obstacle out of the way

            yield return WaitForSeconds(m_Collider.m_SmoothingTime);
            Assert.That(originalCamPosition == m_Vcam.State.FinalPosition, Is.True);
        }
        
        [UnityTest]
        public IEnumerator CheckDampingWhenOccluded()
        {
            m_Collider.m_DampingWhenOccluded = 5;
            var originalCamPosition = m_Vcam.State.FinalPosition;

            yield return WaitForXFullFrames(1);
            Assert.That(originalCamPosition == m_Vcam.State.FinalPosition, Is.True);

            var obstacle = CreateObstacle(originalCamPosition);

            yield return WaitForXFullFrames(2);
            Assert.That(originalCamPosition == m_Vcam.State.FinalPosition, Is.False);
            var pos1 = m_Vcam.State.FinalPosition;

            yield return WaitForXFullFrames(2);
            Assert.That(pos1 == m_Vcam.State.FinalPosition, Is.False);
            
            obstacle.transform.position = originalCamPosition + Vector3.left * 100f; // move obstacle out of the way

            yield return WaitForXFullFrames(2);
            Debug.Log("m_Vcam.State.FinalPosition:"+m_Vcam.State.FinalPosition+" vs "+originalCamPosition);
            Assert.That(originalCamPosition == m_Vcam.State.FinalPosition, Is.True);
        }
        
        [UnityTest]
        public IEnumerator CheckDamping()
        {
            m_Collider.m_Damping = 5;
            var originalCamPosition = m_Vcam.State.FinalPosition;

            yield return WaitForXFullFrames(1);
            Assert.That(originalCamPosition == m_Vcam.State.FinalPosition, Is.True);

            var obstacle = CreateObstacle(originalCamPosition);

            yield return WaitForXFullFrames(2);
            Assert.That(originalCamPosition == m_Vcam.State.FinalPosition, Is.False);
            
            obstacle.transform.position = originalCamPosition + Vector3.left * 100f; // move obstacle out of the way
            var pos1 = m_Vcam.State.FinalPosition;
            
            yield return WaitForXFullFrames(2);
            Assert.That(pos1 == m_Vcam.State.FinalPosition, Is.False);
            Assert.That(originalCamPosition == m_Vcam.State.FinalPosition, Is.False);
        }
        
        internal IEnumerator WaitForXFullFrames(int x)
        {
            for (var i = 0; i <= x; ++i) // wait 1 + x frames, because we need to finish current frame
            {
                yield return new WaitForEndOfFrame();
            }
        }

        internal IEnumerator WaitForSeconds(float time)
        {
            yield return new WaitForEndOfFrame(); // wait for this frame to end
            yield return new WaitForEndOfFrame(); // wait for next frame to end
            yield return new WaitForSeconds(time); // wait for time seconds
            yield return new WaitForEndOfFrame(); // wait for this frame to end
        }
    }
}