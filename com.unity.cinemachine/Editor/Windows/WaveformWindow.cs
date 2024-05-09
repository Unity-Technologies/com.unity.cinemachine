using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;
using UnityEngine.Rendering;
using UnityEngine.UIElements;

namespace Unity.Cinemachine.Editor
{
    class WaveformWindow : EditorWindow
    {
        // Controls how frequently (in seconds) the view will update.
        // Performance is really bad, so keep this as large as possible.
        static float s_UpdateInterval = 0.5f;

        string m_ScreenshotFilename;
        static WaveformWindow s_Window;

        WaveformGenerator m_WaveformGenerator;
        Texture2D m_Screenshot;
        float m_LastUpdateTime = 0;
        VisualElement m_ImageDisplay;
 
        public static void RefreshNow()
        {
            if (s_Window != null)
                s_Window.CaptureScreen();
        }

        //[MenuItem("Window/Waveform Monitor")]
        public static void OpenWindow()
        {
            s_Window = EditorWindow.GetWindow<WaveformWindow>(false);
            s_Window.autoRepaintOnSceneChange = true;
            s_Window.Show(true);
        }

        private void OnEnable()
        {
            m_WaveformGenerator = new();
            m_Screenshot = new (2, 2);

            titleContent = new GUIContent("Waveform", CinemachineSettings.CinemachineLogoTexture);
            m_ScreenshotFilename = Path.GetFullPath(FileUtil.GetUniqueTempPathInProject() + ".png");

            ScreenCapture.CaptureScreenshot(m_ScreenshotFilename);
            m_LastUpdateTime = 0;
            EditorApplication.update += TimedCapture;
        }

        private void OnDisable()
        {
            EditorApplication.update -= TimedCapture;
            if (!string.IsNullOrEmpty(m_ScreenshotFilename) && File.Exists(m_ScreenshotFilename))
                File.Delete(m_ScreenshotFilename);
            m_ScreenshotFilename = null;
            m_WaveformGenerator.DestroyBuffers();
        }
        
        public void CreateGUI()
        {
            var ux = rootVisualElement;
            var exposureField = ux.AddChild(new Slider("Exposure", 0.01f, 2) 
                { value = m_WaveformGenerator.Exposure, showInputField = true });
            exposureField.RemoveFromClassList(InspectorUtility.AlignFieldClassName);
            exposureField.RegisterValueChangedCallback((evt) => 
            {
                m_WaveformGenerator.Exposure = evt.newValue;
                CaptureScreen();
            });
            m_ImageDisplay = ux.AddChild(new VisualElement() { style = { flexGrow = 1 }});
        }

        void TimedCapture()
        {
            // Don't do this costly thing too often
            if (Time.realtimeSinceStartup - m_LastUpdateTime > s_UpdateInterval)
                CaptureScreen();
        }

        void CaptureScreen()
        {
            m_LastUpdateTime = Time.realtimeSinceStartup;
            if (!string.IsNullOrEmpty(m_ScreenshotFilename) && File.Exists(m_ScreenshotFilename))
            {
                byte[] fileData = File.ReadAllBytes(m_ScreenshotFilename);
                m_Screenshot.LoadImage(fileData); // this will auto-resize the texture dimensions.
                m_WaveformGenerator.RenderWaveform(m_Screenshot);

                // The capture is delayed, setup for the next call
                ScreenCapture.CaptureScreenshot(m_ScreenshotFilename);
            }
            m_ImageDisplay.style.backgroundImage = Background.FromRenderTexture(m_WaveformGenerator.Result);
            Repaint();
        }

        class WaveformGenerator
        {
            public float Exposure = 0.2f;

            RenderTexture m_Output;
            ComputeBuffer m_Data;

            int m_ThreadGroupSize;
            int m_ThreadGroupSizeX;
            int m_ThreadGroupSizeY;

            ComputeShader m_WaveformCompute;
            MaterialPropertyBlock m_WaveformProperties;
            Material m_WaveformMaterial;
            CommandBuffer m_Cmd;

            static Mesh s_FullscreenTriangle;
            static Mesh FullscreenTriangle
            {
                get
                {
                    if (s_FullscreenTriangle == null)
                    {
                        s_FullscreenTriangle = new Mesh { name = "Fullscreen Triangle" };
                        s_FullscreenTriangle.SetVertices(new List<Vector3>
                        {
                            new (-1f, -1f, 0f),
                            new (-1f,  3f, 0f),
                            new ( 3f, -1f, 0f)
                        });
                        s_FullscreenTriangle.SetIndices(
                            new [] { 0, 1, 2 }, MeshTopology.Triangles, 0, false);
                        s_FullscreenTriangle.UploadMeshData(false);
                    }
                    return s_FullscreenTriangle;
                }
            }

            public WaveformGenerator()
            {
                m_WaveformCompute = AssetDatabase.LoadAssetAtPath<ComputeShader>(
                    $"{CinemachineCore.kPackageRoot}/Editor/EditorResources/CMWaveform.compute");
                m_WaveformProperties = new MaterialPropertyBlock();
                m_WaveformMaterial = new Material(AssetDatabase.LoadAssetAtPath<Shader>(
                    $"{CinemachineCore.kPackageRoot}/Editor/EditorResources/CMWaveform.shader"))
                {
                    name = "CMWaveformMaterial",
                    hideFlags = HideFlags.DontSave
                };
                m_Cmd = new CommandBuffer();
            }

            void CreateBuffers(int width, int height)
            {
                if (m_Output == null || !m_Output.IsCreated()
                    || m_Output.width != width || m_Output.height != height)
                {
                    DestroyImmediate(m_Output);
                    m_Output = new RenderTexture(width, height, 0, RenderTextureFormat.ARGB32)
                    {
                        anisoLevel = 0,
                        filterMode = FilterMode.Bilinear,
                        wrapMode = TextureWrapMode.Clamp,
                        useMipMap = false
                    };
                    m_Output.Create();
                }

                int count = Mathf.CeilToInt(width / (float)m_ThreadGroupSizeX) * m_ThreadGroupSizeX * height;
                if (m_Data == null)
                    m_Data = new ComputeBuffer(count, sizeof(uint) << 2);
                else if (m_Data.count < count)
                {
                    m_Data.Release();
                    m_Data = new ComputeBuffer(count, sizeof(uint) << 2);
                }
            }

            public void DestroyBuffers()
            {
                m_Data?.Release();
                m_Data = null;
                DestroyImmediate(m_Output);
                m_Output = null;
            }

            public RenderTexture Result => m_Output;

            public void RenderWaveform(Texture2D source)
            {
                if (m_WaveformMaterial == null)
                    return;

                int width = source.width;
                int height = source.height;
                int histogramResolution = 256;

                m_ThreadGroupSize = 256;
                m_ThreadGroupSizeX = 16;
                m_ThreadGroupSizeY = 16;
                CreateBuffers(width, histogramResolution);

                m_Cmd.Clear();
                m_Cmd.BeginSample("CMWaveform");

                var parameters = new Vector4(
                    width, height,
                    QualitySettings.activeColorSpace == ColorSpace.Linear ? 1 : 0,
                    histogramResolution);

                // Clear the buffer on every frame
                int kernel = m_WaveformCompute.FindKernel("KCMWaveformClear");
                m_Cmd.SetComputeBufferParam(m_WaveformCompute, kernel, "_WaveformBuffer", m_Data);
                m_Cmd.SetComputeVectorParam(m_WaveformCompute, "_Params", parameters);
                m_Cmd.DispatchCompute(m_WaveformCompute, kernel,
                    Mathf.CeilToInt(width / (float)m_ThreadGroupSizeX),
                    Mathf.CeilToInt(histogramResolution / (float)m_ThreadGroupSizeY), 1);

                // Gather all pixels and fill in our waveform
                kernel = m_WaveformCompute.FindKernel("KCMWaveformGather");
                m_Cmd.SetComputeBufferParam(m_WaveformCompute, kernel, "_WaveformBuffer", m_Data);
                m_Cmd.SetComputeTextureParam(m_WaveformCompute, kernel, "_Source", source);
                m_Cmd.SetComputeVectorParam(m_WaveformCompute, "_Params", parameters);
                m_Cmd.DispatchCompute(m_WaveformCompute, kernel, width,
                    Mathf.CeilToInt(height / (float)m_ThreadGroupSize), 1);

                // Generate the waveform texture
                float exposure = Mathf.Max(0f, Exposure);
                exposure *= (float)histogramResolution / height;
                m_WaveformProperties.SetVector(Shader.PropertyToID("_Params"),
                    new Vector4(width, histogramResolution, exposure, 0f));
                m_WaveformProperties.SetBuffer(Shader.PropertyToID("_WaveformBuffer"), m_Data);
                m_Cmd.SetRenderTarget(m_Output);
                m_Cmd.DrawMesh(
                    FullscreenTriangle, Matrix4x4.identity,
                    m_WaveformMaterial, 0, 0, m_WaveformProperties);
                m_Cmd.EndSample("CMWaveform");

                Graphics.ExecuteCommandBuffer(m_Cmd);
            }
        }
    }
}
