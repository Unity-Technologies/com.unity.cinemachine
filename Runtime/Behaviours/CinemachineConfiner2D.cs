#if !UNITY_2019_3_OR_NEWER
#define CINEMACHINE_PHYSICS_2D
#endif

using System;
using System.Collections.Generic;
using Cinemachine.Utility;
using UnityEditor;
using UnityEngine;

namespace Cinemachine
{
#if CINEMACHINE_PHYSICS_2D
    /// <summary>
    /// An add-on module for Cinemachine Virtual Camera that post-processes
    /// the final position of the virtual camera. It will confine the virtual
    /// camera view window to the area specified in the Bounding Shape 2D field
    /// based on the camera's window size and ratio.
    /// The confining area is baked and cached at start.
    /// </summary>
    [SaveDuringPlay, ExecuteAlways]
    public class CinemachineConfiner2D : CinemachineExtension
    {
        /// <summary>The 2D shape within which the camera is to be contained.</summary>
        [Tooltip("The 2D shape within which the camera is to be contained.  " +
                 "Can be a 2D polygon or 2D composite collider.")]
        public Collider2D m_BoundingShape2D;

        /// <summary>Damping applied automatically around corners to avoid jumps.</summary>
        [Tooltip("Damping applied automatically around corners to avoid jumps.  "
                 + "Higher numbers are more gradual.")]
        [Range(0, 5)]
        public float m_CornerDamping = 0;
        private bool m_CornerDampingIsOn = false;
        private float m_CornerDampingSpeedup = 1f;
        private float m_CornerAngleTreshold = 10f;
        /// <summary>Damping applied when getting within the specified proximity to the sides.</summary>
        [Tooltip("Damping applied when getting within the specified proximity to the sides.  When side damping is " +
                 "enabled, then corner damping must be enabled with equal or greater value.")]
        [Range(0, 5)]
        public float m_SideDamping = 0;
        
        /// <summary>User specified proximity used by Side Damping</summary>
        [Tooltip("Proximity used by Side Damping")]
        [Range(0, 100)]
        public float m_SideDampingProximity = 10;
        private float m_SideDampingTime;
        private bool m_SideDampingOn = false;

        // advanced features
        public bool m_DrawGizmos = false;
        [HideInInspector, SerializeField] internal bool m_AutoBake = true; // TODO: remove
                                                                           // reason: if user wants to
                                                                           // switch between cameras, it is better
                                                                           // to just have a different advnaced confiner
                                                                           // for each setup
        [HideInInspector, SerializeField] internal bool m_TriggerBake = false;
        [HideInInspector, SerializeField] internal bool m_TriggerClearCache = false;
        [HideInInspector, SerializeField] internal float m_MaxOrthoSize;
        [HideInInspector, SerializeField] internal bool m_ShrinkToPointsExperimental;
        
        private static readonly float m_bakedConfinerResolution = 0.005f;
        
        internal enum BakeProgressEnum { EMPTY, BAKING, BAKED, INVALID_CACHE } // TODO: remove states after
                                                                               // fist pass cleanup!
        [HideInInspector, SerializeField] internal BakeProgressEnum BakeProgress = BakeProgressEnum.INVALID_CACHE;

        private List<List<Vector2>> m_originalPath;
        private List<List<Vector2>> m_originalPathCache;
        private int m_originalPathTotalPointCount;
        
        private float m_frustumHeightCache;
        private List<List<Vector2>> m_currentPathCache;

        private List<ConfinerOven.ConfinerState> m_confinerStates;
        private ConfinerOven m_confinerBaker = null;

        /// <summary>
        /// Trigger rebake process manually.
        /// The confiner rebakes iff an input parameters affecting the outcome of the baked result change.
        /// </summary>
        private void Bake()
        {
            m_TriggerBake = true;
        }

        /// <summary>Force rebake manually. This function invalidates the cache and rebakes the confiner.</summary>
        public void ForceBake()
        {
            InvalidatePathCache();
            Bake();
        }

        private void OnValidate()
        {
            m_CornerDamping = Mathf.Clamp(m_CornerDamping, m_SideDamping, float.MaxValue);
        }

        private Vector3 prevPosition = Vector3.zero;
        private Vector2 prevDampVector = Vector2.zero;
        private float sideDampingCatchupSpeed = 0;
        private float catchupTimer = 0;
        protected override void PostPipelineStageCallback(CinemachineVirtualCameraBase vcam, 
            CinemachineCore.Stage stage, ref CameraState state, float deltaTime)
        {
            if (stage == CinemachineCore.Stage.Body)
            {
                if (!ValidateConfinerStateCache(state.Lens.Aspect, out bool confinerStateChanged))
                {
                    return; // invalid path
                }
                
                float frustumHeight = CalculateFrustumHeight(state, vcam);
                ValidatePathCache(confinerStateChanged, frustumHeight);

                var extra = GetExtraState<VcamExtraState>(vcam);
                Vector3 displacement = ConfinePoint(state.CorrectedPosition);
                if (VirtualCamera.PreviousStateIsValid && deltaTime >= 0)
                { 
                    float displacementAngle = Vector2.Angle(extra.m_previousDisplacement, displacement);
                    if (m_CornerDampingIsOn || m_CornerDamping > 0 && displacementAngle > m_CornerAngleTreshold)
                    {
                        Vector3 delta = displacement - extra.m_previousDisplacement;
                        var deltaDamped = 
                            Damper.Damp(delta, m_CornerDamping / m_CornerDampingSpeedup, deltaTime);
                        displacement = extra.m_previousDisplacement + deltaDamped;

                        m_CornerDampingSpeedup = displacementAngle < 1f ? 2f : 1f;
                        m_CornerDampingIsOn = displacementAngle > UnityVectorExtensions.Epsilon ||
                                              delta.sqrMagnitude > UnityVectorExtensions.Epsilon;
                    }
                }
                extra.m_previousDisplacement = displacement;
                state.PositionCorrection += displacement;
                extra.confinerDisplacement = displacement.magnitude;
                
                if (!m_CornerDampingIsOn && m_SideDamping > 0)
                {
                    Vector3 delta = state.CorrectedPosition - prevPosition;

                    state.PositionCorrection -= delta;
                    
                    GetDampVectorBasedOnDirection(state.CorrectedPosition, delta,
                        in m_currentPathCache, in m_SideDampingProximity,
                        out Vector2 dampVector);

                    dampVector = dampVector.Abs();
                    if (dampVector == Vector2.zero)
                    {
                        sideDampingCatchupSpeed = Mathf.Max(1, delta.sqrMagnitude) * 2f;
                        sideDampingCatchupSpeed = Mathf.Lerp(1, sideDampingCatchupSpeed, catchupTimer);
                        var multiplier = 1f / m_SideDamping; // TODO: test
                        catchupTimer += deltaTime * multiplier;
                    }
                    else
                    {
                        catchupTimer = 0;
                        prevDampVector = dampVector;
                    }

                    bool zeroDampVector = dampVector.sqrMagnitude > UnityVectorExtensions.Epsilon;
                    if (m_SideDampingOn || zeroDampVector)
                    {
                        if (zeroDampVector)
                        {
                            m_SideDampingTime += deltaTime;
                            m_SideDampingOn = true;
                            if (m_SideDampingTime >= 1)
                            {
                                m_SideDampingTime = 1;
                            }
                        }
                        else
                        {
                            m_SideDampingTime -= deltaTime;
                            if (m_SideDampingTime <= 0)
                            {
                                m_SideDampingTime = 0;
                                m_SideDampingOn = false;
                            }
                        }
                        float sideSmoothingValue =
                            Mathf.Lerp(0, m_SideDamping, m_SideDampingTime);
                        if (dampVector.x > UnityVectorExtensions.Epsilon)
                        {
                            dampVector.x = Mathf.Max(dampVector.x, 1f);
                            delta.x = Damper.Damp(delta.x, sideSmoothingValue, deltaTime * dampVector.x);
                        }
                        else
                        {
                            if (dampVector.x < UnityVectorExtensions.Epsilon && 
                                prevDampVector.x > UnityVectorExtensions.Epsilon)
                            {
                                delta.x = Damper.Damp(delta.x, sideSmoothingValue, deltaTime * sideDampingCatchupSpeed);
                            }
                            else 
                            {
                                delta.x = Damper.Damp(delta.x, 0, deltaTime);
                            }
                        }

                        if (dampVector.y > UnityVectorExtensions.Epsilon)
                        {
                            dampVector.y = Mathf.Max(dampVector.y, 1f);
                            delta.y = Damper.Damp(delta.y, sideSmoothingValue, deltaTime * dampVector.y);
                        }
                        else
                        {
                            if (dampVector.y < UnityVectorExtensions.Epsilon && 
                                prevDampVector.y > UnityVectorExtensions.Epsilon)
                            {
                                delta.y = Damper.Damp(delta.y, sideSmoothingValue, deltaTime * sideDampingCatchupSpeed);
                            }
                            else
                            {
                                delta.y = Damper.Damp(delta.y, 0, deltaTime);
                            }
                        }
                    }
                    state.PositionCorrection += delta;
                }
                
                prevPosition = state.CorrectedPosition;
            }
        }

        // <summary>
        /// Calculates Frustum Height for orthographic or perspective camera.
        /// Ascii illustration of Frustum Height:
        ///  |----+----+  -\
        ///  |    |    |    } frustumHeight = cameraWindowHeight / 2
        ///  |---------+  -/
        ///  |    |    |
        ///  |---------|
        /// </summary>
        private float CalculateFrustumHeight(in CameraState state, in CinemachineVirtualCameraBase vcam)
        {
            float frustumHeight;
            if (state.Lens.Orthographic)
            {
                frustumHeight = Mathf.Abs(state.Lens.OrthographicSize);
            }
            else
            {
                Quaternion inverseRotation = Quaternion.Inverse(m_BoundingShape2D.transform.rotation);
                Vector3 planePosition = inverseRotation * m_BoundingShape2D.transform.position;
                Vector3 cameraPosition = inverseRotation * vcam.transform.position;
                float distance = Mathf.Abs(planePosition.z - cameraPosition.z);
                frustumHeight = distance * Mathf.Tan(state.Lens.FieldOfView * 0.5f * Mathf.Deg2Rad);
            }
            return frustumHeight;
        }


        /// <summary>
        /// Returns a 2D damping vector, that defines a component-wise damping. If a component is 0, that means no
        /// damping in that directions. For example, (0,0) means no damping in either X or Y directions, and (1,1) means
        /// damping in both.
        /// Damping Vector is based on the closest edges in the direction the player is moving. The direction is
        /// decomposed into an X and Y component. These are used to find the closest two edges.
        /// </summary>
        /// <param name="position">Position of the player</param>
        /// <param name="direction">Direction of the player's velocity</param>
        /// <param name="polygons">Polygons that define the confiner area</param>
        /// <param name="proximity">How far to check for edges</param>
        /// <param name="dampVector">2D Vector defining damping to be applied on player</param>
        private void GetDampVectorBasedOnDirection(
            Vector2 position, Vector2 direction, in List<List<Vector2>> polygons, in float proximity,
            out Vector2 dampVector)
        {
            dampVector = Vector2.zero;
            
            var horizontalSearchVector = new Vector2(Math.Abs(direction.x) < UnityVectorExtensions.Epsilon ? 
                0 : 
                Mathf.Sign(direction.x) * proximity, 0); 
            var verticalSearchVector = new Vector2(0, Math.Abs(direction.y) < UnityVectorExtensions.Epsilon ? 
                0 : 
                Mathf.Sign(direction.y) * proximity);
            
            if (direction.sqrMagnitude < UnityVectorExtensions.Epsilon)
            {
                return;
            }
            direction.Normalize();
            
            var normalH = Vector2.zero;
            var normalV = Vector2.zero;
            float minHorizontalDistance = proximity;
            float minVerticalDistance = proximity;
            for (var i = 0; i < polygons.Count; i++)
            {
                for (var p = 0; p < polygons[i].Count; p++)
                {
                    int nextP = (p + 1) % polygons[i].Count;
                    UnityVectorExtensions.FindIntersection(
                        position, position + horizontalSearchVector,
                        polygons[i][p], polygons[i][nextP],
                        out _, out bool segmentsIntersect, out _);

                    if (segmentsIntersect)
                    {
                        var horizontalDistance = UnityVectorExtensions.DistanceBetweenPointAndLineSegment(position, 
                            polygons[i][p],
                            polygons[i][nextP],
                            out float onSegment);

                        if (horizontalDistance < minHorizontalDistance)
                        {
                            minHorizontalDistance = horizontalDistance;
                            var edge = polygons[i][p] - polygons[i][nextP];
                            if (onSegment < 0)
                            {
                                normalH = position - polygons[i][p];
                            }
                            else if (onSegment > 1)
                            {
                                normalH = position - polygons[i][nextP];
                            }
                            else
                            {
                                normalH = new Vector2(edge.y, -edge.x);
                            }
                        }
                    }
                    UnityVectorExtensions.FindIntersection(
                        position, position + verticalSearchVector, 
                        polygons[i][p], polygons[i][nextP],
                        out _, out segmentsIntersect, out _);

                    if (segmentsIntersect)
                    {
                        var verticalDistance = UnityVectorExtensions.DistanceBetweenPointAndLineSegment(position, 
                            polygons[i][p],
                            polygons[i][nextP],
                            out float onSegment);

                        if (verticalDistance < minVerticalDistance)
                        {
                            minVerticalDistance = verticalDistance;
                            var edge = polygons[i][p] - polygons[i][nextP];
                            if (onSegment < 0)
                            {
                                normalV = position - polygons[i][p];
                            }
                            else if (onSegment > 1)
                            {
                                normalV = position - polygons[i][nextP];
                            }
                            else
                            {
                                normalV = new Vector2(edge.y, -edge.x);
                            }
                        }
                    }
                }
            }

            if (minHorizontalDistance >= proximity)
            {
                normalH = Vector2.zero;
            }
            else
            {
                normalH = normalH.normalized * minHorizontalDistance;
            }

            if (minVerticalDistance >= proximity)
            {
                normalV = Vector2.zero;
            }
            else
            {
                normalV = normalV.normalized * minVerticalDistance;
            }

            dampVector = (normalH + normalV).normalized;
        }

        /// <summary>
        /// Confines input 2D point within the confined area.
        /// </summary>
        /// <param name="positionToConfine">2D point to confine</param>
        /// <returns>Confined position</returns>
        private Vector2 ConfinePoint(Vector2 positionToConfine)
        {
            if (ShrinkablePolygon.IsInside(m_currentPathCache, positionToConfine))
            {
                return Vector2.zero;
            }

            Vector2 closest = positionToConfine;
            float minDistance = float.MaxValue;
            for (int i = 0; i < m_currentPathCache.Count; ++i)
            {
                int numPoints = m_currentPathCache[i].Count;
                if (numPoints > 0)
                {
                    Vector2 v0 = m_currentPathCache[i][numPoints - 1];
                    for (int j = 0; j < numPoints; ++j)
                    {
                        Vector2 v = m_currentPathCache[i][j];
                        Vector2 c = Vector2.Lerp(v0, v, positionToConfine.ClosestPointOnSegment(v0, v));
                        float distance = Vector2.SqrMagnitude(positionToConfine - c);
                        if (distance < minDistance)
                        {
                            minDistance = distance;
                            closest = c;
                        }
                        v0 = v;
                    }
                }
            }
            return closest - positionToConfine;
        }
        
        private class VcamExtraState
        {
            public Vector3 m_previousDisplacement;
            public float confinerDisplacement;
            public bool applyAfterAim;
        };

        private float m_sensorRatioCache;
        private Vector3 m_boundingShapePositionCache;
        private Vector3 m_boundingShapeScaleCache;
        private Quaternion m_boundingShapeRotationCache;
        /// <summary>
        /// Invalidates path cache.
        /// </summary>
        private void InvalidatePathCache()
        {
            m_originalPath = null;
            m_originalPathCache = null;
            m_sensorRatioCache = 0;
            m_boundingShapePositionCache = Vector3.negativeInfinity;
            m_boundingShapeScaleCache = Vector3.negativeInfinity;
            m_boundingShapeRotationCache = new Quaternion(0,0,0,0);
        }
        
        private float m_bakedConfinerResolutionCache;
        /// <summary>
        /// Checks if we have a valid confiner state cache. Calculates it if cache is invalid, and bake was requested.
        /// </summary>
        /// <param name="sensorRatio">Camera window ratio (width / height)</param>
        /// <param name="confinerStateChanged">True, if the baked confiner state has changed. False, otherwise.</param>
        /// <returns>True, if path is baked and valid. False, if path is invalid or non-existent.</returns>
        private bool ValidateConfinerStateCache(float sensorRatio, out bool confinerStateChanged)
        {
            if (m_TriggerClearCache)
            {
                InvalidatePathCache();
                m_TriggerClearCache = false;
            }
            
            confinerStateChanged = false;
            bool cacheIsEmpty = m_confinerStates == null;
            bool cacheIsValid = 
                m_originalPath != null && // first time?
                !cacheIsEmpty && // has a prev. baked result?
                !BoundingShapeTransformChanged() && // confiner was moved or rotated or scaled?
                Math.Abs(m_sensorRatioCache - sensorRatio) < UnityVectorExtensions.Epsilon && // sensor ratio changed?
                Math.Abs(m_bakedConfinerResolution - m_bakedConfinerResolutionCache) < UnityVectorExtensions.Epsilon; // resolution changed?
            if (!m_AutoBake && !m_TriggerBake)
            {
                if (cacheIsEmpty)
                {
                    BakeProgress = BakeProgressEnum.EMPTY;
                    return false; // if m_confinerStates is null, then we don't have path -> false
                }
                else if (!cacheIsValid)
                {
                    BakeProgress = BakeProgressEnum.INVALID_CACHE;
                    return true;
                }
                else
                {
                    BakeProgress = BakeProgressEnum.BAKED;
                    return true;
                }
            }
            else if (!cacheIsEmpty && cacheIsValid)
            {
                m_TriggerBake = false;
                BakeProgress = BakeProgressEnum.BAKED;
                return true;
            }
            
            m_TriggerBake = false;
            BakeProgress = BakeProgressEnum.BAKING;
            confinerStateChanged = true;

            bool boundingShapeTransformChanged = BoundingShapeTransformChanged();
            if (boundingShapeTransformChanged || m_originalPath == null)
            {
                Type colliderType = m_BoundingShape2D == null ? null:  m_BoundingShape2D.GetType();
                if (colliderType == typeof(PolygonCollider2D))
                {
                    PolygonCollider2D poly = m_BoundingShape2D as PolygonCollider2D;
                    if (boundingShapeTransformChanged || m_originalPath == null || 
                        m_originalPath.Count != poly.pathCount || 
                        m_originalPathTotalPointCount != poly.GetTotalPointCount())
                    { 
                        m_originalPath = new List<List<Vector2>>();
                        for (int i = 0; i < poly.pathCount; ++i)
                        {
                            Vector2[] path = poly.GetPath(i);
                            List<Vector2> dst = new List<Vector2>();
                            for (int j = 0; j < path.Length; ++j)
                            {
                                dst.Add(m_BoundingShape2D.transform.TransformPoint(path[j]));
                            }
                            m_originalPath.Add(dst);
                        }
                        m_originalPathTotalPointCount = poly.GetTotalPointCount();
                    }
                }
                else if (colliderType == typeof(CompositeCollider2D))
                {
                    CompositeCollider2D poly = m_BoundingShape2D as CompositeCollider2D;
                    if (boundingShapeTransformChanged || m_originalPath == null || 
                        m_originalPath.Count != poly.pathCount || m_originalPathTotalPointCount != poly.pointCount)
                    {
                        m_originalPath = new List<List<Vector2>>();
                        Vector2[] path = new Vector2[poly.pointCount];
                        Vector3 lossyScale = m_BoundingShape2D.transform.lossyScale;
                        Vector2 revertCompositeColliderScale = new Vector2(
                            1f / lossyScale.x, 
                            1f / lossyScale.y);
                        for (int i = 0; i < poly.pathCount; ++i)
                        {
                            int numPoints = poly.GetPath(i, path);
                            List<Vector2> dst = new List<Vector2>();
                            for (int j = 0; j < numPoints; ++j)
                            {
                                dst.Add(m_BoundingShape2D.transform.TransformPoint(
                                    path[j] * revertCompositeColliderScale));
                            }
                            m_originalPath.Add(dst);
                        }
                        m_originalPathTotalPointCount = poly.pointCount;
                    }
                }
                else
                {
                    BakeProgress = BakeProgressEnum.INVALID_CACHE;
                    InvalidatePathCache();
                    return false; // input collider is invalid
                }
            }

            m_bakedConfinerResolutionCache = m_bakedConfinerResolution;
            m_sensorRatioCache = sensorRatio;
            GetConfinerOven().BakeConfiner(m_originalPath, m_sensorRatioCache, m_bakedConfinerResolutionCache, 
                m_MaxOrthoSize, m_ShrinkToPointsExperimental);
            m_confinerStates = GetConfinerOven().GetShrinkablePolygonsAsConfinerStates();

            m_boundingShapePositionCache = m_BoundingShape2D.transform.position;
            m_boundingShapeRotationCache = m_BoundingShape2D.transform.rotation;
            m_boundingShapeScaleCache = m_BoundingShape2D.transform.localScale;

            BakeProgress = BakeProgressEnum.BAKED;
            return true;
        }

        /// <summary>
        /// Checks if the input bounding shape was moved, rotated, or scaled.
        /// </summary>
        /// <returns></returns>
        private bool BoundingShapeTransformChanged()
        {
            return m_BoundingShape2D != null && 
                   (m_boundingShapePositionCache != m_BoundingShape2D.transform.position ||
                    m_boundingShapeScaleCache != m_BoundingShape2D.transform.localScale ||
                    m_boundingShapeRotationCache != m_BoundingShape2D.transform.rotation);
        }
        
        private ConfinerOven.ConfinerState m_confinerCache;
        /// <summary>
        /// Check that the path cache was converted from the current confiner cache, or
        /// converts it if the frustum height was changed.
        /// </summary>
        /// <param name="confinerStateChanged">Confiner cache was changed</param>
        /// <param name="frustumHeight">Camera frustum height</param>
        private void ValidatePathCache(bool confinerStateChanged, float frustumHeight)
        {
            if (confinerStateChanged ||
                m_currentPathCache == null || 
                Math.Abs(frustumHeight - m_frustumHeightCache) > m_bakedConfinerResolution)
            {
                m_frustumHeightCache = frustumHeight;
                m_confinerCache = GetConfinerOven().GetConfinerAtFrustumHeight(m_frustumHeightCache);
                ShrinkablePolygon.ConvertToPath(m_confinerCache.polygons, m_frustumHeightCache, out m_currentPathCache);
            }
        }
        
        /// <summary>
        /// Singleton for this object's ConfinerOven.
        /// </summary>
        /// <returns>ConfinerOven</returns>
        private ConfinerOven GetConfinerOven()
        {
            if (m_confinerBaker == null)
            {
                m_confinerBaker = new ConfinerOven();
            }

            return m_confinerBaker;
        }
        
        protected override void OnEnable()
        {
            base.OnEnable();
            ForceBake();
        }
        
        private void OnDrawGizmos()
        {
            if (!m_DrawGizmos) return;
            if (m_currentPathCache == null || m_BoundingShape2D == null) return;
            
            Gizmos.color = Color.cyan;
            foreach (var path in m_currentPathCache)
            {
                for (var index = 0; index < path.Count; index++)
                {
                    Gizmos.DrawLine(
                        path[index], 
                        path[(index + 1) % path.Count]);
                }
            }
            
            Handles.color = Color.green;
            Handles.DrawWireDisc(prevPosition, Vector3.back, m_SideDampingProximity);
        }
    }
#endif
}