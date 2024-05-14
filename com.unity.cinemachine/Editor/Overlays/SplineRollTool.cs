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
        bool m_RollInUse;
        bool Active => ToolManager.IsActiveTool(this);

        public override GUIContent toolbarIcon => m_IconContent;

        bool GetTargets(out CinemachineSplineRoll splineData, out SplineContainer spline)
        {
            splineData = target as CinemachineSplineRoll;
            if (splineData != null)
            {
                spline = splineData.Container;
                return spline != null && spline.Spline != null;
            }
            spline = null;
            return false;
        }

        void OnEnable()
        {
            m_IconContent = new GUIContent
            {
                image = AssetDatabase.LoadAssetAtPath<Texture2D>(
                    CinemachineCore.kPackageRoot + "/Editor/EditorResources/Icons/CmTrackRoll@256.png"),
                text = "Roll Tool",
                tooltip = "Adjust the roll data points along the spline."
            };
        }

        const float k_UnselectedAlpha = 0.3f;
        public override void OnToolGUI(EditorWindow window)
        {
            if (!GetTargets(out var splineData, out var spline))
                return;

            Undo.RecordObject(splineData, "Modifying Roll SplineData");
            var color = Handles.selectedColor;
            if (!Active) 
                color.a = k_UnselectedAlpha;
            using (new Handles.DrawingScope(color))
            {
                var nativeSpline = new NativeSpline(spline.Spline, spline.transform.localToWorldMatrix);
                m_RollInUse = DrawRollDataPoints(nativeSpline, splineData.Roll, true);
                nativeSpline.DataPointHandles(splineData.Roll);
            }
        }

        public void OnDrawHandles()
        {
            if (Event.current.type != EventType.Repaint || Active || !GetTargets(out var splineData, out var spline))
                return;

            var color = Handles.selectedColor;
            color.a = k_UnselectedAlpha;
            using (new Handles.DrawingScope(color))
            {
                var nativeSpline = new NativeSpline(spline.Spline, spline.transform.localToWorldMatrix);
                DrawRollDataPoints(nativeSpline, splineData.Roll, false);
            }
        }
        
        // inverse pre-calculation optimization
        readonly Quaternion m_DefaultHandleOrientation = Quaternion.Euler(270, 0, 0);
        readonly Quaternion m_DefaultHandleOrientationInverse = Quaternion.Euler(90, 0, 0);
        bool DrawRollDataPoints(NativeSpline spline, SplineData<float> splineData, bool enabled)
        {
            var inUse = false;
            for (var r = 0; r < splineData.Count; ++r)
            {
                var dataPoint = splineData[r];
                var t = SplineUtility.GetNormalizedInterpolation(spline, dataPoint.Index, splineData.PathIndexUnit);
                spline.Evaluate(t, out var position, out var tangent, out var up);

                var id = enabled ? GUIUtility.GetControlID(FocusType.Passive) : -1;
                if (DrawDataPoint(id, position, tangent, up, dataPoint.Value, out var result))
                {
                    dataPoint.Value = result;
                    splineData.SetDataPoint(r, dataPoint);
                    inUse = true;
                }
            }
            return inUse;

            // local function
            bool DrawDataPoint(int controlID, Vector3 position, Vector3 tangent, Vector3 up, float rollData, out float result)
            {
                result = 0;
                if (tangent == Vector3.zero)
                    return false;

                var drawMatrix = Handles.matrix * Matrix4x4.TRS(position, Quaternion.LookRotation(tangent, up), Vector3.one);
                using (new Handles.DrawingScope(drawMatrix)) // use draw matrix, so we work in local space
                {
                    var localRot = Quaternion.Euler(0, rollData, 0);
                    var globalRot = m_DefaultHandleOrientation * localRot;

                    var handleSize = Mathf.Max(HandleUtility.GetHandleSize(Vector3.zero) / 2f, CinemachineSplineDollyPrefs.SplineWidth.Value);
                    if (Event.current.type == EventType.Repaint) 
                    {
                        using (new Handles.DrawingScope(m_RollInUse ? Handles.selectedColor : Handles.color)) 
                            Handles.ArrowHandleCap(-1, Vector3.zero, globalRot, handleSize, EventType.Repaint);
                    }
                    
                    var newGlobalRot = Handles.Disc(controlID, globalRot, Vector3.zero, Vector3.forward, handleSize, false, 0);
                    if (GUIUtility.hotControl == controlID)
                    {
                        // Handles.Disc returns roll values in the [0, 360] range. Therefore, it works only in fixed ranges
                        // For example, within any of these ..., [-720, -360], [-360, 0], [0, 360], [360, 720], ...
                        // But we want to be able to rotate through these ranges, and not get stuck. We can detect when to
                        // move between ranges: when the roll delta is big. e.g. 359 -> 1 (358), instead of 1 -> 2 (1)
                        var newLocalRot = m_DefaultHandleOrientationInverse * newGlobalRot;
                        var deltaRoll = newLocalRot.eulerAngles.y - localRot.eulerAngles.y;
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
