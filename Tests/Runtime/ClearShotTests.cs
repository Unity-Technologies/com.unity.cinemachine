using System.Collections;
using NUnit.Framework;
using UnityEngine;
using Cinemachine;
using UnityEngine.TestTools;

namespace Tests.Runtime
{
    [TestFixture]
    public class ClearShotTests : CinemachineFixtureBase
    {
        private GameObject _character;
        private CinemachineClearShot _clearShot;
        private CinemachineVirtualCamera _vcam1;
        private CinemachineVirtualCamera _vcam2;
        
        [SetUp]
        public override void SetUp()
        {
            // a basic "character" to use as a lookat
            _character = CreateGameObject("Character");
            _character.transform.position = new Vector3(100, 0, 1);

            // main camera
            CreateGameObject("Camera", typeof(Camera), typeof(CinemachineBrain));
            
            // a ClearShot camera
            var clearShotHolder = CreateGameObject("CM ClearShot", typeof(CinemachineClearShot), typeof(CinemachineCollider));
            _clearShot = clearShotHolder.GetComponent<CinemachineClearShot>();
            _clearShot.LookAt = _character.transform;
            var clearShotCollider = clearShotHolder.GetComponent<CinemachineCollider>();
            clearShotCollider.m_MinimumDistanceFromTarget = 0.1f;

            // a stationary vcam1 with a hard lookat
            var vcam1Holder = CreateGameObject("CM Vcam1", typeof(CinemachineVirtualCamera));
            vcam1Holder.transform.SetParent(clearShotHolder.transform);
            vcam1Holder.transform.position = new Vector3(0, 0, 8); 
            _vcam1 = vcam1Holder.GetComponent<CinemachineVirtualCamera>();
            _vcam1.AddCinemachineComponent<CinemachineHardLookAt>();
            _vcam1.Priority = 20;
            
            // a completely locked vcam2
            var vcam2Holder = CreateGameObject("CM Vcam2", typeof(CinemachineVirtualCamera));
            vcam2Holder.transform.SetParent(clearShotHolder.transform);
            vcam2Holder.transform.position = new Vector3(0, 0, -2);
            _vcam2 = vcam2Holder.GetComponent<CinemachineVirtualCamera>();
            _vcam2.Priority = 10;

            // a "wall" composed of a single quad that partially obscures vcam1, but not vcam2
            var wall = CreatePrimitive(PrimitiveType.Quad);
            wall.transform.SetPositionAndRotation(new Vector3(0, 0, 4), Quaternion.Euler(0, 180, 0));
            wall.transform.localScale = new Vector3(2, 2, 2);

            base.SetUp();
        }

        private static IEnumerable ClearShotTestCases
        {
            get
            {
                yield return new TestCaseData(new Vector3(100, 0, 1), "CM Vcam1").Returns(null);
                yield return new TestCaseData(new Vector3(5, 0, 1), "CM Vcam1").Returns(null);
                yield return new TestCaseData(new Vector3(0, 0, 1), "CM Vcam2").Returns(null);
                yield return new TestCaseData(new Vector3(-5, 0, 1), "CM Vcam1").Returns(null);
                yield return new TestCaseData(new Vector3(-100, 0, 1), "CM Vcam1").Returns(null);
            }
        }

        [UnityTest, TestCaseSource(nameof(ClearShotTestCases))]
        public IEnumerator TestClearShotSwitchesCameras(Vector3 characterPosition, string expectedVcamName)
        {
            _character.transform.position = characterPosition;
            yield return null;

            Assert.That(_clearShot.LiveChild.Name, Is.EqualTo(expectedVcamName));
        }
    }
}
