using UnityEngine;
using System;

namespace Unity.Cinemachine
{
    /// <summary>
    /// This structure holds settings for procedural lookahead.
    /// </summary>
    [Serializable]
    public struct LookaheadSettings
    {
        /// <summary>Enable or disable procedural lookahead</summary>
        public bool Enabled;

        /// <summary>Predict the position this many seconds into the future.  
        /// Note that this setting is sensitive to noisy animation, and can amplify the noise, 
        /// resulting in undesirable jitter.
        /// If the camera jitters unacceptably when the target is in motion, turn down this setting,
        /// or increase the Smoothing setting, or animate the target more smoothly.</summary>
        [Tooltip("Predict the position this many seconds into the future.  "
            + "Note that this setting is sensitive to noisy animation, and can amplify the noise, resulting "
            + "in undesirable jitter.  If the camera jitters unacceptably when the target is in motion, "
            + "turn down this setting, or animate the target more smoothly.")]
        [Range(0f, 1f)]
        public float Time;

        /// <summary>Controls the smoothness of the lookahead algorithm.  Larger values smooth out
        /// jittery predictions and also increase prediction lag</summary>
        [Tooltip("Controls the smoothness of the lookahead algorithm.  Larger values smooth "
            + "out jittery predictions and also increase prediction lag")]
        [Range(0, 30)]
        public float Smoothing;

        /// <summary>If checked, movement along the Y axis will be ignored for lookahead calculations</summary>
        [Tooltip("If checked, movement along the Y axis will be ignored for lookahead calculations")]
        public bool IgnoreY;
    }
}