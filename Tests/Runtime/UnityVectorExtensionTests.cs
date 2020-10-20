using System.Collections.Generic;
using Cinemachine;
using UnityEngine;
using UnityEngine.TestTools;
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
            Assert.IsTrue(Mathf.Abs(intersection.x) < 1e-8f &&
                          Mathf.Abs(intersection.y) < 1e-8f); // intersection should be Vector2.zero
        }
        {
            var l1_p1 = new Vector2(0, 1);
            var l1_p2 = new Vector2(0, 0);
            var l2_p1 = new Vector2(-1, 0);
            var l2_p2 = new Vector2(1, 0);
            int intersectionType = UnityVectorExtensions.FindIntersection(l1_p1, l1_p2, l2_p1, l2_p2, 
                out Vector2 intersection);
            Assert.IsTrue(intersectionType == 2);
            Assert.IsTrue(Mathf.Abs(intersection.x) < 1e-8f &&
                          Mathf.Abs(intersection.y) < 1e-8f); // intersection should be Vector2.zero
        }
        {
            var l1_p1 = new Vector2(0, 2);
            var l1_p2 = new Vector2(0, 1);
            var l2_p1 = new Vector2(-1, 0);
            var l2_p2 = new Vector2(1, 0);
            int intersectionType = UnityVectorExtensions.FindIntersection(l1_p1, l1_p2, l2_p1, l2_p2, 
                out Vector2 intersection);
            Assert.IsTrue(intersectionType == 1);
            Assert.IsTrue(Mathf.Abs(intersection.x) < 1e-8f &&
                          Mathf.Abs(intersection.y) < 1e-8f); // intersection should be Vector2.zero
        }
        {
            var l1_p1 = new Vector2(0, 2);
            var l1_p2 = new Vector2(0, 1);
            var l2_p1 = new Vector2(1, 2);
            var l2_p2 = new Vector2(1, 1);
            int intersectionType = UnityVectorExtensions.FindIntersection(l1_p1, l1_p2, l2_p1, l2_p2, 
                out Vector2 intersection);
            Assert.IsTrue(intersectionType == 0);
        }
        {
            var l1_p1 = new Vector2(0, 1);
            var l1_p2 = new Vector2(0, 1);
            var l2_p1 = new Vector2(1, 0);
            var l2_p2 = new Vector2(1, 0);
            int intersectionType = UnityVectorExtensions.FindIntersection(l1_p1, l1_p2, l2_p1, l2_p2, 
                out Vector2 intersection);
            Assert.IsTrue(intersectionType == 0);
        }
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

    [Test]
    public void TestPolygonDivision()
    {
        List<ShrinkablePolygon> subShrinkablePolygon1;
        {
            List<List<Vector2>> points = new List<List<Vector2>>();
            {
                var inputPolygon = new List<Vector2>
                {
                    new Vector2(0, 1),
                    new Vector2(0, -1),
                    new Vector2(1, 0),
                    new Vector2(-1, 0)
                };
                points.Add(inputPolygon);
            }
            List<List<ShrinkablePolygon>> polygons = ConfinerOven.CreateShrinkablePolygons(points, 1f);
            Assert.IsTrue(polygons.Count == 1);
            Assert.IsTrue(polygons[0].Count == 1);
            ShrinkablePolygon.DivideAlongIntersections(polygons[0][0], out subShrinkablePolygon1);

            Assert.IsTrue(subShrinkablePolygon1.Count == 2);
            for (var index = 0; index < subShrinkablePolygon1.Count; index++)
            {
                subShrinkablePolygon1[index].Simplify(CinemachineConfiner2D.m_bakedConfinerResolution);
                Assert.IsTrue(subShrinkablePolygon1[index].m_points.Count == 3);
            }

            Assert.IsTrue(subShrinkablePolygon1[0].m_points[2].m_position == Vector2.zero);
            Assert.IsTrue(subShrinkablePolygon1[1].m_points[0].m_position == Vector2.zero);
        }
        List<ShrinkablePolygon> subShrinkablePolygon2;
        {
            List<List<Vector2>> points = new List<List<Vector2>>();
            {
                var inputPolygon = new List<Vector2>
                {
                    new Vector2(-1, 0),
                    new Vector2(1, 0),
                    new Vector2(0, -1),
                    new Vector2(0, 1), 
                };
                points.Add(inputPolygon);
            }
            List<List<ShrinkablePolygon>> polygons = ConfinerOven.CreateShrinkablePolygons(points, 1f);
            Assert.IsTrue(polygons.Count == 1);
            Assert.IsTrue(polygons[0].Count == 1);
            ShrinkablePolygon.DivideAlongIntersections(polygons[0][0], out subShrinkablePolygon2);
        
            Assert.IsTrue(subShrinkablePolygon2.Count == 2);
            for (var index = 0; index < subShrinkablePolygon2.Count; index++)
            {
                subShrinkablePolygon2[index].Simplify(CinemachineConfiner2D.m_bakedConfinerResolution);
                Assert.IsTrue(subShrinkablePolygon2[index].m_points.Count == 3);
            }

            Assert.IsTrue(subShrinkablePolygon2[0].m_points[1].m_position == Vector2.zero);
            Assert.IsTrue(subShrinkablePolygon2[1].m_points[0].m_position == Vector2.zero);
        }

        
        Assert.IsTrue(
            subShrinkablePolygon1[0].m_points[0].m_position == subShrinkablePolygon2[0].m_points[0].m_position);
        Assert.IsTrue(
            subShrinkablePolygon1[0].m_points[1].m_position == subShrinkablePolygon2[0].m_points[2].m_position);
        Assert.IsTrue(
            subShrinkablePolygon1[0].m_points[2].m_position == subShrinkablePolygon2[0].m_points[1].m_position);
        Assert.IsTrue(
            subShrinkablePolygon1[1].m_points[0].m_position == subShrinkablePolygon2[1].m_points[0].m_position);
        Assert.IsTrue(
            subShrinkablePolygon1[1].m_points[1].m_position == subShrinkablePolygon2[1].m_points[2].m_position);
        Assert.IsTrue(
            subShrinkablePolygon1[1].m_points[2].m_position == subShrinkablePolygon2[1].m_points[1].m_position);
    }
}
