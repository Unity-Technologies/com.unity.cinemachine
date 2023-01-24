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
        [SetUp]
        public override void SetUp()
        {
            base.SetUp();
        }

        [TearDown]
        public override void TearDown()
        {
            base.TearDown();
        }
        
        static IEnumerable PriorityValues
        {
            // Values must be in descending order - just so CheckPriorityOrder algorithm is simpler
            get
            {
                yield return new TestCaseData(new[] {40, 30, 20, 10, 0, -10, -20, -30, -40}).SetName("Standard").Returns(null);
                yield return new TestCaseData(new[] {int.MaxValue, int.MaxValue / 2, 0, int.MinValue / 2, int.MinValue}).SetName("Edge-case limits").Returns(null);
            }
        }
        [UnityTest, TestCaseSource(nameof(PriorityValues))]
        public IEnumerator CheckPriorityOrder(int[] priorities)
        {
            var cmCameras = new List<CinemachineCamera>();
            // Create vcams and set priorities
            for (var i = 0; i < priorities.Length; ++i)
            {
                cmCameras.Add(CreateGameObject("CM Vcam " + i, typeof(CinemachineCamera)).GetComponent<CinemachineCamera>());
                cmCameras[i].Priority = priorities[i];
            }
            yield return null;

            CinemachineCamera activeCamera;
            // Check that activeCamera is equal to cmCamera.
            // Then disable it and check that the next is now the active one.
            foreach (var cmCamera in cmCameras)
            {
                activeCamera = m_Brain.ActiveVirtualCamera as CinemachineCamera;
                Assert.NotNull(activeCamera);
                Assert.That(activeCamera, Is.EqualTo(cmCamera));
                activeCamera.enabled = false;
                yield return null;
            }

            // Re-enable all cm cameras
            foreach (var cmCamera in cmCameras)
                cmCamera.enabled = true;
            
            yield return null;
                
            // Check that the first CinemachineCamera in the list is equal to activeCamera.
            activeCamera = m_Brain.ActiveVirtualCamera as CinemachineCamera;
            Assert.NotNull(activeCamera);
            Assert.That(activeCamera, Is.EqualTo(cmCameras[0]));
        }
    }
}