using System.Collections;
using Cinemachine;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Tests.HDRP.Runtime
{
    [TestFixture]
    public class CheckPhysicalCameraProperties : CinemachineHDRPFixtureBase
    {
        Camera m_Cam;
        CmCamera m_CmCamera;

        [SetUp]
        public override void SetUp()
        {
            base.SetUp();
            
            m_Cam = CreateGameObject("MainCamera", typeof(Camera), typeof(CinemachineBrain)).GetComponent<Camera>();
            m_Cam.usePhysicalProperties = true;
            
            var vcamHolder = CreateGameObject("CM Vcam", typeof(CmCamera), typeof(CinemachineConfiner2D));
            m_CmCamera = vcamHolder.GetComponent<CmCamera>();
        }

        [TearDown]
        public override void TearDown()
        {
            base.TearDown();
        }
        
        [UnityTest]
        public IEnumerator IsPhysical()
        {
            yield return null;
            
            Assert.True(m_Cam.usePhysicalProperties);
            Assert.True(m_CmCamera.Lens.IsPhysicalCamera);
        }

        [UnityTest]
        public IEnumerator CheckAll()
        {
            // Check that initial lens state of cmCamera is equal to Camera's lens
            yield return null;
            {
                var state = m_CmCamera.State;
                Assert.That(m_Cam.sensorSize, Is.EqualTo(state.Lens.SensorSize));
                Assert.That(m_Cam.gateFit, Is.EqualTo(state.Lens.GateFit));
                Assert.That(m_Cam.focalLength, 
                    Is.EqualTo(Camera.FieldOfViewToFocalLength(state.Lens.FieldOfView, state.Lens.SensorSize.y)));
                Assert.That(m_Cam.lensShift, Is.EqualTo(state.Lens.LensShift));
                Assert.That(m_Cam.focusDistance, Is.EqualTo(state.Lens.FocusDistance));
                Assert.That(m_Cam.iso, Is.EqualTo(state.Lens.Iso));
                Assert.That(m_Cam.shutterSpeed, Is.EqualTo(state.Lens.ShutterSpeed));
                Assert.That(m_Cam.aperture, Is.EqualTo(state.Lens.Aperture));
                Assert.That(m_Cam.bladeCount, Is.EqualTo(state.Lens.BladeCount));
                Assert.That(m_Cam.curvature, Is.EqualTo(state.Lens.Curvature));
                Assert.That(m_Cam.barrelClipping, Is.EqualTo(state.Lens.BarrelClipping));
                Assert.That(m_Cam.anamorphism, Is.EqualTo(state.Lens.Anamorphism));
            }

            // Modify lens on cmCamera
            {
                m_CmCamera.Lens.SensorSize += Vector2.one;
                m_CmCamera.Lens.GateFit = Camera.GateFitMode.Overscan;
                m_CmCamera.Lens.FieldOfView += 10;
                m_CmCamera.Lens.LensShift += Vector2.one;
                m_CmCamera.Lens.FocusDistance += 10;
                m_CmCamera.Lens.Iso += 1;
                m_CmCamera.Lens.ShutterSpeed += 1;
                m_CmCamera.Lens.Aperture += 1;
                m_CmCamera.Lens.BladeCount += 1;
                m_CmCamera.Lens.Curvature += Vector2.one;
                m_CmCamera.Lens.BarrelClipping = 0.5f;
                m_CmCamera.Lens.Anamorphism = 0.5f;
            }

            // Check that modification is present in cmCamera's state
            // Check that modification was applied to Camera's lens
            yield return null;
            {
                var cnCameraLens = m_CmCamera.Lens;
                var state = m_CmCamera.State;
                
                Assert.That(cnCameraLens.SensorSize, Is.EqualTo(state.Lens.SensorSize));
                Assert.That(cnCameraLens.GateFit, Is.EqualTo(state.Lens.GateFit));
                Assert.That(Camera.FieldOfViewToFocalLength(cnCameraLens.FieldOfView, cnCameraLens.SensorSize.y), 
                    Is.EqualTo(Camera.FieldOfViewToFocalLength(state.Lens.FieldOfView, state.Lens.SensorSize.y)));
                Assert.That(cnCameraLens.LensShift, Is.EqualTo(state.Lens.LensShift));
                Assert.That(cnCameraLens.FocusDistance, Is.EqualTo(state.Lens.FocusDistance));
                Assert.That(cnCameraLens.Iso, Is.EqualTo(state.Lens.Iso));
                Assert.That(cnCameraLens.ShutterSpeed, Is.EqualTo(state.Lens.ShutterSpeed));
                Assert.That(cnCameraLens.Aperture, Is.EqualTo(state.Lens.Aperture));
                Assert.That(cnCameraLens.BladeCount, Is.EqualTo(state.Lens.BladeCount));
                Assert.That(cnCameraLens.Curvature, Is.EqualTo(state.Lens.Curvature));
                Assert.That(cnCameraLens.BarrelClipping, Is.EqualTo(state.Lens.BarrelClipping));
                Assert.That(cnCameraLens.Anamorphism, Is.EqualTo(state.Lens.Anamorphism));
                
                Assert.That(m_Cam.sensorSize, Is.EqualTo(state.Lens.SensorSize));
                Assert.That(m_Cam.gateFit, Is.EqualTo(state.Lens.GateFit));
                Assert.That(m_Cam.focalLength, 
                    Is.EqualTo(Camera.FieldOfViewToFocalLength(state.Lens.FieldOfView, state.Lens.SensorSize.y)));
                Assert.That(m_Cam.lensShift, Is.EqualTo(state.Lens.LensShift));
                Assert.That(m_Cam.focusDistance, Is.EqualTo(state.Lens.FocusDistance));
                Assert.That(m_Cam.iso, Is.EqualTo(state.Lens.Iso));
                Assert.That(m_Cam.shutterSpeed, Is.EqualTo(state.Lens.ShutterSpeed));
                Assert.That(m_Cam.aperture, Is.EqualTo(state.Lens.Aperture));
                Assert.That(m_Cam.bladeCount, Is.EqualTo(state.Lens.BladeCount));
                Assert.That(m_Cam.curvature, Is.EqualTo(state.Lens.Curvature));
                Assert.That(m_Cam.barrelClipping, Is.EqualTo(state.Lens.BarrelClipping));
                Assert.That(m_Cam.anamorphism, Is.EqualTo(state.Lens.Anamorphism));
            }
        }
    }
}
