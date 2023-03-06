using System;
using UnityEngine;

namespace Unity.Cinemachine
{
    /// <summary>
    /// Structure for holding the output channel ID and priority of a camera.
    /// </summary>
    [Serializable]
    public struct OutputChannel
    {
        /// <summary>The CinemachineCamera will drive all CinemachineBrains that include one or more of its 
        /// channels within its channel mask.</summary>
        [Flags]
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
        [SerializeField] Channels m_Value;

        /// <summary>
        /// This controls which CinemachineBrain will be driven by this camera.  It is needed when there are
        /// multiple CinemachineBrains in the scene (for example, when implementing split-screen).
        /// </summary>
        public Channels Value
        {
            get => Enabled ? m_Value : Channels.Default;
            set { m_Value = value; Enabled = true; }
        }

        /// <summary>Create a default value for this item</summary>
        public static OutputChannel Default => new() { m_Value = Channels.Default };
    }
}
