using System;

namespace Unity.Cinemachine
{
    /// <summary>The CinemachineCamera will drive all CinemachineBrains that include one or more of its
    /// channels within its channel mask.</summary>
    [Flags]
    public enum OutputChannels
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
}
