using UnityEngine;

namespace Cinemachine.Utility
{
    /// <summary>Extensions to the Vector3 class, used by Cinemachine</summary>
    public static class UnityVectorExtensions
    {
        /// <summary>A useful Epsilon</summary>
        public const float Epsilon = 0.0001f;

        /// <summary>
        /// Checks if the Vector2 contains NaN for x or y.
        /// </summary>
        /// <param name="v">Vector2 to check for NaN</param>
        /// <returns>True, if any components of the vector are NaN</returns>
        public static bool IsNaN(this Vector2 v)
        {
            return float.IsNaN(v.x) || float.IsNaN(v.y);
        }
        
        /// <summary>
        /// Checks if the Vector2 contains NaN for x or y.
        /// </summary>
        /// <param name="v">Vector2 to check for NaN</param>
        /// <returns>True, if any components of the vector are NaN</returns>
        public static bool IsNaN(this Vector3 v)
        {
            return float.IsNaN(v.x) || float.IsNaN(v.y) || float.IsNaN(v.z);
        }
        

        /// <summary>
        /// Get the closest point on a line segment.
        /// </summary>
        /// <param name="p">A point in space</param>
        /// <param name="s0">Start of line segment</param>
        /// <param name="s1">End of line segment</param>
        /// <returns>The interpolation parameter representing the point on the segment, with 0==s0, and 1==s1</returns>
        public static float ClosestPointOnSegment(this Vector3 p, Vector3 s0, Vector3 s1)
        {
            Vector3 s = s1 - s0;
            float len2 = Vector3.SqrMagnitude(s);
            if (len2 < Epsilon)
                return 0; // degenrate segment
            return Mathf.Clamp01(Vector3.Dot(p - s0, s) / len2);
        }

        /// <summary>
        /// Get the closest point on a line segment.
        /// </summary>
        /// <param name="p">A point in space</param>
        /// <param name="s0">Start of line segment</param>
        /// <param name="s1">End of line segment</param>
        /// <returns>The interpolation parameter representing the point on the segment, with 0==s0, and 1==s1</returns>
        public static float ClosestPointOnSegment(this Vector2 p, Vector2 s0, Vector2 s1)
        {
            Vector2 s = s1 - s0;
            float len2 = Vector2.SqrMagnitude(s);
            if (len2 < Epsilon)
                return 0; // degenrate segment
            return Mathf.Clamp01(Vector2.Dot(p - s0, s) / len2);
        }

        /// <summary>
        /// Returns a non-normalized projection of the supplied vector onto a plane
        /// as described by its normal
        /// </summary>
        /// <param name="vector"></param>
        /// <param name="planeNormal">The normal that defines the plane.  Must have a length of 1.</param>
        /// <returns>The component of the vector that lies in the plane</returns>
        public static Vector3 ProjectOntoPlane(this Vector3 vector, Vector3 planeNormal)
        {
            return (vector - Vector3.Dot(vector, planeNormal) * planeNormal);
        }
        
        /// <summary>
        /// Calculates the intersection point defined by line_1 [p1, p2], and line_2 [p3, p4).
        /// Meaning of brackets:
        /// <list type="bullet">
        /// <item><description> If line_1 intersects line_2 at exactly p4, then segments do not intersect, but lines do.</description></item>
        /// <item><description> If line_1 intersects line_2 at exactly p3, then segments intersect.</description></item>
        /// </list>
        /// <para>Returns intersection type (0 = no intersection, 1 = lines intersect, 2 = segments intersect).</para>
        /// </summary>
        /// <param name="p1">line_1 is defined by (p1, p2)</param>
        /// <param name="p2">line_1 is defined by (p1, p2)</param>
        /// <param name="p3">line_2 is defined by (p3, p4)</param>
        /// <param name="p4">line_2 is defined by (p3, p4)</param>
        /// <param name="intersection">If lines intersect, then this will hold the intersection point. Otherwise, it will be Vector2.positiveInfinity.</param>
        /// <returns>0 = no intersection, 1 = lines intersect, 2 = segments intersect.</returns>
        public static int FindIntersection(in Vector2 p1, in Vector2 p2, in Vector2 p3, in Vector2 p4, 
            out Vector2 intersection)
        {
            // Get the segments' parameters.
            float dx12 = p2.x - p1.x;
            float dy12 = p2.y - p1.y;
            float dx34 = p4.x - p3.x;
            float dy34 = p4.y - p3.y;

            // Solve for t1 and t2
            float denominator = (dy12 * dx34 - dx12 * dy34);

            float t1 =
                ((p1.x - p3.x) * dy34 + (p3.y - p1.y) * dx34)
                / denominator;
            if (float.IsInfinity(t1) || float.IsNaN(t1))
            {
                // The lines are parallel (or close enough to it).
                intersection = Vector2.positiveInfinity;
                if ((p1 - p3).sqrMagnitude < 0.01f || (p1 - p4).sqrMagnitude < 0.01f ||
                    (p2 - p3).sqrMagnitude < 0.01f || (p2 - p4).sqrMagnitude < 0.01f)
                {
                    return 2; // they are the same line, or very close parallels
                }
                return 0; // no intersection
            }
            
            // Find the point of intersection.
            intersection = new Vector2(p1.x + dx12 * t1, p1.y + dy12 * t1);
            
            float t2 = ((p3.x - p1.x) * dy12 + (p1.y - p3.y) * dx12) / -denominator;
            return (t1 >= 0 && t1 <= 1 && t2 >= 0 && t2 < 1) ? 2 : 1; // 2 = segments intersect, 1 = lines intersect
        }
        
        /// <summary>
        /// Component-wise absolute value
        /// </summary>
        /// <param name="v">Input vector</param>
        /// <returns>Component-wise absolute value of the input vector</returns>
        public static Vector2 Abs(this Vector2 v)
        {
            return new Vector2(Mathf.Abs(v.x), Mathf.Abs(v.y));
        }

        /// <summary>
        /// Component-wise absolute value
        /// </summary>
        /// <param name="v">Input vector</param>
        /// <returns>Component-wise absolute value of the input vector</returns>
        public static Vector3 Abs(this Vector3 v)
        {
            return new Vector3(Mathf.Abs(v.x), Mathf.Abs(v.y), Mathf.Abs(v.z));
        }

        /// <summary>Is the vector within Epsilon of zero length?</summary>
        /// <param name="v"></param>
        /// <returns>True if the square magnitude of the vector is within Epsilon of zero</returns>
        public static bool AlmostZero(this Vector3 v)
        {
            return v.sqrMagnitude < (Epsilon * Epsilon);
        }

        /// <summary>Much more stable for small angles than Unity's native implementation</summary>
        /// <param name="v1">The first vector</param>
        /// <param name="v2">The second vector</param>
        /// <returns>Angle between the vectors, in degrees</returns>
        public static float Angle(Vector3 v1, Vector3 v2)
        {
#if false // Maybe this version is better?  to test....
            float a = v1.magnitude;
            v1 *= v2.magnitude;
            v2 *= a;
            return Mathf.Atan2((v1 - v2).magnitude, (v1 + v2).magnitude) * Mathf.Rad2Deg * 2;
#else            
            v1.Normalize();
            v2.Normalize();
            return Mathf.Atan2((v1 - v2).magnitude, (v1 + v2).magnitude) * Mathf.Rad2Deg * 2;
#endif
        }

        /// <summary>Much more stable for small angles than Unity's native implementation</summary>
        /// <param name="v1">The first vector</param>
        /// <param name="v2">The second vector</param>
        /// <param name="up">Definition of up (used to determine the sign)</param>
        /// <returns>Signed angle between the vectors, in degrees</returns>
        public static float SignedAngle(Vector3 v1, Vector3 v2, Vector3 up)
        {
            float angle = Angle(v1, v2);
            if (Mathf.Sign(Vector3.Dot(up, Vector3.Cross(v1, v2))) < 0)
                return -angle;
            return angle;
        }

        /// <summary>Much more stable for small angles than Unity's native implementation</summary>
        /// <param name="v1">The first vector</param>
        /// <param name="v2">The second vector</param>
        /// <param name="up">Definition of up (used to determine the sign)</param>
        /// <returns>Rotation between the vectors</returns>
        public static Quaternion SafeFromToRotation(Vector3 v1, Vector3 v2, Vector3 up)
        {
            var axis = Vector3.Cross(v1, v2);
            if (axis.AlmostZero())
                axis = up; // in case they are pointing in opposite directions
            return Quaternion.AngleAxis(Angle(v1, v2), axis);
        }

        /// <summary>This is a slerp that mimics a camera operator's movement in that
        /// it chooses a path that avoids the lower hemisphere, as defined by
        /// the up param</summary>
        /// <param name="vA">First direction</param>
        /// <param name="vB">Second direction</param>
        /// <param name="t">Interpolation amoun t</param>
        /// <param name="up">Defines the up direction</param>
        /// <returns>Interpolated vector</returns>
        public static Vector3 SlerpWithReferenceUp(
            Vector3 vA, Vector3 vB, float t, Vector3 up)
        {
            float dA = vA.magnitude;
            float dB = vB.magnitude;
            if (dA < Epsilon || dB < Epsilon)
                return Vector3.Lerp(vA, vB, t);

            Vector3 dirA = vA / dA;
            Vector3 dirB = vB / dB;
            Quaternion qA = Quaternion.LookRotation(dirA, up);
            Quaternion qB = Quaternion.LookRotation(dirB, up);
            Quaternion q = UnityQuaternionExtensions.SlerpWithReferenceUp(qA, qB, t, up);
            Vector3 dir = q * Vector3.forward;
            return dir * Mathf.Lerp(dA, dB, t);
        }
    }

    /// <summary>Extensions to the Quaternion class, usen in various places by Cinemachine</summary>
    public static class UnityQuaternionExtensions
    {
        /// <summary>This is a slerp that mimics a camera operator's movement in that
        /// it chooses a path that avoids the lower hemisphere, as defined by
        /// the up param</summary>
        /// <param name="qA">First direction</param>
        /// <param name="qB">Second direction</param>
        /// <param name="t">Interpolation amoun t</param>
        /// <param name="up">Defines the up direction.  Must have a length of 1.</param>
        /// <returns>Interpolated quaternion</returns>
        public static Quaternion SlerpWithReferenceUp(
            Quaternion qA, Quaternion qB, float t, Vector3 up)
        {
            Vector3 dirA = (qA * Vector3.forward).ProjectOntoPlane(up);
            Vector3 dirB = (qB * Vector3.forward).ProjectOntoPlane(up);
            if (dirA.AlmostZero() || dirB.AlmostZero())
                return Quaternion.Slerp(qA, qB, t);

            // Work on the plane, in eulers
            Quaternion qBase = Quaternion.LookRotation(dirA, up);
            Quaternion qA1 = Quaternion.Inverse(qBase) * qA;
            Quaternion qB1 = Quaternion.Inverse(qBase) * qB;
            Vector3 eA = qA1.eulerAngles;
            Vector3 eB = qB1.eulerAngles;
            return qBase * Quaternion.Euler(
                Mathf.LerpAngle(eA.x, eB.x, t),
                Mathf.LerpAngle(eA.y, eB.y, t),
                Mathf.LerpAngle(eA.z, eB.z, t));
        }

        /// <summary>Normalize a quaternion</summary>
        /// <param name="q"></param>
        /// <returns>The normalized quaternion.  Unit length is 1.</returns>
        public static Quaternion Normalized(this Quaternion q)
        {
            Vector4 v = new Vector4(q.x, q.y, q.z, q.w).normalized;
            return new Quaternion(v.x, v.y, v.z, v.w);
        }

        /// <summary>
        /// Get the rotations, first about world up, then about (travelling) local right,
        /// necessary to align the quaternion's forward with the target direction.
        /// This represents the tripod head movement needed to look at the target.
        /// This formulation makes it easy to interpolate without introducing spurious roll.
        /// </summary>
        /// <param name="orient"></param>
        /// <param name="lookAtDir">The worldspace target direction in which we want to look</param>
        /// <param name="worldUp">Which way is up.  Must have a length of 1.</param>
        /// <returns>Vector2.y is rotation about worldUp, and Vector2.x is second rotation,
        /// about local right.</returns>
        public static Vector2 GetCameraRotationToTarget(
            this Quaternion orient, Vector3 lookAtDir, Vector3 worldUp)
        {
            if (lookAtDir.AlmostZero())
                return Vector2.zero;  // degenerate

            // Work in local space
            Quaternion toLocal = Quaternion.Inverse(orient);
            Vector3 up = toLocal * worldUp;
            lookAtDir = toLocal * lookAtDir;

            // Align yaw based on world up
            float angleH = 0;
            {
                Vector3 targetDirH = lookAtDir.ProjectOntoPlane(up);
                if (!targetDirH.AlmostZero())
                {
                    Vector3 currentDirH = Vector3.forward.ProjectOntoPlane(up);
                    if (currentDirH.AlmostZero())
                    {
                        // We're looking at the north or south pole
                        if (Vector3.Dot(currentDirH, up) > 0)
                            currentDirH = Vector3.down.ProjectOntoPlane(up);
                        else
                            currentDirH = Vector3.up.ProjectOntoPlane(up);
                    }
                    angleH = UnityVectorExtensions.SignedAngle(currentDirH, targetDirH, up);
                }
            }
            Quaternion q = Quaternion.AngleAxis(angleH, up);

            // Get local vertical angle
            float angleV = UnityVectorExtensions.SignedAngle(
                q * Vector3.forward, lookAtDir, q * Vector3.right);

            return new Vector2(angleV, angleH);
        }

        /// <summary>
        /// Apply rotations, first about world up, then about (travelling) local right.
        /// rot.y is rotation about worldUp, and rot.x is second rotation, about local right.
        /// </summary>
        /// <param name="orient"></param>
        /// <param name="rot">Vector2.y is rotation about worldUp, and Vector2.x is second rotation,
        /// about local right.</param>
        /// <param name="worldUp">Which way is up</param>
        /// <returns>Result rotation after the input is applied to the input quaternion</returns>
        public static Quaternion ApplyCameraRotation(
            this Quaternion orient, Vector2 rot, Vector3 worldUp)
        {
            Quaternion q = Quaternion.AngleAxis(rot.x, Vector3.right);
            return (Quaternion.AngleAxis(rot.y, worldUp) * orient) * q;
        }
    }

    /// <summary>Ad-hoc xxtentions to the Rect structure, used by Cinemachine</summary>
    public static class UnityRectExtensions
    {
        /// <summary>Inflate a rect</summary>
        /// <param name="r"></param>
        /// <param name="delta">x and y are added/subtracted fto/from the edges of
        /// the rect, inflating it in all directions</param>
        /// <returns>The inflated rect</returns>
        public static Rect Inflated(this Rect r, Vector2 delta)
        {
            return new Rect(
                r.xMin - delta.x, r.yMin - delta.y,
                r.width + delta.x * 2, r.height + delta.y * 2);
        }
    }
}
