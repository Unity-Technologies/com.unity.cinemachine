using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cinemachine;
using Cinemachine.Utility;

namespace Spectator
{
    [AddComponentMenu("Cinemachine/Spectator/Cinematographer")]
    public class Cinematographer : MonoBehaviour
    {
        [Range(0, 10)]
        public float m_TargetObjectRadius = 0.25f;
        [Range(1, 160)]
        public float[] m_fovFilters = new float[3] { 10, 30, 60 };

        public class CameraPoint
        {
            public int m_CPID;                  // Camera point ID, unique to this point
            public int m_cameraType;            // from an enum
            public int m_nodeID;                // used by GTFO to locate camera point
            public Vector3 m_cameraPos;
            public Vector3 m_cameraFwd;         // used if dynamic target is null
            public Transform m_dynamicTarget;   // may be null

            public CinemachineTargetGroup m_TargetGroup;

            public struct FovData
            {
                public float fov;
                public float shotQuality;
                public List<CinemachineTargetGroup.Target> targets;
            }
            public FovData[] m_fovData;

            public float m_lastActiveTime;
        }

        public struct CameraPointIndex
        {
            public CameraPoint cameraPoint;
            public int fovIndex;

            public bool SameAs(ref CameraPointIndex index)
            {
                return cameraPoint == index.cameraPoint && fovIndex == index.fovIndex;
            }

            public void Clear() { cameraPoint = null; fovIndex = 0; }

            // Debugging
            public string Name { get { return cameraPoint.m_TargetGroup.name; } }
            public int CameraTyep { get { return cameraPoint.m_cameraType; } }
            public float FOV { get { return cameraPoint.m_fovData[fovIndex].fov; } }
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

        /// <summary> Create a camera point with a given unique ID</summary> 
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
                cp = new CameraPoint() { m_fovData = new CameraPoint.FovData[m_fovFilters.Length] };
                for (int i = 0; i < cp.m_fovData.Length; ++i)
                {
                    cp.m_fovData[i].fov = m_fovFilters[i];
                    cp.m_fovData[i].shotQuality = 0;
                    cp.m_fovData[i].targets = new List<CinemachineTargetGroup.Target>();
                }
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

        /// <summary> Destroy the camera point with the given unique ID</summary> 
        public void DestroyCameraPoint(int cpid)
        {
            CameraPoint cp = LookupCameraPoint(cpid);
            if (cp != null)
            {
                DeactivateCameraPoint(cp);
                mCameraPointLookup.Remove(cpid);
                mCameraPointRecycleBin.Insert(0, cp); // GML slow
                mActiveCameraPoints.Remove(cp);
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
                cp.m_TargetGroup.name = name + cpid;
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

                // GML this is really bad - find some way to make this efficient, 
                // TODO: only do delta instead of this brute force
                for (int i = 0; i < cp.m_fovData.Length; ++i)
                {
                    for (int j = 0; j < cp.m_fovData[i].targets.Count; ++j)
                        RemoveReferenceInThread(cp.m_fovData[i].targets[j].target, cp, i);
                    cp.m_fovData[i].targets.Clear();
                }
                
                // Get camera forward
                Vector3 pos = cp.m_cameraPos;
                Vector3 fwd = Vector3.zero;
                if (cp.m_dynamicTarget != null)
                    fwd = (cp.m_dynamicTarget.position - pos).normalized;
                if (fwd.AlmostZero())
                {
#if false
                    fwd = cp.m_cameraFwd; // degenerate
#else
                    // Hack: Use group average position because the cam fwds weren't set properly
                    fwd = InventCameraForward(cp, visibleTargets);
#endif
                }

                // Update the targets
                bool gotOne = false;

                int numTargets = visibleTargets.Count;
                for (int i = 0; i < numTargets; ++i)
                {
                    if (visibleTargets[i] != null)
                    {
                        // Get the actual angle from the camera - does it fit into our FOV?
                        Vector3 dir = (visibleTargets[i].position - pos).normalized;
                        float angle = UnityVectorExtensions.Angle(fwd, dir);
                        for (int j = 0; j < cp.m_fovData.Length; ++j)
                        {
                            if (angle <= cp.m_fovData[j].fov / 2)
                            {
                                cp.m_fovData[j].targets.Add(new CinemachineTargetGroup.Target 
                                    { target = visibleTargets[i], weight = 1, radius = m_TargetObjectRadius});
                                AddReferenceInThread(visibleTargets[i], cp, j);
                                gotOne = true;
                            }
                        }
                    }
                }
                // Make sure a non-empty group is active
                cp.m_TargetGroup.gameObject.SetActive(gotOne);
                AssessShotQuality(cp);
            }
        }

        void DeactivateCameraPoint(CameraPoint cp)
        {
            cp.m_TargetGroup.gameObject.SetActive(false);
            for (int i = 0; i < cp.m_fovData.Length; ++i)
            {
                cp.m_fovData[i].fov = m_fovFilters[i];
                cp.m_fovData[i].shotQuality = 0;
                for (int j = 0; j < cp.m_fovData[i].targets.Count; ++j)
                {
                    RemoveReferenceInThread(cp.m_fovData[i].targets[j].target, cp, i);
                    cp.m_fovData[i].targets.Clear();
                }
            }
            for (int i = 0; i < cp.m_TargetGroup.m_Targets.Length; ++i)
            {
                cp.m_TargetGroup.m_Targets[i].target = null;
                cp.m_TargetGroup.m_Targets[i].weight = 0;
            }
        }

        // GML temp
        void PruneStaleCameraPoints(float timestamp)
        {
            // Disable the target groups that are no longer valid
            for (int i = mActiveCameraPoints.Count-1; i >= 0; --i)
            {
                CameraPoint cp = mActiveCameraPoints[i];
                if (cp.m_lastActiveTime < timestamp)
                {
                    DeactivateCameraPoint(cp);
                    // If it's been quite long enough, trash it
                    if (timestamp - cp.m_lastActiveTime > 5)
                        DestroyCameraPoint(cp.m_CPID);
                }
            }
        }

        // GML Hack: Use group average position because the cam fwds weren't set properly
        Vector3 InventCameraForward(CameraPoint cp, List<Transform> visibleTargets)
        {
            Vector3 pos = cp.m_cameraPos;
            Vector3 fwd = Vector3.forward;
            int numFound = 0;
            for (int i = 0; i < visibleTargets.Count; ++i)
            {
                if (visibleTargets[i] != null)
                {
                    fwd += visibleTargets[i].position;
                    ++numFound;
                }
            }
            if (numFound > 0)
            {
                fwd /= numFound;
                fwd = (fwd - pos).normalized;
/*
                Vector3 center = fwd;
                float best = 0;
                for (int i = 0; i < visibleTargets.Count; ++i) 
                {
                    if (visibleTargets[i] != null)
                    {
                        // Get the actual angle from the camera - does it fit into our FOV?
                        Vector3 dir = (visibleTargets[i].position - pos).normalized;
                        float dot = Vector3.Dot(center, dir);
                        if (dot > best)
                        {
                            best = dot;
                            fwd = dir;
                        }
                    }
                }
*/
            }
            return fwd;
        }

        void AddReferenceInThread(Transform target, CameraPoint cp, int index)
        {
            if (target != null)
            {
                var th = StoryManager.Instance.LookupStoryThread(target);
                if (th != null)
                {
                    var item = new CameraPointIndex { cameraPoint = cp, fovIndex = index };
                    th.m_cameraPoints.Add(item);
                }
            }
        }

        void RemoveReferenceInThread(Transform target, CameraPoint cp, int index)
        {
            if (target != null)
            {
                var th = StoryManager.Instance.LookupStoryThread(target);
                if (th != null)
                {
                    var item = new CameraPointIndex { cameraPoint = cp, fovIndex = index };
                    for (int i = th.m_cameraPoints.Count-1; i >= 0; --i)
                        if (th.m_cameraPoints[i].SameAs(ref item))
                            th.m_cameraPoints.RemoveAt(i);
                }
            }
        }

        // Shot Quality

        [Header("Shot Evaluation")]
        [Tooltip("If greater than zero, a higher score will be given to shots when the target is closer to this distance.  Set this to zero to disable this feature.")]
        public float m_OptimalTargetDistance = 0;

        void AssessShotQuality(CameraPoint cp)
        {
            // We get score for every visible target, weighted by normalized urgency and target distance
            float urgencySum = StoryManager.Instance.SumOfAllUrgencies;
            for (int i = 0; i < cp.m_fovData.Length; ++i)
            {
                cp.m_fovData[i].shotQuality = 0;
                for (int j = 0; j < cp.m_fovData[i].targets.Count; ++j)
                {
                    var t = cp.m_fovData[i].targets[j];
                    if (t.weight > 0 && t.target != null)
                    {
                        var th = StoryManager.Instance.LookupStoryThread(t.target);
                        if (th != null)
                        {
                            float quality = th.Urgency;
                            if (urgencySum > UnityVectorExtensions.Epsilon)
                                quality /= urgencySum; // normalized
                            
                            quality *= GetQualityBoostForTargetDistance(t.target.position, cp.m_cameraPos);
                            cp.m_fovData[i].shotQuality += quality;
                        }
                    }
                }
            }
        }

        float GetQualityBoostForTargetDistance(Vector3 targetPos, Vector3 cameraPos)
        {
            // Boost quality if target is close to optimal nearness
            float nearnessBoost = 0;
            const float kMaxNearBoost = 0.2f;
            if (m_OptimalTargetDistance > 0)
            {
                float distance = Vector3.Magnitude(targetPos - cameraPos);
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
            }
            return 1f + nearnessBoost;
        }
    }
}
