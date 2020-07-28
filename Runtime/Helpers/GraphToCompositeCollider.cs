using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Cinemachine
{
    /// <summary>Converts List of graph states to CompositeCollider2Ds that will be used by CinemachineConfiner.</summary>
    class GraphToCompositeCollider
    {
        private GameObject confinerHolder;
        private List<GameObject> compositeColliders;
        
        /// <summary>
        /// Initializes GraphToCompositeCollider and creates the collider holders BakedConfiner parented to parent.
        /// </summary>
        internal GraphToCompositeCollider(Transform parent)
        {
            confinerHolder = new GameObject("BakedConfiner");
            confinerHolder.transform.parent = parent;
            confinerHolder.transform.localPosition = Vector3.zero;
        }
        
        // TODO: then test if lerping works -> works completely in parallel
        // TODO: when switching from stateX to stateY, we need to disable composite collider of stateX and enable stateY's.
        
        /// <summary>Converts a List<List<Graph> into composite colliders. The outher list represents
        /// different states in pairs: 0-1 is one state, 2-3 another, etc. Between states we can lerp.
        /// The inner list of graphs represent polygon colliders that need to be unioned.
        /// </summary>
        internal void Convert(in List<ConfinerState> confinerStates, in Vector2 graphOffset)
        {
            compositeColliders = new List<GameObject>(confinerStates.Count);
            for (var gs = 0; gs < confinerStates.Count; gs++)
            {
                var confinerState = confinerStates[gs];
                var compositeHolder = new GameObject("CompositeCollider2D - " + confinerState.cameraWindowDiagonal);
                compositeHolder.transform.position = graphOffset;
                compositeHolder.transform.parent = confinerHolder.transform;
                var rigidbody2D = compositeHolder.AddComponent<Rigidbody2D>();
                rigidbody2D.bodyType = RigidbodyType2D.Static;
                rigidbody2D.simulated = false;
                var compositeCollider2D = compositeHolder.AddComponent<CompositeCollider2D>();
                compositeCollider2D.geometryType = CompositeCollider2D.GeometryType.Polygons;
                compositeColliders.Add(compositeHolder);
                
                var polygonHolder = new GameObject("PolygonCollider2Ds");
                polygonHolder.transform.parent = compositeHolder.transform;
                polygonHolder.transform.localPosition = Vector3.zero;

                foreach (var graph in confinerState.graphs)
                {
                    var polygon = polygonHolder.AddComponent<PolygonCollider2D>();
                    polygon.usedByComposite = true;
                    polygon.points = graph.points.Select(x => x.position).ToArray();

                    foreach (var intersectionPoint in graph.intersectionPoints)
                    {
                        Vector2 closestPoint = graph.ClosestPoint(intersectionPoint);
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
            }
        }

        public class FovBakedConfiners
        {
            public float fov;
            public List<List<Vector2>> path;
        }

        // TODO: return List< FOV, List<List<Vector>> >
        internal List<FovBakedConfiners> GetBakedConfiners()
        {
            // todo: cache this
            List<FovBakedConfiners> bakedConfiners = new List<FovBakedConfiners>();
            foreach (var compositeCollider in compositeColliders)
            {
                var compositeCollider2D = compositeCollider.GetComponent<CompositeCollider2D>();
                List<List<Vector2>> pathPoints = new List<List<Vector2>>();
                for (int i = 0; i < compositeCollider2D.pathCount; ++i)
                {
                    Vector2[] points = new Vector2[compositeCollider2D.pointCount];
                    compositeCollider2D.GetPath(i, points);
                    
                    pathPoints.Add(new List<Vector2>(points));
                }
                
                bakedConfiners.Add(new FovBakedConfiners
                {
                    fov = 1,
                    path = pathPoints,
                });
            }

            return bakedConfiners;
        }
    }
}