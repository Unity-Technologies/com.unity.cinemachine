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
        
        static IEnumerable PriorityCases
        {
            get
            {
                yield return new TestCaseData(new[] {40, 30, 20, 10, 0, -10, -20, -30, -40}).SetName("Standard").Returns(null);
                yield return new TestCaseData(new[] {int.MaxValue, int.MaxValue / 2, 0, int.MinValue / 2, int.MinValue}).SetName("Edge-case limits").Returns(null);
            }
        }
        [UnityTest, TestCaseSource(nameof(PriorityCases))]
        public IEnumerator CheckPriorityOrder(int[] priorities)
        {
            yield return null;
            
            // Create vcams and set priorities
            for (var i = 0; i < priorities.Length; ++i)
            {
                m_CmCameras.Add(CreateGameObject("CM Vcam " + i, typeof(CmCamera)).GetComponent<CmCamera>());
                m_CmCameras[i].Priority = priorities[i];
            }

            // Check that active vcam is the current vcam. Then disable it and check that now the next is the active one.
            foreach (var cmCamera in m_CmCameras)
            {
                yield return null;
                
                var activeCamera = m_Brain.ActiveVirtualCamera as CmCamera;
                Assert.NotNull(activeCamera);
                Assert.That(activeCamera, Is.EqualTo(cmCamera));
                activeCamera.enabled = false;
            }
        }
    }
}