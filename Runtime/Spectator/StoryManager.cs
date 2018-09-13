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
    [AddComponentMenu("Cinemachine/Spectator/StoryManager")]
    public class StoryManager : MonoBehaviour
    {
        private static StoryManager sInstance = null;

        /// <summary>Get the singleton instance</summary>
        public static StoryManager Instance
        {
            get
            {
                if (sInstance == null)
                {
                    var a = FindObjectsOfType<StoryManager>();
                    if (a != null && a.Length > 0)
                        sInstance = a[0];
                }
                return sInstance;
            }
        }

        public SpectatorTuningConstants m_TuningConstants;
        public enum ImportanceMode
        {
            Linear = 0,
            Logarithmic10 = 1,
            Logarithmic2 = 2,
        }

        /// <summary>If a thread becomes live, it must stay on at least this long.
        /// Prevents hysteresis.</summary>
        public float m_MinimumThreadTime = 1;

        public delegate void UrgencyComputer(StoryManager storyManager);

        public class StoryThread
        {
            public string Name
            {
                get { return TargetObject.name; }
                set { TargetObject.name = value; }
            }

            internal StoryThread(Transform target)
            {
                TargetObject = target;
                UrgencyGrowthStrength = 1f;
            }

            // All subjects have a wrapper TargetObject, if they are not already a target group
            public Transform TargetObject { get; private set; }

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
            public float LastOnScreenDuration { get { return TimeLastSeenStop - TimeLastSeenStart; } }

            // Urgency - decays while on-screen, increases otherwise.
            // Events and action states influence this heavily.
            // The Urgency evolution function is non-trivial and is configurable
            public float Urgency { get; set; }
            public float UrgencyDerivative { get; set; }
            public float UrgencyGrowthStrength { get; set; }

            public List<Cinematographer.CameraPointIndex> m_cameraPoints = new List<Cinematographer.CameraPointIndex>();

            // Extra data for use by client
            public object ClientData { get; set; }

            VolatileWeightSet m_InterestItems = new VolatileWeightSet();

            public void AddInterest(VolatileWeight w) 
            {
                m_InterestItems.SetWeight(w); 
            }

            // Called by StoryManager - decays the items and sums them
            internal void UpdateInterestLevel(float deltaTime)
            {
                InterestLevel = m_InterestItems.Decay(deltaTime);
            }

            // Decays while not live
            internal VolatileWeight m_satisfaction = new VolatileWeight();
        }

        // Each thread follows a subject
        List<StoryThread> mThreads = new List<StoryThread>();
        public int NumThreads { get { return mThreads.Count; } }
        public StoryThread GetThread(int index) { return mThreads[index]; }

        List<StoryThread> mLiveThreads = new List<StoryThread>();

        // The current Live threads, must be set every frame.
        public List<StoryThread> LiveThreads
        {
            get { return mLiveThreads; }
            set
            {
                if (value != null)
                {
                    for (int i = 0; i < value.Count; ++i)
                    {
                        var th = value[i];
                        if (!mLiveThreads.Contains(th))
                            th.AddInterest(new VolatileWeight() 
                                { 
                                    id = SpectatorTuningConstants.WeightID.NewThreadBoost,
                                    amount = m_TuningConstants.NewThreadBoostAmount,
                                    decayTime = m_TuningConstants.NewThreadBoostDecayTime
                                });
                    }
                }

                float now = Time.time;
                for (int i = mLiveThreads.Count-1; i >= 0; --i)
                    mLiveThreads[i].TimeLastSeenStop = now;
                mLiveThreads.Clear();
                if (value != null)
                {
                    for (int i = 0; i < value.Count; ++i)
                    {
                        mLiveThreads.Add(value[i]);
                        if (value[i].TimeLastSeenStop != now)
                            value[i].TimeLastSeenStart = value[i].TimeLastSeenStop = now;
                    }
                }
            }
        }
        public bool ThreadIsLive(StoryThread th) { return mLiveThreads.Contains(th); }


        public ImportanceMode DecayUrgencyeMode = ImportanceMode.Logarithmic10;
        public ImportanceMode GrowUrgencyMode = ImportanceMode.Logarithmic10;

        public UrgencyComputer mUrgencyComputer = DefaultUrgencyComputer;

        Dictionary<Transform, StoryThread> mThreadLookup = new Dictionary<Transform, StoryThread>();

        public StoryThread CreateStoryThread(Transform target)
        {
            StoryThread th = new StoryThread(target) { InterestLevel = 1 };
            mThreadLookup[target] = th;
            mThreads.Add(th);
            return th;
        }

        public void DestroyStoryThread(StoryThread th)
        {
            if (th != null)
            {
                mThreadLookup.Remove(th.TargetObject);
                mThreads.Remove(th);
                if (mLiveThreads.Contains(th))
                    mLiveThreads.Remove(th);
            }
        }

        public StoryThread LookupStoryThread(Transform target)
        {
            StoryThread th = null;
            mThreadLookup.TryGetValue(target, out th);
            return th;
        }

        public void CreateStoryThreads(List<Transform> objects)
        {
            for (int i = 0; i < objects.Count; ++i)
            {
                var th = LookupStoryThread(objects[i]);
                if (th == null)
                {
                    // Create the thread
                    th = CreateStoryThread(objects[i]);
                    th.InterestLevel = 1;
                }
            }
        }

        public void DestroyStoryThreads(List<Transform> objects)
        {
            for (int i = 0; i < objects.Count; ++i)
            {
                var th = LookupStoryThread(objects[i]);
                if (th != null)
                    DestroyStoryThread(th);
            }
        }

        public bool SetStoryThreadInterestLevel(Transform target, float interest)
        {
            var th = LookupStoryThread(target);
            if (th == null)
                return false;
            th.InterestLevel = interest;
            return true;
        }

        /// <summary>Sum of all urgencies.  Used for computing normalized urgencies</summary>
        public float SumOfAllUrgencies { get; private set; }

        void SortThreadsByUrgency()
        {
            mThreads.Sort((x, y) => y.Urgency.CompareTo(x.Urgency));
        }

        public void TickStoryManager()
        {
            float now = Time.time;
            for (int i = mLiveThreads.Count-1; i >= 0; --i)
                mLiveThreads[i].TimeLastSeenStop = now;

            // Recompute the urgencies, using the installed urgency calculator
            mUrgencyComputer(this);
            SortThreadsByUrgency();

            SumOfAllUrgencies = 0;
            for (int i = 0; i < mThreads.Count; ++i)
                SumOfAllUrgencies += mThreads[i].Urgency;
        }

#if true
        // Default urgency compute function
        public static void DefaultUrgencyComputer(StoryManager storyManager)
        {
            float dt = Time.deltaTime;
            float interestSum = 0;
            for (int i = 0; i < storyManager.mThreads.Count; ++i)
            {
                StoryThread th = storyManager.mThreads[i];
                th.UpdateInterestLevel(dt);
                interestSum += th.InterestLevel;
            }
            for (int i = 0; i < storyManager.mThreads.Count; ++i)
            {
                float starvationBoost = 0;
                StoryThread th = storyManager.mThreads[i];
                if (storyManager.ThreadIsLive(th))
                    th.m_satisfaction.amount = storyManager.m_TuningConstants.ThreadStarvationBoostAmount;
                else
                {
                    float scale = interestSum / Mathf.Max(0.001f, th.InterestLevel);
                    th.m_satisfaction.decayTime = scale * storyManager.m_TuningConstants.NewThreadBoostDecayTime;
                    starvationBoost = storyManager.m_TuningConstants.ThreadStarvationBoostAmount - th.m_satisfaction.Decay(dt);
                }

                float startUrgency = th.Urgency;
                th.Urgency = th.InterestLevel + starvationBoost;
                th.UrgencyDerivative = (th.Urgency - startUrgency) / dt;
            }
        }
#else
        // Default urgency compute function
        public static void DefaultUrgencyComputer(StoryManager storyManager)
        {
            float dt = Time.deltaTime;
            float currentLiveTime = storyManager.m_MinimumThreadTime;
            for (int i = storyManager.LiveThreads.Count-1; i >= 0; --i)
                currentLiveTime += storyManager.LiveThreads[i].LastOnScreenDuration;
            for (int i = 0; i < storyManager.mThreads.Count; ++i)
            {
                StoryThread st = storyManager.mThreads[i];
                float startUrgency = st.Urgency;
                if (currentLiveTime >= storyManager.m_MinimumThreadTime)
                {
                    float timeBasedUrgencyScale = storyManager.ThreadIsLive(st)
                        ? DefaultDecayStoryThreadUrgency(st, dt, Instance.DecayUrgencyeMode)
                        : DefaultGrowStoryThreadUrgency(st, dt, Instance.GrowUrgencyMode);
                
                    st.Urgency = timeBasedUrgencyScale;
                }
                st.UrgencyDerivative = (st.Urgency - startUrgency) / dt;
            }
        }

        private static float DefaultDecayStoryThreadUrgency(
            StoryThread st, float deltaTime, ImportanceMode decayMode)
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

        private static float DefaultGrowStoryThreadUrgency(
            StoryThread st, float deltaTime, ImportanceMode growMode)
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
#endif
    }
}
