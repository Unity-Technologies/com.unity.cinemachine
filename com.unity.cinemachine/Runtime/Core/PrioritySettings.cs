using System;
using UnityEngine;

namespace Cinemachine
{
    /// <summary>
    /// Structure for holding the priority of a camera.
    /// </summary>
    [Serializable]
    public struct PrioritySettings
    {
        /// <summary>
        /// If false, default priority of 0 will be used.
        /// If true, the the Priority field is valid.
        /// </summary>
        [Tooltip("Enable this to expose the Priority field")]
        public bool Enabled;

        /// <summary>The priorty value if enabled</summary>
        [Tooltip("Priority to use.  0 is default.  Camera with highest priority is prioritized.")]
        [SerializeField] int m_Value;

        /// <summary>Priority to use, if Enabled is true</summary>
        public int Value
        {
            get => Enabled ? m_Value : 0;
            set { m_Value = value; Enabled = true; }
        }
    }
}
