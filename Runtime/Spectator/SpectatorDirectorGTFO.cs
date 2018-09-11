using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cinemachine;
using Cinemachine.Utility;

namespace Spectator
{
    [ExecuteInEditMode, DisallowMultipleComponent]
    [AddComponentMenu("Cinemachine/SpectatorDirectorGTFO")]
    public class SpectatorDirectorGTFO : CinemachineVirtualCameraBase 
    {
        /// <summary>When enabled, the current camera and blend will be indicated in the game window, for debugging</summary>
        [Tooltip("When enabled, the current child camera and blend will be indicated in the game window, for debugging")]
        [NoSaveDuringPlay]
        public bool m_ShowDebugText = false;

        public float m_MinimumShotQuality = 0.2f;

        /// <summary>Internal API for the editor.  Do not use this filed.</summary>
        [SerializeField, HideInInspector, NoSaveDuringPlay]
        internal CinemachineVirtualCameraBase[] m_ChildCameras = null;

        /// <summary>Wait this many seconds before activating a new child camera</summary>
        [Tooltip("Wait this many seconds before activating a new child camera")]
        public float m_ActivateAfter;

        /// <summary>An active camera must be active for at least this many seconds</summary>
        [Tooltip("An active camera must be active for at least this many seconds")]
        public float m_MinDuration;

        /// <summary>The blend which is used if you don't explicitly define a blend between two Virtual Cameras</summary>
        [CinemachineBlendDefinitionProperty]
        [Tooltip("The blend which is used if you don't explicitly define a blend between two Virtual Cameras")]
        public CinemachineBlendDefinition m_DefaultBlend
            = new CinemachineBlendDefinition(CinemachineBlendDefinition.Style.Cut, 0);

        /// <summary>This is the asset which contains custom settings for specific blends</summary>
        [HideInInspector]
        public CinemachineBlenderSettings m_CustomBlends = null;

        /// <summary>Gets a brief debug description of this virtual camera, for use when displayiong debug info</summary>
        public override string Description 
        { 
            get 
            { 
                // Show the active camera and blend
                if (mActiveBlend != null) 
                    return mActiveBlend.Description;

                ICinemachineCamera vcam = LiveChild;
                if (vcam == null) 
                    return "(none)";
                var sb = CinemachineDebug.SBFromPool();
                sb.Append("["); sb.Append(vcam.Name); 
                StoryManagerGTFO.CameraPoint cp = LiveCameraPoint;
                if (cp != null)
                    sb.Append(" cp=" + cp.m_TargetGroup.name + ", q=" + cp.m_shotQuality); 
                sb.Append("]");
                string text = sb.ToString(); 
                CinemachineDebug.ReturnToPool(sb); 
                return text;
            }
        }
        
        /// <summary>Get the current "best" child virtual camera, that would be chosen
        /// if the ClearShot camera were active.</summary>
        public ICinemachineCamera LiveChild { set; get; }

        /// <summary>The CameraState of the currently live child</summary>
        public override CameraState State { get { return m_State; } }

        /// <summary>Check whether the vcam a live child of this camera.</summary>
        /// <param name="vcam">The Virtual Camera to check</param>
        /// <returns>True if the vcam is currently actively influencing the state of this vcam</returns>
        public override bool IsLiveChild(ICinemachineCamera vcam) 
        { 
            return vcam == LiveChild 
                || (mActiveBlend != null && (vcam == mActiveBlend.CamA || vcam == mActiveBlend.CamB));
        }

        /// <summary>Used Internally Only</summary>
        override public Transform LookAt { get; set; }

        /// <summary>Used Internally Only</summary>
        override public Transform Follow 
        { 
            get
            {
                if (mFakeFollowTarget == null)
                {
                    mFakeFollowTarget = new GameObject("SpectatorFakeFollowTarget").transform;
                    mFakeFollowTarget.gameObject.hideFlags = HideFlags.DontSave;
                }
                return mFakeFollowTarget;
            }
            set {} // do nothing
        }
        private Transform mFakeFollowTarget;

        /// <summary>Internal use only.  Called by CinemachineCore at designated update time
        /// so the vcam can position itself and track its targets.  This implementation
        /// updates all the children, chooses the best one, and implements any required blending.</summary>
        /// <param name="worldUp">Default world Up, set by the CinemachineBrain</param>
        /// <param name="deltaTime">Delta time for time-based effects (ignore if less than 0)</param>
        public override void InternalUpdateCameraState(Vector3 worldUp, float deltaTime)
        {
            if (!PreviousStateIsValid)
                deltaTime = -1;

            // Choose the best camera
            UpdateListOfChildren();
            ICinemachineCamera previousCam = LiveChild;
            LiveChild = ChooseCurrentCamera(worldUp, deltaTime);

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
                    mActiveBlend = CreateBlend(
                            previousCam, LiveChild,
                            LookupBlend(previousCam, LiveChild), mActiveBlend);

                    // If cutting, generate a camera cut event if live
                    if (mActiveBlend == null)
                        CinemachineCore.Instance.GenerateCameraCutEvent(LiveChild);
                }
            }

            // Advance the current blend (if any)
            if (mActiveBlend != null)
            {
                mActiveBlend.TimeInBlend += (deltaTime >= 0)
                    ? deltaTime : mActiveBlend.Duration;
                if (mActiveBlend.IsComplete)
                    mActiveBlend = null;
            }

            if (mActiveBlend != null)
            {
                mActiveBlend.UpdateCameraState(worldUp, deltaTime);
                m_State =  mActiveBlend.State;
            }
            else if (LiveChild != null)
            {
                if (TransitioningFrom != null)
                    LiveChild.OnTransitionFromCamera(TransitioningFrom, worldUp, deltaTime);
                m_State =  LiveChild.State;
            }
            TransitioningFrom = null;
            InvokePostPipelineStageCallback(this, CinemachineCore.Stage.Finalize, ref m_State, deltaTime);
            PreviousStateIsValid = true;
        }

        /// <summary>Makes sure the internal child cache is up to date</summary>
        protected override void OnEnable()
        {
            base.OnEnable();
            InvalidateListOfChildren();
            mActiveBlend = null;
            CinemachineDebug.OnGUIHandlers -= OnGuiHandler;
            CinemachineDebug.OnGUIHandlers += OnGuiHandler;
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            CinemachineDebug.OnGUIHandlers -= OnGuiHandler;
            if (mFakeFollowTarget != null)
                RuntimeUtility.DestroyObject(mFakeFollowTarget);
            mFakeFollowTarget = null;
        }

        /// <summary>Makes sure the internal child cache is up to date</summary>
        public void OnTransformChildrenChanged()
        {
            InvalidateListOfChildren();
        }

        ///  Will only be called if Unity Editor - never in build
        private void OnGuiHandler()
        {
            if (!m_ShowDebugText)
                CinemachineDebug.ReleaseScreenPos(this);
            else
            {
                var sb = CinemachineDebug.SBFromPool();
                sb.Append(Name); sb.Append(": "); sb.Append(Description);
                string text = sb.ToString(); 
                Rect r = CinemachineDebug.GetScreenPos(this, text, GUI.skin.box);
                GUI.Label(r, text, GUI.skin.box);
                CinemachineDebug.ReturnToPool(sb);
            }
        }

        /// <summary>Is there a blend in progress?</summary>
        public bool IsBlending { get { return mActiveBlend != null; } }

        CameraState m_State = CameraState.Default;

        /// <summary>The list of child cameras.  These are just the immediate children in the hierarchy.</summary>
        public CinemachineVirtualCameraBase[] ChildCameras
        { 
            get { UpdateListOfChildren(); return m_ChildCameras; }
        }

        private CinemachineBlend mActiveBlend = null;

        void InvalidateListOfChildren()
        { 
            m_ChildCameras = null; 
            LiveChild = null; 
        }
       
        void UpdateListOfChildren()
        {
            if (m_ChildCameras != null)
                return;

            List<CinemachineVirtualCameraBase> list = new List<CinemachineVirtualCameraBase>();
            CinemachineVirtualCameraBase[] kids = GetComponentsInChildren<CinemachineVirtualCameraBase>(true);
            foreach (CinemachineVirtualCameraBase k in kids)
                if (k.transform.parent == transform)
                    list.Add(k);
            m_ChildCameras = list.ToArray();

            // Zap the cached current instructions
            mActivationTime = mPendingActivationTime = 0;
            mPendingCameraPoint = null;
            LiveChild = null;
            mActiveBlend = null;
        }

        private CinemachineVirtualCameraBase ChooseCurrentCamera(Vector3 worldUp, float deltaTime)
        {
            if (m_ChildCameras == null || m_ChildCameras.Length == 0)
            {
                mActivationTime = 0;
                return null;
            }

            if (LiveChild != null && !LiveChild.VirtualCameraGameObject.activeSelf)
                LiveChild = null;
            CinemachineVirtualCameraBase best = LiveChild as CinemachineVirtualCameraBase;

            // Choose current target group
            StoryManagerGTFO.CameraPoint cp = ChooseCurrentCameraPoint(deltaTime);
            if (cp != null)
            {
                // Choose the first in the list that matches the desired camera type
                for (int i = 0; i < m_ChildCameras.Length; ++i)
                {
                    CinemachineVirtualCameraBase vcam = m_ChildCameras[i];
                    if (vcam != null && vcam.gameObject.activeInHierarchy)
                    {
                        if (best == null)
                            best = vcam;

                        // GML Note: we're abusing Priority to hold the type
                        int camType = Mathf.RoundToInt(vcam.Priority);
                        if (camType == cp.m_cameraType)
                        {
                            best = vcam;
                            break;
                        }
                    }
                }

                // Set the vcam's target to the target group
                if (best != null)
                {
                    bool isCut = LiveCameraPoint != cp || best != LiveChild as CinemachineVirtualCameraBase;
                    LiveCameraPoint = cp;
                    Follow.position = cp.m_cameraPos;
                    if (isCut)
                    {
                        best.LookAt = cp.m_TargetGroup.transform;
                        best.Follow = Follow;
                        best.PreviousStateIsValid = false;

                        // GML debug
                        if (Time.time - mLastCut < m_MinDuration)
                            Debug.Log("Illegal Cut to " + cp.m_TargetGroup.name + " after " + (Time.time - mLastCut));
                        mLastCut = Time.time;
                        CinemachineCore.Instance.GenerateCameraCutEvent(best);
                    }
                }
            }
            return best;
        }

        float mLastCut = 0;
        float mActivationTime = 0;
        float mPendingActivationTime = 0;
        StoryManagerGTFO.CameraPoint mPendingCameraPoint;
        
        StoryManagerGTFO.CameraPoint LiveCameraPoint { get; set; }

        StoryManagerGTFO.CameraPoint ChooseCurrentCameraPoint(float deltaTime)
        {
            StoryManagerGTFO.CameraPoint liveCP = LiveCameraPoint;
            StoryManagerGTFO.CameraPoint best = ChooseBestCameraPoint();
            float now = Time.time;
            if (mActivationTime != 0)
            {
                // Is it active now?
                if (liveCP == best)
                {
                    // Yes, cancel any pending
                    mPendingActivationTime = 0;
                    mPendingCameraPoint = null;
                    return best;
                }

                // Is it pending?
                if (deltaTime >= 0)
                {
                    if (mPendingActivationTime != 0 && mPendingCameraPoint == best)
                    {
                        // Has it been pending long enough, and are we allowed to switch away
                        // from the active action?
                        if ((now - mPendingActivationTime) > m_ActivateAfter
                            && (now - mActivationTime) > m_MinDuration)
                        {
                            // Yes, activate it now
                            mActivationTime = now;
                            mPendingActivationTime = 0;
                            mPendingCameraPoint = null;
                            return best;
                        }
                        return liveCP;
                    }
                }
            }
            // Neither active nor pending.
            mPendingActivationTime = 0; // cancel the pending, if any
            mPendingCameraPoint = null;

            // Can we activate it now?
            if (deltaTime >= 0 && mActivationTime > 0)
            {
                if (m_ActivateAfter > 0
                    || (now - mActivationTime) < m_MinDuration)
                {
                    // Too early - make it pending
                    mPendingCameraPoint = best;
                    mPendingActivationTime = now;
                    return liveCP;
                }
            }
            // Activate now
            mActivationTime = now;
            return best;
        }

        StoryManagerGTFO.CameraPoint ChooseBestCameraPoint()
        {
            StoryManagerGTFO.CameraPoint best = null;
            for (int i = 0; i < StoryManager.Instance.NumThreads; ++i)
            {
                var th = StoryManager.Instance.GetThread(i);
                var client = th.ClientData as StoryManagerGTFO.ThreadClientData;
                if (client != null)
                {
                    for (int j = 0; j < client.m_CameraPoints.Count; ++j)
                    {
                        var cp = client.m_CameraPoints[j];
                        if (cp.m_TargetGroup.gameObject.activeSelf)
                        {
                            if (best == null || cp.m_shotQuality > best.m_shotQuality)
                                best = cp;
                        }
                    }
                    if (best != null && best.m_shotQuality >= m_MinimumShotQuality)
                        break; // good enough!
                }
            }
            return best;
        }

        private CinemachineBlendDefinition LookupBlend(
            ICinemachineCamera fromKey, ICinemachineCamera toKey)
        {
            // Get the blend curve that's most appropriate for these cameras
            CinemachineBlendDefinition blend = m_DefaultBlend;
            if (m_CustomBlends != null)
            {
                string fromCameraName = (fromKey != null) ? fromKey.Name : string.Empty;
                string toCameraName = (toKey != null) ? toKey.Name : string.Empty;
                blend = m_CustomBlends.GetBlendForVirtualCameras(
                        fromCameraName, toCameraName, blend);
            }
            return blend;
        }

        /// <summary>Notification that this virtual camera is going live.
        /// This implementation resets the child randomization.</summary>
        /// <param name="fromCam">The camera being deactivated.  May be null.</param>
        /// <param name="worldUp">Default world Up, set by the CinemachineBrain</param>
        /// <param name="deltaTime">Delta time for time-based effects (ignore if less than or equal to 0)</param>
        public override void OnTransitionFromCamera(
            ICinemachineCamera fromCam, Vector3 worldUp, float deltaTime) 
        {
            base.OnTransitionFromCamera(fromCam, worldUp, deltaTime);
            TransitioningFrom = fromCam;
            InternalUpdateCameraState(worldUp, deltaTime);
        }

        ICinemachineCamera TransitioningFrom { get; set; }

        List<StoryManager.StoryThread> mLiveThreads = new List<StoryManager.StoryThread>();
        public List<StoryManager.StoryThread> LiveThreads 
        {
            get
            {
                mLiveThreads.Clear();
                var cp = LiveCameraPoint;
                for (int i = 0; cp != null && i < cp.m_TargetGroup.m_Targets.Length; ++i)
                {
                    if (cp.m_TargetGroup.m_Targets[i].target != null && cp.m_TargetGroup.m_Targets[i].weight > 0)
                    {
                        var th = StoryManager.Instance.LookupStoryThread(cp.m_TargetGroup.m_Targets[i].target);
                        if (th != null)
                            mLiveThreads.Add(th);
                    }
                }
                return mLiveThreads;
            }
        }

    }
}
