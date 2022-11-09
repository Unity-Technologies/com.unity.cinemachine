using System.Collections;
using Cinemachine;
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
        protected FloatEqualityComparer m_FloatEqualityComparer = new(0.0001f);
        protected Vector3EqualityComparer m_Vector3EqualityComparer = new(0.0001f);
        
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
    }
}
