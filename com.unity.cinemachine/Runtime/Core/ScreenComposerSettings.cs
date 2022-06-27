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
        + "tracked object here.  0 is screen center, and 1 is screen edge")]
        public Vector2 ScreenPosition;

        /// <summary>The camera will not adjust if the target is within this range of the screen position</summary>
        [Tooltip("The camera will not adjust if the target is within this range of the screen position")]
        public Vector2 DeadZoneSize;

        /// <summary>When the target is within this region, the camera will gradually adjust to re-align
        /// towards the desired position, depending on the damping speed</summary>
        [Tooltip("When the target is within this region, the camera will gradually adjust to re-align "
            + "towards the desired position, depending on the damping speed")]
        public Vector2 SoftZoneSize;
        
        /// <summary>A non-zero Bias will move the target position away from the center of the soft zone</summary>
        [Tooltip("A non-zero Bias will move the target position away from the center of the soft zone")]
        public Vector2 Bias;

        /// <summary>Clamps values to the expected ranges</summary>
        public void Validate()
        {
            ScreenPosition.x = Mathf.Clamp(ScreenPosition.x, -1.5f, 1.5f);
            ScreenPosition.y = Mathf.Clamp(ScreenPosition.y, -1.5f, 1.5f);
            DeadZoneSize.x = Mathf.Clamp(DeadZoneSize.x, 0f, 2f);
            DeadZoneSize.y = Mathf.Clamp(DeadZoneSize.y, 0f, 2f);
            SoftZoneSize.x = Mathf.Clamp(SoftZoneSize.x, 0f, 2f);
            SoftZoneSize.y = Mathf.Clamp(SoftZoneSize.y, 0f, 2f);
            Bias.x = Mathf.Clamp(Bias.x, -1f, 1f);
            Bias.y = Mathf.Clamp(Bias.y, -1f, 1f);
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
                DeadZoneSize = Vector2.Lerp(a.DeadZoneSize, b.DeadZoneSize, t),
                SoftZoneSize = Vector2.Lerp(a.SoftZoneSize, b.SoftZoneSize, t),
                Bias = Vector2.Lerp(a.Bias, b.Bias, t),
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
                && Mathf.Approximately(a.DeadZoneSize.x, b.DeadZoneSize.x)
                && Mathf.Approximately(a.DeadZoneSize.y, b.DeadZoneSize.y)
                && Mathf.Approximately(a.SoftZoneSize.x, b.SoftZoneSize.x)
                && Mathf.Approximately(a.SoftZoneSize.y, b.SoftZoneSize.y)
                && Mathf.Approximately(a.Bias.x, b.Bias.x)
                && Mathf.Approximately(a.Bias.y, b.Bias.y);
        }
    }
}