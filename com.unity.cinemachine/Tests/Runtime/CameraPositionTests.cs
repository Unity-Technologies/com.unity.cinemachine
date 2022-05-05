using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Cinemachine;
using UnityEngine.TestTools.Utils;

namespace Tests.Runtime
{
    public class CameraPositionTests : CinemachineFixtureBase
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
            Assert.That(m_Vcam.State.FinalPosition, Is.EqualTo(oldPos).Using(Vector3EqualityComparer.Instance));
        }

        [UnityTest]
        public IEnumerator ThirdPerson()
        {
            m_Vcam.gameObject.AddComponent<Cinemachine3rdPersonFollow>();
            m_Vcam.Follow = m_FollowObject.transform;
            m_FollowObject.transform.position += new Vector3(10, 0, 0);
            yield return null;
            Assert.That(m_Vcam.State.FinalPosition, Is.EqualTo(m_FollowObject.transform.position).Using(Vector3EqualityComparer.Instance));
        }

        [UnityTest]
        public IEnumerator FramingTransposer()
        {
            var cameraDistance = 1f;
            var framingTransposer = m_Vcam.gameObject.AddComponent<CinemachineFramingTransposer>();
            framingTransposer.m_XDamping = 0;
            framingTransposer.m_YDamping = 0;
            framingTransposer.m_ZDamping = 0;
            framingTransposer.m_CameraDistance = cameraDistance;
            m_Vcam.Follow = m_FollowObject.transform;
            m_FollowObject.transform.position += new Vector3(10, 0, 0);
            yield return null;
            Assert.That(m_Vcam.State.FinalPosition, Is.EqualTo(new Vector3(10, 0, -cameraDistance)).Using(Vector3EqualityComparer.Instance));
        }

        [UnityTest]
        public IEnumerator HardLockToTarget()
        {
            m_Vcam.gameObject.AddComponent<CinemachineHardLockToTarget>();
            m_Vcam.Follow = m_FollowObject.transform;
            m_FollowObject.transform.position += new Vector3(10, 0, 0);
            yield return null;
            Assert.That(m_Vcam.State.FinalPosition, Is.EqualTo(m_FollowObject.transform.position).Using(Vector3EqualityComparer.Instance));
        }

        [UnityTest]
        public IEnumerator OrbTransposer()
        {
            var orbitalTransposer = m_Vcam.gameObject.AddComponent<CinemachineOrbitalTransposer>();
            orbitalTransposer.m_XDamping = 0;
            orbitalTransposer.m_YDamping = 0;
            orbitalTransposer.m_ZDamping = 0;
            orbitalTransposer.m_FollowOffset = Vector3.zero;
            m_Vcam.Follow = m_FollowObject.transform;
            m_FollowObject.transform.position += new Vector3(10, 0, 0);
            yield return null;
            Assert.That(m_Vcam.State.FinalPosition, Is.EqualTo(m_FollowObject.transform.position).Using(Vector3EqualityComparer.Instance));
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
            Assert.That(m_Vcam.State.FinalPosition, Is.EqualTo(oldPos).Using(Vector3EqualityComparer.Instance));
        }

        [UnityTest]
        public IEnumerator Transposer()
        {
            var transposer = m_Vcam.gameObject.AddComponent<CinemachineTransposer>();
            transposer.m_XDamping = 0;
            transposer.m_YDamping = 0;
            transposer.m_ZDamping = 0;
            transposer.m_FollowOffset = Vector3.zero;
            m_Vcam.Follow = m_FollowObject.transform;
            m_FollowObject.transform.position += new Vector3(10, 0, 0);
            yield return null;
            Assert.That(m_Vcam.State.FinalPosition, Is.EqualTo(m_FollowObject.transform.position).Using(Vector3EqualityComparer.Instance));
        }
    }
}