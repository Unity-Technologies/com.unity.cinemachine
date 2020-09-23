using UnityEngine;
using UnityEditor;
using Cinemachine.Utility;

#if UNITY_2019_2_OR_NEWER
using UnityEngine.UIElements;
#endif

namespace Cinemachine.Editor
{
#if !UNITY_2019_2_OR_NEWER
    internal class GameViewEventCatcher
    {
        public void OnEnable() {}
        public void OnDisable() {}
    }
#else
    // This is necessary because in 2019.3 we don't get mouse events in the game view in Edit mode
    internal class GameViewEventCatcher
    {
        class Dragger
        {
            bool mActive;
            VisualElement mRoot;

            void OnMouseDown(MouseDownEvent e) { if (mRoot.panel != null) mActive = true; }
            void OnMouseUp(MouseUpEvent e) { mActive = false; }
            void OnMouseMove(MouseMoveEvent e)
            {
                if (mActive && mRoot.panel != null)
                {
                    if (!Application.isPlaying
                        && CinemachineSettings.CinemachineCoreSettings.ShowInGameGuides
                        && CinemachineBrain.SoloCamera == null)
                    {
                        InspectorUtility.RepaintGameView();
                    }
                }
            }

            public Dragger(VisualElement root)
            {
                mRoot = root;
                if (mRoot == null || mRoot.panel == null || mRoot.panel.visualTree == null)
                    return;
                mRoot.panel.visualTree.RegisterCallback<MouseDownEvent>(OnMouseDown, TrickleDown.TrickleDown);
                mRoot.panel.visualTree.RegisterCallback<MouseUpEvent>(OnMouseUp, TrickleDown.TrickleDown);
                mRoot.panel.visualTree.RegisterCallback<MouseMoveEvent>(OnMouseMove, TrickleDown.TrickleDown);
            }

            public void Unregister()
            {
                if (mRoot == null || mRoot.panel == null || mRoot.panel.visualTree == null)
                    return;
                mRoot.panel.visualTree.UnregisterCallback<MouseDownEvent>(OnMouseDown, TrickleDown.TrickleDown);
                mRoot.panel.visualTree.UnregisterCallback<MouseUpEvent>(OnMouseUp, TrickleDown.TrickleDown);
                mRoot.panel.visualTree.UnregisterCallback<MouseMoveEvent>(OnMouseMove, TrickleDown.TrickleDown);
            }
        }

        Dragger[] mDraggers;

        // Create manipulator in each game view
        public void OnEnable()
        {
            System.Reflection.Assembly assembly = typeof(UnityEditor.EditorWindow).Assembly;
            System.Type type = assembly.GetType( "UnityEditor.GameView" );
            var gameViews = UnityEngine.Resources.FindObjectsOfTypeAll(type);
            mDraggers = new Dragger[gameViews.Length];

            for (int i = 0; i < gameViews.Length; ++i)
            {
                var gameViewRoot = (gameViews[i] as UnityEditor.EditorWindow).rootVisualElement;
                mDraggers[i] = new Dragger(gameViewRoot);
            }
        }

        public void OnDisable()
        {
            for (int i = 0; mDraggers != null && i < mDraggers.Length; ++i)
            {
                var dragger = mDraggers[i];
                if (dragger != null)
                    dragger.Unregister();
            }
            mDraggers = null;
        }
    }
#endif

    /// <summary>
    /// Use an instance of this class to draw screen composer guides in the game view.
    /// This is an internal class, and is not meant to be called outside of Cinemachine.
    /// </summary>
    public class CinemachineScreenComposerGuides
    {
        /// <summary>Delegate for getting the hard/soft guide rects</summary>
        /// <returns>The Hard/Soft guide rect</returns>
        public delegate Rect RectGetter();

        /// <summary>Delegate for setting the hard/soft guide rects</summary>
        /// <param name="rcam">The value to set</param>
        public delegate void RectSetter(Rect r);

        /// <summary>Delegate to get the current object whose guides are being drawn</summary>
        /// <returns>The target object whose guides are being drawn</returns>
        public delegate SerializedObject ObjectGetter();

        /// <summary>Get the Hard Guide.  Client must implement this</summary>
        public RectGetter GetHardGuide;
        /// <summary>Get the Soft Guide.  Client must implement this</summary>
        public RectGetter GetSoftGuide;
        /// <summary>Set the Hard Guide.  Client must implement this</summary>
        public RectSetter SetHardGuide;
        /// <summary>Get the Soft Guide.  Client must implement this</summary>
        public RectSetter SetSoftGuide;
        /// <summary>Get the target object whose guides are being drawn.  Client must implement this</summary>
        public ObjectGetter Target;

        /// <summary>Width of the draggable guide bar in the game view</summary>
        public const float kGuideBarWidthPx = 3f;

        /// <summary>
        /// Helper to set the appropriate new rects in the target object, is something changed.
        /// </summary>
        /// <param name="oldHard">Current hard guide</param>
        /// <param name="oldSoft">Current soft guide</param>
        /// <param name="newHard">New hard guide</param>
        /// <param name="newSoft">New soft guide</param>
        public void SetNewBounds(Rect oldHard, Rect oldSoft, Rect newHard, Rect newSoft)
        {
            if ((oldSoft != newSoft) || (oldHard != newHard))
            {
                Undo.RecordObject(Target().targetObject, "Composer Bounds");
                if (oldSoft != newSoft)
                    SetSoftGuide(newSoft);
                if (oldHard != newHard)
                    SetHardGuide(newHard);
                Target().ApplyModifiedProperties();
            }
        }

        Rect GetCameraRect(Camera outputCamera, LensSettings lens)
        {
            Rect cameraRect = outputCamera.pixelRect;
            float screenHeight = cameraRect.height;
            float screenWidth = cameraRect.width;

#if UNITY_2018_2_OR_NEWER
            float screenAspect = screenWidth / screenHeight;
            switch (outputCamera.gateFit)
            {
                case Camera.GateFitMode.Vertical:
                    screenWidth = screenHeight * lens.Aspect;
                    cameraRect.position += new Vector2((cameraRect.width - screenWidth) * 0.5f, 0);
                    break;
                case Camera.GateFitMode.Horizontal:
                    screenHeight = screenWidth / lens.Aspect;
                    cameraRect.position += new Vector2(0, (cameraRect.height - screenHeight) * 0.5f);
                    break;
                case Camera.GateFitMode.Overscan:
                    if (screenAspect < lens.Aspect)
                    {
                        screenHeight = screenWidth / lens.Aspect;
                        cameraRect.position += new Vector2(0, (cameraRect.height - screenHeight) * 0.5f);
                    }
                    else
                    {
                        screenWidth = screenHeight * lens.Aspect;
                        cameraRect.position += new Vector2((cameraRect.width - screenWidth) * 0.5f, 0);
                    }
                    break;
                case Camera.GateFitMode.Fill:
                    if (screenAspect > lens.Aspect)
                    {
                        screenHeight = screenWidth / lens.Aspect;
                        cameraRect.position += new Vector2(0, (cameraRect.height - screenHeight) * 0.5f);
                    }
                    else
                    {
                        screenWidth = screenHeight * lens.Aspect;
                        cameraRect.position += new Vector2((cameraRect.width - screenWidth) * 0.5f, 0);
                    }
                    break;
                case Camera.GateFitMode.None:
                    break;
            }
#endif
            cameraRect = new Rect(cameraRect.position, new Vector2(screenWidth, screenHeight));

            // Invert Y
            float h = cameraRect.height;
            cameraRect.yMax = Screen.height - cameraRect.yMin;
            cameraRect.yMin = cameraRect.yMax - h;

            // Shift the guides along with the lens
            cameraRect.position += new Vector2(
                -screenWidth * lens.LensShift.x, screenHeight * lens.LensShift.y);

            return cameraRect;
        }

        /// <summary>
        /// Call this from the inspector's OnGUI.  Draws the guides and manages dragging.
        /// </summary>
        /// <param name="isLive">Is the target live</param>
        /// <param name="outputCamera">Destination camera</param>
        /// <param name="lens">Current lens settings</param>
        /// <param name="showHardGuides">True if hard guides should be shown</param>
        public void OnGUI_DrawGuides(bool isLive, Camera outputCamera, LensSettings lens, bool showHardGuides)
        {
            Rect cameraRect = GetCameraRect(outputCamera, lens);
            float screenWidth = cameraRect.width;
            float screenHeight = cameraRect.height;

            // Rotate the guides along with the dutch
            Matrix4x4 oldMatrix = GUI.matrix;
            GUI.matrix = Matrix4x4.Translate(cameraRect.min);
            GUIUtility.RotateAroundPivot(lens.Dutch, cameraRect.center);
            Color hardBarsColour = CinemachineSettings.ComposerSettings.HardBoundsOverlayColour;
            Color softBarsColour = CinemachineSettings.ComposerSettings.SoftBoundsOverlayColour;
            float overlayOpacity = CinemachineSettings.ComposerSettings.OverlayOpacity;
            if (!isLive)
            {
                softBarsColour = CinemachineSettings.CinemachineCoreSettings.InactiveGizmoColour;
                hardBarsColour = Color.Lerp(softBarsColour, Color.black, 0.5f);
                overlayOpacity /= 2;
            }
            hardBarsColour.a *= overlayOpacity;
            softBarsColour.a *= overlayOpacity;

            Rect r = showHardGuides ? GetHardGuide() : new Rect(-2, -2, 4, 4);
            float hardEdgeLeft = r.xMin * screenWidth;
            float hardEdgeTop = r.yMin * screenHeight;
            float hardEdgeRight = r.xMax * screenWidth;
            float hardEdgeBottom = r.yMax * screenHeight;

            mDragBars[(int)DragBar.HardBarLineLeft] = new Rect(hardEdgeLeft - kGuideBarWidthPx / 2f, 0f, kGuideBarWidthPx, screenHeight);
            mDragBars[(int)DragBar.HardBarLineTop] = new Rect(0f, hardEdgeTop - kGuideBarWidthPx / 2f, screenWidth, kGuideBarWidthPx);
            mDragBars[(int)DragBar.HardBarLineRight] = new Rect(hardEdgeRight - kGuideBarWidthPx / 2f, 0f, kGuideBarWidthPx, screenHeight);
            mDragBars[(int)DragBar.HardBarLineBottom] = new Rect(0f, hardEdgeBottom - kGuideBarWidthPx / 2f, screenWidth, kGuideBarWidthPx);

            r = GetSoftGuide();
            float softEdgeLeft = r.xMin * screenWidth;
            float softEdgeTop = r.yMin * screenHeight;
            float softEdgeRight = r.xMax * screenWidth;
            float softEdgeBottom = r.yMax * screenHeight;

            mDragBars[(int)DragBar.SoftBarLineLeft] = new Rect(softEdgeLeft - kGuideBarWidthPx / 2f, 0f, kGuideBarWidthPx, screenHeight);
            mDragBars[(int)DragBar.SoftBarLineTop] = new Rect(0f, softEdgeTop - kGuideBarWidthPx / 2f, screenWidth, kGuideBarWidthPx);
            mDragBars[(int)DragBar.SoftBarLineRight] = new Rect(softEdgeRight - kGuideBarWidthPx / 2f, 0f, kGuideBarWidthPx, screenHeight);
            mDragBars[(int)DragBar.SoftBarLineBottom] = new Rect(0f, softEdgeBottom - kGuideBarWidthPx / 2f, screenWidth, kGuideBarWidthPx);

            mDragBars[(int)DragBar.Center] = new Rect(softEdgeLeft, softEdgeTop, softEdgeRight - softEdgeLeft, softEdgeBottom - softEdgeTop);

            // Handle dragging bars
            if (isLive)
                OnGuiHandleBarDragging(screenWidth, screenHeight);

            // Draw the masks
            GUI.color = hardBarsColour;
            Rect hardBarLeft = new Rect(0, hardEdgeTop, Mathf.Max(0, hardEdgeLeft), hardEdgeBottom - hardEdgeTop);
            Rect hardBarRight = new Rect(hardEdgeRight, hardEdgeTop,
                    Mathf.Max(0, screenWidth - hardEdgeRight), hardEdgeBottom - hardEdgeTop);
            Rect hardBarTop = new Rect(Mathf.Min(0, hardEdgeLeft), 0,
                    Mathf.Max(screenWidth, hardEdgeRight) - Mathf.Min(0, hardEdgeLeft), Mathf.Max(0, hardEdgeTop));
            Rect hardBarBottom = new Rect(Mathf.Min(0, hardEdgeLeft), hardEdgeBottom,
                    Mathf.Max(screenWidth, hardEdgeRight) - Mathf.Min(0, hardEdgeLeft),
                    Mathf.Max(0, screenHeight - hardEdgeBottom));
            GUI.DrawTexture(hardBarLeft, Texture2D.whiteTexture, ScaleMode.StretchToFill);
            GUI.DrawTexture(hardBarTop, Texture2D.whiteTexture, ScaleMode.StretchToFill);
            GUI.DrawTexture(hardBarRight, Texture2D.whiteTexture, ScaleMode.StretchToFill);
            GUI.DrawTexture(hardBarBottom, Texture2D.whiteTexture, ScaleMode.StretchToFill);

            GUI.color = softBarsColour;
            Rect softBarLeft = new Rect(hardEdgeLeft, softEdgeTop, softEdgeLeft - hardEdgeLeft, softEdgeBottom - softEdgeTop);
            Rect softBarTop = new Rect(hardEdgeLeft, hardEdgeTop, hardEdgeRight - hardEdgeLeft, softEdgeTop - hardEdgeTop);
            Rect softBarRight = new Rect(softEdgeRight, softEdgeTop, hardEdgeRight - softEdgeRight, softEdgeBottom - softEdgeTop);
            Rect softBarBottom = new Rect(hardEdgeLeft, softEdgeBottom, hardEdgeRight - hardEdgeLeft, hardEdgeBottom - softEdgeBottom);
            GUI.DrawTexture(softBarLeft, Texture2D.whiteTexture, ScaleMode.StretchToFill);
            GUI.DrawTexture(softBarTop, Texture2D.whiteTexture, ScaleMode.StretchToFill);
            GUI.DrawTexture(softBarRight, Texture2D.whiteTexture, ScaleMode.StretchToFill);
            GUI.DrawTexture(softBarBottom, Texture2D.whiteTexture, ScaleMode.StretchToFill);

            // Draw the drag bars
            GUI.DrawTexture(mDragBars[(int)DragBar.SoftBarLineLeft], Texture2D.whiteTexture, ScaleMode.StretchToFill);
            GUI.DrawTexture(mDragBars[(int)DragBar.SoftBarLineTop], Texture2D.whiteTexture, ScaleMode.StretchToFill);
            GUI.DrawTexture(mDragBars[(int)DragBar.SoftBarLineRight], Texture2D.whiteTexture, ScaleMode.StretchToFill);
            GUI.DrawTexture(mDragBars[(int)DragBar.SoftBarLineBottom], Texture2D.whiteTexture, ScaleMode.StretchToFill);

            GUI.color = hardBarsColour;
            GUI.DrawTexture(mDragBars[(int)DragBar.HardBarLineLeft], Texture2D.whiteTexture, ScaleMode.StretchToFill);
            GUI.DrawTexture(mDragBars[(int)DragBar.HardBarLineTop], Texture2D.whiteTexture, ScaleMode.StretchToFill);
            GUI.DrawTexture(mDragBars[(int)DragBar.HardBarLineRight], Texture2D.whiteTexture, ScaleMode.StretchToFill);
            GUI.DrawTexture(mDragBars[(int)DragBar.HardBarLineBottom], Texture2D.whiteTexture, ScaleMode.StretchToFill);

            GUI.matrix = oldMatrix;
        }

        // For dragging the bars - order defines precedence
        private enum DragBar
        {
            Center,
            SoftBarLineLeft, SoftBarLineTop, SoftBarLineRight, SoftBarLineBottom,
            HardBarLineLeft, HardBarLineTop, HardBarLineRight, HardBarLineBottom,
            NONE
        };
        private DragBar mDragging = DragBar.NONE;
        private Rect[] mDragBars = new Rect[9];

        private void OnGuiHandleBarDragging(float screenWidth, float screenHeight)
        {
            if (Event.current.type == EventType.MouseUp)
                mDragging = DragBar.NONE;
            if (Event.current.type == EventType.MouseDown)
            {
                mDragging = DragBar.NONE;
                for (DragBar i = DragBar.Center; i < DragBar.NONE && mDragging == DragBar.NONE; ++i)
                {
                    Vector2 slop = new Vector2(5f, 5f);
                    if (i == DragBar.Center)
                    {
                        if (mDragBars[(int)i].width > 3f * slop.x)
                            slop.x = -slop.x;
                        if (mDragBars[(int)i].height > 3f * slop.y)
                            slop.y = -slop.y;
                    }
                    Rect r = mDragBars[(int)i].Inflated(slop);
                    if (r.Contains(Event.current.mousePosition))
                        mDragging = i;
                }
            }

            if (mDragging != DragBar.NONE && Event.current.type == EventType.MouseDrag)
            {
                Vector2 d = new Vector2(
                        Event.current.delta.x / screenWidth,
                        Event.current.delta.y / screenHeight);

                // First snapshot some settings
                Rect newHard = GetHardGuide();
                Rect newSoft = GetSoftGuide();
                Vector2 changed = Vector2.zero;
                switch (mDragging)
                {
                    case DragBar.Center: newSoft.position += d; break;
                    case DragBar.SoftBarLineLeft: newSoft = newSoft.Inflated(new Vector2(-d.x, 0)); break;
                    case DragBar.SoftBarLineRight: newSoft = newSoft.Inflated(new Vector2(d.x, 0)); break;
                    case DragBar.SoftBarLineTop: newSoft = newSoft.Inflated(new Vector2(0, -d.y)); break;
                    case DragBar.SoftBarLineBottom: newSoft = newSoft.Inflated(new Vector2(0, d.y)); break;
                    case DragBar.HardBarLineLeft: newHard = newHard.Inflated(new Vector2(-d.x, 0)); break;
                    case DragBar.HardBarLineRight: newHard = newHard.Inflated(new Vector2(d.x, 0)); break;
                    case DragBar.HardBarLineBottom: newHard = newHard.Inflated(new Vector2(0, d.y)); break;
                    case DragBar.HardBarLineTop: newHard = newHard.Inflated(new Vector2(0, -d.y)); break;
                }

                // Apply the changes, enforcing the bounds
                SetNewBounds(GetHardGuide(), GetSoftGuide(), newHard, newSoft);
                Event.current.Use();
            }
        }
    }
}
