using System.Collections;
using System.Collections.Generic;
using Cinemachine;
using Cinemachine.Utility;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.TestTools.Utils;

namespace Tests.Runtime
{
    public class Confiner2DUnitTests : CinemachineFixtureBase
    {
        private Camera cam;
        private CinemachineVirtualCamera vcam;
        private CinemachineConfiner2D confiner2D;

        [SetUp]
        public override void SetUp()
        {
            cam = CreateGameObject("MainCamera", typeof(Camera), typeof(CinemachineBrain)).GetComponent<Camera>();
            var vcamHolder = CreateGameObject("CM Vcam", typeof(CinemachineVirtualCamera), typeof(CinemachineConfiner2D));
            vcam = vcamHolder.GetComponent<CinemachineVirtualCamera>();
            confiner2D = vcamHolder.GetComponent<CinemachineConfiner2D>();
            vcam.Priority = 100;
            cam.orthographic = true;
            vcam.AddExtension(confiner2D);

            vcam.m_Lens.OrthographicSize = UnityVectorExtensions.Epsilon;

            base.SetUp();
        }

        [TearDown]
        public override void TearDown()
        {
            vcam.m_Lens.OrthographicSize = 1;
            
            base.TearDown();
        }

        private static IEnumerable ColliderTestCases
        {
            get
            {
                yield return new TestCaseData(new[] {Vector2.left, Vector2.up, Vector2.right, Vector2.down}).SetName("Clockwise").Returns(null);
                yield return new TestCaseData(new[] {Vector2.left, Vector2.down, Vector2.right, Vector2.up}).SetName("Counter-Clockwise").Returns(null);
            }
        }

        [UnityTest, TestCaseSource(nameof(ColliderTestCases))]
        public IEnumerator Test_SimpleSquareConfiner_OrderIndependent_PolygonCollider2D(Vector2[] testPoints)
        {
            var polygonCollider2D = CreateGameObject("PolygonCollider2DHolder", typeof(PolygonCollider2D)).GetComponent<PolygonCollider2D>();
            confiner2D.m_BoundingShape2D = polygonCollider2D;
            confiner2D.m_Damping = 0;
            confiner2D.m_MaxWindowSize = 0;

            // clockwise
            polygonCollider2D.points = testPoints;
            
            confiner2D.InvalidateCache();

            vcam.transform.position = Vector3.zero;
            yield return null; // wait one frame
            Assert.That(vcam.State.CorrectedPosition, Is.EqualTo(Vector3.zero).Using(Vector3EqualityComparer.Instance));

            vcam.transform.position = Vector2.left * 2f;
            yield return null; // wait one frame
            Assert.That((vcam.State.CorrectedPosition - Vector3.left).sqrMagnitude, Is.LessThan(UnityVectorExtensions.Epsilon));

            vcam.transform.position = Vector2.up * 2f;
            yield return null; // wait one frame
            Assert.That((vcam.State.CorrectedPosition - Vector3.up).sqrMagnitude, Is.LessThan(UnityVectorExtensions.Epsilon));

            vcam.transform.position = Vector2.right * 2f;
            yield return null; // wait one frame
            Assert.That((vcam.State.CorrectedPosition - Vector3.right).sqrMagnitude, Is.LessThan(UnityVectorExtensions.Epsilon));

            vcam.transform.position = Vector2.down * 2f;
            yield return null; // wait one frame
            Assert.That((vcam.State.CorrectedPosition - Vector3.down).sqrMagnitude, Is.LessThan(UnityVectorExtensions.Epsilon));
        }

        [UnityTest, TestCaseSource(nameof(ColliderTestCases))]
        public IEnumerator Test_SimpleSquareConfiner_OrderIndependent_CompositeCollider2D(Vector2[] testPoints)
        {
            var compositeHolder = CreateGameObject("CompositeCollider2DHolder", typeof(Rigidbody2D), typeof(CompositeCollider2D));
            var rigidbody2D = compositeHolder.GetComponent<Rigidbody2D>();
            rigidbody2D.bodyType = RigidbodyType2D.Kinematic;
            rigidbody2D.isKinematic = true;
            rigidbody2D.simulated = false;
            var compositeCollider2D = compositeHolder.GetComponent<CompositeCollider2D>();
            compositeCollider2D.geometryType = CompositeCollider2D.GeometryType.Polygons;

            var polyHolder = CreateGameObject("PolygonCollider2DHolder", typeof(PolygonCollider2D));
            polyHolder.transform.parent = compositeHolder.transform;
            var polygonCollider2D = polyHolder.GetComponent<PolygonCollider2D>();
            polygonCollider2D.usedByComposite = true;
            confiner2D.m_BoundingShape2D = compositeCollider2D;
            confiner2D.m_Damping = 0;
            confiner2D.m_MaxWindowSize = 0;
            
            // clockwise
            polygonCollider2D.points = testPoints;
            confiner2D.InvalidateCache();

            vcam.transform.position = Vector3.zero;
            yield return null; // wait one frame
            Assert.That(vcam.State.CorrectedPosition, Is.EqualTo(Vector3.zero).Using(Vector3EqualityComparer.Instance));

            vcam.transform.position = Vector2.left * 2f;
            yield return null; // wait one frame
            Assert.That((vcam.State.CorrectedPosition - Vector3.left).sqrMagnitude, Is.LessThan(UnityVectorExtensions.Epsilon));

            vcam.transform.position = Vector2.up * 2f;
            yield return null; // wait one frame
            Assert.That((vcam.State.CorrectedPosition - Vector3.up).sqrMagnitude, Is.LessThan(UnityVectorExtensions.Epsilon));

            vcam.transform.position = Vector2.right * 2f;
            yield return null; // wait one frame
            Assert.That((vcam.State.CorrectedPosition - Vector3.right).sqrMagnitude, Is.LessThan(UnityVectorExtensions.Epsilon));

            vcam.transform.position = Vector2.down * 2f;
            yield return null; // wait one frame
            Assert.That((vcam.State.CorrectedPosition - Vector3.down).sqrMagnitude, Is.LessThan(UnityVectorExtensions.Epsilon));
        }
    }
}