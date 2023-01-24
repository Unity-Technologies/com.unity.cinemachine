﻿using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;
using System.Linq;
using Cinemachine.Utility;
using System.Reflection;

#if CINEMACHINE_UNITY_INPUTSYSTEM
using UnityEngine.InputSystem;
#endif

#if CINEMACHINE_HDRP
    using UnityEngine.Rendering.HighDefinition;
#elif CINEMACHINE_LWRP_7_3_1
    using UnityEngine.Rendering.Universal;
#endif

namespace Cinemachine.Editor
{
    /// <summary>
    /// Base class for virtual camera editors.
    /// Handles drawing the header and the basic properties.
    /// </summary>
    /// <typeparam name="T">The type of CinemachineVirtualCameraBase being edited</typeparam>
    class CinemachineVirtualCameraBaseEditor<T> : BaseEditor<T> where T : CinemachineVirtualCameraBase
    {    
        static GUIContent s_AddExtensionLabel = new ("Add Extension", "Add a Cinemachine Extension behaviour");

        static Type[] sExtensionTypes;  // First entry is null
        static string[] sExtensionNames;
        bool IsPrefabBase { get; set; }

        /// <summary>Obsolete, do not use.  Use the overload, which is more performant</summary>
        /// <returns>List of property names to exclude</returns>
        protected override List<string> GetExcludedPropertiesInInspector() 
            { return base.GetExcludedPropertiesInInspector(); }

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
                            (Type t) => typeof(CinemachineExtension).IsAssignableFrom(t)
                            && !t.IsAbstract && t.GetCustomAttribute<ObsoleteAttribute>() == null);
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
            
            if (CinemachineBrain.SoloCamera == Target)
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
            UpgradeManagerInspectorHelpers.DrawUpgradeControls(this, "CinemachineCamera");
            DrawCameraStatusInInspector();
            DrawGlobalControlsInInspector();
            DrawInputProviderButtonInInspector();
            DrawRemainingPropertiesInInspector();
            DrawExtensionsWidgetInInspector();
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
#pragma warning disable 618 // using obsolete stuff
        protected void DrawInputProviderButtonInInspector()
        {
            bool needsButton = false;
            for (int i = 0; !needsButton && i < targets.Length; ++i)
            {
                var vcam = (CinemachineVirtualCameraBase)targets[i];
                var requirer = vcam as AxisState.IRequiresInput;
                if (requirer != null && requirer.RequiresInput() && !vcam.TryGetComponent<AxisState.IInputAxisProvider>(out _))
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
                        if (vcam.TryGetComponent<AxisState.IInputAxisProvider>(out var _))
                            continue;
                        var inputProvider = Undo.AddComponent<CinemachineInputProvider>(vcam.gameObject);
                        inputProvider.XYAxis = (InputActionReference)AssetDatabase.LoadAllAssetsAtPath(
                            "Packages/com.unity.inputsystem/InputSystem/Plugins/PlayerInput/DefaultInputActions.inputactions").FirstOrDefault(
                                x => x.name == "Player/Look");
                    }
                });
            EditorGUILayout.Space();
        }
#pragma warning restore 618
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
                rect = EditorGUI.PrefixLabel(rect, s_AddExtensionLabel);

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
            if (state.HasLookAt() && (state.ReferenceLookAt - state.GetCorrectedPosition()).AlmostZero())
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
            bool isSolo = (CinemachineBrain.SoloCamera == Target);
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

        GUIContent m_GuidesLabel;
        static GUIContent[] s_GuidesChoices = new [] { new GUIContent("Disabled"), new GUIContent("Passive"), new GUIContent("Interactive") };

        /// <summary>
        /// Draw the global settings controls in the inspector
        /// </summary>
        protected void DrawGlobalControlsInInspector()
        {
            if (m_GuidesLabel == null)
                m_GuidesLabel = new ("Game View Guides", CinemachineCorePrefs.s_ShowInGameGuidesLabel.tooltip);

            SaveDuringPlay.SaveDuringPlay.Enabled = EditorGUILayout.Toggle(
                CinemachineCorePrefs.s_SaveDuringPlayLabel, SaveDuringPlay.SaveDuringPlay.Enabled);

            int index = CinemachineCorePrefs.ShowInGameGuides.Value 
                ? (CinemachineCorePrefs.DraggableComposerGuides.Value ? 2 : 1) : 0;
            var newIndex = EditorGUILayout.Popup(m_GuidesLabel, index, s_GuidesChoices);
            if (index != newIndex)
            {
                CinemachineCorePrefs.ShowInGameGuides.Value = newIndex != 0;
                CinemachineCorePrefs.DraggableComposerGuides.Value = newIndex == 2;
                InspectorUtility.RepaintGameView();
            }

            if (Application.isPlaying && SaveDuringPlay.SaveDuringPlay.Enabled)
                EditorGUILayout.HelpBox(
                    "CinemachineCamera settings changes made during Play Mode will be "
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

