using System;
using System.Collections;
using NUnit.Framework;
using Unity.Cinemachine.TargetTracking;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.TestTools.Utils;

namespace Unity.Cinemachine.Tests
{
    /// <summary>
    /// ForceCameraPosition with damping active, and with LazyFollow: neither is covered by
    /// FollowForcePositionTests or FreelookForcePositionTests.
    /// </summary>
    [TestFixture]
    public class ForcePositionContractTests : CinemachineRuntimeTimeInvariantFixtureBase
    {
        const float k_Radius = 5f;
        const int k_SettleFrames = 60;

        CinemachineCamera m_Cam;
        CinemachineOrbitalFollow m_Orbital;
        GameObject m_FollowTarget;

        static readonly Array k_BindingModes = Enum.GetValues(typeof(BindingMode));

        [SetUp]
        public override void SetUp()
        {
            base.SetUp();

            m_FollowTarget = CreateGameObject("FollowTarget");
            m_FollowTarget.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);

            var camGo = CreateGameObject("CM Orbital", typeof(CinemachineCamera));
            m_Cam = camGo.GetComponent<CinemachineCamera>();
            m_Orbital = camGo.AddComponent<CinemachineOrbitalFollow>();
            m_Cam.Target.TrackingTarget = m_FollowTarget.transform;
            m_Orbital.OrbitStyle = CinemachineOrbitalFollow.OrbitStyles.Sphere;
            m_Orbital.Radius = k_Radius;

            // The point of this fixture: damping is ON.
            m_Orbital.TrackerSettings.PositionDamping = Vector3.one;
            m_Orbital.TrackerSettings.RotationDamping = Vector3.one;
        }

        [UnityTest, TestCaseSource(nameof(BindingModes))]
        [Description("Warp intent: camera and target teleport together by the same delta. The camera-relative "
            + "tracker anchor must survive, so damping continues undisturbed and nothing pops.")]
        public IEnumerator ForceCameraPosition_WarpsWithTarget_NoPop(BindingMode bindingMode)
        {
            m_Orbital.TrackerSettings.BindingMode = bindingMode;
            yield return Settle();

            var startPos = m_Cam.State.GetFinalPosition();
            var startOffset = startPos - m_FollowTarget.transform.position;
            var delta = new Vector3(100f, 0f, 50f);

            m_FollowTarget.transform.position += delta;
            m_Cam.ForceCameraPosition(startPos + delta, m_Cam.State.GetFinalOrientation());
            yield return UpdateCinemachine();

            Assert.That(m_Cam.State.GetFinalPosition(), Is.EqualTo(startPos + delta).Using(new Vector3EqualityComparer(0.01f)),
                $"{bindingMode}: warping camera and target together moved the camera relative to its target.");

            // The offset to the target is what the warp has to preserve: it is the thing damping works on.
            yield return Settle();
            Assert.That(m_Cam.State.GetFinalPosition() - m_FollowTarget.transform.position,
                Is.EqualTo(startOffset).Using(new Vector3EqualityComparer(0.01f)),
                $"{bindingMode}: camera settled to a different offset after the warp.");
        }

        [UnityTest]
        [Description("UUM-149754: an InheritPosition transition inherits the outgoing camera's direction but "
            + "not its distance - the camera lands on its own orbit radius. Pins the documented behaviour.")]
        public IEnumerator InheritPosition_InheritsDirectionButNotDistance()
        {
            m_Orbital.TrackerSettings.BindingMode = BindingMode.WorldSpace; // the default binding mode
            m_Cam.BlendHint = CinemachineCore.BlendHints.InheritPosition;
            m_Cam.Priority.Value = 1;
            m_Brain.DefaultBlend = new CinemachineBlendDefinition(CinemachineBlendDefinition.Styles.Cut, 0);

            // Outgoing camera sits much further away than the orbit radius, in a clearly different direction.
            var farCamGo = CreateGameObject("FarCam", typeof(CinemachineCamera));
            var farCam = farCamGo.GetComponent<CinemachineCamera>();
            farCam.Priority.Value = 2;
            farCamGo.transform.position = new Vector3(0f, 0f, 15f); // opposite side from the orbit's resting spot
            farCam.enabled = false;

            yield return Settle();
            Assume.That(ReferenceEquals(m_Brain.ActiveVirtualCamera, m_Cam), "Orbital camera should start live.");
            var ownDirection = DirectionFromTarget(m_Cam.State.GetFinalPosition());

            farCam.enabled = true;
            yield return UpdateCinemachine();
            yield return UpdateCinemachine();
            Assume.That(ReferenceEquals(m_Brain.ActiveVirtualCamera, farCam), "FarCam should be live.");
            var inheritedDirection = DirectionFromTarget(farCam.State.GetFinalPosition());
            Assume.That(Vector3.Dot(ownDirection, inheritedDirection), Is.LessThan(0.9f),
                "The two cameras must sit in clearly different directions for this test to mean anything.");

            farCam.enabled = false;
            yield return UpdateCinemachine();
            Assume.That(ReferenceEquals(m_Brain.ActiveVirtualCamera, m_Cam), "Orbital camera should be live again.");

            var landedPos = m_Brain.State.GetFinalPosition();

            // Direction is inherited from the outgoing camera...
            Assert.That(Vector3.Dot(DirectionFromTarget(landedPos), inheritedDirection), Is.GreaterThan(0.99f),
                "InheritPosition did not inherit the outgoing camera's direction.");

            // ...but distance is not: axis inference deliberately leaves the radial axis alone, so the camera
            // lands on its own orbit radius rather than at the outgoing camera's distance.
            Assert.That(Vector3.Distance(landedPos, m_FollowTarget.transform.position),
                Is.EqualTo(k_Radius).Within(0.01f),
                "InheritPosition should place the camera at its own orbit radius, not the inherited distance.");
        }

        static IEnumerable BindingModes
        {
            get
            {
                foreach (BindingMode mode in k_BindingModes)
                    yield return new TestCaseData(mode).SetName($"Warp-{mode}").Returns(null);
            }
        }

        Vector3 DirectionFromTarget(Vector3 worldPos)
            => (worldPos - m_FollowTarget.transform.position).normalized;

        IEnumerator Settle(int frames = k_SettleFrames)
        {
            for (var i = 0; i < frames; ++i)
                yield return UpdateCinemachine();
        }
    }
}
