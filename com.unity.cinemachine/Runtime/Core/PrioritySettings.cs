using System;
using UnityEngine;

namespace Unity.Cinemachine
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
            readonly get => Enabled ? m_Value : 0;
            set { m_Value = value; Enabled = true; }
        }

        /// <summary> Implicit conversion to int </summary>
        /// <param name="prioritySettings"> The priority settings to convert. </param>
        /// <returns> The value of the priority settings. </returns>
        public static implicit operator int(PrioritySettings prioritySettings) => prioritySettings.Value;

        /// <summary> Implicit conversion from int </summary>
        /// <param name="priority"> The value to initialize the priority settings with. </param> 
        /// <returns> A new priority settings with the given priority. </returns>
        public static implicit operator PrioritySettings(int priority) => new () { Value = priority, Enabled = true };
    }
}
