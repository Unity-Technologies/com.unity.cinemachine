using System;
using UnityEngine;

namespace Cinemachine
{
    /// <summary>
    /// Structure for holding the output channel ID and priority of a camera.
    /// </summary>
    [Serializable]
    public struct OutputChannel
    {
        /// <summary>The CmCamera will drive all CinemachineBrains that include one or more of its 
        /// channels within its channel mask.</summary>
        public enum Channels
        {
            Default = 1,
            Channel01 = 2,
            Channel02 = 4,
            Channel03 = 8,
            Channel04 = 16,
            Channel05 = 32,
            Channel06 = 64,
            Channel07 = 128,
            Channel08 = 256,
            Channel09 = 512,
            Channel10 = 1024,
            Channel11 = 2048,
            Channel12 = 4096,
            Channel13 = 8192,
            Channel14 = 16384,
            Channel15 = 32768
        };
        
        /// <summary>
        /// If false, default priority of 0 will be used.
        /// If true, the the Priority field is valid.
        /// </summary>
        public bool Enabled;

        /// <summary>
        /// This controls which CinemachineBrain will be driven by this camera.  It is needed when there are
        /// multiple CinemachineBrains in the scene (for example, when implementing split-screen).
        /// </summary>
        [Tooltip("This controls which CinemachineBrain will be driven by this camera.  It is needed when there are "
            + "multiple CinemachineBrains in the scene (for example, when implementing split-screen).")]
        public Channels Channel;

        /// <summary>Priority to use, if Enabled is true</summary>
        [Tooltip("Priority to use.  0 is default.  Camera with highest priority is prioritized.")]
        public int Priority;

        /// <summary>Create a default value for this item</summary>
        public static OutputChannel Default => new() { Channel = Channels.Default };

        /// <summary>
        /// Set a custom priority value, and set Enabled to true.
        /// </summary>
        /// <param name="priority">The priority value to set</param>
        public void SetPriority(int priority)
        {
            Priority = priority;
            Enabled = true;
        }

        /// <summary>Get the effective priority.  Returns 0 if not Enabled.</summary>
        public int GetPriority() => Enabled ? Priority : 0;

        /// <summary>Get the effective output channel.  Returns Channels.Default if not Enabled.</summary>
        public Channels GetChannel() => Enabled ? Channel : Channels.Default;
    }
}
