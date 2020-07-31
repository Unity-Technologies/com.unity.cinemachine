using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Cinemachine
{
    /// <summary>Converts List of graph states to CompositeCollider2Ds that will be used by CinemachineConfiner.</summary>
    class ConfinerStateToPath
    {
        private GameObject confinerHolder;
        private Transform parent;
        private GameObject compositeColliderHolder;
        
        /// <summary>
        /// Initializes ConfinerStateToPath and creates the collider holders BakedConfiner parented to parent.
        /// </summary>

        private void CleanBakedConfiner()
        {
            if (confinerHolder != null)
            {
                if (Application.isEditor)
                {
                    // for(int i = confinerHolder.transform.childCount - 1; i >= 1; i--)
                    // {
                    //     Object.DestroyImmediate(confinerHolder.transform.GetChild(i).gameObject);
                    // }

                    Object.DestroyImmediate(confinerHolder);
                }
                else
                {
                    // for(int i = confinerHolder.transform.childCount - 1; i >= 1; i--)
                    // {
                    //     Object.Destroy(confinerHolder.transform.GetChild(i).gameObject);
                    // }
                    
                    Object.Destroy(confinerHolder);
                }
            }
            
            confinerHolder = new GameObject("CMBakedConfiner");
            confinerHolder.transform.position = Vector3.zero;
        }
        
        // TODO: then test if lerping works -> works completely in parallel
        // TODO: when switching from stateX to stateY, we need to disable composite collider of stateX and enable stateY's.
        
        /// <summary>Converts a List<List<Graph> into composite colliders. The outher list represents
        /// different states in pairs: 0-1 is one state, 2-3 another, etc. Between states we can lerp.
        /// The inner list of graphs represent polygon colliders that need to be unioned.
        /// </summary>
        internal void Convert(ConfinerState confinerState, in Vector2 graphOffset, 
            out List<List<Vector2>> path, out Collider2D collider2D)
        {
            CleanBakedConfiner();
            confinerHolder = new GameObject("CMBakedConfiner");
            confinerHolder.transform.position = Vector3.zero;

            compositeColliderHolder = new GameObject("CompositeCollider2D");
            compositeColliderHolder.transform.position = graphOffset;
            compositeColliderHolder.transform.parent = confinerHolder.transform;
            var rigidbody2D = compositeColliderHolder.AddComponent<Rigidbody2D>();
            rigidbody2D.bodyType = RigidbodyType2D.Static;
            rigidbody2D.simulated = false;
            var compositeCollider2D = compositeColliderHolder.AddComponent<CompositeCollider2D>();
            compositeCollider2D.geometryType = CompositeCollider2D.GeometryType.Polygons;
            
            var polygonHolder = new GameObject("PolygonCollider2Ds");
            polygonHolder.transform.parent = compositeColliderHolder.transform;
            polygonHolder.transform.localPosition = Vector2.zero;

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
            
            
            path = new List<List<Vector2>>();
            for (int i = 0; i < compositeCollider2D.pathCount; ++i)
            {
                Vector2[] points = new Vector2[compositeCollider2D.GetPathPointCount(i)];
                compositeCollider2D.GetPath(i, points);

                path.Add(new List<Vector2>(points));
            }

            collider2D = compositeCollider2D;
        }

        public class FovBakedConfiners
        {
            public float orthographicSize;
            public List<List<Vector2>> path;
        }

        // TODO: return List< windowSize, List<List<Vector>> >
        // internal List<FovBakedConfiners> GetBakedConfiners()
        // {
        //     // todo: cache this
        //     List<FovBakedConfiners> bakedConfiners = new List<FovBakedConfiners>();
        //     foreach (var compositeColliderHolder in compositeColliderHolder)
        //     {
        //         var compositeCollider2D = compositeColliderHolder.GetComponent<CompositeCollider2D>();
        //         var fovStr = compositeCollider2D.name.Substring("CompositeCollider2D - ".Length);
        //         var FOV = float.Parse(fovStr);
        //         List<List<Vector2>> pathPoints = new List<List<Vector2>>();
        //         for (int i = 0; i < compositeCollider2D.pathCount; ++i)
        //         {
        //             Vector2[] points = new Vector2[compositeCollider2D.pointCount];
        //             compositeCollider2D.GetPath(i, points);
        //             
        //             pathPoints.Add(new List<Vector2>(points));
        //         }
        //         
        //         bakedConfiners.Add(new FovBakedConfiners
        //         {
        //             orthographicSize = FOV,
        //             path = pathPoints,
        //         });
        //     }
        //
        //     return bakedConfiners;
        // }
    }
}