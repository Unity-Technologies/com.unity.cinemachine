using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cinemachine;

namespace Spectator
{
    /// Story Manager
    /// 
    /// - Priority-based scheduling of story threads (including group-aggregated threads)
    /// - Story thread interest level is the priority
    /// - Makes sure lower-priority threads are not starved (alots screen time to all 
    ///     threads on a round-robin basis, giving proportionally more time to higher-priority threads)
    /// - Output is an ordered list of threads, sorted by desirability for screen time (aka Urgency)
    /// - Tweaking parameters may include time slice granularity and perhaps other 
    ///     ways to guard against hysteresis
    ///     
    public class StoryManager : MonoBehaviour
    {
        private static StoryManager sInstance = null;

        /// <summary>Get the singleton instance</summary>
        public static StoryManager Instance
        {
            get
            {
                if (sInstance == null)
                    sInstance = FindObjectsOfType<StoryManager>()[0];
                return sInstance;
            }
        }

        /// <summary>If a thread becomes live, it must stay on at least this long.  
        /// Prevents hysteresis.</summary> 
        public float m_MinimumThreadTime;

        public delegate float UrgencyComputer(StoryThread thread);

        public class StoryThread
        {
            private CinemachineTargetGroup m_TargetGroup;

            internal StoryThread(CinemachineTargetGroup group)
            {
                m_TargetGroup = group;
            }

            // All subjects have a wrapper TargetGroup, if they are not already a target group
            public CinemachineTargetGroup TargetGroup { get { return m_TargetGroup; } }

            // Interest level - Controlled by the dev, to focus on a specific thread he judges relevant.
            // This is used as part of the weighting algorithm when calculating urgency
            public float InterestLevel { get; set; }

            // Emotional color.  The precice meanings of the axes are chosen by the dev.
            // One possibility:
            //  x - Stress/Calm axis (tension)
            //  y - Fear/Confidence axis (control)
            //  z - Rage/Joy axis (attitude)
            public Vector3 Emotion { get; set; }

            // Time when last on-screen
            public float TimeLastSeenStart { get; set; }
            public float TimeLastSeenStop { get; set; }

            // Last on-screen duration
            public float LastOnScreenDuration { get { return TimeLastSeenStop - TimeLastSeenStart; } }

            // Urgency - decays while on-screen, increases otherwise.  
            // Events and action states influence this heavily.  
            // The Urgency evolution function is non-trivial and is configurable
            public float Urgency { get; set; }
        }

        // Each thread follows a subject
        List<StoryThread> mThreads = new List<StoryThread>();
        public int NumThreads { get { return mThreads.Count; } }
        public StoryThread GetThread(int index) { return mThreads[index]; }

        Dictionary<CinemachineTargetGroup, StoryThread> mThreadLookup 
            = new Dictionary<CinemachineTargetGroup, StoryThread>();
        List<CinemachineTargetGroup> mGroupRecycleBin = new List<CinemachineTargetGroup>();

        public StoryThread CreateStoryThread(string name)
        {
            // Create wrapper group for targets
            CinemachineTargetGroup group = null;
            if (mGroupRecycleBin.Count > 0)
            {
                group = mGroupRecycleBin[0];
                mGroupRecycleBin.RemoveAt(0);
                group.gameObject.name = name;
                group.gameObject.SetActive(true);
            }
            else
            {
                GameObject go = new GameObject(name);
                go.transform.SetParent(this.transform);
                group = go.AddComponent<CinemachineTargetGroup>();
            }

            StoryThread th = new StoryThread(group);
            mThreadLookup[group] = th;
            mThreads.Add(th);
            return th;
        }

        public void DestroyStoryThread(StoryThread th)
        {
            if (th != null)
            {
                mThreadLookup.Remove(th.TargetGroup);
                mThreads.Remove(th);
                // Recycle the target group
                th.TargetGroup.gameObject.SetActive(false);
                mGroupRecycleBin.Add(th.TargetGroup);
                th.TargetGroup.m_Targets = null;
            }
            if (LiveThread == th)
                LiveThread = null;
        }

        public StoryThread LookupStoryThread(CinemachineTargetGroup group)
        {
            StoryThread th;
            mThreadLookup.TryGetValue(group, out th);
            return th;
        }

        // Sort the threads by urgency, using the installed urgency calculator
        public void SortThreads()
        {
            mThreads.Sort((x, y) => x.Urgency.CompareTo(y.Urgency)); 
        }

        // The current Live thread, must be set every frame.
        public StoryThread LiveThread
        {
            get { return m_LiveThread; }
            set
            {
                float now = Time.time;
                if (value != m_LiveThread)
                {
                    if (m_LiveThread != null)
                        m_LiveThread.TimeLastSeenStop = now;
                    if (value != null)
                        value.TimeLastSeenStart = value.TimeLastSeenStop = now;
                    m_LiveThread = value;
                }
            }
        }
        private StoryThread m_LiveThread;

        void Update()
        {
            float now = Time.time;
            if (LiveThread != null)
                LiveThread.TimeLastSeenStop = now;

            // Recompute the urgencies, using the installed urgency calculator
            for (int i = 0; i < mThreads.Count; ++i)
                mThreads[i].Urgency = mUrgencyComputer(mThreads[i]);

            // Sort the threads by urgency
            mThreads.Sort((x, y) => x.Urgency.CompareTo(y.Urgency)); 
        }

        public UrgencyComputer mUrgencyComputer = DefaultUrgencyComputer;

        // Default urgency compute function
        public static float DefaultUrgencyComputer(StoryThread st)
        {
            return 0;
        }
    }

}
