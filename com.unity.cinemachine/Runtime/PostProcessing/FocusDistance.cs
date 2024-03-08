#if CINEMACHINE_HDRP
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.Rendering;

namespace Unity.Cinemachine
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

        [Tooltip("Radius of the FocusDistance sensor in the center of the screen.  A value of 1 would fill the screen.  "
            + "It's recommended to keep this quite small.  Default value is 0.02")]
        [Range(0, 1)]
        public float KernelRadius = 0.02f;

        [Tooltip("Depth tolerance for inclusion in the same depth bucket.  Effectively the depth resolution.")]
        [Range(0, 1)]
        public float DepthTolerance = 0.02f;

        [Tooltip("Position on the screen of the depth sensor.  (0, 0) is screen center.")]
        public Vector2 ScreenPosition;

        [Tooltip("Must be the FocusDistance compute shader.")]
        public ComputeShader ComputeShader;

        [Tooltip("The camera whose depth buffer will be checked.")]
        public Camera Camera;

        [Tooltip("If true, then the focus distance will be pushed to the camera's focusDistance field.")]
        public bool PushToCamera = true;

        /// <summary>Initialize this with the current focus distance, to be used as a default value</summary>
        public float CurrentFocusDistance;

        /// <summary>This holds the computed output.  Clients can read it as desired</summary>
        public float ComputedFocusDistance { get; private set; }

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
            if (Camera == null || ComputeShader == null || ctx.hdCamera.camera != Camera)
                return;

            ctx.cmd.BeginSample(Uniforms.FocusDistanceKeyword);
            ctx.cmd.EnableShaderKeyword(Uniforms.FocusDistanceKeyword);

            m_FocusDistanceParams[0].VoteBias = (uint)Mathf.RoundToInt(Stickiness * 15.0f);
            m_FocusDistanceParams[0].DepthTolerance = DepthTolerance;
            m_FocusDistanceParams[0].SampleRadius = KernelRadius;
            m_FocusDistanceParams[0].SamplePosX = ScreenPosition.x;
            m_FocusDistanceParams[0].SamplePosY = ScreenPosition.y;
            m_FocusDistanceParams[0].DefaultFocusDistance 
                = (PushToCamera || CurrentFocusDistance <= 0) ? Camera.focusDistance : CurrentFocusDistance;

            m_FocusDistanceParamsCB.SetData(m_FocusDistanceParams);
            ctx.cmd.SetComputeBufferParam(ComputeShader, 0, Uniforms._FocusDistanceParams, m_FocusDistanceParamsCB);
            ctx.cmd.SetComputeBufferParam(ComputeShader, 0, Uniforms._FocusDistanceOutput, m_FocusDistanceOutputCB);
            ctx.cmd.DispatchCompute(ComputeShader, 0, 1, 1, 1);
            ctx.cmd.SetGlobalBuffer(Uniforms._FocusDistanceOutput, m_FocusDistanceOutputCB);
            ctx.cmd.EndSample(Uniforms.FocusDistanceKeyword);

            // Read back the output when complete
            ctx.cmd.RequestAsyncReadback(m_FocusDistanceOutputCB, (req) =>
            {
                if (m_FocusDistanceOutputCB != null && Camera != null)
                {
                    m_FocusDistanceOutputCB.GetData(m_FocusDistanceOutput);
                    ComputedFocusDistance = m_FocusDistanceOutput[0].FocusDistance;
                    if (PushToCamera)
                        Camera.focusDistance = ComputedFocusDistance;
                }
            });
        }
    }
}
#endif
