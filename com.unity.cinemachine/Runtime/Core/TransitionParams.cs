using UnityEngine;
using System;

namespace Cinemachine
{
    /// <summary> Collection of parameters that influence how a virtual camera transitions from
    /// other virtual cameras </summary>
    [Serializable]
    public struct TransitionParams
    {
        /// <summary>Hint for transitioning to and from this virtual camera</summary>
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
            /// as the current position of the Unity Camera</summary>
            InheritPosition = 8,
            /// <summary>Do not consider the tracking target when blending, just do a spherical interpolation</summary>
            IgnoreTarget = 16
        }

        /// <summary>Hint for transitioning to and from this CinemachineCamera.  Hints can be combined, although 
        /// not all combinations make sense.  In the case of conflicting hints, Cinemachine will 
        /// make an arbitrary choice.</summary>
        [Tooltip("Hint for transitioning to and from this CinemachineCamera.  Hints can be combined, although "
            + "not all combinations make sense.  In the case of conflicting hints, Cinemachine will "
            + "make an arbitrary choice.")]
        public BlendHints BlendHint;

        /// <summary>Shortcut to read InheritPosition flag in BlendHint</summary>
        public bool InheritPosition => (BlendHint & BlendHints.InheritPosition) != 0;

        /// <summary>
        /// These events fire when a transition occurs
        /// </summary>
        [Serializable]
        public struct TransitionEvents
        {
            /// <summary>This event fires when the CinemachineCamera goes Live</summary>
            [Tooltip("This event fires when the CinemachineCamera goes Live")]
            public CinemachineBrain.VcamActivatedEvent OnCameraLive;
        }
        /// <summary>
        /// These events fire when a transition occurs
        /// </summary>
        [Tooltip("These events fire when a transition occurs")]
        public TransitionEvents Events;
    }
}
