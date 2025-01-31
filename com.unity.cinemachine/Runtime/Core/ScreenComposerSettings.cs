using UnityEngine;
using System;

namespace Unity.Cinemachine
{
    /// <summary>This structure holds settings for screen-space composition.</summary>
    [Serializable]
    public struct ScreenComposerSettings
    {
        /// <summary>Screen position for target. The camera will adjust to position the
        /// tracked object here.  0 is screen center, and +0.5 or -0.5 is screen edge</summary>
        [Tooltip("Screen position for target. The camera will adjust to position the "
        + "tracked object here.  0 is screen center, and +0.5 or -0.5 is screen edge")]
        [DelayedVector]
        public Vector2 ScreenPosition;

        /// <summary>Settings for DeadZone, which is an area within which the camera will not adjust itself.</summary>
        [Serializable]
        public struct DeadZoneSettings
        {
            /// <summary>Enables the Dead Zone settings</summary>
            public bool Enabled;
            /// <summary>The camera will not adjust if the target is within this range of the screen position.
            /// Full screen size is 1.</summary>
            [Tooltip("The camera will not adjust if the target is within this range of the "
                + "screen position.  Full screen size is 1.")]
            [DelayedVector]
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
            /// Full screen size is 1</summary>
            [Tooltip("The target will not be allowed to be outside this region. "
                + "When the target is within this region, the camera will gradually adjust to re-align "
                + "towards the desired position, depending on the damping speed.  "
                + "Full screen size is 1")]
            [DelayedVector]
            public Vector2 Size;
            /// <summary>A zero Offset means that the hard limits will be centered around the target screen position.
            /// A nonzero Offset will uncenter the hard limits relative to the target screen position.
            /// </summary>
            [Tooltip("A zero Offset means that the hard limits will be centered around the target screen position.  "
                + "A nonzero Offset will uncenter the hard limits relative to the target screen position.")]
            [DelayedVector]
            public Vector2 Offset;
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
            HardLimits.Size = new Vector2(
                Mathf.Clamp(HardLimits.Size.x, DeadZone.Size.x, 3),
                Mathf.Clamp(HardLimits.Size.y, DeadZone.Size.y, 3));
            HardLimits.Offset.x = Mathf.Clamp(HardLimits.Offset.x, -1f, 1f);
            HardLimits.Offset.y = Mathf.Clamp(HardLimits.Offset.y, -1f, 1f);
        }

        /// <summary>Get the effective dead zone size, taking enabled state into account</summary>
        public Vector2 EffectiveDeadZoneSize => DeadZone.Enabled ? DeadZone.Size : Vector2.zero;

        /// <summary>Get the effective hard limits size, taking enabled state into account</summary>
        public Vector2 EffectiveHardLimitSize => HardLimits.Enabled ? HardLimits.Size : new Vector2(3, 3);

        /// <summary>Get/set the screenspace rect for the dead zone region.  This also defines screen position</summary>
        public Rect DeadZoneRect
        {
            get
            {
                var deadZoneSize = EffectiveDeadZoneSize;
                return new Rect(ScreenPosition - deadZoneSize * 0.5f + new Vector2(0.5f, 0.5f), deadZoneSize);
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
                    Mathf.Clamp(value.x - 0.5f + deadZoneSize.x * 0.5f, -1.5f,  1.5f),
                    Mathf.Clamp(value.y - 0.5f + deadZoneSize.y * 0.5f, -1.5f,  1.5f));
                HardLimits.Size = new Vector2(
                    Mathf.Clamp(HardLimits.Size.x, deadZoneSize.x, 3),
                    Mathf.Clamp(HardLimits.Size.y, deadZoneSize.y, 3));
            }
        }

        /// <summary>Get/set the screenspace rect for the hard limits.</summary>
        public Rect HardLimitsRect
        {
            get
            {
                if (!HardLimits.Enabled)
                    return new Rect(-EffectiveHardLimitSize * 0.5f, EffectiveHardLimitSize);
                var r = new Rect(ScreenPosition - HardLimits.Size * 0.5f + new Vector2(0.5f, 0.5f), HardLimits.Size);
                var deadZoneSize = EffectiveDeadZoneSize;
                r.position += new Vector2(
                    HardLimits.Offset.x * 0.5f * (HardLimits.Size.x - deadZoneSize.x),
                    HardLimits.Offset.y * 0.5f * (HardLimits.Size.y - deadZoneSize.y));
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
        /// <returns>The interpolated value.</returns>
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
                    Offset = Vector2.Lerp(a.HardLimits.Offset, b.HardLimits.Offset, t)
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
                && Mathf.Approximately(a.HardLimits.Offset.x, b.HardLimits.Offset.x)
                && Mathf.Approximately(a.HardLimits.Offset.y, b.HardLimits.Offset.y);
        }

        /// <summary>The default screen composition</summary>
        public static ScreenComposerSettings Default => new ()
        {
            DeadZone = new () { Enabled = false, Size = new Vector2(0.2f, 0.2f) },
            HardLimits = new () { Enabled = false, Size = new Vector2(0.8f, 0.8f) }
        };
    }
}