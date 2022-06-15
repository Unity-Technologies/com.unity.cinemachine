#if CINEMACHINE_HDRP
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.Rendering;

namespace Cinemachine.PostFX
{
    internal class FocusDistance : CustomPass
    {
        static class Uniforms
        {
            internal const string _FocusDistanceParams = "_FocusDistanceParams";
            internal const string _FocusDistanceOutput = "_FocusDistanceOutput";
            internal const string FocusDistanceKeyword = "FOCUS_DISTANCE";
        }

        [Tooltip("Stickier auto focus is more stable (less switching back and forth as tiny "
            + "grass blades cross the camera), but requires looking at a bigger uniform-ish area to switch focus to it.")]
        [Range(0, 1)]
        public float Stickiness = 0.4f;

        [Range(0, 1)]
        public float KernelRadius = 0.02f;

        [Range(0, 1)]
        public float DepthTolerance = 0.02f;

        public Vector2 ScreenPosition;

        public ComputeShader m_ComputeShader;
        public Camera m_Camera;
        public bool PushToCamera = true;

        public float ComputedFocusDistance;


        // Same As FocusDistance.compute
        struct FocusDistanceParams
        {
            public uint  VoteBias;		    // 0...15
            public float DepthTolerance;	// 0.02
            public float SampleRadius;		// 0.02
            public float SamplePosX;		// 0
            public float SamplePosY;		// 0
            public float DefaultFocusDistance; // current focus distance
        };
        ComputeBuffer m_FocusDistanceParamsCB;
        FocusDistanceParams[] m_FocusDistanceParams = new FocusDistanceParams[1];

        // Same As FocusDistance.compute
        struct FocusDistanceOutput
        {
            public float FocusDistance;
        }
        ComputeBuffer m_FocusDistanceOutputCB;
        FocusDistanceOutput[] m_FocusDistanceOutput = new FocusDistanceOutput[1];

        // It can be used to configure render targets and their clear state. Also to create temporary render target textures.
        // When empty this render pass will render to the active camera render target.
        // You should never call CommandBuffer.SetRenderTarget. Instead call <c>ConfigureTarget</c> and <c>ConfigureClear</c>.
        // The render pipeline will ensure target setup and clearing happens in an performance manner.
        protected override void Setup(ScriptableRenderContext renderContext, CommandBuffer cmd)
        {
            if (m_FocusDistanceParamsCB == null)
                m_FocusDistanceParamsCB = new ComputeBuffer(1, 6 * 4); // sizeof(FocusDistanceParams)
            if (m_FocusDistanceOutputCB == null)
                m_FocusDistanceOutputCB = new ComputeBuffer(1, 1 * 4); // sizeof(FocusDistanceOutput)
        }

        protected override void Cleanup()
        {
            if (m_FocusDistanceParamsCB != null)
                m_FocusDistanceParamsCB.Release();
            m_FocusDistanceParamsCB = null;
            if (m_FocusDistanceOutputCB != null)
                m_FocusDistanceOutputCB.Release();
            m_FocusDistanceOutputCB = null;
        }

        protected override void Execute(CustomPassContext ctx)
        {
            if (m_Camera == null || m_ComputeShader == null || ctx.hdCamera.camera != m_Camera)
                return;

            ctx.cmd.BeginSample(Uniforms.FocusDistanceKeyword);
            ctx.cmd.EnableShaderKeyword(Uniforms.FocusDistanceKeyword);

            m_FocusDistanceParams[0].VoteBias = (uint)Mathf.RoundToInt(Stickiness * 15.0f);
            m_FocusDistanceParams[0].DepthTolerance = DepthTolerance;
            m_FocusDistanceParams[0].SampleRadius = KernelRadius;
            m_FocusDistanceParams[0].SamplePosX = ScreenPosition.x;
            m_FocusDistanceParams[0].SamplePosY = ScreenPosition.y;
            m_FocusDistanceParams[0].DefaultFocusDistance = ComputedFocusDistance;

            m_FocusDistanceParamsCB.SetData(m_FocusDistanceParams);
            ctx.cmd.SetComputeBufferParam(m_ComputeShader, 0, Uniforms._FocusDistanceParams, m_FocusDistanceParamsCB);
            ctx.cmd.SetComputeBufferParam(m_ComputeShader, 0, Uniforms._FocusDistanceOutput, m_FocusDistanceOutputCB);
            ctx.cmd.DispatchCompute(m_ComputeShader, 0, 1, 1, 1);
            ctx.cmd.SetGlobalBuffer(Uniforms._FocusDistanceOutput, m_FocusDistanceOutputCB);
            ctx.cmd.EndSample(Uniforms.FocusDistanceKeyword);

            // Read back the output when complete
            ctx.cmd.RequestAsyncReadback(m_FocusDistanceOutputCB, (req) =>
            {
                if (m_FocusDistanceOutputCB != null && m_Camera != null)
                {
                    m_FocusDistanceOutputCB.GetData(m_FocusDistanceOutput);
                    ComputedFocusDistance = m_FocusDistanceOutput[0].FocusDistance;
                    if (PushToCamera)
                        m_Camera.focusDistance = ComputedFocusDistance;
                }
            });
        }
    }
}
#endif
