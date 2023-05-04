using System;
using UnityEditor;
using UnityEditor.EditorTools;
using UnityEditor.Splines;
using UnityEngine;
using UnityEngine.Splines;

namespace Unity.Cinemachine.Editor
{
    [EditorTool("Roll Tool", typeof(CinemachineSplineRoll))]
    sealed class SplineRollTool : EditorTool, IDrawSelectedHandles
    {
        GUIContent m_IconContent;
        public override GUIContent toolbarIcon => m_IconContent;

        bool m_DisableHandles;
        bool m_RollInUse;
        bool m_IsSelected;

        void OnEnable()
        {
            m_IconContent = new GUIContent
            {
                image = AssetDatabase.LoadAssetAtPath<Texture2D>(
                    CinemachineCore.kPackageRoot + "/Editor/EditorResources/Icons/CmTrack@256.png"),
                text = "Roll Tool",
                tooltip = "Adjust the roll data points along the spline."
            };
        }

        /// <summary>This is called when the Tool is selected in the editor.</summary>
        public override void OnActivated()
        {
            base.OnActivated();
            m_IsSelected = true;
        }

        /// <summary>This is called when the Tool is deselected in the editor.</summary>
        public override void OnWillBeDeactivated()
        {
            base.OnWillBeDeactivated();
            m_IsSelected = false;
        }

        const float k_UnselectedAlpha = 0.5f;
        public override void OnToolGUI(EditorWindow window)
        {
            var splineDataTarget = target as CinemachineSplineRoll;
            if (splineDataTarget == null || splineDataTarget.Container == null)
                return;

            var nativeSpline = new NativeSpline(splineDataTarget.Container.Spline, splineDataTarget.Container.transform.localToWorldMatrix);
            
            Undo.RecordObject(splineDataTarget, "Modifying Roll SplineData");
            m_DisableHandles = false;
            var color = Handles.selectedColor;
            if (!m_IsSelected) 
                color.a = k_UnselectedAlpha;
            using (new Handles.DrawingScope(color))
            {
                m_RollInUse = DrawRollDataPoints(nativeSpline, splineDataTarget.Roll);
                nativeSpline.DataPointHandles(splineDataTarget.Roll);
            }
        }

        public void OnDrawHandles()
        {
            var splineDataTarget = target as CinemachineSplineRoll;
            if (ToolManager.IsActiveTool(this) || splineDataTarget == null || splineDataTarget.Container == null)
                return;

            if (Event.current.type != EventType.Repaint)
                return;

            m_DisableHandles = true;
            var nativeSpline = new NativeSpline(splineDataTarget.Container.Spline, 
                splineDataTarget.Container.transform.localToWorldMatrix);

            var color = Handles.selectedColor;
            if (!m_IsSelected) 
                color.a = k_UnselectedAlpha;
            using (new Handles.DrawingScope(color))
            {
                DrawRollDataPoints(nativeSpline, splineDataTarget.Roll);
            }
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

                var normalizedT =
                    SplineUtility.GetNormalizedInterpolation(spline, dataPoint.Index, rollSplineData.PathIndexUnit);
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

            // local function
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
                    var handleLocalRotation = Quaternion.Euler(0, rollData, 0);
                    var handleWorldRotation = m_DefaultHandleOrientation * handleLocalRotation;

                    var handleSize = Mathf.Max(
                        HandleUtility.GetHandleSize(Vector3.zero) / 2f, CinemachineSplineDollyPrefs.SplineWidth.Value);
                    if (Event.current.type == EventType.Repaint) {
                        using (new Handles.DrawingScope(m_RollInUse ? Handles.selectedColor : Handles.color)) 
                            Handles.ArrowHandleCap(-1, Vector3.zero, handleWorldRotation, handleSize, EventType.Repaint);
                    }
                    
                    var newHandleRotationGlobalSpace = Handles.Disc(controlID, handleWorldRotation,
                        Vector3.zero, Vector3.forward, handleSize, false, 0);
                    if (GUIUtility.hotControl == controlID)
                    {
                        // Handles.Disc returns roll values in the [0, 360] range. Therefore, it works only in fixed ranges
                        // For example, within any of these ..., [-720, -360], [-360, 0], [0, 360], [360, 720], ...
                        // But we want to be able to rotate through these ranges, and not get stuck. We can detect when to
                        // move between ranges: when the roll delta is big. e.g. 359 -> 1 (358), instead of 1 -> 2 (1)
                        var newHandleRotationLocalSpace = m_DefaultHandleOrientationInverse * newHandleRotationGlobalSpace;
                        var deltaRoll = newHandleRotationLocalSpace.eulerAngles.y - handleLocalRotation.eulerAngles.y;
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
    }
}
