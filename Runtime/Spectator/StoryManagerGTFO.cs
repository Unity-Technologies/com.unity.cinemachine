using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cinemachine;
using Cinemachine.Utility;

namespace Spectator
{
    public class StoryManagerGTFO : MonoBehaviour
    {
        private static StoryManagerGTFO sInstance = null;

        /// <summary>Get the singleton instance</summary>
        public static StoryManagerGTFO Instance
        {
            get
            {
                if (sInstance == null)
                    sInstance = FindObjectsOfType<StoryManagerGTFO>()[0];
                return sInstance;
            }
        }

        StoryManager m_StoryManager;
        private void Awake()
        {
            m_StoryManager = StoryManager.Instance;
        }

        // Section 1 - Story Thread Management

        List<StoryManager.StoryThread> mThreadList = new List<StoryManager.StoryThread>();
        Dictionary<Transform, StoryManager.StoryThread> mThreadLookup 
            = new Dictionary<Transform, StoryManager.StoryThread>();

        public class ThreadClientData
        {
            public float m_ValidityTimestamp;
            public List<CameraPoint> m_CameraPoints = new List<CameraPoint>();
        }

        StoryManager.StoryThread LookupThread(Transform target)
        {
            StoryManager.StoryThread st = null;
            if (target != null)
                mThreadLookup.TryGetValue(target, out st);
            return st;
        }

        /// <summary>Call this to set the current list of interesting objects</summary> 
        public void SetInterestingObjects(List<Transform> newObjects)
        {
            float now = Time.time;
            
            int numGiven = newObjects.Count;
            for (int i = 0; i < numGiven; ++i)
            {
                var th = LookupThread(newObjects[i]);
                if (th == null)
                {
                    // Create the thread
                    th = StoryManager.Instance.CreateStoryThread(newObjects[i]);
                    th.ClientData = new ThreadClientData();
                    th.InterestLevel = 1;
                    mThreadLookup[newObjects[i]] = th;
                }
                // Update the thread
                var cd = th.ClientData as ThreadClientData;
                cd.m_ValidityTimestamp = now;
            }

            // Prune stale threads
            int numThreads = StoryManager.Instance.NumThreads;
            for (int i = StoryManager.Instance.NumThreads-1; i >= 0; --i)
            {
                var th = StoryManager.Instance.GetThread(i);
                var cd = th.ClientData as ThreadClientData;
                if (cd == null || cd.m_ValidityTimestamp != now)
                {
                    // Don't prune if live and onscreen for too short a time
                    if (!StoryManager.Instance.ThreadIsLive(th) 
                            || th.LastOnScreenDuration >= StoryManager.Instance.m_MinimumThreadTime)
                        StoryManager.Instance.DestroyStoryThread(th);
                }
            }
        }

        /// <summary>Call this to set the interest level of a current target</summary> 
        public bool SetInterestLevel(Transform target, float interest)
        {
            var th = LookupThread(target);
            if (th == null)
                return false;
            th.InterestLevel = interest;
            return true;
        }


        // Section 2 - Camera Point and Group Management

        [Range(0, 10)]
        public float m_TargetObjectRadius = 0.25f;
        [Range(1, 160)]
        public float m_FovFilter = 30;

        public class CameraPoint
        {
            public float m_validityTimestamp;
            public Transform m_cameraPoint;
            public CinemachineTargetGroup m_TargetGroup;
            public float m_shotQuality;
        }

        Dictionary<Transform, CameraPoint> mCameraPointLookup = new Dictionary<Transform, CameraPoint>();

        public CameraPoint LookupCameraPoint(Transform target)
        {
            CameraPoint cp = null;
            if (target != null)
                mCameraPointLookup.TryGetValue(target, out cp);
            return cp;
        }

        float mLastValidityTimestamp;

        /// <summary>Call this to set camera point visibility data</summary>
        public void SetCameraObjectVisibility(
            Transform cameraPoint,
            Transform centerTarget,        // what the camera is looking at (may be null)
            List<Transform> visibleTargets) // fixed-length, entries can be null
        {
            // Disable the target groups that are no longer valid
            float timestamp = Time.time;
            if (mLastValidityTimestamp != timestamp)
                DisableStaleGroups(mLastValidityTimestamp);
            mLastValidityTimestamp = timestamp;

            // Update our target groups
            int numTargets = visibleTargets.Count;
            CameraPoint cp = LookupCameraPoint(cameraPoint);
            if (cp == null)
            {
                // Create the camera point object and target group
                cp = new CameraPoint() { m_cameraPoint = cameraPoint };
                GameObject go = new GameObject(cameraPoint.name + cameraPoint.GetInstanceID());
                mCameraPointLookup[cameraPoint] = cp;
                go.transform.SetParent(this.transform);
                cp.m_TargetGroup = go.AddComponent<CinemachineTargetGroup>();
                cp.m_TargetGroup.m_UpdateMethod = CinemachineTargetGroup.UpdateMethod.LateUpdate;
                cp.m_TargetGroup.m_Targets = new CinemachineTargetGroup.Target[numTargets];
            }

            // Get camera forward
            Vector3 pos = cameraPoint.transform.position;
            Vector3 fwd = cameraPoint.transform.forward;
            if (centerTarget != null)
                fwd = centerTarget.transform.position - pos;
            if (fwd.AlmostZero())
                fwd = cameraPoint.transform.forward; // degenerate

            // Update the targets
            bool gotOne = false;
            if (cp.m_TargetGroup.m_Targets.Length < visibleTargets.Count)
                cp.m_TargetGroup.m_Targets = new CinemachineTargetGroup.Target[numTargets];
            for (int i = 0; i < cp.m_TargetGroup.m_Targets.Length; ++i)
            {
                bool isValid = false;
                if (i < numTargets && visibleTargets[i] != null)
                {
                    // Get the actual angle from the camera - does it fit into our FOV?
                    Vector3 dir = visibleTargets[i].position - pos;
                    float angle = UnityVectorExtensions.Angle(fwd, dir);
                    if (angle < m_FovFilter / 2)
                        isValid = true;
                }
                if (isValid)
                {
                    cp.m_TargetGroup.m_Targets[i].weight = 1;
                    cp.m_TargetGroup.m_Targets[i].target = visibleTargets[i];
                    cp.m_TargetGroup.m_Targets[i].radius = m_TargetObjectRadius;
                    AddReferenceInThread(cp, cp.m_TargetGroup.m_Targets[i].target);
                    gotOne = true;
                }
                else
                {
                    RemoveReferenceInThread(cp, cp.m_TargetGroup.m_Targets[i].target);
                    cp.m_TargetGroup.m_Targets[i].weight = 0;
                    cp.m_TargetGroup.m_Targets[i].target = null;
                }

            }
            // Make sure a non-empty group is active
            if (gotOne)
            {
                cp.m_TargetGroup.gameObject.SetActive(true);
                cp.m_validityTimestamp = timestamp;
                AssessShotQuality(cp);
            }
        }

        public void DisableStaleGroups(float timestamp)
        {
            // Disable the target groups that are no longer valid
            var it = mCameraPointLookup.GetEnumerator();
            while (it.MoveNext())
            {
                var cp = it.Current.Value;
                if (cp.m_validityTimestamp != timestamp 
                    && cp.m_TargetGroup.gameObject.activeSelf)
                {
                    for (int i = 0; i < cp.m_TargetGroup.m_Targets.Length; ++i)
                        RemoveReferenceInThread(cp, cp.m_TargetGroup.m_Targets[i].target);
                    cp.m_TargetGroup.gameObject.SetActive(false);
                    cp.m_shotQuality = 0;
                }
            }
        }

        void AddReferenceInThread(CameraPoint cp, Transform target)
        {
            if (target != null)
            {
                var th = LookupThread(target);
                if (th != null)
                {
                    var cd = th.ClientData as ThreadClientData;
                    if (cd != null && !cd.m_CameraPoints.Contains(cp))
                        cd.m_CameraPoints.Add(cp);
                }
            }
        }

        void RemoveReferenceInThread(CameraPoint cp, Transform target)
        {
            if (target != null)
            {
                var th = LookupThread(target);
                if (th != null)
                {
                    var cd = th.ClientData as ThreadClientData;
                    if (cd != null)
                        cd.m_CameraPoints.Remove(cp);
                }
            }
        }

        // Section 3 - Shot Quality

        [Header("Shot Evaluation")]
        [Tooltip("If greater than zero, a higher score will be given to shots when the target is closer to this distance.  Set this to zero to disable this feature.")]
        public float m_OptimalTargetDistance = 0;

        void AssessShotQuality(CameraPoint cp)
        {
            // We get score for every visible target, weighted by normalized urgency and target distance
            cp.m_shotQuality = 0;
            Vector3 pos = cp.m_cameraPoint.position;
            for (int i = 0; i < cp.m_TargetGroup.m_Targets.Length; ++i)
            {
                if (cp.m_TargetGroup.m_Targets[i].weight > 0 && cp.m_TargetGroup.m_Targets[i].target != null)
                {
                    var th = LookupThread(cp.m_TargetGroup.m_Targets[i].target);
                    if (th != null)
                    {
                        float quality = th.Urgency;
                        float urgencySum = StoryManager.Instance.SumOfAllUrgencies;
                        if (urgencySum > UnityVectorExtensions.Epsilon)
                            quality /= urgencySum; // normalized

                        // Boost quality if target is close to optimal nearness
                        float nearnessBoost = 0;
                        const float kMaxNearBoost = 0.2f;
                        if (m_OptimalTargetDistance > 0)
                        {
                            float distance = Vector3.Magnitude(
                                cp.m_TargetGroup.m_Targets[i].target.position - pos);
                            if (distance <= m_OptimalTargetDistance)
                            {
                                float threshold = m_OptimalTargetDistance / 2;
                                if (distance >= threshold)
                                    nearnessBoost = kMaxNearBoost * (distance - threshold)
                                        / (m_OptimalTargetDistance - threshold);
                            }
                            else
                            {
                                distance -= m_OptimalTargetDistance;
                                float threshold = m_OptimalTargetDistance * 3;
                                if (distance < threshold)
                                    nearnessBoost = kMaxNearBoost * (1f - (distance / threshold));
                            }
                            quality *= (1f + nearnessBoost);
                        }
                        cp.m_shotQuality += quality;
                    }
                }
            }
        }
    }
}
