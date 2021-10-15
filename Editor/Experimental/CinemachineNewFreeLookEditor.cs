﻿#if CINEMACHINE_EXPERIMENTAL_VCAM
using UnityEngine;
using UnityEditor;
using Cinemachine.Editor;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Cinemachine
{
    [CustomEditor(typeof(CinemachineNewFreeLook))]
    sealed class CinemachineNewFreeLookEditor
        : CinemachineVirtualCameraBaseEditor<CinemachineNewFreeLook>, ISceneToolAware
    {
        GUIContent[] m_OrbitNames = new GUIContent[]
            { new GUIContent("Top Rig"), new GUIContent("Main Rig"), new GUIContent("Bottom Rig") };

        GUIContent m_CustomizeLabel = new GUIContent(
            "Customize", "Custom settings for this rig.  If unchecked, main rig settins will be used");

        VcamStageEditorPipeline m_PipelineSet = new VcamStageEditorPipeline();

        /// <summary>Get the property names to exclude in the inspector.</summary>
        /// <param name="excluded">Add the names to this list</param>
        protected override void GetExcludedPropertiesInInspector(List<string> excluded)
        {
            base.GetExcludedPropertiesInInspector(excluded);
            excluded.Add(FieldPath(x => x.m_Rigs)); // can't use HideInInspector for this
            excluded.Add(FieldPath(x => x.m_Orbits));
            excluded.Add(FieldPath(x => x.m_SplineCurvature));
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            Undo.undoRedoPerformed += ResetTargetOnUndo;
            m_PipelineSet.Initialize(
                // GetComponent
                (stage, result) =>
                {
                    int numNullComponents = 0;
                    foreach (var obj in targets)
                    {
                        var vcam = obj as CinemachineNewVirtualCamera;
                        if (vcam != null)
                        {
                            var c = vcam.GetCinemachineComponent(stage);
                            if (c != null)
                                result.Add(c);
                            else
                                ++numNullComponents;
                        }
                    }
                    return numNullComponents;
                },
                // SetComponent
                (stage, type) => 
                {
                    Undo.SetCurrentGroupName("Cinemachine pipeline change");
                    foreach (var obj in targets)
                    {
                        var vcam = obj as CinemachineNewVirtualCamera;
                        if (vcam != null)
                        {
                            Component c = vcam.GetCinemachineComponent(stage);
                            if (c != null && c.GetType() == type)
                                continue;
                            if (c != null)
                            {
                                Undo.DestroyObjectImmediate(c);
                                vcam.InvalidateComponentCache();
                            }
                            if (type != null)
                            {
                                Undo.AddComponent(vcam.gameObject, type);
                                vcam.InvalidateComponentCache();
                            }
                        }
                    }
                });

            m_PipelineSet.SetStageIsLocked(CinemachineCore.Stage.Body);

            for (int i = 0; i < targets.Length; ++i)
                (targets[i] as CinemachineNewFreeLook).UpdateInputAxisProvider();
            
#if UNITY_2021_2_OR_NEWER
            CinemachineSceneToolUtility.RegisterTool(typeof(SoloVcamTool));
            CinemachineSceneToolUtility.RegisterTool(typeof(FoVTool));
            CinemachineSceneToolUtility.RegisterTool(typeof(FarNearClipTool));
            CinemachineSceneToolUtility.RegisterTool(typeof(FollowOffsetTool));
#endif
        }

        protected override void OnDisable()
        {
            m_PipelineSet.Shutdown();
            base.OnDisable();
            
#if UNITY_2021_2_OR_NEWER
            CinemachineSceneToolUtility.UnregisterTool(typeof(SoloVcamTool));
            CinemachineSceneToolUtility.UnregisterTool(typeof(FoVTool));
            CinemachineSceneToolUtility.UnregisterTool(typeof(FarNearClipTool));
            CinemachineSceneToolUtility.UnregisterTool(typeof(FollowOffsetTool));
#endif
        }

        void ResetTargetOnUndo() 
        {
            ResetTarget();
        }

        public override void OnInspectorGUI()
        {
            // Ordinary properties
            BeginInspector();
            DrawHeaderInInspector();
            DrawPropertyInInspector(FindProperty(x => x.m_Priority));
            DrawTargetsInInspector(FindProperty(x => x.m_Follow), FindProperty(x => x.m_LookAt));
            DrawPropertyInInspector(FindProperty(x => x.m_StandbyUpdate));
            DrawLensSettingsInInspector(FindProperty(x => x.m_Lens));
            DrawRemainingPropertiesInInspector();

            // Orbits
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Orbits", EditorStyles.boldLabel);
            SerializedProperty orbits = FindProperty(x => x.m_Orbits);
            EditorGUI.BeginChangeCheck();
            for (int i = 0; i < 3; ++i)
            {
                var o = orbits.GetArrayElementAtIndex(i);
                Rect rect = EditorGUILayout.GetControlRect(true);
                InspectorUtility.MultiPropertyOnLine(
                    rect, m_OrbitNames[i],
                    new [] { o.FindPropertyRelative(() => Target.m_Orbits[i].m_Height),
                            o.FindPropertyRelative(() => Target.m_Orbits[i].m_Radius) },
                    null);
            }
            EditorGUILayout.PropertyField(FindProperty(x => x.m_SplineCurvature));
            if (EditorGUI.EndChangeCheck())
                serializedObject.ApplyModifiedProperties();

            // Pipeline Stages
            EditorGUILayout.Space();
            var selectedRig = Selection.objects.Length == 1 
                ? GUILayout.Toolbar(s_SelectedRig, s_RigNames) : 0;
            if (selectedRig != s_SelectedRig)
            {
                Undo.RecordObject(Target, "selected rig");
                Target.m_VerticalAxis.Value = selectedRig == 0 ? 1 : (selectedRig == 1 ? 0.5f : 0);
            }
            s_SelectedRig = selectedRig;
            EditorGUILayout.BeginVertical(GUI.skin.box);
            if (selectedRig == 1)
                m_PipelineSet.OnInspectorGUI(false);
            else
                DrawRigEditor(selectedRig == 0 ? 0 : 1);
            EditorGUILayout.EndVertical();

            // Extensions
            DrawExtensionsWidgetInInspector();
        }

        static GUIContent[] s_RigNames = 
        {
            new GUIContent("Top Rig"), 
            new GUIContent("Main Rig"), 
            new GUIContent("Bottom Rig")
        };
        static int s_SelectedRig = 1;

        void OnSceneGUI()
        {
            // TODO: newFreelook does not have rigeditor, its different check how it works 
            // if (m_rigEditor != null && m_rigEditor is ISceneToolAware sceneToolAware)
            // {
            //     sceneToolAware.DrawSceneToolsOnSceneGUI();
            // }
            ((ISceneToolAware)this).DrawSceneToolsOnSceneGUI(); // call default implementation
        }

#if UNITY_2021_2_OR_NEWER
        float m_Fov; // needed for reversing the scale slider
        public void DrawSceneTools()
        {
            var newFreelook = Target;
            if (newFreelook == null || !newFreelook.IsValid)
            {
                return;
            }

            var originalColor = Handles.color;
            Handles.color = Handles.preselectionColor;
            if (CinemachineSceneToolUtility.IsToolActive(typeof(FoVTool)))
            {
                if (GUIUtility.hotControl == 0)
                {
                    m_Fov = Target.m_Lens.Orthographic ? Target.m_Lens.OrthographicSize : Target.m_Lens.FieldOfView;
                }
                
                CinemachineSceneToolHelpers.FovToolHandle(newFreelook, ref newFreelook.m_Lens, 
                    m_LensSettingsInspectorHelper == null ? false : m_LensSettingsInspectorHelper.UseHorizontalFOV, 
                    ref m_Fov);
            }
            else if (/*newFreelook.m_CommonLens && */CinemachineSceneToolUtility.IsToolActive(typeof(FarNearClipTool)))
            {
                CinemachineSceneToolHelpers.NearFarClipHandle(newFreelook, ref newFreelook.m_Lens);
            }
            else if (newFreelook.Follow != null && 
                CinemachineSceneToolUtility.IsToolActive(typeof(FollowOffsetTool)))
            {
                var followPos = newFreelook.Follow.position;
                for (var i = 0; i < newFreelook.m_Orbits.Length; ++i)
                {
                    EditorGUI.BeginChangeCheck();
                
                    var heightHandleId = GUIUtility.GetControlID(FocusType.Passive);
                    var heightHandlePos = followPos + Vector3.up * newFreelook.m_Orbits[i].m_Height;
                    var newHeightHandlePos = Handles.Slider(heightHandleId, heightHandlePos, Vector3.up,
                        HandleUtility.GetHandleSize(heightHandlePos) / 10f, Handles.CubeHandleCap, 0.5f);

                    if (EditorGUI.EndChangeCheck())
                    {
                        Undo.RecordObject(newFreelook, "Changed freelook rig orbit height using handle in scene view.");
                
                        newFreelook.m_Orbits[i].m_Height += CinemachineSceneToolHelpers.SliderHandleDelta(
                            newHeightHandlePos, heightHandlePos, Vector3.up);

                        InspectorUtility.RepaintGameView();
                    }
                }

                // TODO: when rig selection becomes an option in freelook, then this won't be needed!
                var dirs = new[] { Vector3.forward, Vector3.right, Vector3.back, Vector3.left };
                for (var i = 0; i < newFreelook.m_Orbits.Length; ++i)
                {
                    for (int d = 0; d < 4; ++d)
                    {
                        EditorGUI.BeginChangeCheck();
                        var radiusHandleId = GUIUtility.GetControlID(FocusType.Passive);
                        var radiusHandlePos = followPos + Vector3.up * newFreelook.m_Orbits[i].m_Height 
                            + dirs[d] * newFreelook.m_Orbits[i].m_Radius;
                        var newRadiusHandlePos = Handles.Slider(radiusHandleId, radiusHandlePos, dirs[d],
                            HandleUtility.GetHandleSize(radiusHandlePos) / 10f, Handles.CubeHandleCap, 0.5f);
                    
                        if (EditorGUI.EndChangeCheck())
                        {
                            Undo.RecordObject(newFreelook, "Changed freelook rig orbit radius using handle in scene view.");
                
                            newFreelook.m_Orbits[i].m_Radius += CinemachineSceneToolHelpers.SliderHandleDelta(
                                newRadiusHandlePos, radiusHandlePos, dirs[d]);

                            InspectorUtility.RepaintGameView();
                        }
                    }
                }
            }
            Handles.color = originalColor;
        }
#endif

        void DrawRigEditor(int rigIndex)
        {
            const float kBoxMargin = 3;

            SerializedProperty rig = FindProperty(x => x.m_Rigs).GetArrayElementAtIndex(rigIndex);

            CinemachineNewFreeLook.Rig def = new CinemachineNewFreeLook.Rig(); // for properties
            EditorGUIUtility.labelWidth -= kBoxMargin;

            ++EditorGUI.indentLevel;
            var components = Target.ComponentCache;
            if (DrawFoldoutPropertyWithEnabledCheckbox(
                rig.FindPropertyRelative(() => def.m_CustomLens),
                rig.FindPropertyRelative(() => def.m_Lens)))
            {
                Target.m_Rigs[rigIndex].m_Lens = Target.m_Lens;
            }

            int index = (int)CinemachineCore.Stage.Body;
            if (components[index] is CinemachineTransposer)
            {
                if (DrawFoldoutPropertyWithEnabledCheckbox(
                    rig.FindPropertyRelative(() => def.m_CustomBody),
                    rig.FindPropertyRelative(() => def.m_Body)))
                {
                    Target.m_Rigs[rigIndex].m_Body.PullFrom(
                        components[index] as CinemachineTransposer);
                }
            }

            index = (int)CinemachineCore.Stage.Aim;
            if (components[index] is CinemachineComposer)
            {
                if (DrawFoldoutPropertyWithEnabledCheckbox(
                    rig.FindPropertyRelative(() => def.m_CustomAim),
                    rig.FindPropertyRelative(() => def.m_Aim)))
                {
                    Target.m_Rigs[rigIndex].m_Aim.PullFrom(
                        components[index] as CinemachineComposer);
                }
            }

            index = (int)CinemachineCore.Stage.Noise;
            if (components[index] is CinemachineBasicMultiChannelPerlin)
            {
                if (DrawFoldoutPropertyWithEnabledCheckbox(
                    rig.FindPropertyRelative(() => def.m_CustomNoise),
                    rig.FindPropertyRelative(() => def.m_Noise)))
                {
                    Target.m_Rigs[rigIndex].m_Noise.PullFrom(
                        components[index] as CinemachineBasicMultiChannelPerlin);
                }
            }
            --EditorGUI.indentLevel;
            EditorGUIUtility.labelWidth += kBoxMargin;
        }

        // Returns true if default value should be applied
        bool DrawFoldoutPropertyWithEnabledCheckbox(
            SerializedProperty enabledProperty, SerializedProperty property)
        {
            GUIContent label = new GUIContent(property.displayName, property.tooltip);
            Rect rect = EditorGUILayout.GetControlRect(true,
                (enabledProperty.boolValue && property.isExpanded)
                    ? EditorGUI.GetPropertyHeight(property)
                        : EditorGUIUtility.singleLineHeight);
            Rect r = rect; r.height = EditorGUIUtility.singleLineHeight;
            if (!enabledProperty.boolValue)
                EditorGUI.LabelField(r, label);

            float labelWidth = EditorGUIUtility.labelWidth;
            bool newValue = EditorGUI.ToggleLeft(
                new Rect(labelWidth, r.y, r.width - labelWidth, r.height),
                m_CustomizeLabel, enabledProperty.boolValue);
            if (newValue != enabledProperty.boolValue)
            {
                enabledProperty.boolValue = newValue;
                enabledProperty.serializedObject.ApplyModifiedProperties();
                property.isExpanded = newValue;
                return true;
            }
            if (newValue == true)
            {
                EditorGUI.BeginChangeCheck();
                EditorGUI.PropertyField(rect, property, property.isExpanded);
                if (EditorGUI.EndChangeCheck())
                    enabledProperty.serializedObject.ApplyModifiedProperties();
            }
            return false;
        }

        [DrawGizmo(GizmoType.Active | GizmoType.Selected, typeof(CinemachineNewFreeLook))]
        private static void DrawFreeLookGizmos(CinemachineNewFreeLook vcam, GizmoType selectionType)
        {
            // Standard frustum and logo
            CinemachineBrainEditor.DrawVirtualCameraBaseGizmos(vcam, selectionType);

            Color originalGizmoColour = Gizmos.color;
            bool isActiveVirtualCam = CinemachineCore.Instance.IsLive(vcam);
            Gizmos.color = isActiveVirtualCam
                ? CinemachineSettings.CinemachineCoreSettings.ActiveGizmoColour
                : CinemachineSettings.CinemachineCoreSettings.InactiveGizmoColour;

            if (vcam.Follow != null)
            {
                Vector3 pos = vcam.Follow.position;
                Vector3 up = Vector3.up;
                CinemachineBrain brain = CinemachineCore.Instance.FindPotentialTargetBrain(vcam);
                if (brain != null)
                    up = brain.DefaultWorldUp;

                var middleRig = vcam.GetComponent<CinemachineTransposer>();
                if (middleRig != null)
                {
                    float scale = vcam.m_RadialAxis.Value;
                    Quaternion orient = middleRig.GetReferenceOrientation(up);
                    up = orient * Vector3.up;
                    var orbital = middleRig as CinemachineOrbitalTransposer;
                    if (orbital != null)
                    {
                        float rotation = orbital.m_XAxis.Value + orbital.m_Heading.m_Bias;
                        orient = Quaternion.AngleAxis(rotation, up) * orient;
                    }
                    CinemachineOrbitalTransposerEditor.DrawCircleAtPointWithRadius(
                        pos + up * vcam.m_Orbits[0].m_Height * scale,
                        orient, vcam.m_Orbits[0].m_Radius * scale);
                    CinemachineOrbitalTransposerEditor.DrawCircleAtPointWithRadius(
                        pos + up * vcam.m_Orbits[1].m_Height * scale,
                        orient, vcam.m_Orbits[1].m_Radius * scale);
                    CinemachineOrbitalTransposerEditor.DrawCircleAtPointWithRadius(
                        pos + up * vcam.m_Orbits[2].m_Height * scale,
                        orient, vcam.m_Orbits[2].m_Radius * scale);

                    DrawCameraPath(pos, orient, vcam);
                }
            }
            Gizmos.color = originalGizmoColour;
        }

        private static void DrawCameraPath(
            Vector3 atPos, Quaternion orient, CinemachineNewFreeLook vcam)
        {
            Matrix4x4 prevMatrix = Gizmos.matrix;
            Gizmos.matrix = Matrix4x4.TRS(atPos, orient, Vector3.one);

            const int kNumSteps = 20;
            Vector3 currPos = vcam.GetLocalPositionForCameraFromInput(0f);
            for (int i = 1; i < kNumSteps + 1; ++i)
            {
                float t = (float)i / (float)kNumSteps;
                Vector3 nextPos = vcam.GetLocalPositionForCameraFromInput(t);
                Gizmos.DrawLine(currPos, nextPos);
                currPos = nextPos;
            }
            Gizmos.matrix = prevMatrix;
        }
    }
}
#endif
