using System.Collections;
using System.Collections.Generic;
using Unity.Cinemachine;
using UnityEngine;

namespace Unity.Cinemachine.Tests
{
    public class UpdateCounterForTests : CinemachineExtension
    {
        public int UpdateCount { get; set; }

        protected override void PostPipelineStageCallback(
            CinemachineVirtualCameraBase vcam,
            CinemachineCore.Stage stage, ref CameraState state, float deltaTime) 
        {
            if (stage == CinemachineCore.Stage.Finalize)
                ++UpdateCount;
        }
    }
}
