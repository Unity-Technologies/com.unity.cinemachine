using UnityEngine;
using System.Collections.Generic;
using System;

namespace Cinemachine
{
    internal class TargetPositionCache
    {
        public static bool UseCache { get; set; }

        public static int kMaxResolution = 5;
        public static int Resolution { get; set; }

        public static float CacheStepSize => kMaxResolution / (Mathf.Max(1, (float)Resolution) * 60.0f);

        public enum Mode { Disabled, Record, Playback }
        
        static Mode m_CacheMode = Mode.Disabled;

        public static Mode CacheMode
        {
            get => m_CacheMode;
            set
            {
                if (value == m_CacheMode)
                    return;
                m_CacheMode = value;
                switch (m_CacheMode)
                {
                    default: case Mode.Disabled: ClearCache(); break;
                    case Mode.Record: InitCache(); break;
                    case Mode.Playback: CreatePlaybackCurves(); break;
                }
            }
        }

        public static bool IsRecording => UseCache && m_CacheMode == Mode.Record;
        public static bool CurrentPlaybackTimeValid => UseCache && m_CacheMode == Mode.Playback && HasHurrentTime;

        public static float CurrentTime { get; set; }

        class CacheCurve
        {
            public struct Item
            {
                public Vector3 Pos;
                public Quaternion Rot;

                public static Item Lerp(Item a, Item b, float t)
                {
                    return new Item
                    {
                        Pos = Vector3.LerpUnclamped(a.Pos, b.Pos, t),
                        Rot = Quaternion.SlerpUnclamped(a.Rot, b.Rot, t)
                    };
                }
            }

            public float StartTime;
            public float StepSize;
            public int Count => m_Cache.Count;
 
            List<Item> m_Cache;

            public CacheCurve(float startTime, float endTime, float stepSize)
            {
                StepSize = stepSize;
                StartTime = startTime;
                m_Cache = new List<Item>(Mathf.CeilToInt((endTime - startTime) / StepSize));
            }

            public void Add(Item item, float time)
            {
                if (time < StartTime)
                    return;
                var lastIndex = m_Cache.Count - 1;
                if (lastIndex < 0)
                    m_Cache.Add(item);
                else
                {
                    int index = Mathf.FloorToInt((time - StartTime) / StepSize);
                    var range = (float)(index - lastIndex)
                        + ((time - StartTime) - (index * StepSize)) / StepSize;
                    var lastItem = m_Cache[lastIndex];
                    var lastTime = StartTime + lastIndex * StepSize;
                    for (int i = lastIndex + 1; i <= index; ++i)
                        m_Cache.Add(Item.Lerp(lastItem, item, (float)(i - lastIndex) / range));
                }
            }

            public Item Evaluate(float time)
            {
                var numItems = m_Cache.Count;
                if (numItems == 0)
                    return new Item { Rot = Quaternion.identity };

                var s = time - StartTime;
                var index = Mathf.Max(Mathf.FloorToInt(s / StepSize), 0);
                if (index >= numItems - 1)
                    return m_Cache[numItems - 1];

                float t = (s - (index * StepSize)) / StepSize;
                return Item.Lerp(m_Cache[index], m_Cache[index + 1], t);
            }
        }

        class CacheEntry
        {
            public CacheCurve Curve;

            struct RecordingItem : IComparable<RecordingItem>
            {
                public float Time;
                public CacheCurve.Item Item;
                public int CompareTo(RecordingItem other) { return Time.CompareTo(other.Time); }
            }
            List<RecordingItem> RawItems = new List<RecordingItem>();
            RecordingItem LastRawItem;

            public void AddRawItem(float time, Transform target)
            {
                var n = RawItems.Count;
                LastRawItem = new RecordingItem
                {
                    Time = time,
                    Item = new CacheCurve.Item { Pos = target.position, Rot = target.rotation }
                };
                if (n == 0 || Mathf.Abs(RawItems[n-1].Time - time) >= CacheStepSize)
                    RawItems.Add(LastRawItem);
            }

            public void CreateCurves()
            {
                RawItems.Sort();

                int numItems = RawItems.Count;
                float startTime = numItems == 0 ? 0 : RawItems[0].Time;
                float endTime = numItems == 0 ? 0 : LastRawItem.Time;
                Curve = new CacheCurve(startTime, endTime, CacheStepSize);

                var lastAddedItem = new RecordingItem { Time = float.MaxValue };
                for (int i = 0; i < numItems; ++i)
                {
                    var item = RawItems[i];
                    if (Mathf.Abs(item.Time - lastAddedItem.Time) < CacheStepSize)
                        continue;
                    Curve.Add(item.Item, item.Time);
                    lastAddedItem = item;
                }
                if (numItems > 0)
                {
                    var r = LastRawItem.Time - lastAddedItem.Time;
                    var step = CacheStepSize * 1.9f;
                    if (r > 0.0001f)
                        Curve.Add(CacheCurve.Item.Lerp(lastAddedItem.Item, LastRawItem.Item, step / r), 
                            lastAddedItem.Time + step);
                }
                RawItems.Clear();
            }
        }

        static Dictionary<Transform, CacheEntry> m_Cache;

        public struct TimeRange
        {
            public float Start;
            public float End;

            public bool IsEmpty => End < Start;
            public bool Contains(float time) => time >= Start && time <= End;
            public static TimeRange Empty 
                { get => new TimeRange { Start = float.MaxValue, End = float.MinValue }; }

            public void Include(float time)
            {
                Start = Mathf.Min(Start, time);
                End = Mathf.Max(End, time);
            }
        }
        static TimeRange m_CacheTimeRange;
        public static TimeRange CacheTimeRange { get => m_CacheTimeRange; }
        public static bool HasHurrentTime { get => m_CacheTimeRange.Contains(CurrentTime); }

        static void ClearCache()
        {
            m_Cache = null;
            m_CacheTimeRange = TimeRange.Empty;
        }

        static void InitCache()
        {
            m_Cache = new Dictionary<Transform, CacheEntry>();
            m_CacheTimeRange = TimeRange.Empty;
        }

        static void CreatePlaybackCurves()
        {
            if (m_Cache == null)
                m_Cache = new Dictionary<Transform, CacheEntry>();
            var iter = m_Cache.GetEnumerator();
            while (iter.MoveNext())
                iter.Current.Value.CreateCurves();
        }

        const float kWraparoundSlush = 0.1f;

        /// <summary>
        /// If Recording, will log the target position at the CurrentTime.
        /// Otherwise, will fetch the cached position at CurrentTime.
        /// </summary>
        /// <param name="target">Target whose transform is tracked</param>
        /// <param name="position">Target's position at CurrentTime</param>
        public static Vector3 GetTargetPosition(Transform target)
        {
            if (!UseCache || CacheMode == Mode.Disabled)
                return target.position;

            // Wrap around during record?
            if (CacheMode == Mode.Record 
                && !m_CacheTimeRange.IsEmpty 
                && CurrentTime < m_CacheTimeRange.Start - kWraparoundSlush)
            {
                ClearCache();
                InitCache();
            }

            if (CacheMode == Mode.Playback && !HasHurrentTime)
                return target.position;

            if (!m_Cache.TryGetValue(target, out var entry))
            {
                if (CacheMode != Mode.Record)
                    return target.position;

                entry = new CacheEntry();
                m_Cache.Add(target, entry);
            }
            if (CacheMode == Mode.Record)
            {
                if (m_CacheTimeRange.End <= CurrentTime)
                {
                    entry.AddRawItem(CurrentTime, target);
                    m_CacheTimeRange.Include(CurrentTime);
                }
                return target.position;
            }
            if (entry.Curve == null)
                return target.position;
            return entry.Curve.Evaluate(CurrentTime).Pos;
        }

        /// <summary>
        /// If Recording, will log the target rotation at the CurrentTime.
        /// Otherwise, will fetch the cached position at CurrentTime.
        /// </summary>
        /// <param name="target">Target whose transform is tracked</param>
        /// <param name="rotation">Target's rotation at CurrentTime</param>
        public static Quaternion GetTargetRotation(Transform target)
        {
            if (CacheMode == Mode.Disabled)
                return target.rotation;

            // Wrap around during record?
            if (CacheMode == Mode.Record 
                && !m_CacheTimeRange.IsEmpty 
                && CurrentTime < m_CacheTimeRange.Start - kWraparoundSlush)
            {
                ClearCache();
                InitCache();
            }

            if (CacheMode == Mode.Playback && !HasHurrentTime)
                return target.rotation;

            if (!m_Cache.TryGetValue(target, out var entry))
            {
                if (CacheMode != Mode.Record)
                    return target.rotation;

                entry = new CacheEntry();
                m_Cache.Add(target, entry);
            }
            if (CacheMode == Mode.Record)
            {
                if (m_CacheTimeRange.End <= CurrentTime)
                {
                    entry.AddRawItem(CurrentTime, target);
                    m_CacheTimeRange.Include(CurrentTime);
                }
                return target.rotation;
            }
            return entry.Curve.Evaluate(CurrentTime).Rot;
        }
    }
}
