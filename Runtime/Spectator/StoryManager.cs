using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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
    public class StoryManager 
    {
        private static StoryManager sInstance = null;

        /// <summary>Get the singleton instance</summary>
        public static StoryManager Instance
        {
            get
            {
                if (sInstance == null)
                    sInstance = new StoryManager();
                return sInstance;
            }
        }

        public class Subject 
        {
            public Transform m_Transform;
            public Subject(Transform t)
            {
                m_Transform = t;
            }
        }

        public delegate float UrgencyComputer(StoryThread thread);

        public class StoryThread
        {
            // Subject (character, or object, or ?) 
            public Subject ThreadSubject { get; set; }

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

        Dictionary<Subject, StoryThread> mThreadLookup = new Dictionary<Subject, StoryThread>();

        public StoryThread CreateStoryThread(Subject s)
        {
            StoryThread th = new StoryThread { ThreadSubject = s };
            mThreadLookup[s] = th;
            mThreads.Add(th);
            return th;
        }

        public void DestroyStoryThread(Subject s)
        {
            StoryThread th;
            if (mThreadLookup.TryGetValue(s, out th))
            {
                mThreadLookup.Remove(s);
                mThreads.Remove(th);
            }
            if (LiveThread == th)
                LiveThread = null;
        }

        public StoryThread GetStoryThread(Subject s)
        {
            StoryThread th;
            if (!mThreadLookup.TryGetValue(s, out th))
                th = CreateStoryThread(s);
            return th;
        }

        // Sort the threads by urgency, using the installed urgency calculator
        public void SortThreads()
        {
            mThreads.Sort((x, y) => x.Urgency.CompareTo(y.Urgency)); 
        }

        // The current Live thread, set every frame by Update().
        public StoryThread LiveThread { get; private set; }

        // Call this every frame with the current live story thread
        public void Update(StoryThread liveThread)
        {
            float now = Time.time;
            if (liveThread != LiveThread)
            {
                liveThread.TimeLastSeenStart = now;
                if (LiveThread != null)
                    LiveThread.TimeLastSeenStop = now;
                LiveThread = liveThread;
            }
            liveThread.TimeLastSeenStop = now;

            // Recompute the urgencies
            for (int i = 0; i < mThreads.Count; ++i)
                mThreads[i].Urgency = mUrgencyComputer(mThreads[i]);
        }

        public UrgencyComputer mUrgencyComputer = DefaultUrgencyComputer;

        // Default urgency compute function
        public static float DefaultUrgencyComputer(StoryThread st)
        {
            return 0;
        }
    }

}
