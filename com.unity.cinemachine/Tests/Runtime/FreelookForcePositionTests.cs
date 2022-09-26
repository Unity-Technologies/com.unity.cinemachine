using System.Collections;
using Cinemachine;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.TestTools.Utils;

namespace Tests.Runtime
{
    public class FreelookForcePositionTests : CinemachineFixtureBase
    {
        CinemachineFreeLook m_Freelook;
        Vector3 m_OriginalPosition;
        Quaternion m_OriginalOrientation;
        
        [SetUp]
        public override void SetUp()
        {
            CreateGameObject("MainCamera", typeof(Camera), typeof(CinemachineBrain)).GetComponent<Camera>();
            var vcamHolder = CreateGameObject("CM Freelook", typeof(CinemachineFreeLook));
            m_Freelook = vcamHolder.GetComponent<CinemachineFreeLook>();
            m_Freelook.Priority = 100;

            base.SetUp();
        }

        [TearDown]
        public override void TearDown()
        {
            base.TearDown();
        }

        static IEnumerable RigSetups
        {
            get
            {
                yield return new TestCaseData(new[]
                {
                    new CinemachineFreeLook.Orbit { m_Height = 0, m_Radius = 1 },
                    new CinemachineFreeLook.Orbit { m_Height = 1, m_Radius = 2 },
                    new CinemachineFreeLook.Orbit { m_Height = 2, m_Radius = 3 }
                }).SetName("Rig1").Returns(null);

                yield return new TestCaseData(new[]
                {
                    new CinemachineFreeLook.Orbit { m_Height = 5, m_Radius = 3 },
                    new CinemachineFreeLook.Orbit { m_Height = 3, m_Radius = 1 },
                    new CinemachineFreeLook.Orbit { m_Height = 1, m_Radius = 2 }
                }).SetName("Rig2").Returns(null);

                yield return new TestCaseData(new[]
                {
                    new CinemachineFreeLook.Orbit { m_Height = -1, m_Radius = 3 },
                    new CinemachineFreeLook.Orbit { m_Height = -3, m_Radius = 8 },
                    new CinemachineFreeLook.Orbit { m_Height = -5, m_Radius = 5 }
                }).SetName("Rig3").Returns(null);
            }
        }

        [UnityTest, TestCaseSource(nameof(RigSetups))]
        public IEnumerator Test_Freelook_ForcePosition_Precision(CinemachineFreeLook.Orbit[] rigSetup)
        {
            for (var i = 0; i < m_Freelook.m_Orbits.Length; ++i) 
                m_Freelook.m_Orbits[i] = rigSetup[i];
            
            for (var i = 0; i < 10; ++i)
            {
                // set axis randomly
                m_Freelook.m_XAxis.Value = Random.Range(0f, 180f);
                m_Freelook.m_YAxis.Value = Random.Range(0f, 1f);
        
                yield return null;
        
                // save camera current position and rotation
                m_OriginalPosition = m_Freelook.State.CorrectedPosition;
                m_OriginalOrientation = m_Freelook.State.CorrectedOrientation;
        
                yield return null;
        
                m_Freelook.ForceCameraPosition(m_OriginalPosition, m_OriginalOrientation);
        
                yield return null;
        
                Assert.That(m_Freelook.State.CorrectedPosition, Is.EqualTo(m_OriginalPosition).Using(new Vector3EqualityComparer(0.01f)));
                Assert.That(m_Freelook.State.CorrectedOrientation, Is.EqualTo(m_OriginalOrientation).Using(new QuaternionEqualityComparer(0.001f)));
            }
        }
    }
}
