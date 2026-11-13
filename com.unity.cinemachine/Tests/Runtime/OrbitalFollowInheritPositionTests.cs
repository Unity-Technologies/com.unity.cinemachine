using System.Collections;
using NUnit.Framework;
using Unity.Cinemachine.TargetTracking;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.TestTools.Utils;

namespace Unity.Cinemachine.Tests
{
    [TestFixture]
    public class OrbitalFollowInheritPositionTests : CinemachineRuntimeTimeInvariantFixtureBase
    {
        CinemachineCamera m_BaseCam;
        CinemachineOrbitalFollow m_BaseCamOrbital;
        CinemachineCamera m_FarCam;
        GameObject m_FollowTarget;

        [SetUp]
        public override void SetUp()
        {
            base.SetUp();

            m_FollowTarget = CreateGameObject("FollowTarget");
            m_FollowTarget.transform.position = Vector3.zero;

            // BaseCam: orbital, orbiting at 90 degrees horizontal (camera to the side of target),
            // with InheritPosition blend hint and non-zero position damping.
            var baseCamGo = CreateGameObject("BaseCam", typeof(CinemachineCamera));
            m_BaseCam = baseCamGo.GetComponent<CinemachineCamera>();
            m_BaseCamOrbital = baseCamGo.AddComponent<CinemachineOrbitalFollow>();
            m_BaseCam.Priority.Value = 1;
            m_BaseCam.Target.TrackingTarget = m_FollowTarget.transform;
            m_BaseCam.BlendHint = CinemachineCore.BlendHints.InheritPosition;
            m_BaseCamOrbital.OrbitStyle = CinemachineOrbitalFollow.OrbitStyles.Sphere;
            m_BaseCamOrbital.Radius = 5f;
            m_BaseCamOrbital.HorizontalAxis.Value = 90f;
            m_BaseCamOrbital.TrackerSettings.PositionDamping = Vector3.one;

            var farCamGo = CreateGameObject("FarCam", typeof(CinemachineCamera));
            m_FarCam = farCamGo.GetComponent<CinemachineCamera>();
            m_FarCam.Priority.Value = 2; // wins when enabled
            m_FarCam.transform.position = new Vector3(0, 0, -15);
            m_FarCam.enabled = false;
        }

        [UnityTest]
        [Description("UUM-149754: OrbitalFollow with InheritPosition + position damping should not jerk on blend-back.")]
        public IEnumerator InheritPosition_WithPositionDamping_CameraStartsAtInheritedPosition()
        {
            m_Brain.DefaultBlend = new CinemachineBlendDefinition(CinemachineBlendDefinition.Styles.Cut, 0);
            CinemachineCore.UniformDeltaTimeOverride = 0.0f;

            yield return UpdateCinemachine();
            yield return UpdateCinemachine();
            Assume.That(ReferenceEquals(m_Brain.ActiveVirtualCamera, m_BaseCam), "BaseCam should start live.");
            Assume.That(m_BaseCamOrbital.HorizontalAxis.Value, Is.EqualTo(90f).Using(new FloatEqualityComparer(0.5f)),
                "BaseCam should be settled at its own orbit angle before the switch.");

            m_FarCam.enabled = true;
            yield return UpdateCinemachine();
            yield return UpdateCinemachine();
            Assume.That(ReferenceEquals(m_Brain.ActiveVirtualCamera, m_FarCam), "FarCam should be live after enabling.");

            m_FarCam.enabled = false;
            yield return UpdateCinemachine();
            Assume.That(ReferenceEquals(m_Brain.ActiveVirtualCamera, m_BaseCam), "BaseCam should be live again.");

            Assert.That(
                m_BaseCamOrbital.HorizontalAxis.Value,
                Is.EqualTo(0f).Using(new FloatEqualityComparer(0.5f)),
                "Orbit horizontal axis should be inherited from FarCam's direction, not BaseCam's own angle.");
        }

        [UnityTest]
        [Description("UUM-149754: LazyFollow + position damping should keep the inherited direction, not revert.")]
        public IEnumerator InheritPosition_LazyFollowWithPositionDamping_KeepsInheritedDirection()
        {
            m_Brain.DefaultBlend =
                new CinemachineBlendDefinition(CinemachineBlendDefinition.Styles.Cut, 0);

            CinemachineCore.UniformDeltaTimeOverride = 0.1f;
            m_BaseCamOrbital.TrackerSettings.BindingMode = BindingMode.LazyFollow;

            m_FarCam.transform.position = new Vector3(15, 0, 0);

            yield return UpdateCinemachine();
            yield return UpdateCinemachine();
            Assume.That(ReferenceEquals(m_Brain.ActiveVirtualCamera, m_BaseCam), "BaseCam should start live.");
            var originalDirection = DirectionFromTarget(m_Brain.State.GetFinalPosition());

            m_FarCam.enabled = true;
            yield return UpdateCinemachine();
            yield return UpdateCinemachine();
            Assume.That(ReferenceEquals(m_Brain.ActiveVirtualCamera, m_FarCam), "FarCam should be live after enabling.");
            var inheritedDirection = DirectionFromTarget(m_FarCam.State.GetFinalPosition());
            Assume.That(Vector3.Dot(originalDirection, inheritedDirection), Is.LessThan(0.5f),
                "The two cameras must sit in clearly different directions for this test to mean anything.");

            m_FarCam.enabled = false;
            yield return UpdateCinemachine();
            Assume.That(ReferenceEquals(m_Brain.ActiveVirtualCamera, m_BaseCam), "BaseCam should be live again.");
            for (var i = 0; i < 30; ++i)
                yield return UpdateCinemachine();

            Assert.That(
                DirectionFromTarget(m_Brain.State.GetFinalPosition()),
                Is.EqualTo(inheritedDirection).Using(new Vector3EqualityComparer(0.05f)),
                "Camera should hold FarCam's inherited direction, not revert to its own side.");
        }

        Vector3 DirectionFromTarget(Vector3 position)
            => (position - m_FollowTarget.transform.position).normalized;
    }
}
