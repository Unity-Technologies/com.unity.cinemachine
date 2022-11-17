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
        [Test]
        public void DampFloat()
        {
            const float dampTime = 10f;
            const float initial = 100f;

            float t = 0;
            float r = Damper.Damp(initial, dampTime, t);
            Assert.That(r, Is.EqualTo(0f));
            Assert.That(r, Is.LessThan(initial));
            const int iterations = 10;
            for (int i = 0; i < iterations; ++i)
            {
                t += dampTime / iterations;

                if (i != iterations - 1)
                    Assert.That(t, Is.LessThan(dampTime));
                else
                    t = dampTime;
                float r2 = Damper.Damp(initial, dampTime, t);
                Assert.That(r, Is.LessThan(r2));
                r = r2;
            }
        }

        static IEnumerable AxisDriverAccelTestCases
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
        [Test, TestCaseSource(nameof(AxisDriverAccelTestCases))]
        public void DampVector(Vector3 initial)
        {
            float[] deltaTimes = { 0.0069444445F, 0.008333334F, 0.016666668F, 0.033333335F, 0.1f }; // 144, 100, 60, 30, 10 fps
            //for (var dampTime = 0.1f; dampTime <= 2f; dampTime += 0.1f)
            var dampTime = 1f;
            {
                foreach (var deltaTime in deltaTimes)
                {
                    var previousDampedMagnitude = -1f;
                    var previousDamped = Vector3.zero;
                    int iterationCount;
                    var damped = initial;
                    for (iterationCount = 0; true; iterationCount++)
                    {
                        damped = Damper.Damp(damped, dampTime, deltaTime);
                        var dampedMagnitude = damped.magnitude;

                        if (dampedMagnitude < UnityVectorExtensions.Epsilon)
                            break; // stop when delta is small enough
                        if (dampedMagnitude == previousDampedMagnitude)
                            break; // stop 
                    
                        if (previousDampedMagnitude >= 0)
                        {
                            Assert.That(damped.normalized, 
                                Is.EqualTo(previousDamped.normalized).Using(m_Vector3EqualityComparer)); // monotonic 
                            Assert.That(dampedMagnitude, Is.LessThan(previousDampedMagnitude)); // strictly decreasing
                        }
                    
                        previousDampedMagnitude = dampedMagnitude;
                        previousDamped = damped;
                    }

                    var realDampTime = iterationCount * deltaTime;
                    Debug.Log(iterationCount + "*" + deltaTime + "=" + realDampTime + " seconds instead of " + dampTime + " seconds.");
                }
            }
        }
    }
}