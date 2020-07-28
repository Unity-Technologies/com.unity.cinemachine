using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Cinemachine
{
    /// <summary>Converts List of graph states to CompositeCollider2Ds that will be used by CinemachineConfiner.</summary>
    class GraphToCompositeCollider
    {
        private CompositeCollider2D _compositeCollider2D;
        
        /// <summary>
        /// Initializes GraphToCompositeCollider and creates the collider holders BakedConfiner parented to parent.
        /// </summary>
        internal GraphToCompositeCollider(Transform parent)
        {
            var confinerHolder = new GameObject("BakedConfiner");
            confinerHolder.transform.parent = parent;

            var rigidbody2D = confinerHolder.AddComponent<Rigidbody2D>();
            rigidbody2D.bodyType = RigidbodyType2D.Static;
            rigidbody2D.simulated = false;

            _compositeCollider2D = confinerHolder.AddComponent<CompositeCollider2D>();
            _compositeCollider2D.geometryType = CompositeCollider2D.GeometryType.Polygons;
        }
        
        // TODO: then test if lerping works -> works completely in parallel
        // TODO: when switching from stateX to stateY, we need to disable composite collider of stateX and enable stateY's.
        
        /// <summary>Converts a List<List<Graph> into composite colliders. The outher list represents
        /// different states in pairs: 0-1 is one state, 2-3 another, etc. Between states we can lerp.
        /// The inner list of graphs represent polygon colliders that need to be unioned.
        /// </summary>
        internal void Convert(in List<ConfinerState> confinerStates, in Vector2 graphOffset)
        {
            for (var gs = 0; gs < confinerStates.Count; gs++)
            {
                var confinerState = confinerStates[gs];
                var polygonHolder = new GameObject("PolygonCollider2Ds - " + confinerState.cameraWindowDiagonal);
                polygonHolder.transform.position = graphOffset;
                polygonHolder.transform.parent = _compositeCollider2D.transform;
                
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
    }
}