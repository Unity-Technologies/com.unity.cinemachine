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
        private CinemachineVirtualCamera vcam;
        private GameObject followObject;

        [SetUp]
        public override void SetUp()
        {
            CreateGameObject("MainCamera", typeof(Camera), typeof(CinemachineBrain));
            vcam = CreateGameObject("CM Vcam", typeof(CinemachineVirtualCamera)).GetComponent<CinemachineVirtualCamera>();
            vcam.Priority = 100;
            followObject = CreateGameObject("Follow Object");
            
            base.SetUp();
        }

        [UnityTest]
        public IEnumerator DoNothing()
        {
            vcam.Follow = followObject.transform;
            Vector3 oldPos = vcam.transform.position;
            followObject.transform.position += new Vector3(2, 2, 2);
            yield return null;
            Assert.That(vcam.State.FinalPosition, Is.EqualTo(oldPos).Using(Vector3EqualityComparer.Instance));
        }

        [UnityTest]
        public IEnumerator ThirdPerson()
        {
            vcam.AddCinemachineComponent<Cinemachine3rdPersonFollow>();
            vcam.Follow = followObject.transform;
            followObject.transform.position += new Vector3(10, 0, 0);
            yield return null;
            Assert.That(vcam.State.FinalPosition, Is.EqualTo(followObject.transform.position).Using(Vector3EqualityComparer.Instance));
        }

        [UnityTest]
        public IEnumerator FramingTransposer()
        {
            CinemachineFramingTransposer component = vcam.AddCinemachineComponent<CinemachineFramingTransposer>();
            component.m_XDamping = 0;
            component.m_YDamping = 0;
            component.m_ZDamping = 0;
            component.m_CameraDistance = 1f;
            vcam.Follow = followObject.transform;
            followObject.transform.position += new Vector3(10, 0, 0);
            yield return null;
            Assert.That(vcam.State.FinalPosition, Is.EqualTo(new Vector3(10, 0, -component.m_CameraDistance)).Using(Vector3EqualityComparer.Instance));
        }

        [UnityTest]
        public IEnumerator HardLockToTarget()
        {
            vcam.AddCinemachineComponent<CinemachineHardLockToTarget>();
            vcam.Follow = followObject.transform;
            followObject.transform.position += new Vector3(10, 0, 0);
            yield return null;
            Assert.That(vcam.State.FinalPosition, Is.EqualTo(followObject.transform.position).Using(Vector3EqualityComparer.Instance));
        }

        [UnityTest]
        public IEnumerator OrbTransposer()
        {
            CinemachineOrbitalTransposer component = vcam.AddCinemachineComponent<CinemachineOrbitalTransposer>();
            component.m_XDamping = 0;
            component.m_YDamping = 0;
            component.m_ZDamping = 0;
            component.m_FollowOffset = new Vector3(0, 0, 0);
            vcam.Follow = followObject.transform;
            followObject.transform.position += new Vector3(10, 0, 0);
            yield return null;
            Assert.That(vcam.State.FinalPosition, Is.EqualTo(followObject.transform.position).Using(Vector3EqualityComparer.Instance));
        }

        [UnityTest]
        public IEnumerator TrackedDolly()
        {
            vcam.AddCinemachineComponent<CinemachineTrackedDolly>();
            vcam.Follow = followObject.transform;
            Vector3 oldPos = vcam.transform.position;
            followObject.transform.position += new Vector3(2, 2, 2);
            yield return null;
            Assert.That(vcam.State.FinalPosition, Is.EqualTo(oldPos).Using(Vector3EqualityComparer.Instance));
        }

        [UnityTest]
        public IEnumerator Transposer()
        {
            CinemachineTransposer component = vcam.AddCinemachineComponent<CinemachineTransposer>();
            component.m_XDamping = 0;
            component.m_YDamping = 0;
            component.m_ZDamping = 0;
            component.m_FollowOffset = new Vector3(0, 0, 0);
            vcam.Follow = followObject.transform;
            followObject.transform.position += new Vector3(10, 0, 0);
            yield return null;
            Assert.That(vcam.State.FinalPosition, Is.EqualTo(followObject.transform.position).Using(Vector3EqualityComparer.Instance));
        }
    }
}