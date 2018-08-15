using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cinemachine;

namespace Spectator
{
    /// AggregatedGroup Aggregator
    /// 
    /// - Dynamically creates and destroys groups
    /// - Each story thread has a target group, containing the item of interest.  
    ///     By default the target group will contain a single object - the item of interest 
    ///     that created the thread
    /// - AggregatedGroup aggregator dynamically creates and destroys groups based on 
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

        public class AggregatedGroup
        {
            StoryManager.StoryThread m_StoryThread;

            public AggregatedGroup(StoryManager.StoryThread th) { m_StoryThread = th; }
            public StoryManager.StoryThread StoryThread { get { return m_StoryThread; } }
            public List<StoryManager.StoryThread> m_Parents = new List<StoryManager.StoryThread>();
            public List<StoryManager.StoryThread> m_Children = new List<StoryManager.StoryThread>();
        }
        Dictionary<StoryManager.StoryThread, AggregatedGroup> mGroupLookup 
            = new Dictionary<StoryManager.StoryThread, AggregatedGroup>();

        private void Start()
        {
            m_StoryManager = GetComponent<StoryManager>();
        }

        public AggregatedGroup LookupGroup(StoryManager.StoryThread subject)
        {
            AggregatedGroup g = null;
            mGroupLookup.TryGetValue(subject, out g);
            return g;
        }

        public AggregatedGroup CreateGroup(string name)
        {
            AggregatedGroup g = new AggregatedGroup(m_StoryManager.CreateStoryThread(name));
            mGroupLookup[g.StoryThread] = g;
            return g;
        }

        public void DestroyGroup(AggregatedGroup g)
        {
            // Disconnect parents 
            for (int i = 0; i < g.m_Parents.Count; ++i)
            {
                AggregatedGroup parent;
                if (mGroupLookup.TryGetValue(g.m_Parents[i], out parent))
                    parent.m_Children.Remove(g.StoryThread);
                else
                    g.m_Parents[i].TargetGroup.RemoveMember(g.StoryThread.TargetGroup.transform);
            }
            g.m_Parents.Clear();

            // Disconnect children
            for (int i = 0; i < g.m_Children.Count; ++i)
            {
                AggregatedGroup child;
                if (mGroupLookup.TryGetValue(g.m_Children[i], out child))
                    child.m_Parents.Remove(g.StoryThread);
                else
                    g.m_Children[i].TargetGroup.RemoveMember(g.StoryThread.TargetGroup.transform);
            }
            g.m_Children.Clear();

            // Delete the empty group and its thread
            mGroupLookup.Remove(g.StoryThread);
            StoryManager.Instance.DestroyStoryThread(g.StoryThread);
        }
    }
}
