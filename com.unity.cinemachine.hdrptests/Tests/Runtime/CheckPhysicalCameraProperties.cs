using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Unity.Cinemachine.Tests.HDRP
{
    [TestFixture]
    public class CheckPhysicalCameraProperties : CinemachineHDRPFixtureBase
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

            // Check that modification is present in cmCamera's state and that modification was applied to Camera's lens
            yield return null;
            CompareLensProperties(m_CmCamera.Lens, m_CmCamera.State);
            CompareLensProperties(m_Cam, m_CmCamera.State);
        }

        static void CompareLensProperties(Camera camera, CameraState state)
        {
            Assert.That(camera.sensorSize, Is.EqualTo(state.Lens.SensorSize));
            Assert.That(camera.gateFit, Is.EqualTo(state.Lens.GateFit));
            Assert.That(camera.focalLength, 
                Is.EqualTo(Camera.FieldOfViewToFocalLength(state.Lens.FieldOfView, state.Lens.SensorSize.y)));
            Assert.That(camera.lensShift, Is.EqualTo(state.Lens.LensShift));
            Assert.That(camera.focusDistance, Is.EqualTo(state.Lens.FocusDistance));
            Assert.That(camera.iso, Is.EqualTo(state.Lens.Iso));
            Assert.That(camera.shutterSpeed, Is.EqualTo(state.Lens.ShutterSpeed));
            Assert.That(camera.aperture, Is.EqualTo(state.Lens.Aperture));
            Assert.That(camera.bladeCount, Is.EqualTo(state.Lens.BladeCount));
            Assert.That(camera.curvature, Is.EqualTo(state.Lens.Curvature));
            Assert.That(camera.barrelClipping, Is.EqualTo(state.Lens.BarrelClipping));
            Assert.That(camera.anamorphism, Is.EqualTo(state.Lens.Anamorphism));
        }
        
        static void CompareLensProperties(LensSettings lens, CameraState state)
        {
            Assert.That(lens.SensorSize, Is.EqualTo(state.Lens.SensorSize));
            Assert.That(lens.GateFit, Is.EqualTo(state.Lens.GateFit));
            Assert.That(Camera.FieldOfViewToFocalLength(lens.FieldOfView, lens.SensorSize.y), 
                Is.EqualTo(Camera.FieldOfViewToFocalLength(state.Lens.FieldOfView, state.Lens.SensorSize.y)));
            Assert.That(lens.LensShift, Is.EqualTo(state.Lens.LensShift));
            Assert.That(lens.FocusDistance, Is.EqualTo(state.Lens.FocusDistance));
            Assert.That(lens.Iso, Is.EqualTo(state.Lens.Iso));
            Assert.That(lens.ShutterSpeed, Is.EqualTo(state.Lens.ShutterSpeed));
            Assert.That(lens.Aperture, Is.EqualTo(state.Lens.Aperture));
            Assert.That(lens.BladeCount, Is.EqualTo(state.Lens.BladeCount));
            Assert.That(lens.Curvature, Is.EqualTo(state.Lens.Curvature));
            Assert.That(lens.BarrelClipping, Is.EqualTo(state.Lens.BarrelClipping));
            Assert.That(lens.Anamorphism, Is.EqualTo(state.Lens.Anamorphism));
        }
    }
}
