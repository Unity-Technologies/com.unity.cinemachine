#if !CINEMACHINE_NO_CM2_SUPPORT && !UNITY_7000_0_OR_NEWER
using System;
using UnityEngine;

namespace Unity.Cinemachine
{
    /// <summary>
    /// This is a deprecated component. 
    /// </summary>
    [Obsolete("CinemachinePipeline has been deprecated.")]
    [AddComponentMenu("")] // Don't display in add component menu
    public sealed class CinemachinePipeline : MonoBehaviour
    {
    }
}
#endif
