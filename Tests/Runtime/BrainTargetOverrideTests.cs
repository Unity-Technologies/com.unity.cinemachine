using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Cinemachine;
using UnityEngine.TestTools.Utils;

namespace Tests.Runtime
{
    public class BrainTargetOverrideTests : CinemachineFixtureBase
    {
        GameObject m_CameraHolderWithBrain, m_CameraHolderWithoutBrain, m_GoWithBrain, m_GoWithoutBrain;
        CinemachineVirtualCamera m_Vcam;
        GameObject m_FollowObject;

        [SetUp]
        public override void SetUp()
        {
            m_CameraHolderWithBrain = CreateGameObject("MainCamera 1", typeof(Camera), typeof(CinemachineBrain));
            
            m_CameraHolderWithoutBrain = CreateGameObject("MainCamera 2", typeof(Camera));
            var brainAlone = CreateGameObject("BrainAlone for MainCamera 2", typeof(CinemachineBrain)).GetComponent<CinemachineBrain>();
            brainAlone.TargetOverride = m_CameraHolderWithoutBrain;
            
            m_GoWithBrain = CreateGameObject("Empty 1", typeof(CinemachineBrain));

            m_GoWithoutBrain = CreateGameObject("Empty 2");
            var brainAlone2 = CreateGameObject("BrainAlone for Empty 2", typeof(CinemachineBrain)).GetComponent<CinemachineBrain>(); 
            brainAlone2.TargetOverride = m_GoWithoutBrain;
            
            m_Vcam = CreateGameObject("CM Vcam", typeof(CinemachineVirtualCamera)).GetComponent<CinemachineVirtualCamera>();
            m_Vcam.Priority = 100;
            m_FollowObject = CreateGameObject("Follow Object");
            
            base.SetUp();
        }
        
        void AreBrainControlledTransformsTheSame()
        {
            Debug.Log(m_CameraHolderWithBrain.GetComponent<Camera>().fieldOfView + "|" + m_CameraHolderWithoutBrain.GetComponent<Camera>().fieldOfView);
            Assert.That(m_CameraHolderWithBrain.GetComponent<Camera>().fieldOfView == m_CameraHolderWithoutBrain.GetComponent<Camera>().fieldOfView, Is.True);
            Assert.That(EqualTransforms(m_CameraHolderWithBrain.transform, m_CameraHolderWithoutBrain.transform), Is.True);
            Assert.That(EqualTransforms(m_CameraHolderWithBrain.transform, m_GoWithBrain.transform), Is.True);
            Assert.That(EqualTransforms(m_CameraHolderWithBrain.transform, m_GoWithoutBrain.transform), Is.True);
            
            bool EqualTransforms(Transform a, Transform b) => a.position == b.position && a.rotation == b.rotation;
        }

        [UnityTest]
        public IEnumerator DoNothing()
        {
            m_Vcam.Follow = m_FollowObject.transform;
            m_Vcam.m_Lens.FieldOfView = 50;
            m_FollowObject.transform.position += new Vector3(2, 2, 2);
            yield return null;
            AreBrainControlledTransformsTheSame();
            yield return null;
            AreBrainControlledTransformsTheSame();
        }

        [UnityTest]
        public IEnumerator ThirdPerson()
        {
            m_Vcam.AddCinemachineComponent<Cinemachine3rdPersonFollow>();
            m_Vcam.Follow = m_FollowObject.transform;
            m_FollowObject.transform.position += new Vector3(10, 0, 0);
            yield return null;
            AreBrainControlledTransformsTheSame();
            yield return null;
            AreBrainControlledTransformsTheSame();
        }

        [UnityTest]
        public IEnumerator FramingTransposer()
        {
            var component = m_Vcam.AddCinemachineComponent<CinemachineFramingTransposer>();
            component.m_XDamping = 0;
            component.m_YDamping = 0;
            component.m_ZDamping = 0;
            component.m_CameraDistance = 1f;
            m_Vcam.Follow = m_FollowObject.transform;
            m_FollowObject.transform.position += new Vector3(10, 0, 0);
            yield return null;
            AreBrainControlledTransformsTheSame();
            yield return null;
            AreBrainControlledTransformsTheSame();
        }

        [UnityTest]
        public IEnumerator HardLockToTarget()
        {
            m_Vcam.AddCinemachineComponent<CinemachineHardLockToTarget>();
            m_Vcam.Follow = m_FollowObject.transform;
            m_FollowObject.transform.position += new Vector3(10, 0, 0);
            yield return null;
            AreBrainControlledTransformsTheSame();
            yield return null;
            AreBrainControlledTransformsTheSame();
        }

        [UnityTest]
        public IEnumerator OrbTransposer()
        {
            var component = m_Vcam.AddCinemachineComponent<CinemachineOrbitalTransposer>();
            component.m_XAxis.m_InputAxisName = ""; // to avoid error when the Input System is installed in the testing project
            component.m_XDamping = 0;
            component.m_YDamping = 0;
            component.m_ZDamping = 0;
            component.m_FollowOffset = new Vector3(0, 0, 0);
            m_Vcam.Follow = m_FollowObject.transform;
            m_FollowObject.transform.position += new Vector3(10, 0, 0);
            yield return null;
            AreBrainControlledTransformsTheSame();
            yield return null;
            AreBrainControlledTransformsTheSame();
        }

        [UnityTest]
        public IEnumerator TrackedDolly()
        {
            m_Vcam.AddCinemachineComponent<CinemachineTrackedDolly>();
            m_Vcam.Follow = m_FollowObject.transform;
            m_FollowObject.transform.position += new Vector3(2, 2, 2);
            yield return null;
            AreBrainControlledTransformsTheSame();
            yield return null;
            AreBrainControlledTransformsTheSame();
        }

        [UnityTest]
        public IEnumerator Transposer()
        {
            var component = m_Vcam.AddCinemachineComponent<CinemachineTransposer>();
            component.m_XDamping = 0;
            component.m_YDamping = 0;
            component.m_ZDamping = 0;
            component.m_FollowOffset = new Vector3(0, 0, 0);
            m_Vcam.Follow = m_FollowObject.transform;
            m_FollowObject.transform.position += new Vector3(10, 0, 0);
            yield return null;
            AreBrainControlledTransformsTheSame();
            yield return null;
            AreBrainControlledTransformsTheSame();
        }
    }
}