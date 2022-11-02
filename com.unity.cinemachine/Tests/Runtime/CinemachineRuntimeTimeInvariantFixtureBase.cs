using System.Collections;
using Cinemachine;
using UnityEngine;

namespace Tests.Runtime
{
    public class CinemachineRuntimeTimeInvariantFixtureBase : CinemachineRuntimeFixtureBase
    {
        protected CinemachineBrain m_Brain;
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

        /// <summary>Triggers manual update and increments cinemachine time.</summary>
        protected void UpdateCinemachine()
        {
            m_Brain.ManualUpdate();
            CinemachineCore.CurrentTimeOverride += CinemachineCore.UniformDeltaTimeOverride;
            CinemachineCore.CurrentUnscaledTimeTimeOverride += CinemachineCore.UniformDeltaTimeOverride;
        }
        
        /// <summary>Waits for the t seconds.</summary>
        /// <param name="t">Time in seconds.</param>
        protected IEnumerator WaitForSeconds(float t)
        {
            var timer = 0f;
            while (timer <= t)
            {
                UpdateCinemachine();
                timer += CinemachineCore.UniformDeltaTimeOverride;
                yield return null;
            } 
        }
        
        /// <summary>Ensures to wait until at least one physics frame.</summary>
        protected static IEnumerator WaitForOnePhysicsFrame()
        {
            yield return new WaitForFixedUpdate(); // this is needed to ensure physics system is up-to-date
            yield return null; 
        }
    }
}
