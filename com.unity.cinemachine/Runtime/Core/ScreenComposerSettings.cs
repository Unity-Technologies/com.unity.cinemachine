using UnityEngine;
using System;

namespace Cinemachine
{
    /// <summary>This structure holds settings for screen-space composition.</summary>
    [Serializable]
    public struct ScreenComposerSettings
    {
        /// <summary>Screen position for target. The camera will adjust to position the 
        /// tracked object here.  0 is screen center, and 1 is screen edge</summary>
        [Tooltip("Screen position for target. The camera will adjust to position the "
        + "tracked object here.  0 is screen center, and +1 or -1 is screen edge")]
        public Vector2 ScreenPosition;

        [Serializable]
        public struct DeadZoneSettings
        {
            /// <summary>Enables the Dead Zone settings</summary>
            public bool Enabled;
            /// <summary>The camera will not adjust if the target is within this range of the screen position.  
            /// 0 is screen center, and +1 or -1 is screen edge</summary>
            [Tooltip("The camera will not adjust if the target is within this range of the screen position.  "
                + "0 is screen center, and +1 or -1 is screen edge")]
            public Vector2 Size;
        }
        /// <summary>The camera will not adjust if the target is within this range of the screen position</summary>
        [Tooltip("The camera will not adjust if the target is within this range of the screen position")]
        [FoldoutWithEnabledButton]
        public DeadZoneSettings DeadZone;

        /// <summary>The target will not be allowed to be outside this region.
        /// When the target is within this region, the camera will gradually adjust to re-align
        /// towards the desired position, depending on the damping speed</summary>
        [Serializable]
        public struct HardLimitSettings
        {
            /// <summary>Enables the Hard Limit settings</summary>
            public bool Enabled;
            /// <summary>The target will not be allowed to be outside this region.
            /// When the target is within this region, the camera will gradually adjust to re-align
            /// towards the desired position, depending on the damping speed.  
            /// 0 is screen center, and +1 or -1 is screen edge</summary>
            [Tooltip("The target will not be allowed to be outside this region. "
                + "When the target is within this region, the camera will gradually adjust to re-align "
                + "towards the desired position, depending on the damping speed.  "
                + "0 is screen center, and +1 or -1 is screen edge")]
            public Vector2 Size;
            /// <summary>A zero Bias means that the hard limits will be centered around the target screen position.  
            /// A nonzero bias will uncenter the target screen position within the hard limits.
            /// </summary>
            [Tooltip("A zero Bias means that the hard limits will be centered around the target screen position.  "
                + "A nonzero bias will uncenter the target screen position within the hard limits.")]
            public Vector2 Bias;
        }
        /// <summary>The target will not be allowed to be outside this region.
        /// When the target is within this region, the camera will gradually adjust to re-align
        /// towards the desired position, depending on the damping speed</summary>
        [Tooltip("The target will not be allowed to be outside this region. "
            + "When the target is within this region, the camera will gradually adjust to re-align "
            + "towards the desired position, depending on the damping speed")]
        [FoldoutWithEnabledButton]
        public HardLimitSettings HardLimits;

        /// <summary>Clamps values to the expected ranges</summary>
        public void Validate()
        {
            ScreenPosition.x = Mathf.Clamp(ScreenPosition.x, -1.5f, 1.5f);
            ScreenPosition.y = Mathf.Clamp(ScreenPosition.y, -1.5f, 1.5f);
            DeadZone.Size.x = Mathf.Clamp(DeadZone.Size.x, 0f, 2f);
            DeadZone.Size.y = Mathf.Clamp(DeadZone.Size.y, 0f, 2f);
            HardLimits.Size.x = Mathf.Clamp(HardLimits.Size.x, 0f, 6f);
            HardLimits.Size.y = Mathf.Clamp(HardLimits.Size.y, 0f, 6f);
            HardLimits.Bias.x = Mathf.Clamp(HardLimits.Bias.x, -1f, 1f);
            HardLimits.Bias.y = Mathf.Clamp(HardLimits.Bias.y, -1f, 1f);
        }

        /// <summary>Get the effictive dead zone size, taking enabled state into account</summary>
        public Vector2 EffectiveDeadZoneSize => DeadZone.Enabled ? DeadZone.Size : Vector2.zero;

        /// <summary>Get the effictive hard limits size, taking enabled state into account</summary>
        public Vector2 EffectiveHardLimitSize => HardLimits.Enabled ? HardLimits.Size : new Vector2(6, 6);

        /// <summary>Get/set the screenspace rect for the dead zone region.  This also defines screen position</summary>
        public Rect DeadZoneRect
        {
            get
            {
                var deadZoneSize = EffectiveDeadZoneSize;
                return new Rect(ScreenPosition - deadZoneSize / 2 + new Vector2(0.5f, 0.5f), deadZoneSize);
            }
            set
            {
                var deadZoneSize = EffectiveDeadZoneSize;
                if (DeadZone.Enabled)
                {
                    deadZoneSize = new Vector2(Mathf.Clamp(value.width, 0, 2), Mathf.Clamp(value.height, 0, 2));
                    DeadZone.Size = deadZoneSize;
                }
                ScreenPosition = new Vector2(
                    Mathf.Clamp(value.x - 0.5f + deadZoneSize.x / 2, -1.5f,  1.5f), 
                    Mathf.Clamp(value.y - 0.5f + deadZoneSize.y / 2, -1.5f,  1.5f));
                HardLimits.Size = new Vector2(
                    Mathf.Max(HardLimits.Size.x, deadZoneSize.x),
                    Mathf.Max(HardLimits.Size.y, deadZoneSize.y));
            }
        }

        /// <summary>Get/set the screenspace rect for the hard limits.</summary>
        public Rect HardLimitsRect
        {
            get
            {
                if (!HardLimits.Enabled)
                    return new Rect(Vector2.zero, EffectiveHardLimitSize);
                var r = new Rect(ScreenPosition - HardLimits.Size / 2 + new Vector2(0.5f, 0.5f), HardLimits.Size);
                var deadZoneSize = EffectiveDeadZoneSize;
                r.position += new Vector2(
                    HardLimits.Bias.x * 0.5f * (HardLimits.Size.x - deadZoneSize.x),
                    HardLimits.Bias.y * 0.5f * (HardLimits.Size.y - deadZoneSize.y));
                return r;
            }
            set
            {
                HardLimits.Size.x = Mathf.Clamp(value.width, 0, 6f);
                HardLimits.Size.y = Mathf.Clamp(value.height, 0, 6f);
                DeadZone.Size.x = Mathf.Min(DeadZone.Size.x, HardLimits.Size.x);
                DeadZone.Size.y = Mathf.Min(DeadZone.Size.y, HardLimits.Size.y);
                // GML todo: set bias
            }
        }
        
        /// <summary>
        /// Linear interpolation between 2 settings objects.
        /// </summary>
        /// <param name="a">First settings object</param>
        /// <param name="b">Second settings object</param>
        /// <param name="t">Interpolation amount: 0 is a, 1 is b</param>
        /// <returns></returns>
        public static ScreenComposerSettings Lerp(in ScreenComposerSettings a, in ScreenComposerSettings b, float t)
        {
            return new ScreenComposerSettings
            {
                ScreenPosition = Vector2.Lerp(a.ScreenPosition, b.ScreenPosition, t),
                DeadZone = new () 
                { 
                    Enabled = a.DeadZone.Enabled || b.DeadZone.Enabled,
                    Size = Vector2.Lerp(a.EffectiveDeadZoneSize, b.EffectiveDeadZoneSize, t) 
                },
                HardLimits = new ()
                { 
                    Enabled = a.HardLimits.Enabled || b.HardLimits.Enabled, 
                    Size = Vector2.Lerp(a.EffectiveHardLimitSize, b.EffectiveHardLimitSize, t),
                    Bias = Vector2.Lerp(a.HardLimits.Bias, b.HardLimits.Bias, t)
                }
            };
        }

        /// <summary>
        /// Tests whether 2 ScreenComposerSettings are approximately equal.
        /// </summary>
        /// <param name="a">First settings object</param>
        /// <param name="b">Second settings object</param>
        /// <returns>True if all fields are equal to within a small epsilon value</returns>
        public static bool Approximately(in ScreenComposerSettings a, in ScreenComposerSettings b)
        {
            return Mathf.Approximately(a.ScreenPosition.x, b.ScreenPosition.x)
                && Mathf.Approximately(a.ScreenPosition.y, b.ScreenPosition.y)
                && Mathf.Approximately(a.EffectiveDeadZoneSize.x, b.EffectiveDeadZoneSize.x)
                && Mathf.Approximately(a.EffectiveDeadZoneSize.y, b.EffectiveDeadZoneSize.y)
                && Mathf.Approximately(a.EffectiveHardLimitSize.x, b.EffectiveHardLimitSize.x)
                && Mathf.Approximately(a.EffectiveHardLimitSize.y, b.EffectiveHardLimitSize.y)
                && Mathf.Approximately(a.HardLimits.Bias.x, b.HardLimits.Bias.x)
                && Mathf.Approximately(a.HardLimits.Bias.y, b.HardLimits.Bias.y);
        }

        /// <summary>The default screen composition</summary>
        public static ScreenComposerSettings Default => new () 
        { 
            DeadZone = new () { Enabled = false, Size = new Vector2(0.2f, 0.2f) },
            HardLimits = new () { Enabled = false, Size = new Vector2(0.8f, 0.8f) }
        };
    }
}