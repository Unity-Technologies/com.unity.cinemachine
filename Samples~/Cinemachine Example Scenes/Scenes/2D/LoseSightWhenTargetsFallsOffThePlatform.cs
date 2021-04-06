using System;
using System.Collections;
using System.Collections.Generic;
using Cinemachine;
using UnityEngine;

public class LoseSightWhenTargetsFallsOffThePlatform : MonoBehaviour
{
    public Transform platform;
    [Range(1, 30)]
    public float LoseSightSlowness = 10;
    private CinemachineTargetGroup targetGroup;
    void Start()
    {
        targetGroup = GetComponent<CinemachineTargetGroup>();
    }

    // Update is called once per frame
    void Update()
    {
        for (var index = 0; index < targetGroup.m_Targets.Length; index++)
        {
            if (targetGroup.m_Targets[index].target.position.y < platform.position.y)
            {
                targetGroup.m_Targets[index].weight = 
                    1f - (platform.position.y - targetGroup.m_Targets[index].target.position.y) / LoseSightSlowness;
                targetGroup.m_Targets[index].weight = Mathf.Clamp(targetGroup.m_Targets[index].weight, 0, 1);
            }
            else
            {
                targetGroup.m_Targets[index].weight = 1;
            }
        }
    }

    private void OnValidate()
    {
        LoseSightSlowness = Mathf.Clamp(LoseSightSlowness, 1f, 30f);
    }
}
