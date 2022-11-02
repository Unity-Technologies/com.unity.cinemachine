using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Cinemachine;
using UnityEngine.TestTools.Utils;

namespace Tests.Runtime
{
    [TestFixture]
    public class CamerasBlendingTests : CinemachineRuntimeTimeInvariantFixtureBase
    {
        const float k_BlendingTime = 1;

        CinemachineVirtualCameraBase m_Source, m_Target;

        [SetUp]
        public override void SetUp()
        {
            base.SetUp();

            // Blending
            m_Brain.DefaultBlend = new CinemachineBlendDefinition(
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
        }

        [UnityTest]
        public IEnumerator BlendFromSourceToTarget()
        {
            // Check that source vcam is active
            UpdateCinemachine();
            Assert.That(ReferenceEquals(m_Brain.ActiveVirtualCamera, m_Source));
            yield return null;
            
            // Active target and blend from source to target completely
            m_Target.enabled = true;
            var startTime = CinemachineCore.CurrentTimeOverride;
            UpdateCinemachine();
            yield return null;
            
            while (CinemachineCore.CurrentTimeOverride - startTime < k_BlendingTime)
            {
                Assert.That(ReferenceEquals(m_Brain.ActiveVirtualCamera, m_Target));
                Assert.That(m_Brain.IsBlending, Is.True);
                UpdateCinemachine();
                yield return null;
            }

            Assert.That(m_Brain.IsBlending, Is.False);
        }
        
        [UnityTest]
        public IEnumerator BlendBetweenSourceAndTarget()
        {
            // Check that source vcam is active
            UpdateCinemachine();
            Assert.That(ReferenceEquals(m_Brain.ActiveVirtualCamera, m_Source));
            yield return null;
            
            // Activate Target vcam and blend 50% between source and target
            m_Target.enabled = true;
            var startTime = CinemachineCore.CurrentTimeOverride;
            UpdateCinemachine();
            yield return null;

            CinemachineBlend activeBlend;
            while (CinemachineCore.CurrentTimeOverride - startTime < k_BlendingTime * 0.5f)
            {
                Assert.That(ReferenceEquals(m_Brain.ActiveVirtualCamera, m_Target));
                Assert.That(m_Brain.IsBlending, Is.True);
                UpdateCinemachine();
                activeBlend = m_Brain.ActiveBlend;
                Assert.That(activeBlend, Is.Not.Null); 
                Assert.That(activeBlend.TimeInBlend, Is.EqualTo(CinemachineCore.CurrentTimeOverride - startTime).Using(FloatEqualityComparer.Instance));
                yield return null;
            }
            
            // Blend back to source from 50% between source and target
            m_Target.enabled = false;
            startTime = CinemachineCore.CurrentTimeOverride;
            UpdateCinemachine();
            yield return null;
            
            while (CinemachineCore.CurrentTimeOverride - startTime < k_BlendingTime * 0.3f)
            {
                Assert.That(ReferenceEquals(m_Brain.ActiveVirtualCamera, m_Source));
                Assert.That(m_Brain.IsBlending, Is.True);
                UpdateCinemachine();
                
                activeBlend = m_Brain.ActiveBlend;
                Assert.That(activeBlend, Is.Not.Null); 
                Assert.That(activeBlend.TimeInBlend, Is.EqualTo(CinemachineCore.CurrentTimeOverride - startTime).Using(FloatEqualityComparer.Instance));
                yield return null;
            }
            
            activeBlend = m_Brain.ActiveBlend;
            Assert.That(activeBlend, Is.Not.Null); 
            Assert.That(activeBlend.TimeInBlend, Is.EqualTo(CinemachineCore.CurrentTimeOverride - startTime).Using(FloatEqualityComparer.Instance));
            
            // wait for blend to finish
            var timeToFinish = activeBlend.Duration - activeBlend.TimeInBlend;
            startTime = CinemachineCore.CurrentTimeOverride;
            while (CinemachineCore.CurrentTimeOverride - startTime < timeToFinish)
            {
                Assert.That(m_Brain.IsBlending, Is.True);
                UpdateCinemachine();
                yield return null;
            }
            Assert.That(m_Brain.IsBlending, Is.False);
        }
        
        // [UnityTest, ConditionalIgnore("IgnoreHDRP2020", "Ignored on HDRP Unity 2020.")]
        [UnityTest]
        public IEnumerator DoesInterruptedBlendingBetweenCamerasTakesDoubleTime()
        {
            // Check that source vcam is active
            UpdateCinemachine();
            Assert.That(ReferenceEquals(m_Brain.ActiveVirtualCamera, m_Source));
            yield return null;
            
            // Start blending
            m_Target.enabled = true;
            var startTime = CinemachineCore.CurrentTimeOverride;
            UpdateCinemachine();
            yield return null;
        
            CinemachineBlend activeBlend = null;
            // Blend 90% between source and target
            while (CinemachineCore.CurrentTimeOverride - startTime < k_BlendingTime * 0.9f)
            {
                Assert.That(ReferenceEquals(m_Brain.ActiveVirtualCamera, m_Target));
                Assert.That(m_Brain.IsBlending, Is.True);
                UpdateCinemachine();
                activeBlend = m_Brain.ActiveBlend;
                Assert.That(activeBlend, Is.Not.Null); 
                Assert.That(activeBlend.TimeInBlend, Is.EqualTo(CinemachineCore.CurrentTimeOverride - startTime).Using(FloatEqualityComparer.Instance));
                yield return null;
            }

            m_Target.enabled = false;
            startTime = CinemachineCore.CurrentTimeOverride;
            UpdateCinemachine();
            yield return null;
            
            // Blend 10% backwards
            while (CinemachineCore.CurrentTimeOverride - startTime < k_BlendingTime * 0.1f)
            {
                Assert.That(ReferenceEquals(m_Brain.ActiveVirtualCamera, m_Source));
                Assert.That(m_Brain.IsBlending, Is.True);
                UpdateCinemachine();
                activeBlend = m_Brain.ActiveBlend;
                Assert.That(activeBlend, Is.Not.Null); 
                Assert.That(activeBlend.TimeInBlend, Is.EqualTo(CinemachineCore.CurrentTimeOverride - startTime).Using(FloatEqualityComparer.Instance));
                yield return null;
            }

            m_Target.enabled = true;
            startTime = CinemachineCore.CurrentTimeOverride;
            UpdateCinemachine();
            yield return null;
            
            // finish blend
            while (CinemachineCore.CurrentTimeOverride - startTime < k_BlendingTime * 0.2f)
            {
                Assert.That(ReferenceEquals(m_Brain.ActiveVirtualCamera, m_Target));
                Assert.That(m_Brain.IsBlending, Is.True);
                UpdateCinemachine();
                yield return null;
            }
            Assert.That(m_Brain.IsBlending, Is.False);
        }
        
        [UnityTest]
        public IEnumerator SetActiveBlend()
        {
            // Check that source vcam is active
            UpdateCinemachine();
            Assert.That(ReferenceEquals(m_Brain.ActiveVirtualCamera, m_Source));
            yield return null;
            
            // Active target vcam and wait for 5 frames
            m_Target.enabled = true;
            for (int i = 0; i < 5; ++i)
            {
                UpdateCinemachine();
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
            UpdateCinemachine();
            yield return null;
            blend = m_Brain.ActiveBlend;
            Assert.That(percentComplete, Is.EqualTo(blend.TimeInBlend / blend.Duration).Using(FloatEqualityComparer.Instance));
        
            // Force the blend to complete
            blend.Duration = 0;
            m_Brain.ActiveBlend = blend;
        
            // Wait a frame and check that blend is finished
            UpdateCinemachine();
            yield return null;
            Assert.That(m_Brain.ActiveBlend, Is.Null);
        
            // Disable target, blend back to source, wait 5 frames
            m_Target.enabled = false;
            for (int i = 0; i < 5; ++i)
            {
                UpdateCinemachine();
                yield return null;
            }
            blend = m_Brain.ActiveBlend;
            Assert.That(blend, Is.Not.Null);
            Assert.That(percentComplete, Is.EqualTo(blend.TimeInBlend / blend.Duration).Using(FloatEqualityComparer.Instance));
        
            // Kill the blend
            m_Brain.ActiveBlend = null;
        
            // Wait a frame and check that blend is finished
            UpdateCinemachine();
            yield return null;
            Assert.That(m_Brain.ActiveBlend == null);
        }
    }
}