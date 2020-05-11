using UnityEngine;
using System.Collections.Generic;
using System;

namespace Cinemachine
{
    internal class TargetPositionCache
    {
        public static bool UseCache { get; set; }
        public enum Mode { Disabled, Record, Playback }
        public static Mode m_CacheMode = Mode.Disabled;

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
                    default: case Mode.Disabled: m_Cache = null; break;
                    case Mode.Record: m_Cache = new Dictionary<Transform, CacheEntry>(); break;
                    case Mode.Playback: CreatePlaybackCurves(); break;
                }
            }
        }

        public static float CurrentTime { get; set; }

        class CacheEntry
        {
            public AnimationCurve X = new AnimationCurve();
            public AnimationCurve Y = new AnimationCurve();
            public AnimationCurve Z = new AnimationCurve();
            public AnimationCurve RotX = new AnimationCurve();
            public AnimationCurve RotY = new AnimationCurve();
            public AnimationCurve RotZ = new AnimationCurve();

            struct RecordingItem : IComparable<RecordingItem>
            {
                public float Time;
                public Vector3 Pos;
                public Vector3 Rot;
                public int CompareTo(RecordingItem other) { return Time.CompareTo(other.Time); }
            }
            List<RecordingItem> RawItems = new List<RecordingItem>();

            public void AddRawItem(float time, Transform target)
            {
                RawItems.Add(new RecordingItem
                {
                    Time = time,
                    Pos = target.position,
                    Rot = target.rotation.eulerAngles
                });
            }

            public void CreateCurves()
            {
                X = new AnimationCurve();
                Y = new AnimationCurve();
                Z = new AnimationCurve();
                RotX = new AnimationCurve();
                RotY = new AnimationCurve();
                RotZ = new AnimationCurve();

                RawItems.Sort();
                float time = float.MaxValue;
                for (int i = 0; i < RawItems.Count; ++i)
                {
                    var item = RawItems[i];
                    if (item.Time == time)
                        continue;
                    time = item.Time;

                    X.AddKey(new Keyframe(time, item.Pos.x));
                    Y.AddKey(new Keyframe(time, item.Pos.y));
                    Z.AddKey(new Keyframe(time, item.Pos.z));
                    RotX.AddKey(new Keyframe(time, item.Rot.x));
                    RotY.AddKey(new Keyframe(time, item.Rot.y));
                    RotZ.AddKey(new Keyframe(time, item.Rot.z));
                }
                RawItems.Clear();
            }
        }

        static Dictionary<Transform, CacheEntry> m_Cache;

        static void CreatePlaybackCurves()
        {
            if (m_Cache == null)
                m_Cache = new Dictionary<Transform, CacheEntry>();
            var iter = m_Cache.GetEnumerator();
            while (iter.MoveNext())
                iter.Current.Value.CreateCurves();
        }

        /// <summary>
        /// If Recording, will log the target position at the CurrentTime.
        /// Otherwise, will fetch the cached position at CurrentTime.
        /// </summary>
        /// <param name="target">Target whose transform is tracked</param>
        /// <param name="position">Target's position at CurrentTime</param>
        public static Vector3 GetTargetPosition(Transform target)
        {
            if (CacheMode == Mode.Disabled)
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
                entry.AddRawItem(CurrentTime, target);
                return target.position;
            }
            return new Vector3(
                entry.X.Evaluate(CurrentTime),
                entry.Y.Evaluate(CurrentTime),
                entry.Z.Evaluate(CurrentTime));
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

            if (!m_Cache.TryGetValue(target, out var entry))
            {
                if (CacheMode != Mode.Record)
                    return target.rotation;

                entry = new CacheEntry();
                m_Cache.Add(target, entry);
            }
            if (CacheMode == Mode.Record)
            {
                entry.AddRawItem(CurrentTime, target);
                return target.rotation;
            }
            return Quaternion.Euler(
                entry.RotX.Evaluate(CurrentTime),
                entry.RotY.Evaluate(CurrentTime),
                entry.RotZ.Evaluate(CurrentTime));
        }
    }
}
