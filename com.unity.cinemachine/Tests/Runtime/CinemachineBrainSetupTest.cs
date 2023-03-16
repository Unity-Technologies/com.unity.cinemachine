#if UNITY_EDITOR //Selection.activeGameObject
using System.Collections;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

namespace Unity.Cinemachine.Tests.Editor
{
    [TestFixture]
    public class CinemachineBrainSetup : CinemachineFixtureBase
    {
        CinemachineCamera m_Vcam;
        GameObject m_FollowObject;

        [SetUp]
        public void Setup()
        {
            base.SetUp();
            CreateGameObject("Brain", typeof(CinemachineBrain));
            m_Vcam = CreateGameObject("CM Vcam", typeof(CinemachineCamera), 
                typeof(CinemachineOrbitalFollow), 
                typeof(CinemachineRotationComposer),
                typeof(CinemachineFreeLookModifier),
                typeof(CinemachineInputAxisController)).GetComponent<CinemachineCamera>();
            m_FollowObject = CreateGameObject("Follow");
            m_Vcam.Target.TrackingTarget = m_FollowObject.transform;
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