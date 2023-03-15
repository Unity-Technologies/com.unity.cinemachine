#if UNITY_EDITOR //Selection.activeGameObject
using System.Collections;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

namespace Unity.Cinemachine.Tests.Editor
{
    [TestFixture]
    public class CinemachineBrainSetup
    {
        CinemachineCamera m_Vcam;
        CinemachineBrain m_Brain;
        GameObject m_FollowObject;

        [SetUp]
        public void Setup()
        {
            m_Brain = new GameObject("Brain").AddComponent<CinemachineBrain>();
            m_Vcam = new GameObject("CM Vcam").AddComponent<CinemachineCamera>();
            m_Vcam.Priority.Value = 100;
            m_FollowObject = new GameObject("Follow");
            m_Vcam.Target.TrackingTarget = m_FollowObject.transform;
            m_Vcam.gameObject.AddComponent<CinemachineOrbitalFollow>();
            m_Vcam.gameObject.AddComponent<CinemachineRotationComposer>();
            m_Vcam.gameObject.AddComponent<CinemachineFreeLookModifier>();
            m_Vcam.gameObject.AddComponent<InputController1AllAxis>();
        }

        [TearDown]
        public void TearDown()
        {
            GameObject.Destroy(m_Vcam.gameObject);
            GameObject.Destroy(m_Brain.gameObject);
        }

        [UnityTest]
        public IEnumerator SelectBrainWithoutCameraComponentDoesNotCauseErrors()
        {
            Selection.activeGameObject = m_Vcam.gameObject;
            yield return null;
            yield return null;
            Selection.activeGameObject = null;
        }
    }
}
#endif