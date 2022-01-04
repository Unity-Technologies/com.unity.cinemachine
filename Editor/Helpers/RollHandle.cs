#if CINEMACHINE_UNITY_SPLINES
using System.Collections.Generic;
using System.Linq;
using Unity.Mathematics;
using UnityEditor;
using UnityEditor.Splines;
using UnityEngine;
using UnityEngine.Splines;

namespace Cinemachine.Editor
{
    /// <summary>
    /// RollHandle drawer (roll is used by CinemachineSplineRollOverride). This draws the disc handles that allows the
    /// user to rotate the roll values in the scene view. It also draws up vectors along the spline indicating
    /// the roll at those points.
    /// </summary>
    [CustomSplineDataHandle(typeof(RollHandleAttribute))]
    class RollHandle : SplineDataHandle<float>
    {
        static float s_DisplaySpace = 0.5f;
        static List<Vector3> s_LineSegments = new List<Vector3>();
        
        /// <summary>
        /// Draws up vectors along the spline indicating roll.
        /// </summary>
        public override void DrawSplineData(
            SplineData<float> splineData,
            Spline spline,
            Matrix4x4 localToWorld,
            Color color)
        {
            s_LineSegments.Clear();
            using (var nativeSpline = new NativeSpline(spline))
            {
                if(GUIUtility.hotControl == 0 || controlIDs.Contains(GUIUtility.hotControl))
                {
                    var currentOffset = s_DisplaySpace;
                    while (currentOffset < nativeSpline.GetLength())
                    {
                        var t = currentOffset / nativeSpline.GetLength();
                        nativeSpline.Evaluate(t, out float3 position, out float3 direction, out float3 up);
                        var roll = splineData.Evaluate(nativeSpline, t, PathIndexUnit.Normalized,
                            new UnityEngine.Splines.Interpolators.LerpFloat());

                        Matrix4x4 localMatrix = Matrix4x4.identity;
                        var rollRotation = Quaternion.AngleAxis(-roll, direction);
                        var rolledUp = rollRotation * up;
                        localMatrix.SetTRS(position, Quaternion.LookRotation(direction, rolledUp), Vector3.one);
                        var currentPosition = localMatrix.GetPosition();
                        s_LineSegments.Add(localToWorld.MultiplyPoint(currentPosition));
                        s_LineSegments.Add(localToWorld.MultiplyPoint(localMatrix.MultiplyPoint(up)));
                        
                        currentOffset += s_DisplaySpace;
                    }
                }
                
                if(!(controlIDs.Contains(HandleUtility.nearestControl) || controlIDs.Contains(GUIUtility.hotControl)))
                    color.a = 0.33f;
                
                using(new Handles.DrawingScope(color))
                    Handles.DrawLines(s_LineSegments.ToArray());    
            }
        }

        // inverse pre-calculation optimization
        readonly Quaternion m_DefaultHandleOrientation = Quaternion.Euler(270, 0, 0);
        readonly Quaternion m_DefaultHandleOrientationInverse = Quaternion.Euler(90, 0, 0);
        /// <summary>
        /// Handles for roll control: rotatable disc and an arrow indicating the up vector.
        /// </summary>
        public override void DrawDataPoint(
            int controlID, 
            Vector3 position, 
            Vector3 direction,
            Vector3 upDirection,
            SplineData<float> splineData, 
            int dataPointIndex)
        {
            if (direction == Vector3.zero)
                return;

            Matrix4x4 localMatrix = Matrix4x4.identity;
            localMatrix.SetTRS(position, Quaternion.LookRotation(direction, upDirection), Vector3.one);
            var matrix = Handles.matrix * localMatrix;
            using (new Handles.DrawingScope(matrix)) // use draw matrix, so we work in local space
            {
                var rollData = splineData[dataPointIndex];
                var handleRotationLocalSpace = Quaternion.Euler(0, rollData.Value, 0);
                var handleRotationGlobalSpace = m_DefaultHandleOrientation * handleRotationLocalSpace;

                var color = Handles.color;
                if (!(controlIDs.Contains(HandleUtility.nearestControl) || controlIDs.Contains(GUIUtility.hotControl)))
                    color.a = 0.33f;

                using (new Handles.DrawingScope(color))
                    Handles.ArrowHandleCap(-1, Vector3.zero, handleRotationGlobalSpace, 1f, EventType.Repaint);
                
                var newHandleRotationGlobalSpace = Handles.Disc(controlID, handleRotationGlobalSpace, Vector3.zero, Vector3.forward, 1, false, 0);
                if (GUIUtility.hotControl == controlID)
                {
                    // Handles.Disc returns roll values in the [0, 360] range. Therefore, it works only in fixed ranges
                    // For example, within any of these ..., [-720, -360], [-360, 0], [0, 360], [360, 720], ...
                    // But we want to be able to rotate through these ranges, and not get stuck. We can detect when to
                    // move between ranges: when the roll delta is big. e.g. 359 -> 1 (358), instead of 1 -> 2 (1)
                    var newHandleRotationLocalSpace = m_DefaultHandleOrientationInverse * newHandleRotationGlobalSpace;
                    var deltaRoll = newHandleRotationLocalSpace.eulerAngles.y - handleRotationLocalSpace.eulerAngles.y;
                    if (deltaRoll > 180)
                    {
                        deltaRoll -= 360; // Roll down one range
                    }
                    else if (deltaRoll < -180)
                    {
                        deltaRoll += 360; // Roll up one range
                    }

                    rollData.Value += deltaRoll;
                    splineData[dataPointIndex] = rollData;
                }
            }
        }
    }
}
#endif
