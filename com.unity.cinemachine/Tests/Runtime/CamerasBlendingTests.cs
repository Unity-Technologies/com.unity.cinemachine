using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Cinemachine;

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
            m_Brain.DefaultBlend = 
                new CinemachineBlendDefinition(CinemachineBlendDefinition.Styles.Linear, k_BlendingTime);
            
#if CINEMACHINE_V3_OR_HIGHER
            m_Source = CreateGameObject("A", typeof(CinemachineCamera)).GetComponent<CinemachineCamera>();
            m_Target = CreateGameObject("B", typeof(CinemachineCamera)).GetComponent<CinemachineCamera>();
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
            yield return UpdateCinemachine();
            Assert.That(ReferenceEquals(m_Brain.ActiveVirtualCamera, m_Source));
            
            // Active target and blend from source to target completely
            m_Target.enabled = true;
            var startTime = CurrentTime;
            yield return UpdateCinemachine();
            
            while (CurrentTime - startTime < k_BlendingTime)
            {
                Assert.That(ReferenceEquals(m_Brain.ActiveVirtualCamera, m_Target));
                Assert.That(m_Brain.IsBlending, Is.True);
                yield return UpdateCinemachine();
            }

            Assert.That(m_Brain.IsBlending, Is.False);
        }
        
        [UnityTest]
        public IEnumerator BlendBetweenSourceAndTarget()
        {
            // Check that source vcam is active
            yield return UpdateCinemachine();
            Assert.That(ReferenceEquals(m_Brain.ActiveVirtualCamera, m_Source));
            
            // Activate Target vcam and blend 50% between source and target
            m_Target.enabled = true;
            var startTime = CurrentTime;
            yield return UpdateCinemachine();

            CinemachineBlend activeBlend = null;
            while (CurrentTime - startTime < k_BlendingTime * 0.5f)
            {
                Assert.That(ReferenceEquals(m_Brain.ActiveVirtualCamera, m_Target));
                Assert.That(m_Brain.IsBlending, Is.True);
                yield return UpdateCinemachine();
                activeBlend = m_Brain.ActiveBlend;
                Assert.That(activeBlend, Is.Not.Null); 
                Assert.That(activeBlend.TimeInBlend, Is.EqualTo(CurrentTime - startTime).Using(m_FloatEqualityComparer));
            }
            
            // Blend back to source from 50% between source and target
            m_Target.enabled = false;
            startTime = CurrentTime;
            yield return UpdateCinemachine();
            
            while (CurrentTime - startTime < k_BlendingTime * 0.3f)
            {
                Assert.That(ReferenceEquals(m_Brain.ActiveVirtualCamera, m_Source));
                Assert.That(m_Brain.IsBlending, Is.True);
                yield return UpdateCinemachine();
                
                activeBlend = m_Brain.ActiveBlend;
                Assert.That(activeBlend, Is.Not.Null); 
                Assert.That(activeBlend.TimeInBlend, Is.EqualTo(CurrentTime - startTime).Using(m_FloatEqualityComparer));
            }
            
            // wait for blend to finish
            Assert.NotNull(activeBlend);
            var timeToFinish = activeBlend.Duration - activeBlend.TimeInBlend;
            startTime = CurrentTime;
            while (CurrentTime - startTime < timeToFinish)
            {
                Assert.That(m_Brain.IsBlending, Is.True);
                yield return UpdateCinemachine();
            }
            Assert.That(m_Brain.IsBlending, Is.False);
        }
        
        // [UnityTest, ConditionalIgnore("IgnoreHDRP2020", "Ignored on HDRP Unity 2020.")]
        [UnityTest]
        public IEnumerator DoesInterruptedBlendingBetweenCamerasTakesDoubleTime()
        {
            // Check that source vcam is active
            yield return UpdateCinemachine();
            Assert.That(ReferenceEquals(m_Brain.ActiveVirtualCamera, m_Source));
            
            // Start blending
            m_Target.enabled = true;
            var startTime = CurrentTime;
            yield return UpdateCinemachine();
        
            // Blend 90% between source and target
            while (CurrentTime - startTime < k_BlendingTime * 0.9f)
            {
                Assert.That(ReferenceEquals(m_Brain.ActiveVirtualCamera, m_Target));
                Assert.That(m_Brain.IsBlending, Is.True);
                yield return UpdateCinemachine();
                var activeBlend = m_Brain.ActiveBlend;
                Assert.That(activeBlend, Is.Not.Null); 
                Assert.That(activeBlend.TimeInBlend, Is.EqualTo(CurrentTime - startTime).Using(m_FloatEqualityComparer));
            }

            m_Target.enabled = false;
            startTime = CurrentTime;
            yield return UpdateCinemachine();
            
            // Blend 10% backwards
            while (CurrentTime - startTime < k_BlendingTime * 0.1f)
            {
                Assert.That(ReferenceEquals(m_Brain.ActiveVirtualCamera, m_Source));
                Assert.That(m_Brain.IsBlending, Is.True);
                yield return UpdateCinemachine();
                var activeBlend = m_Brain.ActiveBlend;
                Assert.That(activeBlend, Is.Not.Null); 
                Assert.That(activeBlend.TimeInBlend, Is.EqualTo(CurrentTime - startTime).Using(m_FloatEqualityComparer));
            }

            m_Target.enabled = true;
            startTime = CurrentTime;
            yield return UpdateCinemachine();
            
            // finish blend
            while (CurrentTime - startTime < k_BlendingTime * 0.2f)
            {
                Assert.That(ReferenceEquals(m_Brain.ActiveVirtualCamera, m_Target));
                Assert.That(m_Brain.IsBlending, Is.True);
                yield return UpdateCinemachine();
            }
            Assert.That(m_Brain.IsBlending, Is.False);
        }
        
        [UnityTest]
        public IEnumerator SetActiveBlend()
        {
            var halfBlendIterationCount = 
                Mathf.FloorToInt((k_BlendingTime / 2f) / CinemachineCore.UniformDeltaTimeOverride);
            
            // Check that source vcam is active
            yield return UpdateCinemachine();
            Assert.That(ReferenceEquals(m_Brain.ActiveVirtualCamera, m_Source));
            
            // Active target vcam and wait for half blend duration
            m_Target.enabled = true;
            for (var i = 0; i < halfBlendIterationCount; ++i)
                yield return UpdateCinemachine();

            var blend = m_Brain.ActiveBlend;
            Assert.That(blend, Is.Not.Null);
            // Save current blend progress
            var percentComplete = blend.TimeInBlend / blend.Duration;
        
            // Step blend back a frame
            blend.TimeInBlend -= CinemachineCore.UniformDeltaTimeOverride;
            m_Brain.ActiveBlend = blend;
        
            // Wait a frame and check that blend progress is the same
            yield return UpdateCinemachine();
            blend = m_Brain.ActiveBlend;
            Assert.That(percentComplete, Is.EqualTo(blend.TimeInBlend / blend.Duration).Using(m_FloatEqualityComparer));
        
            // Force the blend to complete
            blend.Duration = 0;
            m_Brain.ActiveBlend = blend;
        
            // Wait a frame and check that blend is finished
            yield return UpdateCinemachine();
            Assert.That(m_Brain.ActiveBlend, Is.Null);
        
            // Disable target, blend back to source, wait 5 frames
            m_Target.enabled = false;
            for (var i = 0; i < halfBlendIterationCount; ++i)
                yield return UpdateCinemachine();
            
            blend = m_Brain.ActiveBlend;
            Assert.That(blend, Is.Not.Null);
            Assert.That(percentComplete, Is.EqualTo(blend.TimeInBlend / blend.Duration).Using(m_FloatEqualityComparer));
        
            // Kill the blend
            m_Brain.ActiveBlend = null;
        
            // Wait a frame and check that blend is finished
            yield return UpdateCinemachine();
            Assert.That(m_Brain.ActiveBlend == null);
        }
    }
}