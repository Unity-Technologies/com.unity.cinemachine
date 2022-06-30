using System;
using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Cinemachine;
using UnityEngine.TestTools.Utils;

namespace Tests.Runtime
{
#if CINEMACHINE_PHYSICS
    public class CmColliderTests : CinemachineFixtureBase
    {
        CinemachineBrain m_Brain;
        CinemachineVirtualCamera m_Vcam;
        CinemachineCollider m_Collider;
        GameObject m_FollowObject;

        [SetUp]
        public override void SetUp()
        {
            var camera = CreateGameObject("MainCamera", typeof(Camera), typeof(CinemachineBrain));
            m_Brain = camera.GetComponent<CinemachineBrain>();

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
            
            // Manual update is needed because when waiting for physics frame, we may pass 1-3 frames. Without manual
            // update the test won't be deterministic, because we would update 1-3 times, instead of just once.
            m_Brain.m_UpdateMethod = CinemachineBrain.UpdateMethod.ManualUpdate; 
        }

        [UnityTest, ConditionalIgnore("IgnoreHDRP2020", "Ignored on HDRP Unity 2020.")]
        public IEnumerator CheckSmoothingTime()
        {
            m_Collider.m_SmoothingTime = 1;
            m_Collider.m_Damping = 0;
            m_Collider.m_DampingWhenOccluded = 0;
            var originalCamPosition = m_Vcam.State.FinalPosition;

            yield return null; 
            m_Brain.ManualUpdate();
            Assert.That(originalCamPosition, Is.EqualTo(m_Vcam.State.FinalPosition).Using(Vector3EqualityComparer.Instance));

            var obstacle = GameObject.CreatePrimitive(PrimitiveType.Cube);
            obstacle.transform.position = originalCamPosition; // place obstacle so that camera needs to move
            
            yield return WaitForOnePhysicsFrame();
            m_Brain.ManualUpdate();
            
            // Camera moved check
            Assert.That(originalCamPosition, !Is.EqualTo(m_Vcam.State.FinalPosition).Using(Vector3EqualityComparer.Instance));
            Assert.That(new Vector3(0, 0, -4.4f), Is.EqualTo(m_Vcam.State.FinalPosition).Using(Vector3EqualityComparer.Instance));
            
            UnityEngine.Object.Destroy(obstacle);
            
            // wait smoothing time and a frame so that camera move back to its original position
            var timerStart = CinemachineCore.CurrentTime;
            yield return WaitForOnePhysicsFrame();
            do
            {
                m_Brain.ManualUpdate();
                yield return null;
            } while ((CinemachineCore.CurrentTime - timerStart) < m_Collider.m_SmoothingTime);
            
            m_Brain.ManualUpdate();
            Assert.That(originalCamPosition, Is.EqualTo(m_Vcam.State.FinalPosition).Using(Vector3EqualityComparer.Instance));
        }
        
        [UnityTest, ConditionalIgnore("IgnoreHDRP2020", "Ignored on HDRP Unity 2020.")]
        public IEnumerator CheckDampingWhenOccluded()
        {
            m_Collider.m_SmoothingTime = 0;
            m_Collider.m_Damping = 0;
            m_Collider.m_DampingWhenOccluded = 1;
            var originalCamPosition = m_Vcam.State.FinalPosition;
            
            yield return null; 
            m_Brain.ManualUpdate();
            
            Assert.That(originalCamPosition, Is.EqualTo(m_Vcam.State.FinalPosition).Using(Vector3EqualityComparer.Instance));

            var obstacle = GameObject.CreatePrimitive(PrimitiveType.Cube);
            obstacle.transform.position = originalCamPosition; // place obstacle so that camera needs to move

            yield return WaitForOnePhysicsFrame();
            m_Brain.ManualUpdate();
            
            // we are pulling away from obstacle
            Assert.That(originalCamPosition, !Is.EqualTo(m_Vcam.State.FinalPosition).Using(Vector3EqualityComparer.Instance));
            Assert.That(new Vector3(0,0,-4.778574f), Is.EqualTo(m_Vcam.State.FinalPosition).Using(Vector3EqualityComparer.Instance));
            
            var previousPosition = m_Vcam.State.FinalPosition;
            var timerStart = CinemachineCore.CurrentTime;
            do
            {
                m_Brain.ManualUpdate();
                yield return null;
            } while ((CinemachineCore.CurrentTime - timerStart) < 0.5f);
            m_Brain.ManualUpdate();
            
            Assert.That(previousPosition, !Is.EqualTo(m_Vcam.State.FinalPosition).Using(Vector3EqualityComparer.Instance));
            Assert.That(new Vector3(0,0,-4.4f), Is.EqualTo(m_Vcam.State.FinalPosition).Using(Vector3EqualityComparer.Instance));
            
            UnityEngine.Object.Destroy(obstacle);

            yield return WaitForOnePhysicsFrame();
            m_Brain.ManualUpdate();
            
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
            m_Brain.ManualUpdate();
            
            Assert.That(originalCamPosition, Is.EqualTo(m_Vcam.State.FinalPosition).Using(Vector3EqualityComparer.Instance));

            var obstacle = GameObject.CreatePrimitive(PrimitiveType.Cube);
            obstacle.transform.position = originalCamPosition; // place obstacle so that camera needs to move

            yield return WaitForOnePhysicsFrame();
            m_Brain.ManualUpdate();
            
            // camera moved check
            Assert.That(originalCamPosition, !Is.EqualTo(m_Vcam.State.FinalPosition).Using(Vector3EqualityComparer.Instance));
            
            UnityEngine.Object.Destroy(obstacle);
            var previousPosition = m_Vcam.State.FinalPosition;
            
            yield return WaitForOnePhysicsFrame();
            m_Brain.ManualUpdate();
            
            // camera has moved and it is not yet back at its original position
            Assert.That(previousPosition, !Is.EqualTo(m_Vcam.State.FinalPosition).Using(Vector3EqualityComparer.Instance));
            Assert.That(originalCamPosition, !Is.EqualTo(m_Vcam.State.FinalPosition).Using(Vector3EqualityComparer.Instance));
            
            Assert.That(new Vector3(0, 0, -4.621426f), Is.EqualTo(m_Vcam.State.FinalPosition).Using(Vector3EqualityComparer.Instance));
        }
    }
#endif
}
