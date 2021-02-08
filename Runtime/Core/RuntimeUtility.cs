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
        /// <summary>
        /// Perform a raycast, but pass through any objects that have a given tag
        /// </summary>
        /// <param name="ray">The ray to cast</param>
        /// <param name="hitInfo">The returned results</param>
        /// <param name="rayLength">Length of the raycast</param>
        /// <param name="layerMask">Layers to include</param>
        /// <param name="ignoreTag">Tag to ignore</param>
        /// <returns>True if something was hit.  Results in hitInfo</returns>
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

        /// <summary>
        /// Perform a sphere cast, but pass through objects with a given tag
        /// </summary>
        /// <param name="rayStart">Start of the ray</param>
        /// <param name="radius">Radius of the sphere cast</param>
        /// <param name="dir">Direction of the ray</param>
        /// <param name="hitInfo">Results go here</param>
        /// <param name="rayLength">Length of the ray</param>
        /// <param name="layerMask">Layers to include</param>
        /// <param name="ignoreTag">Tag to ignore</param>
        /// <returns>True if something is hit.  Results in hitInfo.</returns>
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

        /// <summary>
        /// Normalize a curve so that its X and Y axes range from 0 to 1
        /// </summary>
        /// <param name="curve">Curve to normalize</param>
        /// <param name="normalizeX">If true, normalize the X axis</param>
        /// <param name="normalizeY">If true, normalize the Y axis</param>
        /// <returns>The normalized curve</returns>
        public static AnimationCurve NormalizeCurve(
            AnimationCurve curve, bool normalizeX, bool normalizeY)
        {
            if (!normalizeX && !normalizeY)
                return curve;
            Keyframe[] keys = curve.keys;
            if (keys.Length > 0)
            {
                float minTime = keys[0].time;
                float maxTime = minTime;
                float minVal = keys[0].value;
                float maxVal = minVal;
                for (int i = 0; i < keys.Length; ++i)
                {
                    minTime = Mathf.Min(minTime, keys[i].time);
                    maxTime = Mathf.Max(maxTime, keys[i].time);
                    minVal = Mathf.Min(minVal, keys[i].value);
                    maxVal = Mathf.Max(maxVal, keys[i].value);
                }
                float range = maxTime - minTime;
                float timeScale = range < 0.0001f ? 1 : 1 / range;
                range = maxVal - minVal;
                float valScale = range < 1 ? 1 : 1 / range;
                float valOffset = 0;
                if (range < 1)
                {
                    if (minVal > 0 && minVal + range <= 1)
                        valOffset = minVal;
                    else
                        valOffset = 1 - range;
                }
                for (int i = 0; i < keys.Length; ++i)
                {
                    if (normalizeX)
                        keys[i].time = (keys[i].time - minTime) * timeScale;
                    if (normalizeY)
                        keys[i].value = ((keys[i].value - minVal) * valScale) + valOffset;
                }
                curve.keys = keys;
            }
            return curve;
        }
    }
}

