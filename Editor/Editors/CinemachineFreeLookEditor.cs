using UnityEngine;
using UnityEditor;
using Cinemachine.Editor;
using System.Collections.Generic;
using System.Numerics;
using Cinemachine.Utility;
using Matrix4x4 = UnityEngine.Matrix4x4;
using Quaternion = UnityEngine.Quaternion;
using Vector3 = UnityEngine.Vector3;

namespace Cinemachine
{
    [CustomEditor(typeof(CinemachineFreeLook))]
    [CanEditMultipleObjects]
    internal sealed class CinemachineFreeLookEditor
        : CinemachineVirtualCameraBaseEditor<CinemachineFreeLook>
    {
        /// <summary>Get the property names to exclude in the inspector.</summary>
        /// <param name="excluded">Add the names to this list</param>
        protected override void GetExcludedPropertiesInInspector(List<string> excluded)
        {
            base.GetExcludedPropertiesInInspector(excluded);
            excluded.Add(FieldPath(x => x.m_Orbits));
            if (!Target.m_CommonLens)
                excluded.Add(FieldPath(x => x.m_Lens));
            if (Target.m_BindingMode == CinemachineTransposer.BindingMode.SimpleFollowWithWorldUp)
            {
                excluded.Add(FieldPath(x => x.m_Heading));
                excluded.Add(FieldPath(x => x.m_RecenterToTargetHeading));
            }
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            Target.UpdateInputAxisProvider();
            
#if UNITY_2021_2_OR_NEWER
            CinemachineSceneToolUtility.RegisterTool(typeof(SoloVcamTool));
            CinemachineSceneToolUtility.RegisterTool(typeof(FoVTool));
            CinemachineSceneToolUtility.RegisterTool(typeof(FarNearClipTool));
            CinemachineSceneToolUtility.RegisterTool(typeof(FollowOffsetTool));
#endif
        }
        
        protected override void OnDisable()
        {
            base.OnDisable();

            // Must destroy child editors or we get exceptions
            if (m_rigEditor != null)
                UnityEngine.Object.DestroyImmediate(m_rigEditor);
        
#if UNITY_2021_2_OR_NEWER
            CinemachineSceneToolUtility.UnregisterTool(typeof(SoloVcamTool));
            CinemachineSceneToolUtility.UnregisterTool(typeof(FoVTool));
            CinemachineSceneToolUtility.UnregisterTool(typeof(FarNearClipTool));
            CinemachineSceneToolUtility.UnregisterTool(typeof(FollowOffsetTool));
#endif
        }

        public override void OnInspectorGUI()
        {
            Target.m_XAxis.ValueRangeLocked
                = (Target.m_BindingMode == CinemachineTransposer.BindingMode.SimpleFollowWithWorldUp);

            // Ordinary properties
            BeginInspector();
            DrawHeaderInInspector();
            DrawPropertyInInspector(FindProperty(x => x.m_Priority));
            DrawTargetsInInspector(FindProperty(x => x.m_Follow), FindProperty(x => x.m_LookAt));
            DrawPropertyInInspector(FindProperty(x => x.m_StandbyUpdate));
            DrawPropertyInInspector(FindProperty(x => x.m_CommonLens));
            DrawLensSettingsInInspector(FindProperty(x => x.m_Lens));
            DrawRemainingPropertiesInInspector();

            // Orbits
            EditorGUI.BeginChangeCheck();
            SerializedProperty orbits = FindProperty(x => x.m_Orbits);
            for (int i = 0; i < CinemachineFreeLook.RigNames.Length; ++i)
            {
                Rect rect = EditorGUILayout.GetControlRect(true);
                SerializedProperty orbit = orbits.GetArrayElementAtIndex(i);
                InspectorUtility.MultiPropertyOnLine(rect,
                    new GUIContent(CinemachineFreeLook.RigNames[i]),
                    new [] { orbit.FindPropertyRelative(() => Target.m_Orbits[i].m_Height),
                            orbit.FindPropertyRelative(() => Target.m_Orbits[i].m_Radius) },
                    null);
            }
            if (EditorGUI.EndChangeCheck())
                serializedObject.ApplyModifiedProperties();

            // Rigs
            if (Selection.objects.Length == 1)
            {
                EditorGUILayout.Separator();
                EditorGUILayout.Separator();
                s_SelectedRig = GUILayout.Toolbar(s_SelectedRig, s_RigNames);
                UpdateRigEditor();
                if (m_rigEditor != null)
                {
                    EditorGUILayout.BeginVertical(GUI.skin.box);
                    ++EditorGUI.indentLevel;
                    m_rigEditor.OnInspectorGUI();
                    --EditorGUI.indentLevel;
                    EditorGUILayout.EndVertical();
                }
            }
            
            // Extensions
            DrawExtensionsWidgetInInspector();
        }
        
        protected override void OnSceneGUI()
        {
            base.OnSceneGUI();
            if (m_rigEditor != null)
            {
                var mi = m_rigEditor.GetType().GetMethod("OnSceneGUI", 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (mi != null && m_rigEditor.target != null)
                {
                    mi.Invoke(m_rigEditor, null);
                }
            }
        }

#if UNITY_2021_2_OR_NEWER
        protected override void DrawSceneTools()
        {
            var freelook = Target;
            if (!freelook.IsValid || !freelook.m_CommonLens)
            {
                return;
            }

            var handleIsUsed = GUIUtility.hotControl > 0;
            var originalColor = Handles.color;
            Handles.color = handleIsUsed ? Handles.selectedColor : Handles.preselectionColor;
            if (CinemachineSceneToolUtility.IsToolActive(typeof(FoVTool)))
            {
                CinemachineSceneToolHelpers.FovToolHandle(freelook, ref freelook.m_Lens, 
                    m_LensSettingsInspectorHelper == null ? false : m_LensSettingsInspectorHelper.UseHorizontalFOV);
            }
            else if (CinemachineSceneToolUtility.IsToolActive(typeof(FarNearClipTool)))
            {
                CinemachineSceneToolHelpers.NearFarClipHandle(freelook, ref freelook.m_Lens);
            }
            else if (CinemachineSceneToolUtility.IsToolActive(typeof(FollowOffsetTool)))
            {
                var followPos = freelook.Follow.position;
                for (var i = 0; i < freelook.m_Orbits.Length; ++i)
                {
                    EditorGUI.BeginChangeCheck();
                
                    var heightHandleId = GUIUtility.GetControlID(FocusType.Passive);
                    var heightHandlePos = followPos + Vector3.up * freelook.m_Orbits[i].m_Height;
                    var newHeightHandlePos = Handles.Slider(heightHandleId, heightHandlePos, Vector3.up,
                        HandleUtility.GetHandleSize(heightHandlePos) / 10f, Handles.CubeHandleCap, 0.5f);

                    if (EditorGUI.EndChangeCheck())
                    {
                        Undo.RecordObject(freelook, "Changed freelook rig orbit height using handle in scene view.");
                
                        freelook.m_Orbits[i].m_Height += CinemachineSceneToolHelpers.SliderHandleDelta(
                            newHeightHandlePos, heightHandlePos, Vector3.up);

                        InspectorUtility.RepaintGameView();
                    }
                }

                // TODO: when rig selection becomes an option in freelook, then this won't be needed!
                var dirs = new[] { Vector3.forward, Vector3.right, Vector3.back, Vector3.left };
                for (var i = 0; i < freelook.m_Orbits.Length; ++i)
                {
                    for (int d = 0; d < 4; ++d)
                    {
                        EditorGUI.BeginChangeCheck();
                        var radiusHandleId = GUIUtility.GetControlID(FocusType.Passive);
                        var radiusHandlePos = followPos + Vector3.up * freelook.m_Orbits[i].m_Height 
                            + dirs[d] * freelook.m_Orbits[i].m_Radius;
                        var newRadiusHandlePos = Handles.Slider(radiusHandleId, radiusHandlePos, dirs[d],
                            HandleUtility.GetHandleSize(radiusHandlePos) / 10f, Handles.CubeHandleCap, 0.5f);
                    
                        if (EditorGUI.EndChangeCheck())
                        {
                            Undo.RecordObject(freelook, "Changed freelook rig orbit radius using handle in scene view.");
                
                            freelook.m_Orbits[i].m_Radius += CinemachineSceneToolHelpers.SliderHandleDelta(
                                newRadiusHandlePos, radiusHandlePos, dirs[d]);

                            InspectorUtility.RepaintGameView();
                        }
                    }
                }
            }
            Handles.color = originalColor;
        }
#endif
        
        static GUIContent[] s_RigNames = 
        {
            new GUIContent("Top Rig"), 
            new GUIContent("Middle Rig"), 
            new GUIContent("Bottom Rig")
        };
        static int s_SelectedRig = 1;

        UnityEditor.Editor m_rigEditor;
        CinemachineVirtualCameraBase m_EditedRig = null;

        void UpdateRigEditor()
        {
            CinemachineVirtualCamera rig = Target.GetRig(s_SelectedRig);
            if (m_EditedRig != rig || m_rigEditor == null)
            {
                m_EditedRig = rig;
                if (m_rigEditor != null)
                {
                    UnityEngine.Object.DestroyImmediate(m_rigEditor);
                    m_rigEditor = null;
                }
                if (rig != null)
                {
                    Undo.RecordObject(Target, "selected rig");
                    Target.m_YAxis.Value = s_SelectedRig == 0 ? 1 : (s_SelectedRig == 1 ? 0.5f : 0);
                    CreateCachedEditor(rig, null, ref m_rigEditor);
                }
            }
        }

        /// <summary>
        /// Register with CinemachineFreeLook to create the pipeline in an undo-friendly manner
        /// </summary>
        [InitializeOnLoad]
        class CreateRigWithUndo
        {
            static CreateRigWithUndo()
            {
                CinemachineFreeLook.CreateRigOverride
                    = (CinemachineFreeLook vcam, string name, CinemachineVirtualCamera copyFrom) =>
                    {
                        // Create a new rig with default components
                        GameObject go = InspectorUtility.CreateGameObject(name);
                        Undo.RegisterCreatedObjectUndo(go, "created rig");
                        Undo.SetTransformParent(go.transform, vcam.transform, "parenting rig");
                        CinemachineVirtualCamera rig = Undo.AddComponent<CinemachineVirtualCamera>(go);
                        Undo.RecordObject(rig, "creating rig");
                        if (copyFrom != null)
                            ReflectionHelpers.CopyFields(copyFrom, rig);
                        else
                        {
                            go = rig.GetComponentOwner().gameObject;
                            Undo.RecordObject(Undo.AddComponent<CinemachineOrbitalTransposer>(go), "creating rig");
                            Undo.RecordObject(Undo.AddComponent<CinemachineComposer>(go), "creating rig");
                        }
                        return rig;
                    };
                CinemachineFreeLook.DestroyRigOverride = (GameObject rig) =>
                    {
                        Undo.DestroyObjectImmediate(rig);
                    };
            }
        }

        [DrawGizmo(GizmoType.Active | GizmoType.Selected, typeof(CinemachineFreeLook))]
        private static void DrawFreeLookGizmos(CinemachineFreeLook vcam, GizmoType selectionType)
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
                Vector3 up = vcam.State.ReferenceUp;

                var MiddleRig = vcam.GetRig(1).GetCinemachineComponent<CinemachineOrbitalTransposer>();
                if (MiddleRig != null)
                {
                    Quaternion orient = MiddleRig.GetReferenceOrientation(up);
                    up = orient * Vector3.up;
                    float rotation = vcam.m_XAxis.Value + vcam.m_Heading.m_Bias;
                    orient = Quaternion.AngleAxis(rotation, up) * orient;

                    CinemachineOrbitalTransposerEditor.DrawCircleAtPointWithRadius(
                        pos + up * vcam.m_Orbits[0].m_Height, orient, vcam.m_Orbits[0].m_Radius);
                    CinemachineOrbitalTransposerEditor.DrawCircleAtPointWithRadius(
                        pos + up * vcam.m_Orbits[1].m_Height, orient, vcam.m_Orbits[1].m_Radius);
                    CinemachineOrbitalTransposerEditor.DrawCircleAtPointWithRadius(
                        pos + up * vcam.m_Orbits[2].m_Height, orient, vcam.m_Orbits[2].m_Radius);

                    DrawCameraPath(pos, orient, vcam);
                }
            }

            Gizmos.color = originalGizmoColour;
        }

        private static void DrawCameraPath(Vector3 atPos, Quaternion orient, CinemachineFreeLook vcam)
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
