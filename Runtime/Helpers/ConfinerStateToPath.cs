using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Cinemachine
{
    /// <summary>
    /// Converts confiner state to CompositeCollider2Ds to calculate the union of the graphs making up the confiner state.
    /// Then, it converts the CompositeCollider2D into a List of points.
    /// </summary>
    class ConfinerStateToPath
    {
        private Transform parent;
        private GameObject compositeColliderHolder;
        private CompositeCollider2D compositeCollider2D;
        private GameObject polygonHolder;

        private string nametag;
        private static int ID = 0;
        private int myID = 0;

        public ConfinerStateToPath(string vcamNametag)
        {
            nametag = vcamNametag;
            compositeColliderHolder = GameObject.Find("CMBakedConfiner for " + nametag +" - "+ myID);
        }

        private void Cleanup()
        {
            if (compositeColliderHolder != null)
                if (Application.isPlaying)
                {
                    Object.Destroy(compositeColliderHolder.gameObject);
                }
                else
                {
                    Object.DestroyImmediate(compositeColliderHolder.gameObject);
                }

            compositeColliderHolder = null;
            compositeCollider2D = null;
            polygonHolder = null;
        }
        
        private void InitializeCompositeColliderHolder()
        {
            if (compositeColliderHolder == null || compositeCollider2D == null)
            {
                Cleanup();
                
                myID = ID; ID++;
                compositeColliderHolder = new GameObject("CMBakedConfiner for " + nametag +" - "+ myID);
                compositeColliderHolder.hideFlags = HideFlags.HideInHierarchy;
            
                var rigidbody2D = compositeColliderHolder.AddComponent<Rigidbody2D>();
                rigidbody2D.bodyType = RigidbodyType2D.Static;
                rigidbody2D.simulated = false;
                rigidbody2D.hideFlags = HideFlags.HideInHierarchy;
                
                compositeCollider2D = compositeColliderHolder.AddComponent<CompositeCollider2D>();
                compositeCollider2D.geometryType = CompositeCollider2D.GeometryType.Polygons;
            }

            if (polygonHolder != null)
            {
                if (Application.isPlaying)
                {
                    Object.Destroy(polygonHolder.gameObject);
                }
                else
                {
                    Object.DestroyImmediate(polygonHolder.gameObject);
                }
            }
        }
        
        
        /// <summary>Converts a List<List<Graph> into composite colliders. The outher list represents
        /// different states in pairs: 0-1 is one state, 2-3 another, etc. Between states we can lerp.
        /// The inner list of graphs represent polygon colliders that need to be unioned.
        /// </summary>
        internal void Convert(ConfinerState confinerState,
            out List<List<Vector2>> path, out Collider2D collider2D)
        {
            // TODO: performance optimization!
            // instead of creating these colliders, we could do polygon union directly
            // -> need to implement or port from Core.
            // Then, in ConfinePoint algorithm, we need to write our
            // own (m_BoundingCompositeShape2D.OverlapPoint(camPos)) instead of relying on the collider
            InitializeCompositeColliderHolder();
            
            polygonHolder = new GameObject("PolygonCollider2Ds");
            polygonHolder.transform.parent = compositeColliderHolder.transform;
            polygonHolder.hideFlags = HideFlags.NotEditable;
            foreach (var graph in confinerState.graphs)
            {
                var polygon = polygonHolder.AddComponent<PolygonCollider2D>();
                polygon.usedByComposite = true;
                polygon.points = graph.points.Select(x => x.position).ToArray();
                
                foreach (var intersectionPoint in graph.intersectionPoints)
                {
                    Vector2 closestPoint = graph.ClosestGraphPoint(intersectionPoint);
                    var intersectionPolygon = polygonHolder.AddComponent<PolygonCollider2D>();
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
            for (int i = 0; i < compositeCollider2D.pathCount; ++i)
            {
                Vector2[] points = new Vector2[compositeCollider2D.GetPathPointCount(i)];
                compositeCollider2D.GetPath(i, points);

                path.Add(new List<Vector2>(points));
            }

            collider2D = compositeCollider2D;
        }
    }
}