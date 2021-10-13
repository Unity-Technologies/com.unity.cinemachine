using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Cinemachine;

namespace Tests.Runtime
{
    [TestFixture]
    public class LookaheadTests : CinemachineFixtureBase
    {
        CinemachineVirtualCamera m_SourceVCam;
        CinemachineComposer m_Composer;
        CinemachineFramingTransposer m_FramingTransposer;
        Transform m_Target;

        [SetUp]
        public override void SetUp()
        {
            // Camera
            CreateGameObject("MainCamera", typeof(Camera), typeof(CinemachineBrain));
            m_Target = CreateGameObject("Target Object").transform;

            // Source vcam
            m_SourceVCam = CreateGameObject("Source CM Vcam", typeof(CinemachineVirtualCamera)).GetComponent<CinemachineVirtualCamera>();
            m_SourceVCam.Priority = 2;
            m_SourceVCam.Follow = m_Target;
            m_SourceVCam.LookAt = m_Target;
            m_FramingTransposer = m_SourceVCam.AddCinemachineComponent<CinemachineFramingTransposer>();
            m_Composer = m_SourceVCam.AddCinemachineComponent<CinemachineComposer>();
            m_FramingTransposer.m_LookaheadSmoothing = m_Composer.m_LookaheadSmoothing = 0.3f;
            m_FramingTransposer.m_LookaheadTime = m_Composer.m_LookaheadTime = 10;

            base.SetUp();
        }

        [UnityTest]
        public IEnumerator TargetsChanged()
        {
            yield return null;
            Assert.That(m_SourceVCam.FollowTargetChanged, Is.False);
            Assert.That(m_SourceVCam.LookAtTargetChanged, Is.False);

            yield return null;
            Assert.That(m_SourceVCam.FollowTargetChanged, Is.False);
            Assert.That(m_SourceVCam.LookAtTargetChanged, Is.False);
            
            var newTarget = CreateGameObject("Target Object 2").transform;
            m_SourceVCam.LookAt = newTarget;

            yield return null; // wait until next frame
            
            Assert.That(m_SourceVCam.FollowTargetChanged, Is.False);
            Assert.That(m_SourceVCam.LookAtTargetChanged, Is.True);

            m_SourceVCam.Follow = newTarget;

            yield return null; // wait until next frame
            
            Assert.That(m_SourceVCam.FollowTargetChanged, Is.True);
            Assert.That(m_SourceVCam.LookAtTargetChanged, Is.False);

            m_SourceVCam.Follow = m_Target;
            m_SourceVCam.LookAt = m_Target;

            yield return null; // wait until next frame
            
            Assert.That(m_SourceVCam.FollowTargetChanged, Is.True);
            Assert.That(m_SourceVCam.LookAtTargetChanged, Is.True);
        }

        [UnityTest]
        public IEnumerator LookaheadDelta()
        {
            yield return null;
            var delta = m_Composer.m_Predictor.PredictPositionDelta(m_Composer.m_LookaheadTime);
            Assert.That(delta.sqrMagnitude > 0, Is.False);
            
            delta = m_FramingTransposer.m_Predictor.PredictPositionDelta(m_FramingTransposer.m_LookaheadTime);
            Assert.That(delta.sqrMagnitude > 0, Is.False);
            
            m_Target.Translate(10, 0, 0);
            yield return null;
            
            delta = m_Composer.m_Predictor.PredictPositionDelta(m_Composer.m_LookaheadTime);
            Assert.That(delta.sqrMagnitude > 0, Is.True);
            
            delta = m_FramingTransposer.m_Predictor.PredictPositionDelta(m_FramingTransposer.m_LookaheadTime);
            Assert.That(delta.sqrMagnitude > 0, Is.True);
        }
    }
}