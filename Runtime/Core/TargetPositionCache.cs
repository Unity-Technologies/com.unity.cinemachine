using UnityEngine;
using System.Collections.Generic;
using System;

namespace Cinemachine
{
    internal class TargetPositionCache
    {
        const float kItemsPerSecond = 10f;
        static bool m_Enabled;

        public static bool Enabled 
        { 
            get => m_Enabled;
            set
            {
                if (value != m_Enabled)
                {
                    m_Enabled = value;
                    m_Cache = m_Enabled ? new List<CacheEntry>() : null;
                    m_LastCurrentTimeIndex = -1;
                }
            }
        }

        public static float CurrentTime { get; set; }
        public static float CurrentRealTime { get; set; }
        public static bool Recording { get; set; }

        struct Item
        {
            public Vector3 Position;
            public Quaternion Rotation;
        }

        struct CacheEntry : IComparable<CacheEntry>
        {
            public float Timestamp;
            public Dictionary<Transform, Item> Targets;

            public int CompareTo(CacheEntry other)
            {
                return Timestamp.CompareTo(other.Timestamp);
            }
        }

        static List<CacheEntry> m_Cache;

        // Optimization so we don't keep binary-searching the same frame
        static float m_LastCurrentTime;
        static int m_LastCurrentTimeIndex;

        /// <summary>
        /// If Recording, will log the target position at the CurrentTime.
        /// Otherwise, will fetch the cached position at CurrentTime.
        /// </summary>
        /// <param name="target">Target whose transform is tracked</param>
        /// <param name="position">Target's position at CurrentTime</param>
        /// <param name="rotation">Target's rotation at CurrentTime</param>
        public static void GetTargetPosition(
            Transform target, out Vector3 position, out Quaternion rotation)
        {
            if (Enabled && (Recording || m_Cache.Count > 0))
            {
                var scaledTime = CurrentTime * kItemsPerSecond;
                var key = Mathf.Round(scaledTime);

                int index = m_LastCurrentTimeIndex;
                if (m_LastCurrentTimeIndex < 0 || CurrentTime != m_LastCurrentTime)
                    index = m_Cache.BinarySearch(new CacheEntry { Timestamp = key });
                if (index < 0)
                {
                    index = ~index;
                    if (Recording)
                        m_Cache.Insert(index, new CacheEntry 
                            { Timestamp = key, Targets = new Dictionary<Transform, Item>() });
                    else
                        index = Mathf.Max(0, index - 1); // best we can do
                }
                m_LastCurrentTime = CurrentTime;
                m_LastCurrentTimeIndex = index;

                var item = new Item
                { 
                    Position = target.position,
                    Rotation = target.rotation
                };
                var cache = m_Cache[index].Targets;
                if (!cache.TryGetValue(target, out var existing))
                {
                    if (Recording)
                        cache.Add(target, item);
                    existing = item;
                }

                if (Recording)
                    cache[target] = existing = item;

                if (CurrentTime != CurrentRealTime)
                {
                    position = existing.Position;
                    rotation = existing.Rotation;
                    return;
                }
            }
            
            // Nothing in cache
            position = target.position;
            rotation = target.rotation;
            return;
        }

        public static void GetTimestamps(float startTime, float endTime, List<float> returns)
        {
            returns.Clear();

            var scaledTime = startTime * kItemsPerSecond;
            var key = Mathf.Round(scaledTime);
            int index = m_LastCurrentTimeIndex;
            if (m_LastCurrentTimeIndex < 0 || startTime != m_LastCurrentTime)
                index = m_Cache.BinarySearch(new CacheEntry { Timestamp = key });
            if (index < 0)
                index = Mathf.Max(0, ~index - 1); // best we can do
            while (index < m_Cache.Count)
            {
                var t = m_Cache[index++].Timestamp / kItemsPerSecond;
                if (t > endTime + 0.001f)
                    break;
                returns.Add(t);
            }
        }
    }
}
