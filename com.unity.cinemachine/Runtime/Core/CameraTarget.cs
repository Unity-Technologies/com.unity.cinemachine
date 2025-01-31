using System;
using UnityEngine;

namespace Unity.Cinemachine
{
    /// <summary>
    /// Structure for holding the procedural motion targets for a CinemachineCamera.
    /// The main TrackingTarget is used by default for all object tracking.  Optionally, a second
    /// LookAt target can be provided to make the camera follow one object while
    /// pointing at another.
    /// </summary>
    [Serializable]
    public struct CameraTarget
    {
        /// <summary>Object for the camera to follow</summary>
        [Tooltip("Object for the camera to follow")]
        public Transform TrackingTarget;

        /// <summary>Optional secondary object for the camera to look at</summary>
        [Tooltip("Object for the camera to look at")]
        public Transform LookAtTarget;

        /// <summary>
        /// If false, TrackingTarget will be used for all object tracking.
        /// If true, then LookAt is used for rotation tracking and
        /// TrackingTarget is used only for position tracking.
        /// </summary>
        public bool CustomLookAtTarget;
    }
}
