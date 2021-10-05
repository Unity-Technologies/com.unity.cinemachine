using UnityEngine;

namespace Cinemachine.Editor
{
    /// <summary>
    /// Collection of tools and helpers for scene view
    /// </summary>
    class SceneViewUtility
    {
        /// <summary>
        /// Solo the given vcam on conditionToSolo, and unsolos when it is false.
        /// Extra condition to unsolo is to detect an error from handles, and don't unsolo when error is detected.
        /// </summary>
        /// <param name="vcam">Vcam to unsolo</param>
        /// <param name="conditionToSolo"></param>
        /// <param name="extraConditionToUnsolo">This is only needed until the work around is done.</param>
        internal static void SoloVcamOnConditions(ICinemachineCamera vcam, bool conditionToSolo, bool extraConditionToUnsolo = true)
        {
            Debug.Log(vcam.Name +": conditionToSolo:"+conditionToSolo+"extraConditionToUnsolo:"+extraConditionToUnsolo);
            // solo this vcam when dragging
            if (conditionToSolo)
            {
                // if solo was activated by the user, then it was not the tool who set it to solo.
                s_SoloSetByTools = s_SoloSetByTools || CinemachineBrain.SoloCamera != vcam;
                CinemachineBrain.SoloCamera = vcam;
            }
            else if (s_SoloSetByTools && extraConditionToUnsolo)
            {
                CinemachineBrain.SoloCamera = null;
                s_SoloSetByTools = false;
            }
        } 
        static bool s_SoloSetByTools;
    }
}
