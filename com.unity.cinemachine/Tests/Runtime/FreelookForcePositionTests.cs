using System;
using System.Collections;
using Cinemachine;
using Cinemachine.TargetTracking;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.TestTools.Utils;

namespace Tests.Runtime
{
    public class FreelookForcePositionTests : CinemachineFixtureBase
    {
        CmCamera m_CmCamera;
        CinemachineOrbitalFollow m_OrbitalFollow;
        GameObject m_FollowTargetGo;
        Vector3 m_OriginalPosition;
        Quaternion m_OriginalOrientation;
        static readonly Vector2 k_AxisRange = new(-180f, 180f);
        static readonly Array k_BindingModes = Enum.GetValues(typeof(BindingMode));

        [SetUp]
        public override void SetUp()
        {
            CreateGameObject("MainCamera", typeof(Camera), typeof(CinemachineBrain));

            var camGo = CreateGameObject("CM Freelook", typeof(CmCamera));
            m_CmCamera = camGo.GetComponent<CmCamera>();
            m_OrbitalFollow = camGo.AddComponent<CinemachineOrbitalFollow>();
            m_OrbitalFollow.HorizontalAxis.Range = k_AxisRange;
            m_OrbitalFollow.HorizontalAxis.Center = 0f;
            m_OrbitalFollow.HorizontalAxis.Wrap = true;
            m_OrbitalFollow.VerticalAxis.Range = k_AxisRange;
            m_OrbitalFollow.VerticalAxis.Center = 0f;
            m_OrbitalFollow.VerticalAxis.Wrap = false;
            m_OrbitalFollow.RadialAxis.Range = new Vector2(1f, 5f);
            m_OrbitalFollow.RadialAxis.Center = 1f;
            m_OrbitalFollow.RadialAxis.Wrap = false;
            m_OrbitalFollow.RadialAxis.Value = m_OrbitalFollow.RadialAxis.Center;
            m_OrbitalFollow.TrackerSettings.PositionDamping = Vector3.zero;
            m_OrbitalFollow.TrackerSettings.RotationDamping = Vector3.zero;
            m_OrbitalFollow.TrackerSettings.QuaternionDamping = 0;
            camGo.AddComponent<CinemachineHardLookAt>();
            
            m_FollowTargetGo = CreatePrimitive(PrimitiveType.Cube);
            m_FollowTargetGo.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
            m_CmCamera.Target.TrackingTarget = m_FollowTargetGo.transform;

            base.SetUp();
        }

        [TearDown]
        public override void TearDown()
        {
            base.TearDown();
        }

        public struct TestData
        {
            public CinemachineOrbitalFollow.OrbitStyles OrbitStyle;
            public float Radius;
            public Cinemachine3OrbitRig.Settings Orbits;
            public float Precision;
        }
        static IEnumerable RigSetups
        {
            get
            {
                yield return new TestCaseData(
                    new TestData
                    {
                        OrbitStyle = CinemachineOrbitalFollow.OrbitStyles.ThreeRing,
                        Orbits = new Cinemachine3OrbitRig.Settings { 
                            Top = new Cinemachine3OrbitRig.Orbit { Height = 5, Radius = 3},
                            Center = new Cinemachine3OrbitRig.Orbit { Height = -3, Radius = 8},
                            Bottom = new Cinemachine3OrbitRig.Orbit { Height = -5, Radius = 5}
                        },
                        Precision = 0.01f // this does not have difficult edge cases
                    }).SetName("3Ring-Centered").Returns(null);

                yield return new TestCaseData(
                    new TestData
                    {
                        OrbitStyle = CinemachineOrbitalFollow.OrbitStyles.ThreeRing,
                        Orbits = new Cinemachine3OrbitRig.Settings {
                            Top = new Cinemachine3OrbitRig.Orbit { Height = 5, Radius = 3},
                            Center = new Cinemachine3OrbitRig.Orbit { Height = 3, Radius = 1},
                            Bottom = new Cinemachine3OrbitRig.Orbit { Height = 1, Radius = 2}
                        },
                        Precision = 0.5f // this has a few difficult cases to resolve and thus error is expected
                    }).SetName("3Ring-Above").Returns(null);
                
                yield return new TestCaseData(
                    new TestData
                    {
                        OrbitStyle = CinemachineOrbitalFollow.OrbitStyles.ThreeRing,
                        Orbits = new Cinemachine3OrbitRig.Settings { 
                            Top = new Cinemachine3OrbitRig.Orbit { Height = -1, Radius = 5},
                            Center = new Cinemachine3OrbitRig.Orbit { Height = -3, Radius = 8},
                            Bottom = new Cinemachine3OrbitRig.Orbit { Height = -5, Radius = 3}
                        },
                        Precision = 0.5f // this has a few difficult cases to resolve and thus error is expected
                    }).SetName("3Ring-Below").Returns(null);

                yield return new TestCaseData(
                    new TestData
                    {
                        OrbitStyle = CinemachineOrbitalFollow.OrbitStyles.Sphere,
                        Radius = 3f,
                        Precision = 0.001f
                    }).SetName("Sphere-r3").Returns(null);
                
                yield return new TestCaseData(
                    new TestData
                    {
                        OrbitStyle = CinemachineOrbitalFollow.OrbitStyles.Sphere,
                        Radius = 7f,
                        Precision = 0.001f
                    }).SetName("Sphere-r7").Returns(null);
            }
        }

        [UnityTest, TestCaseSource(nameof(RigSetups))]
        public IEnumerator Test_Freelook_ForcePosition_AllBindings_RotatedTarget(TestData rigSetup)
        {
            m_FollowTargetGo.transform.SetPositionAndRotation(new Vector3(-5f, 11f, -7f), Quaternion.Euler(31, 31, 31));
            yield return null;
            
            yield return Test_Freelook_ForcePosition_AllBindings(rigSetup);
        }
        
        [UnityTest, TestCaseSource(nameof(RigSetups))]
        public IEnumerator Test_Freelook_ForcePosition_AllBindings(TestData rigSetup)
        {
            m_OrbitalFollow.OrbitStyle = rigSetup.OrbitStyle;
            var floatEqualityComparer = new FloatEqualityComparer(rigSetup.Precision);
            switch (rigSetup.OrbitStyle)
            {
                case CinemachineOrbitalFollow.OrbitStyles.Sphere:
                    m_OrbitalFollow.Radius = rigSetup.Radius;
                    break;
                case CinemachineOrbitalFollow.OrbitStyles.ThreeRing:
                    m_OrbitalFollow.Orbits = rigSetup.Orbits;
                    m_OrbitalFollow.Orbits.SplineCurvature = 1f;
                    break;
            }
            
            const float step = 5f; // so tests are not too long
            foreach (BindingMode bindingMode in k_BindingModes)
            {
                m_OrbitalFollow.TrackerSettings.BindingMode = bindingMode;
                yield return null;
                
                for (var axisValue = k_AxisRange.x + 1; axisValue < k_AxisRange.y; axisValue += step)
                {
                    // Set Axis values
                    m_OrbitalFollow.HorizontalAxis.Value = axisValue;
                    m_OrbitalFollow.VerticalAxis.Value = axisValue;
                    yield return null;
        
                    // Save camera current position and rotation
                    m_OriginalPosition = m_CmCamera.State.GetCorrectedPosition();
                    m_OriginalOrientation = m_CmCamera.State.GetCorrectedOrientation();
                    yield return null;
        
                    // Force camera to position
                    m_CmCamera.ForceCameraPosition(m_OriginalPosition, m_OriginalOrientation);
                    yield return null;
        
                    
                    // ignore SimpleFollowWithWorldUp because axis value is 0 always in this case
                    if (m_OrbitalFollow.TrackerSettings.BindingMode != BindingMode.SimpleFollowWithWorldUp)
                        Assert.That(m_OrbitalFollow.HorizontalAxis.Value, Is.EqualTo(axisValue).Using(floatEqualityComparer));
                    Assert.That(m_OrbitalFollow.VerticalAxis.Value, Is.EqualTo(axisValue).Using(floatEqualityComparer));
                }
            }
        }
    }
}
