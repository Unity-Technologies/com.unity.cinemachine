#if !CINEMACHINE_NO_CM2_SUPPORT
#if CINEMACHINE_PHYSICS || CINEMACHINE_PHYSICS_2D

using UnityEngine;
using System.Collections.Generic;
using System;

namespace Unity.Cinemachine
{
    /// <summary>
    /// This is a deprecated component.  Use CinemachineConfiner2D or CinemachineConfiner3D instead.
    /// </summary>
    [Obsolete("CinemachineConfiner has been deprecated. Use CinemachineConfiner2D or CinemachineConfiner3D instead")]
    [AddComponentMenu("")] // Hide in menu
    [SaveDuringPlay]
    [ExecuteAlways]
    [DisallowMultipleComponent]
    public class CinemachineConfiner : CinemachineExtension
    {
#if CINEMACHINE_PHYSICS && CINEMACHINE_PHYSICS_2D
        /// <summary>The confiner can operate using a 2D bounding shape or a 3D bounding volume</summary>
        public enum Mode
        {
            /// <summary>Use a 2D bounding shape, suitable for an orthographic camera</summary>
            Confine2D,
            /// <summary>Use a 3D bounding shape, suitable for perspective cameras</summary>
            Confine3D
        };
        /// <summary>The confiner can operate using a 2D bounding shape or a 3D bounding volume</summary>
        [Tooltip("The confiner can operate using a 2D bounding shape or a 3D bounding volume")]
        public Mode m_ConfineMode;
#endif

#if CINEMACHINE_PHYSICS
        /// <summary>The volume within which the camera is to be contained.</summary>
        [Tooltip("The volume within which the camera is to be contained")]
        public Collider m_BoundingVolume;
#endif

#if CINEMACHINE_PHYSICS_2D

        /// <summary>The 2D shape within which the camera is to be contained.</summary>
        [Tooltip("The 2D shape within which the camera is to be contained")]
        public Collider2D m_BoundingShape2D;

        Collider2D m_BoundingShape2DCache;
#endif
        /// <summary>If camera is orthographic, screen edges will be confined to the volume.</summary>
        [Tooltip("If camera is orthographic, screen edges will be confined to the volume.  "
            + "If not checked, then only the camera center will be confined")]
        public bool m_ConfineScreenEdges = true;

        /// <summary>How gradually to return the camera to the bounding volume if it goes beyond the borders</summary>
        [Tooltip("How gradually to return the camera to the bounding volume if it goes beyond the borders.  "
            + "Higher numbers are more gradual.")]
        [Range(0, 10)]
        public float m_Damping = 0;
        

        List<List<Vector2>> m_PathCache;
        int m_PathTotalPointCount;
        
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
            return GetExtraState<VcamExtraState>(vcam).ConfinerDisplacement;
        }

        void Reset()
        {
#if CINEMACHINE_PHYSICS && CINEMACHINE_PHYSICS_2D
            m_ConfineMode = Mode.Confine3D;
#endif
#if CINEMACHINE_PHYSICS
            m_BoundingVolume = null;
#endif
#if CINEMACHINE_PHYSICS_2D
            m_BoundingShape2D = null;
#endif
            m_ConfineScreenEdges = true;
            m_Damping = 0;

        }
        void OnValidate()
        {
            m_Damping = Mathf.Max(0, m_Damping);
        }

        /// <summary>
        /// Called when connecting to a virtual camera
        /// </summary>
        /// <param name="connect">True if connecting, false if disconnecting</param>
        protected override void ConnectToVcam(bool connect)
        {
            base.ConnectToVcam(connect);
        }

        class VcamExtraState : VcamExtraStateBase
        {
            public Vector3 PreviousDisplacement;
            public float ConfinerDisplacement;
        };

        /// <summary>Check if the bounding volume is defined</summary>
        public bool IsValid
        {
            get
            {
#if CINEMACHINE_PHYSICS && !CINEMACHINE_PHYSICS_2D
                return m_BoundingVolume != null && m_BoundingVolume.enabled && m_BoundingVolume.gameObject.activeInHierarchy;
#elif CINEMACHINE_PHYSICS_2D && !CINEMACHINE_PHYSICS
                return m_BoundingShape2D != null && m_BoundingShape2D.enabled && m_BoundingShape2D.gameObject.activeInHierarchy;
#else
                return (m_ConfineMode == Mode.Confine3D && m_BoundingVolume != null && m_BoundingVolume.enabled 
                            && m_BoundingVolume.gameObject.activeInHierarchy)
                       || (m_ConfineMode == Mode.Confine2D && m_BoundingShape2D != null && m_BoundingShape2D.enabled 
                            && m_BoundingShape2D.gameObject.activeInHierarchy);
#endif
            }
        }

        /// <summary>
        /// Report maximum damping time needed for this component.
        /// </summary>
        /// <returns>Highest damping setting in this component</returns>
        public override float GetMaxDampTime() => m_Damping;

        /// <summary>
        /// Callback to do the camera confining
        /// </summary>
        /// <param name="vcam">The virtual camera being processed</param>
        /// <param name="stage">The current pipeline stage</param>
        /// <param name="state">The current virtual camera state</param>
        /// <param name="deltaTime">The current applicable deltaTime</param>
        protected override void PostPipelineStageCallback(
            CinemachineVirtualCameraBase vcam,
            CinemachineCore.Stage stage, ref CameraState state, float deltaTime)
        {
            if (IsValid && stage == CinemachineCore.Stage.Body)
            {
                var extra = GetExtraState<VcamExtraState>(vcam);
                Vector3 displacement;
                if (m_ConfineScreenEdges && state.Lens.Orthographic)
                    displacement = ConfineOrthoCameraToScreenEdges(ref state);
                else
                    displacement = ConfinePoint(state.GetCorrectedPosition());

                if (m_Damping > 0 && deltaTime >= 0 && vcam.PreviousStateIsValid)
                {
                    var delta = displacement - extra.PreviousDisplacement;
                    delta = Damper.Damp(delta, m_Damping, deltaTime);
                    displacement = extra.PreviousDisplacement + delta;
                }
                extra.PreviousDisplacement = displacement;
                state.PositionCorrection += displacement;
                extra.ConfinerDisplacement = displacement.magnitude;
            }
        }

        [Obsolete("Please use InvalidateCache() instead")]
        public void InvalidatePathCache() => InvalidatePathCache();

        /// <summary>Call this if the bounding shape's points change at runtime</summary>
        public void InvalidateCache()
        {
#if CINEMACHINE_PHYSICS_2D
            m_PathCache = null;
            m_BoundingShape2DCache = null;
#endif
        }

        bool ValidatePathCache()
        {
#if CINEMACHINE_PHYSICS_2D
            if (m_BoundingShape2DCache != m_BoundingShape2D)
            {
                InvalidateCache();
                m_BoundingShape2DCache = m_BoundingShape2D;
            }
            
            var colliderType = m_BoundingShape2D == null ? null : m_BoundingShape2D.GetType();
            if (colliderType == typeof(PolygonCollider2D))
            {
                var poly = m_BoundingShape2D as PolygonCollider2D;
                if (m_PathCache == null || m_PathCache.Count != poly.pathCount || m_PathTotalPointCount != poly.GetTotalPointCount())
                {
                    m_PathCache = new List<List<Vector2>>();
                    for (int i = 0; i < poly.pathCount; ++i)
                    {
                        Vector2[] path = poly.GetPath(i);
                        List<Vector2> dst = new List<Vector2>();
                        for (int j = 0; j < path.Length; ++j)
                            dst.Add(path[j]);
                        m_PathCache.Add(dst);
                    }
                    m_PathTotalPointCount = poly.GetTotalPointCount();
                }
                return true;
            }
            else if (colliderType == typeof(CompositeCollider2D))
            {
                var poly = m_BoundingShape2D as CompositeCollider2D;
                if (m_PathCache == null || m_PathCache.Count != poly.pathCount || m_PathTotalPointCount != poly.pointCount)
                {
                    m_PathCache = new List<List<Vector2>>();
                    Vector2[] path = new Vector2[poly.pointCount];
                    var lossyScale = m_BoundingShape2D.transform.lossyScale;
                    var revertCompositeColliderScale = new Vector2(
                        1f / lossyScale.x, 
                        1f / lossyScale.y);
                    for (int i = 0; i < poly.pathCount; ++i)
                    {
                        int numPoints = poly.GetPath(i, path);
                        List<Vector2> dst = new List<Vector2>();
                        for (int j = 0; j < numPoints; ++j)
                            dst.Add(path[j] * revertCompositeColliderScale);
                        m_PathCache.Add(dst);
                    }
                    m_PathTotalPointCount = poly.pointCount;
                }
                return true;
            }
#endif
            InvalidateCache();
            return false;
        }

        private Vector3 ConfinePoint(Vector3 camPos)
        {
#if CINEMACHINE_PHYSICS
            // 3D version
    #if CINEMACHINE_PHYSICS_2D
            if (m_ConfineMode == Mode.Confine3D)
    #endif
                return m_BoundingVolume.ClosestPoint(camPos) - camPos;
#endif

#if CINEMACHINE_PHYSICS_2D
            // 2D version
            Vector2 p = camPos; // cast Vector3 to Vector2
            var closest = p;
            if (m_BoundingShape2D.OverlapPoint(camPos))
                return Vector3.zero;
            // Find the nearest point on the shape's boundary
            if (!ValidatePathCache())
                return Vector3.zero;

            var bestDistance = float.MaxValue;
            for (int i = 0; i < m_PathCache.Count; ++i)
            {
                int numPoints = m_PathCache[i].Count;
                if (numPoints > 0)
                {
                    var v0 = m_BoundingShape2D.transform.TransformPoint(
                        m_PathCache[i][numPoints - 1] + m_BoundingShape2D.offset);
                    for (int j = 0; j < numPoints; ++j)
                    {
                        Vector2 v = m_BoundingShape2D.transform.TransformPoint(m_PathCache[i][j] + m_BoundingShape2D.offset);
                        var c = Vector2.Lerp(v0, v, p.ClosestPointOnSegment(v0, v));
                        var d = Vector2.SqrMagnitude(p - c);
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
#endif
        }
        
        Vector3 ConfineOrthoCameraToScreenEdges(ref CameraState state)
        {
            var rot = state.GetCorrectedOrientation();
            var dy = state.Lens.OrthographicSize;
            var dx = dy * state.Lens.Aspect;
            var vx = (rot * Vector3.right) * dx;
            var vy = (rot * Vector3.up) * dy;

            var displacement = Vector3.zero;
            var camPos = state.GetCorrectedPosition();
            var lastD = Vector3.zero;

            const int kMaxIter = 12;
            for (var i = 0; i < kMaxIter; ++i)
            {
                var d = ConfinePoint((camPos - vy) - vx);
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
    
        // Helper to upgrade to CM3
        internal Type UpgradeToCm3_GetTargetType()
        {
#if CINEMACHINE_PHYSICS && CINEMACHINE_PHYSICS_2D
            return m_ConfineMode == Mode.Confine3D ? typeof(CinemachineConfiner3D) : typeof(CinemachineConfiner2D);
#elif CINEMACHINE_PHYSICS_2D
            return typeof(CinemachineConfiner2D);
#else
            return typeof(CinemachineConfiner3D);
#endif
        }

#if CINEMACHINE_PHYSICS
        // Helper to upgrade to CM3
        internal void UpgradeToCm3(CinemachineConfiner3D c)
        {
            c.BoundingVolume = m_BoundingVolume;
            //c.SlowingDistance = m_Damping;  // we can't upgrade this because one is time and the other is distance
        }
#endif
#if CINEMACHINE_PHYSICS_2D
        // Helper to upgrade to CM3
        internal void UpgradeToCm3(CinemachineConfiner2D c)
        {
            c.BoundingShape2D = m_BoundingShape2D;
            c.Damping = m_Damping;
        }
#endif
    }
}
#endif
#endif
