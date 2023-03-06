using UnityEngine;
using System;
using UnityEngine.Serialization;

namespace Unity.Cinemachine
{
    /// <summary>
    /// Asset that defines the rules for blending between Virtual Cameras.
    /// </summary>
    [Serializable]
    [HelpURL(Documentation.BaseURL + "manual/CinemachineBlending.html")]
    public sealed class CinemachineBlenderSettings : ScriptableObject
    {
        /// <summary>
        /// Container specifying how two specific CinemachineCameras blend together.
        /// </summary>
        [Serializable]
        public struct CustomBlend
        {
            /// <summary>When blending from this camera</summary>
            [Tooltip("When blending from this camera")]
            [FormerlySerializedAs("m_From")]
            public string From;

            /// <summary>When blending to this camera</summary>
            [Tooltip("When blending to this camera")]
            [FormerlySerializedAs("m_To")]
            public string To;

            /// <summary>Blend curve definition</summary>
            [Tooltip("Blend curve definition")]
            [FormerlySerializedAs("m_Blend")]
            public CinemachineBlendDefinition Blend;
        }
        /// <summary>The array containing explicitly defined blends between two Virtual Cameras</summary>
        [Tooltip("The array containing explicitly defined blends between two Virtual Cameras")]
        [FormerlySerializedAs("m_CustomBlends")]
        public CustomBlend[] CustomBlends = null;

        /// <summary>Internal API for the inspector editopr: a label to represent any camera</summary>
        internal const string kBlendFromAnyCameraLabel = "**ANY CAMERA**";

        /// <summary>
        /// Attempts to find a blend definition which matches the to and from cameras as specified.
        /// If no match is found, the function returns the supplied default blend.
        /// </summary>
        /// <param name="fromCameraName">The game object name of the from camera</param>
        /// <param name="toCameraName">The game object name of the to camera</param>
        /// <param name="defaultBlend">Blend to return if no custom blend found.</param>
        /// <returns></returns>
        public CinemachineBlendDefinition GetBlendForVirtualCameras(
            string fromCameraName, string toCameraName, CinemachineBlendDefinition defaultBlend)
        {
            bool gotAnyToMe = false;
            bool gotMeToAny = false;
            CinemachineBlendDefinition anyToMe = defaultBlend;
            CinemachineBlendDefinition meToAny = defaultBlend;
            if (CustomBlends != null)
            {
                for (int i = 0; i < CustomBlends.Length; ++i)
                {
                    // Attempt to find direct name first
                    CustomBlend blendParams = CustomBlends[i];
                    if ((blendParams.From == fromCameraName)
                        && (blendParams.To == toCameraName))
                    {
                        return blendParams.Blend;
                    }
                    // If we come across applicable wildcards, remember them
                    if (blendParams.From == kBlendFromAnyCameraLabel)
                    {
                        if (!string.IsNullOrEmpty(toCameraName)
                            && blendParams.To == toCameraName)
                        {
                            if (!gotAnyToMe)
                                anyToMe = blendParams.Blend;
                            gotAnyToMe = true;
                        }
                        else if (blendParams.To == kBlendFromAnyCameraLabel)
                            defaultBlend = blendParams.Blend;
                    }
                    else if (blendParams.To == kBlendFromAnyCameraLabel
                             && !string.IsNullOrEmpty(fromCameraName)
                             && blendParams.From == fromCameraName)
                    {
                        if (!gotMeToAny)
                            meToAny = blendParams.Blend;
                        gotMeToAny = true;
                    }
                }
            }

            // If nothing is found try to find wild card blends from any
            // camera to our new one
            if (gotAnyToMe)
                return anyToMe;

            // Still have nothing? Try from our camera to any camera
            if (gotMeToAny)
                return meToAny;

            return defaultBlend;
        }
    }
}
