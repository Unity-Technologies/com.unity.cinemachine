using UnityEngine;
using System.Collections.Generic;

namespace Cinemachine
{
    /// <summary>A singleton that manages complete lists of CinemachineBrain and,
    /// Cinemachine Virtual Cameras, and the priority queue.  Provides
    /// services to keeping track of whether Cinemachine Virtual Cameras have
    /// been updated each frame.</summary>
    public sealed class CinemachineCore
    {
        /// <summary>Data version string.  Used to upgrade from legacy projects</summary>
        public static readonly int kStreamingVersion = 20170927;

        /// <summary>Human-readable Cinemachine Version</summary>
        public static readonly string kVersionString = "2.1.11";

        /// <summary>
        /// Stages in the Cinemachine Component pipeline, used for
        /// UI organization>.  This enum defines the pipeline order.
        /// </summary>
        public enum Stage
        {
            /// <summary>Second stage: position the camera in space</summary>
            Body,

            /// <summary>Third stage: orient the camera to point at the target</summary>
            Aim,

            /// <summary>Final pipeline stage: apply noise (this is done separately, in the
            /// Correction channel of the CameraState)</summary>
            Noise,

            /// <summary>Not a pipeline stage.  This is invoked on all virtual camera 
            /// types, after the pipeline is complete</summary>
            Finalize
        };

        private static CinemachineCore sInstance = null;

        /// <summary>Get the singleton instance</summary>
        public static CinemachineCore Instance
        {
            get
            {
                if (sInstance == null)
                    sInstance = new CinemachineCore();
                return sInstance;
            }
        }

        /// <summary>
        /// If true, show hidden Cinemachine objects, to make manual script mapping possible.
        /// </summary>
        public static bool sShowHiddenObjects = false;

        /// <summary>Delegate for overriding Unity's default input system.  Returns the value
        /// of the named axis.</summary>
        public delegate float AxisInputDelegate(string axisName);

        /// <summary>Delegate for overriding Unity's default input system.
        /// If you set this, then your delegate will be called instead of
        /// System.Input.GetAxis(axisName) whenever in-game user input is needed.</summary>
        public static AxisInputDelegate GetInputAxis = UnityEngine.Input.GetAxis;

        /// <summary>This event will fire after a brain updates its Camera</summary>
        public static CinemachineBrain.BrainEvent CameraUpdatedEvent = new CinemachineBrain.BrainEvent();

        /// <summary>List of all active CinemachineBrains.</summary>
        private List<CinemachineBrain> mActiveBrains = new List<CinemachineBrain>();

        /// <summary>Access the array of active CinemachineBrains in the scene</summary>
        public int BrainCount { get { return mActiveBrains.Count; } }

        /// <summary>Access the array of active CinemachineBrains in the scene 
        /// without gebnerating garbage</summary>
        /// <param name="index">Index of the brain to access, range 0-BrainCount</param>
        /// <returns>The brain at the specified index</returns>
        public CinemachineBrain GetActiveBrain(int index)
        {
            return mActiveBrains[index];
        }

        /// <summary>Called when a CinemachineBrain is enabled.</summary>
        internal void AddActiveBrain(CinemachineBrain brain)
        {
            // First remove it, just in case it's being added twice
            RemoveActiveBrain(brain);
            mActiveBrains.Insert(0, brain);
        }

        /// <summary>Called when a CinemachineBrain is disabled.</summary>
        internal void RemoveActiveBrain(CinemachineBrain brain)
        {
            mActiveBrains.Remove(brain);
        }

        /// <summary>List of all active ICinemachineCameras.</summary>
        private List<CinemachineVirtualCameraBase> mActiveCameras = new List<CinemachineVirtualCameraBase>();

        /// <summary>
        /// List of all active Cinemachine Virtual Cameras for all brains.
        /// This list is kept sorted by priority.
        /// </summary>
        public int VirtualCameraCount { get { return mActiveCameras.Count; } }

        /// <summary>Access the array of active ICinemachineCamera in the scene 
        /// without gebnerating garbage</summary>
        /// <param name="index">Index of the camera to access, range 0-VirtualCameraCount</param>
        /// <returns>The virtual camera at the specified index</returns>
        public CinemachineVirtualCameraBase GetVirtualCamera(int index)
        {
            return mActiveCameras[index];
        }

        /// <summary>Called when a Cinemachine Virtual Camera is enabled.</summary>
        internal void AddActiveCamera(CinemachineVirtualCameraBase vcam)
        {
            // Bring it to the top of the list
            RemoveActiveCamera(vcam);

            // Keep list sorted by priority
            int insertIndex;
            for (insertIndex = 0; insertIndex < mActiveCameras.Count; ++insertIndex)
                if (vcam.Priority >= mActiveCameras[insertIndex].Priority)
                    break;

            mActiveCameras.Insert(insertIndex, vcam);
        }

        /// <summary>Called when a Cinemachine Virtual Camera is disabled.</summary>
        internal void RemoveActiveCamera(CinemachineVirtualCameraBase vcam)
        {
            mActiveCameras.Remove(vcam);
        }

        // Registry of all vcams that are present, active or not
        private List<List<CinemachineVirtualCameraBase>> mAllCameras 
            = new List<List<CinemachineVirtualCameraBase>>();

        /// <summary>Called when a vcam is awakened.</summary>
        internal void CameraAwakened(CinemachineVirtualCameraBase vcam)
        {
            int parentLevel = 0;
            for (ICinemachineCamera p = vcam.ParentCamera; p != null; p = p.ParentCamera)
                ++parentLevel;
            while (mAllCameras.Count <= parentLevel)
                mAllCameras.Add(new List<CinemachineVirtualCameraBase>());
            mAllCameras[parentLevel].Add(vcam);
        }

        /// <summary>Called when a vcam is destroyed.</summary>
        internal void CameraDestroyed(CinemachineVirtualCameraBase vcam)
        {
            for (int i = 0; i < mAllCameras.Count; ++i)
                mAllCameras[i].Remove(vcam);
        }

        CinemachineVirtualCameraBase mRoundRobinVcamLastFrame = null;

        /// <summary>Update all the active vcams in the scene, in the correct dependency order.</summary>
        internal void UpdateAllActiveVirtualCameras(Vector3 worldUp, float deltaTime)
        {
            bool isLateUpdate = CurrentUpdateFilter == UpdateFilter.Late 
                || CurrentUpdateFilter == UpdateFilter.ForcedLate;
            bool canUpdateStandby = isLateUpdate || CinemachineBrain.GetSubframeCount() == 1;

            bool didRoundRobinUpdate = false;
            CinemachineVirtualCameraBase currentRoundRobin = mRoundRobinVcamLastFrame;

            // Update the leaf-most cameras first
            for (int i = mAllCameras.Count-1; i >= 0; --i)
            {
                var sublist = mAllCameras[i];
                for (int j = sublist.Count - 1; j >= 0; --j)
                {
                    var vcam = sublist[j];
                    if (!IsLive(vcam))
                    {
                        // Don't ever update subframes if not live
                        if (!canUpdateStandby)
                            continue;

                        if (vcam.m_StandbyUpdate == CinemachineVirtualCameraBase.StandbyUpdateMode.Never)
                            continue;

                        if (!vcam.isActiveAndEnabled)
                            continue;

                        // Handle round-robin
                        if (vcam.m_StandbyUpdate == CinemachineVirtualCameraBase.StandbyUpdateMode.RoundRobin)
                        {
                            if (currentRoundRobin != null)
                            {
                                if (currentRoundRobin == vcam)
                                    currentRoundRobin = null; // Take the next vcam for round-robin
                                continue;
                            }
                            didRoundRobinUpdate = true;
                            currentRoundRobin = vcam;
                        }
                    }
                    bool updated = UpdateVirtualCamera(vcam, worldUp, deltaTime);
                    if (canUpdateStandby && vcam == currentRoundRobin)
                    {
                        // Did the previous roundrobin go live this frame?
                        if (!didRoundRobinUpdate)
                            currentRoundRobin = null; // yes, take the next vcam for round-robin
                        else if (!updated)
                            currentRoundRobin = mRoundRobinVcamLastFrame; // We tried to update but it didn't happen - keep the old one for next time
                    }
                }
            }
            // Finally, if the last roundrobin update candidate no longer exists, get rid of it
            if (canUpdateStandby && !didRoundRobinUpdate)
                currentRoundRobin = null; // take the first vcam for next round-robin
            mRoundRobinVcamLastFrame = currentRoundRobin; 
        }

        /// <summary>
        /// Update a single Cinemachine Virtual Camera if and only if it
        /// hasn't already been updated this frame.  Always update vcams via this method.
        /// Calling this more than once per frame for the same camera will have no effect.
        /// </summary>
        internal bool UpdateVirtualCamera(
            CinemachineVirtualCameraBase vcam, Vector3 worldUp, float deltaTime)
        {
            UpdateFilter filter = CurrentUpdateFilter;
            bool isSmartUpdate = filter != UpdateFilter.ForcedFixed 
                && filter != UpdateFilter.ForcedLate;
            bool isSmartLateUpdate = filter == UpdateFilter.Late;
            if (!isSmartUpdate)
            {
                if (filter == UpdateFilter.ForcedFixed)
                    filter = UpdateFilter.Fixed;
                if (filter == UpdateFilter.ForcedLate)
                    filter = UpdateFilter.Late;
            }

            if (mUpdateStatus == null)
                mUpdateStatus = new Dictionary<CinemachineVirtualCameraBase, UpdateStatus>();
            if (vcam.gameObject == null)
            {
                if (mUpdateStatus.ContainsKey(vcam))
                    mUpdateStatus.Remove(vcam);
                return false; // camera was deleted
            }
            int now = Time.frameCount;
            UpdateStatus status;
            if (!mUpdateStatus.TryGetValue(vcam, out status))
            {
                status = new UpdateStatus(now);
                mUpdateStatus.Add(vcam, status);
            }

            int subframes = isSmartLateUpdate ? 1 : CinemachineBrain.GetSubframeCount();
            if (status.lastUpdateFrame != now)
                status.lastUpdateSubframe = 0;

            // If we're in smart update mode and the target moved, then we must examine
            // how the target has been moving recently in order to figure out whether to
            // update now
            bool updateNow = !isSmartUpdate;
            if (isSmartUpdate)
            {
                Matrix4x4 targetPos;
                if (!GetTargetPosition(vcam, out targetPos))
                    updateNow = isSmartLateUpdate; // no target
                else
                    updateNow = status.ChoosePreferredUpdate(now, targetPos, filter) 
                        == filter;
            }

            if (updateNow)
            {
                status.preferredUpdate = filter;
                while (status.lastUpdateSubframe < subframes)
                {
//Debug.Log((vcam.ParentCamera == null ? "" : vcam.ParentCamera.Name + ".") + vcam.Name + ": frame " + Time.frameCount + "." + status.lastUpdateSubframe + ", " + CurrentUpdateFilter + ", deltaTime = " + deltaTime);
                    vcam.InternalUpdateCameraState(worldUp, deltaTime);
                    ++status.lastUpdateSubframe;
                }
                status.lastUpdateFrame = now;
            }

            mUpdateStatus[vcam] = status;
            return updateNow;
        }

        struct UpdateStatus
        {
            const int kWindowSize = 30;

            public int lastUpdateFrame;
            public int lastUpdateSubframe;

            public int windowStart;
            public int numWindowLateUpdateMoves;
            public int numWindowFixedUpdateMoves;
            public int numWindows;
            public UpdateFilter preferredUpdate;

            public Matrix4x4 targetPos;

            public UpdateStatus(int currentFrame)
            {
                lastUpdateFrame = -1;
                lastUpdateSubframe = 0;
                windowStart = currentFrame;
                numWindowLateUpdateMoves = 0;
                numWindowFixedUpdateMoves = 0;
                numWindows = 0;
                preferredUpdate = UpdateFilter.Late;
                targetPos = Matrix4x4.zero;
            }

            // Important: updateFilter may ONLY be Late or Fixed
            public UpdateFilter ChoosePreferredUpdate(
                int currentFrame, Matrix4x4 pos, UpdateFilter updateFilter)
            {
                if (targetPos != pos)
                {
                    if (updateFilter == UpdateFilter.Late)
                        ++numWindowLateUpdateMoves;
                    else if (lastUpdateSubframe == 0)
                        ++numWindowFixedUpdateMoves;
                    targetPos = pos;
                }
                //Debug.Log("Fixed=" + numWindowFixedUpdateMoves + ", Late=" + numWindowLateUpdateMoves);
                UpdateFilter choice = preferredUpdate;
                bool inconsistent = numWindowLateUpdateMoves > 0 && numWindowFixedUpdateMoves > 0;
                if (inconsistent || numWindowLateUpdateMoves >= numWindowFixedUpdateMoves)
                    choice = UpdateFilter.Late;
                else
                    choice = UpdateFilter.Fixed;
                if (numWindows == 0)
                    preferredUpdate = choice;
 
                if (windowStart + kWindowSize <= currentFrame)
                {
                    preferredUpdate = choice;
                    ++numWindows;
                    windowStart = currentFrame;
                    numWindowLateUpdateMoves = numWindowFixedUpdateMoves = 0;
                }
                return preferredUpdate;
            }
        }
        Dictionary<CinemachineVirtualCameraBase, UpdateStatus> mUpdateStatus;

        /// <summary>Internal use only</summary>
        internal enum UpdateFilter { Fixed, ForcedFixed, Late, ForcedLate };
        internal UpdateFilter CurrentUpdateFilter { get; set; }
        private static bool GetTargetPosition(CinemachineVirtualCameraBase vcam, out Matrix4x4 targetPos)
        {
            CinemachineVirtualCameraBase vcamTarget = vcam;
            if (vcamTarget == null || vcamTarget.gameObject == null)
            {
                targetPos = Matrix4x4.identity;
                return false;
            }
            targetPos = vcamTarget.transform.localToWorldMatrix;
            if (vcamTarget.LookAt != null)
            {
                targetPos = vcamTarget.LookAt.localToWorldMatrix;
                return true;
            }
            if (vcamTarget.Follow != null)
            {
                targetPos = vcamTarget.Follow.localToWorldMatrix;
                return true;
            }
            // If no target, use the vcam itself
            targetPos = vcam.transform.localToWorldMatrix;
            return true;
        }

        /// <summary>Internal use only - inspector</summary>
        internal UpdateFilter GetVcamUpdateStatus(CinemachineVirtualCameraBase vcam)
        {
            UpdateStatus status;
            if (mUpdateStatus == null || !mUpdateStatus.TryGetValue(vcam, out status))
                return UpdateFilter.Late;
            return status.preferredUpdate;
        }

        /// <summary>
        /// Is this virtual camera currently actively controlling any Camera?
        /// </summary>
        public bool IsLive(ICinemachineCamera vcam)
        {
            if (vcam != null)
            {
                for (int i = 0; i < BrainCount; ++i)
                {
                    CinemachineBrain b = GetActiveBrain(i);
                    if (b != null && b.IsLive(vcam))
                        return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Signal that the virtual has been activated.
        /// If the camera is live, then all CinemachineBrains that are showing it will
        /// send an activation event.
        /// </summary>
        public void GenerateCameraActivationEvent(ICinemachineCamera vcam)
        {
            if (vcam != null)
            {
                for (int i = 0; i < BrainCount; ++i)
                {
                    CinemachineBrain b = GetActiveBrain(i);
                    if (b != null && b.IsLive(vcam))
                        b.m_CameraActivatedEvent.Invoke(vcam);
                }
            }
        }

        /// <summary>
        /// Signal that the virtual camera's content is discontinuous WRT the previous frame.
        /// If the camera is live, then all CinemachineBrains that are showing it will send a cut event.
        /// </summary>
        public void GenerateCameraCutEvent(ICinemachineCamera vcam)
        {
            if (vcam != null)
            {
                for (int i = 0; i < BrainCount; ++i)
                {
                    CinemachineBrain b = GetActiveBrain(i);
                    if (b != null && b.IsLive(vcam))
                        b.m_CameraCutEvent.Invoke(b);
                }
            }
        }

        /// <summary>
        /// Try to find a CinemachineBrain to associate with a
        /// Cinemachine Virtual Camera.  The first CinemachineBrain
        /// in which this Cinemachine Virtual Camera is live will be used.
        /// If none, then the first active CinemachineBrain with the correct 
        /// layer filter will be used.  
        /// Brains with OutputCamera == null will not be returned.
        /// Final result may be null.
        /// </summary>
        /// <param name="vcam">Virtual camera whose potential brain we need.</param>
        /// <returns>First CinemachineBrain found that might be
        /// appropriate for this vcam, or null</returns>
        public CinemachineBrain FindPotentialTargetBrain(CinemachineVirtualCameraBase vcam)
        {
            if (vcam != null)
            {
                int numBrains = BrainCount;
                for (int i = 0; i < numBrains; ++i)
                {
                    CinemachineBrain b = GetActiveBrain(i);
                    if (b != null && b.OutputCamera != null && b.IsLive(vcam))
                        return b;
                }
                int layer = 1 << vcam.gameObject.layer;
                for (int i = 0; i < numBrains; ++i)
                {
                    CinemachineBrain b = GetActiveBrain(i);
                    if (b != null && b.OutputCamera != null && (b.OutputCamera.cullingMask & layer) != 0)
                        return b;
                }
            }
            return null;
        }
    }
}
