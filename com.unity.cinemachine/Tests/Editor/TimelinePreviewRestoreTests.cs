#if CINEMACHINE_TIMELINE
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace Unity.Cinemachine.Tests.Editor
{
    /// <summary>
    /// Timeline's preview resets the brain's camera to its authored pose when the graph is rebuilt
    /// (e.g. when a clip is edited in the Inspector).  The brain must restore its output as soon as
    /// the rebuilt graph is evaluated, not on its next update, or the Game view flickers.
    /// </summary>
    [TestFixture]
    public class TimelinePreviewRestoreTests : CinemachineFixtureBase
    {
        static readonly Vector3 k_VcamPos = new (10, 2, -5);
        static readonly Vector3 k_AuthoredPos = new (-3, 7, 4);

        CinemachineBrain m_Brain;
        PlayableDirector m_Director;
        TimelineAsset m_Timeline;

        [SetUp]
        public override void SetUp()
        {
            base.SetUp();

            var brainGo = CreateGameObject("Brain", typeof(Camera), typeof(CinemachineBrain));
            m_Brain = brainGo.GetComponent<CinemachineBrain>();
            m_Brain.UpdateMethod = CinemachineBrain.UpdateMethods.ManualUpdate;
            brainGo.transform.position = k_AuthoredPos;

            var vcamGo = CreateGameObject("Vcam", typeof(CinemachineCamera));
            vcamGo.transform.position = k_VcamPos;
            var vcam = vcamGo.GetComponent<CinemachineCamera>();

            m_Timeline = ScriptableObject.CreateInstance<TimelineAsset>();
            var track = m_Timeline.CreateTrack<CinemachineTrack>();
            var clip = track.CreateDefaultClip();
            clip.start = 0;
            clip.duration = 10;
            var shot = (CinemachineShot)clip.asset;

            var directorGo = CreateGameObject("Director", typeof(PlayableDirector));
            m_Director = directorGo.GetComponent<PlayableDirector>();
            m_Director.playableAsset = m_Timeline;
            m_Director.SetGenericBinding(track, m_Brain);
            m_Director.SetReferenceValue(shot.VirtualCamera.exposedName, vcam);
            m_Director.time = 1;
        }

        [TearDown]
        public override void TearDown()
        {
            if (m_Timeline != null)
                Object.DestroyImmediate(m_Timeline);
            base.TearDown();
        }

        // Evaluates the timeline, then lets the brain update (as it would in LateUpdate)
        void EvaluateAndUpdateBrain()
        {
            m_Director.RebuildGraph();
            m_Director.Play();
            m_Director.Evaluate();
            m_Brain.ManualUpdate();
        }

        [Test]
        public void RebuiltGraph_RestoresBrainPose_WithoutWaitingForBrainUpdate()
        {
            EvaluateAndUpdateBrain();
            Assert.That((m_Brain.transform.position - k_VcamPos).magnitude, Is.LessThan(0.01f),
                "Sanity check: the brain should drive the camera to the vcam's pose");

            // Simulate Timeline reverting the bound object when the graph is rebuilt
            m_Brain.transform.position = k_AuthoredPos;

            // Rebuild and evaluate, but do NOT update the brain.  Anything that renders now
            // (a Game view repaint) must already see the restored pose.
            m_Director.RebuildGraph();
            m_Director.Play();
            m_Director.Evaluate();

            Assert.That((m_Brain.transform.position - k_VcamPos).magnitude, Is.LessThan(0.01f),
                "Evaluating the rebuilt timeline left the camera at the reverted pose");
        }

        [Test]
        public void DisabledBrain_IsNotOverwrittenByTimelinePreview()
        {
            EvaluateAndUpdateBrain();
            m_Brain.enabled = false;
            m_Brain.transform.position = k_AuthoredPos;

            m_Director.RebuildGraph();
            m_Director.Play();
            m_Director.Evaluate();

            Assert.That((m_Brain.transform.position - k_AuthoredPos).magnitude, Is.LessThan(0.01f),
                "A disabled brain's camera must be left alone");
        }

        [Test]
        public void ReenabledBrain_DoesNotRestoreStaleState()
        {
            EvaluateAndUpdateBrain();
            m_Brain.enabled = false;
            m_Brain.transform.position = k_AuthoredPos;
            m_Brain.enabled = true;

            m_Director.RebuildGraph();
            m_Director.Play();
            m_Director.Evaluate();

            Assert.That((m_Brain.transform.position - k_AuthoredPos).magnitude, Is.LessThan(0.01f),
                "State cached before a disable/enable cycle must not be re-applied");
        }
    }
}
#endif
