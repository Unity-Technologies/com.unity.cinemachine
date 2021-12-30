using System.Collections.Generic;
using System.Linq;
using Unity.Mathematics;
using UnityEditor;
using UnityEditor.Splines;
using UnityEngine;
using UnityEngine.Splines;

using Interpolators = UnityEngine.Splines.Interpolators;

namespace Cinemachine.Editor
{
    /// <summary>
    /// Taken from the Spline package.
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
            using (var nativeSpline = new NativeSpline(spline, localToWorld))
            {
                if(GUIUtility.hotControl == 0 || controlIDs.Contains(GUIUtility.hotControl))
                {
                    var currentOffset = s_DisplaySpace;
                    while (currentOffset < nativeSpline.GetLength())
                    {
                        var t = currentOffset / nativeSpline.GetLength();
                        nativeSpline.Evaluate(t, out float3 position, out float3 direction, out float3 up);
                        var roll = splineData.Evaluate(nativeSpline, t, PathIndexUnit.Normalized,
                            new Interpolators.LerpFloat());

                        Matrix4x4 localMatrix = Matrix4x4.identity;
                        var rollRotation = Quaternion.AngleAxis(-roll, direction);
                        var rolledUp = rollRotation * up;
                        localMatrix.SetTRS(position, Quaternion.LookRotation(direction, rolledUp), Vector3.one);
                        var currentPosition = localMatrix.GetPosition();
                        s_LineSegments.Add(currentPosition);
                        s_LineSegments.Add(localMatrix.MultiplyPoint(up));
                        
                        currentOffset += s_DisplaySpace;
                    }
                }
                
                if(!(controlIDs.Contains(HandleUtility.nearestControl) || controlIDs.Contains(GUIUtility.hotControl)))
                    color.a = 0.33f;
                
                using(new Handles.DrawingScope(color))
                    Handles.DrawLines(s_LineSegments.ToArray());    
            }
        }

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
                    var newHandleRotationLocalSpace = m_DefaultHandleOrientationInverse * newHandleRotationGlobalSpace;
                    var deltaRoll = newHandleRotationLocalSpace.eulerAngles.y - handleRotationLocalSpace.eulerAngles.y;
                    rollData.Value += deltaRoll;
                    splineData[dataPointIndex] = rollData;
                }
            }
        }
    }
}
