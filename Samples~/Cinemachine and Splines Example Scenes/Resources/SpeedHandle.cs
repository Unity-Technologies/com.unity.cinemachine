using System.Collections;
using System.Collections.Generic;
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
    [CustomSplineDataHandle(typeof(SpeedHandleAttribute))]
    class SpeedHandle : SplineDataHandle<float>
    {
        const float k_HandleSize = 0.15f; 
        const float k_SpeedScaleFactor = 10f;
        static float s_DisplaySpace = 0.2f;
        static List<Vector3> s_LineSegments = new List<Vector3>();

        public override void DrawSplineData(
             SplineData<float> splineData,
             Spline spline,
             Matrix4x4 localToWorld,
             Color color)
        {
             s_LineSegments.Clear();
             if(GUIUtility.hotControl == 0 || ( (IList)controlIDs ).Contains(GUIUtility.hotControl))
             {
                 var data = splineData.Evaluate(spline, 0, PathIndexUnit.Distance, new Interpolators.LerpFloat());
                 var position = spline.EvaluatePosition(0);
                 var previousExtremity = (Vector3)position + ( data / k_SpeedScaleFactor ) * Vector3.up;

                 var currentOffset = s_DisplaySpace;
                 while(currentOffset < spline.GetLength())
                 {
                     var t = currentOffset / spline.GetLength();
                     position = spline.EvaluatePosition(t);
                     data = splineData.Evaluate(spline, currentOffset, PathIndexUnit.Distance, new Interpolators.LerpFloat());

                     var extremity = (Vector3)position + ( data / k_SpeedScaleFactor ) * Vector3.up;

                     s_LineSegments.Add(previousExtremity);
                     s_LineSegments.Add(extremity);

                     currentOffset += s_DisplaySpace;
                     previousExtremity = extremity;
                 }

                 position = spline.EvaluatePosition(1);
                 data = splineData.Evaluate(spline, spline.GetLength(), PathIndexUnit.Distance, new Interpolators.LerpFloat());

                 var lastExtremity = (Vector3)position + ( data / k_SpeedScaleFactor ) * Vector3.up;

                 s_LineSegments.Add(previousExtremity);
                 s_LineSegments.Add(lastExtremity);
             }
                 
             using(new Handles.DrawingScope(color, localToWorld))
                 Handles.DrawLines(s_LineSegments.ToArray());
        }
                  
        public override void DrawDataPoint(
            int controlID, 
            Vector3 position, 
            Vector3 direction,
            Vector3 upDirection,
            SplineData<float> splineData, 
            int dataPointIndex)
        {
            if(direction == Vector3.zero)
                return;
             
            var handleColor = Handles.color;
            if(GUIUtility.hotControl == controlID)
                handleColor = Handles.selectedColor;
            else if(GUIUtility.hotControl == 0 && HandleUtility.nearestControl==controlID)
                handleColor = Handles.preselectionColor;
             
            var dataPoint = splineData[dataPointIndex];

            var maxSpeed = ( (SpeedHandleAttribute)attribute ).maxSpeed;
            var speedValue = dataPoint.Value;
            if(speedValue > maxSpeed)
            {
                speedValue = maxSpeed;
                dataPoint.Value = maxSpeed;
                splineData[dataPointIndex] = dataPoint;
            }

            var extremity = position + (speedValue / k_SpeedScaleFactor) * Vector3.up;
            using(new Handles.DrawingScope(handleColor))
            {
                var size = k_HandleSize * HandleUtility.GetHandleSize(position);
                Handles.DrawLine(position, extremity);
                var val = Handles.Slider(controlID, extremity, Vector3.up, size, Handles.SphereHandleCap, 0);
                Handles.Label(extremity + 2f * size * Vector3.up, speedValue.ToString());
                 
                if(GUIUtility.hotControl == controlID)
                {
                    var result = k_SpeedScaleFactor * (val - position).magnitude * math.sign(math.dot(val - position, Vector3.up));
                    dataPoint.Value = Mathf.Clamp(result, 0.01f, maxSpeed);
                    splineData[dataPointIndex] = dataPoint;
                }
            } 
        } 
    }
}
