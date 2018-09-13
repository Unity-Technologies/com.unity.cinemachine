using Cinemachine;
using Cinemachine.Utility;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Spectator
{
    public class SpectatorShotQualityEvaluator : MonoBehaviour 
    {
        [Tooltip("This must match the camera type setting in the client (e.g. static vs roaming).")]
        public int m_cameraType;

        [Tooltip("If greater than zero, a higher score will be given to shots when the target is closer to this distance.  Set this to zero to disable this feature.")]
        public float m_optimalTargetDistance;

        [Tooltip("Lens FOV matching will not apply if this is set.")]
        public bool m_hasZoomLens;

//        VolatileWeightSet m_weightSet;
//        public void AddWeight(VolatileWeight w) { m_weightSet.SetWeight(w); }
//        public float DecayWeight(float deltaTime) { return m_weightSet.Decay(deltaTime); }

        CinemachineVirtualCameraBase mVcam;

        private void Start()
        {
            mVcam = GetComponent<CinemachineVirtualCameraBase>();
        }

        public float AssessShotQuality(
            Cinematographer.CameraPointIndex cpi, int preferredCameraType, int preferredLensIndex)
        {
            if (cpi.cameraPoint == null)
                return -1;
            float q = AssessShotQualityForTargets(cpi);
            if (m_cameraType == preferredCameraType)
                q *= StoryManager.Instance.m_TuningConstants.CameraTypeSelectionBoost;

            if (cpi.fovIndex == preferredLensIndex)
                q *= StoryManager.Instance.m_TuningConstants.CameraLensSelectionBoost;

            return q;
        }

        public float AssessShotQualityForTargets(Cinematographer.CameraPointIndex cpi)
        {
            // We get score for every visible target, weighted by urgency and target distance
            float quality = 0;
            var cp = cpi.cameraPoint;
            for (int i = 0; i < cp.m_fovData[cpi.fovIndex].targets.Count; ++i)
            {
                var t = cp.m_fovData[cpi.fovIndex].targets[i];
                if (t.weight > 0 && t.target != null)
                {
                    var th = StoryManager.Instance.LookupStoryThread(t.target);
                    if (th != null)
                    {
                        float q = th.Urgency;
                        q *= GetQualityBoostForTargetDistance(t.target.position, cp.m_cameraPos);
                        quality += q;
                    }
                }
            }
            return quality;
        }

        float GetQualityBoostForTargetDistance(Vector3 targetPos, Vector3 cameraPos)
        {
            // Boost quality if target is close to optimal nearness
            float nearnessBoost = 0;
            if (m_optimalTargetDistance > 0)
            {
                float kMaxNearBoost = StoryManager.Instance.m_TuningConstants.OptimalTargetDistanceMultiplier;
                float distance = Vector3.Magnitude(targetPos - cameraPos);
                if (distance <= m_optimalTargetDistance)
                {
                    float threshold = m_optimalTargetDistance / 2;
                    if (distance >= threshold)
                        nearnessBoost = kMaxNearBoost * (distance - threshold)
                            / (m_optimalTargetDistance - threshold);
                }
                else
                {
                    distance -= m_optimalTargetDistance;
                    float threshold = m_optimalTargetDistance * 3;
                    if (distance < threshold)
                        nearnessBoost = kMaxNearBoost * (1f - (distance / threshold));
                }
            }
            return 1f + nearnessBoost;
        }

    }
}
