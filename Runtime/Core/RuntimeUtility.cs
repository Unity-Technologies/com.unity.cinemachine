#if !UNITY_2019_3_OR_NEWER
#define CINEMACHINE_PHYSICS
#define CINEMACHINE_PHYSICS_2D
#endif

using System;
using UnityEngine;

namespace Cinemachine
{
    /// <summary>An ad-hoc collection of helpers, used by Cinemachine
    /// or its editor tools in various places</summary>
    [DocumentationSorting(DocumentationSortingAttribute.Level.Undoc)]
    public static class RuntimeUtility
    {
        /// <summary>Convenience to destroy an object, using the appropriate method depending 
        /// on whether the game is playing</summary>
        /// <param name="obj">The object to destroy</param>
        public static void DestroyObject(UnityEngine.Object obj)
        {
            if (obj != null)
            {
#if UNITY_EDITOR
                if (Application.isPlaying)
                    UnityEngine.Object.Destroy(obj);
                else
                    UnityEngine.Object.DestroyImmediate(obj);
#else
                UnityEngine.Object.Destroy(obj);
#endif
            }
        }

        /// <summary>
        /// Check whether a GameObject is a prefab.  
        /// For editor only - some things are disallowed if prefab.  In runtime, will always return false.
        /// </summary>
        /// <param name="gameObject">the object to check</param>
        /// <returns>If editor, checks if object is a prefab or prefab instance.  
        /// In runtime, returns false always</returns>
        public static bool IsPrefab(GameObject gameObject)
        {
#if UNITY_EDITOR
            return UnityEditor.PrefabUtility.GetPrefabInstanceStatus(gameObject)
                    != UnityEditor.PrefabInstanceStatus.NotAPrefab;
#else
            return false;
#endif
        }
        
        #if CINEMACHINE_PHYSICS
        public static bool RaycastIgnoreTag(
            Ray ray, out RaycastHit hitInfo, float rayLength, int layerMask, in string ignoreTag)
        {
            const float Epsilon = 0.001f;
            float extraDistance = 0;
            while (Physics.Raycast(
                ray, out hitInfo, rayLength, layerMask,
                QueryTriggerInteraction.Ignore))
            {
                if (ignoreTag.Length == 0 || !hitInfo.collider.CompareTag(ignoreTag))
                {
                    hitInfo.distance += extraDistance;
                    return true;
                }

                // Ignore the hit.  Pull ray origin forward in front of obstacle
                Ray inverseRay = new Ray(ray.GetPoint(rayLength), -ray.direction);
                if (!hitInfo.collider.Raycast(inverseRay, out hitInfo, rayLength))
                    break;
                float deltaExtraDistance = rayLength - (hitInfo.distance - Epsilon);
                if (deltaExtraDistance < Epsilon)
                    break;
                extraDistance += deltaExtraDistance;
                rayLength = hitInfo.distance - Epsilon;
                if (rayLength < Epsilon)
                    break;
                ray.origin = inverseRay.GetPoint(rayLength);
            }
            return false;
        }

        public static bool SphereCastIgnoreTag(
            Vector3 rayStart, float radius, Vector3 dir, 
            out RaycastHit hitInfo, float rayLength, 
            int layerMask, in string ignoreTag)
        {
            const float Epsilon = 0.001f;
            float extraDistance = 0;
            while (Physics.SphereCast(
                rayStart, radius, dir, out hitInfo, rayLength, layerMask,
                QueryTriggerInteraction.Ignore))
            {
                if (ignoreTag.Length == 0 || !hitInfo.collider.CompareTag(ignoreTag))
                {
                    hitInfo.distance += extraDistance;
                    return true;
                }

                // Ignore the hit.  Pull ray origin forward in front of obstacle
                Ray inverseRay = new Ray(rayStart + rayLength * dir, -dir);
                if (!hitInfo.collider.Raycast(inverseRay, out hitInfo, rayLength))
                    break;
                float deltaExtraDistance = rayLength - (hitInfo.distance - Epsilon);
                if (deltaExtraDistance < Epsilon)
                    break;
                extraDistance += deltaExtraDistance;
                rayLength = hitInfo.distance - Epsilon;
                if (rayLength < Epsilon)
                    break;
                rayStart = inverseRay.GetPoint(rayLength);
            }
            return false;
        }
#endif
    }
}

