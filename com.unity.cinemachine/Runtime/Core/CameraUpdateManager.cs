using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Assertions;

namespace Unity.Cinemachine
{
    /// <summary>A singleton that manages complete lists of CinemachineBrain and,
    /// CinemachineCamera, and the priority queue.  Provides
    /// services to keeping track of whether CinemachineCameras have
    /// been updated each frame.</summary>
    internal sealed class CameraUpdateManager
    {
        /// <summary>List of all active CinemachineBrains.</summary>
        static readonly List<CinemachineBrain> s_ActiveBrains = new ();

        /// <summary>List of all active ICinemachineCameras.</summary>
        static readonly List<CinemachineVirtualCameraBase> s_ActiveCameras = new ();

        // Registry of all vcams that are present, active or not
        static readonly List<List<CinemachineVirtualCameraBase>> s_AllCameras = new();

        static bool s_ActiveCamerasAreSorted;
        static int s_ActivationSequence;
        static CinemachineVirtualCameraBase s_RoundRobinVcamLastFrame = null;
        static float s_LastUpdateTime;
        static int s_FixedFrameCount; // Current fixed frame count

        class UpdateStatus
        {
            public int lastUpdateFrame;
            public int lastUpdateFixedFrame;
            public UpdateTracker.UpdateClock lastUpdateMode;
            public float lastUpdateDeltaTime;
        }
        static Dictionary<CinemachineVirtualCameraBase, UpdateStatus> s_UpdateStatus;

        [RuntimeInitializeOnLoadMethod]
        static void InitializeModule() => s_UpdateStatus = new ();

        /// <summary>Internal use only</summary>
        internal enum UpdateFilter
        {
            Fixed = UpdateTracker.UpdateClock.Fixed,
            Late = UpdateTracker.UpdateClock.Late,
            Smart = 8, // meant to be or'ed with the others
            SmartFixed = Smart | Fixed,
            SmartLate = Smart | Late
        }
        internal static UpdateFilter s_CurrentUpdateFilter;

        /// <summary>Access the array of active CinemachineBrains in the scene</summary>
        public static int BrainCount => s_ActiveBrains.Count;

        /// <summary>Access the array of active CinemachineBrains in the scene
        /// without generating garbage</summary>
        /// <param name="index">Index of the brain to access, range 0-BrainCount</param>
        /// <returns>The brain at the specified index</returns>
        public static CinemachineBrain GetActiveBrain(int index) => s_ActiveBrains[index];

        /// <summary>Called when a CinemachineBrain is enabled.</summary>
        internal static void AddActiveBrain(CinemachineBrain brain)
        {
            // First remove it, just in case it's being added twice
            RemoveActiveBrain(brain);
            s_ActiveBrains.Insert(0, brain);
        }

        /// <summary>Called when a CinemachineBrain is disabled.</summary>
        internal static void RemoveActiveBrain(CinemachineBrain brain) => s_ActiveBrains.Remove(brain);
       
        /// <summary>
        /// List of all active CinemachineCameras for all brains.
        /// This list is kept sorted by priority.
        /// </summary>
        public static int VirtualCameraCount => s_ActiveCameras.Count;

        /// <summary>Access the priority-sorted array of active ICinemachineCamera in the scene
        /// without generating garbage</summary>
        /// <param name="index">Index of the camera to access, range 0-VirtualCameraCount</param>
        /// <returns>The virtual camera at the specified index</returns>
        public static CinemachineVirtualCameraBase GetVirtualCamera(int index)
        {
            if (!s_ActiveCamerasAreSorted && s_ActiveCameras.Count > 1)
            {
                s_ActiveCameras.Sort((x, y) => 
                    x.Priority.Value == y.Priority.Value 
                        ? y.ActivationId.CompareTo(x.ActivationId) 
                        : y.Priority.Value.CompareTo(x.Priority.Value));
                s_ActiveCamerasAreSorted = true;
            }
            return s_ActiveCameras[index];
        }

        /// <summary>Called when a CinemachineCamera is enabled.</summary>
        internal static void AddActiveCamera(CinemachineVirtualCameraBase vcam)
        {
            Assert.IsFalse(s_ActiveCameras.Contains(vcam));
            vcam.ActivationId = s_ActivationSequence++;
            s_ActiveCameras.Add(vcam);
            s_ActiveCamerasAreSorted = false;
        }

        /// <summary>Called when a CinemachineCamera is disabled.</summary>
        internal static void RemoveActiveCamera(CinemachineVirtualCameraBase vcam)
        {
            if (s_ActiveCameras.Contains(vcam))
                s_ActiveCameras.Remove(vcam);
        }

        /// <summary>Called when a CinemachineCamera is destroyed.</summary>
        internal static void CameraDestroyed(CinemachineVirtualCameraBase vcam)
        {
            if (s_ActiveCameras.Contains(vcam))
                s_ActiveCameras.Remove(vcam);
            if (s_UpdateStatus != null && s_UpdateStatus.ContainsKey(vcam))
                s_UpdateStatus.Remove(vcam);
        }

        /// <summary>Called when a vcam is enabled.</summary>
        internal static void CameraEnabled(CinemachineVirtualCameraBase vcam)
        {
            int parentLevel = 0;
            for (var p = vcam.ParentCamera; p != null; p = p.ParentCamera)
                ++parentLevel;
            while (s_AllCameras.Count <= parentLevel)
                s_AllCameras.Add(new List<CinemachineVirtualCameraBase>());
            s_AllCameras[parentLevel].Add(vcam);
        }

        /// <summary>Called when a vcam is disabled.</summary>
        internal static void CameraDisabled(CinemachineVirtualCameraBase vcam)
        {
            for (int i = 0; i < s_AllCameras.Count; ++i)
                s_AllCameras[i].Remove(vcam);
            if (s_RoundRobinVcamLastFrame == vcam)
                s_RoundRobinVcamLastFrame = null;
        }

        /// <summary>Update all the active vcams in the scene, in the correct dependency order.</summary>
        internal static void UpdateAllActiveVirtualCameras(uint channelMask, Vector3 worldUp, float deltaTime)
        {
            // Setup for roundRobin standby updating
            var filter = s_CurrentUpdateFilter;
            bool canUpdateStandby = (filter != UpdateFilter.SmartFixed); // never in smart fixed
            var currentRoundRobin = s_RoundRobinVcamLastFrame;

            // Update the fixed frame count
            float now = CinemachineCore.CurrentTime;
            if (now != s_LastUpdateTime)
            {
                s_LastUpdateTime = now;
                if ((filter & ~UpdateFilter.Smart) == UpdateFilter.Fixed)
                    ++s_FixedFrameCount;
            }

            // Update the leaf-most cameras first
            for (int i = s_AllCameras.Count-1; i >= 0; --i)
            {
                var sublist = s_AllCameras[i];
                for (int j = sublist.Count - 1; j >= 0; --j)
                {
                    var vcam = sublist[j];
                    if (canUpdateStandby && vcam == s_RoundRobinVcamLastFrame)
                        currentRoundRobin = null; // update the next roundrobin candidate
                    if (vcam == null)
                    {
                        sublist.RemoveAt(j);
                        continue; // deleted
                    }
                    if (vcam.StandbyUpdate == CinemachineVirtualCameraBase.StandbyUpdateMode.Always
                        || CinemachineCore.IsLive(vcam))
                    {
                        // Skip this vcam if it's not on the channel mask
                        if (((uint)vcam.OutputChannel.Value & channelMask) != 0)
                            UpdateVirtualCamera(vcam, worldUp, deltaTime);
                    }
                    else if (currentRoundRobin == null
                        && s_RoundRobinVcamLastFrame != vcam
                        && canUpdateStandby
                        && vcam.StandbyUpdate != CinemachineVirtualCameraBase.StandbyUpdateMode.Never
                        && vcam.isActiveAndEnabled)
                    {
                        // Do the round-robin update
                        s_CurrentUpdateFilter &= ~UpdateFilter.Smart; // force it
                        UpdateVirtualCamera(vcam, worldUp, deltaTime);
                        s_CurrentUpdateFilter = filter;
                        currentRoundRobin = vcam;
                    }
                }
            }

            // Did we manage to update a roundrobin?
            if (canUpdateStandby)
            {
                if (currentRoundRobin == s_RoundRobinVcamLastFrame)
                    currentRoundRobin = null; // take the first candidate
                s_RoundRobinVcamLastFrame = currentRoundRobin;
            }
        }

        /// <summary>
        /// Update a single CinemachineCamera if and only if it
        /// hasn't already been updated this frame.  Always update vcams via this method.
        /// Calling this more than once per frame for the same camera will have no effect.
        /// </summary>
        internal static void UpdateVirtualCamera(
            CinemachineVirtualCameraBase vcam, Vector3 worldUp, float deltaTime)
        {
            if (vcam == null)
                return;

            bool isSmartUpdate = (s_CurrentUpdateFilter & UpdateFilter.Smart) == UpdateFilter.Smart;
            var updateClock = (UpdateTracker.UpdateClock)(s_CurrentUpdateFilter & ~UpdateFilter.Smart);

            // If we're in smart update mode and the target moved, then we must examine
            // how the target has been moving recently in order to figure out whether to
            // update now
            if (isSmartUpdate)
            {
                var updateTarget = GetUpdateTarget(vcam);
                if (updateTarget == null)
                    return;   // vcam deleted
                if (UpdateTracker.GetPreferredUpdate(updateTarget) != updateClock)
                    return;   // wrong clock
            }

            // Have we already been updated this frame?
            if (s_UpdateStatus == null)
                s_UpdateStatus = new();
            if (!s_UpdateStatus.TryGetValue(vcam, out UpdateStatus status))
            {
                status = new UpdateStatus
                {
                    lastUpdateDeltaTime = -2,
                    lastUpdateMode = UpdateTracker.UpdateClock.Late,
                    lastUpdateFrame = Time.frameCount + 2, // so that frameDelta ends up negative
                    lastUpdateFixedFrame = s_FixedFrameCount + 2
                };
                s_UpdateStatus.Add(vcam, status);
            }
            
            int frameDelta = (updateClock == UpdateTracker.UpdateClock.Late)
                ? Time.frameCount - status.lastUpdateFrame
                : s_FixedFrameCount - status.lastUpdateFixedFrame;
            
            if (deltaTime >= 0)
            {
                if (frameDelta == 0 && status.lastUpdateMode == updateClock
                        && status.lastUpdateDeltaTime == deltaTime)
                    return; // already updated
                if (!CinemachineCore.UnitTestMode && frameDelta > 0)
                    deltaTime *= frameDelta; // try to catch up if multiple frames
            }

//Debug.Log((vcam.ParentCamera == null ? "" : vcam.ParentCamera.Name + ".") + vcam.Name + ": frame " + Time.frameCount + "/" + status.lastUpdateFixedFrame + ", " + CurrentUpdateFilter + ", deltaTime = " + deltaTime);
            vcam.InternalUpdateCameraState(worldUp, deltaTime);
            status.lastUpdateFrame = Time.frameCount;
            status.lastUpdateFixedFrame = s_FixedFrameCount;
            status.lastUpdateMode = updateClock;
            status.lastUpdateDeltaTime = deltaTime;
        }

        static Transform GetUpdateTarget(CinemachineVirtualCameraBase vcam)
        {
            if (vcam == null || vcam.gameObject == null)
                return null;
            var target = vcam.LookAt;
            if (target != null)
                return target;
            target = vcam.Follow;
            if (target != null)
                return target;
            // If no target, use the vcam itself
            return vcam.transform;
        }

        /// <summary>Internal use only - inspector</summary>
        internal static UpdateTracker.UpdateClock GetVcamUpdateStatus(CinemachineVirtualCameraBase vcam)
        {
            if (s_UpdateStatus == null || !s_UpdateStatus.TryGetValue(vcam, out UpdateStatus status))
                return UpdateTracker.UpdateClock.Late;
            return status.lastUpdateMode;
        }
    }
}
