using UnityEngine;

namespace Unity.Cinemachine.Samples
{
    public class BlendStyleManager : MonoBehaviour
    {
        class LookAtFirstBlender : CinemachineBlend.IBlender
        {
            // This method is free to blend the states any way it likes.
            // In this case, we do a default blend then override the rotation to make
            // it happen at the beginning of the blend.
            public CameraState GetIntermediateState(ICinemachineCamera CamA, ICinemachineCamera CamB, float t)
            {
                var stateA = CamA.State;
                var stateB = CamB.State;

                // Standard blend - first we disable cylidrical position 
                stateA.BlendHint &= ~CameraState.BlendHints.CylindricalPositionBlend;
                stateB.BlendHint &= ~CameraState.BlendHints.CylindricalPositionBlend;
                var state = CameraState.Lerp(stateA, stateB, t);

                // Override the rotation blend: look directly at the new target at the beginning
                const float kThreshold = 0.1f;
                var rotB = Quaternion.LookRotation(stateB.ReferenceLookAt - state.RawPosition, state.ReferenceUp);
                t = Bias(Mathf.Lerp(0, 1, t / kThreshold), 0.9f);
                state.RawOrientation = Quaternion.Slerp(stateA.RawOrientation, rotB, t);

                return state;
            }

            static float Bias(float t, float b) 
                => (Mathf.Clamp(t, 0, 1) / ((((1f/Mathf.Clamp(b, 0, 1)) - 2f) * (1f - t)) + 1f));
        }

        LookAtFirstBlender m_CustomBlender = new ();

        bool m_UseCustomBlend;

        public void OnBlendCreated(CinemachineCore.BlendEventParams evt)
        {
            // Override the blender with a custom blender
            if (m_UseCustomBlend)
                evt.Blend.CustomBlender = m_CustomBlender;
        }

        public void SetDefaultBlend()
        {
            m_UseCustomBlend = false;
            ChangeCamera();
        }

        public void SetCustomBlend1()
        {
            m_UseCustomBlend = true;
            ChangeCamera();
        }

        void ChangeCamera()
        {
            // Cycle through all the virtual cameras, assuming that they all have the same priority.
            // Prioritize the least-recently used one.
            int numCameras = CinemachineCore.VirtualCameraCount;
            CinemachineCore.GetVirtualCamera(numCameras - 1).Prioritize();
        }
    }
}