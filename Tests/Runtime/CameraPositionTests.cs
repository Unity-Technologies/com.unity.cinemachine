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
        CinemachineNewVirtualCamera m_Vcam;
        GameObject m_FollowObject;

        [SetUp]
        public override void SetUp()
        {
            CreateGameObject("MainCamera", typeof(Camera), typeof(CinemachineBrain));
            m_Vcam = CreateGameObject("CM Vcam", typeof(CinemachineNewVirtualCamera)).GetComponent<CinemachineNewVirtualCamera>();
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
            m_Vcam.AddCinemachineComponent(new Cinemachine3rdPersonFollow());
            m_Vcam.Follow = m_FollowObject.transform;
            m_FollowObject.transform.position += new Vector3(10, 0, 0);
            yield return null;
            Assert.That(m_Vcam.State.FinalPosition, Is.EqualTo(m_FollowObject.transform.position).Using(Vector3EqualityComparer.Instance));
        }

        [UnityTest]
        public IEnumerator FramingTransposer()
        {
            var cameraDistance = 1f;
            m_Vcam.AddCinemachineComponent(new CinemachineFramingTransposer
            {
                m_XDamping = 0,
                m_YDamping = 0,
                m_ZDamping = 0,
                m_CameraDistance = cameraDistance,
            });
            m_Vcam.Follow = m_FollowObject.transform;
            m_FollowObject.transform.position += new Vector3(10, 0, 0);
            yield return null;
            Assert.That(m_Vcam.State.FinalPosition, Is.EqualTo(new Vector3(10, 0, -cameraDistance)).Using(Vector3EqualityComparer.Instance));
        }

        [UnityTest]
        public IEnumerator HardLockToTarget()
        {
            m_Vcam.AddCinemachineComponent(new CinemachineHardLockToTarget());
            m_Vcam.Follow = m_FollowObject.transform;
            m_FollowObject.transform.position += new Vector3(10, 0, 0);
            yield return null;
            Assert.That(m_Vcam.State.FinalPosition, Is.EqualTo(m_FollowObject.transform.position).Using(Vector3EqualityComparer.Instance));
        }

        [UnityTest]
        public IEnumerator OrbTransposer()
        {
            m_Vcam.AddCinemachineComponent(new CinemachineOrbitalTransposer
            {
                m_XDamping = 0,
                m_YDamping = 0,
                m_ZDamping = 0,
                m_FollowOffset = new Vector3(0, 0, 0)
            });
            m_Vcam.Follow = m_FollowObject.transform;
            m_FollowObject.transform.position += new Vector3(10, 0, 0);
            yield return null;
            Assert.That(m_Vcam.State.FinalPosition, Is.EqualTo(m_FollowObject.transform.position).Using(Vector3EqualityComparer.Instance));
        }

        [UnityTest]
        public IEnumerator TrackedDolly()
        {
            m_Vcam.AddCinemachineComponent(new CinemachineTrackedDolly());
            m_Vcam.Follow = m_FollowObject.transform;
            var oldPos = m_Vcam.transform.position;
            m_FollowObject.transform.position += new Vector3(2, 2, 2);
            yield return null;
            Assert.That(m_Vcam.State.FinalPosition, Is.EqualTo(oldPos).Using(Vector3EqualityComparer.Instance));
        }

        [UnityTest]
        public IEnumerator Transposer()
        {
            m_Vcam.AddCinemachineComponent(new CinemachineTransposer
            {
                m_XDamping = 0,
                m_YDamping = 0,
                m_ZDamping = 0,
                m_FollowOffset = new Vector3(0, 0, 0),
            });
            m_Vcam.Follow = m_FollowObject.transform;
            m_FollowObject.transform.position += new Vector3(10, 0, 0);
            yield return null;
            Assert.That(m_Vcam.State.FinalPosition, Is.EqualTo(m_FollowObject.transform.position).Using(Vector3EqualityComparer.Instance));
        }
    }
}