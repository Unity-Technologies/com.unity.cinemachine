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
        CmCamera m_Vcam;
        CinemachineDeoccluder m_Collider;
        GameObject m_FollowObject;

        [SetUp]
        public override void SetUp()
        {
            var camera = CreateGameObject("MainCamera", typeof(Camera), typeof(CinemachineBrain));
            m_Brain = camera.GetComponent<CinemachineBrain>();

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
            
            base.SetUp();
            
            // Manual update is needed because when waiting for physics frame, we may pass 1-3 frames. Without manual
            // update the test won't be deterministic, because we would update 1-3 times, instead of just once.
            m_Brain.UpdateMethod = CinemachineBrain.UpdateMethods.ManualUpdate; 
        }

        [UnityTest]
        public IEnumerator CheckSmoothingTime()
        {
            m_Collider.SmoothingTime = 1;
            m_Collider.Damping = 0;
            m_Collider.DampingWhenOccluded = 0;
            var originalCamPosition = m_Vcam.State.GetFinalPosition();

            yield return null; 
            m_Brain.ManualUpdate();
            Assert.That(originalCamPosition, Is.EqualTo(m_Vcam.State.GetFinalPosition()).Using(Vector3EqualityComparer.Instance));

            var obstacle = GameObject.CreatePrimitive(PrimitiveType.Cube);
            obstacle.transform.position = originalCamPosition; // place obstacle so that camera needs to move
            
            yield return WaitForOnePhysicsFrame();
            m_Brain.ManualUpdate();
            
            // Camera moved check
            Assert.That(originalCamPosition, Is.Not.EqualTo(m_Vcam.State.GetFinalPosition()).Using(Vector3EqualityComparer.Instance));
            Assert.That(new Vector3(0, 0, -4.4f), Is.EqualTo(m_Vcam.State.GetFinalPosition()).Using(Vector3EqualityComparer.Instance));
            
            UnityEngine.Object.Destroy(obstacle);
            
            // wait smoothing time and a frame so that camera move back to its original position
            var timerStart = CinemachineCore.CurrentTime;
            yield return WaitForOnePhysicsFrame();
            do
            {
                m_Brain.ManualUpdate();
                yield return null;
            } while ((CinemachineCore.CurrentTime - timerStart) < m_Collider.SmoothingTime);
            
            m_Brain.ManualUpdate();
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
            m_Brain.ManualUpdate();
            
            Assert.That(originalCamPosition, Is.EqualTo(m_Vcam.State.GetFinalPosition()).Using(Vector3EqualityComparer.Instance));

            var obstacle = GameObject.CreatePrimitive(PrimitiveType.Cube);
            obstacle.transform.position = originalCamPosition; // place obstacle so that camera needs to move

            yield return WaitForOnePhysicsFrame();
            m_Brain.ManualUpdate();
            
            // we are pulling away from obstacle
            Assert.That(originalCamPosition, Is.Not.EqualTo(m_Vcam.State.GetFinalPosition()).Using(Vector3EqualityComparer.Instance));
            Assert.That(new Vector3(0,0,-4.778574f), Is.EqualTo(m_Vcam.State.GetFinalPosition()).Using(Vector3EqualityComparer.Instance));
            
            var previousPosition = m_Vcam.State.GetFinalPosition();
            var timerStart = CinemachineCore.CurrentTime;
            do
            {
                m_Brain.ManualUpdate();
                yield return null;
            } while ((CinemachineCore.CurrentTime - timerStart) < 0.5f);
            m_Brain.ManualUpdate();
            
            Assert.That(previousPosition, Is.Not.EqualTo(m_Vcam.State.GetFinalPosition()).Using(Vector3EqualityComparer.Instance));
            Assert.That(new Vector3(0,0,-4.4f), Is.EqualTo(m_Vcam.State.GetFinalPosition()).Using(Vector3EqualityComparer.Instance));
            
            UnityEngine.Object.Destroy(obstacle);

            yield return WaitForOnePhysicsFrame();
            m_Brain.ManualUpdate();
            
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
            m_Brain.ManualUpdate();
            
            Assert.That(originalCamPosition, Is.EqualTo(m_Vcam.State.GetFinalPosition()).Using(Vector3EqualityComparer.Instance));

            var obstacle = GameObject.CreatePrimitive(PrimitiveType.Cube);
            obstacle.transform.position = originalCamPosition; // place obstacle so that camera needs to move

            yield return WaitForOnePhysicsFrame();
            m_Brain.ManualUpdate();
            
            // camera moved check
            Assert.That(originalCamPosition, Is.Not.EqualTo(m_Vcam.State.GetFinalPosition()).Using(Vector3EqualityComparer.Instance));
            
            var obstructedPosition = m_Vcam.State.GetFinalPosition();
            Assert.That(originalCamPosition, !Is.EqualTo(obstructedPosition).Using(Vector3EqualityComparer.Instance));

            // wait another frame to avoid snap - we need to have a previous damp time to avoid snap
            yield return null;
            m_Brain.ManualUpdate();
            Assert.That(obstructedPosition, Is.EqualTo(m_Vcam.State.GetFinalPosition()).Using(Vector3EqualityComparer.Instance));
            
            UnityEngine.Object.Destroy(obstacle);
            
            yield return WaitForOnePhysicsFrame();
            m_Brain.ManualUpdate();
            
            // camera has moved and it is not yet back at its original position because damping > 0.
            var finalPosition = m_Vcam.State.GetFinalPosition();
            Assert.That(obstructedPosition, Is.Not.EqualTo(finalPosition).Using(Vector3EqualityComparer.Instance));
            Assert.That(originalCamPosition, Is.Not.EqualTo(finalPosition).Using(Vector3EqualityComparer.Instance));
            Assert.That(new Vector3(0, 0, -4.71081734f), Is.EqualTo(finalPosition).Using(Vector3EqualityComparer.Instance));
        }
    }
#endif
}
