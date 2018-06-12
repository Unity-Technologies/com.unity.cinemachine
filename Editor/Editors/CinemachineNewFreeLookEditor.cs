using UnityEngine;
using UnityEditor;
using Cinemachine.Editor;
using System.Collections.Generic;
using Cinemachine.Utility;

namespace Cinemachine
{
    [CustomEditor(typeof(CinemachineNewFreeLook))]
    sealed class CinemachineNewFreeLookEditor 
        : CinemachineVirtualCameraBaseEditor<CinemachineNewFreeLook>
    {
        GUIContent[] mRigNames = new GUIContent[] 
            { new GUIContent("Main Rig"), new GUIContent("Top Rig"), new GUIContent("Bottom Rig") };

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

        protected override void OnDisable()
        {
            base.OnDisable();

            // Must destroy child editors or we get exceptions
            if (m_editors != null)
                foreach (UnityEditor.Editor e in m_editors)
                    if (e != null)
                        UnityEngine.Object.DestroyImmediate(e);
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
            UpdateRigEditors();
            SerializedProperty rigs = FindProperty(x => x.m_Rigs);
            for (int i = 0; i < 3; ++i)
            {
                //if (m_editors[i] == null)
                //    continue;
                EditorGUILayout.Separator();
                EditorGUILayout.BeginVertical(GUI.skin.box);
                EditorGUILayout.LabelField(mRigNames[i], EditorStyles.boldLabel);
                ++EditorGUI.indentLevel;

                SerializedProperty rig = rigs.GetArrayElementAtIndex(i);

                EditorGUI.BeginChangeCheck();
                Rect rect = EditorGUILayout.GetControlRect(true);
                InspectorUtility.MultiPropertyOnLine(rect, 
                    new GUIContent(mRigNames[i]),
                    new [] { rig.FindPropertyRelative(() => Target.m_Rigs[i].m_Height), 
                            rig.FindPropertyRelative(() => Target.m_Rigs[i].m_Radius) },
                    null);
                if (EditorGUI.EndChangeCheck())
                    serializedObject.ApplyModifiedProperties();

                SerializedProperty rigUseLens = rig.FindPropertyRelative(() => Target.m_Rigs[i].m_CustomLens);
                EditorGUI.BeginChangeCheck();
                EditorGUILayout.PropertyField(rigUseLens);
                if (EditorGUI.EndChangeCheck())
                    serializedObject.ApplyModifiedProperties();

                EditorGUI.BeginChangeCheck();
                if (rigUseLens.boolValue)
                    EditorGUILayout.PropertyField(rig.FindPropertyRelative(() => Target.m_Rigs[i].m_Lens));

                if (EditorGUI.EndChangeCheck())
                    serializedObject.ApplyModifiedProperties();

                //m_editors[i].OnInspectorGUI();

                --EditorGUI.indentLevel;
                EditorGUILayout.EndVertical();
            }

            // Extensions
            DrawExtensionsWidgetInInspector();
        }

        UnityEditor.Editor[] m_editors;

        //string[] RigNames;
        //CinemachineVirtualCameraBase[] m_rigs;
        void UpdateRigEditors()
        {
#if false
            RigNames = CinemachineNewFreeLook.RigNames;
            if (m_rigs == null)
                m_rigs = new CinemachineVirtualCameraBase[RigNames.Length];
            if (m_editors == null)
                m_editors = new UnityEditor.Editor[RigNames.Length];
            for (int i = 0; i < RigNames.Length; ++i)
            {
                CinemachineVirtualCamera rig = Target.GetRig(i);
                if (rig == null || rig != m_rigs[i])
                {
                    m_rigs[i] = rig;
                    if (m_editors[i] != null)
                        UnityEngine.Object.DestroyImmediate(m_editors[i]);
                    m_editors[i] = null;
                    if (rig != null)
                        CreateCachedEditor(rig, null, ref m_editors[i]);
                }
            }
#endif
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
