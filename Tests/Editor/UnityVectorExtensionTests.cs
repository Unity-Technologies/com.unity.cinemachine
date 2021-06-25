using UnityEngine;
using NUnit.Framework;
using Cinemachine.Utility;
using Assert = UnityEngine.Assertions.Assert;

public class UnityVectorExtensionTests
{
    [Test]
	public void FindIntersectionTests()
    {
        {
            var l1_p1 = new Vector2(0, 1);
            var l1_p2 = new Vector2(0, -1);
            var l2_p1 = new Vector2(-1, 0);
            var l2_p2 = new Vector2(1, 0);
            int intersectionType = UnityVectorExtensions.FindIntersection(l1_p1, l1_p2, l2_p1, l2_p2, 
                out Vector2 intersection);
            Assert.IsTrue(intersectionType == 2);
            Assert.IsTrue(AreApproximatelyEqual(intersection, Vector2.zero));
        }
        {
            var l1_p1 = new Vector2(0, 1);
            var l1_p2 = new Vector2(0, 0);
            var l2_p1 = new Vector2(-1, 0);
            var l2_p2 = new Vector2(1, 0);
            int intersectionType = UnityVectorExtensions.FindIntersection(l1_p1, l1_p2, l2_p1, l2_p2, 
                out Vector2 intersection);
            Assert.IsTrue(intersectionType == 2);
            Assert.IsTrue(AreApproximatelyEqual(intersection, Vector2.zero));
        }
        {
            var l1_p1 = new Vector2(0, 2);
            var l1_p2 = new Vector2(0, 1);
            var l2_p1 = new Vector2(-1, 0);
            var l2_p2 = new Vector2(1, 0);
            int intersectionType = UnityVectorExtensions.FindIntersection(l1_p1, l1_p2, l2_p1, l2_p2, 
                out Vector2 intersection);
            Assert.IsTrue(intersectionType == 1);
            Assert.IsTrue(AreApproximatelyEqual(intersection, Vector2.zero));
        }
        {
            var l1_p1 = new Vector2(0, 2);
            var l1_p2 = new Vector2(0, 1);
            var l2_p1 = new Vector2(1, 2);
            var l2_p2 = new Vector2(1, 1);
            int intersectionType = UnityVectorExtensions.FindIntersection(l1_p1, l1_p2, l2_p1, l2_p2, 
                out Vector2 intersection);
            Assert.IsTrue(intersectionType == 0);
            Assert.IsTrue(float.IsInfinity(intersection.x) && float.IsInfinity(intersection.y));
        }
        {
            var l1_p1 = new Vector2(1, 2);
            var l1_p2 = new Vector2(1, 1);
            var l2_p1 = new Vector2(1, -2);
            var l2_p2 = new Vector2(1, -1);
            int intersectionType = UnityVectorExtensions.FindIntersection(l1_p1, l1_p2, l2_p1, l2_p2, 
                out Vector2 intersection);
            Assert.IsTrue(intersectionType == 3);
            Assert.IsTrue(float.IsInfinity(intersection.x) && float.IsInfinity(intersection.y));
        }
        {
            var l1_p1 = new Vector2(1, 2);
            var l1_p2 = new Vector2(1, -2);
            var l2_p1 = new Vector2(1, 3);
            var l2_p2 = new Vector2(1, 1);
            int intersectionType = UnityVectorExtensions.FindIntersection(l1_p1, l1_p2, l2_p1, l2_p2, 
                out Vector2 intersection);
            Assert.IsTrue(intersectionType == 4);
            Assert.IsTrue(float.IsInfinity(intersection.x) && float.IsInfinity(intersection.y));
        }
        {
            var l1_p1 = new Vector2(1, 2);
            var l1_p2 = new Vector2(1, -2);
            var l2_p1 = new Vector2(1, 2);
            var l2_p2 = new Vector2(1, -2);
            int intersectionType = UnityVectorExtensions.FindIntersection(l1_p1, l1_p2, l2_p1, l2_p2, 
                out Vector2 intersection);
            Assert.IsTrue(intersectionType == 4);
            Assert.IsTrue(float.IsInfinity(intersection.x) && float.IsInfinity(intersection.y));
        }
        {
            var l1_p1 = new Vector2(1, 2);
            var l1_p2 = new Vector2(1, -2);
            var l2_p1 = new Vector2(1, -2);
            var l2_p2 = new Vector2(1, 2);
            int intersectionType = UnityVectorExtensions.FindIntersection(l1_p1, l1_p2, l2_p1, l2_p2, 
                out Vector2 intersection);
            Assert.IsTrue(intersectionType == 4);
            Assert.IsTrue(float.IsInfinity(intersection.x) && float.IsInfinity(intersection.y));
        }
        {
            var l1_p1 = new Vector2(0, 1);
            var l1_p2 = new Vector2(0, 1);
            var l2_p1 = new Vector2(1, 0);
            var l2_p2 = new Vector2(1, 0);
            int intersectionType = UnityVectorExtensions.FindIntersection(l1_p1, l1_p2, l2_p1, l2_p2, 
                out Vector2 intersection);
            Assert.IsTrue(intersectionType == 4);
            Assert.IsTrue(float.IsInfinity(intersection.x) && float.IsInfinity(intersection.y));
        }
        {
            var l1_p1 = new Vector2(0, 0);
            var l1_p2 = new Vector2(2, 0);
            var l2_p1 = new Vector2(0, 1);
            var l2_p2 = new Vector2(1, 0);
            int intersectionType = UnityVectorExtensions.FindIntersection(l1_p1, l1_p2, l2_p1, l2_p2, 
                out Vector2 intersection);
            Assert.IsTrue(intersectionType == 2);
            Assert.IsTrue(AreApproximatelyEqual(intersection, l2_p2));
        }
        {
            var l1_p1 = new Vector2(0, 0);
            var l1_p2 = new Vector2(2, 0);
            var l2_p1 = new Vector2(1, 0);
            var l2_p2 = new Vector2(0, 1);
            int intersectionType = UnityVectorExtensions.FindIntersection(l1_p1, l1_p2, l2_p1, l2_p2, 
                out Vector2 intersection);
            Assert.IsTrue(intersectionType == 2);
            Assert.IsTrue(AreApproximatelyEqual(intersection, l2_p1));
        }

        // Parallel segments touching at one point
        {
            var l1_p1 = new Vector2(0, 3);
            var l1_p2 = new Vector2(0, 5);
            var l2_p1 = new Vector2(0, 5);
            var l2_p2 = new Vector2(0, 9);
            int intersectionType = UnityVectorExtensions.FindIntersection(l1_p1, l1_p2, l2_p1, l2_p2, 
                out Vector2 intersection);
            Assert.IsTrue(intersectionType == 4);
            Assert.IsTrue(AreApproximatelyEqual(intersection, l2_p1));
        }
        {
            var l1_p1 = new Vector2(0, 5);
            var l1_p2 = new Vector2(0, 3);
            var l2_p1 = new Vector2(0, 5);
            var l2_p2 = new Vector2(0, 9);
            int intersectionType = UnityVectorExtensions.FindIntersection(l1_p1, l1_p2, l2_p1, l2_p2, 
                out Vector2 intersection);
            Assert.IsTrue(intersectionType == 4);
            Assert.IsTrue(AreApproximatelyEqual(intersection, l2_p1));
        }
        {
            var l1_p1 = new Vector2(0, 3);
            var l1_p2 = new Vector2(0, 5);
            var l2_p1 = new Vector2(0, 9);
            var l2_p2 = new Vector2(0, 5);
            int intersectionType = UnityVectorExtensions.FindIntersection(l1_p1, l1_p2, l2_p1, l2_p2, 
                out Vector2 intersection);
            Assert.IsTrue(intersectionType == 4);
            Assert.IsTrue(AreApproximatelyEqual(intersection, l2_p2));
        }
        {
            var l1_p1 = new Vector2(0, 5);
            var l1_p2 = new Vector2(0, 3);
            var l2_p1 = new Vector2(0, 9);
            var l2_p2 = new Vector2(0, 5);
            int intersectionType = UnityVectorExtensions.FindIntersection(l1_p1, l1_p2, l2_p1, l2_p2, 
                out Vector2 intersection);
            Assert.IsTrue(intersectionType == 4);
            Assert.IsTrue(AreApproximatelyEqual(intersection, l2_p2));
        }
    }

    static bool AreApproximatelyEqual(Vector2 v1, Vector2 v2)
    {
        return Mathf.Abs(v2.x - v1.x) < 1e-5f && Mathf.Abs(v2.y - v1.y) < 1e-5f;
    }

    [Test]
    public void TestAngle()
    {
        {
            Vector3 v1 = Vector3.up;
            Vector2 v2 = Vector3.left;
            float angle = UnityVectorExtensions.Angle(v1, v2);
            Assert.AreApproximatelyEqual(angle, 90f);
            float angle2 = Vector2.Angle(v1, v2);
            Assert.AreApproximatelyEqual(angle, angle2);
        }
        {
            Vector3 v1 = Vector3.up;
            Vector2 v2 = Vector3.right;
            float angle = UnityVectorExtensions.Angle(v1, v2);
            Assert.AreApproximatelyEqual(angle, 90f);
            float angle2 = Vector2.Angle(v1, v2);
            Assert.AreApproximatelyEqual(angle, angle2);
        }
        {
            Vector3 v1 = Vector3.up;
            Vector2 v2 = new Vector3(-0.0001f, 1, 0);
            float angle = UnityVectorExtensions.Angle(v1, v2);
            Assert.AreApproximatelyEqual(angle, 0.00572958f);
        }
        {
            Vector3 v1 = Vector3.up;
            Vector2 v2 = new Vector3(0.0001f, 1, 0);
            float angle = UnityVectorExtensions.Angle(v1, v2);
            Assert.AreApproximatelyEqual(angle, 0.00572958f);
        }
    }
    
    [Test]
    public void TestSignedAngle()
    {
        {
            Vector3 v1 = Vector3.up;
            Vector2 v2 = Vector3.left;
            float angle = UnityVectorExtensions.SignedAngle(v1, v2, Vector3.forward);
            Assert.AreApproximatelyEqual(angle, 90f);
            float angle2 = Vector2.SignedAngle(v1, v2);
            Assert.AreApproximatelyEqual(angle, angle2);
            float angle3 = UnityVectorExtensions.SignedAngle(v1, v2, Vector3.back);
            Assert.AreApproximatelyEqual(angle, -angle3);
        }
        {
            Vector3 v1 = Vector3.up;
            Vector2 v2 = Vector3.right;
            float angle = UnityVectorExtensions.SignedAngle(v1, v2, Vector3.forward);
            Assert.AreApproximatelyEqual(angle, -90f);
            float angle2 = Vector2.SignedAngle(v1, v2);
            Assert.AreApproximatelyEqual(angle, angle2);
            float angle3 = UnityVectorExtensions.SignedAngle(v1, v2, Vector3.back);
            Assert.AreApproximatelyEqual(angle, -angle3);
        }
        {
            Vector3 v1 = Vector3.up;
            Vector2 v2 = new Vector3(-0.0001f, 1, 0);
            float angle = UnityVectorExtensions.SignedAngle(v1, v2, Vector3.forward);
            Assert.AreApproximatelyEqual(angle, 0.00572958f);
            float angle3 = UnityVectorExtensions.SignedAngle(v1, v2, Vector3.back);
            Assert.AreApproximatelyEqual(angle, -angle3);
        }
        {
            Vector3 v1 = Vector3.up;
            Vector2 v2 = new Vector3(0.0001f, 1, 0);
            float angle = UnityVectorExtensions.SignedAngle(v1, v2, Vector3.forward);
            Assert.AreApproximatelyEqual(angle, -0.00572958f);
            float angle3 = UnityVectorExtensions.SignedAngle(v1, v2, Vector3.back);
            Assert.AreApproximatelyEqual(angle, -angle3);
        }
    }
}