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
        public enum ImportanceMode
        {
            Linear = 0,
            Logarithmic10 = 1,
            Logarithmic2 = 2,
        }

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

        public delegate float UrgencyComputer(StoryThread thread, bool isActiveThread);

        public class StoryThread
        {
            public string Name
            {
                get { return m_TargetGroup.name; }
                set { m_TargetGroup.name = value; }
            }

            private CinemachineTargetGroup m_TargetGroup;

            internal StoryThread(CinemachineTargetGroup group)
            {
                m_TargetGroup = group;
                RateModifier = 1f;
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
            public float UrgencyDerivative { get; set; }

            public float RateModifier { get; set; }
        }

        // Each thread follows a subject
        List<StoryThread> mThreads = new List<StoryThread>();
        public int NumThreads { get { return mThreads.Count; } }
        public StoryThread GetThread(int index) { return mThreads[index]; }

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

                    mLiveThreadIndex = mThreads.IndexOf(m_LiveThread);
                    mNextLiveThreadIndex = -1;
                }
            }
        }
        private StoryThread m_LiveThread;

        public StoryThread NextLiveThread
        {
            get { return ((mNextLiveThreadIndex != -1) && (mNextLiveThreadIndex < mThreads.Count)) ? mThreads[mNextLiveThreadIndex] : null; }
        }

        public ImportanceMode DecayUrgencyeMode = ImportanceMode.Logarithmic10;
        public ImportanceMode GrowUrgencyMode = ImportanceMode.Logarithmic10;

        public UrgencyComputer mUrgencyComputer = DefaultUrgencyComputer;

        Dictionary<Transform, StoryThread> mThreadLookup
            = new Dictionary<Transform, StoryThread>();
        List<CinemachineTargetGroup> mGroupRecycleBin = new List<CinemachineTargetGroup>();

        int mNextLiveThreadIndex = -1;
        int mLiveThreadIndex = -1;

        private static readonly CinemachineTargetGroup.Target[] sEmptyTargetsArray = new CinemachineTargetGroup.Target[0];

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
            mThreadLookup[group.transform] = th;
            mThreads.Add(th);
            return th;
        }

        public void DestroyStoryThread(StoryThread th)
        {
            if (th != null)
            {
                mThreadLookup.Remove(th.TargetGroup.transform);
                mThreads.Remove(th);
                // Recycle the target group
                th.TargetGroup.gameObject.SetActive(false);
                mGroupRecycleBin.Add(th.TargetGroup);
                th.TargetGroup.m_Targets = sEmptyTargetsArray;
            }
            if (LiveThread == th)
                LiveThread = null;
        }

        public StoryThread LookupStoryThread(Transform group)
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

        internal void TickStoryManagerExternal()
        {
            Update();
        }

        void Update()
        {
            float now = Time.time;
            float dt = Time.deltaTime;

            StoryThread liveThread = LiveThread;
            if (liveThread != null)
            {
                liveThread.TimeLastSeenStop = now;
            }

            // Recompute the urgencies, using the installed urgency calculator
            float highestUrgencySeen = float.MinValue;
            mNextLiveThreadIndex = -1;

            for (int i = 0; i < mThreads.Count; ++i)
            {
                StoryThread thread = mThreads[i];
                float startUrgency = thread.Urgency;
                thread.Urgency = mUrgencyComputer(thread, thread == LiveThread);
                thread.UrgencyDerivative = (thread.Urgency - startUrgency) / dt;

                if ((thread.Urgency > highestUrgencySeen) && (i != mLiveThreadIndex))
                {
                    mNextLiveThreadIndex = i;
                    highestUrgencySeen = thread.Urgency;
                }
            }

            if (((liveThread == null) && (mNextLiveThreadIndex != -1)) ||
                ((mNextLiveThreadIndex != mLiveThreadIndex) && (liveThread.LastOnScreenDuration > m_MinimumThreadTime)))
            {
                LiveThread = mThreads[mNextLiveThreadIndex];
            }
        }

        // Default urgency compute function
        public static float DefaultUrgencyComputer(StoryThread st, bool isActiveThread)
        {
            float newUrgency = 0f;
            if (isActiveThread)
            {
                newUrgency = DefaultDecayStoryThreadUrgency(st, Time.deltaTime, Instance.DecayUrgencyeMode);
            }
            else
            {
                newUrgency = DefaultGrowStoryThreadUrgency(st, Time.deltaTime, Instance.GrowUrgencyMode);
            }
            return newUrgency;
        }


        private static float DefaultDecayStoryThreadUrgency(StoryThread thread, float deltaTime, ImportanceMode decayMode)
        {
            float newUrgency = 0f;
            switch (decayMode)
            {
                case ImportanceMode.Logarithmic10:
                    newUrgency = thread.Urgency - deltaTime * 1f / Mathf.Log10(thread.InterestLevel + 2f);
                    break;

                case ImportanceMode.Logarithmic2:
                    newUrgency = thread.Urgency - deltaTime * 1f / Mathf.Log(thread.InterestLevel + 2f);
                    break;

                case ImportanceMode.Linear:
                    newUrgency = thread.Urgency - deltaTime * 1f / thread.InterestLevel;
                    break;
            }

            return Mathf.Max(0f, newUrgency);
        }

        private static float DefaultGrowStoryThreadUrgency(StoryThread thread, float deltaTime, ImportanceMode growMode)
        {
            float urgencyDelta = 0f;
            switch (growMode)
            {
                case ImportanceMode.Logarithmic10:
                    urgencyDelta = Mathf.Log10(thread.InterestLevel + 1f) * deltaTime;
                    break;

                case ImportanceMode.Logarithmic2:
                    urgencyDelta = Mathf.Log(thread.InterestLevel + 1f) * deltaTime;
                    break;

                case ImportanceMode.Linear:
                    urgencyDelta = thread.InterestLevel * deltaTime;
                    break;
            }

            return thread.Urgency + urgencyDelta * thread.RateModifier;
        }
    }

}
