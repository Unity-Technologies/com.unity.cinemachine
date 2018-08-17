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
        public float m_proximitySlush = 1;
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
        Dictionary<CinemachineTargetGroup, AggregatedGroup> mGroupMemberLookup
            = new Dictionary<CinemachineTargetGroup, AggregatedGroup>();

        private void Start()
        {
            m_StoryManager = StoryManager.Instance;
        }

        public AggregatedGroup LookupGroup(StoryManager.StoryThread subject)
        {
            AggregatedGroup ag = null;
            mGroupLookup.TryGetValue(subject, out ag);
            return ag;
        }

        void DestroyGroup(AggregatedGroup ag)
        {
            // Disconnect parents 
            for (int i = 0; i < ag.m_Parents.Count; ++i)
            {
                AggregatedGroup parent;
                if (mGroupLookup.TryGetValue(ag.m_Parents[i], out parent))
                    parent.m_Children.Remove(ag.StoryThread);
                else
                    ag.m_Parents[i].TargetGroup.RemoveMember(ag.StoryThread.TargetGroup.transform);
            }
            ag.m_Parents.Clear();

            // Disconnect children
            for (int i = 0; i < ag.m_Children.Count; ++i)
            {
                AggregatedGroup child;
                if (mGroupLookup.TryGetValue(ag.m_Children[i], out child))
                    child.m_Parents.Remove(ag.StoryThread);
                else
                    mGroupMemberLookup.Remove(ag.StoryThread.TargetGroup);
            }
            ag.m_Children.Clear();

            // Delete the empty group and its thread
            mGroupLookup.Remove(ag.StoryThread);
            StoryManager.Instance.DestroyStoryThread(ag.StoryThread);
        }

        void AddMember(AggregatedGroup ag, StoryManager.StoryThread member)
        {
            AggregatedGroup agMember;
            if (mGroupLookup.TryGetValue(member, out agMember))
            {
                agMember.m_Parents.Add(ag.StoryThread);
                ag.m_Children.Add(agMember.StoryThread);
            }
            else
            {
                mGroupMemberLookup[member.TargetGroup] = ag;
            }
            ag.StoryThread.TargetGroup.AddMember(
                member.TargetGroup.transform, 1, member.TargetGroup.Sphere.radius);
        }

        // Todo: make this automatic within CinemachineTargetGroup
        private void UpdateGroupRadii(AggregatedGroup ag)
        {
            for (int i = 0; i < ag.m_Children.Count; ++i)
            {
                int index = ag.StoryThread.TargetGroup.FindMember(
                    ag.m_Children[i].TargetGroup.transform);
                if (index >= 0)
                    ag.StoryThread.TargetGroup.m_Targets[index].radius 
                        = ag.m_Children[i].TargetGroup.Sphere.radius;
            }
        }

        private bool GroupIsTooBig(AggregatedGroup ag)
        {
            float r = ag.StoryThread.TargetGroup.Sphere.radius;
            if (r > m_proximityThreshold + m_proximitySlush)
                return true;
            return false;
        }

        private void UpdateGroupInterestLevel(AggregatedGroup ag)
        {
            // Group interest level is just the sum of the members
            float interest = 0;
            var targets = ag.StoryThread.TargetGroup.m_Targets;
            for (int i = 0; i < targets.Length; ++i)
            {
                var th = m_StoryManager.LookupStoryThread(targets[i].target);
                if (th != null)
                    interest += th.InterestLevel;
            }
            // Significantly less interesting if too big
            if (GroupIsTooBig(ag))
                interest /= 2;
            ag.StoryThread.InterestLevel = interest;
        }

        private void CreateGroupsIfCloseEnough(AggregatedGroup ag, List<AggregatedGroup> newGroups)
        {
            // GML todo
            // AggregatedGroup ag = new AggregatedGroup(m_StoryManager.CreateStoryThread(name));
            // newGroups.Add(ag);
        }

        List<AggregatedGroup> mScratchList = new List<AggregatedGroup>();
        private void Update()
        {
            // Update radii
            var it = mGroupLookup.GetEnumerator();
            while (it.MoveNext())
                UpdateGroupRadii(it.Current.Value);

            // Disband groups that are too large or degenerate
            mScratchList.Clear();
            it = mGroupLookup.GetEnumerator();
            while (it.MoveNext())
            {
                var ag = it.Current.Value;
                if (m_StoryManager.LiveThread != ag.StoryThread)
                {
                    if (GroupIsTooBig(ag) || ag.StoryThread.TargetGroup.m_Targets.Length < 2)
                        mScratchList.Add(ag);
                }
            }
            for (int i = 0; i < mScratchList.Count; ++i)
                DestroyGroup(mScratchList[i]);

            // Create new groups for threads that are close enough
            mScratchList.Clear();
            it = mGroupLookup.GetEnumerator();
            while (it.MoveNext())
                CreateGroupsIfCloseEnough(it.Current.Value, mScratchList);
            for (int i = 0; i < mScratchList.Count; ++i)
                mGroupLookup[mScratchList[i].StoryThread] = mScratchList[i];
            mScratchList.Clear();

            // Update interest levels
            it = mGroupLookup.GetEnumerator();
            while (it.MoveNext())
            {
                var ag = it.Current.Value;
                UpdateGroupInterestLevel(ag);
            }
        }
    }
}
