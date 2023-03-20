using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Assertions;
using System;
using UnityEngine.Events;

namespace Unity.Cinemachine
{
    internal static class Documentation 
    {
        /// <summary>This must be used like
        /// [HelpURL(Documentation.BaseURL + "api/some-page.html")]
        /// or
        /// [HelpURL(Documentation.BaseURL + "manual/some-page.html")]
        /// It cannot support String.Format nor string interpolation</summary>
        public const string BaseURL = "https://docs.unity3d.com/Packages/com.unity.cinemachine@3.0/";
    }

    /// <summary>A singleton that manages complete lists of CinemachineBrain and,
    /// CinemachineCamera, and the priority queue.  Provides
    /// services to keeping track of whether CinemachineCameras have
    /// been updated each frame.</summary>
    public sealed class CinemachineCore
    {
        /// <summary>Data version string.  Used to upgrade from legacy projects</summary>
        internal const int kStreamingVersion = 20230301;

        /// <summary>
        /// Stages in the Cinemachine Component pipeline.  This enum defines the pipeline order.
        /// </summary>
        public enum Stage
        {
            /// <summary>First stage: position the camera in space</summary>
            Body,

            /// <summary>Second stage: orient the camera to point at the target</summary>
            Aim,

            /// <summary>Final pipeline stage: apply noise (this is done separately, in the
            /// Correction channel of the CameraState)</summary>
            Noise,

            /// <summary>Post-correction stage.  This is invoked on all virtual camera
            /// types, after the pipeline is complete</summary>
            Finalize
        };
        

        /// <summary>Hint for transitioning to and from Cinemachine cameras</summary>
        [Flags]
        public enum BlendHints
        {
            /// <summary>Spherical blend about Tracking target position</summary>
            SphericalPosition = 1, 
            /// <summary>Cylindrical blend about Tracking target position (vertical co-ordinate is linearly interpolated)</summary>
            CylindricalPosition = 2,
            /// <summary>Screen-space blend between LookAt targets instead of world space lerp of target position</summary>
            ScreenSpaceAimWhenTargetsDiffer = 4,
            /// <summary>When this virtual camera goes Live, attempt to force the position to be the same 
            /// as the current position of the outgoing Camera</summary>
            InheritPosition = 8,
            /// <summary>Do not consider the tracking target when blending, just do a spherical interpolation</summary>
            IgnoreTarget = 16,
            // /// <summary>When blending out from this camera, use a snapshot of its outgoing state instead of a live state</summary>
            //BlendOutFromSnapshot = 32, // GML todo: think about this
        }

        /// <summary>Delegate for overriding Unity's default input system.  Returns the value
        /// of the named axis.</summary>
        public delegate float AxisInputDelegate(string axisName);

        /// <summary>Delegate for overriding Unity's default input system.
        /// If you set this, then your delegate will be called instead of
        /// System.Input.GetAxis(axisName) whenever in-game user input is needed.</summary>
#if ENABLE_LEGACY_INPUT_MANAGER
        public static AxisInputDelegate GetInputAxis = UnityEngine.Input.GetAxis;
#else
        public static AxisInputDelegate GetInputAxis = delegate { return 0; };
#endif

        /// <summary>
        /// If non-negative, cinemachine will update with this uniform delta time.
        /// Usage is for timelines in manual update mode.
        /// </summary>
        public static float UniformDeltaTimeOverride = -1;

        /// <summary>
        /// Replacement for Time.deltaTime, taking UniformDeltaTimeOverride into account.
        /// </summary>
        public static float DeltaTime => UniformDeltaTimeOverride >= 0 ? UniformDeltaTimeOverride : Time.deltaTime;

        /// <summary>
        /// If non-negative, cinemachine will use this value whenever it wants current game time.
        /// Usage is for master timelines in manual update mode, for deterministic behaviour.
        /// </summary>
        public static float CurrentTimeOverride = -1;

        /// <summary>
        /// Replacement for Time.time, taking CurrentTimeTimeOverride into account.
        /// </summary>
        public static float CurrentTime => CurrentTimeOverride >= 0 ? CurrentTimeOverride : Time.time;
        
        /// <summary>
        /// If non-negative, cinemachine will use this value whenever it wants current unscaled game time.
        /// Usage is for InputAxis in manual update mode, for deterministic behaviour.
        /// </summary>
        internal static float CurrentUnscaledTimeTimeOverride = -1;
        
        /// <summary>
        /// Replacement for Time.unscaledTime, taking CurrentUnscaledTimeTimeOverride into account.
        /// </summary>
        internal static float CurrentUnscaledTime => CurrentUnscaledTimeTimeOverride >= 0
            ? CurrentUnscaledTimeTimeOverride
            : Time.unscaledTime;

        /// <summary>
        /// Delegate for overriding a blend that is about to be applied to a transition.
        /// A handler can either return the default blend, or a new blend specific to
        /// current conditions.
        /// </summary>
        /// <param name="fromVcam">The outgoing virtual camera</param>
        /// <param name="toVcam">Yhe incoming virtual camera</param>
        /// <param name="defaultBlend">The blend that would normally be applied</param>
        /// <param name="owner">The context in which the blend is taking place.
        /// Can be a CinemachineBrain, or CinemachineStateDrivenCamera, or other manager
        /// object that can initiate a blend</param>
        /// <returns>The blend definition to use for this transition.</returns>
        public delegate CinemachineBlendDefinition GetBlendOverrideDelegate(
            ICinemachineCamera fromVcam, ICinemachineCamera toVcam,
            CinemachineBlendDefinition defaultBlend,
            UnityEngine.Object owner);

        /// <summary>
        /// Delegate for overriding a blend that is about to be applied to a transition.
        /// A handler can either return the default blend, or a new blend specific to
        /// current conditions.
        /// </summary>
        public static GetBlendOverrideDelegate GetBlendOverride;

        /// <summary>Event with CinemachineBrain as parameter.</summary>
        [Serializable]
        public class BrainEvent : UnityEvent<CinemachineBrain> {}
        
        /// <summary>This event will fire after a brain updates its Camera</summary>
        public static BrainEvent CameraUpdatedEvent = new ();

        /// <summary>This event will fire when the current camera changes, at the start of a blend</summary>
        public static ICinemachineCamera.ActivationEvent CameraActivatedEvent = new ();

        /// <summary>Special mode for deterministic unit tests.</summary>
        internal static bool UnitTestMode = false;

        /// <summary>Access the array of active CinemachineBrains in the scene</summary>
        public static int BrainCount => CameraUpdateManager.BrainCount;
        
        /// <summary>Access the array of active CinemachineBrains in the scene
        /// without generating garbage</summary>
        /// <param name="index">Index of the brain to access, range 0-BrainCount</param>
        /// <returns>The brain at the specified index</returns>
        public static CinemachineBrain GetActiveBrain(int index) => CameraUpdateManager.GetActiveBrain(index);

        /// <summary>
        /// List of all active CinemachineCameras for all brains.
        /// This list is kept sorted by priority.
        /// </summary>
        public static int VirtualCameraCount => CameraUpdateManager.VirtualCameraCount;

        /// <summary>Access the priority-sorted array of active ICinemachineCamera in the scene
        /// without generating garbage</summary>
        /// <param name="index">Index of the camera to access, range 0-VirtualCameraCount</param>
        /// <returns>The virtual camera at the specified index</returns>
        public static CinemachineVirtualCameraBase GetVirtualCamera(int index) => CameraUpdateManager.GetVirtualCamera(index);

        /// <summary>
        /// API for the Unity Editor.
        /// Show this camera no matter what.  This is static, and so affects all Cinemachine brains.
        /// </summary>
        public static ICinemachineCamera SoloCamera
        {
            get => s_SoloCamera;
            set
            {
                if (value != null && !CinemachineCore.IsLive(value))
                    value.OnCameraActivated(new ICinemachineCamera.ActivationEventParams
                    {
                        Origin = null,
                        OutgoingCamera = null,
                        IncomingCamera = value,
                        IsCut = true,
                        WorldUp = Vector3.up,
                        DeltaTime = DeltaTime
                    });
                s_SoloCamera = value;
            }
        }
        static ICinemachineCamera s_SoloCamera;

        /// <summary>
        /// Is this virtual camera currently actively controlling any Camera?
        /// </summary>
        /// <param name="vcam">The virtual camera in question</param>
        /// <returns>True if the vcam is currently driving a Brain</returns>
        public static bool IsLive(ICinemachineCamera vcam)
        {
            if (vcam != null)
            {
                int numBrains = CameraUpdateManager.BrainCount;
                for (int i = 0; i < numBrains; ++i)
                {
                    var b = CameraUpdateManager.GetActiveBrain(i);
                    if (b != null && b.IsLiveChild(vcam))
                        return true;
                }
            }
            return false;
        }
        
        /// <summary>
        /// Checks if the vcam is live as part of an outgoing blend in any active CinemachineBrain.  
        /// Does not check whether the vcam is also the current active vcam.
        /// </summary>
        /// <param name="vcam">The virtual camera to check</param>
        /// <returns>True if the virtual camera is part of a live outgoing blend, false otherwise</returns>
        public static bool IsLiveInBlend(ICinemachineCamera vcam)
        {
            if (vcam != null)
            {
                int numBrains = CameraUpdateManager.BrainCount;
                for (int i = 0; i < numBrains; ++i)
                {
                    var b = CameraUpdateManager.GetActiveBrain(i);
                    if (b != null && b.IsLiveInBlend(vcam))
                        return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Try to find a CinemachineBrain to associate with a
        /// CinemachineCamera.  The first CinemachineBrain
        /// in which this CinemachineCamera is live will be used.
        /// If none, then the first active CinemachineBrain with the correct
        /// layer filter will be used.
        /// Brains with OutputCamera == null will not be returned.
        /// Final result may be null.
        /// </summary>
        /// <param name="vcam">Virtual camera whose potential brain we need.</param>
        /// <returns>First CinemachineBrain found that might be
        /// appropriate for this vcam, or null</returns>
        public static CinemachineBrain FindPotentialTargetBrain(CinemachineVirtualCameraBase vcam)
        {
            if (vcam != null)
            {
                int numBrains = CameraUpdateManager.BrainCount;
                for (int i = 0; i < numBrains; ++i)
                {
                    var b = GetActiveBrain(i);
                    if (b != null && b.OutputCamera != null && b.IsLiveChild(vcam))
                        return b;
                }
                var channel = (uint)vcam.OutputChannel.Value;
                for (int i = 0; i < numBrains; ++i)
                {
                    var b = GetActiveBrain(i);
                    if (b != null && b.OutputCamera != null && ((uint)b.ChannelMask & channel) != 0)
                        return b;
                }
            }
            return null;
        }

        /// <summary>Call this to notify all virtual cameras that may be tracking a target
        /// that the target's position has suddenly warped to somewhere else, so that
        /// the virtual cameras can update their internal state to make the camera
        /// warp seamlessly along with the target.
        /// 
        /// All virtual cameras are iterated so this call will work no matter how many 
        /// are tracking the target, and whether they are active or inactive.
        /// </summary>
        /// <param name="target">The object that was warped</param>
        /// <param name="positionDelta">The amount the target's position changed</param>
        public static void OnTargetObjectWarped(Transform target, Vector3 positionDelta)
        {
            var numVcams = CameraUpdateManager.VirtualCameraCount;
            for (int i = 0; i < numVcams; ++i)
                GetVirtualCamera(i).OnTargetObjectWarped(target, positionDelta);
        }

        /// <summary>
        /// Signal that the virtual has been activated.
        /// If the camera is live, then all CinemachineBrains that are showing it will
        /// send an activation event.
        /// </summary>
        /// <param name="outgoing">The previously-active virtual camera (may be null)</param>
        /// <param name="incoming">The virtual camera being activated</param>
        /// <param name="isCut">If true then transition is instantaneous</param>
        internal static void GenerateCameraActivationEvent(
            ICinemachineCamera outgoing, ICinemachineCamera incoming, bool isCut)
        {
            if (incoming != null)
            {
                // Generate an event for each brain in which the camera is live
                int numBrains = CameraUpdateManager.BrainCount;
                for (int i = 0; i < numBrains; ++i)
                {
                    var brain = GetActiveBrain(i);
                    if (brain != null && brain.IsLiveChild(incoming))
                    {
                        var eventParams = new ICinemachineCamera.ActivationEventParams()
                        {
                            Origin = brain,
                            IncomingCamera = incoming,
                            OutgoingCamera = outgoing,
                            IsCut = isCut
                        };
                        CameraActivatedEvent.Invoke(eventParams);
                    }
                }
            }
        }
    }
}
