using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Cinemachine;

namespace Tests.Runtime
{
    [TestFixture]
    public class CameraPositionTests : CinemachineRuntimeFixtureBase
    {
        CinemachineCamera m_Vcam;
        GameObject m_FollowObject;

        [SetUp]
        public override void SetUp()
        {
            base.SetUp();
            
            m_Vcam = CreateGameObject("CM Vcam", typeof(CinemachineCamera)).GetComponent<CinemachineCamera>();
            m_Vcam.Priority = 100;
            m_FollowObject = CreateGameObject("Follow Object");
        }

        [UnityTest]
        public IEnumerator DoNothing()
        {
            m_Vcam.Follow = m_FollowObject.transform;
            var oldPos = m_Vcam.transform.position;
            m_FollowObject.transform.position += new Vector3(2, 2, 2);
            yield return null;
            Assert.That(m_Vcam.State.GetFinalPosition(), Is.EqualTo(oldPos).Using(m_Vector3EqualityComparer));
        }

        [UnityTest]
        public IEnumerator ThirdPersonFollow()
        {
            m_Vcam.gameObject.AddComponent<CinemachineThirdPersonFollow>();
            m_Vcam.Follow = m_FollowObject.transform;
            m_FollowObject.transform.position += new Vector3(10, 0, 0);
            yield return null;
            Assert.That(m_Vcam.State.GetFinalPosition(), Is.EqualTo(m_FollowObject.transform.position).Using(m_Vector3EqualityComparer));
        }

        [UnityTest]
        public IEnumerator PositionComposer()
        {
            var cameraDistance = 1f;
            var positionComposer = m_Vcam.gameObject.AddComponent<CinemachinePositionComposer>();
            positionComposer.Damping = Vector3.zero;
            positionComposer.CameraDistance = cameraDistance;
            m_Vcam.Follow = m_FollowObject.transform;
            m_FollowObject.transform.position += new Vector3(10, 0, 0);
            yield return null;
            Assert.That(m_Vcam.State.GetFinalPosition(), Is.EqualTo(new Vector3(10, 0, -cameraDistance)).Using(m_Vector3EqualityComparer));
        }

        [UnityTest]
        public IEnumerator HardLockToTarget()
        {
            m_Vcam.gameObject.AddComponent<CinemachineHardLockToTarget>();
            m_Vcam.Follow = m_FollowObject.transform;
            m_FollowObject.transform.position += new Vector3(10, 0, 0);
            yield return null;
            Assert.That(m_Vcam.State.GetFinalPosition(), Is.EqualTo(m_FollowObject.transform.position).Using(m_Vector3EqualityComparer));
        }

        [UnityTest]
        public IEnumerator OrbitalFollow()
        {
            var orbitalFollow = m_Vcam.gameObject.AddComponent<CinemachineOrbitalFollow>();
            orbitalFollow.TrackerSettings.PositionDamping = Vector3.zero;
            orbitalFollow.OrbitStyle = CinemachineOrbitalFollow.OrbitStyles.Sphere;
            orbitalFollow.Radius = 0;
            m_Vcam.Follow = m_FollowObject.transform;
            m_FollowObject.transform.position += new Vector3(10, 0, 0);
            yield return null;
            Assert.That(m_Vcam.State.GetFinalPosition(), Is.EqualTo(m_FollowObject.transform.position).Using(m_Vector3EqualityComparer));
        }

        [UnityTest]
        public IEnumerator Follow()
        {
            var follow = m_Vcam.gameObject.AddComponent<CinemachineFollow>();
            follow.TrackerSettings.PositionDamping = Vector3.zero;
            follow.FollowOffset = Vector3.zero;
            m_Vcam.Follow = m_FollowObject.transform;
            m_FollowObject.transform.position += new Vector3(10, 0, 0);
            yield return null;
            Assert.That(m_Vcam.State.GetFinalPosition(), Is.EqualTo(m_FollowObject.transform.position).Using(m_Vector3EqualityComparer));
        }
    }
}