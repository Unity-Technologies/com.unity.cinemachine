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
                yield return new TestCaseData(Vector3.one);
                yield return new TestCaseData(Vector3.one / 2f);
                yield return new TestCaseData(Vector3.one / 3f);
                yield return new TestCaseData(Vector3.one / 5f);
                yield return new TestCaseData(Vector3.one / 7f);
                yield return new TestCaseData(Vector3.forward);
                yield return new TestCaseData(Vector3.back);
                yield return new TestCaseData(Vector3.left);
                yield return new TestCaseData(Vector3.right);
                yield return new TestCaseData(Vector3.up);
                yield return new TestCaseData(Vector3.down);
            }
        }

        readonly Vector3EqualityComparer m_Vector3EqualityComparer = new(UnityVectorExtensions.Epsilon);
        [Test, TestCaseSource(nameof(AxisDriverAccelTestCases))]
        public void DampVector(Vector3 initial)
        {
            var previousDeltaMagnitude = -1f;
            var previousDelta = Vector3.zero;
            const float deltaTime = 0.1f;
            const float dampTime = 1f;
            int iterationCount;
            for (iterationCount = 0; true; iterationCount++)
            {
                var delta =  Damper.Damp(initial, dampTime, deltaTime);
                var deltaMagnitude = delta.magnitude;

                if (deltaMagnitude < UnityVectorExtensions.Epsilon)
                    break; // stop when delta is small enough
                
                if (previousDeltaMagnitude >= 0)
                {
                    Assert.That(delta.normalized, 
                        Is.EqualTo(previousDelta.normalized).Using(m_Vector3EqualityComparer)); // monotonic 
                    Assert.That(deltaMagnitude, Is.LessThan(previousDeltaMagnitude)); // strictly decreasing
                }
                
                previousDeltaMagnitude = deltaMagnitude;
                previousDelta = delta;
                initial -= delta;
            }

            var realDampTime = iterationCount * deltaTime;
            Debug.Log(realDampTime + " seconds instead of " + dampTime + " seconds.");
        }
    }
}