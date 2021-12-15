using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using Unity.Mathematics;
using UnityEditor;
using UnityEditor.Splines;
using UnityEngine;
using UnityEngine.Splines;

using Interpolators = UnityEngine.Splines.Interpolators;

namespace Cinemachine.Samples
{
    /// <summary>
    /// Taken from the Spline package.
    /// </summary>
    [CustomSplineDataHandle(typeof(TiltHandleAttribute))]
    class TiltHandle : SplineDataHandle<float3>
    {
        static float s_DisplaySpace = 0.5f;

        static Quaternion s_StartingRotation;

        static List<Vector3> s_LineSegments = new List<Vector3>();
        
        public override void DrawSplineData(
            SplineData<float3> splineData,
            Spline spline,
            Matrix4x4 localToWorld,
            Color color)
        {
            s_LineSegments.Clear();
            using(var nativeSpline = new NativeSpline(spline, localToWorld, Allocator.Temp))
            {
                if(GUIUtility.hotControl == 0 || controlIDs.Contains(GUIUtility.hotControl))
                {
                    var currentOffset = s_DisplaySpace;
                    while(currentOffset < nativeSpline.GetLength())
                    {
                        var t = currentOffset / nativeSpline.GetLength();
                        nativeSpline.Evaluate(t, out float3 position, out float3 direction, out float3 up);
                        var data = splineData.Evaluate(nativeSpline, t, PathIndexUnit.Normalized,
                            new Interpolators.LerpFloat3());

                        Matrix4x4 localMatrix = Matrix4x4.identity;
                        localMatrix.SetTRS(position, Quaternion.LookRotation(direction, up), Vector3.one);
                        s_LineSegments.Add(localMatrix.GetPosition());
                        s_LineSegments.Add(localMatrix.MultiplyPoint(math.normalize(data)));
                        
                        currentOffset += s_DisplaySpace;
                    }
                }
                
                if(!(controlIDs.Contains(HandleUtility.nearestControl) || controlIDs.Contains(GUIUtility.hotControl)))
                    color.a = 0.33f;
                
                using(new Handles.DrawingScope(color))
                    Handles.DrawLines(s_LineSegments.ToArray());    
            }
        }

        public override void DrawDataPoint(
            int controlID, 
            Vector3 position, 
            Vector3 direction,
            Vector3 upDirection,
            SplineData<float3> splineData, 
            int dataPointIndex)
        {
            if(direction == Vector3.zero)
                return;
            
            var dataPoint = splineData[dataPointIndex];
            
            Matrix4x4 localMatrix = Matrix4x4.identity;
            localMatrix.SetTRS(position, Quaternion.LookRotation(direction, upDirection), Vector3.one);

            var matrix = Handles.matrix * localMatrix;
            using(new Handles.DrawingScope(matrix))
            {
                var dataPointRotation = Quaternion.FromToRotation(Vector3.up, dataPoint.Value);

                if(GUIUtility.hotControl == 0)
                    s_StartingRotation = dataPointRotation;

                var color = Handles.color;
                if(!(controlIDs.Contains(HandleUtility.nearestControl) || controlIDs.Contains(GUIUtility.hotControl)))
                    color.a = 0.33f;

                using(new Handles.DrawingScope(color))
                    Handles.ArrowHandleCap(-1, Vector3.zero, Quaternion.FromToRotation(Vector3.forward, dataPoint.Value), 1f, EventType.Repaint);
                
                var rotation = Handles.Disc(controlID, dataPointRotation, Vector3.zero, Vector3.forward, 1, false, 0);
                
                 if(GUIUtility.hotControl == controlID)
                 {
                      var deltaRot = Quaternion.Inverse(s_StartingRotation) * rotation;
                      dataPoint.Value = deltaRot * s_StartingRotation * Vector3.up;
                      splineData[dataPointIndex] = dataPoint;
                 }
            }
        }
    }
}
