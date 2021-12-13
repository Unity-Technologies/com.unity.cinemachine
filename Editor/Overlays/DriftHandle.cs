using System.Collections;
using System.Collections.Generic;
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
    [CustomSplineDataHandle(typeof(DriftHandleAttribute))]
    class DriftHandle  : SplineDataHandle<float>
    {
         const float k_HandleSize = 0.15f;
         
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
                 var direction = spline.EvaluateTangent(0);
                 var upDirection = spline.EvaluateUpVector(0);
                 
                 var right = math.normalize(math.cross(upDirection, direction));
                 var previousExtremity = (Vector3)position + data * (Vector3)right;
     
                 var currentOffset = s_DisplaySpace;
                 while(currentOffset < spline.GetLength())
                 {
                     var t = currentOffset / spline.GetLength();
                     position = spline.EvaluatePosition(t);
                     direction = spline.EvaluateTangent(t);
                     upDirection = spline.EvaluateUpVector(t);
                     right = math.normalize(math.cross(upDirection, direction));
                     
                     data = splineData.Evaluate(spline, currentOffset, PathIndexUnit.Distance, new Interpolators.LerpFloat());
     
                     var extremity = (Vector3)position + data * (Vector3)right;

                     s_LineSegments.Add(previousExtremity);
                     s_LineSegments.Add(extremity);
     
                     currentOffset += s_DisplaySpace;
                     previousExtremity = extremity;
                 }
     
                 position = spline.EvaluatePosition(1);
                 direction = spline.EvaluateTangent(1);
                 upDirection = spline.EvaluateUpVector(1);
                 right = math.normalize(math.cross(upDirection, direction));
                 data = splineData.Evaluate(spline, 1f, PathIndexUnit.Normalized, new Interpolators.LerpFloat());
     
                 var lastExtremity = (Vector3)position + data * (Vector3)right;
     
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

             var right = math.normalize(math.cross(upDirection, direction));
             var extremity = position + (dataPoint.Value) * (Vector3)right;
             
             using(new Handles.DrawingScope(handleColor))
             {
                 var size = 1.5f * k_HandleSize * HandleUtility.GetHandleSize(position);
                 Handles.DrawLine(position, extremity);
                 var val = Handles.Slider(controlID, extremity, right*math.sign(dataPoint.Value), size, Handles.ConeHandleCap, 0);
                 Handles.Label(extremity + 2f * size * Vector3.up, dataPoint.Value.ToString());
                 
                 if(GUIUtility.hotControl == controlID)
                 {
                     dataPoint.Value = (val - position).magnitude * math.sign(math.dot(val - position, right));
                     splineData[dataPointIndex] = dataPoint;
                 }
             }
         }
     }
}
