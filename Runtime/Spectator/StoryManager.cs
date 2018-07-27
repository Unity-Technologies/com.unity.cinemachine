using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Spectator
{
    /*
    Component A - The Story Manager: determining what the current story line is
    To accomplish this, we will introduce some new concepts:

    The Narrative composed of multiple Story Threads
    The Story Thread, which in essence follows a single character (or group of characters).  
    The Story thread has the following attributes: 
    Subject (character, or object, or ?) 
    Action (walk, run, shoot, idle, etc) 
    Event (death, fight, enter, talk, etc)
    Time when last on-screen
    Last on-screen duration
    Interest level - Controlled by the dev, to focus on a specific thread he judges relevant 
    Urgency - decays while on-screen, increases otherwise.  Events and action states influence 
    this heavily.  
    The Urgency evolution function is non-trivial and is configurable

    Story Threads will be kept sorted by urgency.  A high-quality shot covering the most 
    urgent Story Thread will be selected.

    A story thread follows an interesting subject (or group of subjects).  "Interesting" is 
    defined by the dev.
    Story threads are created as interesting subjects are created.   This should be a call to 
    the Story Manager
    Story threads disappear when their subjects do, or when the dev decides that the subjects 
    are no longer interesting (another call to Story Manager)
    Grouping ideas:  
    the Story Manager has the possibility of automatically generating subject groups, when the 
    subjects of multiple groups are "close enough" to be nicely framed by a camera, with at 
    least one high quality shot.  
    The group’s Urgency could take on the value of the most urgent group member + 1, so that 
    the group shot will be favored over the members.  While the group storyline is "Live" 
    the members Urgency would decay as if they were live.
    Groups could automatically be destroyed when there is no longer a good shot of all of 
    them members, or when these are far enough (ideally should be the opposite condition 
    that leads to the creation of a group).
    The Story Manager should take precautions against hysteresis.
    Urgency must decay while the story is live, and increase while it’s not.
    The Urgency is a function of events and actions as well as time and Live status, 
    and its evolution function is non-trivial.  This needs to be tweakable by the dev 
    in some comprehensible fashion

    The interest level of a Story Thread: 
    Dev maintains an "interest level" for story lines.  
    Some subjects are more interesting than others, that depends on the story.  Only the dev knows.  
    Not to be confused with "Urgency", which is calculated by the story manager, and represents the 
    urgency of making this story thread go live now.
    Interest level is manually maintained by the dev, according to the story content.  
    Quite possibly it remains constant most of the time (Gimli the dwarf is fundamentally 
    less interesting than Frodo and Sam).

    The Director will try to show the most urgent thing.  
    If no nice shot can be found, or if there is no suitable transition, 
    it will try to show the next-most-urgent thing, or stay on the current thing.

    Once we have a collection of shots that cover the desired Story Thread (rule 1: What we 
    want to see), the Director must then choose from among the available shots covering 
    the Story Thread (rules 2 and 3: How we want to see it).  
    The shots themselves will be rated for quality, as before, but a new element will 
    be added: the quality of the transition. In other words: is this a good cut?
    */

    public class StoryManager 
    {
        public class Subject 
        {
        }

        public delegate float UrgencyComputer(StoryThread thread);

        public class StoryThread
        {
            // Subject (character, or object, or ?) 
            public Subject ThreadSubject { get; set; }

            // Interest level - Controlled by the dev, to focus on a specific thread he judges relevant.
            // This is used as part of the weighting algorithm when calculating urgency
            public float IntrinsicInterestLevel { get; set; }

            // Action (walk, run, shoot, idle, death, fight, enter, talk, etc)
            public int ThreadAction { get; set; }

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

        // Sort the threads by urgence, using the installed urgency calculator
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
