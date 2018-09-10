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
        public float m_MinimumThreadTime = 3;

        public delegate void UrgencyComputer(StoryManager storyManager);

        public class StoryThread
        {
            public string Name
            {
                get { return TargetGroup.name; }
                set { TargetGroup.name = value; }
            }

            internal StoryThread(CinemachineTargetGroup group)
            {
                TargetGroup = group;
                UrgencyGrowthStrength = 1f;
            }

            // All subjects have a wrapper TargetGroup, if they are not already a target group
            public CinemachineTargetGroup TargetGroup { get; private set; }

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

            public float TimeUrgency { get; set; }

            // Urgency - decays while on-screen, increases otherwise.
            // Events and action states influence this heavily.
            // The Urgency evolution function is non-trivial and is configurable
            public float Urgency { get; set; }
            public float UrgencyDerivative { get; set; }

            public float UrgencyGrowthStrength { get; set; }

            // Extra data for use by client
            public object ClientData { get; set; }
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
                }
            }
        }
        private StoryThread m_LiveThread;

        public ImportanceMode DecayUrgencyeMode = ImportanceMode.Logarithmic10;
        public ImportanceMode GrowUrgencyMode = ImportanceMode.Logarithmic10;

        public UrgencyComputer mUrgencyComputer = DefaultUrgencyComputer;

        Dictionary<Transform, StoryThread> mThreadLookup
            = new Dictionary<Transform, StoryThread>();
        List<CinemachineTargetGroup> mGroupRecycleBin = new List<CinemachineTargetGroup>();

        private static readonly CinemachineTargetGroup.Target[] sEmptyTargetsArray 
            = new CinemachineTargetGroup.Target[0];

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
                group.m_Targets = sEmptyTargetsArray;
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

        /// <summary>Sum of all urgencies.  Used for computing normalized urgencies</summary>
        public float SumOfAllUrgencies { get; private set; }

        public void SortThreadsByUrgency()
        {
            mThreads.Sort((x, y) => y.Urgency.CompareTo(x.Urgency));
        }

        public void TickStoryManagerExternal()
        {
            InternalUpdate();
        }

        internal void InternalUpdate()
        {
            StoryThread liveThread = LiveThread;
            if (liveThread != null)
                liveThread.TimeLastSeenStop = Time.time;

            // Recompute the urgencies, using the installed urgency calculator
            mUrgencyComputer(this);
            SortThreadsByUrgency();

            SumOfAllUrgencies = 0;
            for (int i = 0; i < mThreads.Count; ++i)
                SumOfAllUrgencies += mThreads[i].Urgency;
        }

        // Default urgency compute function
        public static void DefaultUrgencyComputer(StoryManager storyManager)
        {
            float dt = Time.deltaTime;
            float currentLiveTime = storyManager.m_MinimumThreadTime;
            if (storyManager.LiveThread != null)
                currentLiveTime = storyManager.LiveThread.LastOnScreenDuration;
            for (int i = 0; i < storyManager.mThreads.Count; ++i)
            {
                StoryThread st = storyManager.mThreads[i];
                float startUrgency = st.Urgency;
                if (currentLiveTime >= storyManager.m_MinimumThreadTime)
                {
                    float timeBasedUrgencyScale = (st == storyManager.LiveThread)
                        ? DefaultDecayStoryThreadUrgency(st, dt, Instance.DecayUrgencyeMode)
                        : DefaultGrowStoryThreadUrgency(st, dt, Instance.GrowUrgencyMode);
                
                    st.Urgency = timeBasedUrgencyScale;
                }
                st.UrgencyDerivative = (st.Urgency - startUrgency) / dt;
            }
        }

        private static float DefaultDecayStoryThreadUrgency(StoryThread st, float deltaTime, ImportanceMode decayMode)
        {
            float newUrgency = 0f;
            switch (decayMode)
            {
                case ImportanceMode.Logarithmic10:
                    newUrgency = st.Urgency - deltaTime * 1f / Mathf.Log10(st.InterestLevel + 2f);
                    break;

                case ImportanceMode.Logarithmic2:
                    newUrgency = st.Urgency - deltaTime * 1f / Mathf.Log(st.InterestLevel + 2f);
                    break;

                case ImportanceMode.Linear:
                    newUrgency = st.Urgency - deltaTime * 1f / st.InterestLevel;
                    break;
            }
            return Mathf.Max(0f, newUrgency);        
        }

        private static float DefaultGrowStoryThreadUrgency(StoryThread st, float deltaTime, ImportanceMode growMode)
        {
            float urgencyDelta = 0f;
            switch (growMode)
            {
                case ImportanceMode.Logarithmic10:
                    urgencyDelta = Mathf.Log10(st.InterestLevel + 1f) * deltaTime;
                    break;

                case ImportanceMode.Logarithmic2:
                    urgencyDelta = Mathf.Log(st.InterestLevel + 1f) * deltaTime;
                    break;

                case ImportanceMode.Linear:
                    urgencyDelta = st.InterestLevel * deltaTime;
                    break;
            }

            return st.Urgency + urgencyDelta * st.UrgencyGrowthStrength;
        }
    }

}
