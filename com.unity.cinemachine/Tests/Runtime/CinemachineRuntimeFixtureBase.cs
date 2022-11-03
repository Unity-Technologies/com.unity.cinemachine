using System.Collections;
using Cinemachine;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools.Utils;

namespace Tests.Runtime
{
    public class CinemachineRuntimeFixtureBase : CinemachineFixtureBase
    {
        protected FloatEqualityComparer m_FloatEqualityComparer = new(0.0001f);
        protected Vector3EqualityComparer m_Vector3EqualityComparer = new(0.0001f);
        
        [SetUp]
        public override void SetUp()
        {
            base.SetUp();
            
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
