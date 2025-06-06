using UnityEngine;

namespace Unity.Cinemachine
{
    /// <summary>An ad-hoc collection of helpers, used by Cinemachine
    /// or its editor tools in various places</summary>
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
        static RaycastHit[] s_HitBuffer = new RaycastHit[16];
        static int[] s_PenetrationIndexBuffer = new int[16];

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
            if (ignoreTag.Length == 0)
                return Physics.Raycast(ray, out hitInfo, rayLength, layerMask, QueryTriggerInteraction.Ignore);
            else
            {
                int closestHit = -1;
                int numHits = Physics.RaycastNonAlloc(
                    ray, s_HitBuffer, rayLength, layerMask, QueryTriggerInteraction.Ignore);
                for (int i = 0; i < numHits; ++i)
                {
                    if (s_HitBuffer[i].collider.CompareTag(ignoreTag))
                        continue;
                    if (closestHit < 0 || s_HitBuffer[i].distance < s_HitBuffer[closestHit].distance)
                        closestHit = i;
                }
                if (closestHit >= 0)
                {
                    hitInfo = s_HitBuffer[closestHit];
                    if (numHits == s_HitBuffer.Length)
                        s_HitBuffer = new RaycastHit[s_HitBuffer.Length * 2];   // full! grow for next time
                    return true;
                }
            }
            hitInfo = new RaycastHit();
            return false;
        }

        /// <summary>
        /// Perform a sphere cast, but pass through objects with a given tag
        /// </summary>
        /// <param name="ray">The ray to cast - this will be the center of the spherecast.</param>
        /// <param name="radius">Radius of the sphere cast</param>
        /// <param name="hitInfo">Results go here</param>
        /// <param name="rayLength">Length of the ray</param>
        /// <param name="layerMask">Layers to include</param>
        /// <param name="ignoreTag">Tag to ignore</param>
        /// <returns>True if something is hit.  Results in hitInfo.</returns>
        public static bool SphereCastIgnoreTag(
            Ray ray, float radius,
            out RaycastHit hitInfo, float rayLength,
            int layerMask, in string ignoreTag)
        {
            if (radius < UnityVectorExtensions.Epsilon)
                return RaycastIgnoreTag(ray, out hitInfo, rayLength, layerMask, ignoreTag);

            Vector3 rayStart = ray.origin;
            Vector3 dir = ray.direction;

            int closestHit = -1;
            int numPenetrations = 0;
            float penetrationDistanceSum = 0;
            int numHits = Physics.SphereCastNonAlloc(
                rayStart, radius, dir, s_HitBuffer, rayLength, layerMask,
                QueryTriggerInteraction.Ignore);
            for (int i = 0; i < numHits; ++i)
            {
                var h = s_HitBuffer[i];
                if (ignoreTag.Length > 0 && h.collider.CompareTag(ignoreTag))
                    continue;

                // Collect overlapping items
                if (h.distance == 0 && h.normal == -dir)
                {
                    // hitInfo for overlapping colliders will have special
                    // values that are not helpful to the caller.  Fix that here.
                    var scratchCollider = GetScratchCollider();
                    scratchCollider.radius = radius;
                    var c = h.collider;

                    if (Physics.ComputePenetration(
                        scratchCollider, rayStart, Quaternion.identity,
                        c, c.transform.position, c.transform.rotation,
                        out var offsetDir, out var offsetDistance))
                    {
                        h.point = rayStart + offsetDir * (offsetDistance - radius);
                        h.distance = offsetDistance - radius; // will be -ve
                        h.normal = offsetDir;
                        s_HitBuffer[i] = h;
                        if (h.distance < -0.0001f)
                        {
                            penetrationDistanceSum += h.distance;
                            if (s_PenetrationIndexBuffer.Length > numPenetrations + 1)
                                s_PenetrationIndexBuffer[numPenetrations++] = i;
                        }
                    }
                    else
                    {
                        continue; // don't know what's going on, just forget about it
                    }
                }
                if (h.collider != null && (closestHit < 0 || h.distance < s_HitBuffer[closestHit].distance))
                {
                    closestHit = i;
                }
            }

            // Naively combine penetrating items
            if (numPenetrations > 1 && penetrationDistanceSum > UnityVectorExtensions.Epsilon)
            {
                hitInfo = new RaycastHit();
                for (int i = 0; i < numPenetrations; ++i)
                {
                    var h = s_HitBuffer[s_PenetrationIndexBuffer[i]];
                    var t = h.distance / penetrationDistanceSum;
                    hitInfo.point += h.point * t;
                    hitInfo.distance += h.distance * t;
                    hitInfo.normal += h.normal * t;
                }
                hitInfo.normal = hitInfo.normal.normalized;
                return true;
            }

            if (closestHit >= 0)
            {
                hitInfo = s_HitBuffer[closestHit];
                return true;
            }
            hitInfo = new RaycastHit();
            return false;
        }

        static SphereCollider s_ScratchCollider;
        static GameObject s_ScratchColliderGameObject;
        static int s_ScratchColliderRefCount;

        /// <summary>
        /// This is a hidden sphere collider that won't interfere with the scene and can be used
        /// for making scratch calculations.  It is a single static object that gets created on first
        /// demand and recycled on subsequent demands.  Call DestroyScratchCollider() to destroy the cached
        /// object.
        /// </summary>
        /// <returns>A cached SphereCollider that will get recycled on subsequent calls.</returns>
        public static SphereCollider GetScratchCollider()
        {
            if (s_ScratchColliderGameObject == null)
            {
                s_ScratchColliderGameObject = new("Cinemachine Scratch Collider");
                s_ScratchColliderGameObject.hideFlags = HideFlags.HideAndDontSave;
                s_ScratchColliderGameObject.transform.position = Vector3.zero;
                s_ScratchColliderGameObject.SetActive(true);
                s_ScratchCollider = s_ScratchColliderGameObject.AddComponent<SphereCollider>();
                s_ScratchCollider.isTrigger = true;
                var rb = s_ScratchColliderGameObject.AddComponent<Rigidbody>();
                rb.detectCollisions = false;
                rb.isKinematic = true;
            }
            ++s_ScratchColliderRefCount;
            return s_ScratchCollider;
        }

        /// <summary>
        /// This will destroy the object created by GetScratchCollider() and cause the next call
        /// to GetScratchCollider() to create a new cached object.
        /// </summary>
        public static void DestroyScratchCollider()
        {
            if (--s_ScratchColliderRefCount == 0)
            {
                s_ScratchColliderGameObject.SetActive(false);
                DestroyObject(s_ScratchColliderGameObject.GetComponent<Rigidbody>());
                DestroyObject(s_ScratchCollider);
                DestroyObject(s_ScratchColliderGameObject);
                s_ScratchColliderGameObject = null;
                s_ScratchCollider = null;
            }
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

