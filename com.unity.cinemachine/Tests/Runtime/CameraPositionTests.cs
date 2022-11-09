using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Cinemachine;

namespace Tests.Runtime
{
    public class CameraPositionTests : CinemachineRuntimeFixtureBase
    {
        CmCamera m_Vcam;
        GameObject m_FollowObject;

        [SetUp]
        public override void SetUp()
        {
            CreateGameObject("MainCamera", typeof(Camera), typeof(CinemachineBrain));
            m_Vcam = CreateGameObject("CM Vcam", typeof(CmCamera)).GetComponent<CmCamera>();
            m_Vcam.Priority = 100;
            m_FollowObject = CreateGameObject("Follow Object");
            
            base.SetUp();
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
        public IEnumerator ThirdPerson()
        {
            m_Vcam.gameObject.AddComponent<CinemachineThirdPersonFollow>();
            m_Vcam.Follow = m_FollowObject.transform;
            m_FollowObject.transform.position += new Vector3(10, 0, 0);
            yield return null;
            Assert.That(m_Vcam.State.GetFinalPosition(), Is.EqualTo(m_FollowObject.transform.position).Using(m_Vector3EqualityComparer));
        }

        [UnityTest]
        public IEnumerator FramingTransposer()
        {
            var cameraDistance = 1f;
            var framingTransposer = m_Vcam.gameObject.AddComponent<CinemachinePositionComposer>();
            framingTransposer.Damping = Vector3.zero;
            framingTransposer.CameraDistance = cameraDistance;
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
        public IEnumerator OrbTransposer()
        {
            var orbitalTransposer = m_Vcam.gameObject.AddComponent<CinemachineOrbitalFollow>();
            orbitalTransposer.TrackerSettings.PositionDamping = Vector3.zero;
            orbitalTransposer.OrbitStyle = CinemachineOrbitalFollow.OrbitStyles.Sphere;
            orbitalTransposer.Radius = 0;
            m_Vcam.Follow = m_FollowObject.transform;
            m_FollowObject.transform.position += new Vector3(10, 0, 0);
            yield return null;
            Assert.That(m_Vcam.State.GetFinalPosition(), Is.EqualTo(m_FollowObject.transform.position).Using(m_Vector3EqualityComparer));
        }

        [UnityTest]
        public IEnumerator TrackedDolly()
        {
#pragma warning disable 618 // disable obsolete warning
            m_Vcam.gameObject.AddComponent<CinemachineTrackedDolly>();
#pragma warning restore 618
            m_Vcam.Follow = m_FollowObject.transform;
            var oldPos = m_Vcam.transform.position;
            m_FollowObject.transform.position += new Vector3(2, 2, 2);
            yield return null;
            Assert.That(m_Vcam.State.GetFinalPosition(), Is.EqualTo(oldPos).Using(m_Vector3EqualityComparer));
        }

        [UnityTest]
        public IEnumerator Follow()
        {
            var transposer = m_Vcam.gameObject.AddComponent<CinemachineFollow>();
            transposer.TrackerSettings.PositionDamping = Vector3.zero;
            transposer.FollowOffset = Vector3.zero;
            m_Vcam.Follow = m_FollowObject.transform;
            m_FollowObject.transform.position += new Vector3(10, 0, 0);
            yield return null;
            Assert.That(m_Vcam.State.GetFinalPosition(), Is.EqualTo(m_FollowObject.transform.position).Using(m_Vector3EqualityComparer));
        }
    }
}