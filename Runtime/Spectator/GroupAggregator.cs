using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cinemachine;

namespace Spectator
{
    /// ThreadGroup Aggregator
    /// 
    /// - Dynamically creates and destroys groups
    /// - Each story thread has a target group, containing the item of interest.  
    ///     By default the target group will contain a single object - the item of interest 
    ///     that created the thread
    /// - ThreadGroup aggregator dynamically creates and destroys groups based on 
    ///     proximity (player + enemy, groups of players, groups of enemies, etc).
    /// - New story threads are then created for the new groups.  
    ///     Interest level of the new threads is an aggregate of the interest levels of the members
    /// - Polls periodically
    /// 
    public class GroupAggregator : MonoBehaviour
    {   
        public float m_proximityThreshold = 5;
        public float m_updateTimeStep = 2;

        StoryManager m_StoryManager;

        public class ThreadGroup
        {
            StoryManager.StoryThread m_StoryThread;

            public ThreadGroup(StoryManager.StoryThread th) { m_StoryThread = th; }
            public StoryManager.StoryThread StoryThread { get { return m_StoryThread; } }
            public List<StoryManager.StoryThread> m_Parents = new List<StoryManager.StoryThread>();
            public List<StoryManager.StoryThread> m_Children = new List<StoryManager.StoryThread>();
        }
        Dictionary<StoryManager.StoryThread, ThreadGroup> mGroupLookup = new Dictionary<StoryManager.StoryThread, ThreadGroup>();

        private void Start()
        {
            m_StoryManager = GetComponent<StoryManager>();
        }

        public ThreadGroup LookupGroup(StoryManager.StoryThread subject)
        {
            ThreadGroup g = null;
            mGroupLookup.TryGetValue(subject, out g);
            return g;
        }

        public ThreadGroup CreateGroup(string name)
        {
            ThreadGroup g = new ThreadGroup(m_StoryManager.CreateStoryThread(null, name, 0));
            mGroupLookup[g.StoryThread] = g;
            return g;
        }

        public void DestroyGroup(ThreadGroup g)
        {
            mGroupLookup.Remove(g.StoryThread);
            // todo: Disconnect parents and children
        }
/*
        static void AddTargetGroupMember(
            CinemachineTargetGroup g, StoryManager.Subject subject, float radius)
        {
            int numTargets = 0;
            var targets = g.m_Targets;
            if (targets != null)
            {
                numTargets = targets.Length;
                for (int i = 0; i < numTargets; ++i)
                {
                    if (targets[i].target == subject.m_Transform)
                    {
                        targets[i].radius = radius;
                        g.m_Targets = targets;
                        return;
                    }
                }
            }
            var newTargets = new CinemachineTargetGroup.Target[numTargets + 1];
            if (numTargets > 0)
                targets.CopyTo(newTargets, 0);
            newTargets[numTargets].target = subject.m_Transform;
            newTargets[numTargets].weight = 1;
            newTargets[numTargets].radius = radius;
            g.m_Targets = newTargets;
        }
*/
    }
}
