using System;
using UnityEngine;

namespace Unity.Cinemachine
{
    /// <summary>
    /// Evaluates shot quality in the Finalize stage based on LookAt target occlusion and distance.
    /// </summary>
    [AddComponentMenu("Cinemachine/Procedural/Extensions/Cinemachine Shot Quality Evaluator")]
    [SaveDuringPlay]
    [ExecuteAlways]
    [DisallowMultipleComponent]
    [RequiredTarget(RequiredTargetAttribute.RequiredTargets.Tracking)]
    [HelpURL(Documentation.BaseURL + "manual/CinemachineShotQualityEvaluator.html")]
    public class CinemachineShotQualityEvaluator : CinemachineExtension, IShotQualityEvaluator
    {
        /// <summary>Objects on these layers will be detected.</summary>
        [Tooltip("Objects on these layers will be detected")]
        public LayerMask OcclusionLayers = 1;

        /// <summary>Obstacles with this tag will be ignored.  It is a good idea to set this field to the target's tag</summary>
        [TagField]
        [Tooltip("Obstacles with this tag will be ignored.  It is a good idea to set this field to the target's tag")]
        public string IgnoreTag = string.Empty;

        /// <summary>Obstacles closer to the target than this will be ignored</summary>
        [Tooltip("Obstacles closer to the target than this will be ignored")]
        public float MinimumDistanceFromTarget = 0.2f;

        /// <summary>
        /// Radius of the spherecast that will be done to check for occlusions.
        /// </summary>
        [Tooltip("Radius of the spherecast that will be done to check for occlusions.")]
        public float CameraRadius;

        /// <summary>Settings for shot quality evaluation</summary>
        [Serializable]
        public struct DistanceEvaluationSettings
        {
            /// <summary>If enabled, will evaluate shot quality based on target distance</summary>
            [Tooltip("If enabled, will evaluate shot quality based on target distance")]
            public bool Enabled;

            /// <summary>If greater than zero, maximum quality boost will occur when target is this far from the camera</summary>
            [Tooltip("If greater than zero, maximum quality boost will occur when target is this far from the camera")]
            public float OptimalDistance;

            /// <summary>Shots with targets closer to the camera than this will not get a quality boost</summary>
            [Tooltip("Shots with targets closer to the camera than this will not get a quality boost")]
            [Delayed]
            public float NearLimit;

            /// <summary>Shots with targets farther from the camera than this will not get a quality boost</summary>
            [Tooltip("Shots with targets farther from the camera than this will not get a quality boost")]
            public float FarLimit;

            /// <summary>High quality shots will be boosted by this fraction of their normal quality</summary>
            [Tooltip("High quality shots will be boosted by this fraction of their normal quality")]
            public float MaxQualityBoost;

            internal static DistanceEvaluationSettings Default => new () { NearLimit = 5, FarLimit = 30, OptimalDistance = 10, MaxQualityBoost = 0.2f };
        }
        /// <summary>If enabled, will evaluate shot quality based on target distance and occlusion</summary>
        [FoldoutWithEnabledButton]
        public DistanceEvaluationSettings DistanceEvaluation = DistanceEvaluationSettings.Default;

        void OnValidate()
        {
            CameraRadius = Mathf.Max(0, CameraRadius);
            MinimumDistanceFromTarget = Mathf.Max(0.01f, MinimumDistanceFromTarget);
            CameraRadius = Mathf.Max(0, CameraRadius);
            DistanceEvaluation.NearLimit = Mathf.Max(0.1f, DistanceEvaluation.NearLimit);
            DistanceEvaluation.FarLimit = Mathf.Max(DistanceEvaluation.NearLimit, DistanceEvaluation.FarLimit);
            DistanceEvaluation.OptimalDistance = Mathf.Clamp(
                DistanceEvaluation.OptimalDistance, DistanceEvaluation.NearLimit, DistanceEvaluation.FarLimit);
        }

        private void Reset()
        {
            OcclusionLayers = 1;
            IgnoreTag = string.Empty;
            MinimumDistanceFromTarget = 0.2f;
            CameraRadius = 0;
            DistanceEvaluation = DistanceEvaluationSettings.Default;
        }

        /// <inheritdoc />
        protected override void PostPipelineStageCallback(
            CinemachineVirtualCameraBase vcam, CinemachineCore.Stage stage, ref CameraState state, float deltaTime)
        {
            if (stage == CinemachineCore.Stage.Finalize && state.HasLookAt())
            {
                var targetObscured = state.IsTargetOffscreen() || IsTargetObscured(state);
                if (targetObscured)
                    state.ShotQuality *= 0.2f;

                if (DistanceEvaluation.Enabled)
                {
                    float nearnessBoost = 0;
                    if (DistanceEvaluation.OptimalDistance > 0)
                    {
                        var distance = Vector3.Magnitude(state.ReferenceLookAt - state.GetFinalPosition());
                        if (distance <= DistanceEvaluation.OptimalDistance)
                        {
                            if (distance >= DistanceEvaluation.NearLimit)
                                nearnessBoost = DistanceEvaluation.MaxQualityBoost * (distance - DistanceEvaluation.NearLimit)
                                    / (DistanceEvaluation.OptimalDistance - DistanceEvaluation.NearLimit);
                        }
                        else
                        {
                            distance -= DistanceEvaluation.OptimalDistance;
                            if (distance < DistanceEvaluation.FarLimit)
                                nearnessBoost = DistanceEvaluation.MaxQualityBoost * (1f - (distance / DistanceEvaluation.FarLimit));
                        }
                        state.ShotQuality *= (1f + nearnessBoost);
                    }
                }
            }
        }

        bool IsTargetObscured(CameraState state)
        {
#if CINEMACHINE_PHYSICS
            var lookAtPos = state.ReferenceLookAt;
            var pos = state.GetCorrectedPosition();
            var dir = lookAtPos - pos;
            var distance = dir.magnitude;
            if (distance < Mathf.Max(MinimumDistanceFromTarget, Epsilon))
                return true;
            var ray = new Ray(pos, dir.normalized);
            return RuntimeUtility.SphereCastIgnoreTag(
                    ray, CameraRadius, out _, distance - MinimumDistanceFromTarget, OcclusionLayers, IgnoreTag);
#else
            return false;
#endif
        }
    }
}
