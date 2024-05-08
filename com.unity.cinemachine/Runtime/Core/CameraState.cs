using UnityEngine;
using System.Collections.Generic;
using System.Data.Common;

namespace Unity.Cinemachine
{
    /// <summary>
    /// The output of the Cinemachine engine for a specific virtual camera.  The information
    /// in this struct can be blended, and provides what is needed to calculate an
    /// appropriate camera position, orientation, and lens setting.
    /// 
    /// Raw values are what the Cinemachine behaviours generate.  The correction channel
    /// holds perturbations to the raw values - e.g. noise or smoothing, or obstacle
    /// avoidance corrections.  Corrections are not considered when making time-based
    /// calculations such as damping.
    /// 
    /// The Final position and orientation is the combination of the raw values and
    /// their corrections.
    /// </summary>
    public struct CameraState
    {
        /// <summary>
        /// Camera Lens Settings.
        /// </summary>
        public LensSettings Lens;

        /// <summary>
        /// Which way is up.  World space unit vector.  Must have a length of 1.
        /// </summary>
        public Vector3 ReferenceUp;

        /// <summary>
        /// The world space focus point of the camera.  What the camera wants to look at.
        /// There is a special constant define to represent "nothing".  Be careful to 
        /// check for that (or check the HasLookAt property).
        /// </summary>
        public Vector3 ReferenceLookAt;

        /// <summary>
        /// This constant represents "no point in space" or "no direction".
        /// </summary>
        public static Vector3 kNoPoint = new Vector3(float.NaN, float.NaN, float.NaN);

        /// <summary>
        /// Raw (un-corrected) world space position of this camera
        /// </summary>
        public Vector3 RawPosition;

        /// <summary>
        /// Raw (un-corrected) world space orientation of this camera
        /// </summary>
        public Quaternion RawOrientation;

        /// <summary>This is a way for the Body component to set a bypass hint for aim damping,
        /// useful for when the body needs to rotate its point of view, but does not
        /// want interference from the aim damping.  The value is the amount that the camera
        /// has been rotated, in world coords.</summary>
        public Quaternion RotationDampingBypass;

        /// <summary>
        /// Subjective estimation of how "good" the shot is.
        /// Larger values mean better quality.  Default is 1.
        /// </summary>
        public float ShotQuality;

        /// <summary>
        /// Position correction.  This will be added to the raw position.
        /// This value doesn't get fed back into the system when calculating the next frame.
        /// Can be noise, or smoothing, or both, or something else.
        /// </summary>
        public Vector3 PositionCorrection;

        /// <summary>
        /// Orientation correction.  This will be added to the raw orientation.
        /// This value doesn't get fed back into the system when calculating the next frame.
        /// Can be noise, or smoothing, or both, or something else.
        /// </summary>
        public Quaternion OrientationCorrection;

        /// <summary>
        /// Combine these hints to influence how blending is done, and how state is applied to the camera.
        /// </summary>
        public enum BlendHints
        {
            /// <summary>Normal state blending</summary>
            Nothing = 0,
            /// <summary>Spherical blend about the LookAt target (if any)</summary>
            SphericalPositionBlend = CinemachineCore.BlendHints.SphericalPosition,
            /// <summary>Cylindrical blend about the LookAt target (if any)</summary>
            CylindricalPositionBlend = CinemachineCore.BlendHints.CylindricalPosition,
            /// <summary>Radial blend when the LookAt target changes(if any)</summary>
            ScreenSpaceAimWhenTargetsDiffer = CinemachineCore.BlendHints.ScreenSpaceAimWhenTargetsDiffer,
            /// <summary>When this virtual camera goes Live, attempt to force the position to be the same 
            /// as the current position of the outgoing Camera</summary>
            InheritPosition = CinemachineCore.BlendHints.InheritPosition,
            /// <summary>Ignore the LookAt target and just slerp the orientation</summary>
            IgnoreLookAtTarget = CinemachineCore.BlendHints.IgnoreTarget,
            /// <summary>When blending out from this camera, use a snapshot of its outgoing state instead of a live state</summary>
            FreezeWhenBlendingOut = CinemachineCore.BlendHints.FreezeWhenBlendingOut,

            /// <summary>This state does not affect the camera position</summary>
            NoPosition = 1 << 16,
            /// <summary>This state does not affect the camera rotation</summary>
            NoOrientation = 2 << 16,
            /// <summary>Combination of NoPosition and NoOrientation</summary>
            NoTransform = NoPosition | NoOrientation,
            /// <summary>This state does not affect the lens</summary>
            NoLens = 4 << 16,
        }

        /// <summary>
        /// Combine these hints to influence how blending is done, and how state is applied to the camera.
        /// </summary>
        public BlendHints BlendHint;

        /// <summary>
        /// State with default values
        /// </summary>
        public static CameraState Default => new CameraState
        {
            Lens = LensSettings.Default,
            ReferenceUp = Vector3.up,
            ReferenceLookAt = kNoPoint,
            RawPosition = Vector3.zero,
            RawOrientation = Quaternion.identity,
            ShotQuality = 1,
            PositionCorrection = Vector3.zero,
            OrientationCorrection = Quaternion.identity,
            RotationDampingBypass = Quaternion.identity,
            BlendHint = BlendHints.Nothing
        };

        /// <summary>
        /// Custom Blendables are a way to attach opaque custom data to a CameraState and have 
        /// their weights blend along with the camera weights.  For efficiency, a fixed number of slots
        /// are provided, plus a (more expensive) overflow list.
        /// The base system manages but otherwise ignores this data - it is intended for 
        /// extension modules.
        /// </summary>
        public struct CustomBlendableItems
        {
            /// <summary>Opaque structure represent extra blendable stuff and its weight.
            /// The base system ignores this data - it is intended for extension modules</summary>
            public struct Item 
            { 
                /// <summary>The custom stuff that the extension module will consider</summary>
                public Object Custom; 
                /// <summary>The weight of the custom stuff.  Must be 0...1</summary>
                public float Weight; 
            };

            // This is to avoid excessive GC allocs
            internal Item m_Item0;
            internal Item m_Item1;
            internal Item m_Item2;
            internal Item m_Item3;

            internal List<Item> m_Overflow;

            /// <summary>The number of custom blendable items that will be applied to the camera.  
            /// The base system manages but otherwise ignores this data - it is intended for 
            /// extension modules</summary>
            internal int NumItems;
        }

        /// <summary>
        /// Custom Blendables are a way to attach opaque custom data to a CameraState and have 
        /// their weights blend along with the camera weights.  For efficiency, a fixed number of slots
        /// are provided, plus a (more expensive) overflow list.
        /// The base system manages but otherwise ignores this data - it is intended for 
        /// extension modules.
        /// </summary>
        internal CustomBlendableItems CustomBlendables;

        /// <summary>Add a custom blendable to the pot for eventual application to the camera.
        /// The base system manages but otherwise ignores this data - it is intended for 
        /// extension modules</summary>
        /// <param name="b">The custom blendable to add.  If b.m_Custom is the same as an 
        /// already-added custom blendable, then they will be merged and the weights combined.</param>
        public void AddCustomBlendable(CustomBlendableItems.Item b)
        {
            // Attempt to merge common blendables to avoid growth
            var index = this.FindCustomBlendable(b.Custom);
            if (index >= 0)
                b.Weight += this.GetCustomBlendable(index).Weight;
            else
                index = CustomBlendables.NumItems++;

            switch (index)
            {
                case 0: CustomBlendables.m_Item0 = b; break;
                case 1: CustomBlendables.m_Item1 = b; break;
                case 2: CustomBlendables.m_Item2 = b; break;
                case 3: CustomBlendables.m_Item3 = b; break;
                default: 
                {
                    index -= 4;
                    CustomBlendables.m_Overflow ??= new();
                    if (index < CustomBlendables.m_Overflow.Count)
                        CustomBlendables.m_Overflow[index] = b;
                    else
                        CustomBlendables.m_Overflow.Add(b);
                    break;
                }
            }
        }

        /// <summary>Intelligently blend the contents of two states.</summary>
        /// <param name="stateA">The first state, corresponding to t=0</param>
        /// <param name="stateB">The second state, corresponding to t=1</param>
        /// <param name="t">How much to interpolate.  Internally clamped to 0..1</param>
        /// <returns>Linearly interpolated CameraState</returns>
        public static CameraState Lerp(in CameraState stateA, in CameraState stateB, float t)
        {
            t = Mathf.Clamp01(t);
            float adjustedT = t;

            CameraState state = new ();

            // Combine the blend hints intelligently
            if (((stateA.BlendHint & stateB.BlendHint) & BlendHints.NoPosition) != 0)
                state.BlendHint |= BlendHints.NoPosition;
            if (((stateA.BlendHint & stateB.BlendHint) & BlendHints.NoOrientation) != 0)
                state.BlendHint |= BlendHints.NoOrientation;
            if (((stateA.BlendHint & stateB.BlendHint) & BlendHints.NoLens) != 0)
                state.BlendHint |= BlendHints.NoLens;
            if (((stateA.BlendHint | stateB.BlendHint) & BlendHints.SphericalPositionBlend) != 0)
                state.BlendHint |= BlendHints.SphericalPositionBlend;
            if (((stateA.BlendHint | stateB.BlendHint) & BlendHints.CylindricalPositionBlend) != 0)
                state.BlendHint |= BlendHints.CylindricalPositionBlend;
            if (((stateA.BlendHint | stateB.BlendHint) & BlendHints.FreezeWhenBlendingOut) != 0)
                state.BlendHint |= BlendHints.FreezeWhenBlendingOut;

            if (((stateA.BlendHint | stateB.BlendHint) & BlendHints.NoLens) == 0)
                state.Lens = LensSettings.Lerp(stateA.Lens, stateB.Lens, t);
            else if (((stateA.BlendHint & stateB.BlendHint) & BlendHints.NoLens) == 0)
            {
                if ((stateA.BlendHint & BlendHints.NoLens) != 0)
                    state.Lens = stateB.Lens;
                else
                    state.Lens = stateA.Lens;
            }
            state.ReferenceUp = Vector3.Slerp(stateA.ReferenceUp, stateB.ReferenceUp, t);
            state.ShotQuality = Mathf.Lerp(stateA.ShotQuality, stateB.ShotQuality, t);

            state.PositionCorrection = ApplyPosBlendHint(
                stateA.PositionCorrection, stateA.BlendHint,
                stateB.PositionCorrection, stateB.BlendHint, 
                state.PositionCorrection, 
                Vector3.Lerp(stateA.PositionCorrection, stateB.PositionCorrection, t));

            state.OrientationCorrection = ApplyRotBlendHint(
                stateA.OrientationCorrection, stateA.BlendHint,
                stateB.OrientationCorrection, stateB.BlendHint, 
                state.OrientationCorrection, 
                Quaternion.Slerp(stateA.OrientationCorrection, stateB.OrientationCorrection, t));

            // LookAt target
            if (!stateA.HasLookAt() || !stateB.HasLookAt())
                state.ReferenceLookAt = kNoPoint;
            else
            {
                // Re-interpolate FOV to preserve target composition, if possible
                float fovA = stateA.Lens.FieldOfView;
                float fovB = stateB.Lens.FieldOfView;
                if (((stateA.BlendHint | stateB.BlendHint) & BlendHints.NoLens) == 0
                    && !state.Lens.Orthographic && !Mathf.Approximately(fovA, fovB))
                {
                    LensSettings lens = state.Lens;
                    lens.FieldOfView = InterpolateFOV(
                            fovA, fovB,
                            Mathf.Max((stateA.ReferenceLookAt - stateA.GetCorrectedPosition()).magnitude, stateA.Lens.NearClipPlane),
                            Mathf.Max((stateB.ReferenceLookAt - stateB.GetCorrectedPosition()).magnitude, stateB.Lens.NearClipPlane), t);
                    state.Lens = lens;

                    // Make sure we preserve the screen composition through FOV changes
                    adjustedT = Mathf.Abs((lens.FieldOfView - fovA) / (fovB - fovA));
                }
                // Linear interpolation of lookAt target point
                state.ReferenceLookAt = Vector3.Lerp(stateA.ReferenceLookAt, stateB.ReferenceLookAt, adjustedT);
            }
            
            // Raw position
            state.RawPosition = ApplyPosBlendHint(
                stateA.RawPosition, stateA.BlendHint,
                stateB.RawPosition, stateB.BlendHint, 
                state.RawPosition, InterpolatePosition(
                    stateA.RawPosition, stateA.ReferenceLookAt,
                    stateB.RawPosition, stateB.ReferenceLookAt,
                    t, state.BlendHint, state.ReferenceUp));

            // Interpolate the LookAt in Screen Space if requested
            if (state.HasLookAt() 
                && ((stateA.BlendHint | stateB.BlendHint) & BlendHints.ScreenSpaceAimWhenTargetsDiffer) != 0)
            {
                state.ReferenceLookAt = state.RawPosition + Vector3.Slerp(
                        stateA.ReferenceLookAt - state.RawPosition, 
                        stateB.ReferenceLookAt - state.RawPosition, adjustedT);
            }

            // Clever orientation interpolation
            Quaternion newOrient = state.RawOrientation;
            if (((stateA.BlendHint | stateB.BlendHint) & BlendHints.NoOrientation) == 0)
            {
                Vector3 dirTarget = Vector3.zero;
                if (state.HasLookAt())//&& ((stateA.BlendHint | stateB.BlendHint) & BlendHints.ScreenSpaceAimWhenTargetsDiffer) == 0)
                {
                    // If orientations are different, use LookAt to blend them
                    float angle = Quaternion.Angle(stateA.RawOrientation, stateB.RawOrientation);
                    if (angle > UnityVectorExtensions.Epsilon)
                        dirTarget = state.ReferenceLookAt - state.GetCorrectedPosition();
                }
                
                if (dirTarget.AlmostZero() 
                    || ((stateA.BlendHint | stateB.BlendHint) & BlendHints.IgnoreLookAtTarget) != 0)
                {
                    // Don't know what we're looking at - can only slerp
                    newOrient = Quaternion.Slerp(stateA.RawOrientation, stateB.RawOrientation, t);
                }
                else
                {
                    // Rotate while preserving our lookAt target
                    var up = state.ReferenceUp;
                    dirTarget.Normalize();
                    if (Vector3.Cross(dirTarget, up).AlmostZero())
                    {
                        // Looking up or down at the pole
                        newOrient = Quaternion.Slerp(stateA.RawOrientation, stateB.RawOrientation, t);
                        up = newOrient * Vector3.up;
                    }

                    // Blend the desired offsets from center
                    newOrient = Quaternion.LookRotation(dirTarget, up);
                    var deltaA = -stateA.RawOrientation.GetCameraRotationToTarget(
                            stateA.ReferenceLookAt - stateA.GetCorrectedPosition(), up);
                    var deltaB = -stateB.RawOrientation.GetCameraRotationToTarget(
                            stateB.ReferenceLookAt - stateB.GetCorrectedPosition(), up);
                    newOrient = newOrient.ApplyCameraRotation(Vector2.Lerp(deltaA, deltaB, adjustedT), up);
                }
            }
            state.RawOrientation = ApplyRotBlendHint(
                stateA.RawOrientation, stateA.BlendHint,
                stateB.RawOrientation, stateB.BlendHint, 
                state.RawOrientation, newOrient);

            // Accumulate the custom blendables and apply the weights
            for (int i = 0; i < stateA.CustomBlendables.NumItems; ++i)
            {
                var b = stateA.GetCustomBlendable(i);
                b.Weight *= (1-t);
                if (b.Weight > 0)
                    state.AddCustomBlendable(b);
            }
            for (int i = 0; i < stateB.CustomBlendables.NumItems; ++i)
            {
                var b = stateB.GetCustomBlendable(i);
                b.Weight *= t;
                if (b.Weight > 0)
                    state.AddCustomBlendable(b);
            }
            return state;
        }

        static float InterpolateFOV(float fovA, float fovB, float dA, float dB, float t)
        {
            // We interpolate shot height
            float hA = dA * 2f * Mathf.Tan(fovA * Mathf.Deg2Rad / 2f);
            float hB = dB * 2f * Mathf.Tan(fovB * Mathf.Deg2Rad / 2f);
            float h = Mathf.Lerp(hA, hB, t);
            float fov = 179f;
            float d = Mathf.Lerp(dA, dB, t);
            if (d > UnityVectorExtensions.Epsilon)
                fov = 2f * Mathf.Atan(h / (2 * d)) * Mathf.Rad2Deg;
            return Mathf.Clamp(fov, Mathf.Min(fovA, fovB), Mathf.Max(fovA, fovB));
        }

        static Vector3 ApplyPosBlendHint(
            Vector3 posA, BlendHints hintA, 
            Vector3 posB, BlendHints hintB, 
            Vector3 original, Vector3 blended)
        {
            if (((hintA | hintB) & BlendHints.NoPosition) == 0)
                return blended;
            if (((hintA & hintB) & BlendHints.NoPosition) != 0)
                return original;
            if ((hintA & BlendHints.NoPosition) != 0)
                return posB;
            return posA;
        }

        static Quaternion ApplyRotBlendHint(
            Quaternion rotA, BlendHints hintA, 
            Quaternion rotB, BlendHints hintB, 
            Quaternion original, Quaternion blended)
        {
            if (((hintA | hintB) & BlendHints.NoOrientation) == 0)
                return blended;
            if (((hintA & hintB) & BlendHints.NoOrientation) != 0)
                return original;
            if ((hintA & BlendHints.NoOrientation) != 0)
                return rotB;
            return rotA;
        }

        static Vector3 InterpolatePosition(
            Vector3 posA, Vector3 pivotA,
            Vector3 posB, Vector3 pivotB,
            float t,
            BlendHints blendHint, Vector3 up)
        {
        #pragma warning disable 1718 // comparison made to same variable
            if (pivotA == pivotA && pivotB == pivotB) // check for NaN
        #pragma warning restore 1718
            {
                if ((blendHint & BlendHints.CylindricalPositionBlend) != 0)
                {
                    // Cylindrical interpolation about pivot
                    var a = Vector3.ProjectOnPlane(posA - pivotA, up);
                    var b = Vector3.ProjectOnPlane(posB - pivotB, up);
                    var c = Vector3.Slerp(a, b, t);
                    posA = (posA - a) + c;
                    posB = (posB - b) + c;
                }
                else if ((blendHint & BlendHints.SphericalPositionBlend) != 0)
                {
                    // Spherical interpolation about pivot
                    var c = Vector3.Slerp(posA - pivotA, posB - pivotB, t);
                    posA = pivotA + c;
                    posB = pivotB + c;
                }
            }
            return Vector3.Lerp(posA, posB, t);
        }
    }


    /// <summary>
    /// Extension methods for CameraState.
    /// </summary>
    public static class CameraStateExtensions
    {
        #pragma warning disable 1718 // comparison made to same variable
        /// <summary>Returns true if this state has a valid ReferenceLookAt value.</summary>
        /// <param name="s">State to check.</param>
        /// <returns>True, if state has a valid ReferenceLookAt value. False, otherwise.</returns>
        public static bool HasLookAt(this CameraState s) => s.ReferenceLookAt == s.ReferenceLookAt; // will be false if NaN
        #pragma warning restore 1718

        /// <summary>Position with correction applied.</summary>
        /// <param name="s">State to check.</param>
        /// <returns>Position with correction applied.</returns>
        public static Vector3 GetCorrectedPosition(this CameraState s) => s.RawPosition + s.PositionCorrection;

        /// <summary>Orientation with correction applied.</summary>
        /// <param name="s">State to check.</param>
        /// <returns>Orientation with correction applied.</returns>
        public static Quaternion GetCorrectedOrientation(this CameraState s) => s.RawOrientation * s.OrientationCorrection;

        /// <summary>Position with correction applied.  This is what the final camera gets.</summary>
        /// <param name="s">State to check.</param>
        /// <returns>Position with correction applied.</returns>
        public static Vector3 GetFinalPosition(this CameraState s) => s.RawPosition + s.PositionCorrection;

        /// <summary>Orientation with correction and dutch applied.  This is what the final camera gets.</summary>
        /// <param name="s">State to check</param>
        /// <returns>Orientation with correction and dutch applied.</returns>
        public static Quaternion GetFinalOrientation(this CameraState s)
        {
            if (Mathf.Abs(s.Lens.Dutch) > UnityVectorExtensions.Epsilon)
                return s.GetCorrectedOrientation() * Quaternion.AngleAxis(s.Lens.Dutch, Vector3.forward);
            return s.GetCorrectedOrientation();
        }

        /// <summary>Get the number of custom blendable items that have been added to this CameraState</summary>
        /// <param name="s">State to check.</param>
        /// <returns>The number of custom blendable items added.</returns>
        public static int GetNumCustomBlendables(this CameraState s) => s.CustomBlendables.NumItems;

        /// <summary>Get a custom blendable that will be applied to the camera.  
        /// The base system manages but otherwise ignores this data - it is intended for 
        /// extension modules</summary>
        /// <param name="s">State to check.</param>
        /// <param name="index">Which one to get.  Must be in range [0...NumCustomBlendables)</param>
        /// <returns>The custom blendable at the specified index.</returns>
        public static CameraState.CustomBlendableItems.Item GetCustomBlendable(this CameraState s, int index)
        {
            switch (index)
            {
                case 0: return s.CustomBlendables.m_Item0;
                case 1: return s.CustomBlendables.m_Item1;
                case 2: return s.CustomBlendables.m_Item2;
                case 3: return s.CustomBlendables.m_Item3;
                default: 
                {
                    index -= 4;
                    if (s.CustomBlendables.m_Overflow != null && index < s.CustomBlendables.m_Overflow.Count)
                        return s.CustomBlendables.m_Overflow[index];
                    return default;
                }
            }
        }

        
        /// <summary>Returns the index of the custom blendable that is associated with the input.</summary>
        /// <param name="s">State to check.</param>
        /// <param name="custom">The object with which the returned custom blendable index is associated.</param>
        /// <returns>The index of the custom blendable that is associated with the input.</returns>
        public static int FindCustomBlendable(this CameraState s, Object custom)
        {
            if (s.CustomBlendables.m_Item0.Custom == custom)
                return 0;
            if (s.CustomBlendables.m_Item1.Custom == custom)
                return 1;
            if (s.CustomBlendables.m_Item2.Custom == custom)
                return 2;
            if (s.CustomBlendables.m_Item3.Custom == custom)
                return 3;
            if (s.CustomBlendables.m_Overflow != null)
            {
                for (int i = 0; i < s.CustomBlendables.m_Overflow.Count; ++i)
                    if (s.CustomBlendables.m_Overflow[i].Custom == custom)
                        return i + 4;
            }
            return -1;
        }

        /// <summary>
        /// Checks whether the LookAt point falls within the camera's frustum
        /// </summary>
        /// <param name="state">Camera state to check</param>
        /// <returns>True if target is outside the camera frustum</returns>
        public static bool IsTargetOffscreen(this CameraState state)
        {
            if (state.HasLookAt())
            {
                var dir = state.ReferenceLookAt - state.GetCorrectedPosition();
                dir = Quaternion.Inverse(state.GetCorrectedOrientation()) * dir;
                if (state.Lens.Orthographic)
                {
                    if (Mathf.Abs(dir.y) > state.Lens.OrthographicSize)
                        return true;
                    if (Mathf.Abs(dir.x) > state.Lens.OrthographicSize * state.Lens.Aspect)
                        return true;
                }
                else
                {
                    var fov = state.Lens.FieldOfView / 2;
                    var angle = UnityVectorExtensions.Angle(dir.ProjectOntoPlane(Vector3.right), Vector3.forward);
                    if (angle > fov)
                        return true;

                    fov = Mathf.Rad2Deg * Mathf.Atan(Mathf.Tan(fov * Mathf.Deg2Rad) * state.Lens.Aspect);
                    angle = UnityVectorExtensions.Angle(dir.ProjectOntoPlane(Vector3.up), Vector3.forward);
                    if (angle > fov)
                        return true;
                }
            }
            return false;
        }
    }
}
