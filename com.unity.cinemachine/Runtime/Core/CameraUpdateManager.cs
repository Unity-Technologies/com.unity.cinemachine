using UnityEngine;
using System.Collections.Generic;

namespace Unity.Cinemachine
{
    /// <summary>Owns Camera Registry.
    /// Provides services to update cinemachine cameras and keep track of
    /// whether and how they have been updated each frame.</summary>
    internal static class CameraUpdateManager
    {
        static readonly VirtualCameraRegistry s_CameraRegistry = new ();
        static int s_RoundRobinIndex = 0;
        static int s_RoundRobinSubIndex = 0;
        static object s_LastFixedUpdateContext;
        static float s_LastUpdateTime = 0;
        static int s_FixedFrameCount = 0; // Current fixed frame count

        class UpdateStatus
        {
            public int lastUpdateFrame;
            public int lastUpdateFixedFrame;
            public UpdateTracker.UpdateClock lastUpdateMode;
        }
        static Dictionary<CinemachineVirtualCameraBase, UpdateStatus> s_UpdateStatus;

        [RuntimeInitializeOnLoadMethod]
        static void InitializeModule() => s_UpdateStatus = new ();

        /// <summary>Internal use only</summary>
        public enum UpdateFilter
        {
            Fixed = UpdateTracker.UpdateClock.Fixed,
            Late = UpdateTracker.UpdateClock.Late,
            Smart = 8, // meant to be or'ed with the others
            SmartFixed = Smart | Fixed,
            SmartLate = Smart | Late
        }
        public static UpdateFilter s_CurrentUpdateFilter;

        /// <summary>
        /// List of all active CinemachineCameras for all brains.
        /// This list is kept sorted by priority.
        /// </summary>
        public static int VirtualCameraCount => s_CameraRegistry.ActiveCameraCount;

        /// <summary>Access the priority-sorted array of active ICinemachineCamera in the scene
        /// without generating garbage</summary>
        /// <param name="index">Index of the camera to access, range 0-VirtualCameraCount</param>
        /// <returns>The virtual camera at the specified index</returns>
        public static CinemachineVirtualCameraBase GetVirtualCamera(int index)
            => s_CameraRegistry.GetActiveCamera(index);

        /// <summary>Called when a CinemachineCamera is enabled.</summary>
        public static void AddActiveCamera(CinemachineVirtualCameraBase vcam)
            => s_CameraRegistry.AddActiveCamera(vcam);

        /// <summary>Called when a CinemachineCamera is disabled.</summary>
        public static void RemoveActiveCamera(CinemachineVirtualCameraBase vcam)
            => s_CameraRegistry.RemoveActiveCamera(vcam);

        /// <summary>Called when a CinemachineCamera is destroyed.</summary>
        public static void CameraDestroyed(CinemachineVirtualCameraBase vcam)
        {
            s_CameraRegistry.CameraDestroyed(vcam);
            if (s_UpdateStatus != null && s_UpdateStatus.ContainsKey(vcam))
                s_UpdateStatus.Remove(vcam);
        }

        /// <summary>Called when a vcam is enabled.</summary>
        public static void CameraEnabled(CinemachineVirtualCameraBase vcam)
            => s_CameraRegistry.CameraEnabled(vcam);

        /// <summary>Called when a vcam is disabled.</summary>
        public static void CameraDisabled(CinemachineVirtualCameraBase vcam)
            => s_CameraRegistry.CameraDisabled(vcam);

        public static void ForgetContext(object context)
        {
            if (s_LastFixedUpdateContext == context)
                s_LastFixedUpdateContext = null;
        }

        /// <summary>Update all the active vcams in the scene, in the correct dependency order.</summary>
        public static void UpdateAllActiveVirtualCameras(uint channelMask, Vector3 worldUp, float deltaTime, object context)
        {
            // Update the fixed frame count - do it only once per fixed frmae
            if ((s_CurrentUpdateFilter & ~UpdateFilter.Smart) == UpdateFilter.Fixed
                && (s_LastFixedUpdateContext == null || s_LastFixedUpdateContext == context))
            {
                ++s_FixedFrameCount;
                s_LastFixedUpdateContext = context;
            }

            // Advance the round-robin index once per rendered frame
            var allCameras = s_CameraRegistry.AllCamerasSortedByNestingLevel;
            float now = CinemachineCore.CurrentTime;
            if (now != s_LastUpdateTime)
            {
                s_LastUpdateTime = now;
                if (allCameras.Count > 0)
                {
                    if (s_RoundRobinIndex >= allCameras.Count)
                        s_RoundRobinIndex = 0;
                    if (++s_RoundRobinSubIndex >= allCameras[s_RoundRobinIndex].Count)
                    {
                        s_RoundRobinSubIndex = 0;
                        if (++s_RoundRobinIndex >= allCameras.Count)
                            s_RoundRobinIndex = 0;
                    }
                }
            }

            // Update the leaf-most cameras first
            for (int i = allCameras.Count-1; i >= 0; --i)
            {
                var sublist = allCameras[i];
                for (int j = sublist.Count - 1; j >= 0; --j)
                {
                    var vcam = sublist[j];
                    if (vcam == null)
                    {
                        sublist.RemoveAt(j);
                        continue; // deleted
                    }

                    // Skip this vcam if it's not on the channel mask
                    if (((uint)vcam.OutputChannel & channelMask) == 0)
                        continue;

                    if (CinemachineCore.IsLive(vcam)
                        || vcam.StandbyUpdate == CinemachineVirtualCameraBase.StandbyUpdateMode.Always)
                    {
                        UpdateVirtualCamera(vcam, worldUp, deltaTime);
                    }
                    // Do round-robin update
                    else if (vcam.StandbyUpdate == CinemachineVirtualCameraBase.StandbyUpdateMode.RoundRobin
                        && s_RoundRobinIndex == i && s_RoundRobinSubIndex == j
                        && vcam.isActiveAndEnabled)
                    {
                        UpdateVirtualCamera(vcam, worldUp, deltaTime);
                    }
                }
            }
        }

        /// <summary>
        /// Update a single CinemachineCamera if and only if it
        /// hasn't already been updated this frame.  Always update vcams via this method.
        /// Calling this more than once per frame for the same camera will have no effect.
        /// </summary>
        public static void UpdateVirtualCamera(
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
            s_UpdateStatus ??= new();
            if (!s_UpdateStatus.TryGetValue(vcam, out UpdateStatus status))
            {
                status = new UpdateStatus
                {
                    lastUpdateMode = UpdateTracker.UpdateClock.Late,
                    lastUpdateFrame = CinemachineCore.CurrentUpdateFrame + 2, // so that frameDelta ends up negative
                    lastUpdateFixedFrame = s_FixedFrameCount + 2
                };
                s_UpdateStatus.Add(vcam, status);
            }

            int frameDelta = (updateClock == UpdateTracker.UpdateClock.Late)
                ? CinemachineCore.CurrentUpdateFrame - status.lastUpdateFrame
                : s_FixedFrameCount - status.lastUpdateFixedFrame;

            if (deltaTime >= 0)
            {
                if (frameDelta == 0 && status.lastUpdateMode == updateClock)
                    return; // already updated
                if (!CinemachineCore.UnitTestMode && frameDelta > 0)
                    deltaTime *= frameDelta; // try to catch up if multiple frames
            }

//Debug.Log((vcam.ParentCamera == null ? "" : vcam.ParentCamera.Name + ".") + vcam.Name + ": frame " + CinemachineCore.CurrentUpdateFrame + "/" + status.lastUpdateFixedFrame + ", " + s_CurrentUpdateFilter + ", deltaTime = " + deltaTime);
            vcam.InternalUpdateCameraState(worldUp, deltaTime);
            status.lastUpdateFrame = CinemachineCore.CurrentUpdateFrame;
            status.lastUpdateFixedFrame = s_FixedFrameCount;
            status.lastUpdateMode = updateClock;
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
            // If no target, use the vcam itself (maybe its position is being animated)
            return vcam.transform;
        }

        /// <summary>Internal use only - inspector</summary>
        public static UpdateTracker.UpdateClock GetVcamUpdateStatus(CinemachineVirtualCameraBase vcam)
        {
            if (s_UpdateStatus == null || !s_UpdateStatus.TryGetValue(vcam, out UpdateStatus status))
                return UpdateTracker.UpdateClock.Late;
            return status.lastUpdateMode;
        }
    }
}
