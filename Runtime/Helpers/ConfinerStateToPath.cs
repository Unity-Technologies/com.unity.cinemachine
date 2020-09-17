// TODO: to delete
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Cinemachine
{
    /// <summary>
    /// Converts confiner m_state to CompositeCollider2Ds to calculate the union of the graphs making up the confiner m_state.
    /// Then, it converts the CompositeCollider2D into a List of m_points.
    /// </summary>
    class ConfinerStateToPath
    {
        private Transform m_parent;
        private GameObject m_compositeColliderHolder;
        private CompositeCollider2D m_compositeCollider2D;
        private GameObject m_polygonHolder;

        private string m_nametag;
        private static int m_ID = 0;
        private int m_myID = 0;

        public ConfinerStateToPath(string vcamMNametag)
        {
            m_nametag = vcamMNametag;
            m_compositeColliderHolder = GameObject.Find("CMBakedConfiner for " + m_nametag +" - "+ m_myID);
        }

        private void Cleanup()
        {
            if (m_compositeColliderHolder != null)
                if (Application.isPlaying)
                {
                    Object.Destroy(m_compositeColliderHolder.gameObject);
                }
                else
                {
                    Object.DestroyImmediate(m_compositeColliderHolder.gameObject);
                }

            m_compositeColliderHolder = null;
            m_compositeCollider2D = null;
            m_polygonHolder = null;
        }
        
        private void InitializeCompositeColliderHolder()
        {
            if (m_compositeColliderHolder == null || m_compositeCollider2D == null)
            {
                Cleanup();
                
                m_myID = m_ID; m_ID++;
                m_compositeColliderHolder = new GameObject("CMBakedConfiner for " + m_nametag +" - "+ m_myID);
                //m_compositeColliderHolder.hideFlags = HideFlags.HideInHierarchy;
            
                var rigidbody2D = m_compositeColliderHolder.AddComponent<Rigidbody2D>();
                rigidbody2D.bodyType = RigidbodyType2D.Static;
                rigidbody2D.simulated = false;
                //rigidbody2D.hideFlags = HideFlags.HideInHierarchy;
                
                m_compositeCollider2D = m_compositeColliderHolder.AddComponent<CompositeCollider2D>();
                m_compositeCollider2D.geometryType = CompositeCollider2D.GeometryType.Polygons;
            }

            if (m_polygonHolder != null)
            {
                if (Application.isPlaying)
                {
                    Object.Destroy(m_polygonHolder.gameObject);
                }
                else
                {
                    Object.DestroyImmediate(m_polygonHolder.gameObject);
                }
            }
        }
        
        
        /// <summary>Converts a List<List<Graph> into composite colliders. The outher list represents
        /// different states in pairs: 0-1 is one m_state, 2-3 another, etc. Between states we can lerp.
        /// The inner list of graphs represent polygon colliders that need to be unioned.
        /// </summary>
        internal void Convert(ConfinerOven.ConfinerState confinerState,
            out List<List<Vector2>> path, out Collider2D collider2D)
        {
            // TODO: performance optimization!
            // instead of creating these colliders, we could do polygon union directly
            // -> need to implement or port from Core.
            // Then, in ConfinePoint algorithm, we need to write our
            // own (m_BoundingCompositeShape2D.OverlapPoint(camPos)) instead of relying on the collider
            InitializeCompositeColliderHolder();
            
            m_polygonHolder = new GameObject("PolygonCollider2Ds");
            m_polygonHolder.transform.parent = m_compositeColliderHolder.transform;
            //m_polygonHolder.hideFlags = HideFlags.NotEditable;
            foreach (var graph in confinerState.graphs)
            {
                var polygon = m_polygonHolder.AddComponent<PolygonCollider2D>();
                polygon.isTrigger = true;
                polygon.usedByComposite = true;
                polygon.points = graph.m_points.Select(x => x.m_position).ToArray();
                
                foreach (var intersectionPoint in graph.m_intersectionPoints)
                {
                    Vector2 closestPoint = graph.ClosestGraphPoint(intersectionPoint);
                    var intersectionPolygon = m_polygonHolder.AddComponent<PolygonCollider2D>();
                    intersectionPolygon.usedByComposite = true;

                    Vector2 direction = (closestPoint - intersectionPoint).normalized;
                    Vector2 epsilonNormal = new Vector2(direction.y, -direction.x) * 0.01f;

                    intersectionPolygon.points = new[]
                    {
                        closestPoint + epsilonNormal, intersectionPoint + epsilonNormal,
                        intersectionPoint - epsilonNormal, closestPoint - epsilonNormal
                    };
                }
            }

            path = new List<List<Vector2>>();
            for (int i = 0; i < m_compositeCollider2D.pathCount; ++i)
            {
                Vector2[] points = new Vector2[m_compositeCollider2D.GetPathPointCount(i)];
                m_compositeCollider2D.GetPath(i, points);

                path.Add(new List<Vector2>(points));
            }

            collider2D = m_compositeCollider2D;
        }
    }
}