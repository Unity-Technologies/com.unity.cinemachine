#if CINEMACHINE_PHYSICS
using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Unity.Cinemachine.Tests
{
    [TestFixture]
    public class ClearShotTests : CinemachineRuntimeFixtureBase
    {
        GameObject m_Character;
        CinemachineClearShot m_ClearShot;
        CinemachineCamera m_Vcam1;
        CinemachineCamera m_Vcam2;
        
        [SetUp]
        public override void SetUp()
        {
            base.SetUp();
            
            // a basic "character" to use as a lookat
            m_Character = CreateGameObject("Character");
            m_Character.transform.position = new Vector3(10, 0, 1);

            // a ClearShot camera
            var clearShotHolder = CreateGameObject("CM ClearShot", typeof(CinemachineClearShot), typeof(CinemachineDeoccluder));
            m_ClearShot = clearShotHolder.GetComponent<CinemachineClearShot>();
            m_ClearShot.LookAt = m_Character.transform;
            var clearShotCollider = clearShotHolder.GetComponent<CinemachineDeoccluder>();
            clearShotCollider.MinimumDistanceFromTarget = 0.1f;

            // a stationary vcam1 with a hard lookat
            var vcam1Holder = CreateGameObject("CM Vcam1", typeof(CinemachineCamera));
            vcam1Holder.transform.SetParent(clearShotHolder.transform);
            vcam1Holder.transform.position = new Vector3(0, 0, 8); 
            m_Vcam1 = vcam1Holder.GetComponent<CinemachineCamera>();
            m_Vcam1.gameObject.AddComponent<CinemachineHardLookAt>();
            m_Vcam1.Priority = 20;
            
            // a completely locked vcam2
            var vcam2Holder = CreateGameObject("CM Vcam2", typeof(CinemachineCamera));
            vcam2Holder.transform.SetParent(clearShotHolder.transform);
            vcam2Holder.transform.position = new Vector3(0, 0, -2);
            m_Vcam2 = vcam2Holder.GetComponent<CinemachineCamera>();
            m_Vcam2.Priority = 10;

            // a "wall" composed of a single quad that partially obscures vcam1, but not vcam2
            var wall = CreatePrimitive(PrimitiveType.Quad);
            wall.transform.SetPositionAndRotation(new Vector3(0, 0, 4), Quaternion.Euler(0, 180, 0));
            wall.transform.localScale = new Vector3(2, 2, 2);
        }

        static IEnumerable ClearShotTestCases
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
            m_Character.transform.position = characterPosition;

            yield return new WaitForSeconds(0.5f);

            Assert.That(m_ClearShot.LiveChild.Name, Is.EqualTo(expectedVcamName));
        }
    }
}
#endif
