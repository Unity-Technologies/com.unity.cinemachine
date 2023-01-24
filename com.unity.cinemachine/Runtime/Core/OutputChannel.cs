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
        /// <summary>The CinemachineCamera will drive all CinemachineBrains that include one or more of its 
        /// channels within its channel mask.</summary>
        public enum Channels
        {
            /// <summary>Default Cinemachine channel.</summary>
            Default = 1,
            /// <summary>Alternate Cinemachine channel, used for assigning CinemachineCameras to specific CinemachineBrains.</summary>
            Channel01 = 2,
            /// <summary>Alternate Cinemachine channel, used for assigning CinemachineCameras to specific CinemachineBrains.</summary>
            Channel02 = 4,
            /// <summary>Alternate Cinemachine channel, used for assigning CinemachineCameras to specific CinemachineBrains.</summary>
            Channel03 = 8,
            /// <summary>Alternate Cinemachine channel, used for assigning CinemachineCameras to specific CinemachineBrains.</summary>
            Channel04 = 16,
            /// <summary>Alternate Cinemachine channel, used for assigning CinemachineCameras to specific CinemachineBrains.</summary>
            Channel05 = 32,
            /// <summary>Alternate Cinemachine channel, used for assigning CinemachineCameras to specific CinemachineBrains.</summary>
            Channel06 = 64,
            /// <summary>Alternate Cinemachine channel, used for assigning CinemachineCameras to specific CinemachineBrains.</summary>
            Channel07 = 128,
            /// <summary>Alternate Cinemachine channel, used for assigning CinemachineCameras to specific CinemachineBrains.</summary>
            Channel08 = 256,
            /// <summary>Alternate Cinemachine channel, used for assigning CinemachineCameras to specific CinemachineBrains.</summary>
            Channel09 = 512,
            /// <summary>Alternate Cinemachine channel, used for assigning CinemachineCameras to specific CinemachineBrains.</summary>
            Channel10 = 1024,
            /// <summary>Alternate Cinemachine channel, used for assigning CinemachineCameras to specific CinemachineBrains.</summary>
            Channel11 = 2048,
            /// <summary>Alternate Cinemachine channel, used for assigning CinemachineCameras to specific CinemachineBrains.</summary>
            Channel12 = 4096,
            /// <summary>Alternate Cinemachine channel, used for assigning CinemachineCameras to specific CinemachineBrains.</summary>
            Channel13 = 8192,
            /// <summary>Alternate Cinemachine channel, used for assigning CinemachineCameras to specific CinemachineBrains.</summary>
            Channel14 = 16384,
            /// <summary>Alternate Cinemachine channel, used for assigning CinemachineCameras to specific CinemachineBrains.</summary>
            Channel15 = 32768
        };
        
        /// <summary>
        /// If false, default priority of 0 will be used.
        /// If true, the the Priority field is valid.
        /// </summary>
        [Tooltip("Enable this to expose the Priority and Output Channel fields")]
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
        /// <returns>Gets the priority value, when priority is Enabled. 0 otherwise.</returns>
        public int GetPriority() => Enabled ? Priority : 0;
        
        /// <summary>Get the effective output channel mask.</summary>
        /// <returns>Returns the effective output channel mask, when Custom Priority is enabled.
        /// Returns Channels.Default otherwise.</returns>
        public Channels GetChannel() => Enabled ? Channel : Channels.Default;
    }
}
