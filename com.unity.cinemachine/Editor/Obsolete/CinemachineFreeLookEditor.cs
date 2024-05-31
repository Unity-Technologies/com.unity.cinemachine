#if !CINEMACHINE_NO_CM2_SUPPORT
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using Matrix4x4 = UnityEngine.Matrix4x4;
using Quaternion = UnityEngine.Quaternion;
using Vector3 = UnityEngine.Vector3;
using System;
using Unity.Cinemachine.Editor;

namespace Unity.Cinemachine
{
    [Obsolete]
    [CustomEditor(typeof(CinemachineFreeLook))]
    [CanEditMultipleObjects]
    class CinemachineFreeLookEditor : CinemachineVirtualCameraBaseEditor<CinemachineFreeLook>
    {
        /// <summary>Get the property names to exclude in the inspector.</summary>
        /// <param name="excluded">Add the names to this list</param>
        protected override void GetExcludedPropertiesInInspector(List<string> excluded)
        {
            base.GetExcludedPropertiesInInspector(excluded);
            excluded.Add(FieldPath(x => x.m_Orbits));
            if (!Target.m_CommonLens)
                excluded.Add(FieldPath(x => x.m_Lens));
            if (Target.m_BindingMode == TargetTracking.BindingMode.LazyFollow)
            {
                excluded.Add(FieldPath(x => x.m_Heading));
                excluded.Add(FieldPath(x => x.m_RecenterToTargetHeading));
            }
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            Target.UpdateInputAxisProvider();
        }
        
        protected override void OnDisable()
        {
            base.OnDisable();

            // Must destroy child editors or we get exceptions
            if (m_rigEditor != null)
                UnityEngine.Object.DestroyImmediate(m_rigEditor);
        }

        public override void OnInspectorGUI()
        {
            Target.GetRig(0); // force an update of the rig cache
            if (!Target.RigsAreCreated)
            {
                EditorGUILayout.HelpBox(
                    "It's not possible to add this component to a prefab instance.  "
                    + "Instead, you can open the Prefab in Prefab Mode or unpack "
                    + "the Prefab instance to remove the Prefab connection.",
                    MessageType.Error);
                return;
            }
            
            Target.m_XAxis.ValueRangeLocked
                = (Target.m_BindingMode == TargetTracking.BindingMode.LazyFollow);

            // Ordinary properties
            BeginInspector();
            UpgradeManagerInspectorHelpers.IMGUI_DrawUpgradeControls(this, "CinemachineCamera");
            DrawCameraStatusInInspector();
            DrawGlobalControlsInInspector();
            DrawInputProviderButtonInInspector();
            DrawPropertyInInspector(serializedObject.FindProperty(() => Target.Priority));
            DrawPropertyInInspector(serializedObject.FindProperty(() => Target.OutputChannel));
            DrawTargetsInInspector(serializedObject.FindProperty(() => Target.m_Follow), serializedObject.FindProperty(() => Target.m_LookAt));
            DrawPropertyInInspector(serializedObject.FindProperty(() => Target.StandbyUpdate));
            DrawPropertyInInspector(serializedObject.FindProperty(() => Target.m_CommonLens));
            DrawPropertyInInspector(serializedObject.FindProperty(() => Target.m_Lens));
            DrawRemainingPropertiesInInspector();

            // Orbits
            EditorGUI.BeginChangeCheck();
            SerializedProperty orbits = serializedObject.FindProperty(() => Target.m_Orbits);
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
                SetSelectedRig(Target, GUILayout.Toolbar(GetSelectedRig(Target), s_RigNames));
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

        void OnSceneGUI()
        {
            // Forward to embedded rig editor
            if (m_rigEditor != null && m_RigEditorOnSceneGUI != null)
                m_RigEditorOnSceneGUI.Invoke(m_rigEditor, null);
        }

        static GUIContent[] s_RigNames = 
        {
            new GUIContent("Top Rig"), 
            new GUIContent("Middle Rig"), 
            new GUIContent("Bottom Rig")
        };
        internal static GUIContent[] RigNames => s_RigNames;

        static int GetSelectedRig(CinemachineFreeLook freelook)
        {
            return freelook.m_YAxis.Value < 0.33f ? 2 : (freelook.m_YAxis.Value > 0.66f ? 0 : 1);
        }

        internal static void SetSelectedRig(CinemachineFreeLook freelook, int rigIndex)
        {
            Debug.Assert(rigIndex >= 0 && rigIndex < 3);
            if (GetSelectedRig(freelook) != rigIndex)
            {
                var prop = new SerializedObject(freelook).FindProperty(
                    () => freelook.m_YAxis).FindPropertyRelative(() => freelook.m_YAxis.Value);
                prop.floatValue = rigIndex == 0 ? 1 : (rigIndex == 1 ? 0.5f : 0);
                prop.serializedObject.ApplyModifiedProperties();
                freelook.InternalUpdateCameraState(Vector3.up, -1);
            }
        }

        UnityEditor.Editor m_rigEditor;
        System.Reflection.MethodInfo m_RigEditorOnSceneGUI;
        CinemachineVirtualCameraBase m_EditedRig = null;

        void UpdateRigEditor()
        {
            var selectedRig = GetSelectedRig(Target);
            CinemachineVirtualCamera rig = Target.GetRig(selectedRig);
            if (m_EditedRig != rig || m_rigEditor == null)
            {
                m_EditedRig = rig;
                m_RigEditorOnSceneGUI = null;
                if (m_rigEditor != null)
                {
                    UnityEngine.Object.DestroyImmediate(m_rigEditor);
                    m_rigEditor = null;
                }
                if (rig != null)
                {
                    CreateCachedEditor(rig, null, ref m_rigEditor);
                    if (m_rigEditor != null)
                        m_RigEditorOnSceneGUI = m_rigEditor.GetType().GetMethod("OnSceneGUI", 
                            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
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
                    // Recycle the game object if it exists
                    GameObject go = null;
                    foreach (Transform child in vcam.transform)
                    {
                        if (child.gameObject.name == name)
                        {
                            go = child.gameObject;
                            break;
                        }
                    }

                    CinemachineVirtualCamera rig = null;
                    if (go == null)
                    {
                        // Create a new rig - can't do it if prefab instance
                        if (PrefabUtility.IsPartOfAnyPrefab(vcam.gameObject))
                            return null;
                        go =  ObjectFactory.CreateGameObject(name);
                        Undo.RegisterCreatedObjectUndo(go, "created rig");
                        Undo.SetTransformParent(go.transform, vcam.transform, "parenting pipeline");
                    }

                    // Create a new rig with default components
                    rig = Undo.AddComponent<CinemachineVirtualCamera>(go);
                    rig.CreatePipeline(copyFrom);
                    if (copyFrom != null)
                        ReflectionHelpers.CopyFields(copyFrom, rig);
                    else
                    {
                        // Defaults
                        go = rig.GetComponentOwner().gameObject;
                        Undo.AddComponent<CinemachineOrbitalTransposer>(go);
                        Undo.AddComponent<CinemachineComposer>(go);
                        rig.InvalidateComponentPipeline();
                    }
                    return rig;
                };

                CinemachineFreeLook.DestroyRigOverride = (GameObject rig) =>
                {
                    var vcam = rig.GetComponent<CinemachineVirtualCamera>();
                    if (vcam != null)
                    {
                        vcam.DestroyPipeline();
                        Undo.DestroyObjectImmediate(vcam);
                    }
                };
            }
        }

        [DrawGizmo(GizmoType.Active | GizmoType.Selected, typeof(CinemachineFreeLook))]
        private static void DrawFreeLookGizmos(CinemachineFreeLook vcam, GizmoType selectionType)
        {
            // Standard frustum and logo
            CinemachineBrainEditor.DrawVirtualCameraBaseGizmos(vcam, selectionType);

            Color originalGizmoColour = Gizmos.color;
            bool isActiveVirtualCam = CinemachineCore.IsLive(vcam);
            Gizmos.color = isActiveVirtualCam
                ? CinemachineCorePrefs.ActiveGizmoColour.Value
                : CinemachineCorePrefs.InactiveGizmoColour.Value;

            if (vcam.Follow != null)
            {
                var pos = vcam.Follow.position;
                var middleRig = vcam.GetRig(1).GetCinemachineComponent<CinemachineOrbitalTransposer>();
                if (middleRig != null)
                {
                    Quaternion orient = middleRig.GetReferenceOrientation(vcam.State.ReferenceUp);
                    var up = orient * Vector3.up;
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
#endif
