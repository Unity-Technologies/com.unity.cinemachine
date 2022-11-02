using System.Collections;
using Cinemachine;
using NUnit.Framework;
using UnityEngine;

namespace Tests.Runtime
{
    public class CinemachineRuntimeTimeInvariantFixtureBase : CinemachineRuntimeFixtureBase
    {
        protected CinemachineBrain m_Brain;
        [SetUp]
        public override void SetUp()
        {
            base.SetUp();
            
            var camera = CreateGameObject("MainCamera", typeof(Camera), typeof(CinemachineBrain));
            m_Brain = camera.GetComponent<CinemachineBrain>();
            
            // Manual update is needed because when waiting for physics frame, we may pass 1-3 frames. Without manual
            // update the test won't be deterministic, because we would update 1-3 times, instead of just once.
            m_Brain.UpdateMethod = CinemachineBrain.UpdateMethods.ManualUpdate;
            CinemachineCore.CurrentTimeOverride = 0;
            CinemachineCore.CurrentUnscaledTimeTimeOverride = 0;
        }
        
        [TearDown]
        public override void TearDown()
        {
            base.TearDown();
        }

        /// <summary>Triggers manual update, increments cinemachine time, and waits one frame</summary>
        protected IEnumerator UpdateCinemachine()
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
    }
}
