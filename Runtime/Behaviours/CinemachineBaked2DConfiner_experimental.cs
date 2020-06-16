using UnityEngine;
using System.Collections.Generic;
using Cinemachine.Utility;
using System;

namespace Cinemachine
{
    [SaveDuringPlay]
#if UNITY_2018_3_OR_NEWER
    [ExecuteAlways]
#else
    [ExecuteInEditMode]
#endif
    public class CinemachineBaked2DConfiner_experimental : CinemachineExtension
    {
        /// <summary>The 2D shape within which the camera is to be contained.</summary>
        [Tooltip("The 2D shape within which the camera is to be contained")]
        public PolygonCollider2D m_BoundingShape2D;
        private PolygonCollider2D m_internal_BoundingShape2D;

        /// <summary>If camera is orthographic, screen edges will be confined to the volume.</summary>
        [Tooltip("If camera is orthographic, screen edges will be confined to the volume.  "
            + "If not checked, then only the camera center will be confined")]
        public bool m_ConfineScreenEdges = true;

        /// <summary>How gradually to return the camera to the bounding volume if it goes beyond the borders</summary>
        [Tooltip("How gradually to return the camera to the bounding volume if it goes beyond the borders.  "
            + "Higher numbers are more gradual.")]
        [Range(0, 10)]
        public float m_Damping = 0;

        [Tooltip("Damping applied automatically around corners to avoid jumps.  "
                 + "Higher numbers produce more smooth cornering.")]
        [Range(0, 10)]
        public float m_CornerDamping = 0;

        [Tooltip("After going through the corner should the camera return smoothly or snap?")]
        public bool m_SnapFromCorner = true;
        private float m_CornerAngleTreshold = 10f;
        private bool m_Cornerring = false;

        private ConfinerBakery m_ConfinerBakery;

        /// <summary>See whether the virtual camera has been moved by the confiner</summary>
        /// <param name="vcam">The virtual camera in question.  This might be different from the
        /// virtual camera that owns the confiner, in the event that the camera has children</param>
        /// <returns>True if the virtual camera has been repositioned</returns>
        public bool CameraWasDisplaced(CinemachineVirtualCameraBase vcam)
        {
            return GetCameraDisplacementDistance(vcam) > 0;
        }

        /// <summary>See how far virtual camera has been moved by the confiner</summary>
        /// <param name="vcam">The virtual camera in question.  This might be different from the
        /// virtual camera that owns the confiner, in the event that the camera has children</param>
        /// <returns>True if the virtual camera has been repositioned</returns>
        public float GetCameraDisplacementDistance(CinemachineVirtualCameraBase vcam)
        {
            return GetExtraState<VcamExtraState>(vcam).confinerDisplacement;
        }
        
        private void OnValidate()
        {
            m_Damping = Mathf.Max(0, m_Damping);
        }

        class VcamExtraState
        {
            public Vector3 m_previousDisplacement;
            public float confinerDisplacement;
            public bool applyAfterAim;
        };

        /// <summary>Check if the bounding volume is defined</summary>
        public bool IsValid
        {
            get
            {
                return m_BoundingShape2D != null;
            }
        }

        protected override void ConnectToVcam(bool connect)
        {
            base.ConnectToVcam(connect);

            CinemachineVirtualCamera vcam = VirtualCamera as CinemachineVirtualCamera;
            if (vcam == null) return;
            
            var components = vcam.GetComponentPipeline();
            foreach (var component in components)
            {
                if (component.BodyAppliesAfterAim)
                {
                    var extraState = GetExtraState<VcamExtraState>(vcam);
                    extraState.applyAfterAim = true;
                    break;
                }
            }
        }
        
        /// <summary>
        /// Report maximum damping time needed for this component.
        /// </summary>
        /// <returns>Highest damping setting in this component</returns>
        public override float GetMaxDampTime() 
        { 
            return m_Damping;
        }

        /// <summary>Callback to to the camera confining</summary>
        protected override void PostPipelineStageCallback(
            CinemachineVirtualCameraBase vcam,
            CinemachineCore.Stage stage, ref CameraState state, float deltaTime)
        {
            if (IsValid)
            {
                if (m_ConfinerBakery == null)
                {
                    m_ConfinerBakery = new ConfinerBakery(ref m_BoundingShape2D);
                }
                m_ConfinerBakery.InputConfiner = m_BoundingShape2D;
                if (m_ConfinerBakery.Bake())
                {
                    InvalidatePathCache();
                }
                m_internal_BoundingShape2D = m_ConfinerBakery.OutputConfiner;
                
                var extra = GetExtraState<VcamExtraState>(vcam);
                if ((extra.applyAfterAim && stage == CinemachineCore.Stage.Finalize)
                    ||
                    (!extra.applyAfterAim && stage == CinemachineCore.Stage.Body))
                {
                    Vector3 displacement;
                    if (m_ConfineScreenEdges && state.Lens.Orthographic)
                        displacement = ConfineScreenEdges(vcam, ref state);
                    else
                        displacement = ConfinePoint(state.CorrectedPosition);
                    
                   
                    if (VirtualCamera.PreviousStateIsValid && deltaTime >= 0)
                    { 
                        var displacementAngle = Vector2.Angle(extra.m_previousDisplacement, displacement);
                        if (m_CornerDamping > 0 && (m_Cornerring || displacementAngle > m_CornerAngleTreshold))
                        {
                            if (!m_SnapFromCorner) {
                                m_Cornerring = displacementAngle > 1f;
                            }
                            Vector3 delta = displacement - extra.m_previousDisplacement;
                            delta = Damper.Damp(delta, m_CornerDamping, deltaTime);
                            displacement = extra.m_previousDisplacement + delta;
                        }
                        else if (m_Damping > 0)
                        {
                            Vector3 delta = displacement - extra.m_previousDisplacement;
                            delta = Damper.Damp(delta, m_Damping, deltaTime);
                            displacement = extra.m_previousDisplacement + delta;
                        }
                    }
                    
                    extra.m_previousDisplacement = displacement;
                    state.PositionCorrection += displacement;
                    extra.confinerDisplacement = displacement.magnitude;
                }
            }
        }

        private List<List<Vector2>> m_pathCache;
        private int m_pathTotalPointCount;

        /// <summary>Call this if the bounding shape's points change at runtime</summary>
        public void InvalidatePathCache()
        {
            m_pathCache = null;
            m_internal_BoundingShape2D = null;
        }

        bool ValidatePathCache()
        {
            PolygonCollider2D poly = m_internal_BoundingShape2D;
            if (m_pathCache == null || m_pathCache.Count != poly.pathCount || m_pathTotalPointCount != poly.GetTotalPointCount())
            {
                m_pathCache = new List<List<Vector2>>();
                for (int i = 0; i < poly.pathCount; ++i)
                {
                    Vector2[] path = poly.GetPath(i);
                    List<Vector2> dst = new List<Vector2>();
                    for (int j = 0; j < path.Length; ++j)
                        dst.Add(path[j]);
                    m_pathCache.Add(dst);
                }
                m_pathTotalPointCount = poly.GetTotalPointCount();
            }
            return true;
        }

        private Vector3 ConfinePoint(Vector3 camPos)
        {
            // 2D version
            Vector2 p = camPos;
            Vector2 closest = p;
            if (m_internal_BoundingShape2D.OverlapPoint(camPos))
                return Vector3.zero;
            // Find the nearest point on the shape's boundary
            if (!ValidatePathCache())
                return Vector3.zero;

            float bestDistance = float.MaxValue;
            for (int i = 0; i < m_pathCache.Count; ++i)
            {
                int numPoints = m_pathCache[i].Count;
                if (numPoints > 0)
                {
                    Vector2 v0 = m_internal_BoundingShape2D.transform.TransformPoint(m_pathCache[i][numPoints - 1]);
                    for (int j = 0; j < numPoints; ++j)
                    {
                        Vector2 v = m_internal_BoundingShape2D.transform.TransformPoint(m_pathCache[i][j]);
                        Vector2 c = Vector2.Lerp(v0, v, p.ClosestPointOnSegment(v0, v));
                        float d = Vector2.SqrMagnitude(p - c);
                        if (d < bestDistance)
                        {
                            bestDistance = d;
                            closest = c;
                        }
                        v0 = v;
                    }
                }
            }
            return closest - p;
        }

        // Camera must be orthographic
        private Vector3 ConfineScreenEdges(CinemachineVirtualCameraBase vcam, ref CameraState state)
        {
            Quaternion rot = Quaternion.Inverse(state.CorrectedOrientation);
            float dy = state.Lens.OrthographicSize;
            float dx = dy * state.Lens.Aspect;
            Vector3 vx = (rot * Vector3.right) * dx;
            Vector3 vy = (rot * Vector3.up) * dy;

            Vector3 displacement = Vector3.zero;
            Vector3 camPos = state.CorrectedPosition;
            Vector3 lastD = Vector3.zero;
            const int kMaxIter = 12;
            for (int i = 0; i < kMaxIter; ++i)
            {
                Vector3 d = ConfinePoint((camPos - vy) - vx);
                if (d.AlmostZero())
                    d = ConfinePoint((camPos + vy) + vx);
                if (d.AlmostZero())
                    d = ConfinePoint((camPos - vy) + vx);
                if (d.AlmostZero())
                    d = ConfinePoint((camPos + vy) - vx);
                if (d.AlmostZero())
                    break;
                if ((d + lastD).AlmostZero())
                {
                    displacement += d * 0.5f;  // confiner too small: center it
                    break;
                }
                displacement += d;
                camPos += d;
                lastD = d;
            }
            return displacement;
        }
    }
}