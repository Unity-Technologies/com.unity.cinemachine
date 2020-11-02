using System.Collections;
using Cinemachine;
using UnityEngine;
using UnityEngine.TestTools;
using Cinemachine.Utility;
using Assert = UnityEngine.Assertions.Assert;

public class Confiner2DTests
{
    
    [UnityTest]
    public IEnumerator ConfinerPolyTest()
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
        confiner2D.m_MaxOrthoSize = 0;
        { // clockwise
            polygonCollider2D.points = new[]
            {
                Vector2.left,
                Vector2.up,
                Vector2.right,
                Vector2.down,
            };
            confiner2D.InvalidatePathCache();
            
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
            
            confiner2D.InvalidatePathCache();
            
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
    public IEnumerator ConfinerCompositeTest()
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
        confiner2D.m_MaxOrthoSize = 0;
        { // clockwise
            polygonCollider2D.points = new[]
            {
                Vector2.left,
                Vector2.up,
                Vector2.right,
                Vector2.down,
            };
            confiner2D.InvalidatePathCache();
            
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
            
            confiner2D.InvalidatePathCache();
            
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
