using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cinemachine;
using Cinemachine.Utility;

namespace Spectator
{
    [ExecuteInEditMode, DisallowMultipleComponent]
    [AddComponentMenu("Cinemachine/Spectator/Director")]
    public class SpectatorDirectorGTFO : CinemachineVirtualCameraBase 
    {
        /// <summary>When enabled, the current camera and blend will be indicated in the game window, for debugging</summary>
        [Tooltip("When enabled, the current child camera and blend will be indicated in the game window, for debugging")]
        [NoSaveDuringPlay]
        public bool m_ShowDebugText = false;

        public float m_MinimumShotQuality = 0.2f;

        public enum LensMode { Auto = 0, Narrow = 10, Medium = 30, Wide = 60 };
        public LensMode m_LensMode = LensMode.Auto;

        public enum CameraMode { Auto = -1, Static = 0, Roaming = 1 };
        public CameraMode m_CameraMode = CameraMode.Auto;


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
                Cinematographer.CameraPointIndex cp = mLiveCameraPoint;
                if (cp.cameraPoint != null)
                {
                    sb.Append(" " + cp.cameraPoint.m_TargetGroup.name 
                        + ", L = " + cp.cameraPoint.m_fovData[cp.fovIndex].fov
                        + ", Q = " + cp.cameraPoint.m_fovData[cp.fovIndex].shotQuality
                        + ", T = " + cp.cameraPoint.m_TargetGroup.m_Targets.Length); 
                }
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
            mPendingCameraPoint.Clear();
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
            Cinematographer.CameraPointIndex cpi = ChooseCurrentCameraPoint(deltaTime);
            if (cpi.cameraPoint != null)
            {
                Cinematographer.CameraPoint cp = cpi.cameraPoint;
                bool isCut = !cpi.SameAs(ref mLiveCameraPoint);
                if (isCut)
                {
                    mLiveCameraPoint = cpi;

                    // Choose the first in the list that matches the desired camera type
                    for (int i = 0; i < m_ChildCameras.Length; ++i)
                    {
                        CinemachineVirtualCameraBase vcam = m_ChildCameras[i];
                        if (vcam != null && vcam.gameObject.activeInHierarchy)
                        {
                            if (best == null)
                                best = vcam;

                            // GML Note: we're abusing Priority to hold the type
                            int vcamType = Mathf.RoundToInt(vcam.Priority);
                            int bestType = Mathf.RoundToInt(best.Priority);
                            if (vcamType == cp.m_cameraType)
                            {
                                if (bestType != vcamType)
                                    best = vcam;
                                else
                                {
                                    // If the cam's fov is closer, take it
                                    float fov = cp.m_fovData[cpi.fovIndex].fov;
                                    float dA = vcam.State.Lens.FieldOfView - fov;
                                    var v = vcam as CinemachineVirtualCamera; // GML hack
                                    if (v != null) 
                                        dA = v.m_Lens.FieldOfView - fov;
                                    float dB = best.State.Lens.FieldOfView - fov;
                                    v = best as CinemachineVirtualCamera; // GML hack
                                    if (v != null) 
                                        dB = v.m_Lens.FieldOfView - fov;
                                    if (Mathf.Abs(dA) < Mathf.Abs(dB) || bestType != vcamType)
                                        best = vcam;
                                }
                            }
                        }
                    }

                    // Set the vcam's target to the target group
                    if (best != null)
                    {
                        best.LookAt = cp.m_TargetGroup.transform;
                        best.Follow = Follow;
                        best.PreviousStateIsValid = false;

                        // GML debug
                        if (Time.time - mLastCut < m_MinDuration)
                            Debug.Log("Early Cut to " + cp.m_TargetGroup.name + " after " + (Time.time - mLastCut));
                        mLastCut = Time.time;
                        CinemachineCore.Instance.GenerateCameraCutEvent(best);
                    }
                }

                // Track the camera point 
                Follow.position = cp.m_cameraPos;
                cp.m_TargetGroup.m_Targets = cp.m_fovData[cpi.fovIndex].targets.ToArray();
                cp.m_TargetGroup.DoUpdate();
            }
            return best;
        }

        float mLastCut = 0;
        float mActivationTime = 0;
        float mPendingActivationTime = 0;
        Cinematographer.CameraPointIndex mPendingCameraPoint;
        Cinematographer.CameraPointIndex mLiveCameraPoint;

        Cinematographer.CameraPointIndex ChooseCurrentCameraPoint(float deltaTime)
        {
            Cinematographer.CameraPointIndex liveCP = mLiveCameraPoint;
            Cinematographer.CameraPointIndex best = ChooseBestCameraPoint();
            float now = Time.time;
            // Is the current camera point still valid?
            if (liveCP.cameraPoint == null || !liveCP.cameraPoint.m_TargetGroup.gameObject.activeSelf)
                mActivationTime = 0;
            if (mActivationTime != 0)
            {
                // Is it active now?
                if (liveCP.cameraPoint != null && liveCP.SameAs(ref best))
                {
                    // Yes, cancel any pending
                    mPendingActivationTime = 0;
                    mPendingCameraPoint.Clear();
                    return best;
                }

                // Is it pending?
                if (deltaTime >= 0)
                {
                    if (mPendingActivationTime != 0 && !mPendingCameraPoint.SameAs(ref best))
                    {
                        // Has it been pending long enough, and are we allowed to switch away
                        // from the active action?
                        if ((now - mPendingActivationTime) > m_ActivateAfter
                            && (now - mActivationTime) > m_MinDuration)
                        {
                            // Yes, activate it now
                            mActivationTime = now;
                            mPendingActivationTime = 0;
                            mPendingCameraPoint.Clear();
                            return best;
                        }
                        return liveCP;
                    }
                }
            }
            // Neither active nor pending.
            mPendingActivationTime = 0; // cancel the pending, if any
            mPendingCameraPoint.Clear();

            // Can we activate it now?
            if (deltaTime >= 0 && mActivationTime > 0)
            {
                if (m_ActivateAfter > 0 || (now - mActivationTime) < m_MinDuration)
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

        public float m_PreferredFovBoost = 4;
        public float m_PreferredCameraTypeBoost = 4;
        
        Cinematographer.CameraPointIndex ChooseBestCameraPoint()
        {
            Cinematographer.CameraPointIndex result = new Cinematographer.CameraPointIndex();
            for (int i = 0; i < StoryManager.Instance.NumThreads; ++i)
            {
                var th = StoryManager.Instance.GetThread(i);
                for (int j = 0; j < th.m_cameraPoints.Count; ++j)
                {
                    if (!th.m_cameraPoints[j].cameraPoint.m_TargetGroup.gameObject.activeSelf)
                        continue;
                    if (result.cameraPoint == null)
                        result = th.m_cameraPoints[j];
                    else
                        result = ChooseBetterCameraPoint(result, th.m_cameraPoints[j]);
                }
                if (result.cameraPoint != null && result.cameraPoint.m_fovData[result.fovIndex].shotQuality >= m_MinimumShotQuality)
                    break; // good enough!
            }
            return result;
        }

        Cinematographer.CameraPointIndex ChooseBetterCameraPoint(
            Cinematographer.CameraPointIndex cpiA,
            Cinematographer.CameraPointIndex cpiB)
        {
            float qA = cpiA.cameraPoint.m_fovData[cpiA.fovIndex].shotQuality;
            float qB = cpiB.cameraPoint.m_fovData[cpiB.fovIndex].shotQuality;

            if (cpiA.cameraPoint.m_cameraType == (int)m_CameraMode)
                qA += m_PreferredCameraTypeBoost;
            if (cpiB.cameraPoint.m_cameraType == (int)m_CameraMode)
                qB += m_PreferredCameraTypeBoost;

            if ((float)m_LensMode > 0)
            {
                float dA = Mathf.Abs(cpiA.cameraPoint.m_fovData[cpiA.fovIndex].fov - (float)m_LensMode);
                float dB = Mathf.Abs(cpiB.cameraPoint.m_fovData[cpiB.fovIndex].fov - (float)m_LensMode);
                if (dA < dB)
                    qA += m_PreferredFovBoost;
                else if (dA > dB)
                    qB += m_PreferredFovBoost;
            }
            if (qB > qA)
                return cpiB;
            return cpiA;
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
                var cp = mLiveCameraPoint.cameraPoint;
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
