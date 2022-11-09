using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Cinemachine;

namespace Tests.Runtime
{
    public class BrainTargetOverrideTests : CinemachineRuntimeFixtureBase
    {
        GameObject m_CameraHolderWithBrain, m_CameraHolderWithoutBrain, m_GoWithBrain, m_GoWithoutBrain;
        CmCamera m_Vcam;
        GameObject m_FollowObject;
        CinemachineBrain m_BrainAlone, m_BrainAlone2;

        [SetUp]
        public override void SetUp()
        {
            m_CameraHolderWithBrain = CreateGameObject("MainCamera 1", typeof(Camera), typeof(CinemachineBrain));
            
            m_CameraHolderWithoutBrain = CreateGameObject("MainCamera 2", typeof(Camera));
            m_BrainAlone = CreateGameObject("BrainAlone for MainCamera 2", typeof(CinemachineBrain)).GetComponent<CinemachineBrain>();
            m_BrainAlone.ControlledObject = m_CameraHolderWithoutBrain;
            
            m_GoWithBrain = CreateGameObject("Empty 1", typeof(CinemachineBrain));

            m_GoWithoutBrain = CreateGameObject("Empty 2");
            m_BrainAlone2 = CreateGameObject("BrainAlone for Empty 2", typeof(CinemachineBrain)).GetComponent<CinemachineBrain>(); 
            m_BrainAlone2.ControlledObject = m_GoWithoutBrain;
            
            m_Vcam = CreateGameObject("CM Vcam", typeof(CmCamera)).GetComponent<CmCamera>();
            m_Vcam.Priority = 100;
            m_FollowObject = CreateGameObject("Follow Object");
            
            base.SetUp();
        }

        Vector3 m_Delta = new Vector3(10, 0, 0);
        IEnumerator CheckThatBrainsAreControllingTheirTargets()
        {
            m_FollowObject.transform.position += m_Delta;
            yield return null;
            AreBrainControlledTransformsTheSame();
            m_FollowObject.transform.position += m_Delta;
            yield return null;
            AreBrainControlledTransformsTheSame();
            
            void AreBrainControlledTransformsTheSame()
            {
                Assert.That(m_CameraHolderWithBrain.GetComponent<Camera>().fieldOfView == m_CameraHolderWithoutBrain.GetComponent<Camera>().fieldOfView, Is.True);
                Assert.That(EqualTransforms(m_CameraHolderWithBrain.transform, m_CameraHolderWithoutBrain.transform), Is.True);
                Assert.That(EqualTransforms(m_CameraHolderWithBrain.transform, m_GoWithBrain.transform), Is.True);
                Assert.That(EqualTransforms(m_CameraHolderWithBrain.transform, m_GoWithoutBrain.transform), Is.True);
            
                bool EqualTransforms(Transform a, Transform b) => a.position == b.position && a.rotation == b.rotation;
            }
        }
        
        IEnumerator CheckDisconnectedBrains()
        {
            m_BrainAlone.ControlledObject = null;
            m_BrainAlone2.ControlledObject = null;
            m_FollowObject.transform.position += m_Delta;
            yield return null;
            var position = m_CameraHolderWithBrain.transform.position;
            Assert.That(position == m_CameraHolderWithoutBrain.transform.position, Is.False);
            Assert.That(position == m_GoWithoutBrain.transform.position, Is.False);
        }
        
        [UnityTest]
        public IEnumerator DoNothing()
        {
            m_Vcam.Lens.FieldOfView = 50;
            yield return CheckThatBrainsAreControllingTheirTargets();
        }

        [UnityTest]
        public IEnumerator ThirdPerson()
        {
            m_Vcam.gameObject.AddComponent<CinemachineThirdPersonFollow>();
            m_Vcam.Lens.FieldOfView = 50;
            m_Vcam.Follow = m_FollowObject.transform;
            yield return CheckThatBrainsAreControllingTheirTargets();
            yield return CheckDisconnectedBrains();
        }
        
        [UnityTest]
        public IEnumerator FramingTransposer()
        {
            var framingTransposer = m_Vcam.gameObject.AddComponent<CinemachinePositionComposer>();
            framingTransposer.Damping = Vector3.zero;
            framingTransposer.CameraDistance = 1f;
            m_Vcam.Follow = m_FollowObject.transform;
            yield return CheckThatBrainsAreControllingTheirTargets();
            yield return CheckDisconnectedBrains();
        }

        [UnityTest]
        public IEnumerator HardLockToTarget()
        {
            m_Vcam.gameObject.AddComponent<CinemachineHardLockToTarget>();
            m_Vcam.Follow = m_FollowObject.transform;
            yield return CheckThatBrainsAreControllingTheirTargets();
            yield return CheckDisconnectedBrains();
        }

        [UnityTest]
        public IEnumerator OrbTransposer()
        {
            var orbitalTransposer = m_Vcam.gameObject.AddComponent<CinemachineOrbitalFollow>();
            orbitalTransposer.TrackerSettings.PositionDamping = Vector3.zero;
            orbitalTransposer.OrbitStyle = CinemachineOrbitalFollow.OrbitStyles.Sphere;
            orbitalTransposer.Radius = 0;
            m_Vcam.Follow = m_FollowObject.transform;
            yield return CheckThatBrainsAreControllingTheirTargets();
            yield return CheckDisconnectedBrains();
        }

        [UnityTest]
        public IEnumerator Transposer()
        {
            var transposer = m_Vcam.gameObject.AddComponent<CinemachineFollow>();
            transposer.TrackerSettings.PositionDamping = Vector3.zero;
            transposer.FollowOffset = Vector3.zero;
            m_Vcam.Follow = m_FollowObject.transform;
            yield return CheckThatBrainsAreControllingTheirTargets();
            yield return CheckDisconnectedBrains();
        }
    }
}