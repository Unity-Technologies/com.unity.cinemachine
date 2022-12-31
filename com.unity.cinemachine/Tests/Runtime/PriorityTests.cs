using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Cinemachine;

namespace Tests.Runtime
{
    [TestFixture]
    public class PriorityTests : CinemachineRuntimeFixtureBase
    {
        List<CmCamera> m_CmCameras;
        [SetUp]
        public override void SetUp()
        {
            base.SetUp();
            
            m_CmCameras = new List<CmCamera>();
        }

        [TearDown]
        public override void TearDown()
        {
            m_CmCameras.Clear();
            
            base.TearDown();
        }
        
        static IEnumerable PriorityValues
        {
            // Values must be in descending order
            get
            {
                yield return new TestCaseData(new[] {40, 30, 20, 10, 0, -10, -20, -30, -40}).SetName("Standard").Returns(null);
                yield return new TestCaseData(new[] {int.MaxValue, int.MaxValue / 2, 0, int.MinValue / 2, int.MinValue}).SetName("Edge-case limits").Returns(null);
            }
        }
        [UnityTest, TestCaseSource(nameof(PriorityValues))]
        public IEnumerator CheckPriorityOrder(int[] priorities)
        {
            // Create vcams and set priorities
            for (var i = 0; i < priorities.Length; ++i)
            {
                m_CmCameras.Add(CreateGameObject("CM Vcam " + i, typeof(CmCamera)).GetComponent<CmCamera>());
                m_CmCameras[i].Priority = priorities[i];
            }
            yield return null;

            CmCamera activeCamera;
            // Check that activeCamera is equal to cmCamera.
            // Then disable it and check that the next is now the active one.
            foreach (var cmCamera in m_CmCameras)
            {
                activeCamera = m_Brain.ActiveVirtualCamera as CmCamera;
                Assert.NotNull(activeCamera);
                Assert.That(activeCamera, Is.EqualTo(cmCamera));
                activeCamera.enabled = false;
                yield return null;
            }

            // Re-enable all cm cameras
            foreach (var cmCamera in m_CmCameras)
            {
                cmCamera.enabled = true;
            }
            yield return null;
                
            // Check that the first CmCamera in the list is equal to activeCamera.
            activeCamera = m_Brain.ActiveVirtualCamera as CmCamera;
            Assert.NotNull(activeCamera);
            Assert.That(activeCamera, Is.EqualTo(m_CmCameras[0]));
        }
    }
}