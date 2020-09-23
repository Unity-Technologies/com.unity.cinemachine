using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;
using Cinemachine.Utility;

namespace Cinemachine.Editor
{
    /// <summary>
    /// Base class for virtual camera editors.
    /// Handles drawing the header and the basic properties.
    /// </summary>
    /// <typeparam name="T">The type of CinemachineVirtualCameraBase being edited</typeparam>
    public class CinemachineVirtualCameraBaseEditor<T>
        : BaseEditor<T> where T : CinemachineVirtualCameraBase
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

        /// <summary>Get the property names to exclude in the inspector.</summary>
        /// <param name="excluded">Add the names to this list</param>
        protected override void GetExcludedPropertiesInInspector(List<string> excluded)
        {
            base.GetExcludedPropertiesInInspector(excluded);
            if (Target.m_ExcludedPropertiesInInspector != null)
                excluded.AddRange(Target.m_ExcludedPropertiesInInspector);
        }

        /// <summary>Inspector panel is being enabled</summary>
        protected virtual void OnEnable()
        {
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

        /// <summary>Inspector panel is being disabled</summary>
        protected virtual void OnDisable()
        {
            if (CinemachineBrain.SoloCamera == (ICinemachineCamera)Target)
            {
                CinemachineBrain.SoloCamera = null;
                InspectorUtility.RepaintGameView();
            }
        }

        /// <summary>Create the contents of the inspector panel</summary>
        public override void OnInspectorGUI()
        {
            BeginInspector();
            DrawHeaderInInspector();
            DrawRemainingPropertiesInInspector();
            DrawExtensionsWidgetInInspector();
        }

        /// <summary>
        /// Draw the virtual camera header in the inspector.  
        /// This includes Solo button, Live status, and global settings
        /// </summary>
        protected void DrawHeaderInInspector()
        {
            if (!IsPropertyExcluded("Header"))
            {
                DrawCameraStatusInInspector();
                DrawGlobalControlsInInspector();
            }
            ExcludeProperty("Header");
        }

        /// <summary>
        /// Draw the LookAt and Follow targets in the inspector
        /// </summary>
        /// <param name="followTarget">Follow target property</param>
        /// <param name="lookAtTarget">LookAt target property</param>
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

        /// <summary>
        /// Draw the global settings controls in the inspector
        /// </summary>
        protected void DrawGlobalControlsInInspector()
        {
            CinemachineSettings.CinemachineCoreSettings.ShowInGameGuides
                = EditorGUILayout.Toggle(
                    new GUIContent(
                        "Game Window Guides",
                        "Enable the display of overlays in the Game window.  You can adjust colours and opacity in Edit/Preferences/Cinemachine."),
                    CinemachineSettings.CinemachineCoreSettings.ShowInGameGuides);

            SaveDuringPlay.SaveDuringPlay.Enabled
                = EditorGUILayout.Toggle(
                    new GUIContent(
                        "Save During Play",
                        "If checked, Virtual Camera settings changes made during Play Mode will be propagated back to the scene when Play Mode is exited."),
                    SaveDuringPlay.SaveDuringPlay.Enabled);

            if (Application.isPlaying && SaveDuringPlay.SaveDuringPlay.Enabled)
                EditorGUILayout.HelpBox(
                    " Virtual Camera settings changes made during Play Mode will be propagated back to the scene when Play Mode is exited.",
                    MessageType.Info);
        }
    }
}
