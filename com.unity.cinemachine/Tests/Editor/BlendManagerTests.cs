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
            public CameraState State 
            {
                get 
                {
                    var state = CameraState.Default;
                    state.RawPosition = Position;
                    return state;
                }
            }
            public bool IsValid => true;
            public ICinemachineMixer ParentCamera => null;
            public void UpdateCameraState(Vector3 worldUp, float deltaTime) {}
            public void OnCameraActivated(ICinemachineCamera.ActivationEventParams evt) {}

            public Vector3 Position;
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
        FakeCamera m_Cam3 = new ("Cam3");

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

        void Reset(float blendTime)
        {
            m_BlendManager.LookupBlendDelegate = (outgoing, incoming)
                => new (CinemachineBlendDefinition.Styles.Linear, blendTime); // linear blend, constant blend time
            m_BlendManager.OnEnable();
            ProcessFrame(null, 0.1f);
            ResetCounters();
        }

        void ProcessFrame(ICinemachineCamera activeCam, float deltaTime)
        {
            m_BlendManager.UpdateRootFrame(m_Mixer, activeCam, Vector3.up, deltaTime);
            m_BlendManager.ComputeCurrentBlend();
            m_BlendManager.ProcessActiveCamera(m_Mixer, Vector3.up, deltaTime);
        }

        [Test]
        public void TestEvents()
        {
            Reset(1); // constant blend time of 1

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

        [Test]
        public void TestEventsNestedBlend()
        {
            Reset(1); // constant blend time of 1

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

            // Activate new camera, blend will take 1 sec
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

            // Acivate new cam before old blend is finished
            ProcessFrame(m_Cam3, 0.1f);
            Assert.AreEqual(3, m_ActivatedEventCount);
            Assert.AreEqual(0, m_DeactivatedEventCount);
            Assert.AreEqual(2, m_BlendCreatedCount);
            Assert.AreEqual(0, m_BlendFinishedCount);
            Assert.That(m_BlendManager.IsBlending, Is.True);

            // After first blend time has elapsed, check the counters
            ProcessFrame(m_Cam3, 0.5f);
            Assert.AreEqual(3, m_ActivatedEventCount);
            Assert.AreEqual(1, m_DeactivatedEventCount);
            Assert.AreEqual(2, m_BlendCreatedCount);
            Assert.AreEqual(0, m_BlendFinishedCount); // blend was interrupted, never finished
            Assert.That(m_BlendManager.IsBlending, Is.True);

            // After second blend is finished, check the counters
            ProcessFrame(m_Cam3, 0.5f);
            Assert.AreEqual(3, m_ActivatedEventCount);
            Assert.AreEqual(2, m_DeactivatedEventCount);
            Assert.AreEqual(2, m_BlendCreatedCount);
            Assert.AreEqual(1, m_BlendFinishedCount);
            Assert.That(m_BlendManager.IsBlending, Is.False);
        }

        [Test]
        public void TestEventsBlendToNestedBlend()
        {
            var customBlend = new NestedBlendSource(new CinemachineBlend()
            {
                CamA = m_Cam1,
                CamB = m_Cam2,
                BlendCurve = AnimationCurve.Linear(0, 0, 1, 1),
                Duration = 1,
                TimeInBlend = 0.1f
            });

            Reset(1); // constant blend time of 1

            // We should get an initial activation event, no blend
            ProcessFrame(m_Cam1, 0.1f);
            Assert.AreEqual(1, m_ActivatedEventCount);
            Assert.AreEqual(0, m_DeactivatedEventCount);
            Assert.AreEqual(0, m_BlendFinishedCount);
            Assert.AreEqual(0, m_BlendCreatedCount);
            Assert.That(m_BlendManager.IsBlending, Is.False);

            // Activate nested blend camera, blend will take 1 sec
            ProcessFrame(customBlend, 0.1f);
            Assert.AreEqual(2, m_ActivatedEventCount);
            Assert.AreEqual(0, m_DeactivatedEventCount);
            Assert.AreEqual(1, m_BlendCreatedCount);
            Assert.AreEqual(0, m_BlendFinishedCount);
            Assert.That(m_BlendManager.IsBlending, Is.True);

            // change camera in the custom blend - we expect activation and deactivation events
            customBlend.Blend.CamB = m_Cam3;
            ProcessFrame(customBlend, 0.1f);
            Assert.AreEqual(3, m_ActivatedEventCount);
            Assert.AreEqual(1, m_DeactivatedEventCount);
            Assert.AreEqual(1, m_BlendCreatedCount);
            Assert.AreEqual(0, m_BlendFinishedCount);
            Assert.That(m_BlendManager.IsBlending, Is.True);

            customBlend.Blend.CamA = null;
            ProcessFrame(customBlend, 1);
            Assert.AreEqual(3, m_ActivatedEventCount);
            Assert.AreEqual(2, m_DeactivatedEventCount);
            Assert.AreEqual(1, m_BlendCreatedCount);
            Assert.AreEqual(1, m_BlendFinishedCount);
            Assert.That(m_BlendManager.IsBlending, Is.False);
        }

        [Test]
        public void TestBlendReversal()
        {
            Reset(1); // constant blend time of 1
            m_Cam1.Position = new Vector3(0, 0, 0);
            m_Cam2.Position = new Vector3(1, 0, 0);

            // Start with cam1
            ProcessFrame(m_Cam1, 0.1f);
            Assert.That(m_BlendManager.IsBlending, Is.False);

            // Activate cam2
            ProcessFrame(m_Cam2, 0.5f);
            Assert.That(m_BlendManager.IsBlending, Is.True);
            Assert.AreEqual(0.5f, m_BlendManager.CameraState.RawPosition.x, 0.001f);

            // Reverse the blend to cam1
            ProcessFrame(m_Cam1, 0.2f);
            Assert.That(m_BlendManager.IsBlending, Is.True);
            Assert.AreEqual(0.3f, m_BlendManager.CameraState.RawPosition.x, 0.001f);

            // Reverse the blend again to cam2
            ProcessFrame(m_Cam2, 0.1f);
            Assert.That(m_BlendManager.IsBlending, Is.True);
            Assert.AreEqual(0.4f, m_BlendManager.CameraState.RawPosition.x, 0.001f);

            ProcessFrame(m_Cam2, 0.4f);
            Assert.That(m_BlendManager.IsBlending, Is.True);
            Assert.AreEqual(0.8f, m_BlendManager.CameraState.RawPosition.x, 0.001f);

            // Reverse the blend again to cam1
            ProcessFrame(m_Cam1, 0.1f);
            Assert.That(m_BlendManager.IsBlending, Is.True);
            Assert.AreEqual(0.7f, m_BlendManager.CameraState.RawPosition.x, 0.001f);

            // And finish the blend on cam2
            ProcessFrame(m_Cam2, 0.1f);
            Assert.That(m_BlendManager.IsBlending, Is.True);
            Assert.AreEqual(0.8f, m_BlendManager.CameraState.RawPosition.x, 0.001f);

            ProcessFrame(m_Cam2, 0.201f);
            Assert.That(m_BlendManager.IsBlending, Is.False);
            Assert.AreEqual(1.0f, m_BlendManager.CameraState.RawPosition.x, 0.001f);
        }

        [Test]
        public void TestBlendCancellationKeepsOutgoingCameraUpdating()
        {
            void MoveCameras() {
                m_Cam1.Position += Vector3.forward;
                m_Cam2.Position += Vector3.forward;
            }

            Reset(1); // constant blend time of 1
            m_Cam1.Position = new Vector3(0, 0, 0);
            m_Cam2.Position = new Vector3(1, 0, 0);

            // Start with cam1, then blend towards cam2.
            ProcessFrame(m_Cam1, 0.1f);
            MoveCameras();
            ProcessFrame(m_Cam2, 0.5f);
            MoveCameras();
            Assume.That(m_BlendManager.IsBlending, Is.True);

            // Cancel/reverse the blend back to cam1 and advance a couple of frames.
            ProcessFrame(m_Cam1, 0.1f);
            MoveCameras();
            ProcessFrame(m_Cam1, 0.1f);
            Assume.That(m_BlendManager.IsBlending, Is.True);

            // Z coordinate should be the same for both cameras and blend result
            Assert.AreEqual(m_Cam1.Position.z, m_BlendManager.CameraState.RawPosition.z, 0.001f);
        }
    }
}