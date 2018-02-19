using System;
using System.Collections.Generic;
using UnityEngine;

namespace Cinemachine
{
    /// <summary>
    /// An add-on module for Cinemachine Virtual Camera that places an image in screen space 
    /// over the camera's output.
    /// </summary>
    [ExecuteInEditMode]
    [SaveDuringPlay]
    [DocumentationSorting(DocumentationSortingAttribute.Level.UserRef)]
    [AddComponentMenu("")] // Hide in menu
    public class CinemachineStoryboard : CinemachineExtension
    {
        [Tooltip("If checked, the specified image will be displayed as an overlay over the virtual camera's output")]
        public bool m_ShowImage = true;

        [Tooltip("The image to display")]
        public Texture m_Image;

        /// <summary>How to fit the image in the frame, in the event that the aspect ratios don't match</summary>
        public enum FillStrategy
        {
            /// <summary>Image will be as large as possible on the screen, without being cropped</summary>
            BestFit,
            /// <summary>Image will be cropped if necessary so that the screen is entirely filled</summary>
            CropImageToFit,
            /// <summary>Image will be stretched to cover any aspect mismatch with the screen</summary>
            StretchToFit
        };
        [Tooltip("How to handle differences between image aspect and screen aspect")]
        public FillStrategy m_Aspect = FillStrategy.BestFit;

        [Tooltip("The opacity of the image.  0 is transparent, 1 is opaque")]
        [Range(0, 1)]
        public float m_Alpha = 1;

        [Tooltip("The screen-space position at which to display the image.  Zero is center")]
        public Vector2 m_Center = Vector2.zero;

        [Tooltip("The screen-space rotation to apply to the image")]
        public Vector3 m_Rotation = Vector3.zero;

        [Tooltip("The screen-space scaling to apply to the image")]
        public Vector2 m_Scale = Vector3.one;

        [Tooltip("If checked, X and Y scale are synchronized")]
        public bool m_SyncScale = true;

        [Tooltip("If checked, Camera transform will not be controlled by this virtual camera")]
        public bool m_MuteCamera;

        [Range(-1, 1)]
        [Tooltip("Wipe the image on and off horizontally")]
        public float m_SplitView = 0f;

        GameObject mCanvas;
        CinemachineBrain mCanvasParent;
        RectTransform mViewport; // for mViewport clipping
        UnityEngine.UI.RawImage mRawImage;

        /// <summary>Standard CinemachineExtension callback</summary>
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
            bool showIt = enabled && m_ShowImage && CinemachineCore.Instance.IsLive(VirtualCamera);
            LocateMyCanvas(brain, showIt);
            if (mCanvas != null)
                mCanvas.SetActive(showIt);
        }

        protected override void ConnectToVcam(bool connect)
        {
            base.ConnectToVcam(connect);
            CinemachineCore.CameraUpdatedEvent.RemoveListener(CameraUpdatedCallback);
            if (connect)
                CinemachineCore.CameraUpdatedEvent.AddListener(CameraUpdatedCallback);
            else
                DestroyCanvas();
        }
        
        string CanvasName { get { return "_CM_canvas" + gameObject.GetInstanceID().ToString(); } }

        void LocateMyCanvas(CinemachineBrain parent, bool createIfNotFound)
        {
            if (mCanvas == null || mCanvasParent != parent)
            {
                mCanvas = null;
                mRawImage = null;
                mViewport = null;
                mCanvasParent = parent;
                string canvasName = CanvasName;
                int numChildren = parent.transform.childCount;
                for (int i = 0; mCanvas == null && i < numChildren; ++i)
                {
                    RectTransform child = parent.transform.GetChild(i) as RectTransform;
                    if (child != null && child.name == canvasName)
                        mCanvas = child.gameObject;
                }
            }
            if (mCanvas == null && createIfNotFound)
                CreateCanvas(parent);
            if (mCanvas != null && (mRawImage == null || mViewport == null))
            {
                mViewport = mCanvas.GetComponentInChildren<RectTransform>();
                mRawImage = mCanvas.GetComponentInChildren<UnityEngine.UI.RawImage>();
            }
        }

        void CreateCanvas(CinemachineBrain parent)
        {
            mCanvas = new GameObject(CanvasName, typeof(RectTransform));
            mCanvas.hideFlags = HideFlags.HideAndDontSave;
            mCanvas.transform.SetParent(parent.transform);
            mCanvasParent = parent;
#if UNITY_EDITOR
            // Workaround for Unity bug case Case 1004117
            CanvasesAndTheirOwners.AddCanvas(mCanvas, this);
#endif

            var c = mCanvas.AddComponent<Canvas>();
            c.renderMode = RenderMode.ScreenSpaceOverlay;

            var go = new GameObject("Viewport", typeof(RectTransform));
            go.transform.SetParent(mCanvas.transform);
            mViewport = (RectTransform)go.transform;
            go.AddComponent<UnityEngine.UI.RectMask2D>();

            go = new GameObject("RawImage", typeof(RectTransform));
            go.transform.SetParent(mViewport.transform);
            mRawImage = go.AddComponent<UnityEngine.UI.RawImage>();
        }

        void DestroyCanvas()
        {
            int numBrains = CinemachineCore.Instance.BrainCount;
            for (int i = 0; i < numBrains; ++i)
            {
                LocateMyCanvas(CinemachineCore.Instance.GetActiveBrain(i), false);
                if (mCanvas != null)
                {
                    RuntimeUtility.DestroyObject(mCanvas);
#if UNITY_EDITOR
                    // Workaround for Unity bug case Case 1004117
                    CanvasesAndTheirOwners.RemoveCanvas(mCanvas);
#endif
                }
                mCanvas = null;
            }
        }

        void PlaceImage(float alpha)
        {
            if (mRawImage != null && mViewport != null)
            {
                Rect screen = new Rect(0, 0, Screen.width, Screen.height);
                if (mCanvasParent.OutputCamera != null)
                    screen = mCanvasParent.OutputCamera.pixelRect;
                screen.x -= (float)Screen.width/2;
                screen.y -= (float)Screen.height/2;

                mViewport.localPosition = screen.center;
                mViewport.localRotation = Quaternion.identity;
                mViewport.localScale = Vector3.one;;
                mViewport.sizeDelta = screen.size;

                Vector2 scale = Vector2.one;
                if (m_Image != null
                    && m_Image.width > 0 && m_Image.width > 0 
                    && screen.width > 0 && screen.height > 0)
                {
                    float f = (screen.height * m_Image.width) / (screen.width * m_Image.height);
                    switch (m_Aspect)
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

                mRawImage.texture = m_Image;
                Color tintColor = Color.white;
                tintColor.a = m_Alpha * alpha;
                mRawImage.color = tintColor;
                mRawImage.rectTransform.localPosition 
                    = new Vector2(screen.width * m_Center.x, screen.height * m_Center.y);
                mRawImage.rectTransform.localRotation = Quaternion.Euler(m_Rotation);
                mRawImage.rectTransform.localScale = scale;
                mRawImage.rectTransform.sizeDelta = screen.size;

                // Apply Split View
                float delta = -Mathf.Clamp(m_SplitView, -1, 1) * screen.width;
                var p = mViewport.localPosition; p.x -= delta/2; mViewport.localPosition = p;
                p = mRawImage.rectTransform.localPosition; p.x += delta/2; mRawImage.rectTransform.localPosition = p;
                mViewport.sizeDelta = new Vector2(screen.width - Mathf.Abs(delta), screen.height);
            }        
        }

        static void StaticBlendingHandler(CinemachineBrain brain)
        {
            //UnityEngine.Profiling.Profiler.BeginSample("CinemachineStoryboard.StaticBlendingHandler");
            CameraState state = brain.CurrentCameraState;
            int numBlendables = state.NumCustomBlendables;
            for (int i = 0; i < numBlendables; ++i)
            {
                var b = state.GetCustomBlendable(i);
                CinemachineStoryboard src = b.m_Custom as CinemachineStoryboard;
                if (!(src == null)) // in case it was deleted
                {
                    src.LocateMyCanvas(brain, true);
                    src.PlaceImage(b.m_Weight);
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


#if UNITY_EDITOR
        // Workaround for the Unity bug where OnDestroy doesn't get called if Undo
        // bug case Case 1004117
        [UnityEditor.InitializeOnLoad]
        class CanvasesAndTheirOwners 
        { 
            static Dictionary<UnityEngine.Object, UnityEngine.Object> sCanvasesAndTheirOwners;
            static CanvasesAndTheirOwners() 
            { 
                UnityEditor.Undo.undoRedoPerformed -= OnUndoRedoPerformed;
                UnityEditor.Undo.undoRedoPerformed += OnUndoRedoPerformed;
            } 
            static void OnUndoRedoPerformed()
            {
                if (sCanvasesAndTheirOwners != null)
                {
                    List<UnityEngine.Object> toDestroy = null;
                    foreach (var v in sCanvasesAndTheirOwners)
                    {
                        if (v.Value == null)
                        {
                            if (toDestroy == null)
                                toDestroy = new List<UnityEngine.Object>();
                            toDestroy.Add(v.Key);
                        }
                    }
                    if (toDestroy != null)
                    {
                        foreach (var o in toDestroy)
                        {
                            RemoveCanvas(o);
                            RuntimeUtility.DestroyObject(o);
                        }
                    }
                }
            }
            public static void RemoveCanvas(UnityEngine.Object canvas)
            {
                if (sCanvasesAndTheirOwners != null && sCanvasesAndTheirOwners.ContainsKey(canvas))
                    sCanvasesAndTheirOwners.Remove(canvas);
            }
            public static void AddCanvas(UnityEngine.Object canvas, UnityEngine.Object owner)
            {
                if (sCanvasesAndTheirOwners == null)
                    sCanvasesAndTheirOwners = new Dictionary<UnityEngine.Object, UnityEngine.Object>();
                sCanvasesAndTheirOwners.Add(canvas, owner);
            }
        }
#endif
    }
}
