using UnityEngine;
using System.Collections.Generic;
using Cinemachine.Utility;
using System;
using UnityEngine.Serialization;

namespace Cinemachine
{
#if CINEMACHINE_PHYSICS || CINEMACHINE_PHYSICS_2D
    /// <summary>
    /// An add-on module for Cinemachine Virtual Camera that post-processes
    /// the final position of the virtual camera. It will confine the virtual
    /// camera's position to the volume specified in the Bounding Volume field.
    /// </summary>
    [AddComponentMenu("Cinemachine/Procedural/Extensions/Cinemachine Confiner")]
    [SaveDuringPlay]
    [ExecuteAlways]
    [DisallowMultipleComponent]
    [HelpURL(Documentation.BaseURL + "manual/CinemachineConfiner.html")]
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
        [FormerlySerializedAs("m_ConfineMode")]
        public Mode ConfineMode;
#endif

#if CINEMACHINE_PHYSICS
        /// <summary>The volume within which the camera is to be contained.</summary>
        [Tooltip("The volume within which the camera is to be contained")]
        [FormerlySerializedAs("m_BoundingVolume")]
        public Collider BoundingVolume;
#endif

#if CINEMACHINE_PHYSICS_2D

        /// <summary>The 2D shape within which the camera is to be contained.</summary>
        [Tooltip("The 2D shape within which the camera is to be contained")]
        [FormerlySerializedAs("m_BoundingShape2D")]
        public Collider2D BoundingShape2D;

        Collider2D m_BoundingShape2DCache;
#endif
        /// <summary>If camera is orthographic, screen edges will be confined to the volume.</summary>
        [Tooltip("If camera is orthographic, screen edges will be confined to the volume.  "
            + "If not checked, then only the camera center will be confined")]
        [FormerlySerializedAs("m_ConfineScreenEdges")]
        public bool ConfineScreenEdges = true;

        /// <summary>How gradually to return the camera to the bounding volume if it goes beyond the borders</summary>
        [Tooltip("How gradually to return the camera to the bounding volume if it goes beyond the borders.  "
            + "Higher numbers are more gradual.")]
        [RangeSlider(0, 10)]
        [FormerlySerializedAs("m_Damping")]
        public float Damping = 0;
        

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
            ConfineMode = Mode.Confine3D;
#endif
#if CINEMACHINE_PHYSICS
            BoundingVolume = null;
#endif
#if CINEMACHINE_PHYSICS_2D
            BoundingShape2D = null;
#endif
            ConfineScreenEdges = true;
            Damping = 0;

        }
        void OnValidate()
        {
            Damping = Mathf.Max(0, Damping);
        }

        /// <summary>
        /// Called when connecting to a virtual camera
        /// </summary>
        /// <param name="connect">True if connecting, false if disconnecting</param>
        protected override void ConnectToVcam(bool connect)
        {
            base.ConnectToVcam(connect);
        }

        class VcamExtraState
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
                return BoundingVolume != null && BoundingVolume.enabled && BoundingVolume.gameObject.activeInHierarchy;
#elif CINEMACHINE_PHYSICS_2D && !CINEMACHINE_PHYSICS
                return BoundingShape2D != null && BoundingShape2D.enabled && BoundingShape2D.gameObject.activeInHierarchy;
#else
                return (ConfineMode == Mode.Confine3D && BoundingVolume != null && BoundingVolume.enabled 
                            && BoundingVolume.gameObject.activeInHierarchy)
                       || (ConfineMode == Mode.Confine2D && BoundingShape2D != null && BoundingShape2D.enabled 
                            && BoundingShape2D.gameObject.activeInHierarchy);
#endif
            }
        }

        /// <summary>
        /// Report maximum damping time needed for this component.
        /// </summary>
        /// <returns>Highest damping setting in this component</returns>
        public override float GetMaxDampTime() => Damping;

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
                if (ConfineScreenEdges && state.Lens.Orthographic)
                    displacement = ConfineOrthoCameraToScreenEdges(ref state);
                else
                    displacement = ConfinePoint(state.GetCorrectedPosition());

                if (Damping > 0 && deltaTime >= 0 && VirtualCamera.PreviousStateIsValid)
                {
                    var delta = displacement - extra.PreviousDisplacement;
                    delta = Damper.Damp(delta, Damping, deltaTime);
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
            if (m_BoundingShape2DCache != BoundingShape2D)
            {
                InvalidateCache();
                m_BoundingShape2DCache = BoundingShape2D;
            }
            
            var colliderType = BoundingShape2D == null ? null : BoundingShape2D.GetType();
            if (colliderType == typeof(PolygonCollider2D))
            {
                var poly = BoundingShape2D as PolygonCollider2D;
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
                var poly = BoundingShape2D as CompositeCollider2D;
                if (m_PathCache == null || m_PathCache.Count != poly.pathCount || m_PathTotalPointCount != poly.pointCount)
                {
                    m_PathCache = new List<List<Vector2>>();
                    Vector2[] path = new Vector2[poly.pointCount];
                    var lossyScale = BoundingShape2D.transform.lossyScale;
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
            if (ConfineMode == Mode.Confine3D)
    #endif
                return BoundingVolume.ClosestPoint(camPos) - camPos;
#endif

#if CINEMACHINE_PHYSICS_2D
            // 2D version
            Vector2 p = camPos; // cast Vector3 to Vector2
            var closest = p;
            if (BoundingShape2D.OverlapPoint(camPos))
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
                    var v0 = BoundingShape2D.transform.TransformPoint(
                        m_PathCache[i][numPoints - 1] + BoundingShape2D.offset);
                    for (int j = 0; j < numPoints; ++j)
                    {
                        Vector2 v = BoundingShape2D.transform.TransformPoint(m_PathCache[i][j] + BoundingShape2D.offset);
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
    }
#endif
}
