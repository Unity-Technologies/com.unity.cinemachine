#if !UNITY_2019_1_OR_NEWER
#define CINEMACHINE_UGUI
#endif

using UnityEngine;

#if CINEMACHINE_UGUI
using System.Collections.Generic;

namespace Cinemachine
{
    /// <summary>
    /// An add-on module for Cinemachine Virtual Camera that places an image in screen space
    /// over the camera's output.
    /// </summary>
    [SaveDuringPlay]
    [DocumentationSorting(DocumentationSortingAttribute.Level.UserRef)]
    [AddComponentMenu("")] // Hide in menu
#if UNITY_2018_3_OR_NEWER
    [ExecuteAlways]
#else
    [ExecuteInEditMode]
#endif
    [DisallowMultipleComponent]
    [HelpURL(Documentation.BaseURL + "manual/CinemachineStoryboard.html")]
    public class CinemachineStoryboard : CinemachineExtension
    {
        /// <summary>
        /// If checked, all storyboards are globally muted
        /// </summary>
        [Tooltip("If checked, all storyboards are globally muted")]
        public static bool s_StoryboardGlobalMute;

        /// <summary>
        /// If checked, the specified image will be displayed as an overlay over the virtual camera's output
        /// </summary>
        [Tooltip("If checked, the specified image will be displayed as an overlay over the virtual camera's output")]
        public bool m_ShowImage = true;

        /// <summary>
        /// The image to display
        /// </summary>
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
        /// <summary>
        /// How to handle differences between image aspect and screen aspect
        /// </summary>
        [Tooltip("How to handle differences between image aspect and screen aspect")]
        public FillStrategy m_Aspect = FillStrategy.BestFit;

        /// <summary>
        /// The opacity of the image.  0 is transparent, 1 is opaque
        /// </summary>
        [Tooltip("The opacity of the image.  0 is transparent, 1 is opaque")]
        [Range(0, 1)]
        public float m_Alpha = 1;

        /// <summary>
        /// The screen-space position at which to display the image.  Zero is center
        /// </summary>
        [Tooltip("The screen-space position at which to display the image.  Zero is center")]
        public Vector2 m_Center = Vector2.zero;

        /// <summary>
        /// The screen-space rotation to apply to the image
        /// </summary>
        [Tooltip("The screen-space rotation to apply to the image")]
        public Vector3 m_Rotation = Vector3.zero;

        /// <summary>
        /// The screen-space scaling to apply to the image
        /// </summary>
        [Tooltip("The screen-space scaling to apply to the image")]
        public Vector2 m_Scale = Vector3.one;

        /// <summary>
        /// If checked, X and Y scale are synchronized
        /// </summary>
        [Tooltip("If checked, X and Y scale are synchronized")]
        public bool m_SyncScale = true;

        /// <summary>
        /// If checked, Camera transform will not be controlled by this virtual camera
        /// </summary>
        [Tooltip("If checked, Camera transform will not be controlled by this virtual camera")]
        public bool m_MuteCamera;

        /// <summary>
        /// Wipe the image on and off horizontally
        /// </summary>
        [Range(-1, 1)]
        [Tooltip("Wipe the image on and off horizontally")]
        public float m_SplitView = 0f;

        class CanvasInfo
        {
            public GameObject mCanvas;
            public CinemachineBrain mCanvasParent;
            public RectTransform mViewport; // for mViewport clipping
            public UnityEngine.UI.RawImage mRawImage;
        }
        List<CanvasInfo> mCanvasInfo = new List<CanvasInfo>();

        /// <summary>Callback to display the image</summary>
        /// <param name="vcam">The virtual camera being processed</param>
        /// <param name="stage">The current pipeline stage</param>
        /// <param name="state">The current virtual camera state</param>
        /// <param name="deltaTime">The current applicable deltaTime</param>
        protected override void PostPipelineStageCallback(
            CinemachineVirtualCameraBase vcam,
            CinemachineCore.Stage stage, ref CameraState state, float deltaTime)
        {
            // Apply to this vcam only, not the children
            if (vcam != VirtualCamera || stage != CinemachineCore.Stage.Finalize)
                return;

            if (m_ShowImage)
                state.AddCustomBlendable(new CameraState.CustomBlendable(this, 1));
            if (m_MuteCamera)
                state.BlendHint |= CameraState.BlendHintValue.NoTransform | CameraState.BlendHintValue.NoLens;
        }

        /// <summary>Connect to virtual camera.  Adds/removes listener</summary>
        /// <param name="connect">True if connecting, false if disconnecting</param>
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

        void CameraUpdatedCallback(CinemachineBrain brain)
        {
            bool showIt = enabled && m_ShowImage && CinemachineCore.Instance.IsLive(VirtualCamera);
            int layer = 1 << gameObject.layer;
            if (brain.OutputCamera == null || (brain.OutputCamera.cullingMask & layer) == 0)
                showIt = false;
            if (s_StoryboardGlobalMute)
                showIt = false;
            CanvasInfo ci = LocateMyCanvas(brain, showIt);
            if (ci != null && ci.mCanvas != null)
                ci.mCanvas.SetActive(showIt);
        }

        CanvasInfo LocateMyCanvas(CinemachineBrain parent, bool createIfNotFound)
        {
            CanvasInfo ci = null;
            for (int i = 0; ci == null && i < mCanvasInfo.Count; ++i)
                if (mCanvasInfo[i].mCanvasParent == parent)
                    ci = mCanvasInfo[i];
            if (createIfNotFound)
            {
                if (ci == null)
                {
                    ci = new CanvasInfo() { mCanvasParent = parent };
                    int numChildren = parent.transform.childCount;
                    for (int i = 0; ci.mCanvas == null && i < numChildren; ++i)
                    {
                        RectTransform child = parent.transform.GetChild(i) as RectTransform;
                        if (child != null && child.name == CanvasName)
                        {
                            ci.mCanvas = child.gameObject;
                            ci.mViewport = ci.mCanvas.GetComponentInChildren<RectTransform>();
                            ci.mRawImage = ci.mCanvas.GetComponentInChildren<UnityEngine.UI.RawImage>();
                        }
                    }
                    mCanvasInfo.Add(ci);
                }
                if (ci.mCanvas == null || ci.mViewport == null || ci.mRawImage == null)
                    CreateCanvas(ci);
            }
            return ci;
        }

        void CreateCanvas(CanvasInfo ci)
        {
            ci.mCanvas = new GameObject(CanvasName, typeof(RectTransform));
            ci.mCanvas.layer = gameObject.layer;
            ci.mCanvas.hideFlags = HideFlags.HideAndDontSave;
            ci.mCanvas.transform.SetParent(ci.mCanvasParent.transform);
#if UNITY_EDITOR
            // Workaround for Unity bug case Case 1004117
            CanvasesAndTheirOwners.AddCanvas(ci.mCanvas, this);
#endif

            var c = ci.mCanvas.AddComponent<Canvas>();
            c.renderMode = RenderMode.ScreenSpaceOverlay;

            var go = new GameObject("Viewport", typeof(RectTransform));
            go.transform.SetParent(ci.mCanvas.transform);
            ci.mViewport = (RectTransform)go.transform;
            go.AddComponent<UnityEngine.UI.RectMask2D>();

            go = new GameObject("RawImage", typeof(RectTransform));
            go.transform.SetParent(ci.mViewport.transform);
            ci.mRawImage = go.AddComponent<UnityEngine.UI.RawImage>();
        }

        void DestroyCanvas()
        {
            int numBrains = CinemachineCore.Instance.BrainCount;
            for (int i = 0; i < numBrains; ++i)
            {
                var parent = CinemachineCore.Instance.GetActiveBrain(i);
                int numChildren = parent.transform.childCount;
                for (int j = numChildren - 1; j >= 0; --j)
                {
                    RectTransform child = parent.transform.GetChild(j) as RectTransform;
                    if (child != null && child.name == CanvasName)
                    {
                        var canvas = child.gameObject;
                        RuntimeUtility.DestroyObject(canvas);
#if UNITY_EDITOR
                        // Workaround for Unity bug case Case 1004117
                        CanvasesAndTheirOwners.RemoveCanvas(canvas);
#endif
                    }
                }
            }
            mCanvasInfo.Clear();
        }

        void PlaceImage(CanvasInfo ci, float alpha)
        {
            if (ci.mRawImage != null && ci.mViewport != null)
            {
                Rect screen = new Rect(0, 0, Screen.width, Screen.height);
                if (ci.mCanvasParent.OutputCamera != null)
                    screen = ci.mCanvasParent.OutputCamera.pixelRect;
                screen.x -= (float)Screen.width/2;
                screen.y -= (float)Screen.height/2;

                // Apply Split View
                float wipeAmount = -Mathf.Clamp(m_SplitView, -1, 1) * screen.width;

                Vector3 pos = screen.center;
                pos.x -= wipeAmount/2;
                ci.mViewport.localPosition = pos;
                ci.mViewport.localRotation = Quaternion.identity;
                ci.mViewport.localScale = Vector3.one;
                ci.mViewport.ForceUpdateRectTransforms();
                ci.mViewport.sizeDelta = new Vector2(screen.width - Mathf.Abs(wipeAmount), screen.height);

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

                ci.mRawImage.texture = m_Image;
                Color tintColor = Color.white;
                tintColor.a = m_Alpha * alpha;
                ci.mRawImage.color = tintColor;

                pos = new Vector2(screen.width * m_Center.x, screen.height * m_Center.y);
                pos.x += wipeAmount/2;
                ci.mRawImage.rectTransform.localPosition = pos;
                ci.mRawImage.rectTransform.localRotation = Quaternion.Euler(m_Rotation);
                ci.mRawImage.rectTransform.localScale = scale;
                ci.mRawImage.rectTransform.ForceUpdateRectTransforms();
                ci.mRawImage.rectTransform.sizeDelta = screen.size;
            }
        }

        static void StaticBlendingHandler(CinemachineBrain brain)
        {
            CameraState state = brain.CurrentCameraState;
            int numBlendables = state.NumCustomBlendables;
            for (int i = 0; i < numBlendables; ++i)
            {
                var b = state.GetCustomBlendable(i);
                CinemachineStoryboard src = b.m_Custom as CinemachineStoryboard;
                if (!(src == null)) // in case it was deleted
                {
                    bool showIt = true;
                    int layer = 1 << src.gameObject.layer;
                    if (brain.OutputCamera == null || (brain.OutputCamera.cullingMask & layer) == 0)
                        showIt = false;
                    if (s_StoryboardGlobalMute)
                        showIt = false;
                    CanvasInfo ci = src.LocateMyCanvas(brain, showIt);
                    if (ci != null)
                        src.PlaceImage(ci, b.m_Weight);
                }
            }
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
#else
// We need this dummy MonoBehaviour for Unity to properly recognize this script asset.
namespace Cinemachine
{
    [AddComponentMenu("")] // Hide in menu
    public class CinemachineStoryboard : MonoBehaviour {}
}
#endif

