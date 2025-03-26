using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Unity.Cinemachine.Tests
{
    [TestFixture]
    public class ManualUpdateTests : CinemachineRuntimeFixtureBase
    {
        CinemachineVirtualCameraBase m_vcam;
        UpdateCounterForTests m_TestCounter;

        /// <summary>Triggers manual update, increments cinemachine time, and waits one frame</summary>
        virtual protected IEnumerator UpdateCinemachine(int frameCount)
        {
            m_Brain.ManualUpdate(frameCount, CinemachineCore.UniformDeltaTimeOverride);
            CinemachineCore.CurrentTimeOverride += CinemachineCore.UniformDeltaTimeOverride;
            CinemachineCore.CurrentUnscaledTimeTimeOverride += CinemachineCore.UniformDeltaTimeOverride;
            yield return null;
        }

        [SetUp]
        public override void SetUp()
        {
            base.SetUp();
            m_Brain.UpdateMethod = CinemachineBrain.UpdateMethods.ManualUpdate;
            m_vcam = CreateGameObject("vcam", typeof(CinemachineCamera)).GetComponent<CinemachineCamera>();
            m_TestCounter = m_vcam.gameObject.AddComponent<UpdateCounterForTests>();
        }

        [UnityTest]
        public IEnumerator ManualUpdateRespectsFrameCount()
        {
            // Check that source vcam is active
            int currentFrame = 0;
            yield return UpdateCinemachine(currentFrame++);
            Assert.That(ReferenceEquals(m_Brain.ActiveVirtualCamera, m_vcam));

            // Update should do something if we call it exactly once per frame
            m_TestCounter.UpdateCount = 0;
            for (int i = 0; i < 5; ++i)
            {
                Assert.AreEqual(i, m_TestCounter.UpdateCount, "Single call to ManualUpdate per frame, update count does not match");
                yield return UpdateCinemachine(currentFrame++);
            }

            // Multiple calls to ManualUpdate within the same frame should have no effect (other than to waste time)
            m_TestCounter.UpdateCount = 0;
            for (int i = 0; i < 5; ++i)
            {
                Assert.AreEqual(i, m_TestCounter.UpdateCount, "Multiple calls to ManualUpdate per frame, update count does not match");
                m_Brain.ManualUpdate(currentFrame, CinemachineCore.UniformDeltaTimeOverride); // spurious call to ManualUpdate
                yield return UpdateCinemachine(currentFrame++);
            }
        }
    }
}
