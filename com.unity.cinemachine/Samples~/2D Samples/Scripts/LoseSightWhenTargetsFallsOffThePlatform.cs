using System;
using UnityEngine;

namespace Cinemachine.Examples
{
    [RequireComponent(typeof(CinemachineTargetGroup))]
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

        CinemachineTargetGroup m_TargetGroup;

        void Awake()
        {
            m_TargetGroup = GetComponent<CinemachineTargetGroup>();
        }

        void Update()
        {
            // iterate through each target in the targetGroup
            foreach (var target in m_TargetGroup.Targets)
            {
                // calculate the distance between target and the LowerPlatform along the Y axis
                var distanceBelow = LowerPlatform.position.y - target.Object.position.y;

                // weight goes to 0 if it's farther below than LoseSightAtRange
                var weight = Mathf.Clamp(1 - distanceBelow / Mathf.Max(0.001f, LoseSightAtRange), 0, 1);
                target.Weight = weight;
            }
        }
    }
}
