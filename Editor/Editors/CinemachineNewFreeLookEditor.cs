using UnityEngine;
using UnityEditor;
using Cinemachine.Editor;
using System.Collections.Generic;

namespace Cinemachine
{
    [CustomEditor(typeof(CinemachineNewFreeLook))]
    sealed class CinemachineNewFreeLookEditor 
        : CinemachineVirtualCameraBaseEditor<CinemachineNewFreeLook>
    {
        GUIContent[] mRigNames = new GUIContent[] 
            { new GUIContent("Top Rig"), new GUIContent("Bottom Rig") };

        GUIContent[] mOrbitNames = new GUIContent[] 
            { new GUIContent("Top Rig"), new GUIContent("Main Rig"), new GUIContent("Bottom Rig") };

        GUIContent mAllLensLabel = new GUIContent(
            "Customize", "Custom settings for this rig.  If unchecked, main rig settins will be used");

        CinemachineStageEditor[] m_editors = null;

        protected override List<string> GetExcludedPropertiesInInspector()
        {
            List<string> excluded = base.GetExcludedPropertiesInInspector();
            excluded.Add(FieldPath(x => x.m_Rigs));
            return excluded;
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            m_editors = new CinemachineStageEditor[(int)CinemachineCore.Stage.Finalize];
            if (Target == null)
                return;
            for (CinemachineCore.Stage stage = CinemachineCore.Stage.Body; 
                stage < CinemachineCore.Stage.Finalize; ++stage)
            {
                var ed = new CinemachineStageEditor(stage, Target.gameObject);
                m_editors[(int)stage] = ed;
                ed.SetComponent = (type) 
                    => {
                        var vcam = Target.GetComponent<CinemachineNewFreeLook>();
                        Undo.RecordObject(Undo.AddComponent(Target.gameObject, type), "Set CM Component");
                        vcam.InvalidateComponentCache();
                    };
                ed.DestroyComponent = (component) 
                    => {
                        var vcam = Target.GetComponent<CinemachineNewFreeLook>();
                        Undo.DestroyObjectImmediate(component);
                        vcam.InvalidateComponentCache();
                    };
            }
        }

        protected override void OnDisable()
        {
            OnDestroy();
            base.OnDisable();
        }

        void OnDestroy()
        {
            if (m_editors != null)
            {
                for (int i = 0; i < m_editors.Length; ++i)
                {
                    if (m_editors[i] != null)
                        m_editors[i].Shutdown();
                    m_editors[i] = null;
                }
                m_editors = null;
            }
        }

        public override void OnInspectorGUI()
        {
            // Ordinary properties
            BeginInspector();
            DrawHeaderInInspector();
            DrawPropertyInInspector(FindProperty(x => x.m_Priority));
            DrawTargetsInInspector(FindProperty(x => x.m_Follow), FindProperty(x => x.m_LookAt));
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
                    rect, mOrbitNames[i],
                    new [] { o.FindPropertyRelative(() => Target.m_Orbits[i].m_Height), 
                            o.FindPropertyRelative(() => Target.m_Orbits[i].m_Radius) },
                    null);
            }
            EditorGUILayout.PropertyField(FindProperty(x => x.m_SplineCurvature));
            if (EditorGUI.EndChangeCheck())
                serializedObject.ApplyModifiedProperties();

            // CM Components
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Main Rig", EditorStyles.boldLabel);
            DrawSubeditors();

            // Rigs
            EditorGUILayout.Space();
            SerializedProperty rigs = FindProperty(x => x.m_Rigs);
            for (int i = 0; i < 2; ++i)
            {
                EditorGUILayout.Separator();
                DrawRigEditor(i, rigs.GetArrayElementAtIndex(i));
            }
            
            // Extensions
            DrawExtensionsWidgetInInspector();
        }

        void DrawSubeditors()
        {
            // Pipeline Stages
            var components = Target.ComponentCache;
            for (int i = 0; i < m_editors.Length; ++i)
            {
                var ed = m_editors[i];
                if (ed == null)
                    continue;
                if (!ed.HasImplementation)
                    continue;
                if ((CinemachineCore.Stage)i == CinemachineCore.Stage.Body)
                    ed.TypeIsLocked = true;

                ed.OnInspectorGUI(components[i]); // may destroy component
            }
        }

        void DrawRigEditor(int rigIndex, SerializedProperty rig)
        {
            const float kBoxMargin = 3;

            CinemachineNewFreeLook.Rig def = new CinemachineNewFreeLook.Rig(); // for properties
            EditorGUILayout.BeginVertical(GUI.skin.box);
            EditorGUIUtility.labelWidth -= kBoxMargin;
            EditorGUILayout.LabelField(new GUIContent(mRigNames[rigIndex]), EditorStyles.boldLabel);

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
            EditorGUILayout.EndVertical();
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
                mAllLensLabel, enabledProperty.boolValue);
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