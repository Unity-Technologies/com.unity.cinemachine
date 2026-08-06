using NUnit.Framework;
using UnityEngine;

namespace Unity.Cinemachine.Tests
{
    // This module registers listeners at AfterSceneLoad (default).
    // Tests verify that CinemachineCore.ResetStaticsOnLoad (SubsystemRegistration)
    // runs BEFORE this, so user-registered listeners aren't cleared.
    static class RuntimeEventRegistration
    {
        public static bool dummyCameraActivatedCallbackWasCalled = false;
        public static bool dummyCameraDeactivatedCallbackWasCalled = false;
        public static bool dummyCameraUpdatedCallbackWasCalled = false;

        static void DummyCameraActivatedCallback(ICinemachineCamera.ActivationEventParams _)
        {
            dummyCameraActivatedCallbackWasCalled = true;
        }

        static void DummyCameraDeactivatedCallback(ICinemachineMixer _, ICinemachineCamera __)
        {
            dummyCameraDeactivatedCallbackWasCalled = true;
        }

        static void DummyCameraUpdatedCallback(CinemachineBrain _)
        {
            dummyCameraUpdatedCallbackWasCalled = true;
        }

        [RuntimeInitializeOnLoadMethod]
        static void InitializeModule()
        {
            CinemachineCore.CameraActivatedEvent.RemoveListener(DummyCameraActivatedCallback);
            CinemachineCore.CameraActivatedEvent.AddListener(DummyCameraActivatedCallback);

            CinemachineCore.CameraDeactivatedEvent.RemoveListener(DummyCameraDeactivatedCallback);
            CinemachineCore.CameraDeactivatedEvent.AddListener(DummyCameraDeactivatedCallback);

            CinemachineCore.CameraUpdatedEvent.RemoveListener(DummyCameraUpdatedCallback);
            CinemachineCore.CameraUpdatedEvent.AddListener(DummyCameraUpdatedCallback);
        }
    }

    [TestFixture]
    public class CinemachineEventRegistrationTests : CinemachineRuntimeFixtureBase
    {
        [SetUp]
        public override void SetUp()
        {
            base.SetUp();
            
            RuntimeEventRegistration.dummyCameraActivatedCallbackWasCalled = false;
            RuntimeEventRegistration.dummyCameraDeactivatedCallbackWasCalled = false;
            RuntimeEventRegistration.dummyCameraUpdatedCallbackWasCalled = false;
        }

        [TearDown]
        public override void TearDown()
        {
            base.TearDown();
        }

        [Test]
        public void CameraActivatedEvent_IsResetBeforeSceneLoad()
        {
            CinemachineCore.CameraActivatedEvent.Invoke(default);
            Assert.That(RuntimeEventRegistration.dummyCameraActivatedCallbackWasCalled, Is.True);
        }

        [Test]
        public void CameraDeactivatedEvent_IsResetBeforeSceneLoad()
        {
            CinemachineCore.CameraDeactivatedEvent.Invoke(null, null);
            Assert.That(RuntimeEventRegistration.dummyCameraDeactivatedCallbackWasCalled, Is.True);
        }

        [Test]
        public void CameraUpdatedEvent_IsResetBeforeSceneLoad()
        {
            CinemachineCore.CameraUpdatedEvent.Invoke(m_Brain);
            Assert.That(RuntimeEventRegistration.dummyCameraUpdatedCallbackWasCalled, Is.True);
        }
    }
}
