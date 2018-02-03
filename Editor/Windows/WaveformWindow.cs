using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;
using UnityEngine.Rendering;

namespace Cinemachine.Editor
{
    public class WaveformWindow : EditorWindow
    {
        Texture2D mScreenshot;
        string mScreenshotFilename;
        WaveformGenerator mWaveformGenerator;

        [MenuItem("Window/Waveform Monitor")]
        private static void OpenWindow()
        {
            WaveformWindow window = EditorWindow.GetWindow<WaveformWindow>(true);
            window.autoRepaintOnSceneChange = true;
            window.position = new Rect(100, 100, 400, 400);
            window.Show(true);
        }
        
        private void OnEnable()
        {
            titleContent = new GUIContent("Waveform", CinemachineSettings.CinemachineLogoTexture);
            mScreenshotFilename = Path.GetFullPath(FileUtil.GetUniqueTempPathInProject() + ".png");
            ScreenCapture.CaptureScreenshot(mScreenshotFilename);
            EditorApplication.update += UpdateScreenshot;
            mWaveformGenerator = new WaveformGenerator();
        }

        private void OnDisable()
        {
            EditorApplication.update -= UpdateScreenshot;
            if (File.Exists(mScreenshotFilename))
                File.Delete(mScreenshotFilename);
            mScreenshotFilename = null;
            mWaveformGenerator.DestroyBuffers();
        }

        private void OnGUI()
        {
            if (mWaveformGenerator.Result != null)
            {
                Rect rect = EditorGUILayout.GetControlRect(true);
                EditorGUIUtility.labelWidth /= 2;
                mWaveformGenerator.m_Exposure = EditorGUI.Slider(
                    rect, "Exposure", mWaveformGenerator.m_Exposure, 0.01f, 2);
                EditorGUIUtility.labelWidth *= 2;
                rect.y += rect.height;
                rect.height = position.height - rect.height;
                GUI.DrawTexture(rect, mWaveformGenerator.Result, ScaleMode.StretchToFill);
            }
        }

        float mLastUpdateTime = 0;
        private void UpdateScreenshot()
        {
            if (mScreenshotFilename == null)
                return;

            // Don't do this too often
            float now = Time.time;
            if (mScreenshot != null && now - mLastUpdateTime < 0.1f)
                return;

            mLastUpdateTime = now;
            if (mScreenshot != null)
                Object.DestroyImmediate(mScreenshot);
            mScreenshot = null;
#if false // GML temp hack CaptureScreenshotAsTexture() is broken in Unity
            mScreenshot = ScreenCapture.CaptureScreenshotAsTexture();
#else
            if (File.Exists(mScreenshotFilename))
            {
                byte[] fileData = File.ReadAllBytes(mScreenshotFilename);
                mScreenshot = new Texture2D(2, 2);
                mScreenshot.LoadImage(fileData); // this will auto-resize the texture dimensions.
                mWaveformGenerator.RenderWaveform(mScreenshot);

                // Capture the next one
                ScreenCapture.CaptureScreenshot(mScreenshotFilename);
                Repaint();
            }
#endif
        }

        class WaveformGenerator
        {
            public float m_Exposure = 0.12f;

            RenderTexture mOutput;
            ComputeBuffer mData;

            int mThreadGroupSize;
            int mThreadGroupSizeX;
            int mThreadGroupSizeY;

            ComputeShader mWaveformCompute;
            MaterialPropertyBlock mWaveformProperties;
            Material mWaveformMaterial;
            CommandBuffer mCmd;
            static Mesh sFullscreenTriangle;
          
            public WaveformGenerator()
            {
                mWaveformCompute = Resources.Load<ComputeShader>("CMWaveform");
                mWaveformProperties = new MaterialPropertyBlock();
                mWaveformMaterial = new Material(Resources.Load<Shader>("CMWaveform"))
                {
                    name = "CMWaveformMaterial",
                    hideFlags = HideFlags.DontSave
                };
                mCmd = new CommandBuffer();

                if (sFullscreenTriangle == null)
                {
                    sFullscreenTriangle = new Mesh { name = "Fullscreen Triangle" };
                    sFullscreenTriangle.SetVertices(new List<Vector3>
                    {
                        new Vector3(-1f, -1f, 0f),
                        new Vector3(-1f,  3f, 0f),
                        new Vector3( 3f, -1f, 0f)
                    });
                    sFullscreenTriangle.SetIndices(
                        new [] { 0, 1, 2 }, MeshTopology.Triangles, 0, false);
                    sFullscreenTriangle.UploadMeshData(false);
                }
            }

            void CreateBuffers(int width, int height)
            {
                if (mOutput == null || !mOutput.IsCreated() 
                    || mOutput.width != width || mOutput.height != height)
                {
                    DestroyImmediate(mOutput);
                    mOutput = new RenderTexture(width, height, 0, RenderTextureFormat.ARGB32)
                    {
                        anisoLevel = 0,
                        filterMode = FilterMode.Bilinear,
                        wrapMode = TextureWrapMode.Clamp,
                        useMipMap = false
                    };
                    mOutput.Create();
                }

                int count = width * height;
                if (mData == null)
                {
                    mData = new ComputeBuffer(count, sizeof(uint) << 2);
                }
                else if (mData.count < count)
                {
                    mData.Release();
                    mData = new ComputeBuffer(count, sizeof(uint) << 2);
                }
            }

            public void DestroyBuffers()
            {
                if (mData != null)
                    mData.Release();
                mData = null;
                DestroyImmediate(mOutput);
                mOutput = null;
            }

            public RenderTexture Result { get { return mOutput; } }

            public void RenderWaveform(Texture2D source)
            {
                int width = source.width;
                int height = source.height;

                CreateBuffers(width, height);

                mThreadGroupSizeX = 16;
                mThreadGroupSize = 256;
                mThreadGroupSizeY = 16;

                mCmd.Clear();
                mCmd.BeginSample("CMWaveform");

                var parameters = new Vector4(
                    width, height, 
                    QualitySettings.activeColorSpace == ColorSpace.Linear ? 1 : 0, 0f);

                // Clear the buffer on every frame
                int kernel = mWaveformCompute.FindKernel("KCMWaveformClear");
                mCmd.SetComputeBufferParam(mWaveformCompute, kernel, "_WaveformBuffer", mData);
                mCmd.SetComputeVectorParam(mWaveformCompute, "_Params", parameters);
                mCmd.DispatchCompute(mWaveformCompute, kernel, 
                    Mathf.CeilToInt(width / (float)mThreadGroupSizeX), 
                    Mathf.CeilToInt(height / (float)mThreadGroupSizeY), 1);

                // Gather all pixels and fill in our waveform
                kernel = mWaveformCompute.FindKernel("KCMWaveformGather");
                mCmd.SetComputeBufferParam(mWaveformCompute, kernel, "_WaveformBuffer", mData);
                mCmd.SetComputeTextureParam(mWaveformCompute, kernel, "_Source", source);
                mCmd.SetComputeVectorParam(mWaveformCompute, "_Params", parameters);
                mCmd.DispatchCompute(mWaveformCompute, kernel, width, 
                    Mathf.CeilToInt(height / (float)mThreadGroupSize), 1);

                // Generate the waveform texture
                mWaveformProperties.SetVector(Shader.PropertyToID("_Params"), 
                    new Vector4(width, height, Mathf.Max(0f, m_Exposure), 0f));
                mWaveformProperties.SetBuffer(Shader.PropertyToID("_WaveformBuffer"), mData);
                mCmd.SetRenderTarget(mOutput);
                mCmd.DrawMesh(
                    sFullscreenTriangle, Matrix4x4.identity, 
                    mWaveformMaterial, 0, 0, mWaveformProperties);
                mCmd.EndSample("CMWaveform");
                Graphics.ExecuteCommandBuffer(mCmd);
            }
        }
    }
}
