#if CINEMACHINE_PHYSICS_2D
using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Unity.Cinemachine.Tests
{
    [TestFixture]
    public class Confiner2DUnitTests : CinemachineRuntimeFixtureBase
    {
        CinemachineCamera m_Vcam;
        CinemachineConfiner2D m_Confiner2D;

        [SetUp]
        public override void SetUp()
        {
            base.SetUp();
            
            var vcamHolder = CreateGameObject("CM Vcam", typeof(CinemachineCamera), typeof(CinemachineConfiner2D));
            m_Vcam = vcamHolder.GetComponent<CinemachineCamera>();
            m_Confiner2D = vcamHolder.GetComponent<CinemachineConfiner2D>();
            m_Vcam.Priority.Value = 100;
            m_Cam.orthographic = true;
            m_Vcam.AddExtension(m_Confiner2D);

            m_Vcam.Lens.OrthographicSize = UnityVectorExtensions.Epsilon;
        }

        [TearDown]
        public override void TearDown()
        {
            base.TearDown();
        }

        static IEnumerable ColliderTestCases
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
            m_Confiner2D.BoundingShape2D = polygonCollider2D;
            m_Confiner2D.Damping = 0;
            m_Confiner2D.OversizeWindow = new () { Enabled = true, MaxWindowSize = 0 };

            // clockwise
            polygonCollider2D.points = testPoints;
            
            m_Confiner2D.InvalidateBoundingShapeCache();

            m_Vcam.transform.position = Vector3.zero;
            yield return null; // wait one frame
            Assert.That(m_Vcam.State.GetCorrectedPosition(), Is.EqualTo(Vector3.zero).Using(m_Vector3EqualityComparer));

            m_Vcam.transform.position = Vector2.left * 2f;
            yield return null; // wait one frame
            Assert.That((m_Vcam.State.GetCorrectedPosition() - Vector3.left).sqrMagnitude, Is.LessThan(UnityVectorExtensions.Epsilon));

            m_Vcam.transform.position = Vector2.up * 2f;
            yield return null; // wait one frame
            Assert.That((m_Vcam.State.GetCorrectedPosition() - Vector3.up).sqrMagnitude, Is.LessThan(UnityVectorExtensions.Epsilon));

            m_Vcam.transform.position = Vector2.right * 2f;
            yield return null; // wait one frame
            Assert.That((m_Vcam.State.GetCorrectedPosition() - Vector3.right).sqrMagnitude, Is.LessThan(UnityVectorExtensions.Epsilon));

            m_Vcam.transform.position = Vector2.down * 2f;
            yield return null; // wait one frame
            Assert.That((m_Vcam.State.GetCorrectedPosition() - Vector3.down).sqrMagnitude, Is.LessThan(UnityVectorExtensions.Epsilon));
        }

#if false
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
#if UNITY_2023_1_OR_NEWER
            polygonCollider2D.compositeOperation = Collider2D.CompositeOperation.Merge;
#else
            polygonCollider2D.usedByComposite = true;
#endif
            m_Confiner2D.BoundingShape2D = compositeCollider2D;
            m_Confiner2D.Damping = 0;
            m_Confiner2D.OversizeWindow = new () { Enabled = true, MaxWindowSize = 0 };
            
            // clockwise
            polygonCollider2D.points = testPoints;
            m_Confiner2D.InvalidateBoundingShapeCache();

            m_Vcam.transform.position = Vector3.zero;
            yield return null; // wait one frame
            Assert.That(m_Vcam.State.GetCorrectedPosition(), Is.EqualTo(Vector3.zero).Using(m_Vector3EqualityComparer));

            m_Vcam.transform.position = Vector2.left * 2f;
            yield return null; // wait one frame
            Assert.That((m_Vcam.State.GetCorrectedPosition() - Vector3.left).sqrMagnitude, Is.LessThan(UnityVectorExtensions.Epsilon));

            m_Vcam.transform.position = Vector2.up * 2f;
            yield return null; // wait one frame
            Assert.That((m_Vcam.State.GetCorrectedPosition() - Vector3.up).sqrMagnitude, Is.LessThan(UnityVectorExtensions.Epsilon));

            m_Vcam.transform.position = Vector2.right * 2f;
            yield return null; // wait one frame
            Assert.That((m_Vcam.State.GetCorrectedPosition() - Vector3.right).sqrMagnitude, Is.LessThan(UnityVectorExtensions.Epsilon));

            m_Vcam.transform.position = Vector2.down * 2f;
            yield return null; // wait one frame
            Assert.That((m_Vcam.State.GetCorrectedPosition() - Vector3.down).sqrMagnitude, Is.LessThan(UnityVectorExtensions.Epsilon));
        }
#endif        
        [UnityTest]
        public IEnumerator Test_SimpleSquareConfiner_BoxCollider2D()
        {
            var boxCollider2D = CreateGameObject("BoxCollider2DHolder", typeof(BoxCollider2D)).GetComponent<BoxCollider2D>();
            m_Confiner2D.BoundingShape2D = boxCollider2D;
            m_Confiner2D.Damping = 0;
            m_Confiner2D.OversizeWindow = new () { Enabled = true, MaxWindowSize = 0 };

            // clockwise
            boxCollider2D.size = new Vector2(2f, 2f);
            
            m_Confiner2D.InvalidateBoundingShapeCache();
            m_Confiner2D.BakeBoundingShape(m_Vcam, 5);

            m_Vcam.transform.position = Vector3.zero;
            yield return null; // wait one frame
            Assert.That(m_Vcam.State.GetCorrectedPosition(), Is.EqualTo(Vector3.zero).Using(m_Vector3EqualityComparer));

            m_Vcam.transform.position = Vector2.left * 2f;
            yield return null; // wait one frame
            Assert.That((m_Vcam.State.GetCorrectedPosition() - Vector3.left).sqrMagnitude, Is.LessThan(UnityVectorExtensions.Epsilon));

            m_Vcam.transform.position = Vector2.up * 2f;
            yield return null; // wait one frame
            Assert.That((m_Vcam.State.GetCorrectedPosition() - Vector3.up).sqrMagnitude, Is.LessThan(UnityVectorExtensions.Epsilon));

            m_Vcam.transform.position = Vector2.right * 2f;
            yield return null; // wait one frame
            Assert.That((m_Vcam.State.GetCorrectedPosition() - Vector3.right).sqrMagnitude, Is.LessThan(UnityVectorExtensions.Epsilon));

            m_Vcam.transform.position = Vector2.down * 2f;
            yield return null; // wait one frame
            Assert.That((m_Vcam.State.GetCorrectedPosition() - Vector3.down).sqrMagnitude, Is.LessThan(UnityVectorExtensions.Epsilon));
        }
    }
}
#endif
