#if UNITY_2021_2_OR_NEWER
using System;
using System.Collections.Generic;
using Cinemachine.Utility;
using UnityEditor;
using UnityEngine;

namespace Cinemachine.Editor
{
    /// <summary>
    /// Static class that manages Cinemachine Tools. It knows which tool is active,
    /// and ensures that no tools is active at the same time.
    /// The tools and editors requiring tools register/unregister themselves here for control.
    /// </summary>
    static class CinemachineSceneToolUtility
    {
        /// <summary>
        /// Checks whether tool is the currently active exclusive tool.
        /// </summary>
        /// <param name="tool">Tool to check.</param>
        /// <returns>True, when the tool is the active exclusive tool. False, otherwise.</returns>
        public static bool IsToolActive(Type tool)
        {
            return s_ActiveExclusiveTool == tool;
        }
        static Type s_ActiveExclusiveTool;

        /// <summary>
        /// Register your Type from the editor script's OnEnable function.
        /// This way CinemachineTools will know which tools to display.
        /// </summary>
        /// <param name="tool">Tool to register</param>
        public static void RegisterTool(Type tool)
        {
            if (s_RequiredTools.ContainsKey(tool))
            {
                s_RequiredTools[tool]++;
            }
            else
            {
                s_RequiredTools.Add(tool, 1);
            }
        }
        
        /// <summary>
        /// Unregister your Type from the editor script's OnDisable function.
        /// This way CinemachineTools will know which tools to display.
        /// </summary>
        /// <param name="tool">Tool to register</param>
        public static void UnregisterTool(Type tool)
        {
            if (s_RequiredTools.ContainsKey(tool))
            {
                s_RequiredTools[tool]--;
                if (s_RequiredTools[tool] <= 0)
                {
                    s_RequiredTools.Remove(tool);
                }
            }
        }
        static SortedDictionary<Type, int> s_RequiredTools;

        public delegate void ToolHandler(bool v);
        struct CinemachineSceneToolDelegates
        {
            public ToolHandler ToggleSetter;
            public ToolHandler IsDisplayedSetter;
        }
        static SortedDictionary<Type, CinemachineSceneToolDelegates> s_ExclusiveTools; // tools that can't be active at the same time
        static SortedDictionary<Type, CinemachineSceneToolDelegates> s_Tools; // tools without restrictions
        
        /// <summary>
        /// Use for registering tool handlers for tools that are exclusive with each other,
        /// meaning they cannot be active at the same time.
        /// </summary>
        /// <param name="tool">The tool to register.</param>
        /// <param name="toggleSetter">The tool's toggle value setter.</param>
        /// <param name="isDisplayedSetter">The tool's isDisplayed setter.</param>
        internal static void RegisterExclusiveToolHandlers(Type tool, ToolHandler toggleSetter, ToolHandler isDisplayedSetter)
        {
            RegisterToolHandlers(ref s_ExclusiveTools, tool, toggleSetter, isDisplayedSetter);
        }
        
        /// <summary>
        /// Use for registering tool handlers for tools that can be active anytime
        /// without taking other tools into consideration.
        /// </summary>
        /// <param name="tool">The tool to register.</param>
        /// <param name="toggleSetter">The tool's toggle value setter.</param>
        /// <param name="isDisplayedSetter">The tool's isDisplayed setter.</param>
        internal static void RegisterToolHandlers(Type tool, ToolHandler toggleSetter, ToolHandler isDisplayedSetter)
        {
            RegisterToolHandlers(ref s_Tools, tool, toggleSetter, isDisplayedSetter);
        }

        static void RegisterToolHandlers(ref SortedDictionary<Type, CinemachineSceneToolDelegates> tools,
            Type tool, ToolHandler toggleSetter, ToolHandler isDisplayedSetter)
        {
            if (tools.ContainsKey(tool))
            {
                tools.Remove(tool);
            }
            
            tools.Add(tool, new CinemachineSceneToolDelegates
            {
                ToggleSetter = toggleSetter,
                IsDisplayedSetter = isDisplayedSetter,
            });
        }
        
        internal delegate bool ToolbarHandler();
        static ToolbarHandler s_ToolBarIsDisplayed;
        internal static void RegisterToolbarIsDisplayedHandler(ToolbarHandler handler)
        {
            s_ToolBarIsDisplayed = handler;
        }

        static bool s_ToolbarTurnOffByMe;
        internal delegate bool ToolbarTurnOnOffHandler(bool on);
        static ToolbarTurnOnOffHandler s_ToolBarDisplay;
        internal static void RegisterToolbarDisplayHandler(ToolbarTurnOnOffHandler handler)
        {
            s_ToolBarDisplay = handler;
        }

        internal static void SetSolo(bool active)
        {
            if (active)
            {
                foreach (var o in Selection.gameObjects)
                {
                    var vcam = o.GetComponent<ICinemachineCamera>();
                    if (vcam != null)
                    {
                        CinemachineBrain.SoloCamera = vcam;
                        InspectorUtility.RepaintGameView();
                        break;
                    }
                }
            }
            else
            {
                CinemachineBrain.SoloCamera = null;
            }
        }

        internal static void SetTool(bool active, Type tool)
        {
            if (active)
            {
                s_ActiveExclusiveTool = tool;
                EnsureCinemachineToolsAreExclusiveWithUnityTools();
            }
            else
            {
                s_ActiveExclusiveTool = s_ActiveExclusiveTool == tool ? null : s_ActiveExclusiveTool;
            }
        }

        static void EnsureCinemachineToolsAreExclusiveWithUnityTools()
        {
            foreach (var (key, value) in s_ExclusiveTools)
            {
                value.ToggleSetter(key == s_ActiveExclusiveTool);
            }
            if (s_ActiveExclusiveTool != null)
            {
                Tools.current = Tool.None; // Cinemachine tools are exclusive with unity tools
            }
        }
        
        static CinemachineSceneToolUtility()
        {
            s_ExclusiveTools = new SortedDictionary<Type, CinemachineSceneToolDelegates>(Comparer<Type>.Create(
                (x, y) => x.ToString().CompareTo(y.ToString())));
            s_Tools = new SortedDictionary<Type, CinemachineSceneToolDelegates>(Comparer<Type>.Create(
                (x, y) => x.ToString().CompareTo(y.ToString())));
            s_RequiredTools = new SortedDictionary<Type, int>(Comparer<Type>.Create(
                (x, y) => x.ToString().CompareTo(y.ToString())));

            EditorApplication.update += () =>
            {
                if (s_RequiredTools.Count <= 0)
                {
                    s_ToolbarTurnOffByMe |= s_ToolBarDisplay(false);
                }
                else if (s_ToolbarTurnOffByMe)
                {
                    s_ToolBarDisplay(true);
                    s_ToolbarTurnOffByMe = false;
                }
                
                var cmToolbarIsHidden = !s_ToolBarIsDisplayed();
                // if a unity tool is selected or cmToolbar is hidden, unselect our tools.
                if (s_ActiveExclusiveTool != null && (Tools.current != Tool.None || cmToolbarIsHidden))
                {
                    SetTool(true, null);
                }

                if (!cmToolbarIsHidden)
                {
                    // only display cm tools that are relevant for the current selection
                    foreach (var (key, value) in s_ExclusiveTools)
                    {
                        value.IsDisplayedSetter(s_RequiredTools.ContainsKey(key));
                    }
                    foreach (var (key, value) in s_Tools)
                    {
                        value.IsDisplayedSetter(s_RequiredTools.ContainsKey(key));
                    }
                }
                
                if (s_Tools.ContainsKey(s_SoloVcamToolType)) {
                    s_Tools[s_SoloVcamToolType].ToggleSetter(CinemachineBrain.SoloCamera != null);
                }
            };
        }
        static Type s_SoloVcamToolType = typeof(SoloVcamTool);
    }
    
    static class CinemachineSceneToolHelpers
    {
        public static float dottedLineSpacing = 4f;
        public static float lineThickness = 4f;

        static GUIStyle s_LabelStyle = new GUIStyle 
        { 
            normal =
            {
                background = Texture2D.grayTexture,
                textColor = Handles.selectedColor
            },
            fontStyle = FontStyle.Bold,
            padding = new RectOffset(5, 0, 5, 0)
        };
        

        public static float SliderHandleDelta(Vector3 newPos, Vector3 oldPos, Vector3 forward)
        {
            var delta = newPos - oldPos;
            var sameDirection = Vector3.Dot(delta.normalized, forward) > 0;
            return (sameDirection ? 1f : -1f) * delta.magnitude;
        }

        /// <summary>
        /// Calculate delta and discard imprecision.
        /// </summary>
        public static Vector3 PositionHandleDelta(Quaternion rot, Vector3 newPos, Vector3 oldPos)
        {
            var delta =
                Quaternion.Inverse(rot) * (newPos - oldPos);
            delta = new Vector3(
                Mathf.Abs(delta.x) < UnityVectorExtensions.Epsilon ? 0 : delta.x,
                Mathf.Abs(delta.y) < UnityVectorExtensions.Epsilon ? 0 : delta.y,
                Mathf.Abs(delta.z) < UnityVectorExtensions.Epsilon ? 0 : delta.z);
            return delta;
        }

        public static void DrawLabel(Vector3 position, string text)
        {
            var labelOffset = HandleUtility.GetHandleSize(position) / 5f;
            Handles.Label(position + new Vector3(0, -labelOffset, 0), text, s_LabelStyle);
        }

        static int s_ScaleSliderHash = "ScaleSliderHash".GetHashCode();
        public static void FovToolHandle(CinemachineVirtualCameraBase vcam, ref LensSettings lens, 
            bool isLensHorizontal, ref float fov, ref bool soloSetByTools)
        {
            var camPos = vcam.State.FinalPosition;
            var camRot = vcam.State.FinalOrientation;
            var camForward = camRot * Vector3.forward;
                
            EditorGUI.BeginChangeCheck();
            var fovHandleId = GUIUtility.GetControlID(s_ScaleSliderHash, FocusType.Passive) + 1; // TODO: KGB workaround until id is exposed

            var newFov = Handles.ScaleSlider(
                fov, 
                camPos, camForward, camRot, HandleUtility.GetHandleSize(camPos), 0.1f);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(vcam, "Changed FOV using handle in scene view.");

                if (lens.Orthographic)
                {
                    lens.OrthographicSize += (fov - newFov);
                }
                else
                {
                    lens.FieldOfView += (fov - newFov);
                }
                lens.Validate();
                    
                InspectorUtility.RepaintGameView();
            }
            fov = newFov;

            if (GUIUtility.hotControl == fovHandleId || HandleUtility.nearestControl == fovHandleId)
            {
                var labelPos = camPos - camForward * HandleUtility.GetHandleSize(camPos);
                if (lens.IsPhysicalCamera)
                {
                    DrawLabel(labelPos, "Focal Length (" + 
                        Camera.FieldOfViewToFocalLength(lens.FieldOfView, 
                            lens.SensorSize.y).ToString("F1") + ")");
                }
                else if (lens.Orthographic)
                {
                    DrawLabel(labelPos, "Orthographic Size (" + 
                        lens.OrthographicSize.ToString("F1") + ")");
                }
                else if (isLensHorizontal)
                {
                    DrawLabel(labelPos, "Horizontal FOV (" +
                        Camera.VerticalToHorizontalFieldOfView(lens.FieldOfView,
                            lens.Aspect).ToString("F1") + ")");
                }
                else
                {
                    DrawLabel(labelPos, "Vertical FOV (" + 
                        lens.FieldOfView.ToString("F1") + ")");
                }
            }
                
            // if (GUIUtility.hotControl == fovHandleId) 
            //     CinemachineBrain.SoloCamera = vcam;
            SoloOnDrag(GUIUtility.hotControl == fovHandleId, vcam, fovHandleId, ref soloSetByTools);
        }

        public static void NearFarClipHandle(
            CinemachineVirtualCameraBase vcam, ref LensSettings lens, ref bool soloSetByTools)
        {
            var camPos = vcam.State.FinalPosition;
            var camRot = vcam.State.FinalOrientation;
            var camForward = camRot * Vector3.forward;
            var nearClipPos = camPos + camForward * lens.NearClipPlane;
            var farClipPos = camPos + camForward * lens.FarClipPlane;
            
            EditorGUI.BeginChangeCheck();
            var ncHandleId = GUIUtility.GetControlID(FocusType.Passive);
            var newNearClipPos = Handles.Slider(ncHandleId, nearClipPos, camForward, 
                HandleUtility.GetHandleSize(nearClipPos) / 10f, Handles.CubeHandleCap, 0.5f); // division by 10, because this makes it roughly the same size as the default handles
            var fcHandleId = GUIUtility.GetControlID(FocusType.Passive);
            var newFarClipPos = Handles.Slider(fcHandleId, farClipPos, camForward, 
                HandleUtility.GetHandleSize(farClipPos) / 10f, Handles.CubeHandleCap, 0.5f); // division by 10, because this makes it roughly the same size as the default handles
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(vcam, "Changed clip plane using handle in scene view.");
                
                lens.NearClipPlane += 
                    SliderHandleDelta(newNearClipPos, nearClipPos, camForward);
                lens.FarClipPlane += 
                    SliderHandleDelta(newFarClipPos, farClipPos, camForward);
                lens.Validate();
                
                InspectorUtility.RepaintGameView();
            }

            if (GUIUtility.hotControl == ncHandleId || HandleUtility.nearestControl == ncHandleId)
            {
                DrawLabel(nearClipPos,
                    "Near Clip Plane (" + lens.NearClipPlane.ToString("F1") + ")");
            }
            if (GUIUtility.hotControl == fcHandleId || HandleUtility.nearestControl == fcHandleId)
            {
                DrawLabel(farClipPos, 
                    "Far Clip Plane (" + lens.FarClipPlane.ToString("F1") + ")");
            }
            
            SoloOnDrag(GUIUtility.hotControl == ncHandleId || GUIUtility.hotControl == fcHandleId,
                vcam, Mathf.Min(ncHandleId, fcHandleId), ref soloSetByTools);
        }

        public static void TrackedObjectOffsetTool(
            CinemachineComponentBase cmComponent, ref Vector3 trackedObjectOffset, ref bool soloSetByTools)
        {
            var lookAtPos = cmComponent.LookAtTargetPosition;
            var lookAtRot = cmComponent.LookAtTargetRotation;
            var trackedObjectPos = lookAtPos + lookAtRot * trackedObjectOffset;

            EditorGUI.BeginChangeCheck();
            var tooHandleMinId = GUIUtility.GetControlID(FocusType.Passive); // TODO: KGB workaround until id is exposed
            var newTrackedObjectPos = Handles.PositionHandle(trackedObjectPos, lookAtRot);
            var tooHandleMaxId = GUIUtility.GetControlID(FocusType.Passive); // TODO: KGB workaround until id is exposed
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(cmComponent, "Change Tracked Object Offset using handle in Scene View.");
                
                trackedObjectOffset += PositionHandleDelta(lookAtRot, newTrackedObjectPos, trackedObjectPos);

                InspectorUtility.RepaintGameView();
            }

            var trackedObjectOffsetHandleIsDragged = 
                tooHandleMinId < GUIUtility.hotControl && GUIUtility.hotControl < tooHandleMaxId;
            var trackedObjectOffsetHandleIsUsedOrHovered = trackedObjectOffsetHandleIsDragged || 
                tooHandleMinId < HandleUtility.nearestControl && HandleUtility.nearestControl < tooHandleMaxId;
            if (trackedObjectOffsetHandleIsUsedOrHovered)
            {
                DrawLabel(trackedObjectPos, 
                    "(" + cmComponent.Stage + ") Tracked Object Offset " + trackedObjectOffset.ToString("F1"));
            }
            
            var originalColor = Handles.color;
            Handles.color = trackedObjectOffsetHandleIsUsedOrHovered ? 
                Handles.selectedColor : CinemachineSettings.CinemachineCoreSettings.ActiveGizmoColour;
            Handles.DrawDottedLine(lookAtPos, trackedObjectPos, dottedLineSpacing);
            Handles.DrawLine(trackedObjectPos, cmComponent.VcamState.FinalPosition);
            Handles.color = originalColor;

            SoloOnDrag(
                trackedObjectOffsetHandleIsDragged, cmComponent.VirtualCamera, tooHandleMaxId, ref soloSetByTools);
        }

        public static void TransposerFollowOffsetTool(CinemachineTransposer cmComponent, ref bool soloSetByTools)
        {
            var brain = CinemachineCore.Instance.FindPotentialTargetBrain(cmComponent.VirtualCamera);
            var up = brain != null ? brain.DefaultWorldUp : Vector3.up;
            var camPos = cmComponent.GetTargetCameraPosition(up);
            var camRot = cmComponent.GetReferenceOrientation(up);

            EditorGUI.BeginChangeCheck();
            var foHandleMinId = GUIUtility.GetControlID(FocusType.Passive); // TODO: KGB workaround until id is exposed
            var newPos = Handles.PositionHandle(camPos, camRot);
            var foHandleMaxId = GUIUtility.GetControlID(FocusType.Passive); // TODO: KGB workaround until id is exposed
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(cmComponent, 
                    "Change Follow Offset Position using handle in Scene View.");
                
                cmComponent.m_FollowOffset += PositionHandleDelta(camRot, newPos, camPos);
                cmComponent.m_FollowOffset = cmComponent.EffectiveOffset; // sanitize offset
                
                InspectorUtility.RepaintGameView();
            }

            var followOffsetHandleIsDragged = 
                foHandleMinId < GUIUtility.hotControl && GUIUtility.hotControl < foHandleMaxId;
            var followOffsetHandleIsDraggedOrHovered = followOffsetHandleIsDragged || 
                foHandleMinId < HandleUtility.nearestControl && HandleUtility.nearestControl < foHandleMaxId;
            if (followOffsetHandleIsDraggedOrHovered)
            {
                DrawLabel(camPos, "Follow offset " + cmComponent.m_FollowOffset.ToString("F1"));
            }
            var originalColor = Handles.color;
            Handles.color = followOffsetHandleIsDraggedOrHovered ? 
                Handles.selectedColor : CinemachineSettings.CinemachineCoreSettings.ActiveGizmoColour;
            Handles.DrawDottedLine(cmComponent.FollowTargetPosition, camPos, dottedLineSpacing);
            Handles.color = originalColor;

            SoloOnDrag(followOffsetHandleIsDragged, cmComponent.VirtualCamera, foHandleMaxId, ref soloSetByTools);
        }
        
        public static void SoloOnDrag(
            bool isDragged, CinemachineVirtualCameraBase vcam, int handleMaxId, ref bool soloSetByTools)
        {
            if (isDragged)
            {
                // if solo was activated by the user, then it was not the tool who set it to solo.
                soloSetByTools |= CinemachineBrain.SoloCamera != (ICinemachineCamera) vcam;
                CinemachineBrain.SoloCamera = vcam;
            }
            else if (soloSetByTools && handleMaxId != -1) // Handles sometimes return -1 as id, ignore those frames
            {
                CinemachineBrain.SoloCamera = null;
                soloSetByTools = false;
            }
        }
    } 
}
#endif