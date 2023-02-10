using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Cinemachine.Editor
{
    [CustomEditor(typeof(CinemachineBlendListCamera))]
    [CanEditMultipleObjects]
    class CinemachineBlendListCameraEditor : CinemachineVirtualCameraBaseEditor<CinemachineBlendListCamera>
    {
        ChildListInspectorHelper m_ChildListHelper = new ();
        UnityEditorInternal.ReorderableList m_InstructionList;

        string[] m_CameraCandidates;
        Dictionary<CinemachineVirtualCameraBase, int> m_CameraIndexLookup;

        protected override void OnEnable()
        {
            base.OnEnable();
            m_ChildListHelper.OnEnable();
            m_InstructionList = null;
        }

        protected override void OnDisable()
        {
            base.OnDisable();
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            if (m_InstructionList == null)
                SetupInstructionList();

            DrawStandardInspectorTopSection();

            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(serializedObject.FindProperty(() => Target.DefaultTarget));
            EditorGUILayout.PropertyField(serializedObject.FindProperty(() => Target.ShowDebugText));
            EditorGUILayout.PropertyField(serializedObject.FindProperty(() => Target.Loop));
            if (EditorGUI.EndChangeCheck())
                serializedObject.ApplyModifiedProperties();

            if (targets.Length == 1)
            {
                // Instructions
                UpdateCameraCandidates();

                EditorGUI.BeginChangeCheck();
                EditorGUILayout.Separator();
                m_InstructionList.DoLayoutList();
                EditorGUILayout.Separator();
                if (m_ChildListHelper.OnInspectorGUI(serializedObject.FindProperty(() => Target.m_ChildCameras)))
                    Target.ValidateInstructions();
                if (EditorGUI.EndChangeCheck()) 
                    serializedObject.ApplyModifiedProperties();
            }
            else
            {
                EditorGUILayout.HelpBox(
                    "Child Cameras and State Instructions cannot be displayed when multiple objects are selected", 
                    MessageType.Info);
            }

            // Extensions
            DrawExtensionsWidgetInInspector();
        }

        void UpdateCameraCandidates()
        {
            List<string> vcams = new List<string>();
            m_CameraIndexLookup = new Dictionary<CinemachineVirtualCameraBase, int>();
            vcams.Add("(none)");
            var children = Target.ChildCameras;
            foreach (var c in children)
            {
                m_CameraIndexLookup[c] = vcams.Count;
                vcams.Add(c.Name);
            }
            m_CameraCandidates = vcams.ToArray();
        }

        int GetCameraIndex(Object obj)
        {
            if (obj == null || m_CameraIndexLookup == null)
                return 0;
            var vcam = obj as CinemachineVirtualCameraBase;
            if (vcam == null)
                return 0;
            if (!m_CameraIndexLookup.ContainsKey(vcam))
                return 0;
            return m_CameraIndexLookup[vcam];
        }

        void SetupInstructionList()
        {
            m_InstructionList = new UnityEditorInternal.ReorderableList(serializedObject,
                    serializedObject.FindProperty(() => Target.Instructions),
                    true, true, true, true);

            // Needed for accessing field names as strings
            var def = new CinemachineBlendListCamera.Instruction();

            var vSpace = 2f;
            var hSpace = 3f;
            var floatFieldWidth = EditorGUIUtility.singleLineHeight * 2.5f;
            var hBigSpace = EditorGUIUtility.singleLineHeight * 2 / 3;
            m_InstructionList.drawHeaderCallback = (Rect rect) =>
                {
                    float sharedWidth = rect.width - EditorGUIUtility.singleLineHeight
                        - floatFieldWidth - hSpace - hBigSpace;
                    rect.x += EditorGUIUtility.singleLineHeight; rect.width = sharedWidth / 2;
                    EditorGUI.LabelField(rect, "Child");

                    rect.x += rect.width + hSpace;
                    EditorGUI.LabelField(rect, "Blend in");

                    rect.x += rect.width + hBigSpace; rect.width = floatFieldWidth;
                    EditorGUI.LabelField(rect, "Hold");
                };

            m_InstructionList.drawElementCallback
                = (Rect rect, int index, bool isActive, bool isFocused) =>
                {
                    SerializedProperty instProp = m_InstructionList.serializedProperty.GetArrayElementAtIndex(index);
                    float sharedWidth = rect.width - floatFieldWidth - hSpace - hBigSpace;
                    rect.y += vSpace; rect.height = EditorGUIUtility.singleLineHeight;

                    rect.width = sharedWidth / 2;
                    SerializedProperty vcamSelProp = instProp.FindPropertyRelative(() => def.Camera);
                    int currentVcam = GetCameraIndex(vcamSelProp.objectReferenceValue);
                    int vcamSelection = EditorGUI.Popup(rect, currentVcam, m_CameraCandidates);
                    if (currentVcam != vcamSelection)
                        vcamSelProp.objectReferenceValue = (vcamSelection == 0)
                            ? null : Target.ChildCameras[vcamSelection - 1];

                    rect.x += rect.width + hSpace; rect.width = sharedWidth / 2;
                    if (index > 0 || Target.Loop)
                        EditorGUI.PropertyField(rect, instProp.FindPropertyRelative(() => def.Blend),
                            GUIContent.none);

                    if (index < m_InstructionList.count - 1 || Target.Loop)
                    {
                        float oldWidth = EditorGUIUtility.labelWidth;
                        EditorGUIUtility.labelWidth = hBigSpace;

                        rect.x += rect.width; rect.width = floatFieldWidth + hBigSpace;
                        SerializedProperty holdProp = instProp.FindPropertyRelative(() => def.Hold);
                        EditorGUI.PropertyField(rect, holdProp, new GUIContent(" ", holdProp.tooltip));
                        holdProp.floatValue = Mathf.Max(holdProp.floatValue, 0);

                        EditorGUIUtility.labelWidth = oldWidth;
                    }
                };
        }
    }
}
