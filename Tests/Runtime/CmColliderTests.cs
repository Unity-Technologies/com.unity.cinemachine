using System;
using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Cinemachine;
using UnityEngine.TestTools.Utils;

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
            CinemachineCore.CurrentTimeOverride = 0;
            m_Collider.m_SmoothingTime = 1;
            m_Collider.m_Damping = 0;
            m_Collider.m_DampingWhenOccluded = 0;
            var originalCamPosition = m_Vcam.State.FinalPosition;

            yield return WaitOneFrame(CinemachineCore.DeltaTime);
            Assert.That(originalCamPosition, Is.EqualTo(m_Vcam.State.FinalPosition).Using(Vector3EqualityComparer.Instance));

            // place obstacle so that camera needs to move
            var obstacle = CreateObstacle(originalCamPosition);

            yield return WaitOneFrame(CinemachineCore.DeltaTime);
            yield return WaitOneFrame(CinemachineCore.DeltaTime);
            // camera moved check
            Assert.That(originalCamPosition, !Is.EqualTo(m_Vcam.State.FinalPosition).Using(Vector3EqualityComparer.Instance));
            
            // remove obstacle
            obstacle.transform.position = originalCamPosition + Vector3.left * 100f; // move obstacle out of the way

            // wait smoothing time and a frame so that camera move back to its original position
            yield return WaitOneFrame(m_Collider.m_SmoothingTime);
            yield return WaitOneFrame(CinemachineCore.DeltaTime);
            yield return WaitOneFrame(CinemachineCore.DeltaTime);
            Assert.That(originalCamPosition, Is.EqualTo(m_Vcam.State.FinalPosition).Using(Vector3EqualityComparer.Instance));
        }
        
        [UnityTest]
        public IEnumerator CheckDampingWhenOccluded()
        {
            m_Collider.m_SmoothingTime = 0;
            m_Collider.m_Damping = 0;
            m_Collider.m_DampingWhenOccluded = 1;
            var originalCamPosition = m_Vcam.State.FinalPosition;

            yield return null;
            
            Assert.That(originalCamPosition, Is.EqualTo(m_Vcam.State.FinalPosition).Using(Vector3EqualityComparer.Instance));

            var obstacle = CreateObstacle(originalCamPosition);

            yield return null;
            yield return null;
            
            // we are pulling away from obstacle
            Assert.That(originalCamPosition, !Is.EqualTo(m_Vcam.State.FinalPosition).Using(Vector3EqualityComparer.Instance));
            var pos1 = m_Vcam.State.FinalPosition;
            
            yield return new WaitForSeconds(0.5f);
            
            // we should have finished pulling away by now
            Assert.That(pos1, !Is.EqualTo(m_Vcam.State.FinalPosition).Using(Vector3EqualityComparer.Instance));
            Assert.True((m_Vcam.State.FinalPosition - new Vector3(0, 0, -4.40f)).sqrMagnitude < 0.2f);
            
            obstacle.transform.position = originalCamPosition + Vector3.left * 100f; // move obstacle out of the way

            yield return null;
            yield return null;
            yield return null;
            yield return null;
            
            Assert.That(originalCamPosition, Is.EqualTo(m_Vcam.State.FinalPosition).Using(Vector3EqualityComparer.Instance));
        }
        
        [UnityTest]
        public IEnumerator CheckDamping()
        {
            m_Collider.m_SmoothingTime = 0;
            m_Collider.m_Damping = 1;
            m_Collider.m_DampingWhenOccluded = 0;
            var originalCamPosition = m_Vcam.State.FinalPosition;

            yield return null;
            Assert.That(originalCamPosition, Is.EqualTo(m_Vcam.State.FinalPosition).Using(Vector3EqualityComparer.Instance));

            var obstacle = CreateObstacle(originalCamPosition);
            
            yield return new WaitForSeconds(0.1f);
            Assert.That(originalCamPosition, !Is.EqualTo(m_Vcam.State.FinalPosition).Using(Vector3EqualityComparer.Instance));
            
            obstacle.transform.position = originalCamPosition + Vector3.left * 100f; // move obstacle out of the way
            var pos1 = m_Vcam.State.FinalPosition;
            
            yield return null;
            yield return null;
            
            Assert.That(pos1, !Is.EqualTo(m_Vcam.State.FinalPosition).Using(Vector3EqualityComparer.Instance));
            Assert.That(originalCamPosition, !Is.EqualTo(m_Vcam.State.FinalPosition).Using(Vector3EqualityComparer.Instance));
        }

        /// <summary>
        /// Wait one frame and advances cinemachine internal time by frameTime.
        /// </summary>
        static IEnumerator WaitOneFrame(float frameTime)
        {
            yield return null; 
            CinemachineCore.CurrentTimeOverride += frameTime;
        }
    }
}