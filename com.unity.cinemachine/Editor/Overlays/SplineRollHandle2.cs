using System;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEditor;
using UnityEditor.EditorTools;
using UnityEditor.Splines;
using UnityEngine;
using UnityEngine.Splines;

namespace Cinemachine.Editor
{
    [EditorTool("Roll Tool", typeof(CinemachineSplineRoll))]
    public class SplineRollTool : EditorTool, IDrawSelectedHandles
    {
        Color m_HandleColor = new(1f, 0.6f, 0f);
        List<Vector3> m_LineSegments = new List<Vector3>();

        GUIContent m_IconContent;
        public override GUIContent toolbarIcon => m_IconContent;

        bool m_DisableHandles;
        bool m_RollInUse;

        void OnEnable()
        {
            m_IconContent = new GUIContent()
            {
                image = EditorGUIUtility.IconContent("d_WheelCollider Icon").image, // TODO: create a proper image
                text = "Roll Tool",
                tooltip = "Adjust the roll data points along the spline."
            };
        }

        public override void OnToolGUI(EditorWindow window)
        {
            var splineDataTarget = target as CinemachineSplineRoll;
            if (splineDataTarget == null || splineDataTarget.Container == null)
                return;

            var nativeSpline = new NativeSpline(splineDataTarget.Container.Spline, splineDataTarget.Container.transform.localToWorldMatrix);

            Undo.RecordObject(splineDataTarget, "Modifying Roll SplineData");

            m_DisableHandles = false;

            var originalColor = Handles.color;
            {
                Handles.color = m_HandleColor;

                m_RollInUse = DrawRollDataPoints(nativeSpline, splineDataTarget.Roll);
                DrawRollSplineData(nativeSpline, splineDataTarget.Roll);
                
                nativeSpline.DataPointHandles(splineDataTarget.Roll);
            }
            Handles.color = originalColor;
        }

        public void OnDrawHandles()
        {
            var splineDataTarget = target as CinemachineSplineRoll;
            if (ToolManager.IsActiveTool(this) || splineDataTarget == null || splineDataTarget.Container == null)
                return;

            if (Event.current.type != EventType.Repaint)
                return;

            m_DisableHandles = true;

            var nativeSpline = new NativeSpline(splineDataTarget.Container.Spline, splineDataTarget.Container.transform.localToWorldMatrix);
            var color = m_HandleColor;
            color.a = 0.5f;
            Handles.color = color;
            DrawRollDataPoints(nativeSpline, splineDataTarget.Roll);
            DrawRollSplineData(nativeSpline, splineDataTarget.Roll);
        }
        
        // inverse pre-calculation optimization
        readonly Quaternion m_DefaultHandleOrientation = Quaternion.Euler(270, 0, 0);
        readonly Quaternion m_DefaultHandleOrientationInverse = Quaternion.Euler(90, 0, 0);
        bool DrawRollDataPoints(NativeSpline spline, SplineData<float> rollSplineData)
        {
            var inUse = false;
            for (var r = 0; r < rollSplineData.Count; ++r)
            {
                var dataPoint = rollSplineData[r];

                var normalizedT = SplineUtility.GetNormalizedInterpolation(spline, dataPoint.Index, rollSplineData.PathIndexUnit);
                spline.Evaluate(normalizedT, out var position, out var tangent, out var up);

                var id = m_DisableHandles ? -1 : GUIUtility.GetControlID(FocusType.Passive);
                if (DrawDataPoint(id, position, tangent, up, dataPoint.Value, out var result))
                {
                    dataPoint.Value = result;
                    rollSplineData[r] = dataPoint;
                    inUse = true;
                }
            }
            return inUse;

            bool DrawDataPoint(
                int controlID, Vector3 position, Vector3 tangent, Vector3 up, float rollData, out float result)
            {
                result = 0;
                if (tangent == Vector3.zero)
                    return false;

                var drawMatrix = Matrix4x4.identity;
                drawMatrix.SetTRS(position, Quaternion.LookRotation(tangent, up), Vector3.one);

                drawMatrix = Handles.matrix * drawMatrix;
                using (new Handles.DrawingScope(drawMatrix)) // use draw matrix, so we work in local space
                {
                    var handleRotationLocalSpace = Quaternion.Euler(0, rollData, 0);
                    var handleRotationGlobalSpace = m_DefaultHandleOrientation * handleRotationLocalSpace;

                    var color = Handles.color;
                    if (!m_RollInUse)
                        color.a = 0.33f;

                    using (new Handles.DrawingScope(color))
                    {
                        Handles.ArrowHandleCap(-1, Vector3.zero, handleRotationGlobalSpace, 1f, EventType.Repaint);
                    }

                    var newHandleRotationGlobalSpace = Handles.Disc(controlID, handleRotationGlobalSpace,
                        Vector3.zero, Vector3.forward,
                        Mathf.Max(HandleUtility.GetHandleSize(Vector3.zero) / 2f, CinemachineSplineDollyPrefs.SplineWidth),
                        false, 0);
                    if (GUIUtility.hotControl == controlID)
                    {
                        // Handles.Disc returns roll values in the [0, 360] range. Therefore, it works only in fixed ranges
                        // For example, within any of these ..., [-720, -360], [-360, 0], [0, 360], [360, 720], ...
                        // But we want to be able to rotate through these ranges, and not get stuck. We can detect when to
                        // move between ranges: when the roll delta is big. e.g. 359 -> 1 (358), instead of 1 -> 2 (1)
                        var newHandleRotationLocalSpace = m_DefaultHandleOrientationInverse * newHandleRotationGlobalSpace;
                        var deltaRoll = newHandleRotationLocalSpace.eulerAngles.y - handleRotationLocalSpace.eulerAngles.y;
                        if (deltaRoll > 180)
                            deltaRoll -= 360; // Roll down one range
                        else if (deltaRoll < -180) 
                            deltaRoll += 360; // Roll up one range

                        rollData += deltaRoll;
                        result = rollData;
                        return true;
                    }
                }

                return false;
            }
        }
        
        const float k_DisplaySpace = 0.5f;
        void DrawRollSplineData(NativeSpline spline, SplineData<float> splineData)
        {
            m_LineSegments.Clear();
            if (GUIUtility.hotControl == 0 || m_RollInUse || !ToolManager.IsActiveTool(this) || Tools.viewToolActive)
            {
                var currentOffset = k_DisplaySpace;
                while (currentOffset < spline.GetLength())
                {
                    var t = currentOffset / spline.GetLength();
                    spline.Evaluate(t, out float3 position, out float3 direction, out float3 up);
                    var data = splineData.Evaluate(spline, t, PathIndexUnit.Normalized,
                        new UnityEngine.Splines.Interpolators.LerpFloat());

                    var localMatrix = Matrix4x4.identity;
                    localMatrix.SetTRS(position, Quaternion.LookRotation(direction, up), Vector3.one);
                    var pos = localMatrix.GetPosition();
                    m_LineSegments.Add(pos);
                    m_LineSegments.Add(pos + (Vector3)up * data);

                    currentOffset += k_DisplaySpace;
                }
            }

            var color = Handles.color;
            if (!m_RollInUse)
                color.a = 0.33f;

            using (new Handles.DrawingScope(color))
                Handles.DrawLines(m_LineSegments.ToArray());
        }
    }
}
