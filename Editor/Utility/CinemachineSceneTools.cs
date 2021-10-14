#if UNITY_2021_2_OR_NEWER
using System;
using System.Collections.Generic;
using Cinemachine.Utility;
using UnityEditor;
using UnityEngine;
using UnityEngine.Experimental.Rendering;

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
        /// Checks whether tool is the currently active tool.
        /// </summary>
        /// <param name="tool">Tool to check.</param>
        /// <returns>True, when the tool is the active tool. False, otherwise.</returns>
        public static bool IsToolActive(Type tool)
        {
            return s_ActiveTool == tool;
        }
        static Type s_ActiveTool;

        /// <summary>
        /// Register your Type from the editor script's OnEnable function.
        /// This way CinemachineTools will know which tools to display.
        /// </summary>
        /// <param name="tool">Tool to register</param>
        public static void RegisterTool(Type tool)
        {
            s_RequiredTools.Add(tool);
        }
        
        /// <summary>
        /// Unregister your Type from the editor script's OnDisable function.
        /// This way CinemachineTools will know which tools to display.
        /// </summary>
        /// <param name="tool">Tool to register</param>
        public static void UnregisterTool(Type tool)
        {
            s_RequiredTools.Remove(tool);
        }
        static HashSet<Type> s_RequiredTools;

        public delegate void ToolHandler(bool v);
        struct CinemachineSceneToolDelegates
        {
            public ToolHandler ToggleSetter;
            public ToolHandler IsDisplayedSetter;
        }
        static SortedDictionary<Type, CinemachineSceneToolDelegates> s_Tools;

        internal static void RegisterToolHandlers(Type tool, ToolHandler toggleSetter, ToolHandler isDisplayedSetter)
        {
            if (s_Tools.ContainsKey(tool))
            {
                s_Tools.Remove(tool);
            }
            
            s_Tools.Add(tool, new CinemachineSceneToolDelegates
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


        internal static void SetTool(bool active, Type tool)
        {
            if (active)
            {
                s_ActiveTool = tool;
                EnsureCinemachineToolsAreExclusiveWithUnityTools();
            }
            else
            {
                s_ActiveTool = s_ActiveTool == tool ? null : s_ActiveTool;
            }
        }

        static void EnsureCinemachineToolsAreExclusiveWithUnityTools()
        {
            foreach (var (key, value) in s_Tools)
            {
                value.ToggleSetter(key == s_ActiveTool);
            }
            if (s_ActiveTool != null)
            {
                Tools.current = Tool.None; // Cinemachine tools are exclusive with unity tools
            }
        }
        
        static CinemachineSceneToolUtility()
        {
            s_Tools = new SortedDictionary<Type, CinemachineSceneToolDelegates>(Comparer<Type>.Create(
                (x, y) => x.ToString().CompareTo(y.ToString())));
            s_RequiredTools = new HashSet<Type>();
            
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
                if (Tools.current != Tool.None || cmToolbarIsHidden)
                {
                    SetTool(true, null);
                }

                if (!cmToolbarIsHidden)
                {
                    // only display cm tools that are relevant for the current selection
                    foreach (var (key, value) in s_Tools)
                    {
                        value.IsDisplayedSetter(s_RequiredTools.Contains(key));
                    }
                }
            };
        }
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
            var labelOffset = HandleUtility.GetHandleSize(position) / 10f;
            Handles.Label(position + new Vector3(0, -labelOffset, 0), text, s_LabelStyle);
        }

        static int s_ScaleSliderHash = "ScaleSliderHash".GetHashCode();
        public static void FovToolHandle(
            CinemachineVirtualCameraBase vcam, ref LensSettings lens, bool isLensHorizontal)
        {
            var camPos = vcam.State.FinalPosition;
            var camRot = vcam.State.FinalOrientation;
            var camForward = camRot * Vector3.forward;
                
            EditorGUI.BeginChangeCheck();
            var fovHandleId = GUIUtility.GetControlID(s_ScaleSliderHash, FocusType.Passive) + 1; // TODO: KGB workaround until id is exposed

            var fieldOfView = Handles.ScaleSlider(
                lens.Orthographic ? lens.OrthographicSize : lens.FieldOfView, 
                camPos, -camForward, camRot, HandleUtility.GetHandleSize(camPos), 0.1f);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(vcam, "Changed FOV using handle in scene view.");

                if (lens.Orthographic)
                {
                    lens.OrthographicSize = fieldOfView;
                }
                else
                {
                    lens.FieldOfView = fieldOfView;
                }
                lens.Validate();
                    
                InspectorUtility.RepaintGameView();
            }

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
                
            if (GUIUtility.hotControl == fovHandleId) 
                CinemachineBrain.SoloCamera = vcam;
        }

        public static void NearFarClipHandle(CinemachineVirtualCameraBase vcam, ref LensSettings lens)
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

            
            if (GUIUtility.hotControl == ncHandleId || GUIUtility.hotControl == fcHandleId) 
                CinemachineBrain.SoloCamera = vcam;
        }

        public static void TrackedObjectOffsetTool(
            CinemachineComponentBase cmComponent, ref Vector3 trackedObjectOffset)
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

            if (trackedObjectOffsetHandleIsDragged)
                CinemachineBrain.SoloCamera = cmComponent.VirtualCamera;
        }

        public static void TransposerFollowOffsetTool(CinemachineTransposer cmComponent)
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

            if (followOffsetHandleIsDragged) 
                CinemachineBrain.SoloCamera = cmComponent.VirtualCamera;
        }
        
        
    } 
}
#endif