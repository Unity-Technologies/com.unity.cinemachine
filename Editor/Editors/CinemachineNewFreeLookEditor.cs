using UnityEngine;
using UnityEditor;
using Cinemachine.Editor;
using System.Collections.Generic;
using System;

namespace Cinemachine
{
    [CustomEditor(typeof(CinemachineNewFreeLook))]
    sealed class CinemachineNewFreeLookEditor 
        : CinemachineVirtualCameraBaseEditor<CinemachineNewFreeLook>
    {
        GUIContent[] mRigNames = new GUIContent[] 
            { new GUIContent("Main Rig"), new GUIContent("Top Rig"), new GUIContent("Bottom Rig") };

        GUIContent mAllLensLabel = new GUIContent("Customize", "If unchecked, lens definition for the middle rig will be used");

        class RigEditor
        {
            int mRigIndex;
            public CinemachineStageEditor[] mStageEditors =
                new CinemachineStageEditor[Enum.GetValues(typeof(CinemachineCore.Stage)).Length];

            public RigEditor(CinemachineNewFreeLook target, int rigIndex)
            {
                mRigIndex = rigIndex;
                foreach (CinemachineCore.Stage stage in Enum.GetValues(typeof(CinemachineCore.Stage)))
                {
                    var ed = mStageEditors[(int)stage] = new CinemachineStageEditor(stage, target.gameObject);
                    ed.SetComponent = (type) 
                        => {
                            var freeLook = ed.Target.GetComponent<CinemachineNewFreeLook>();
                            freeLook.SetRigComponent(mRigIndex, type);
                        };
                    ed.DestroyComponent = (component) 
                        => {
                            var freeLook = ed.Target.GetComponent<CinemachineNewFreeLook>();
                            freeLook.DestroyRigComponent(component);
                        };
                }
            }

            public void OnDisable()
            {
                foreach (CinemachineCore.Stage stage in Enum.GetValues(typeof(CinemachineCore.Stage)))
                    mStageEditors[(int)stage].Shutdown();
            }
        }
        RigEditor[] mRigEditors = new RigEditor[3];

        protected override List<string> GetExcludedPropertiesInInspector()
        {
            List<string> excluded = base.GetExcludedPropertiesInInspector();
            if (Target.m_BindingMode == CinemachineTransposer.BindingMode.SimpleFollowWithWorldUp)
            {
                excluded.Add(FieldPath(x => x.m_Heading));
                excluded.Add(FieldPath(x => x.m_HorizontalRecentering));
            }
            return excluded;
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            for (int i = 0; i < mRigEditors.Length; ++i)
                mRigEditors[i] = new RigEditor(Target, i);
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            for (int i = 0; i < mRigEditors.Length; ++i)
                mRigEditors[i].OnDisable();
        }

        public override void OnInspectorGUI()
        {
            Target.m_HorizontalAxis.ValueRangeLocked 
                = (Target.m_BindingMode == CinemachineTransposer.BindingMode.SimpleFollowWithWorldUp);

            // Ordinary properties
            BeginInspector();
            DrawHeaderInInspector();
            DrawPropertyInInspector(FindProperty(x => x.m_Priority));
            DrawTargetsInInspector(FindProperty(x => x.m_Follow), FindProperty(x => x.m_LookAt));
            DrawRemainingPropertiesInInspector();

            // Rigs
            SerializedProperty rigs = FindProperty(x => x.m_Rigs);
            for (int i = 0; i < 3; ++i)
            {
                EditorGUILayout.Separator();
                DrawRigEditor(i, rigs.GetArrayElementAtIndex(i));
            }
            
            // Extensions
            DrawExtensionsWidgetInInspector();
        }

        void DrawRigEditor(int rigIndex, SerializedProperty rig)
        {
            CinemachineNewFreeLook.Rig def = new CinemachineNewFreeLook.Rig(); // for properties
            EditorGUILayout.BeginVertical(GUI.skin.box);

            // Orbit
            EditorGUI.BeginChangeCheck();
            Rect rect = EditorGUILayout.GetControlRect(true);
            InspectorUtility.MultiPropertyOnLine(rect, 
                new GUIContent(mRigNames[rigIndex]),
                new [] { rig.FindPropertyRelative(() => def.m_Height), 
                        rig.FindPropertyRelative(() => def.m_Radius) },
                null);
            if (EditorGUI.EndChangeCheck())
                serializedObject.ApplyModifiedProperties();

            ++EditorGUI.indentLevel;

            // Lens
            SerializedProperty customLens = rig.FindPropertyRelative(() => def.m_CustomLens);
            Rect rLensLabel = EditorGUILayout.GetControlRect(true, 0); rLensLabel.y += 2;
            if (rigIndex == 0 || customLens.boolValue)
            {
                EditorGUI.BeginChangeCheck();
                EditorGUILayout.PropertyField(rig.FindPropertyRelative(() => def.m_Lens));
                if (EditorGUI.EndChangeCheck())
                    serializedObject.ApplyModifiedProperties();
            }
            if (rigIndex != 0)
            {
                if (!customLens.boolValue)
                {
                    rLensLabel = EditorGUILayout.GetControlRect(true, EditorGUIUtility.singleLineHeight + 2);
                    EditorGUI.LabelField(rLensLabel, "Lens");
                }
                const float indentOffset = 3; // GML wtf get rid of this
                rLensLabel.height = EditorGUIUtility.singleLineHeight;
                float labelWidth = EditorGUIUtility.labelWidth + indentOffset;
                rLensLabel.x = labelWidth;
                rLensLabel.width -= labelWidth;
                bool newValue = EditorGUI.ToggleLeft(rLensLabel, mAllLensLabel, customLens.boolValue);
                if (newValue != customLens.boolValue)
                {
                    customLens.boolValue = newValue;
                    serializedObject.ApplyModifiedProperties();
                    if (newValue == true)
                        Target.m_Rigs[rigIndex].m_Lens = Target.m_Rigs[0].m_Lens;
                }
            }
            
            // Pipeline Stages
            foreach (CinemachineCore.Stage stage in Enum.GetValues(typeof(CinemachineCore.Stage)))
            {
                CinemachineComponentBase oldC = Target.GetRigComponent(rigIndex, stage);
                if (rigIndex > 0 && oldC == null && Target.GetRigComponent(0, stage) == null)
                    continue;

                int stageIndex = (int)stage;
                var ed = mRigEditors[rigIndex].mStageEditors[stageIndex];
                if (!ed.HasImplementation)
                    continue;
                if (stage == CinemachineCore.Stage.Body)
                    ed.TypeIsLocked = true;
                ed.ShowDisabledCheckbox = (rigIndex != 0);

                bool componentWasNull = rigIndex != 0 && oldC == null;
                ed.ComponentSelectionDisabled = componentWasNull;
                ed.OnInspectorGUI(oldC); // may destroy oldC
                if (componentWasNull && !ed.ComponentSelectionDisabled)
                {
                    // Just enabled "Customize" - copy from rig 0 if it exists
                    oldC = Target.GetRigComponent(0, stage);
                    if (oldC != null)
                    {
                        var newC = Target.SetRigComponent(rigIndex, oldC.GetType());
                        EditorUtility.CopySerialized(oldC, newC);
                        newC.m_RigIndex = rigIndex;  // set it back because CopySerialized stomped it
                        GUIUtility.ExitGUI();
                    }
                }
                if (!componentWasNull && ed.ComponentSelectionDisabled)
                {
                    // Just disabled "Customize" - delete the component
                    Target.DestroyRigComponent(rigIndex, stage);
                    GUIUtility.ExitGUI();
                }
            }

            --EditorGUI.indentLevel;
            EditorGUILayout.EndVertical();
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

                var MiddleRig = vcam.GetComponent<CinemachineOrbitalTransposer>();
                Quaternion orient = MiddleRig.GetReferenceOrientation(up);
                up = orient * Vector3.up;
                float rotation = vcam.m_HorizontalAxis.Value + vcam.m_Heading.m_Bias;
                orient = Quaternion.AngleAxis(rotation, up) * orient;

                CinemachineOrbitalTransposerEditor.DrawCircleAtPointWithRadius(
                    pos + up * vcam.m_Rigs[0].m_Height, orient, vcam.m_Rigs[0].m_Radius);
                CinemachineOrbitalTransposerEditor.DrawCircleAtPointWithRadius(
                    pos + up * vcam.m_Rigs[1].m_Height, orient, vcam.m_Rigs[1].m_Radius);
                CinemachineOrbitalTransposerEditor.DrawCircleAtPointWithRadius(
                    pos + up * vcam.m_Rigs[2].m_Height, orient, vcam.m_Rigs[2].m_Radius);

                DrawCameraPath(pos, orient, vcam);
            }

            Gizmos.color = originalGizmoColour;
        }

        private static void DrawCameraPath(Vector3 atPos, Quaternion orient, CinemachineNewFreeLook vcam)
        {
            Matrix4x4 prevMatrix = Gizmos.matrix;
            Gizmos.matrix = Matrix4x4.TRS(atPos, orient, Vector3.one);

            const int kNumStepsPerPair = 30;
            Vector3 currPos = vcam.GetLocalPositionForCameraFromInput(0f);
            for (int i = 1; i < kNumStepsPerPair + 1; ++i)
            {
                float t = (float)i / (float)kNumStepsPerPair;
                Vector3 nextPos = vcam.GetLocalPositionForCameraFromInput(t);
                Gizmos.DrawLine(currPos, nextPos);
                currPos = nextPos;
            }
            Gizmos.matrix = prevMatrix;
        }
    }
}
