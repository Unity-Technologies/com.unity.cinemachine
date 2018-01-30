using UnityEngine;

namespace Cinemachine
{
    /// <summary>
    /// An add-on module for Cinemachine Virtual Camera that replaces the camera's
    /// output with a storyboard image
    /// </summary>
    [ExecuteInEditMode]
    [SaveDuringPlay]
    [AddComponentMenu("")] // Hide in menu
    public class CinemachineOverlayImage : CinemachineExtension
    {
        [Tooltip("If checked, the specified image will be displayed as an overlay over the virtual camera's output")]
        public bool m_ShowImage = true;

        [Tooltip("The image to display")]
        public Texture m_Image;

        public enum FillStrategy
        {
            /// <summary>Image will be as large as possible on the screen, without being cropped</summary>
            BestFit,
            /// <summary>Image will be cropped if necessary so that the screen is entirely filled</summary>
            CropImageToFit,
            /// <summary>Image will be stretched to cover any aspect mismatch with the screen</summary>
            StretchToFit
        };
        [Tooltip("A scale value of 1 will place the image according to this strategy")]
        public FillStrategy m_DefaultSize = FillStrategy.BestFit;

        [Tooltip("The opacity of the image.  0 is transparent, 1 is opaque")]
        [Range(0, 1)]
        public float m_Alpha = 1;

        [Tooltip("The screen-space position at which to display the image.  Zero is center")]
        public Vector2 m_Position = Vector3.zero;

        [Tooltip("The screen-space rotation to apply to the image")]
        public Vector3 m_Rotation = Vector3.zero;

        [Tooltip("The screen-space scaling to apply to the image")]
        public Vector2 m_Scale = Vector3.one;

        [Tooltip("If checked, X and Y scale are synchronized")]
        public bool m_SyncScale = true;

        [Tooltip("If checked, Camera transform will not be controlled by this virtual camera")]
        public bool m_MuteCamera;

        GameObject mCanvas;
        Transform mCanvasParent;
        UnityEngine.UI.RawImage mRawImage;

        protected override void PostPipelineStageCallback(
            CinemachineVirtualCameraBase vcam,
            CinemachineCore.Stage stage, ref CameraState state, float deltaTime)
        {
            // Apply to this vcam only, not the children
            if (vcam != VirtualCamera || stage != CinemachineCore.Stage.Finalize)
                return;

            if (m_ShowImage)
            {
                state.AddCustomBlendable(new CameraState.CustomBlendable(this, 1));
                if (m_MuteCamera)
                    state.BlendHint |= CameraState.BlendHintValue.NoTransform;
            }
        }

        void CameraUpdatedCallback(CinemachineBrain brain)
        {
            bool isLive = CinemachineCore.Instance.IsLive(VirtualCamera);
            LocateMyCanvas(brain.transform, isLive);
            if (mCanvas != null)
                mCanvas.SetActive(isLive && m_ShowImage);
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            int numBrains = CinemachineCore.Instance.BrainCount;
            for (int i = 0; i < numBrains; ++i)
            {
                LocateMyCanvas(CinemachineCore.Instance.GetActiveBrain(i).transform, false);
                if (mCanvas != null)
                    DestroyImmediate(mCanvas);
                mCanvas = null;
            }
        }

        protected override void ConnectToVcam(bool connect)
        {
            base.ConnectToVcam(connect);
            CinemachineCore.CameraUpdatedEvent.RemoveListener(CameraUpdatedCallback);
            if (connect)
                CinemachineCore.CameraUpdatedEvent.AddListener(CameraUpdatedCallback);
        }
        
        string CanvasName { get { return "_CM_canvas" + gameObject.GetInstanceID().ToString(); } }

        void LocateMyCanvas(Transform parent, bool createIfNotFound)
        {
            if (mCanvas == null || mCanvasParent != parent)
            {
                mCanvas = null;
                mRawImage = null;
                mCanvasParent = parent;
                string canvasName = CanvasName;
                int numChildren = parent.childCount;
                for (int i = 0; mCanvas == null && i < numChildren; ++i)
                {
                    RectTransform child = parent.GetChild(i) as RectTransform;
                    if (child != null && child.name == canvasName)
                        mCanvas = child.gameObject;
                }
            }
            if (mCanvas == null && createIfNotFound)
                CreateCanvas(parent);
            if (mCanvas != null && mRawImage == null)
                mRawImage = mCanvas.GetComponentInChildren<UnityEngine.UI.RawImage>();
        }

        void CreateCanvas(Transform parent)
        {
            mCanvas = new GameObject(CanvasName, typeof(RectTransform));
            mCanvas.hideFlags = HideFlags.HideAndDontSave;
            mCanvas.transform.SetParent(parent);

            var c = mCanvas.AddComponent<Canvas>();
            c.renderMode = RenderMode.ScreenSpaceOverlay;

            var go = new GameObject("RawImage", typeof(RectTransform));
            go.transform.SetParent(mCanvas.transform);
            mRawImage = go.AddComponent<UnityEngine.UI.RawImage>();
            mCanvasParent = parent;
        }

        void UpdateSettings(UnityEngine.UI.RawImage rawImage, float alpha)
        {
            if (rawImage != null)
            {
                Vector2 scale = Vector2.one;
                if (m_Image != null
                    && m_Image.width > 0 && m_Image.width > 0 
                    && Screen.width > 0 && Screen.height > 0)
                {
                    float f = ((float)m_Image.width / m_Image.height) 
                        / ((float)Screen.width / Screen.height);
                    switch (m_DefaultSize)
                    {
                        case FillStrategy.BestFit:
                            if (f >= 1)
                                scale.y /= f;
                            else
                                scale.x *= f;
                            break;
                        case FillStrategy.CropImageToFit:
                            if (f >= 1)
                                scale.x *= f;
                            else
                                scale.y /= f;
                            break;
                        case FillStrategy.StretchToFit:
                            break;
                    }
                }
                scale.x *= m_Scale.x;
                scale.y *= m_SyncScale ? m_Scale.x : m_Scale.y;

                rawImage.texture = m_Image;
                Color tintColor = Color.white;
                tintColor.a = m_Alpha * alpha;
                rawImage.color = tintColor;
                rawImage.rectTransform.localPosition = m_Position;
                rawImage.rectTransform.localRotation = Quaternion.Euler(m_Rotation);
                rawImage.rectTransform.localScale = scale;
                rawImage.rectTransform.sizeDelta = new Vector2(Screen.width, Screen.height);
            }        
        }

        static void StaticBlendingHandler(CinemachineBrain brain)
        {
            //UnityEngine.Profiling.Profiler.BeginSample("CinemachineOverlayImage.StaticBlendingHandler");
            CameraState state = brain.CurrentCameraState;
            int numBlendables = state.NumCustomBlendables;
            for (int i = 0; i < numBlendables; ++i)
            {
                var b = state.GetCustomBlendable(i);
                CinemachineOverlayImage src = b.m_Custom as CinemachineOverlayImage;
                if (!(src == null)) // in case it was deleted
                {
                    src.LocateMyCanvas(brain.transform, true);
                    if (src.mRawImage != null)
                        src.UpdateSettings(src.mRawImage, b.m_Weight);
                }
            }
            //UnityEngine.Profiling.Profiler.EndSample();
        }

#if UNITY_EDITOR
        [UnityEditor.InitializeOnLoad]
        class EditorInitialize { static EditorInitialize() { InitializeModule(); } }
#endif
        [RuntimeInitializeOnLoadMethod]
        static void InitializeModule()
        {
            CinemachineCore.CameraUpdatedEvent.RemoveListener(StaticBlendingHandler);
            CinemachineCore.CameraUpdatedEvent.AddListener(StaticBlendingHandler);
        }
    }
}
