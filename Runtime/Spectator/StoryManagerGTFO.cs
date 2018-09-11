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
            public List<CameraPoint> m_CameraPoints = new List<CameraPoint>();
        }

        StoryManager.StoryThread LookupThread(Transform target)
        {
            StoryManager.StoryThread st = null;
            if (target != null)
                mThreadLookup.TryGetValue(target, out st);
            return st;
        }

        /// <summary>Add items to the current list of interesting objects</summary> 
        public void CreateInterestingObjects(List<Transform> objects)
        {
            int numGiven = objects.Count;
            for (int i = 0; i < numGiven; ++i)
            {
                var th = LookupThread(objects[i]);
                if (th == null)
                {
                    // Create the thread
                    th = StoryManager.Instance.CreateStoryThread(objects[i]);
                    th.ClientData = new ThreadClientData();
                    th.InterestLevel = 1;
                    mThreadLookup[objects[i]] = th;
                }
            }
        }

        /// <summary>Remove items from the current list of interesting objects</summary> 
        public void DestroyInterestingObjects(List<Transform> objects)
        {
            int numGiven = objects.Count;
            for (int i = 0; i < numGiven; ++i)
            {
                var th = LookupThread(objects[i]);
                if (th != null)
                    StoryManager.Instance.DestroyStoryThread(th);
            }
        }

        /// <summary>Set the interest level of an interesting object</summary> 
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
        public float m_FovFilter = 40;

        public class CameraPoint
        {
            public int m_CPID;                  // Camera point ID, unique to this point
            public int m_cameraType;            // from an enum
            public int m_nodeID;                // used by GTFO to locate camera point
            public Vector3 m_cameraPos;
            public Vector3 m_cameraFwd;         // used if dynamic target is null
            public Transform m_dynamicTarget;   // may be null

            public CinemachineTargetGroup m_TargetGroup;
            public float m_shotQuality;

            public float m_lastActiveTime;
        }

        Dictionary<int, CameraPoint> mCameraPointLookup = new Dictionary<int, CameraPoint>();
        List<CameraPoint> mActiveCameraPoints = new List<CameraPoint>();
        List<CameraPoint> mCameraPointRecycleBin = new List<CameraPoint>();

        public CameraPoint LookupCameraPoint(int cpid)
        {
            CameraPoint cp = null;
            mCameraPointLookup.TryGetValue(cpid, out cp);
            return cp;
        }

        public void CreateCameraPoint(int cpid, int cameraType, Transform target)
        {
            CameraPoint cp = LookupCameraPoint(cpid);
            if (cp == null)
            {
                int index = mCameraPointRecycleBin.Count - 1;
                if (index >= 0)
                {
                    cp = mCameraPointRecycleBin[index];
                    mCameraPointRecycleBin.RemoveAt(index);
                    mActiveCameraPoints.Add(cp);
                }
            }
            if (cp == null)
            {
                // Create the camera point object and target group
                cp = new CameraPoint();
                GameObject go = new GameObject();
                go.transform.SetParent(this.transform);
                cp.m_TargetGroup = go.AddComponent<CinemachineTargetGroup>();
                cp.m_TargetGroup.m_UpdateMethod = CinemachineTargetGroup.UpdateMethod.LateUpdate;
                cp.m_TargetGroup.m_Targets = new CinemachineTargetGroup.Target[1];
                mActiveCameraPoints.Add(cp);
            }
            mCameraPointLookup[cpid] = cp;
            cp.m_CPID = cpid;
            cp.m_cameraType = cameraType;
            cp.m_dynamicTarget = target;
            cp.m_TargetGroup.name = cpid.ToString();    // GML debugging
            cp.m_TargetGroup.gameObject.SetActive(true);
        }

        public void DestroyCameraPoint(int cpid)
        {
            CameraPoint cp = LookupCameraPoint(cpid);
            if (cp != null)
            {
                mCameraPointLookup.Remove(cpid);
                mCameraPointRecycleBin.Insert(0, cp); // GML slow
                mActiveCameraPoints.Remove(cp);
                cp.m_TargetGroup.gameObject.SetActive(false);

                for (int i = 0; i < cp.m_TargetGroup.m_Targets.Length; ++i)
                {
                    RemoveReferenceInThread(cp, cp.m_TargetGroup.m_Targets[i].target);
                    cp.m_TargetGroup.m_Targets[i].weight = 0;
                    cp.m_TargetGroup.m_Targets[i].target = null;
                }
            }
        }

        public void SetCameraPoint(int cpid, Vector3 position, Vector3 fwd, int nodeId)
        {
            CameraPoint cp = LookupCameraPoint(cpid);
            if (cp != null)
            {
                cp.m_cameraPos = position;
                cp.m_cameraFwd = fwd;
                cp.m_nodeID = nodeId;
            }
        }

        // Debugging
        public void SetCameraPointName(int cpid, string name)
        {
            CameraPoint cp = LookupCameraPoint(cpid);
            if (cp != null)
            {
                cp.m_TargetGroup.name = name;
            }
        }

        private float mLastCameraPointUpdateTime = 0;

        /// <summary>Set camera point visibility data</summary>
        public void SetCameraPointObjectVisibility(
            int cpid, List<Transform> visibleTargets) // fixed-length, entries can be null
        {
            // GML temp
            float now = Time.time;
            if (mLastCameraPointUpdateTime != now)
                PruneStaleCameraPoints(mLastCameraPointUpdateTime);
            mLastCameraPointUpdateTime = now;

            CameraPoint cp = LookupCameraPoint(cpid);
            if (cp != null)
            {
                cp.m_lastActiveTime = now;

                // Get camera forward
                Vector3 pos = cp.m_cameraPos;
                Vector3 fwd = cp.m_cameraFwd;
                if (cp.m_dynamicTarget != null)
                    fwd = (cp.m_dynamicTarget.position - pos).normalized;
                if (fwd.AlmostZero())
                    fwd = cp.m_cameraFwd; // degenerate

                // Update the targets
                int numTargets = visibleTargets.Count;
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
                cp.m_TargetGroup.gameObject.SetActive(gotOne);
                AssessShotQuality(cp);
            }
            // Temp here to clean up stale ones (should be in Update())
            RemoveStaleCameraPoint();
        }

        // GML temp
        public void PruneStaleCameraPoints(float timestamp)
        {
            // Disable the target groups that are no longer valid
            for (int i = mActiveCameraPoints.Count-1; i>= 0; --i)
            {
                if (mActiveCameraPoints[i].m_lastActiveTime != timestamp)
                    DestroyCameraPoint(mActiveCameraPoints[i].m_CPID);
            }
        }

        int mLastIndexCheckForStaleness = 0;
        void RemoveStaleCameraPoint()
        {
            int numActive = mActiveCameraPoints.Count;
            if (numActive > 0)
            {
                if (++mLastIndexCheckForStaleness >= numActive)
                    mLastIndexCheckForStaleness = 0;
                if (mActiveCameraPoints[mLastIndexCheckForStaleness].m_lastActiveTime < Time.time - 10)
                {
                    DestroyCameraPoint(mActiveCameraPoints[mLastIndexCheckForStaleness].m_CPID);
                    --mLastIndexCheckForStaleness;
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
            Vector3 pos = cp.m_cameraPos;
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
