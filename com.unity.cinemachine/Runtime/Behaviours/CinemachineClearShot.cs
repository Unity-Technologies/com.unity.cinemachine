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

        /// <summary>The blend which is used if you don't explicitly define a blend between two Virtual Cameras</summary>
        [Tooltip("The blend which is used if you don't explicitly define a blend between two Virtual Cameras")]
        [FormerlySerializedAs("m_DefaultBlend")]
        public CinemachineBlendDefinition DefaultBlend = new(CinemachineBlendDefinition.Styles.Cut, 0);

        /// <summary>This is the asset which contains custom settings for specific blends</summary>
        [HideInInspector]
        [FormerlySerializedAs("m_CustomBlends")]
        public CinemachineBlenderSettings CustomBlends = null;

        [SerializeField, HideInInspector, FormerlySerializedAs("m_LookAt")] Transform m_LegacyLookAt;
        [SerializeField, HideInInspector, FormerlySerializedAs("m_Follow")] Transform m_LegacyFollow;

        CinemachineVirtualCameraBase m_LiveChild;
        CinemachineBlend m_ActiveBlend;
        CameraState m_State = CameraState.Default;
        float m_ActivationTime = 0;
        float m_PendingActivationTime = 0;
        CinemachineVirtualCameraBase m_PendingCamera;
        bool m_RandomizeNow = false;
        List<CinemachineVirtualCameraBase> m_RandomizedChildren = null;
        ICinemachineCamera m_TransitioningFrom;

        /// <summary>Reset the component to default values.</summary>
        protected override void Reset()
        {
            base.Reset();
            ActivateAfter = 0;
            MinDuration = 0;
            RandomizeChoice = false;
            DefaultBlend = new (CinemachineBlendDefinition.Styles.EaseInOut, 0.5f);
            CustomBlends = null;
        }

        protected internal override void LegacyUpgradeMayBeCalledFromThread(int streamedVersion)
        {
            base.LegacyUpgradeMayBeCalledFromThread(streamedVersion);
            if (streamedVersion < 20220721)
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

        /// <summary>Get the current "best" child virtual camera, that would be chosen
        /// if the State Driven Camera were active.</summary>
        public override CinemachineVirtualCameraBase LiveChild => PreviousStateIsValid ? m_LiveChild : null;

        /// <summary>
        /// Get the current active blend in progress.  Will return null if no blend is in progress.
        /// </summary>
        public override CinemachineBlend ActiveBlend => PreviousStateIsValid ? m_ActiveBlend : null;

        /// <summary>The State of the current live child</summary>
        public override CameraState State => m_State;

        /// <summary>Notification that this virtual camera is going live.
        /// This implementation resets the child randomization.</summary>
        /// <param name="fromCam">The camera being deactivated.  May be null.</param>
        /// <param name="worldUp">Default world Up, set by the CinemachineBrain</param>
        /// <param name="deltaTime">Delta time for time-based effects (ignore if less than or equal to 0)</param>
        public override void OnTransitionFromCamera(
            ICinemachineCamera fromCam, Vector3 worldUp, float deltaTime)
        {
            base.OnTransitionFromCamera(fromCam, worldUp, deltaTime);
            m_TransitioningFrom = fromCam;
            if (RandomizeChoice && m_ActiveBlend == null)
            {
                m_RandomizedChildren = null;
                m_LiveChild = null;
            }
            InternalUpdateCameraState(worldUp, deltaTime);
        }

        /// <summary>Internal use only.  Called by CinemachineCore at designated update time
        /// so the vcam can position itself and track its targets.  This implementation
        /// updates all the children, chooses the best one, and implements any required blending.</summary>
        /// <param name="worldUp">Default world Up, set by the CinemachineBrain</param>
        /// <param name="deltaTime">Delta time for time-based effects (ignore if less than 0)</param>
        public override void InternalUpdateCameraState(Vector3 worldUp, float deltaTime)
        {
            UpdateCameraCache();
            if (!PreviousStateIsValid)
            {
                m_LiveChild = null;
                m_ActiveBlend = null;
                m_ActivationTime = 0;
                m_PendingActivationTime = 0;
                m_PendingCamera = null;
                m_RandomizedChildren = null;
            }

            // Choose the best camera
            var previousCam = LiveChild;
            m_LiveChild = ChooseCurrentCamera();

            // Are we transitioning cameras?
            if (previousCam != LiveChild && LiveChild != null)
            {
                // Notify incoming camera of transition
                LiveChild.OnTransitionFromCamera(previousCam, worldUp, deltaTime);

                // Generate Camera Activation event in the brain if live
                CinemachineCore.Instance.GenerateCameraActivationEvent(LiveChild, previousCam);

                // Are we transitioning cameras?
                if (previousCam != null)
                {
                    // Create a blend (will be null if a cut)
                    m_ActiveBlend = CreateActiveBlend(previousCam, LiveChild, LookupBlend(previousCam, LiveChild));

                    // If cutting, generate a camera cut event if live
                    if (m_ActiveBlend == null || !m_ActiveBlend.Uses(previousCam))
                        CinemachineCore.Instance.GenerateCameraCutEvent(LiveChild);
                }
            }

            // Advance the current blend (if any)
            if (m_ActiveBlend != null)
            {
                m_ActiveBlend.TimeInBlend += (deltaTime >= 0)
                    ? deltaTime : m_ActiveBlend.Duration;
                if (m_ActiveBlend.IsComplete)
                    m_ActiveBlend = null;
            }

            if (m_ActiveBlend != null)
            {
                m_ActiveBlend.UpdateCameraState(worldUp, deltaTime);
                m_State =  m_ActiveBlend.State;
            }
            else if (LiveChild != null)
            {
                if (m_TransitioningFrom != null)
                    LiveChild.OnTransitionFromCamera(m_TransitioningFrom, worldUp, deltaTime);
                m_State =  LiveChild.State;
            }
            m_TransitioningFrom = null;
            InvokePostPipelineStageCallback(this, CinemachineCore.Stage.Finalize, ref m_State, deltaTime);
            PreviousStateIsValid = true;
        }

        /// <summary>If RandomizeChoice is enabled, call this to re-randomize the children next frame.
        /// This is useful if you want to freshen up the shot.</summary>
        public void ResetRandomization()
        {
            m_RandomizedChildren = null;
            m_RandomizeNow = true;
        }

        CinemachineVirtualCameraBase ChooseCurrentCamera()
        {
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
                if (m_RandomizedChildren == null)
                    m_RandomizedChildren = Randomize(allCameras);
                allCameras = m_RandomizedChildren;
            }

            if (m_LiveChild != null && !m_LiveChild.gameObject.activeSelf)
                m_LiveChild = null;
            var best = m_LiveChild;
            for (var i = 0; i < allCameras.Count; ++i)
            {
                var vcam = allCameras[i];
                if (vcam != null && vcam.gameObject.activeInHierarchy)
                {
                    // Choose the first in the list that is better than the current
                    if (best == null
                        || vcam.State.ShotQuality > best.State.ShotQuality
                        || (vcam.State.ShotQuality == best.State.ShotQuality && vcam.Priority.Value > best.Priority.Value)
                        || (RandomizeChoice && m_RandomizeNow && vcam != m_LiveChild
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
                if (m_LiveChild == best)
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
                        return m_LiveChild;
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
                    return m_LiveChild;
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

        CinemachineBlendDefinition LookupBlend(
            ICinemachineCamera fromKey, ICinemachineCamera toKey)
        {
            // Get the blend curve that's most appropriate for these cameras
            var blend = DefaultBlend;
            if (CustomBlends != null)
            {
                string fromCameraName = (fromKey != null) ? fromKey.Name : string.Empty;
                string toCameraName = (toKey != null) ? toKey.Name : string.Empty;
                blend = CustomBlends.GetBlendForVirtualCameras(
                        fromCameraName, toCameraName, blend);
            }
            if (CinemachineCore.GetBlendOverride != null)
                blend = CinemachineCore.GetBlendOverride(fromKey, toKey, blend, this);
            return blend;
        }
    }
}
