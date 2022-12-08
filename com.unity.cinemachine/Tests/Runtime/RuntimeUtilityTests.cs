#if CINEMACHINE_PHYSICS
using System.Collections;
using Cinemachine;
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
        static readonly Vector3 k_BoxPosition = new (0, 0, 10);

        [SetUp]
        public override void SetUp()
        {
            base.SetUp();

            m_Box = CreatePrimitive(PrimitiveType.Cube);
            m_Box.tag = k_BoxTag;
            m_Box.transform.SetPositionAndRotation(k_BoxPosition, Quaternion.identity);
        }
        
        [UnityTest]
        public IEnumerator RaycastIgnoreTagTest()
        {
            yield return WaitForOnePhysicsFrame();
            
            var ray = new Ray(Vector3.zero, k_BoxPosition);
            Assert.True(RuntimeUtility.RaycastIgnoreTag(ray, out var hitInfo, k_BoxPosition.sqrMagnitude, LayerMask.NameToLayer("Default"), string.Empty));
            Debug.Log(hitInfo.distance);
            
            Assert.False(RuntimeUtility.RaycastIgnoreTag(ray, out hitInfo, k_BoxPosition.sqrMagnitude, LayerMask.NameToLayer("Default"), k_BoxTag));
        }
        
        // [UnityTest]
        // public IEnumerator SphereCastIgnoreTagTest()
        // {
        //     
        // }
    }
}
#endif
