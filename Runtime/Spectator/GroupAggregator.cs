using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cinemachine;

namespace Spectator
{
    /// Group Aggregator
    /// 
    /// - Dynamically creates and destroys groups
    /// - Each story thread has a target group, containing the item of interest.  
    ///     By default the target group will contain a single object - the item of interest 
    ///     that created the thread
    /// - Group aggregator dynamically creates and destroys groups based on 
    ///     proximity (player + enemy, groups of players, groups of enemies, etc).
    /// - New story threads are then created for the new groups.  
    ///     Interest level of the new threads is an aggregate of the interest levels of the members
    /// - Polls periodically
    /// 
    public class GroupAggregator : MonoBehaviour
    {   
        public float m_proximityThreshold = 5;
        public float m_updateTimeStep = 2;
        public Transform m_Root;

        public class Group
        {
            public CinemachineTargetGroup m_TargetGroup;
            public List<Group> m_Parents;
            public List<Group> m_Children;
        }
        Dictionary<StoryManager.Subject, Group> mGroupLookup;
        
        public Group GetGroup(StoryManager.Subject subject)
        {
            Group g;
            if (mGroupLookup == null)
                mGroupLookup = new Dictionary<StoryManager.Subject, Group>();
            mGroupLookup.TryGetValue(subject, out g);
            return g;
        }

        List<CinemachineTargetGroup> mGroupRecycleBin = new List<CinemachineTargetGroup>();

        public Group CreateGroup(StoryManager.Subject subject, float radius)
        {
            CinemachineTargetGroup cmTargetGroup = null;
            if (mGroupRecycleBin.Count > 0)
            {
                cmTargetGroup = mGroupRecycleBin[0];
                mGroupRecycleBin.RemoveAt(0);
                cmTargetGroup.gameObject.name = subject.m_Transform.name;
            }
            else
            {
                GameObject go = new GameObject(subject.m_Transform.name);
                cmTargetGroup = go.AddComponent<CinemachineTargetGroup>();
            }
            cmTargetGroup.m_Targets = 
            AddTargetGroupMember(cmTargetGroup, subject, radius);

        }

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
    }
}
