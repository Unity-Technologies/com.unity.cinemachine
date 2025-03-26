using System.Collections;
using NUnit.Framework;

namespace Unity.Cinemachine.Tests
{
    /// <summary>
    /// A class that handles creation and deletion of GameObjects, and gets set up for Time Invariant RunTime testing.
    /// </summary>
    public class CinemachineRuntimeTimeInvariantFixtureBase : CinemachineRuntimeFixtureBase
    {
        [SetUp]
        public override void SetUp()
        {
            base.SetUp();

            // Manual update is needed because when waiting for physics frame, we may pass 1-3 frames. Without manual
            // update the test won't be deterministic, because we would update 1-3 times, instead of just once.
            m_Brain.UpdateMethod = CinemachineBrain.UpdateMethods.ManualUpdate;
            CinemachineCore.CurrentTimeOverride = 0;
            CinemachineCore.CurrentUnscaledTimeTimeOverride = 0;
        }

        [TearDown]
        public override void TearDown()
        {
            CinemachineCore.CurrentTimeOverride = -1f;
            CinemachineCore.CurrentUnscaledTimeTimeOverride = -1f;

            base.TearDown();
        }

        /// <summary>Triggers manual update, increments cinemachine time, and waits one frame</summary>
        virtual protected IEnumerator UpdateCinemachine()
        {
            m_Brain.ManualUpdate();
            CinemachineCore.CurrentTimeOverride += CinemachineCore.UniformDeltaTimeOverride;
            CinemachineCore.CurrentUnscaledTimeTimeOverride += CinemachineCore.UniformDeltaTimeOverride;
            yield return null;
        }

        /// <summary>Waits for the t seconds.</summary>
        /// <param name="t">Time in seconds.</param>
        protected IEnumerator WaitForSeconds(float t)
        {
            var startTime = CinemachineCore.CurrentTimeOverride;
            while (CinemachineCore.CurrentTimeOverride - startTime <= t)
                yield return UpdateCinemachine();
        }

        protected static float CurrentTime => CinemachineCore.CurrentTime;
    }
}
