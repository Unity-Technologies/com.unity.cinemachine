using System.Collections;
using System.Collections.Generic;
using Cinemachine;
using UnityEngine;
using UnityEngine.TestTools;
using NUnit.Framework;
using Cinemachine.Utility;
using Assert = UnityEngine.Assertions.Assert;

public class Confiner2DTests
{
    [UnityTest]
    public IEnumerator ConfinerTest()
    {
        CreateCameraAndAddVcam(out Camera cam, out CinemachineVirtualCamera vcam);
        var confiner2D = vcam.gameObject.AddComponent<CinemachineConfiner2D>();
        vcam.AddExtension(confiner2D);
        cam.orthographic = true;
        vcam.m_Lens.OrthographicSize = UnityVectorExtensions.Epsilon;
        
        var go = new GameObject();
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
    
    private void CreateCameraAndAddVcam(out Camera cam, out CinemachineVirtualCamera vcam)
    {
        var cameraHolder = new GameObject();
        cam = cameraHolder.AddComponent<Camera>();
        cameraHolder.AddComponent<CinemachineBrain>();
        
        var vcamHolder = new GameObject();
        vcam = vcamHolder.AddComponent<CinemachineVirtualCamera>();
        vcam.Priority = 100;
    }
}
