using UnityEngine;
using UnityEditor;
using Cinemachine.Utility;
using UnityEngine.UIElements;

namespace Cinemachine.Editor
{
    // This is necessary because we don't get mouse events in the game view in Edit mode
    class GameViewEventCatcher
    {
        class Dragger
        {
            VisualElement m_Root;

            //bool m_Active;
            //void OnMouseDown(MouseDownEvent e) { if (m_Root.panel != null) m_Active = true; }
            //void OnMouseUp(MouseUpEvent e) { m_Active = false; }
            void OnMouseMove(MouseMoveEvent e)
            {
                if (/*m_Active &&*/ m_Root.panel != null)
                {
                    if (!Application.isPlaying
                        && CinemachineCorePrefs.ShowInGameGuides.Value
                        && CinemachineCorePrefs.DraggableComposerGuides.Value
                        && CinemachineBrain.SoloCamera == null)
                    {
                        InspectorUtility.RepaintGameView();
                    }
                }
            }

            public Dragger(VisualElement root)
            {
                m_Root = root;
                if (m_Root == null || m_Root.panel == null || m_Root.panel.visualTree == null)
                    return;
                //m_Root.panel.visualTree.RegisterCallback<MouseDownEvent>(OnMouseDown, TrickleDown.TrickleDown);
                //m_Root.panel.visualTree.RegisterCallback<MouseUpEvent>(OnMouseUp, TrickleDown.TrickleDown);
                m_Root.panel.visualTree.RegisterCallback<MouseMoveEvent>(OnMouseMove, TrickleDown.TrickleDown);
            }

            public void Unregister()
            {
                if (m_Root == null || m_Root.panel == null || m_Root.panel.visualTree == null)
                    return;
                //m_Root.panel.visualTree.UnregisterCallback<MouseDownEvent>(OnMouseDown, TrickleDown.TrickleDown);
                //m_Root.panel.visualTree.UnregisterCallback<MouseUpEvent>(OnMouseUp, TrickleDown.TrickleDown);
                m_Root.panel.visualTree.UnregisterCallback<MouseMoveEvent>(OnMouseMove, TrickleDown.TrickleDown);
            }
        }

        Dragger[] m_Draggers;

        // Create manipulator in each game view
        public void OnEnable()
        {
            System.Reflection.Assembly assembly = typeof(UnityEditor.EditorWindow).Assembly;
            System.Type type = assembly.GetType( "UnityEditor.GameView" );
            var gameViews = UnityEngine.Resources.FindObjectsOfTypeAll(type);
            m_Draggers = new Dragger[gameViews.Length];

            for (int i = 0; i < gameViews.Length; ++i)
            {
                var gameViewRoot = (gameViews[i] as UnityEditor.EditorWindow).rootVisualElement;
                m_Draggers[i] = new Dragger(gameViewRoot);
            }
        }

        public void OnDisable()
        {
            for (int i = 0; m_Draggers != null && i < m_Draggers.Length; ++i)
            {
                var dragger = m_Draggers[i];
                if (dragger != null)
                    dragger.Unregister();
            }
            m_Draggers = null;
        }
    }

    /// <summary>
    /// Use an instance of this class to draw screen composer guides in the game view.
    /// This is an internal class, and is not meant to be called outside of Cinemachine.
    /// </summary>
    class CinemachineScreenComposerGuides
    {
        /// <summary>Delegate for getting the hard/soft guide rects</summary>
        /// <returns>The Hard/Soft guide rect</returns>
        public delegate ScreenComposerSettings CompositionGetter();

        /// <summary>Delegate for setting the hard/soft guide rects</summary>
        /// <param name="s">The value to set</param>
        public delegate void CompositionSetter(ScreenComposerSettings s);

        /// <summary>Delegate to get the current object whose guides are being drawn</summary>
        /// <returns>The target object whose guides are being drawn</returns>
        public delegate SerializedObject ObjectGetter();

        /// <summary>Get the Composition settings.  Client must implement this</summary>
        public CompositionGetter GetComposition;
        /// <summary>Get the Composition settings.  Client must implement this</summary>
        public CompositionSetter SetComposition;
        /// <summary>Get the target object whose guides are being drawn.  Client must implement this</summary>
        public ObjectGetter Target;

        /// <summary>Width of the draggable guide bar in the game view</summary>
        public const float kGuideBarWidthPx = 3f;

        // For dragging the bars - order defines precedence
        enum DragBar
        {
            Center,
            SoftBarLineLeft, SoftBarLineTop, SoftBarLineRight, SoftBarLineBottom,
            HardBarLineLeft, HardBarLineTop, HardBarLineRight, HardBarLineBottom,
            NONE
        };

        DragBar m_IsDragging = DragBar.NONE;
        DragBar m_IsHot = DragBar.NONE;
        Rect[] m_DragBars = new Rect[9];

        Rect GetCameraRect(Camera outputCamera, LensSettings lens)
        {
            Rect cameraRect = outputCamera.pixelRect;
            float screenHeight = cameraRect.height;
            float screenWidth = cameraRect.width;

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
        public void OnGUI_DrawGuides(bool isLive, Camera outputCamera, LensSettings lens)
        {
            Rect cameraRect = GetCameraRect(outputCamera, lens);
            float screenWidth = cameraRect.width;
            float screenHeight = cameraRect.height;

            // Rotate the guides along with the dutch
            Matrix4x4 oldMatrix = GUI.matrix;
            GUI.matrix = Matrix4x4.Translate(cameraRect.min);
            GUIUtility.RotateAroundPivot(lens.Dutch, cameraRect.center);
            Color hardBarsColour = CinemachineComposerPrefs.HardBoundsOverlayColour.Value;
            Color softBarsColour = CinemachineComposerPrefs.SoftBoundsOverlayColour.Value;
            float overlayOpacity = CinemachineComposerPrefs.OverlayOpacity.Value;
            if (!isLive)
            {
                softBarsColour = CinemachineCorePrefs.InactiveGizmoColour.Value;
                hardBarsColour = Color.Lerp(softBarsColour, Color.black, 0.5f);
                overlayOpacity /= 2;
            }
            hardBarsColour.a *= overlayOpacity;
            softBarsColour.a *= overlayOpacity;

            Rect r = GetComposition().HardLimitsRect;
            float hardEdgeLeft = r.xMin * screenWidth;
            float hardEdgeTop = r.yMin * screenHeight;
            float hardEdgeRight = r.xMax * screenWidth;
            float hardEdgeBottom = r.yMax * screenHeight;

            m_DragBars[(int)DragBar.HardBarLineLeft] = new Rect(hardEdgeLeft - kGuideBarWidthPx / 2f, 0f, kGuideBarWidthPx, screenHeight);
            m_DragBars[(int)DragBar.HardBarLineTop] = new Rect(0f, hardEdgeTop - kGuideBarWidthPx / 2f, screenWidth, kGuideBarWidthPx);
            m_DragBars[(int)DragBar.HardBarLineRight] = new Rect(hardEdgeRight - kGuideBarWidthPx / 2f, 0f, kGuideBarWidthPx, screenHeight);
            m_DragBars[(int)DragBar.HardBarLineBottom] = new Rect(0f, hardEdgeBottom - kGuideBarWidthPx / 2f, screenWidth, kGuideBarWidthPx);

            r = GetComposition().DeadZoneRect;
            float softEdgeLeft = r.xMin * screenWidth;
            float softEdgeTop = r.yMin * screenHeight;
            float softEdgeRight = r.xMax * screenWidth;
            float softEdgeBottom = r.yMax * screenHeight;

            m_DragBars[(int)DragBar.SoftBarLineLeft] = new Rect(softEdgeLeft - kGuideBarWidthPx / 2f, 0f, kGuideBarWidthPx, screenHeight);
            m_DragBars[(int)DragBar.SoftBarLineTop] = new Rect(0f, softEdgeTop - kGuideBarWidthPx / 2f, screenWidth, kGuideBarWidthPx);
            m_DragBars[(int)DragBar.SoftBarLineRight] = new Rect(softEdgeRight - kGuideBarWidthPx / 2f, 0f, kGuideBarWidthPx, screenHeight);
            m_DragBars[(int)DragBar.SoftBarLineBottom] = new Rect(0f, softEdgeBottom - kGuideBarWidthPx / 2f, screenWidth, kGuideBarWidthPx);

            m_DragBars[(int)DragBar.Center] = new Rect(softEdgeLeft, softEdgeTop, softEdgeRight - softEdgeLeft, softEdgeBottom - softEdgeTop);

            // Handle dragging bars
            if (CinemachineCorePrefs.DraggableComposerGuides.Value && isLive)
                OnGuiHandleBarDragging(screenWidth, screenHeight);

            // Draw the masks
            var oldColor = GUI.color;
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
            GUI.DrawTexture(m_DragBars[(int)DragBar.SoftBarLineLeft], Texture2D.whiteTexture, ScaleMode.StretchToFill);
            GUI.DrawTexture(m_DragBars[(int)DragBar.SoftBarLineTop], Texture2D.whiteTexture, ScaleMode.StretchToFill);
            GUI.DrawTexture(m_DragBars[(int)DragBar.SoftBarLineRight], Texture2D.whiteTexture, ScaleMode.StretchToFill);
            GUI.DrawTexture(m_DragBars[(int)DragBar.SoftBarLineBottom], Texture2D.whiteTexture, ScaleMode.StretchToFill);

            GUI.color = hardBarsColour;
            GUI.DrawTexture(m_DragBars[(int)DragBar.HardBarLineLeft], Texture2D.whiteTexture, ScaleMode.StretchToFill);
            GUI.DrawTexture(m_DragBars[(int)DragBar.HardBarLineTop], Texture2D.whiteTexture, ScaleMode.StretchToFill);
            GUI.DrawTexture(m_DragBars[(int)DragBar.HardBarLineRight], Texture2D.whiteTexture, ScaleMode.StretchToFill);
            GUI.DrawTexture(m_DragBars[(int)DragBar.HardBarLineBottom], Texture2D.whiteTexture, ScaleMode.StretchToFill);

            // Highlight the hot one
            if (m_IsHot != DragBar.NONE)
            {
                GUI.color = new Color(1, 1, 1, 0.2f);
                var k = m_IsHot == DragBar.Center ? 10 : 2;
                GUI.DrawTexture(m_DragBars[(int)m_IsHot].Inflated(new Vector2(k, k)), Texture2D.whiteTexture, ScaleMode.StretchToFill);
            }

            GUI.matrix = oldMatrix;
            GUI.color = oldColor;
        }

        void OnGuiHandleBarDragging(float screenWidth, float screenHeight)
        {
            if (Event.current.type == EventType.MouseUp)
                m_IsDragging = m_IsHot = DragBar.NONE;
            if (Event.current.type == EventType.MouseDown)
                m_IsDragging = GetDragBarUnderPoint(Event.current.mousePosition);
            if (Event.current.type == EventType.Repaint)
                m_IsHot = m_IsDragging != DragBar.NONE ? m_IsDragging : GetDragBarUnderPoint(Event.current.mousePosition);

            // Handle an actual drag event
            if (m_IsDragging != DragBar.NONE && Event.current.type == EventType.MouseDrag)
            {
                var d = new Vector2(Event.current.delta.x / screenWidth, Event.current.delta.y / screenHeight);

                // First snapshot some settings
                var newHard = GetComposition().HardLimitsRect;
                var newSoft = GetComposition().DeadZoneRect;
                var changed = Vector2.zero;
                switch (m_IsDragging)
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
                SetNewBounds(GetComposition().HardLimitsRect, GetComposition().DeadZoneRect, newHard, newSoft);
                Event.current.Use();
            }
        }

        DragBar GetDragBarUnderPoint(Vector2 point)
        {
            var bar = DragBar.NONE;
            for (DragBar i = DragBar.Center; i < DragBar.NONE && bar == DragBar.NONE; ++i)
            {
                var slop = new Vector2(5f, 5f);
                if (i == DragBar.Center)
                {
                    if (m_DragBars[(int)i].width > 3f * slop.x)
                        slop.x = -slop.x;
                    if (m_DragBars[(int)i].height > 3f * slop.y)
                        slop.y = -slop.y;
                }
                var r = m_DragBars[(int)i].Inflated(slop);
                if (r.Contains(point))
                    bar = i;
            }
            return bar;
        }

        /// <summary>
        /// Helper to set the appropriate new rects in the target object, if something changed.
        /// </summary>
        /// <param name="oldHard">Current hard guide</param>
        /// <param name="oldSoft">Current soft guide</param>
        /// <param name="newHard">New hard guide</param>
        /// <param name="newSoft">New soft guide</param>
        void SetNewBounds(Rect oldHard, Rect oldSoft, Rect newHard, Rect newSoft)
        {
            if ((oldSoft != newSoft) || (oldHard != newHard))
            {
                Undo.RecordObject(Target().targetObject, "Composer Bounds");
                var c = GetComposition();
                if (oldSoft != newSoft)
                    c.DeadZoneRect = newSoft;
                if (oldHard != newHard)
                    c.HardLimitsRect = newHard;
                SetComposition(c);
                Target().ApplyModifiedProperties();
            }
        }
    }
}
