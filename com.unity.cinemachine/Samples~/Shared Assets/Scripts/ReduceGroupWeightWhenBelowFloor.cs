using System;
using UnityEngine;

namespace Unity.Cinemachine.Samples
{
    [RequireComponent(typeof(CinemachineTargetGroup))]
    public class ReduceGroupWeightWhenBelowFloor : MonoBehaviour
    {
        [Tooltip("The platform from which LoseSightAtRange is calculated")]
        public Transform Floor;
        
        [Tooltip("The weight of a transform in the target group is 1 when above the Floor. When a transform is " +
            "below the Floor, then its weight decreases based on the distance between the transform and the " +
            "Floor and it reaches 0 at LoseSightAtRange. If you set this value to 0, then the transform is removed " +
            "instantly when below the Floor.")]
        [Range(0, 30)]
        public float LoseSightAtRange = 20;

        CinemachineTargetGroup m_TargetGroup;

        void Awake()
        {
            m_TargetGroup = GetComponent<CinemachineTargetGroup>();
        }

        void Update()
        {
            // iterate through each target in the targetGroup
            var floor = Floor.position.y;
            for (int i = 0; i < m_TargetGroup.Targets.Count; ++i)
            {
                var target = m_TargetGroup.Targets[i];

                // calculate the distance between target and the Floor along the Y axis
                var distanceBelow = floor - target.Object.position.y;

                // weight goes to 0 if it's farther below than LoseSightAtRange
                var weight = Mathf.Clamp(1 - distanceBelow / Mathf.Max(0.001f, LoseSightAtRange), 0, 1);
                target.Weight = weight;
            }
        }
    }
}
