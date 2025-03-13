using UnityEngine;
using UnityEditor;
using UnityEngine.UIElements;

namespace Unity.Cinemachine.Editor
{
    /// <summary>
    /// Use an instance of this class to draw screen composer guides in the game view.
    /// This is an internal class, and is not meant to be called outside of Cinemachine.
    /// </summary>
    class GameViewComposerGuides
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

        /// <summary>Delegate to get a bool value</summary>
        public delegate bool BoolGetter();

        /// <summary>Get the Composition settings.  Client must implement this</summary>
        public CompositionGetter GetComposition;
        /// <summary>Get the Composition settings.  Client must implement this</summary>
        public CompositionSetter SetComposition;
        /// <summary>Get the target object whose guides are being drawn.  Client must implement this</summary>
        public ObjectGetter Target;
        /// <summary>Override whether the guides may be dragged.  Client may optionally implement this</summary>
        public BoolGetter IsDraggable = () => CinemachineCorePrefs.DraggableComposerGuides.Value;

        // This is necessary because we don't get mouse events in the game view in Edit mode.
        // We need to trigger repaint when the mouse moves over the window.
        class GameViewEventCatcher
        {
            class Dragger
            {
                readonly VisualElement m_Root;

                void OnMouseMove(MouseMoveEvent e)
                {
                    if (m_Root.panel != null && !Application.isPlaying
                        && CinemachineCorePrefs.ShowInGameGuides.Value
                        && CinemachineCorePrefs.DraggableComposerGuides.Value
                        && CinemachineCore.SoloCamera == null)
                    {
                        InspectorUtility.RepaintGameView();
                    }
                }

                public Dragger(VisualElement root)
                {
                    m_Root = root;
                    if (m_Root == null || m_Root.panel == null || m_Root.panel.visualTree == null)
                        return;
                    m_Root.panel.visualTree.RegisterCallback<MouseMoveEvent>(OnMouseMove, TrickleDown.TrickleDown);
                }

                public void Unregister()
                {
                    if (m_Root == null || m_Root.panel == null || m_Root.panel.visualTree == null)
                        return;
                    m_Root.panel.visualTree.UnregisterCallback<MouseMoveEvent>(OnMouseMove, TrickleDown.TrickleDown);
                }
            }

            // One for each game view
            Dragger[] m_Draggers;

            // Create manipulator in each game view
            public void OnEnable()
            {
                System.Reflection.Assembly assembly = typeof(EditorWindow).Assembly;
                System.Type type = assembly.GetType("UnityEditor.GameView");
                var gameViews = Resources.FindObjectsOfTypeAll(type);
                m_Draggers = new Dragger[gameViews.Length];

                for (int i = 0; i < gameViews.Length; ++i)
                {
                    var gameViewRoot = (gameViews[i] as EditorWindow).rootVisualElement;
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

        // For dragging the bars - order defines precedence
        enum DragBar
        {
            Center,
            HardRight, HardBottom, HardLeft, HardTop,
            DeadRight, DeadBottom, DeadLeft, DeadTop,
            NONE
        };

        DragBar m_IsDragging = DragBar.NONE;
        DragBar m_IsHot = DragBar.NONE;
        Rect[] m_DragBars = new Rect[9];

        GameViewEventCatcher m_EventCatcher = new ();

        // Call this from inspector's OnEnable()
        public void OnEnable() => m_EventCatcher.OnEnable();

        // Call this from inspector's OnDisable()
        public void OnDisable() => m_EventCatcher.OnDisable();

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
            if (lens.IsPhysicalCamera)
                cameraRect.position += new Vector2(
                    -screenWidth * lens.PhysicalProperties.LensShift.x, screenHeight * lens.PhysicalProperties.LensShift.y);

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
            var cameraRect = GetCameraRect(outputCamera, lens);

            // Rotate the guides along with the dutch
            Matrix4x4 oldMatrix = GUI.matrix;
            GUI.matrix = Matrix4x4.Translate(cameraRect.min);
            GUIUtility.RotateAroundPivot(lens.Dutch, cameraRect.center);

            // Desaturate colors if not live came
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

            var composition = GetComposition();
            var hard = composition.HardLimitsRect;
            hard = new (hard.xMin * cameraRect.width, hard.yMin * cameraRect.height,
                hard.width * cameraRect.width, hard.height * cameraRect.height);

            const float kBarSize = 2;

            m_DragBars[(int)DragBar.HardLeft] = ClipToCamera(new Rect(hard.xMin, 0f, 0, cameraRect.height).Inflated(new Vector2(kBarSize, 0)));
            m_DragBars[(int)DragBar.HardTop] = ClipToCamera(new Rect(0f, hard.yMin, cameraRect.width, 0).Inflated(new Vector2(0, kBarSize)));
            m_DragBars[(int)DragBar.HardRight] = ClipToCamera(new Rect(hard.xMax, 0f, 0, cameraRect.height).Inflated(new Vector2(kBarSize, 0)));
            m_DragBars[(int)DragBar.HardBottom] = ClipToCamera(new Rect(0f, hard.yMax, cameraRect.width, 0).Inflated(new Vector2(0, kBarSize)));

            var dead = composition.DeadZoneRect;
            dead = new (dead.xMin * cameraRect.width, dead.yMin * cameraRect.height,
                dead.width * cameraRect.width, dead.height * cameraRect.height);

            m_DragBars[(int)DragBar.DeadLeft] = ClipToCamera(new Rect(dead.xMin, 0f, 0, cameraRect.height).Inflated(new Vector2(kBarSize, 0)));
            m_DragBars[(int)DragBar.DeadTop] = ClipToCamera(new Rect(0f, dead.yMin, cameraRect.width, 0).Inflated(new Vector2(0, kBarSize)));
            m_DragBars[(int)DragBar.DeadRight] = ClipToCamera(new Rect(dead.xMax, 0f, 0, cameraRect.height).Inflated(new Vector2(kBarSize, 0)));
            m_DragBars[(int)DragBar.DeadBottom] = ClipToCamera(new Rect(0f, dead.yMax, cameraRect.width, 0).Inflated(new Vector2(0, kBarSize)));

            m_DragBars[(int)DragBar.Center] = ClipToCamera(new Rect(dead.xMin, dead.yMin, dead.xMax - dead.xMin, dead.yMax - dead.yMin));

            // Handle dragging bars
            if (IsDraggable() && isLive)
                OnGuiHandleBarDragging(cameraRect.width, cameraRect.height, ref composition);

            // Draw the masks
            var tex = Texture2D.whiteTexture;
            var oldColor = GUI.color;
            if (composition.HardLimits.Enabled)
            {
                GUI.color = hardBarsColour;
                var left = ClipToCamera(new Rect(0, hard.yMin, hard.xMin, hard.height));
                var right = ClipToCamera(new Rect(hard.xMax, hard.yMin, cameraRect.width - hard.xMax, hard.height));
                var top = ClipToCamera(new Rect(0, 0, cameraRect.width, hard.yMin));
                var bottom = ClipToCamera(new Rect(0, hard.yMax, cameraRect.width, cameraRect.height - hard.yMax));
                GUI.DrawTexture(left, tex, ScaleMode.StretchToFill);
                GUI.DrawTexture(top, tex, ScaleMode.StretchToFill);
                GUI.DrawTexture(right, tex, ScaleMode.StretchToFill);
                GUI.DrawTexture(bottom, tex, ScaleMode.StretchToFill);
            }
            GUI.color = softBarsColour;
            if (composition.DeadZone.Enabled)
            {
                var left = ClipToCamera(new Rect(hard.xMin, dead.yMin, dead.xMin - hard.xMin, dead.height));
                var top = ClipToCamera(new Rect(hard.xMin, hard.yMin, hard.xMax - hard.xMin, dead.yMin - hard.yMin));
                var right = ClipToCamera(new Rect(dead.xMax, dead.yMin, hard.xMax - dead.xMax, dead.yMax - dead.yMin));
                var bottom = ClipToCamera(new Rect(hard.xMin, dead.yMax, hard.xMax - hard.xMin, hard.yMax - dead.yMax));
                GUI.DrawTexture(left, tex, ScaleMode.StretchToFill);
                GUI.DrawTexture(top, tex, ScaleMode.StretchToFill);
                GUI.DrawTexture(right, tex, ScaleMode.StretchToFill);
                GUI.DrawTexture(bottom, tex, ScaleMode.StretchToFill);
            }

            // Draw the drag bars
            GUI.DrawTexture(m_DragBars[(int)DragBar.DeadLeft], tex, ScaleMode.StretchToFill);
            GUI.DrawTexture(m_DragBars[(int)DragBar.DeadTop], tex, ScaleMode.StretchToFill);
            GUI.DrawTexture(m_DragBars[(int)DragBar.DeadRight], tex, ScaleMode.StretchToFill);
            GUI.DrawTexture(m_DragBars[(int)DragBar.DeadBottom], tex, ScaleMode.StretchToFill);
            if (composition.HardLimits.Enabled)
            {
                GUI.color = hardBarsColour;
                GUI.DrawTexture(m_DragBars[(int)DragBar.HardLeft], tex, ScaleMode.StretchToFill);
                GUI.DrawTexture(m_DragBars[(int)DragBar.HardTop], tex, ScaleMode.StretchToFill);
                GUI.DrawTexture(m_DragBars[(int)DragBar.HardRight], tex, ScaleMode.StretchToFill);
                GUI.DrawTexture(m_DragBars[(int)DragBar.HardBottom], tex, ScaleMode.StretchToFill);

            }
            // Highlight the hot one
            if (m_IsHot != DragBar.NONE)
            {
                GUI.color = new Color(0.5f, 1, 1, 0.2f);
                var k = m_IsHot == DragBar.Center ? 10 : 2;
                GUI.DrawTexture(m_DragBars[(int)m_IsHot].Inflated(new Vector2(k, k)), tex, ScaleMode.StretchToFill);
            }

            GUI.matrix = oldMatrix;
            GUI.color = oldColor;

            Rect ClipToCamera(Rect r) => Rect.MinMaxRect(
                Mathf.Clamp(r.xMin, 0, cameraRect.width), Mathf.Clamp(r.yMin, 0, cameraRect.height),
                Mathf.Clamp(r.xMax, 0, cameraRect.width), Mathf.Clamp(r.yMax, 0, cameraRect.height));
        }

        void OnGuiHandleBarDragging(float screenWidth, float screenHeight, ref ScreenComposerSettings composition)
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
                var hard = composition.HardLimitsRect;
                var dead = composition.DeadZoneRect;
                switch (m_IsDragging)
                {
                    case DragBar.Center: dead.position += d; break;
                    case DragBar.DeadLeft:
                    {
                        if (composition.DeadZone.Enabled)
                            dead = dead.Inflated(new Vector2(-d.x, 0));
                        else
                            dead.position += new Vector2(d.x, 0);
                        break;
                    }
                    case DragBar.DeadRight:
                    {
                        if (composition.DeadZone.Enabled)
                            dead = dead.Inflated(new Vector2(d.x, 0));
                        else
                            dead.position += new Vector2(d.x, 0);
                        break;
                    }
                    case DragBar.DeadTop:
                    {
                        if (composition.DeadZone.Enabled)
                            dead = dead.Inflated(new Vector2(0, -d.y));
                        else
                            dead.position += new Vector2(0, d.y);
                        break;
                    }
                    case DragBar.DeadBottom:
                    {
                        if (composition.DeadZone.Enabled)
                            dead = dead.Inflated(new Vector2(0, d.y));
                        else
                            dead.position += new Vector2(0, d.y);
                        break;
                    }
                    case DragBar.HardLeft: hard = hard.Inflated(new Vector2(-d.x, 0)); break;
                    case DragBar.HardRight: hard = hard.Inflated(new Vector2(d.x, 0)); break;
                    case DragBar.HardBottom: hard = hard.Inflated(new Vector2(0, d.y)); break;
                    case DragBar.HardTop: hard = hard.Inflated(new Vector2(0, -d.y)); break;
                }

                // Apply the changes, enforcing the bounds
                SetNewBounds(hard, dead, ref composition);
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
        void SetNewBounds(Rect hard, Rect dead, ref ScreenComposerSettings composition)
        {
            var oldHard = composition.HardLimitsRect;
            var oldDead = composition.DeadZoneRect;
            if (oldDead != dead || oldHard != hard)
            {
                Undo.RecordObject(Target().targetObject, "Composer Bounds");
                var c = GetComposition();
                if (oldDead != dead)
                    c.DeadZoneRect = dead;
                if (oldHard != hard)
                    c.HardLimitsRect = hard;
                SetComposition(c);
                Target().ApplyModifiedProperties();
            }
        }
    }
}
