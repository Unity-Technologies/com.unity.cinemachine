using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Unity.Cinemachine.Tests
{
    [TestFixture]
    public class LookaheadTests : CinemachineRuntimeFixtureBase
    {
        CinemachineCamera m_VCam;
        CinemachineRotationComposer m_RotationComposer;
        CinemachinePositionComposer m_PositionComposer;
        Transform m_Target;

        [SetUp]
        public override void SetUp()
        {
            base.SetUp();

            m_Target = CreateGameObject("Target Object").transform;

            // Source vcam
            m_VCam = CreateGameObject("Source CM Vcam", typeof(CinemachineCamera)).GetComponent<CinemachineCamera>();
            m_VCam.Follow = m_Target;
            m_VCam.LookAt = m_Target;
            m_PositionComposer = m_VCam.gameObject.AddComponent<CinemachinePositionComposer>();
            m_RotationComposer = m_VCam.gameObject.AddComponent<CinemachineRotationComposer>();
            m_PositionComposer.Lookahead.Smoothing = m_RotationComposer.Lookahead.Smoothing = 0.3f;
            m_PositionComposer.Lookahead.Time = m_RotationComposer.Lookahead.Time = 10;
            m_PositionComposer.Lookahead.Enabled = m_RotationComposer.Lookahead.Enabled = true;

        }

        [UnityTest]
        public IEnumerator TargetsChanged()
        {
            Assert.That(m_VCam.FollowTargetChanged, Is.False);
            Assert.That(m_VCam.LookAtTargetChanged, Is.False);

            yield return null;
            Assert.That(m_VCam.FollowTargetChanged, Is.False);
            Assert.That(m_VCam.LookAtTargetChanged, Is.False);

            var newTarget = CreateGameObject("Target Object 2").transform;
            m_VCam.LookAt = newTarget;

            yield return null;

            Assert.That(m_VCam.FollowTargetChanged, Is.False);
            Assert.That(m_VCam.LookAtTargetChanged, Is.True);

            m_VCam.Follow = newTarget;

            yield return null;

            Assert.That(m_VCam.FollowTargetChanged, Is.True);
            Assert.That(m_VCam.LookAtTargetChanged, Is.False);

            m_VCam.Follow = m_Target;
            m_VCam.LookAt = m_Target;

            yield return null;

            Assert.That(m_VCam.FollowTargetChanged, Is.True);
            Assert.That(m_VCam.LookAtTargetChanged, Is.True);
        }

        [UnityTest]
        public IEnumerator LookaheadDelta()
        {
            var delta = m_RotationComposer.m_Predictor.PredictPositionDelta(m_RotationComposer.Lookahead.Time);
            Assert.That(delta.sqrMagnitude > 0, Is.False);

            delta = m_PositionComposer.m_Predictor.PredictPositionDelta(m_PositionComposer.Lookahead.Time);
            Assert.That(delta.sqrMagnitude > 0, Is.False);

            m_Target.Translate(10, 0, 0);
            yield return null;

            delta = m_RotationComposer.m_Predictor.PredictPositionDelta(m_RotationComposer.Lookahead.Time);
            Assert.That(delta.sqrMagnitude > 0, Is.True);

            delta = m_PositionComposer.m_Predictor.PredictPositionDelta(m_PositionComposer.Lookahead.Time);
            Assert.That(delta.sqrMagnitude > 0, Is.True);
        }
    }
}