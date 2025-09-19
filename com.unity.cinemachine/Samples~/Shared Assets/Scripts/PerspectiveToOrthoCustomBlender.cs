using UnityEngine;

namespace Unity.Cinemachine.Samples
{
    /// <summary>
    /// This behaviour implements a custom algorithm to provide smooth blends between perspective 
    /// and ortho cameras.  Drop it into your scene, and while enabled it will override the default 
    /// blending algorithms when appropriate.
    /// 
    /// Specifically, if one camera is perspective and the other is orthographic, and if the orthographic 
    /// camera has a LookAt target, the custom blend will be used.
    /// 
    /// Because it's not possible to lerp between perspective and ortho cameras, we approximate the ortho
    /// camera with a perspective camera placed far away with a small fov, and blend to that.  Afterwards,
    /// we simply cut to the ortho camera.
    /// 
    /// This class illustrates the use of the CinemachineCore.GetCustomBlender hook, and shows how to 
    /// implement the CinemachineBlend.IBlender interface.
    /// </summary>
    [ExecuteAlways]
    public class PerspectiveToOrthoCustomBlender : MonoBehaviour, CinemachineBlend.IBlender
    {
        [Tooltip("Minimum distance at which to place the perspective camera which will mimic the orthographic one.  \n"
            + "Changing this distance may affect the feel of the blend: a large distance will produce a better approximation "
            + "of the ortho camera, but will also make the FOV change happen more quickly at the start of the blend.  \n"
            + "Keep this distance as small as you can tolerate, to avoid precision errors which can be present at "
            + "large camera distances.")]
        public float FakeOrthoCameraDistance = 100;

        void OnEnable() => CinemachineCore.GetCustomBlender += GetCustomBlender;
        void OnDisable() => CinemachineCore.GetCustomBlender -= GetCustomBlender;

        // CinemachineCore.GetCustomBlender handler
        CinemachineBlend.IBlender GetCustomBlender(ICinemachineCamera camA, ICinemachineCamera camB)
        {
            // Use the custom blender if and only if we're transitioning between ortho and perspective cameras
            if (camA != null && camB != null)
            {
                var stateA = camA.State;
                var stateB = camB.State;
                if (IsBlendToOrthoCandidate(ref stateA, ref stateB))
                    return this;
            }
            // Use default blender
            return null; 
        }

        // CinemachineBlend.IBlender implementation
        public CameraState GetIntermediateState(ICinemachineCamera camA, ICinemachineCamera camB, float t)
        {
            var stateA = camA.State;
            var stateB = camB.State;

            // This can happen if we're blending intermediate states due to interrupted blend
            if (!IsBlendToOrthoCandidate(ref stateA, ref stateB))
                return CameraState.Lerp(stateA, stateB, t);

            if (!stateA.Lens.Orthographic)
                return BlendToOrtho(ref stateA, ref stateB, t);

            return BlendToOrtho(ref stateB, ref stateA, 1-t);
        }

        bool IsBlendToOrthoCandidate(ref CameraState stateA, ref CameraState stateB)
        {
            bool orthoA = stateA.Lens.Orthographic;
            bool orthoB = stateB.Lens.Orthographic;

            // A lookAt target is required on the ortho camera in order to establish the mimic fov
            return orthoA != orthoB && ((orthoA && stateA.HasLookAt()) || (orthoB && stateB.HasLookAt()));
        }

        // Replaces stateB with a fake ortho camera which is a far-away perspective camera with a small fov
        CameraState BlendToOrtho(ref CameraState stateA, ref CameraState stateB, float t)
        {
            var lensB = stateB.Lens;
            var orthoSize = lensB.OrthographicSize;

            var lookAt = stateB.ReferenceLookAt;
            if (!stateA.HasLookAt())
                stateA.ReferenceLookAt = lookAt;

            var distanceFromTarget = Vector3.Distance(lookAt, stateB.GetCorrectedPosition());

            // We want it to be far compared to the ortho size
            var extraDistance = Mathf.Max(0, Mathf.Max(FakeOrthoCameraDistance, orthoSize * 20) - distanceFromTarget);

            var rotB = stateB.GetFinalOrientation();
            stateB.RawPosition = stateB.GetCorrectedPosition() + rotB * Vector3.back * extraDistance;
            stateB.PositionCorrection = Vector3.zero;
            stateB.ReferenceUp = rotB * Vector3.up;

            // Force a spherical position algorithm
            stateB.BlendHint |= CameraState.BlendHints.SphericalPositionBlend;

            // The fov should be such as to produce the ortho size at the target's position
            var lens = stateA.Lens;
            lens.FieldOfView = 2f * Mathf.Atan(orthoSize / (extraDistance + distanceFromTarget)) * Mathf.Rad2Deg;

            // Lerp the clip planes to reduce popping
            lens.NearClipPlane = Mathf.Max(lens.NearClipPlane, extraDistance + lensB.NearClipPlane);
            lens.FarClipPlane = extraDistance + lensB.FarClipPlane;
            stateB.Lens = lens;

            // We square t to spend more time at the start of the blend, producing a smoother result
            // when the fake ortho camera is far away.  This could potentially be tweaked.
            return CameraState.Lerp(stateA, stateB, t * t);
        }
    }
}
