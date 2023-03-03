#if CINEMACHINE_PHYSICS
using System.Collections;
using Unity.Cinemachine;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Tests.Runtime
{
    [TestFixture]
    public class RuntimeUtilityTests : CinemachineRuntimeFixtureBase
    {
        GameObject m_Box;
        const string k_BoxTag = "Player";
        int m_LayerMask = -5; // [DefaultValue("Physics.DefaultRaycastLayers")] 
        BoxCollider m_BoxCollider;

        [SetUp]
        public override void SetUp()
        {
            base.SetUp();

            m_Box = CreatePrimitive(PrimitiveType.Cube);
            m_Box.tag = k_BoxTag;
            m_Box.transform.SetPositionAndRotation(new Vector3(0, 0, 10), Quaternion.identity);
            m_BoxCollider = m_Box.GetComponent<BoxCollider>();
            m_BoxCollider.isTrigger = false;
        }

        [TearDown]
        public override void TearDown()
        {
            base.TearDown();
        }

        [UnityTest]
        public IEnumerator RaycastIgnoreTagTest()
        {
            yield return WaitForOnePhysicsFrame(); // So that colliders are updated/moved

            var boxPosition = m_Box.transform.position;
            var ray = new Ray(Vector3.zero, boxPosition);
            var rayLength = boxPosition.magnitude;
            
            // check ray cast hit
            Assert.True(RuntimeUtility.RaycastIgnoreTag(ray, out var hitInfo, rayLength, m_LayerMask, string.Empty));
            Assert.That(hitInfo.distance, Is.EqualTo(boxPosition.z - (m_BoxCollider.size.z / 2f)));
            
            // check that ray cast did not hit anything, because it ignores the box's tag
            Assert.False(RuntimeUtility.RaycastIgnoreTag(ray, out hitInfo, rayLength, m_LayerMask, k_BoxTag));
        }
        
        [UnityTest]
        public IEnumerator SphereCastIgnoreTagTest()
        {
            yield return WaitForOnePhysicsFrame(); // So that colliders are updated/moved

            var boxPosition = m_Box.transform.position;
            var direction = boxPosition.normalized; // shooting from origin
            var rayLength = boxPosition.magnitude;
            var sphereRadius = 1f;
            
            // check ray cast hit
            Assert.True(RuntimeUtility.SphereCastIgnoreTag(Vector3.zero, sphereRadius, direction, out var hitInfo, rayLength, m_LayerMask, string.Empty));
            Assert.That(hitInfo.distance, Is.EqualTo(boxPosition.z - (m_BoxCollider.size.z / 2f) - sphereRadius));
            
            // check that ray cast did not hit anything, because it ignores the box's tag
            Assert.False(RuntimeUtility.SphereCastIgnoreTag(Vector3.zero, sphereRadius, direction, out hitInfo, rayLength, m_LayerMask, k_BoxTag));
        }
    }
}
#endif
