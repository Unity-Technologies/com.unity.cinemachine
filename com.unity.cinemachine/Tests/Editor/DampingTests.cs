using System.Collections;
using Cinemachine.Utility;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools.Utils;

namespace Tests.Editor
{
    [TestFixture]
    public class DampingTests
    {
        const float k_Initial = 100f;
        const float k_DampTime = 1f;
        [Test]
        public void StandardDampFloat()
        {
            DampFloatTest(Damper.StandardDamp);
        }

        [Test]
        public void StableDampFloat()
        {
            DampFloatTest(Damper.StableDamp);
        }

        static void DampFloatTest(System.Func<float, float, float, float> dampingAlgorithm)
        {
            float t = 0f;
            float r = Damper.StableDamp(k_Initial, k_DampTime, t);
            Assert.That(r, Is.EqualTo(0f));
            Assert.That(r, Is.LessThan(k_Initial));
            const int iterations = 10;
            for (int i = 0; i < iterations; ++i)
            {
                t += k_DampTime / iterations;

                if (i != iterations - 1)
                    Assert.That(t, Is.LessThan(k_DampTime));
                else
                    t = k_DampTime;
                float r2 = dampingAlgorithm(k_Initial, k_DampTime, t);
                Assert.That(r, Is.LessThan(r2));
                r = r2;
            }
        }

        [Test]
        public void StandardAndStableDampFloatTimeEquivalence() {
            const float negligible = Damper.kNegligibleResidual * k_Initial;
            for (int fps = 10; fps <= 1024; fps += 10)
            {
                Damper.AverageFrameRateTracker.SetDampTimeScale(fps);

                var dt = 1.0f / fps;
                var stableTime = DampTime(negligible, dt, Damper.StableDamp);
                var standardTime = DampTime(negligible, dt, Damper.StandardDamp);

                // It should take approx the same amount of time for stable damping and
                // standard damping, regardless of framerate
                Assert.That(Mathf.Abs(1.0f - stableTime/standardTime), Is.LessThan(0.07f));
            }
            Damper.AverageFrameRateTracker.Reset();

            // local function
            static float DampTime(float negligible, float dt, System.Func<float, float, float, float> dampingAlgorithm)
            {
                var r = k_Initial;
                float time = 0;
                while (r > negligible)
                {
                    r -= dampingAlgorithm(r, k_DampTime, dt);
                    time += dt;
                }
                return time;
            }
        }

        static IEnumerable VectorDampingTestCases
        {
            get
            {
                yield return new TestCaseData(Vector3.one * 7f);
                yield return new TestCaseData(Vector3.one * 5f);
                yield return new TestCaseData(Vector3.one * 3f);
                yield return new TestCaseData(Vector3.one * 2f);
                yield return new TestCaseData(Vector3.one);
                yield return new TestCaseData(Vector3.one / 2f);
                yield return new TestCaseData(Vector3.one / 3f);
                yield return new TestCaseData(Vector3.one / 5f);
                yield return new TestCaseData(Vector3.one / 7f);
            }
        }

        readonly Vector3EqualityComparer m_Vector3EqualityComparer = new(UnityVectorExtensions.Epsilon);
        [Test, TestCaseSource(nameof(VectorDampingTestCases))]
        public void DampVector(Vector3 initial)
        {
            float[] deltaTimes = { 0.0069444445F, 0.008333334F, 0.016666668F, 0.033333335F, 0.1f }; // 144, 100, 60, 30, 10 fps
            for (var dampTime = 0.1f; dampTime <= 2f; dampTime += 0.1f)
            {
                float negligibleResidual = (Vector3.one * Damper.kNegligibleResidual).magnitude;
                foreach (var deltaTime in deltaTimes)
                {
                    var vectorToDamp = initial;
                    var previousDelta = vectorToDamp;
                    int iterations;
                    for (iterations = 0; vectorToDamp.magnitude > negligibleResidual; iterations++)
                    {
                        var delta = Damper.Damp(vectorToDamp, dampTime, deltaTime);
                        Assert.That(delta.normalized, Is.EqualTo(previousDelta.normalized).Using(m_Vector3EqualityComparer)); // monotonic 
                        Assert.That(delta.magnitude, Is.LessThan(previousDelta.magnitude)); // strictly decreasing
                        vectorToDamp -= delta;
                        previousDelta = delta;
                    }
/*
                    var realDampTime = iterations * deltaTime;
                    Debug.Log($"dt={deltaTime:F8}: actual damp time = {realDampTime/dampTime:F3} * {dampTime}");
*/
                }
            }
        }
    }
}