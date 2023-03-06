using UnityEngine;
using NUnit.Framework;
using UnityEngine.TestTools.Utils;

namespace Unity.Cinemachine.Tests.Editor
{
    [TestFixture]
    public class UnityVectorExtensionTests
    {
        public enum IntersectionResult
        {
            Zero,
            Infinity,
            l1_p1,
            l1_p2,
            l2_p1,
            l2_p2
        }

        static object[] s_IntersectionTestCases =
        {
            // l1_p1, l1_p2, l2_p1, l2_p2, expectedIntersectionType, expectedIntersectionResult
            new object[] {new Vector2(0, 1), new Vector2(0, -1), new Vector2(-1, 0), new Vector2(1, 0), 2, IntersectionResult.Zero},
            new object[] {new Vector2(0, 1), new Vector2(0, 0), new Vector2(-1, 0), new Vector2(1, 0), 2, IntersectionResult.Zero},
            new object[] {new Vector2(0, 2), new Vector2(0, 1), new Vector2(-1, 0), new Vector2(1, 0), 1, IntersectionResult.Zero},
            new object[] {new Vector2(0, 2), new Vector2(0, 1), new Vector2(1, 2), new Vector2(1, 1), 0, IntersectionResult.Infinity},
            new object[] {new Vector2(1, 2), new Vector2(1, 1), new Vector2(1, -2), new Vector2(1, -1), 3, IntersectionResult.Infinity},
            new object[] {new Vector2(1, 2), new Vector2(1, -2), new Vector2(1, 3), new Vector2(1, 1), 4, IntersectionResult.Infinity},
            new object[] {new Vector2(1, 2), new Vector2(1, -2), new Vector2(1, 2), new Vector2(1, -2), 4, IntersectionResult.Infinity},
            new object[] {new Vector2(1, 2), new Vector2(1, -2), new Vector2(1, -2), new Vector2(1, 2), 4, IntersectionResult.Infinity},
            new object[] {new Vector2(0, 1), new Vector2(0, 1), new Vector2(1, 0), new Vector2(1, 0), 4, IntersectionResult.Infinity},
            new object[] {new Vector2(0, 0), new Vector2(2, 0), new Vector2(0, 1), new Vector2(1, 0), 2, IntersectionResult.l2_p2},
            new object[] {new Vector2(0, 0), new Vector2(2, 0), new Vector2(1, 0), new Vector2(0, 1), 2, IntersectionResult.l2_p1},
            // Parallel segments touching at one point
            new object[] {new Vector2(0, 3), new Vector2(0, 5), new Vector2(0, 5), new Vector2(0, 9), 4, IntersectionResult.l2_p1},
            new object[] {new Vector2(0, 5), new Vector2(0, 3), new Vector2(0, 5), new Vector2(0, 9), 4, IntersectionResult.l2_p1},
            new object[] {new Vector2(0, 3), new Vector2(0, 5), new Vector2(0, 9), new Vector2(0, 5), 4, IntersectionResult.l2_p2},
            new object[] {new Vector2(0, 5), new Vector2(0, 3), new Vector2(0, 9), new Vector2(0, 5), 4, IntersectionResult.l2_p2}
        };
        
        [Test, TestCaseSource(nameof(s_IntersectionTestCases))]
        public void FindIntersectionTest(Vector2 l1_p1, Vector2 l1_p2, Vector2 l2_p1, Vector2 l2_p2, 
            int expectedIntersectionType, IntersectionResult expectedIntersectionResult)
        {
            int intersectionType = UnityVectorExtensions.FindIntersection(l1_p1, l1_p2, l2_p1, l2_p2,
                out Vector2 intersection);
            Assert.That(intersectionType, Is.EqualTo(expectedIntersectionType));
            switch (expectedIntersectionResult)
            {
                case IntersectionResult.Zero:
                    Assert.That(intersection, Is.EqualTo(Vector2.zero).Using(Vector2EqualityComparer.Instance));
                    break;
                case IntersectionResult.Infinity:
                    Assert.That(float.IsInfinity(intersection.x), Is.True);
                    Assert.That(float.IsInfinity(intersection.y), Is.True);
                    break;
                case IntersectionResult.l1_p1:
                    Assert.That(intersection, Is.EqualTo(l1_p1).Using(Vector2EqualityComparer.Instance));
                    break;
                case IntersectionResult.l1_p2:
                    Assert.That(intersection, Is.EqualTo(l1_p2).Using(Vector2EqualityComparer.Instance));
                    break;
                case IntersectionResult.l2_p1:
                    Assert.That(intersection, Is.EqualTo(l2_p1).Using(Vector2EqualityComparer.Instance));
                    break;
                case IntersectionResult.l2_p2:
                    Assert.That(intersection, Is.EqualTo(l2_p2).Using(Vector2EqualityComparer.Instance));
                    break;
            }
        }

        static object[] s_AngleTestCases =
        {
            new object[] {Vector2.left, 90f, true},
            new object[] {Vector2.right, 90f, true},
            new object[] {new Vector2(-0.0001f, 1f), 0.00572958f, false},
            new object[] {new Vector2(0.0001f, 1f), 0.00572958f, false}
        };

        [Test, TestCaseSource(nameof(s_AngleTestCases))]
        public void TestAngle(Vector2 v2, float expectedAngle, bool compareWithBuiltIn)
        {
            var v1 = Vector3.up;
            var angle = UnityVectorExtensions.Angle(v1, v2);
            Assert.That(angle, Is.EqualTo(expectedAngle).Within(UnityVectorExtensions.Epsilon));
            if (compareWithBuiltIn)
            {
                var angle2 = Vector2.Angle(v1, v2);
                Assert.That(angle2, Is.EqualTo(angle).Within(UnityVectorExtensions.Epsilon));
            }
        }

        static object[] s_SignedAngleTestCases =
        {
            new object[] {Vector2.left, 90f, true},
            new object[] {Vector2.right, -90f, true},
            new object[] {new Vector2(-0.0001f, 1f), 0.00572958f, false},
            new object[] {new Vector2(0.0001f, 1f), -0.00572958f, false}
        };

        [Test, TestCaseSource(nameof(s_SignedAngleTestCases))]
        public void TestSignedAngle(Vector2 v2, float expectedAngle, bool compareWithBuiltIn)
        {
            var v1 = Vector3.up;
            var angle = UnityVectorExtensions.SignedAngle(v1, v2, Vector3.forward);
            Assert.That(angle, Is.EqualTo(expectedAngle).Within(UnityVectorExtensions.Epsilon));
            
            if (compareWithBuiltIn)
            {
                var angle2 = Vector2.SignedAngle(v1, v2);
                Assert.That(angle2, Is.EqualTo(angle).Within(UnityVectorExtensions.Epsilon));
            }

            var angle3 = UnityVectorExtensions.SignedAngle(v1, v2, Vector3.back);
            Assert.That(angle3, Is.EqualTo(-angle).Within(UnityVectorExtensions.Epsilon));
        }
    }
}