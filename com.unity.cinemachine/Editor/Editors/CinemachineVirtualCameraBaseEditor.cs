﻿using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;
using System.Linq;
using Cinemachine.Utility;

#if CINEMACHINE_UNITY_INPUTSYSTEM
using UnityEngine.InputSystem;
#endif

#if CINEMACHINE_HDRP || CINEMACHINE_LWRP_7_3_1
    #if CINEMACHINE_HDRP_7_3_1
        using UnityEngine.Rendering.HighDefinition;
    #else
        #if CINEMACHINE_LWRP_7_3_1
            using UnityEngine.Rendering.Universal;
        #else
            using UnityEngine.Experimental.Rendering.HDPipeline;
        #endif
    #endif
#endif

#if UNITY_2019_1_OR_NEWER
    using CameraExtensions = UnityEngine.Camera;
#else
    // Needed only for Unity pre-2019.1 because Camera doesn't have these methods
    static class CameraExtensions
    {
        public static float HorizontalToVerticalFieldOfView(float f, float aspect)
        {
            return Mathf.Rad2Deg * 2 * Mathf.Atan(Mathf.Tan(f * Mathf.Deg2Rad * 0.5f) / aspect);
        }

        public static float VerticalToHorizontalFieldOfView(float f, float aspect)
        {
            return Mathf.Rad2Deg * 2 * Mathf.Atan(Mathf.Tan(f * Mathf.Deg2Rad * 0.5f) * aspect);
        }

        public static float FieldOfViewToFocalLength(float fov, float sensorHeight)
        {
            return sensorHeight * 0.5f / Mathf.Tan(Mathf.Deg2Rad * fov * 0.5f);
        }

        public static float FocalLengthToFieldOfView(float focalLength, float sensorHeight)
        {
            if (focalLength < UnityVectorExtensions.Epsilon)
                return 180f;
            return Mathf.Rad2Deg * 2.0f * Mathf.Atan(sensorHeight * 0.5f / focalLength);
        }
    }
#endif


namespace Cinemachine.Editor
{
    /// <summary>
    /// Base class for virtual camera editors.
    /// Handles drawing the header and the basic properties.
    /// </summary>
    /// <typeparam name="T">The type of CinemachineVirtualCameraBase being edited</typeparam>
    public class CinemachineVirtualCameraBaseEditor<T> : BaseEditor<T> where T : CinemachineVirtualCameraBase
    {    
        /// <summary>A collection of GUIContent for use in the inspector</summary>
        public static class Styles
        {
            /// <summary>GUIContent for Add Extension</summary>
            public static GUIContent addExtensionLabel = new GUIContent("Add Extension");
            /// <summary>GUIContent for no-multi-select message</summary>
            public static GUIContent virtualCameraChildrenInfoMsg 
                = new GUIContent("The Virtual Camera Children field is not available when multiple objects are selected.");
        }
        
        static Type[] sExtensionTypes;  // First entry is null
        static string[] sExtensionNames;
        bool IsPrefabBase { get; set; }

        /// <summary>Obsolete, do not use.  Use the overload, which is more performant</summary>
        /// <returns>List of property names to exclude</returns>
        protected override List<string> GetExcludedPropertiesInInspector() 
            { return base.GetExcludedPropertiesInInspector(); }

        /// <summary>Get the property names to exclude in the inspector.  
        /// Implementation should call the base class implementation</summary>
        /// <param name="excluded">Add the names to this list</param>
        protected override void GetExcludedPropertiesInInspector(List<string> excluded)
        {
            base.GetExcludedPropertiesInInspector(excluded);
            if (Target.m_ExcludedPropertiesInInspector != null)
                excluded.AddRange(Target.m_ExcludedPropertiesInInspector);
        }

        /// <summary>Update state information on undo/redo</summary>
        void UpdateCameraState() 
        { 
            if (Target != null) 
                Target.InternalUpdateCameraState(Vector3.up, -1); 
        }

        /// <summary>Inspector panel is being enabled.  
        /// Implementation should call the base class implementation</summary>
        protected virtual void OnEnable()
        {
            Undo.undoRedoPerformed += UpdateCameraState;
            
            IsPrefabBase = Target.gameObject.scene.name == null; // causes a small GC alloc
            if (sExtensionTypes == null)
            {
                // Populate the extension list
                List<Type> exts = new List<Type>();
                List<string> names = new List<string>();
                exts.Add(null);
                names.Add("(select)");
                var allExtensions
                    = ReflectionHelpers.GetTypesInAllDependentAssemblies(
                            (Type t) => typeof(CinemachineExtension).IsAssignableFrom(t) && !t.IsAbstract);
                foreach (Type t in allExtensions)
                {
                    exts.Add(t);
                    names.Add(t.Name);
                }
                sExtensionTypes = exts.ToArray();
                sExtensionNames = names.ToArray();
            }
        }

        /// <summary>Inspector panel is being disabled.
        /// Implementation should call the base class implementation</summary>
        protected virtual void OnDisable()
        {
            Undo.undoRedoPerformed -= UpdateCameraState;
            
            if (CinemachineBrain.SoloCamera == (ICinemachineCamera)Target)
            {
                CinemachineBrain.SoloCamera = null;
                InspectorUtility.RepaintGameView();
            }
        }

        /// <summary>Create the contents of the inspector panel.
        /// This implementation draws header and Extensions widget, and uses default algorithms 
        /// to draw the properties in the inspector</summary>
        public override void OnInspectorGUI()
        {
            BeginInspector();
            DrawUpgradeButton();
            DrawCameraStatusInInspector();
            DrawGlobalControlsInInspector();
            DrawInputProviderButtonInInspector();
            DrawRemainingPropertiesInInspector();
            DrawExtensionsWidgetInInspector();
        }

        protected void DrawUpgradeButton()
        {
            var attrs = serializedObject.targetObject.GetType()
                .GetCustomAttributes(typeof(ObsoleteAttribute), true);
            if (attrs != null && attrs.Length > 0)
            {
#if true // For testing only - do not release with this because no undo and no animation fixup
                if (GUI.Button(EditorGUILayout.GetControlRect(), new GUIContent("Convert to CmCamera")))
                {
                    var upgrader = new CinemachineUpgradeManager();
                    Undo.SetCurrentGroupName("Convert to CmCamera");
                    for (int i = 0; i < targets.Length; ++i)
                    {
                        upgrader.Upgrade(((CinemachineVirtualCameraBase)targets[i]).gameObject);
                    }
                    GUIUtility.ExitGUI();
                }
#endif
                if (GUI.Button(EditorGUILayout.GetControlRect(), new GUIContent("Upgrade Project to Cinemachine 3")))
                {
                    var upgrader = new CinemachineUpgradeManager();
                    upgrader.UpgradeAll();
                    GUIUtility.ExitGUI();
                }
                EditorGUILayout.Space();
            }
        }

#if CINEMACHINE_UNITY_INPUTSYSTEM
        static GUIContent s_InputProviderAddLabel = new GUIContent("Add Input Provider", 
            "Adds CinemachineInputProvider component to this vcam, "
            + "if it does not have one, enabling the vcam to read input from Input Actions. "
            + "By default, a simple mouse XY input action is added.");

        /// <summary>
        /// Draw a message prompting the user to add a CinemachineInputProvider.  
        /// Does nothing if Input package not installed.
        /// </summary>
        protected void DrawInputProviderButtonInInspector()
        {
            bool needsButton = false;
            for (int i = 0; !needsButton && i < targets.Length; ++i)
            {
                var vcam = (CinemachineVirtualCameraBase)targets[i];
                if (vcam.RequiresUserInput() && vcam.GetComponent<AxisState.IInputAxisProvider>() == null)
                    needsButton = true;
            }
            if (!needsButton)
                return;

            EditorGUILayout.Space();
            InspectorUtility.HelpBoxWithButton(
                "The InputSystem package is installed, but it is not used to control this vcam.", 
                MessageType.Info,
                new GUIContent("Add Input\nProvider"), () =>
                {
                    Undo.SetCurrentGroupName("Add CinemachineInputProvider");
                    for (int i = 0; i < targets.Length; ++i)
                    {
                        var vcam = (CinemachineVirtualCameraBase)targets[i];
                        if (vcam.GetComponent<AxisState.IInputAxisProvider>() != null)
                            continue;
                        var inputProvider = Undo.AddComponent<CinemachineInputProvider>(vcam.gameObject);
                        inputProvider.XYAxis = ScriptableObjectUtility.DefaultLookAction;
                    }
                });
            EditorGUILayout.Space();
        }
#else
        /// <summary>
        /// Draw a message prompting the user to add a CinemachineInputProvider.  
        /// Does nothing if Input package not installed.
        /// </summary>
        protected void DrawInputProviderButtonInInspector() {}
#endif

        /// <summary>
        /// Draw the LookAt and Follow targets in the inspector
        /// </summary>
        /// <param name="followTarget">Follow target SerializedProperty</param>
        /// <param name="lookAtTarget">LookAt target SerializedProperty</param>
        protected void DrawTargetsInInspector(
            SerializedProperty followTarget, SerializedProperty lookAtTarget)
        {
            EditorGUI.BeginChangeCheck();
            if (!IsPropertyExcluded(followTarget.name))
            {
                if (Target.ParentCamera == null || Target.ParentCamera.Follow == null)
                    EditorGUILayout.PropertyField(followTarget);
                else
                    EditorGUILayout.PropertyField(followTarget,
                        new GUIContent(followTarget.displayName + " Override"));
                ExcludeProperty(followTarget.name);
            }
            if (!IsPropertyExcluded(lookAtTarget.name))
            {
                if (Target.ParentCamera == null || Target.ParentCamera.LookAt == null)
                    EditorGUILayout.PropertyField(lookAtTarget);
                else
                    EditorGUILayout.PropertyField(lookAtTarget,
                        new GUIContent(lookAtTarget.displayName + " Override"));
                ExcludeProperty(lookAtTarget.name);
            }
            if (EditorGUI.EndChangeCheck())
                serializedObject.ApplyModifiedProperties();
        }

        /// <summary>
        /// Draw the Extensions dropdown in the inspector
        /// </summary>
        protected void DrawExtensionsWidgetInInspector()
        {
            if (!IsPropertyExcluded("Extensions"))
            {
                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Extensions", EditorStyles.boldLabel);
                Rect rect = EditorGUILayout.GetControlRect(true, EditorGUIUtility.singleLineHeight);
                rect = EditorGUI.PrefixLabel(rect, Styles.addExtensionLabel);

                int selection = EditorGUI.Popup(rect, 0, sExtensionNames);
                if (selection > 0)
                {
                    Type extType = sExtensionTypes[selection];
                    for (int i = 0; i < targets.Length; i++)
                    {
                        var targetGO = (targets[i] as CinemachineVirtualCameraBase).gameObject;
                        if (targetGO != null && targetGO.GetComponent(extType) == null)
                            Undo.AddComponent(targetGO, extType);
                    }
                }
                ExcludeProperty("Extensions");
            }
        }

        /// <summary>
        /// Draw the Live status in the inspector, and the Solo button
        /// </summary>
        protected void DrawCameraStatusInInspector()
        {
            if (Selection.objects.Length > 1)
                return;
            
            // Is the camera navel-gazing?
            CameraState state = Target.State;
            if (state.HasLookAt && (state.ReferenceLookAt - state.CorrectedPosition).AlmostZero())
                EditorGUILayout.HelpBox(
                    "The camera is positioned on the same point at which it is trying to look.",
                    MessageType.Warning);

            // No status and Solo for prefabs
            if (IsPrefabBase)
                return;

            // Active status and Solo button
            Rect rect = EditorGUILayout.GetControlRect(true);
            Rect rectLabel = new Rect(rect.x, rect.y, EditorGUIUtility.labelWidth, rect.height);
            rect.width -= rectLabel.width;
            rect.x += rectLabel.width;

            Color color = GUI.color;
            bool isSolo = (CinemachineBrain.SoloCamera == (ICinemachineCamera)Target);
            if (isSolo)
                GUI.color = CinemachineBrain.GetSoloGUIColor();

            bool isLive = CinemachineCore.Instance.IsLive(Target);
            GUI.enabled = isLive;
            GUI.Label(rectLabel, isLive ? "Status: Live"
                : (Target.isActiveAndEnabled ? "Status: Standby" : "Status: Disabled"));
            GUI.enabled = true;

            float labelWidth = 0;
            GUIContent updateText = GUIContent.none;
            UpdateTracker.UpdateClock updateMode = CinemachineCore.Instance.GetVcamUpdateStatus(Target);
            if (Application.isPlaying)
            {
                updateText = new GUIContent(
                    updateMode == UpdateTracker.UpdateClock.Fixed ? " Fixed Update" : " Late Update");
                var textDimensions = GUI.skin.label.CalcSize(updateText);
                labelWidth = textDimensions.x;
            }
            rect.width -= labelWidth;
            if (GUI.Button(rect, "Solo", "Button"))
            {
                isSolo = !isSolo;
                CinemachineBrain.SoloCamera = isSolo ? Target : null;
                InspectorUtility.RepaintGameView();
            }
            GUI.color = color;
            if (isSolo && !Application.isPlaying)
                InspectorUtility.RepaintGameView();

            if (labelWidth > 0)
            {
                GUI.enabled = false;
                rect.x += rect.width; rect.width = labelWidth;
                GUI.Label(rect, updateText);
                GUI.enabled = true;
            }
        }

        static GUIContent ShowInGameGuidesLabel = new GUIContent(
            "Game Window Guides",
            "Enable the display of overlays in the Game window.  "
                + "You can adjust colours and opacity in Cinemachine Preferences.");

        static GUIContent SaveDuringPlayLabel = new GUIContent(
            "Save During Play",
            "If checked, Virtual Camera settings changes made during Play Mode "
                + "will be propagated back to the scene when Play Mode is exited.");

        /// <summary>
        /// Draw the global settings controls in the inspector
        /// </summary>
        protected void DrawGlobalControlsInInspector()
        {
            CinemachineSettings.CinemachineCoreSettings.ShowInGameGuides
                = EditorGUILayout.Toggle(ShowInGameGuidesLabel,
                    CinemachineSettings.CinemachineCoreSettings.ShowInGameGuides);

            SaveDuringPlay.SaveDuringPlay.Enabled
                = EditorGUILayout.Toggle(SaveDuringPlayLabel, SaveDuringPlay.SaveDuringPlay.Enabled);

            if (Application.isPlaying && SaveDuringPlay.SaveDuringPlay.Enabled)
                EditorGUILayout.HelpBox(
                    " Virtual Camera settings changes made during Play Mode will be "
                        + "propagated back to the scene when Play Mode is exited.",
                    MessageType.Info);
        }

        internal bool IsHorizontalFOVUsed() 
        {
            // This should be a global UX setting, but the best we can do now is to query 
            // the associated camera (if any) for the lens FOV display mode
            Camera camera = null;
            var brain = CinemachineCore.Instance.FindPotentialTargetBrain(Target);
            if (brain != null)
                camera = brain.OutputCamera;
            if (camera != null)
            {
                var p = new SerializedObject(camera).FindProperty("m_FOVAxisMode");
                if (p != null && p.intValue == (int)Camera.FieldOfViewAxis.Horizontal)
                    return true;
            }
            return false;
        }
    }
}

