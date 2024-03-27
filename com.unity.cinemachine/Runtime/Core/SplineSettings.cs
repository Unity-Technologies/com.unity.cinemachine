using System;
using UnityEngine;
using UnityEngine.Splines;

namespace Unity.Cinemachine
{
    /// <summary>
    /// This structure holds the spline reference and the position and position units.
    /// </summary>
    [Serializable]
    public struct SplineSettings
    {
        /// <summary>The Spline container to which the the position will apply.</summary>
        [Tooltip("The Spline container to which the position will apply.")]
        public SplineContainer Spline;

        /// <summary>The position along the spline.  The actual value corresponding to a given point
        /// on the spline will depend on the unity type.</summary>
        [Tooltip("The position along the spline.  The actual value corresponding to a given point "
            + "on the spline will depend on the unity type.")]
        public float Position;
        
        /// <summary>How to interpret the Spline Position:
        /// - Distance: Values range from 0 (start of Spline) to Length of the Spline (end of Spline).
        /// - Normalized: Values range from 0 (start of Spline) to 1 (end of Spline).
        /// - Knot: Values are defined by knot indices and a fractional value representing the normalized
        /// interpolation between the specific knot index and the next knot."</summary>
        [Tooltip("How to interpret the Spline Position:\n"
            + "- <b>Distance</b>: Values range from 0 (start of Spline) to Length of the Spline (end of Spline).\n"
            + "- <b>Normalized</b>: Values range from 0 (start of Spline) to 1 (end of Spline).\n"
            + "- <b>Knot</b>: Values are defined by knot indices and a fractional value representing the normalized " 
            + "interpolation between the specific knot index and the next knot.\n")]
        public PathIndexUnit Units;

        /// <summary>
        /// Change the units of the position, preserving the position on the spline.  
        /// The value of Position may change in order to preserve the position on the spline.
        /// </summary>
        /// <param name="newUnits">The new units to use</param>
        public void ChangeUnitPreservePosition(PathIndexUnit newUnits)
        {
            if (Spline.IsValid() && newUnits != Units)
                Position = Spline.Spline.ConvertIndexUnit(Position, Units, newUnits);
            Units = newUnits;
        }
    }
}
