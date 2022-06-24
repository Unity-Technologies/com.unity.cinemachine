using System;
using UnityEngine;

namespace Cinemachine
{
    /// <summary>
    /// Structure for holding the blending priority of a camera.
    /// </summary>
    [Serializable]
    public struct CameraPriority
    {
        /// <summary>Priority to use, if UseCustomPriority is true</summary>
        [Tooltip("Priority to use, if UseCustomPriority is true")]
        public int Priority;

        /// <summary>
        /// If false, default priority of 0 will be used.
        /// If true, the the Priority field is valid.
        /// </summary>
        public bool UseCustomPriority;
    }
}
