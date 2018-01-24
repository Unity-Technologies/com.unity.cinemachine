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

        [Tooltip("The opacity of the image.  0 is transparent, 1 is opaque")]
        [Range(0, 1)]
        public float m_Alpha = 1;

        [Tooltip("The screen-space position at which to display the image.  Zero is center")]
        public Vector2 m_Position = Vector3.zero;

        [Tooltip("The screen-space rotation to apply to the image")]
        public Vector3 m_Rotation = Vector3.zero;

        [Tooltip("The screen-space scaling to apply to the image")]
        public Vector2 m_Scale = Vector3.one;

        GameObject mCanvas;
        UnityEngine.UI.RawImage mRawImage;

        protected override void PostPipelineStageCallback(
            CinemachineVirtualCameraBase vcam,
            CinemachineCore.Stage stage, ref CameraState state, float deltaTime)
        {
            // Apply to this vcam only, not the children
            if (vcam != VirtualCamera || stage != CinemachineCore.Stage.Noise)
                return;

            bool isLive = CinemachineCore.Instance.IsLive(vcam);
            LocateMyCanvas(isLive);
            if (mCanvas != null)
                mCanvas.SetActive(isLive && m_ShowImage);

            // Apply after the vcam has finished its calculations
            if (isLive)
            {
                mRawImage.texture = m_Image;
                Color tintColor = Color.white;
                tintColor.a = m_Alpha;
                mRawImage.color = tintColor;
                mRawImage.rectTransform.localPosition = m_Position;
                mRawImage.rectTransform.localRotation = Quaternion.Euler(m_Rotation);
                mRawImage.rectTransform.localScale = m_Scale;
                mRawImage.rectTransform.sizeDelta = new Vector2(Screen.width, Screen.height);

                state.AddCustomBlendable(new CameraState.CustomBlendable(this, 1));
            }
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            LocateMyCanvas(false);
            if (mCanvas != null)
                DestroyImmediate(mCanvas);
        }

        string CanvasName { get { return "_CM_canvas" + gameObject.GetInstanceID().ToString(); } }

        void LocateMyCanvas(bool createIfNotFound)
        {
            if (mCanvas == null)
            {
                string canvasName = CanvasName;
                int numChildren = transform.childCount;
                for (int i = 0; mCanvas == null && i < numChildren; ++i)
                {
                    RectTransform child = transform.GetChild(i) as RectTransform;
                    if (child != null && child.name == canvasName)
                        mCanvas = child.gameObject;
                }
            }
            if (mCanvas == null && createIfNotFound)
                CreateCanvas();
            if (mCanvas != null && mRawImage == null)
                mRawImage = mCanvas.GetComponentInChildren<UnityEngine.UI.RawImage>();
        }

        void CreateCanvas()
        {
            mCanvas = new GameObject(CanvasName, typeof(RectTransform));
            mCanvas.hideFlags = HideFlags.HideAndDontSave;
            mCanvas.transform.SetParent(transform);

            var c = mCanvas.AddComponent<Canvas>();
            c.renderMode = RenderMode.ScreenSpaceOverlay;

            var go = new GameObject("RawImage", typeof(RectTransform));
            go.transform.SetParent(mCanvas.transform);
            mRawImage = go.AddComponent<UnityEngine.UI.RawImage>();
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
                    if (src.mRawImage != null)
                    {
                        Color c = src.mRawImage.color;
                        c.a *= b.m_Weight;
                        src.mRawImage.color = c;
                    }
                }
            }
            //UnityEngine.Profiling.Profiler.EndSample();
        }

        /// <summary>Internal method called by the editor</summary>
        [RuntimeInitializeOnLoadMethod]
        public static void InitializeModule()
        {
            // When the brain pushes the state to the camera, hook in to the PostFX
            CinemachineBrain.sPostProcessingHandler.RemoveListener(StaticBlendingHandler);
            CinemachineBrain.sPostProcessingHandler.AddListener(StaticBlendingHandler);
        }
    }
}
