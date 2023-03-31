#if TEST_CINEMACHINE_HDRP
using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Unity.Cinemachine.Tests.HDRP
{
    [TestFixture]
    public class CheckPhysicalCameraProperties : CinemachineFixtureBase
    {
        Camera m_Cam;
        CinemachineCamera m_CmCamera;

        [SetUp]
        public override void SetUp()
        {
            base.SetUp();
            
            m_Cam = CreateGameObject("MainCamera", typeof(Camera), typeof(CinemachineBrain)).GetComponent<Camera>();
            m_Cam.usePhysicalProperties = true;
            
            var vcamHolder = CreateGameObject("CM Vcam", typeof(CinemachineCamera), typeof(CinemachineConfiner2D));
            m_CmCamera = vcamHolder.GetComponent<CinemachineCamera>();
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
        public IEnumerator PhysicalPropertiesAreCorrectlyUpdated()
        {
            // Check that initial lens state of cmCamera is equal to Camera's lens
            yield return null;
            CompareLensProperties(m_CmCamera.Lens, m_CmCamera.State);
            CompareLensProperties(m_Cam, m_CmCamera.State);

            // Modify lens on cmCamera
            m_CmCamera.Lens.PhysicalProperties.SensorSize += Vector2.one;
            m_CmCamera.Lens.PhysicalProperties.GateFit = Camera.GateFitMode.Overscan;
            m_CmCamera.Lens.FieldOfView += 10;
            m_CmCamera.Lens.PhysicalProperties.LensShift += Vector2.one;
            m_CmCamera.Lens.PhysicalProperties.FocusDistance += 10;
            m_CmCamera.Lens.PhysicalProperties.Iso += 1;
            m_CmCamera.Lens.PhysicalProperties.ShutterSpeed += 1;
            m_CmCamera.Lens.PhysicalProperties.Aperture += 1;
            m_CmCamera.Lens.PhysicalProperties.BladeCount += 1;
            m_CmCamera.Lens.PhysicalProperties.Curvature += Vector2.one;
            m_CmCamera.Lens.PhysicalProperties.BarrelClipping = 0.5f;
            m_CmCamera.Lens.PhysicalProperties.Anamorphism = 0.5f;

            // Check that modification is present in cmCamera's state and that modification was applied to Camera's lens
            yield return null;
            CompareLensProperties(m_CmCamera.Lens, m_CmCamera.State);
            CompareLensProperties(m_Cam, m_CmCamera.State);
        }

        static void CompareLensProperties(Camera camera, CameraState state)
        {
            Assert.That(camera.sensorSize, Is.EqualTo(state.Lens.PhysicalProperties.SensorSize));
            Assert.That(camera.gateFit, Is.EqualTo(state.Lens.PhysicalProperties.GateFit));
            Assert.That(camera.focalLength, 
                Is.EqualTo(Camera.FieldOfViewToFocalLength(state.Lens.FieldOfView, state.Lens.PhysicalProperties.SensorSize.y)));
            Assert.That(camera.lensShift, Is.EqualTo(state.Lens.PhysicalProperties.LensShift));
            Assert.That(camera.focusDistance, Is.EqualTo(state.Lens.PhysicalProperties.FocusDistance));
            Assert.That(camera.iso, Is.EqualTo(state.Lens.PhysicalProperties.Iso));
            Assert.That(camera.shutterSpeed, Is.EqualTo(state.Lens.PhysicalProperties.ShutterSpeed));
            Assert.That(camera.aperture, Is.EqualTo(state.Lens.PhysicalProperties.Aperture));
            Assert.That(camera.bladeCount, Is.EqualTo(state.Lens.PhysicalProperties.BladeCount));
            Assert.That(camera.curvature, Is.EqualTo(state.Lens.PhysicalProperties.Curvature));
            Assert.That(camera.barrelClipping, Is.EqualTo(state.Lens.PhysicalProperties.BarrelClipping));
            Assert.That(camera.anamorphism, Is.EqualTo(state.Lens.PhysicalProperties.Anamorphism));
        }
        
        static void CompareLensProperties(LensSettings lens, CameraState state)
        {
            Assert.That(lens.PhysicalProperties.SensorSize, Is.EqualTo(state.Lens.PhysicalProperties.SensorSize));
            Assert.That(lens.PhysicalProperties.GateFit, Is.EqualTo(state.Lens.PhysicalProperties.GateFit));
            Assert.That(Camera.FieldOfViewToFocalLength(lens.FieldOfView, lens.PhysicalProperties.SensorSize.y), 
                Is.EqualTo(Camera.FieldOfViewToFocalLength(state.Lens.FieldOfView, state.Lens.PhysicalProperties.SensorSize.y)));
            Assert.That(lens.PhysicalProperties.LensShift, Is.EqualTo(state.Lens.PhysicalProperties.LensShift));
            Assert.That(lens.PhysicalProperties.FocusDistance, Is.EqualTo(state.Lens.PhysicalProperties.FocusDistance));
            Assert.That(lens.PhysicalProperties.Iso, Is.EqualTo(state.Lens.PhysicalProperties.Iso));
            Assert.That(lens.PhysicalProperties.ShutterSpeed, Is.EqualTo(state.Lens.PhysicalProperties.ShutterSpeed));
            Assert.That(lens.PhysicalProperties.Aperture, Is.EqualTo(state.Lens.PhysicalProperties.Aperture));
            Assert.That(lens.PhysicalProperties.BladeCount, Is.EqualTo(state.Lens.PhysicalProperties.BladeCount));
            Assert.That(lens.PhysicalProperties.Curvature, Is.EqualTo(state.Lens.PhysicalProperties.Curvature));
            Assert.That(lens.PhysicalProperties.BarrelClipping, Is.EqualTo(state.Lens.PhysicalProperties.BarrelClipping));
            Assert.That(lens.PhysicalProperties.Anamorphism, Is.EqualTo(state.Lens.PhysicalProperties.Anamorphism));
        }
    }
}

#endif
