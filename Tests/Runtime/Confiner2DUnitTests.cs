using System.Collections;
using System.Collections.Generic;
using Cinemachine;
using Cinemachine.Utility;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public class Confiner2DUnitTests
{
    [UnityTest]
    public IEnumerator Test_PolygonDivision_OrderIndependent()
    {
        var aspectData = new ShrinkablePolygon.AspectData(1);    

        List<ShrinkablePolygon> subShrinkablePolygon1 = new List<ShrinkablePolygon>();
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
            List<List<ShrinkablePolygon>> polygons = ConfinerOven.CreateShrinkablePolygons(points);
            Assert.IsTrue(polygons.Count == 1);
            Assert.IsTrue(polygons[0].Count == 1);
            ShrinkablePolygon.DivideAlongIntersections(polygons[0][0], ref subShrinkablePolygon1, aspectData);

            Assert.IsTrue(subShrinkablePolygon1.Count == 2);
            for (var index = 0; index < subShrinkablePolygon1.Count; index++)
            {
                subShrinkablePolygon1[index].Simplify(ConfinerOven.k_MinStepSize);
                Assert.IsTrue(subShrinkablePolygon1[index].m_Points.Count == 3);
            }

            Assert.IsTrue(subShrinkablePolygon1[0].m_Points[2].m_Position == Vector2.zero);
            Assert.IsTrue(subShrinkablePolygon1[1].m_Points[0].m_Position == Vector2.zero);
        }
        List<ShrinkablePolygon> subShrinkablePolygon2 = new List<ShrinkablePolygon>();
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
            List<List<ShrinkablePolygon>> polygons = ConfinerOven.CreateShrinkablePolygons(points);
            Assert.IsTrue(polygons.Count == 1);
            Assert.IsTrue(polygons[0].Count == 1);
            ShrinkablePolygon.DivideAlongIntersections(polygons[0][0], ref subShrinkablePolygon2, aspectData);
        
            Assert.IsTrue(subShrinkablePolygon2.Count == 2);
            for (var index = 0; index < subShrinkablePolygon2.Count; index++)
            {
                subShrinkablePolygon2[index].Simplify(ConfinerOven.k_MinStepSize);
                Assert.IsTrue(subShrinkablePolygon2[index].m_Points.Count == 3);
            }

            Assert.IsTrue(subShrinkablePolygon2[0].m_Points[1].m_Position == Vector2.zero);
            Assert.IsTrue(subShrinkablePolygon2[1].m_Points[0].m_Position == Vector2.zero);
        }

        
        Assert.IsTrue(
            subShrinkablePolygon1[0].m_Points[0].m_Position == subShrinkablePolygon2[0].m_Points[0].m_Position);
        Assert.IsTrue(
            subShrinkablePolygon1[0].m_Points[1].m_Position == subShrinkablePolygon2[0].m_Points[2].m_Position);
        Assert.IsTrue(
            subShrinkablePolygon1[0].m_Points[2].m_Position == subShrinkablePolygon2[0].m_Points[1].m_Position);
        Assert.IsTrue(
            subShrinkablePolygon1[1].m_Points[0].m_Position == subShrinkablePolygon2[1].m_Points[0].m_Position);
        Assert.IsTrue(
            subShrinkablePolygon1[1].m_Points[1].m_Position == subShrinkablePolygon2[1].m_Points[2].m_Position);
        Assert.IsTrue(
            subShrinkablePolygon1[1].m_Points[2].m_Position == subShrinkablePolygon2[1].m_Points[1].m_Position);

        yield return null;
    }
   
    [UnityTest]
    public IEnumerator Test_SimpleSquareConfiner_OrderIndependent_PolygonCollider2D()
    {
        CreateCameraAndAddVcam(out Camera cam, out CinemachineVirtualCamera vcam);
        var confiner2D = vcam.gameObject.AddComponent<CinemachineConfiner2D>();
        vcam.AddExtension(confiner2D);
        cam.orthographic = true;
        vcam.m_Lens.OrthographicSize = UnityVectorExtensions.Epsilon;
        
        var go = new GameObject("PolygonCollider2DHolder");
        var polygonCollider2D = go.AddComponent<PolygonCollider2D>();
        confiner2D.m_BoundingShape2D = polygonCollider2D;
        confiner2D.m_Damping = 0;
        confiner2D.m_MaxWindowSize = 0;
        { // clockwise
            polygonCollider2D.points = new[]
            {
                Vector2.left,
                Vector2.up,
                Vector2.right,
                Vector2.down,
            };
            confiner2D.InvalidateCache();
            
            vcam.transform.position = Vector3.zero;
            yield return null; // wait one frame
            Assert.IsTrue(vcam.State.CorrectedPosition == Vector3.zero);

            vcam.transform.position = Vector2.left * 2f;
            yield return null; // wait one frame
            Assert.IsTrue((vcam.State.CorrectedPosition - Vector3.left).sqrMagnitude < UnityVectorExtensions.Epsilon);
            
            vcam.transform.position = Vector2.up * 2f;
            yield return null; // wait one frame
            Assert.IsTrue((vcam.State.CorrectedPosition - Vector3.up).sqrMagnitude < UnityVectorExtensions.Epsilon);
            
            vcam.transform.position = Vector2.right * 2f;
            yield return null; // wait one frame
            Assert.IsTrue((vcam.State.CorrectedPosition - Vector3.right).sqrMagnitude < UnityVectorExtensions.Epsilon);
            
            vcam.transform.position = Vector2.down * 2f;
            yield return null; // wait one frame
            Assert.IsTrue((vcam.State.CorrectedPosition - Vector3.down).sqrMagnitude < UnityVectorExtensions.Epsilon);
            
            vcam.transform.position = 1.5f * (Vector2.down + Vector2.right) / 2f;
            yield return null; // wait one frame
            Assert.IsTrue((vcam.State.CorrectedPosition - (Vector3.down + Vector3.right) / 2f).sqrMagnitude < 
                          UnityVectorExtensions.Epsilon);
        }

        { // counter clockwise
            polygonCollider2D.points = new[]
            {
                Vector2.left,
                Vector2.down,
                Vector2.right,
                Vector2.up,
            };
            
            confiner2D.InvalidateCache();
            
            vcam.transform.position = Vector3.zero;
            yield return null; // wait one frame
            Assert.IsTrue(vcam.State.CorrectedPosition == Vector3.zero);

            vcam.transform.position = Vector2.left * 2f;
            yield return null; // wait one frame
            Assert.IsTrue((vcam.State.CorrectedPosition - Vector3.left).sqrMagnitude < UnityVectorExtensions.Epsilon);
            
            vcam.transform.position = Vector2.up * 2f;
            yield return null; // wait one frame
            Assert.IsTrue((vcam.State.CorrectedPosition - Vector3.up).sqrMagnitude < UnityVectorExtensions.Epsilon);
            
            vcam.transform.position = Vector2.right * 2f;
            yield return null; // wait one frame
            Assert.IsTrue((vcam.State.CorrectedPosition - Vector3.right).sqrMagnitude < UnityVectorExtensions.Epsilon);
            
            vcam.transform.position = Vector2.down * 2f;
            yield return null; // wait one frame
            Assert.IsTrue((vcam.State.CorrectedPosition - Vector3.down).sqrMagnitude < UnityVectorExtensions.Epsilon);
            
            vcam.transform.position = 1.5f * (Vector2.down + Vector2.right) / 2f;
            yield return null; // wait one frame
            Assert.IsTrue((vcam.State.CorrectedPosition - (Vector3.down + Vector3.right) / 2f).sqrMagnitude < 
                          UnityVectorExtensions.Epsilon);
        }
        
        vcam.m_Lens.OrthographicSize = 1;
    }

    [UnityTest]
    public IEnumerator Test_SimpleSquareConfiner_OrderIndependent_CompositeCollider2D()
    {
        CreateCameraAndAddVcam(out Camera cam, out CinemachineVirtualCamera vcam);
        var confiner2D = vcam.gameObject.AddComponent<CinemachineConfiner2D>();
        vcam.AddExtension(confiner2D);
        cam.orthographic = true;
        vcam.m_Lens.OrthographicSize = UnityVectorExtensions.Epsilon;
        
        var compositeHolder = new GameObject("CompositeCollider2DHolder");
        var rigidbody2D = compositeHolder.AddComponent<Rigidbody2D>();
        rigidbody2D.bodyType = RigidbodyType2D.Kinematic;
        rigidbody2D.isKinematic = true;
        rigidbody2D.simulated = false;
        var compositeCollider2D = compositeHolder.AddComponent<CompositeCollider2D>();
        compositeCollider2D.geometryType = CompositeCollider2D.GeometryType.Polygons;

        var polyHolder = new GameObject("PolygonCollider2DHolder");
        polyHolder.transform.parent = compositeHolder.transform;
        var polygonCollider2D = polyHolder.AddComponent<PolygonCollider2D>();
        polygonCollider2D.usedByComposite = true;
        confiner2D.m_BoundingShape2D = compositeCollider2D;
        confiner2D.m_Damping = 0;
        confiner2D.m_MaxWindowSize = 0;
        { // clockwise
            polygonCollider2D.points = new[]
            {
                Vector2.left,
                Vector2.up,
                Vector2.right,
                Vector2.down,
            };
            confiner2D.InvalidateCache();
            
            vcam.transform.position = Vector3.zero;
            yield return null; // wait one frame
            Assert.IsTrue(vcam.State.CorrectedPosition == Vector3.zero);

            vcam.transform.position = Vector2.left * 2f;
            yield return null; // wait one frame
            Assert.IsTrue((vcam.State.CorrectedPosition - Vector3.left).sqrMagnitude < UnityVectorExtensions.Epsilon);
            
            vcam.transform.position = Vector2.up * 2f;
            yield return null; // wait one frame
            Assert.IsTrue((vcam.State.CorrectedPosition - Vector3.up).sqrMagnitude < UnityVectorExtensions.Epsilon);
            
            vcam.transform.position = Vector2.right * 2f;
            yield return null; // wait one frame
            Assert.IsTrue((vcam.State.CorrectedPosition - Vector3.right).sqrMagnitude < UnityVectorExtensions.Epsilon);
            
            vcam.transform.position = Vector2.down * 2f;
            yield return null; // wait one frame
            Assert.IsTrue((vcam.State.CorrectedPosition - Vector3.down).sqrMagnitude < UnityVectorExtensions.Epsilon);
            
            vcam.transform.position = 1.5f * (Vector2.down + Vector2.right) / 2f;
            yield return null; // wait one frame
            Assert.IsTrue((vcam.State.CorrectedPosition - (Vector3.down + Vector3.right) / 2f).sqrMagnitude < 
                          UnityVectorExtensions.Epsilon);
        }

        { // counter clockwise
            polygonCollider2D.points = new[]
            {
                Vector2.left,
                Vector2.down,
                Vector2.right,
                Vector2.up,
            };
            
            confiner2D.InvalidateCache();
            
            vcam.transform.position = Vector3.zero;
            yield return null; // wait one frame
            Assert.IsTrue(vcam.State.CorrectedPosition == Vector3.zero);

            vcam.transform.position = Vector2.left * 2f;
            yield return null; // wait one frame
            Assert.IsTrue((vcam.State.CorrectedPosition - Vector3.left).sqrMagnitude < UnityVectorExtensions.Epsilon);
            
            vcam.transform.position = Vector2.up * 2f;
            yield return null; // wait one frame
            Assert.IsTrue((vcam.State.CorrectedPosition - Vector3.up).sqrMagnitude < UnityVectorExtensions.Epsilon);
            
            vcam.transform.position = Vector2.right * 2f;
            yield return null; // wait one frame
            Assert.IsTrue((vcam.State.CorrectedPosition - Vector3.right).sqrMagnitude < UnityVectorExtensions.Epsilon);
            
            vcam.transform.position = Vector2.down * 2f;
            yield return null; // wait one frame
            Assert.IsTrue((vcam.State.CorrectedPosition - Vector3.down).sqrMagnitude < UnityVectorExtensions.Epsilon);
            
            vcam.transform.position = 1.5f * (Vector2.down + Vector2.right) / 2f;
            yield return null; // wait one frame
            Assert.IsTrue((vcam.State.CorrectedPosition - (Vector3.down + Vector3.right) / 2f).sqrMagnitude < 
                          UnityVectorExtensions.Epsilon);
        }
        
        vcam.m_Lens.OrthographicSize = 1;
    }
    
    
    private void CreateCameraAndAddVcam(out Camera cam, out CinemachineVirtualCamera vcam)
    {
        var cameraHolder = new GameObject("MainCamera");
        cam = cameraHolder.AddComponent<Camera>();
        cameraHolder.AddComponent<CinemachineBrain>();
        
        var vcamHolder = new GameObject("CM Vcam");
        vcam = vcamHolder.AddComponent<CinemachineVirtualCamera>();
        vcam.Priority = 100;
    }
}
