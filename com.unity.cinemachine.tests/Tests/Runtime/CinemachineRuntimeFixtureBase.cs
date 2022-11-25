using System.Collections;
using Cinemachine;
using Cinemachine.Utility;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools.Utils;

namespace Tests.Runtime
{
    /// <summary>
    /// A class that handles creation and deletion of GameObjects, and gets set up for RunTime testing.
    /// </summary>
    public class CinemachineRuntimeFixtureBase : CinemachineFixtureBase
    {
        protected Camera m_Cam;
        protected CinemachineBrain m_Brain;
        protected readonly FloatEqualityComparer m_FloatEqualityComparer = new(UnityVectorExtensions.Epsilon);
        protected readonly Vector3EqualityComparer m_Vector3EqualityComparer = new(UnityVectorExtensions.Epsilon);
        
        [SetUp]
        public override void SetUp()
        {
            base.SetUp();

            m_Cam = CreateGameObject("MainCamera", typeof(Camera), typeof(CinemachineBrain)).GetComponent<Camera>();
            m_Brain = m_Cam.GetComponent<CinemachineBrain>();
            
            // force a uniform deltaTime, otherwise tests will be unstable
            CinemachineCore.UniformDeltaTimeOverride = 0.1f;
            // disable delta time compensation for deterministic test results
            CinemachineCore.UnitTestMode = true;
        }
        
        [TearDown]
        public override void TearDown()
        {
            // force a uniform deltaTime, otherwise tests will be unstable
            CinemachineCore.UniformDeltaTimeOverride = -1;
            // disable delta time compensation for deterministic test results
            CinemachineCore.UnitTestMode = false;
            
            base.TearDown();
        }
        
        /// <summary>Ensures to wait until at least one physics frame.</summary>
        protected static IEnumerator WaitForOnePhysicsFrame()
        {
            yield return new WaitForFixedUpdate(); // this is needed to ensure physics system is up-to-date
            yield return null; // this is so that the frame is completed (since physics frames are not aligned)
        }
        
        
        /// <summary>Destroys an object and waits one physics frame.</summary>
        /// <param name="go">GameObject to destroy.</param>
        protected static IEnumerator PhysicsDestroy(GameObject go)
        {
            RuntimeUtility.DestroyObject(go);
            yield return WaitForOnePhysicsFrame(); // important to ensure the collider is destroyed.
        }
    }
}
