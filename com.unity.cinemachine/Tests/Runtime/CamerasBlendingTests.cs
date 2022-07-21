using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Cinemachine;

namespace Tests.Runtime
{
    [TestFixture]
    public class CamerasBlendingTests : CinemachineFixtureBase
    {
        const float k_BlendingTime = 1;

        CinemachineBrain m_Brain;
        CinemachineVirtualCameraBase m_Source, m_Target;

        [SetUp]
        public override void SetUp()
        {
            // Camera
            var cameraHolder = CreateGameObject("MainCamera", typeof(Camera), typeof(CinemachineBrain));
            m_Brain = cameraHolder.GetComponent<CinemachineBrain>();

            // Blending
            m_Brain.m_DefaultBlend = new CinemachineBlendDefinition(
                CinemachineBlendDefinition.Style.Linear,
                k_BlendingTime);
            
#if CINEMACHINE_V3_OR_HIGHER
            m_Source = CreateGameObject("A", typeof(CmCamera)).GetComponent<CmCamera>();
            m_Target = CreateGameObject("B", typeof(CmCamera)).GetComponent<CmCamera>();
#else
            m_Source = CreateGameObject("A", typeof(CinemachineVirtualCamera)).GetComponent<CinemachineVirtualCamera>();
            m_Target = CreateGameObject("B", typeof(CinemachineVirtualCamera)).GetComponent<CinemachineVirtualCamera>();
#endif
            m_Source.Priority = 10;
            m_Target.Priority = 15;
            m_Source.enabled = true;
            m_Target.enabled = false;
            m_Source.transform.position = Vector3.zero;
            m_Target.transform.position = new Vector3(10, 0, 0);

            base.SetUp();
            
            m_Brain.m_UpdateMethod = CinemachineBrain.UpdateMethod.ManualUpdate; 
        }

        [UnityTest]
        public IEnumerator BlendFromSourceToTarget()
        {
            // Check that source vcam is active
            m_Brain.ManualUpdate();
            Assert.That(ReferenceEquals(m_Brain.ActiveVirtualCamera, m_Source));
            yield return null;
            
            // Active target and blend from source to target completely
            m_Target.enabled = true;
            var timeElapsed = 0f;
            m_Brain.ManualUpdate();
            timeElapsed += CinemachineCore.UniformDeltaTimeOverride;
            while (timeElapsed < k_BlendingTime)
            {
                Assert.That(ReferenceEquals(m_Brain.ActiveVirtualCamera, m_Target));
                Assert.That(m_Brain.IsBlending, Is.True);
                m_Brain.ManualUpdate();
                timeElapsed += CinemachineCore.UniformDeltaTimeOverride;
                yield return null;
            }
            
            Assert.That(m_Brain.IsBlending, Is.False);
        }
        
        [UnityTest]
        public IEnumerator BlendBetweenSourceAndTarget()
        {
            // Check that source vcam is active
            m_Brain.ManualUpdate();
            Assert.That(ReferenceEquals(m_Brain.ActiveVirtualCamera, m_Source));
            yield return null;
            
            // Activate Target vcam and blend 50% between source and target
            m_Target.enabled = true;
            var timeElapsed = 0f;
            m_Brain.ManualUpdate();
            timeElapsed += CinemachineCore.UniformDeltaTimeOverride;
            yield return null;

            CinemachineBlend activeBlend;
            while (timeElapsed < k_BlendingTime * 0.5f)
            {
                Assert.That(ReferenceEquals(m_Brain.ActiveVirtualCamera, m_Target));
                Assert.That(m_Brain.IsBlending, Is.True);
                m_Brain.ManualUpdate();
                timeElapsed += CinemachineCore.UniformDeltaTimeOverride;
                activeBlend = m_Brain.ActiveBlend;
                Assert.That(activeBlend, Is.Not.Null); 
                Assert.That(activeBlend.TimeInBlend, Is.EqualTo(timeElapsed));
                yield return null;
            }
            
            // Blend back to source from 50% between source and target
            m_Target.enabled = false;
            timeElapsed = 0f;
            m_Brain.ManualUpdate();
            timeElapsed += CinemachineCore.UniformDeltaTimeOverride;
            yield return null;
            while (timeElapsed < k_BlendingTime * 0.3f)
            {
                Assert.That(ReferenceEquals(m_Brain.ActiveVirtualCamera, m_Source));
                Assert.That(m_Brain.IsBlending, Is.True);
                m_Brain.ManualUpdate();
                timeElapsed += CinemachineCore.UniformDeltaTimeOverride;
                
                activeBlend = m_Brain.ActiveBlend;
                Assert.That(activeBlend, Is.Not.Null); 
                Assert.That(activeBlend.TimeInBlend, Is.EqualTo(timeElapsed));
                yield return null;
            }
            
            activeBlend = m_Brain.ActiveBlend;
            Assert.That(activeBlend, Is.Not.Null); 
            Assert.That(activeBlend.TimeInBlend, Is.EqualTo(timeElapsed));
            
            // wait for blend to finish
            var timeToFinish = activeBlend.Duration - activeBlend.TimeInBlend;
            timeElapsed = 0f;
            while (timeElapsed < timeToFinish)
            {
                Assert.That(m_Brain.IsBlending, Is.True);
                m_Brain.ManualUpdate();
                timeElapsed += CinemachineCore.UniformDeltaTimeOverride;
                yield return null;
            }
            Assert.That(m_Brain.IsBlending, Is.False);
        }
        
        // [UnityTest, ConditionalIgnore("IgnoreHDRP2020", "Ignored on HDRP Unity 2020.")]
        [UnityTest]
        public IEnumerator DoesInterruptedBlendingBetweenCamerasTakesDoubleTime()
        {
            // Check that source vcam is active
            m_Brain.ManualUpdate();
            Assert.That(ReferenceEquals(m_Brain.ActiveVirtualCamera, m_Source));
            yield return null;
            
            // Start blending
            m_Target.enabled = true;
            var timeElapsed = 0f;
            m_Brain.ManualUpdate();
            timeElapsed += CinemachineCore.UniformDeltaTimeOverride;
            yield return null;
        
            CinemachineBlend activeBlend = null;
            // Blend 90% between source and target
            while (timeElapsed < k_BlendingTime * 0.9f)
            {
                Assert.That(ReferenceEquals(m_Brain.ActiveVirtualCamera, m_Target));
                Assert.That(m_Brain.IsBlending, Is.True);
                m_Brain.ManualUpdate();
                timeElapsed += CinemachineCore.UniformDeltaTimeOverride;
                activeBlend = m_Brain.ActiveBlend;
                Assert.That(activeBlend, Is.Not.Null); 
                Assert.That(activeBlend.TimeInBlend, Is.EqualTo(timeElapsed));
                yield return null;
            }

            m_Target.enabled = false;
            timeElapsed = 0f;
            m_Brain.ManualUpdate();
            timeElapsed += CinemachineCore.UniformDeltaTimeOverride;
            yield return null;
            
            // Blend 10% backwards
            while (timeElapsed < k_BlendingTime * 0.1f)
            {
                Assert.That(ReferenceEquals(m_Brain.ActiveVirtualCamera, m_Source));
                Assert.That(m_Brain.IsBlending, Is.True);
                m_Brain.ManualUpdate();
                timeElapsed += CinemachineCore.UniformDeltaTimeOverride;
                activeBlend = m_Brain.ActiveBlend;
                Assert.That(activeBlend, Is.Not.Null); 
                Assert.That(activeBlend.TimeInBlend, Is.EqualTo(timeElapsed));
                yield return null;
            }

            m_Target.enabled = true;
            timeElapsed = 0f;
            m_Brain.ManualUpdate();
            timeElapsed += CinemachineCore.UniformDeltaTimeOverride;
            yield return null;
            
            // finish blend
            while (timeElapsed < k_BlendingTime * 0.2f)
            {
                Assert.That(ReferenceEquals(m_Brain.ActiveVirtualCamera, m_Target));
                Assert.That(m_Brain.IsBlending, Is.True);
                m_Brain.ManualUpdate();
                timeElapsed += CinemachineCore.UniformDeltaTimeOverride;
                yield return null;
            }
            Assert.That(m_Brain.IsBlending, Is.False);
        }
        
        [UnityTest]
        public IEnumerator SetActiveBlend()
        {
            // Check that source vcam is active
            m_Brain.ManualUpdate();
            Assert.That(ReferenceEquals(m_Brain.ActiveVirtualCamera, m_Source));
            yield return null;
            
            // Active target vcam and wait for 5 frames
            m_Target.enabled = true;
            for (int i = 0; i < 5; ++i)
            {
                m_Brain.ManualUpdate();
                yield return null;
            }
        
            var blend = m_Brain.ActiveBlend;
            Assert.That(blend, Is.Not.Null);
            // Save current blend progress
            var percentComplete = blend.TimeInBlend / blend.Duration;
        
            // Step blend back a frame
            blend.TimeInBlend -= CinemachineCore.UniformDeltaTimeOverride;
            m_Brain.ActiveBlend = blend;
        
            // Wait a frame and check that blend progress is the same
            m_Brain.ManualUpdate();
            yield return null;
            blend = m_Brain.ActiveBlend;
            Assert.That(percentComplete, Is.EqualTo(blend.TimeInBlend / blend.Duration));
        
            // Force the blend to complete
            blend.Duration = 0;
            m_Brain.ActiveBlend = blend;
        
            // Wait a frame and check that blend is finished
            m_Brain.ManualUpdate();
            yield return null;
            Assert.That(m_Brain.ActiveBlend, Is.Null);
        
            // Disable target, blend back to source, wait 5 frames
            m_Target.enabled = false;
            for (int i = 0; i < 5; ++i)
            {
                m_Brain.ManualUpdate();
                yield return null;
            }
            blend = m_Brain.ActiveBlend;
            Assert.That(blend, Is.Not.Null);
            Assert.That(percentComplete, Is.EqualTo(blend.TimeInBlend / blend.Duration));
        
            // Kill the blend
            m_Brain.ActiveBlend = null;
        
            // Wait a frame and check that blend is finished
            m_Brain.ManualUpdate();
            yield return null;
            Assert.That(m_Brain.ActiveBlend == null);
        }
    }
}