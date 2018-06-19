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

        class RigEditor
        {
            public CinemachineStageEditor[] mStageEditors =
                new CinemachineStageEditor[Enum.GetValues(typeof(CinemachineCore.Stage)).Length];

            public RigEditor(CinemachineNewFreeLook target)
            {
                foreach (CinemachineCore.Stage stage in Enum.GetValues(typeof(CinemachineCore.Stage)))
                    mStageEditors[(int)stage] = new CinemachineStageEditor(stage, target.gameObject);
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
                mRigEditors[i] = new RigEditor(Target);
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
            EditorGUILayout.LabelField(mRigNames[rigIndex], EditorStyles.boldLabel);
            ++EditorGUI.indentLevel;

            // Orbit
            EditorGUI.BeginChangeCheck();
            Rect rect = EditorGUILayout.GetControlRect(true);
            InspectorUtility.MultiPropertyOnLine(rect, 
                new GUIContent("Orbit"),
                new [] { rig.FindPropertyRelative(() => def.m_Height), 
                        rig.FindPropertyRelative(() => def.m_Radius) },
                null);
            if (EditorGUI.EndChangeCheck())
                serializedObject.ApplyModifiedProperties();

            // Lens
            SerializedProperty commonLens = FindProperty(x => x.m_CommonLens);
            Rect rLensLabel = EditorGUILayout.GetControlRect(true, 0);
            if (rigIndex == 0 || !commonLens.boolValue)
            {
                EditorGUI.BeginChangeCheck();
                EditorGUILayout.PropertyField(rig.FindPropertyRelative(() => def.m_Lens));
                if (EditorGUI.EndChangeCheck())
                    serializedObject.ApplyModifiedProperties();
            }
            if (rigIndex == 0)
            {
                rLensLabel.height = EditorGUIUtility.singleLineHeight;
                rLensLabel.y += 1;
                rLensLabel.width = EditorGUIUtility.labelWidth;
                bool value = commonLens.boolValue;
                if (InjectAllRigsToggle(rLensLabel, ref value))
                {
                    commonLens.boolValue = value;
                    serializedObject.ApplyModifiedProperties();
                }
            }
            
            // Pipeline Stages
            SerializedProperty commonStages = FindProperty(x => x.m_CommonStage);
            foreach (CinemachineCore.Stage stage in Enum.GetValues(typeof(CinemachineCore.Stage)))
            {
                int stageIndex = (int)stage;
                if (!mRigEditors[rigIndex].mStageEditors[stageIndex].HasImplementation)
                    continue;
                bool isCommon = commonStages.GetArrayElementAtIndex(stageIndex).boolValue;
                if (rigIndex == 0 || !isCommon)
                {
                    CinemachineComponentBase c = Target.GetRigComponent(rigIndex, stage);
                    CinemachineComponentBase newC 
                        = mRigEditors[rigIndex].mStageEditors[stageIndex].OnInspectorGUI(c);
                    if (c != newC)
                        Target.InvalidateRigCache();
                }
                if (rigIndex == 0)
                {
                    
                    Rect r = mRigEditors[rigIndex].mStageEditors[stageIndex].StageLabelRect;
                    if (InjectAllRigsToggle(r, ref isCommon))
                    {
                        commonStages.GetArrayElementAtIndex(stageIndex).boolValue = isCommon;
                        serializedObject.ApplyModifiedProperties();
                        Target.InvalidateRigCache();
                    }
                }
            }

            --EditorGUI.indentLevel;
            EditorGUILayout.EndVertical();
        }

        GUIContent mAllLabel = new GUIContent("All", "Use this setting for all three rigs");

        // Returns true if value changes
        private bool InjectAllRigsToggle(Rect r, ref bool value)
        {
            float x = GUI.skin.toggle.CalcSize(mAllLabel).x;
            float labelWidth = EditorGUIUtility.labelWidth;
            EditorGUIUtility.labelWidth = x;
            x += EditorGUIUtility.singleLineHeight;
            r.xMin = r.xMax - x;
            r.width = x; r.height -= 1;
            bool oldValue = value;
            value = EditorGUI.ToggleLeft(r, mAllLabel, oldValue);
            EditorGUIUtility.labelWidth = labelWidth;
            return (oldValue != value);
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
