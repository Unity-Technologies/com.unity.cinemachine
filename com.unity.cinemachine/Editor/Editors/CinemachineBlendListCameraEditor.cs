using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Cinemachine.Editor
{
    [CustomEditor(typeof(CinemachineBlendListCamera))]
    [CanEditMultipleObjects]
    class CinemachineBlendListCameraEditor : CinemachineVirtualCameraBaseEditor<CinemachineBlendListCamera>
    {
        /// <summary>Get the property names to exclude in the inspector.</summary>
        /// <param name="excluded">Add the names to this list</param>
        protected override void GetExcludedPropertiesInInspector(List<string> excluded)
        {
            base.GetExcludedPropertiesInInspector(excluded);
            excluded.Add(FieldPath(x => x.Instructions));
        }

        UnityEditorInternal.ReorderableList m_ChildList;
        UnityEditorInternal.ReorderableList m_InstructionList;

        string[] m_CameraCandidates;
        Dictionary<CinemachineVirtualCameraBase, int> m_CameraIndexLookup;

        protected override void OnEnable()
        {
            base.OnEnable();
            m_ChildList = null;
            m_InstructionList = null;
        }

        protected override void OnDisable()
        {
            base.OnDisable();
        }

        public override void OnInspectorGUI()
        {
            BeginInspector();
            if (m_InstructionList == null)
                SetupInstructionList();
            if (m_ChildList == null)
                SetupChildList();

            // Ordinary properties
            DrawCameraStatusInInspector();
            DrawGlobalControlsInInspector();
            DrawPropertyInInspector(FindProperty(x => x.PriorityAndChannel));
            DrawPropertyInInspector(FindProperty(x => x.DefaultTarget));
            DrawRemainingPropertiesInInspector();

            if (targets.Length == 1)
            {
                // Instructions
                UpdateCameraCandidates();
                EditorGUI.BeginChangeCheck();
                EditorGUILayout.Separator();
                m_InstructionList.DoLayoutList();

                EditorGUILayout.Separator();

                m_ChildList.DoLayoutList();
                if (EditorGUI.EndChangeCheck())
                {
                    serializedObject.ApplyModifiedProperties();
                    Target.ValidateInstructions();
                }   
            }
            else
            {
                EditorGUILayout.HelpBox(Styles.virtualCameraChildrenInfoMsg.text, MessageType.Info);
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

        void SetupChildList()
        {
            var vSpace = 2f;
            var hSpace = 3f;
            var floatFieldWidth = EditorGUIUtility.singleLineHeight * 2.5f;
            var hBigSpace = EditorGUIUtility.singleLineHeight * 2 / 3;

            m_ChildList = new UnityEditorInternal.ReorderableList(serializedObject,
                    serializedObject.FindProperty(() => Target.m_ChildCameras),
                    true, true, true, true);

            m_ChildList.drawHeaderCallback = (Rect rect) =>
                {
                    EditorGUI.LabelField(rect, "Virtual Camera Children");
                    var priorityText = new GUIContent("Priority");
                    var textDimensions = GUI.skin.label.CalcSize(priorityText);
                    rect.x += rect.width - textDimensions.x;
                    rect.width = textDimensions.x;
                    EditorGUI.LabelField(rect, priorityText);
                };
            m_ChildList.drawElementCallback
                = (Rect rect, int index, bool isActive, bool isFocused) =>
                {
                    rect.y += vSpace; rect.height = EditorGUIUtility.singleLineHeight;
                    rect.width -= floatFieldWidth + hBigSpace;
                    SerializedProperty element = m_ChildList.serializedProperty.GetArrayElementAtIndex(index);
                    GUI.enabled = false;
                    EditorGUI.PropertyField(rect, element, GUIContent.none);
                    GUI.enabled = true;
                    float oldWidth = EditorGUIUtility.labelWidth;
                    EditorGUIUtility.labelWidth = hBigSpace;
                    var obj = new SerializedObject(element.objectReferenceValue);
                    rect.x += rect.width + hSpace; rect.width = floatFieldWidth + hBigSpace;
                    var priorityProp = obj.FindProperty(
                        () => Target.PriorityAndChannel).FindPropertyRelative("Priority");
                    EditorGUI.PropertyField(rect, priorityProp, new GUIContent(" ", priorityProp.tooltip));
                    EditorGUIUtility.labelWidth = oldWidth;
                    obj.ApplyModifiedProperties();
                };
            m_ChildList.onChangedCallback = (UnityEditorInternal.ReorderableList l) =>
                {
                    if (l.index < 0 || l.index >= l.serializedProperty.arraySize)
                        return;
                    Object o = l.serializedProperty.GetArrayElementAtIndex(
                            l.index).objectReferenceValue;
                    CinemachineVirtualCameraBase vcam = (o != null)
                        ? (o as CinemachineVirtualCameraBase) : null;
                    if (vcam != null)
                        vcam.transform.SetSiblingIndex(l.index);
                };
            m_ChildList.onAddCallback = (UnityEditorInternal.ReorderableList l) =>
                {
                    var index = l.serializedProperty.arraySize;
                    var vcam = CinemachineMenu.CreatePassiveCmCamera(parentObject: Target.gameObject);
                    vcam.transform.SetSiblingIndex(index);
                };
            m_ChildList.onRemoveCallback = (UnityEditorInternal.ReorderableList l) =>
                {
                    Object o = l.serializedProperty.GetArrayElementAtIndex(
                            l.index).objectReferenceValue;
                    CinemachineVirtualCameraBase vcam = (o != null)
                        ? (o as CinemachineVirtualCameraBase) : null;
                    if (vcam != null)
                        Undo.DestroyObjectImmediate(vcam.gameObject);
                };
        }
    }
}
