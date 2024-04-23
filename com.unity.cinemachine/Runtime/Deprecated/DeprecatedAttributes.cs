#if !CINEMACHINE_NO_CM2_SUPPORT && !UNITY_7000_0_OR_NEWER
using System;
using UnityEngine;

namespace Unity.Cinemachine
{
    /// <summary>
    /// Property applied to Vcam Target fields.  Used for custom drawing in the inspector.
    /// </summary>
    [Obsolete]
    public sealed class VcamTargetPropertyAttribute : PropertyAttribute { }

}
#endif
