using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

namespace Unity.Cinemachine
{
    /// <summary>
    /// Cinemachine ClearShot is a "manager camera" that owns and manages a set of
    /// Virtual Camera gameObject children.  When Live, the ClearShot will check the
    /// children, and choose the one with the best quality shot and make it Live.
    ///
    /// This can be a very powerful tool.  If the child cameras have shot evaluator
    /// extensions, they will analyze the scene for target obstructions, optimal target
    /// distance, and other items, and report their assessment of shot quality back to
    /// the ClearShot parent, who will then choose the best one.  You can use this to set
    /// up complex multi-camera coverage of a scene, and be assured that a clear shot of
    /// the target will always be available.
    ///
    /// If multiple child cameras have the same shot quality, the one with the highest
    /// priority will be chosen.
    ///
    /// You can also define custom blends between the ClearShot children.
    /// </summary>
    [DisallowMultipleComponent]
    [ExecuteAlways]
    [ExcludeFromPreset]
    [SaveDuringPlay]
    [AddComponentMenu("Cinemachine/Cinemachine ClearShot")]
    [HelpURL(Documentation.BaseURL + "manual/CinemachineClearShot.html")]
    public class CinemachineClearShot : CinemachineCameraManagerBase
    {
        /// <summary>Wait this many seconds before activating a new child camera</summary>
        [Tooltip("Wait this many seconds before activating a new child camera")]
        [FormerlySerializedAs("m_ActivateAfter")]
        public float ActivateAfter;

        /// <summary>An active camera must be active for at least this many seconds</summary>
        [Tooltip("An active camera must be active for at least this many seconds")]
        [FormerlySerializedAs("m_MinDuration")]
        public float MinDuration;

        /// <summary>If checked, camera choice will be randomized if multiple cameras are equally desirable.  
        /// Otherwise, child list order will be used</summary>
        [Tooltip("If checked, camera choice will be randomized if multiple cameras are equally desirable.  "
           + "Otherwise, child list order and child camera priority will be used.")]
        [FormerlySerializedAs("m_RandomizeChoice")]
        public bool RandomizeChoice = false;

        [SerializeField, HideInInspector, NoSaveDuringPlay, FormerlySerializedAs("m_LookAt")] Transform m_LegacyLookAt;
        [SerializeField, HideInInspector, NoSaveDuringPlay, FormerlySerializedAs("m_Follow")] Transform m_LegacyFollow;

        float m_ActivationTime = 0;
        float m_PendingActivationTime = 0;
        CinemachineVirtualCameraBase m_PendingCamera;
        bool m_RandomizeNow = false;
        List<CinemachineVirtualCameraBase> m_RandomizedChildren = null;

        /// <inheritdoc />
        protected override void Reset()
        {
            base.Reset();
            ActivateAfter = 0;
            MinDuration = 0;
            RandomizeChoice = false;
            DefaultBlend = new (CinemachineBlendDefinition.Styles.EaseInOut, 0.5f);
            CustomBlends = null;
        }

        /// <inheritdoc/>
        protected internal override void PerformLegacyUpgrade(int streamedVersion)
        {
            base.PerformLegacyUpgrade(streamedVersion);
            if (streamedVersion < 20220721)
            {
                if (m_LegacyLookAt != null || m_LegacyFollow != null)
                {
                    DefaultTarget = new DefaultTargetSettings 
                    { 
                        Enabled = true,
                        Target = new CameraTarget
                        {
                            LookAtTarget = m_LegacyLookAt, 
                            TrackingTarget = m_LegacyFollow, 
                            CustomLookAtTarget = m_LegacyLookAt != m_LegacyFollow 
                        }
                    };
                    m_LegacyLookAt = m_LegacyFollow = null;
                }
            }
        }

        /// <inheritdoc />
        public override void OnTransitionFromCamera(
            ICinemachineCamera fromCam, Vector3 worldUp, float deltaTime)
        {
            if (RandomizeChoice && !IsBlending)
                m_RandomizedChildren = null;
            base.OnTransitionFromCamera(fromCam, worldUp, deltaTime);
        }

        /// <summary>If RandomizeChoice is enabled, call this to re-randomize the children next frame.
        /// This is useful if you want to freshen up the shot.</summary>
        public void ResetRandomization()
        {
            m_RandomizedChildren = null;
            m_RandomizeNow = true;
        }

        /// <inheritdoc />
        protected override CinemachineVirtualCameraBase ChooseCurrentCamera(Vector3 worldUp, float deltaTime)
        {
            if (!PreviousStateIsValid)
            {
                m_ActivationTime = 0;
                m_PendingActivationTime = 0;
                m_PendingCamera = null;
                m_RandomizedChildren = null;
            }

            var liveChild = LiveChild as CinemachineVirtualCameraBase;
            if (ChildCameras == null || ChildCameras.Count == 0)
            {
                m_ActivationTime = 0;
                return null;
            }

            var allCameras = ChildCameras;
            if (!RandomizeChoice)
                m_RandomizedChildren = null;
            else if (allCameras.Count> 1)
            {
                m_RandomizedChildren ??= Randomize(allCameras);
                allCameras = m_RandomizedChildren;
            }

            if (liveChild != null && (!liveChild.IsValid || !liveChild.gameObject.activeSelf))
                liveChild = null;

            var best = liveChild;
            for (var i = 0; i < allCameras.Count; ++i)
            {
                var vcam = allCameras[i];
                if (vcam != null && vcam.gameObject.activeInHierarchy)
                {
                    // Choose the first in the list that is better than the current
                    if (best == null
                        || vcam.State.ShotQuality > best.State.ShotQuality
                        || (vcam.State.ShotQuality == best.State.ShotQuality && vcam.Priority.Value > best.Priority.Value)
                        || (RandomizeChoice && m_RandomizeNow && vcam != liveChild
                            && vcam.State.ShotQuality == best.State.ShotQuality
                            && vcam.Priority.Value == best.Priority.Value))
                    {
                        best = vcam;
                    }
                }
            }
            m_RandomizeNow = false;

            float now = CinemachineCore.CurrentTime;
            if (m_ActivationTime != 0)
            {
                // Is it active now?
                if (liveChild == best)
                {
                    // Yes, cancel any pending
                    m_PendingActivationTime = 0;
                    m_PendingCamera = null;
                    return best;
                }

                // Is it pending?
                if (PreviousStateIsValid)
                {
                    if (m_PendingActivationTime != 0 && m_PendingCamera == best)
                    {
                        // Has it been pending long enough, and are we allowed to switch away
                        // from the active action?
                        if ((now - m_PendingActivationTime) > ActivateAfter
                            && (now - m_ActivationTime) > MinDuration)
                        {
                            // Yes, activate it now
                            m_RandomizedChildren = null; // reshuffle the children
                            m_ActivationTime = now;
                            m_PendingActivationTime = 0;
                            m_PendingCamera = null;
                            return best;
                        }
                        return liveChild;
                    }
                }
            }
            // Neither active nor pending.
            m_PendingActivationTime = 0; // cancel the pending, if any
            m_PendingCamera = null;

            // Can we activate it now?
            if (PreviousStateIsValid && m_ActivationTime > 0)
            {
                if (ActivateAfter > 0
                    || (now - m_ActivationTime) < MinDuration)
                {
                    // Too early - make it pending
                    m_PendingCamera = best;
                    m_PendingActivationTime = now;
                    return liveChild;
                }
            }
            // Activate now
            m_RandomizedChildren = null; // reshuffle the children
            m_ActivationTime = now;
            return best;
        }

        struct Pair { public int a; public float b; }

        static List<CinemachineVirtualCameraBase> Randomize(List<CinemachineVirtualCameraBase> src)
        {
            var pairs = new List<Pair>();
            for (var i = 0; i < src.Count; ++i)
            {
                var p = new Pair { a = i, b = Random.Range(0, 1000f) };
                pairs.Add(p);
            }
            pairs.Sort((p1, p2) => (int)p1.b - (int)p2.b);
            var dst = new List<CinemachineVirtualCameraBase>(src.Count);
            for (var i = 0; i < src.Count; ++i)
                dst.Add(src[pairs[i].a]);
            return dst;
        }
    }
}
