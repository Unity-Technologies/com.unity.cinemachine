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

        [UnityTest]
        public IEnumerator CheckSmoothingTime()
        {
            CinemachineCore.CurrentTimeOverride = 0;
            m_Collider.m_SmoothingTime = 1;
            m_Collider.m_Damping = 0;
            m_Collider.m_DampingWhenOccluded = 0;
            var originalCamPosition = m_Vcam.State.FinalPosition;

            CinemachineCore.CurrentTimeOverride += CinemachineCore.DeltaTime;
            yield return null; 
            
            Assert.That(originalCamPosition, Is.EqualTo(m_Vcam.State.FinalPosition).Using(Vector3EqualityComparer.Instance));

            // place obstacle so that camera needs to move
            var obstacle = GameObject.CreatePrimitive(PrimitiveType.Cube);
            obstacle.transform.position = originalCamPosition; // put obstacle in the way
            
            CinemachineCore.CurrentTimeOverride += CinemachineCore.DeltaTime;
            yield return new WaitForFixedUpdate();
            yield return null;
            // camera moved check
            Assert.That(originalCamPosition, !Is.EqualTo(m_Vcam.State.FinalPosition).Using(Vector3EqualityComparer.Instance));
            
            // remove obstacle
            UnityEngine.Object.Destroy(obstacle);
            
            // wait smoothing time and a frame so that camera move back to its original position
            CinemachineCore.CurrentTimeOverride += m_Collider.m_SmoothingTime;
            yield return new WaitForFixedUpdate();
            yield return null;
            Assert.That(originalCamPosition, Is.EqualTo(m_Vcam.State.FinalPosition).Using(Vector3EqualityComparer.Instance));
        }
        
        [UnityTest]
        public IEnumerator CheckDampingWhenOccluded()
        {
            CinemachineCore.CurrentTimeOverride = 0;
            m_Collider.m_SmoothingTime = 0;
            m_Collider.m_Damping = 0;
            m_Collider.m_DampingWhenOccluded = 1;
            var originalCamPosition = m_Vcam.State.FinalPosition;
            
            CinemachineCore.CurrentTimeOverride += CinemachineCore.DeltaTime;
            yield return null; 
            
            Assert.That(originalCamPosition, Is.EqualTo(m_Vcam.State.FinalPosition).Using(Vector3EqualityComparer.Instance));

            var obstacle = GameObject.CreatePrimitive(PrimitiveType.Cube);
            obstacle.transform.position = originalCamPosition; // put obstacle in the way

            CinemachineCore.CurrentTimeOverride += CinemachineCore.DeltaTime;
            yield return new WaitForFixedUpdate();
            yield return null;
            
            // we are pulling away from obstacle
            Assert.That(originalCamPosition, !Is.EqualTo(m_Vcam.State.FinalPosition).Using(Vector3EqualityComparer.Instance));
            var pos1 = m_Vcam.State.FinalPosition;

            CinemachineCore.CurrentTimeOverride += 0.5f;
            yield return null;
            
            // we should have finished pulling away by now
            Assert.That(pos1, !Is.EqualTo(m_Vcam.State.FinalPosition).Using(Vector3EqualityComparer.Instance));
            Assert.True((m_Vcam.State.FinalPosition - new Vector3(0, 0, -4.40f)).sqrMagnitude < 0.2f);
            
            // remove obstacle
            UnityEngine.Object.Destroy(obstacle);

            CinemachineCore.CurrentTimeOverride += CinemachineCore.DeltaTime;
            yield return new WaitForFixedUpdate();
            yield return null;
            
            Assert.That(originalCamPosition, Is.EqualTo(m_Vcam.State.FinalPosition).Using(Vector3EqualityComparer.Instance));
        }
        
        [UnityTest]
        public IEnumerator CheckDamping()
        {
            CinemachineCore.CurrentTimeOverride = 0;
            m_Collider.m_SmoothingTime = 0;
            m_Collider.m_Damping = 1;
            m_Collider.m_DampingWhenOccluded = 0;
            var originalCamPosition = m_Vcam.State.FinalPosition;

            CinemachineCore.CurrentTimeOverride += CinemachineCore.DeltaTime;
            yield return null; 
            Assert.That(originalCamPosition, Is.EqualTo(m_Vcam.State.FinalPosition).Using(Vector3EqualityComparer.Instance));

            var obstacle = GameObject.CreatePrimitive(PrimitiveType.Cube);
            obstacle.transform.position = originalCamPosition; // put obstacle in the way

            CinemachineCore.CurrentTimeOverride += 0.1f;
            yield return new WaitForFixedUpdate();
            yield return null;
            Assert.That(originalCamPosition, !Is.EqualTo(m_Vcam.State.FinalPosition).Using(Vector3EqualityComparer.Instance));
            
            // remove obstacle
            UnityEngine.Object.Destroy(obstacle);
            var pos1 = m_Vcam.State.FinalPosition;
            
            CinemachineCore.CurrentTimeOverride += CinemachineCore.DeltaTime;
            yield return new WaitForFixedUpdate();
            yield return null; 
            
            Assert.That(pos1, !Is.EqualTo(m_Vcam.State.FinalPosition).Using(Vector3EqualityComparer.Instance));
            Assert.That(originalCamPosition, !Is.EqualTo(m_Vcam.State.FinalPosition).Using(Vector3EqualityComparer.Instance));
        }
    }
}