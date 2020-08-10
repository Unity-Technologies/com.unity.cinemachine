using System.Collections.Generic;
using System.Linq;
using UnityEngine;

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

        private string nametag;

        public ConfinerStateToPath(string vcamNametag)
        {
            nametag = vcamNametag;
            compositeColliderHolder = GameObject.Find("CMBakedConfiner for " + nametag);
        }

        /// <summary>
        /// Initializes ConfinerStateToPath and creates the collider holders BakedConfiner parented to parent.
        /// </summary>
        private void CleanBakedConfiner()
        {
            if (compositeColliderHolder != null)
            {
                if (Application.isEditor)
                {
                    Object.DestroyImmediate(compositeColliderHolder);
                }
                else
                {
                    Object.Destroy(compositeColliderHolder);
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
            CleanBakedConfiner();
            compositeColliderHolder = new GameObject("CMBakedConfiner for " + nametag);
            compositeColliderHolder.hideFlags = HideFlags.HideInHierarchy;
            // compositeColliderHolder.transform.position = inputTransform.position;
            // compositeColliderHolder.transform.rotation = Quaternion.identity;
            
            var rigidbody2D = compositeColliderHolder.AddComponent<Rigidbody2D>();
            rigidbody2D.bodyType = RigidbodyType2D.Static;
            rigidbody2D.simulated = false;
            rigidbody2D.hideFlags = HideFlags.HideInHierarchy;
            var compositeCollider2D = compositeColliderHolder.AddComponent<CompositeCollider2D>();
            compositeCollider2D.geometryType = CompositeCollider2D.GeometryType.Polygons;
            
            var polygonHolder = new GameObject("PolygonCollider2Ds");
            polygonHolder.transform.parent = compositeColliderHolder.transform;
            // polygonHolder.transform.localPosition = Vector2.zero;
            // polygonHolder.transform.localRotation = inputTransform.rotation;
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