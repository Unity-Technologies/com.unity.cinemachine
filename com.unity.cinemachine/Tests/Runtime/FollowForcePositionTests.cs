using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Unity.Cinemachine;

namespace Unity.Cinemachine.Tests
{
    [TestFixture]
    public class FollowForcePositionTests : CinemachineRuntimeFixtureBase
    {
        CinemachineCamera m_Vcam;
        GameObject m_FollowObject;

        [SetUp]
        public override void SetUp()
        {
            base.SetUp();

            m_Vcam = CreateGameObject("CM Vcam", typeof(CinemachineCamera)).GetComponent<CinemachineCamera>();
            m_FollowObject = CreateGameObject("Follow Object");
        }

        [UnityTest, Description("UUM-131870: ForceCameraPosition should allow looking away from the target.")]
        public IEnumerator LookAwayFromTarget()
        {
            CinemachineCore.UniformDeltaTimeOverride = 0.0f; // we only test force position, not damping
            var follow = m_Vcam.gameObject.AddComponent<CinemachineFollow>();
            follow.FollowOffset = new Vector3(0, 0, -10);
            m_Vcam.Follow = m_FollowObject.transform;

            // Initial update to settle camera
            m_FollowObject.transform.position = Vector3.zero;
            yield return null;
            Assume.That(m_Vcam.State.GetFinalPosition(), Is.EqualTo(follow.FollowOffset).Using(m_Vector3EqualityComparer));

            // Move camera away from target
            var forcedPos = new Vector3(0, 5, -5);
            var forcedRot = Quaternion.identity;
            m_Vcam.ForceCameraPosition(forcedPos, forcedRot);
            yield return null;
            Assert.That(m_Vcam.State.GetFinalPosition(), Is.EqualTo(forcedPos).Using(m_Vector3EqualityComparer));
            Assert.That(m_Vcam.State.GetFinalOrientation(), Is.EqualTo(forcedRot).Using(m_QuaternionComparer));
        }
    }
}
