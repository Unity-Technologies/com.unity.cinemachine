using NUnit.Framework;
using UnityEngine;

namespace Unity.Cinemachine.Tests.Editor
{    
    [TestFixture]
    public class BlendManagerTests
    {
        class FakeCamera : ICinemachineCamera
        {
            readonly string m_Name;
            public FakeCamera(string name) => m_Name = name; 
            public string Name => m_Name;
            public string Description => string.Empty;
            public CameraState State => CameraState.Default;
            public bool IsValid => true;
            public ICinemachineMixer ParentCamera => null;
            public void UpdateCameraState(Vector3 worldUp, float deltaTime) {}
            public void OnCameraActivated(ICinemachineCamera.ActivationEventParams evt) {}
        }

        class FakeMixer : FakeCamera, ICinemachineMixer 
        { 
            public FakeMixer(string name) : base(name) {}
            public bool IsLiveChild(ICinemachineCamera child, bool dominantChildOnly) => false; 
        }

        BlendManager m_BlendManager = new();
        FakeMixer m_Mixer = new ("Mixer");
        FakeCamera m_Cam1 = new ("Cam1");
        FakeCamera m_Cam2 = new ("Cam2");

        int m_ActivatedEventCount;
        int m_DeactivatedEventCount;
        int m_BlendCreatedCount;
        int m_BlendFinishedCount;

        void ActivatedEventHandler(ICinemachineCamera.ActivationEventParams evt) => ++m_ActivatedEventCount;
        void DeactivateEventHandler(ICinemachineMixer mixer, ICinemachineCamera cam) => ++m_DeactivatedEventCount;
        void BlendCreatedEventHandler(CinemachineCore.BlendEventParams evt) => ++m_BlendCreatedCount;
        void BlendFinishedEventHandler(ICinemachineMixer mixer, ICinemachineCamera cam) => ++m_BlendFinishedCount;

        [SetUp] public void Setup() 
        { 
            CinemachineCore.CameraActivatedEvent.AddListener(ActivatedEventHandler);
            CinemachineCore.CameraDeactivatedEvent.AddListener(DeactivateEventHandler);
            CinemachineCore.BlendCreatedEvent.AddListener(BlendCreatedEventHandler);
            CinemachineCore.BlendFinishedEvent.AddListener(BlendFinishedEventHandler);
        }
        [TearDown] public void TearDown() 
        { 
            CinemachineCore.CameraActivatedEvent.RemoveListener(ActivatedEventHandler);
            CinemachineCore.CameraDeactivatedEvent.RemoveListener(DeactivateEventHandler);
            CinemachineCore.BlendCreatedEvent.RemoveListener(BlendCreatedEventHandler);
            CinemachineCore.BlendFinishedEvent.RemoveListener(BlendFinishedEventHandler);
        }

        void ResetCounters() => m_ActivatedEventCount = m_DeactivatedEventCount = m_BlendCreatedCount = m_BlendFinishedCount = 0;

        void ProcessFrame(ICinemachineCamera cam, float deltaTime)
        {
            m_BlendManager.UpdateRootFrame(m_Mixer, cam, Vector3.up, deltaTime);
            m_BlendManager.ComputeCurrentBlend();
            m_BlendManager.ProcessActiveCamera(m_Mixer, Vector3.up, deltaTime);
        }
            
        [Test]
        public void TestEvents()
        {
            m_BlendManager.LookupBlendDelegate = (outgoing, incoming)
                => new (CinemachineBlendDefinition.Styles.EaseInOut, 1); // constant blend time of 1

            ResetCounters();
            m_BlendManager.ResetRootFrame();

            // We should get an initial activation event, no blend
            ProcessFrame(m_Cam1, 0.1f);
            Assert.AreEqual(1, m_ActivatedEventCount);
            Assert.AreEqual(0, m_DeactivatedEventCount);
            Assert.AreEqual(0, m_BlendFinishedCount);
            Assert.AreEqual(0, m_BlendCreatedCount);
            Assert.That(m_BlendManager.IsBlending, Is.False);

            ProcessFrame(m_Cam1, 0.1f);
            Assert.AreEqual(1, m_ActivatedEventCount);
            Assert.AreEqual(0, m_DeactivatedEventCount);
            Assert.AreEqual(0, m_BlendCreatedCount);
            Assert.AreEqual(0, m_BlendFinishedCount);
            Assert.That(m_BlendManager.IsBlending, Is.False);

            // Activate new camera, make sure blending finishes after 1 sec
            ProcessFrame(m_Cam2, 0.1f);
            Assert.AreEqual(2, m_ActivatedEventCount);
            Assert.AreEqual(0, m_DeactivatedEventCount);
            Assert.AreEqual(1, m_BlendCreatedCount);
            Assert.AreEqual(0, m_BlendFinishedCount);
            Assert.That(m_BlendManager.IsBlending, Is.True);

            ProcessFrame(m_Cam2, 0.5f);
            Assert.AreEqual(2, m_ActivatedEventCount);
            Assert.AreEqual(0, m_DeactivatedEventCount);
            Assert.AreEqual(1, m_BlendCreatedCount);
            Assert.AreEqual(0, m_BlendFinishedCount);
            Assert.That(m_BlendManager.IsBlending, Is.True);

            ProcessFrame(m_Cam2, 0.5f);
            Assert.AreEqual(2, m_ActivatedEventCount);
            Assert.AreEqual(1, m_DeactivatedEventCount);
            Assert.AreEqual(1, m_BlendCreatedCount);
            Assert.AreEqual(1, m_BlendFinishedCount);
            Assert.That(m_BlendManager.IsBlending, Is.False);
        }
    }
}