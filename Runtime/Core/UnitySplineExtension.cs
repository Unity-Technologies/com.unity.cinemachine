using UnityEngine;
using UnityEngine.Splines;
using Unity.Mathematics;

namespace Cinemachine.Utility
{
    /// <summary>Extensions to the Unity.Spline class, used by Cinemachine</summary>
    public static class UnitySplineExtension
    {
        /// <summary>How to interpret the Path Position</summary>
        public enum PositionUnits
        {
            /// <summary>Use PathPosition units, where 0 is first waypoint, 1 is second waypoint, etc</summary>
            PathUnits,
            /// <summary>Use Distance Along Path.  Path will be sampled according to its Resolution
            /// setting, and a distance lookup table will be cached internally</summary>
            Distance,
            /// <summary>Normalized units, where 0 is the start of the path, and 1 is the end.
            /// Path will be sampled according to its Resolution
            /// setting, and a distance lookup table will be cached internally</summary>
            Normalized
        }

        /// <summary>The maximum value for the path position</summary>
        /// <param name="path"></param>
        private static float MaxPos(this SplineContainer path)
        {
            int count = path.Spline.KnotCount - 1;
            if (count < 1)
                return 0;
            return path.Spline.Closed ? count + 1 : count;
        }

        /// <summary>Standardize the unit, so that it lies between MinUmit and MaxUnit</summary>
        /// <param name="path"></param>
        /// <param name="pos">The value to be standardized</param>
        /// <param name="units">The unit type</param>
        /// <returns>The standardized value of pos, between MinUnit and MaxUnit</returns>
        public static float StandardizeUnit(this SplineContainer path, float pos, PositionUnits units)
        {
            if (units == PositionUnits.PathUnits)
            {
                return StandardizePos(path, pos);
            }

            float len = path.CalculateLength();
            if (units == PositionUnits.Distance)
            {
                return StandardizePathDistance(path, pos, len);
            } else
            {
                return StandardizePathDistance(path, pos * len, len) / len;
            }               
        }

        /// <summary>Get a standardized path position, taking spins into account if looped</summary>
        /// <param name="path"></param>
        /// <param name="pos">Position along the path</param>
        /// <returns>Standardized position, between MinPos and MaxPos</returns>
        private static float StandardizePos(this SplineContainer path, float pos)
        {
            float maxPos = path.MaxPos();
            if (path.Spline.Closed && maxPos > 0)
            {
                pos = pos % maxPos;
                if (pos < 0)
                    pos += maxPos;
                return pos;
            }
            return Mathf.Clamp(pos, 0, maxPos);
        }

        /// <summary>Standardize a distance along the path based on the path length.
        /// If the distance cache is not valid, then calling this will
        /// trigger a potentially costly regeneration of the path distance cache</summary>
        /// <param name="path"></param>
        /// <param name="distance">The distance to standardize</param>
        /// <param name="length">path length</param>
        /// <returns>The standardized distance, ranging from 0 to path length</returns>
        private static float StandardizePathDistance(this SplineContainer path, float distance, float length)
        {
            if (length < UnityVectorExtensions.Epsilon)
                return 0;
            if (path.Spline.Closed)
            {
                distance = distance % length;
                if (distance < 0)
                    distance += length;
            }
            return Mathf.Clamp(distance, 0, length);
        }

        /// <summary>Get a worldspace position of a point along the path</summary>
        /// <param name="path"></param>
        /// <param name="pos">Postion along the path.  Need not be normalized.</param>
        /// <param name="units">The unit to use when interpreting the value of pos.</param>
        /// <returns>World-space position of the point along at path at pos</returns>
        public static float3 EvaluatePositionAtUnit(this SplineContainer path, ref float pos, PositionUnits units)
        {
            if (path.Spline.KnotCount == 0)
            {
                return float3.zero;
            }

            if (units == PositionUnits.PathUnits)
            {
                pos = path.StandardizePos(pos);
                return path.EvaluateCurvePosition(Mathf.FloorToInt(pos), pos - Mathf.FloorToInt(pos));
            }

            float len = path.CalculateLength();
            if (units == PositionUnits.Distance)
            {
                pos = path.StandardizePathDistance(pos, len);
                return path.EvaluatePosition(pos / len);
            } else
            {
                pos = path.StandardizePathDistance(pos * len, len) / len;
                return path.EvaluatePosition(pos);
            }
        }

    }

    
}
