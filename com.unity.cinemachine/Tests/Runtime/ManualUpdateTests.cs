using System.Collections;
using NUnit.Framework;
using UnityEngine.TestTools;

namespace Unity.Cinemachine.Tests
{
    [TestFixture]
    public class ManualUpdateTests : CinemachineRuntimeTimeInvariantFixtureBase
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
            m_vcam = CreateGameObject("vcam", typeof(CinemachineCamera)).GetComponent<CinemachineCamera>();
            m_TestCounter = m_vcam.gameObject.AddComponent<UpdateCounterForTests>();
        }

        [UnityTest]
        public IEnumerator ManualUpdateRespectsFrameCount()
        {
            // Check that source vcam is active
            yield return UpdateCinemachine();
            Assert.That(ReferenceEquals(m_Brain.ActiveVirtualCamera, m_vcam));

            // Update should do something if we call it exactly once per frame
            m_TestCounter.UpdateCount = 0;
            for (int i = 0; i < 5; ++i)
            {
                Assert.AreEqual(m_TestCounter.UpdateCount, i);
                yield return UpdateCinemachine(i);
            }

            // Multiple calls to ManualUpdate within the same frame should have no effect (other than to waste time)
            m_TestCounter.UpdateCount = 0;
            for (int i = 0; i < 5; ++i)
            {
                Assert.AreEqual(m_TestCounter.UpdateCount, i);
                yield return UpdateCinemachine(i);
                yield return UpdateCinemachine(i);
            }
        }
    }
}
