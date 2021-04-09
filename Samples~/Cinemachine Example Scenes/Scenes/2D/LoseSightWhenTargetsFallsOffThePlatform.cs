using System;
using UnityEngine;

namespace Cinemachine.Examples
{
    public class LoseSightWhenTargetsFallsOffThePlatform : MonoBehaviour
    {
        [Tooltip("The platform from which LoseSightAtRange is calculated")]
        public Transform LowerPlatform;
        
        [Tooltip("The weight of a transform in the target group is 1 when above the Lower Platform. When a transform is " +
            "below the Lower Platform, then its weight decreases based on the distance between the transform and the " +
            "Lower Platform and it reaches 0 at LoseSightAtRange. If you set this value to 0, then the transform is removed " +
            "instantly when below the Lower Platform.")]
        [Range(0, 30)]
        public float LoseSightAtRange = 20;

        void Update()
        {
            // iterate through each target in the targetGroup
            for (var index = 0; index < m_TargetGroup.m_Targets.Length; index++)
            {
                // skip null targets
                if (m_TargetGroup.m_Targets[index].target == null)
                {
                    continue;
                }
                
                // if a target is below the LowerPlatform along the Y axis
                if (m_TargetGroup.m_Targets[index].target.position.y < LowerPlatform.position.y)
                {
                    // calculate the distance between target and the LowerPlatform along the Y axis
                    var yDistanceFromLowerPlatform = 
                        LowerPlatform.position.y - m_TargetGroup.m_Targets[index].target.position.y;
                    
                    // weight is 0 when yDistanceFromLowerPlatform = LoseSightAtRange, and is bigger than 0 otherwise
                    m_TargetGroup.m_Targets[index].weight = 
                        1f - yDistanceFromLowerPlatform / LoseSightAtRange;
                    
                    // ensure the weight is non-negative
                    m_TargetGroup.m_Targets[index].weight = Mathf.Max(m_TargetGroup.m_Targets[index].weight, 0);
                }
                // if a target is above the LowerPlatform along the Y axis
                else
                {
                    m_TargetGroup.m_Targets[index].weight = 1;
                }
            }
        }
        
        CinemachineTargetGroup m_TargetGroup;
        void Awake()
        {
            m_TargetGroup = GetComponent<CinemachineTargetGroup>();
        }

        void OnValidate()
        {
            LoseSightAtRange = Mathf.Clamp(LoseSightAtRange, 0f, 30f);
        }
    }
}
