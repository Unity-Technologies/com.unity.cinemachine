#define USE_IMGUI_INSTRUCTION_LIST // We use IMGUI while we wait for UUM-27687 and UUM-27688 to be fixed

using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.Cinemachine.Editor
{
    [CustomEditor(typeof(CinemachineSequencerCamera))]
    [CanEditMultipleObjects]
    class CinemachineSequencerCameraEditor : UnityEditor.Editor
    {
        CinemachineSequencerCamera Target => target as CinemachineSequencerCamera;

        public override VisualElement CreateInspectorGUI()
        {
            var ux = new VisualElement();

            this.AddCameraStatus(ux);
            this.AddTransitionsSection(ux);

            ux.AddHeader("Global Settings");
            this.AddGlobalControls(ux);

            ux.AddHeader("Sequencer");
            ux.Add(new PropertyField(serializedObject.FindProperty(() => Target.DefaultTarget)));
            ux.Add(new PropertyField(serializedObject.FindProperty(() => Target.Loop)));

            ux.AddSpace();
            var multiSelectMsg = ux.AddChild(new HelpBox(
                "Child Cameras and State Instructions cannot be displayed when multiple objects are selected.", 
                HelpBoxMessageType.Info));

            var container = ux.AddChild(new VisualElement());
#if USE_IMGUI_INSTRUCTION_LIST
            // GML todo: We use IMGUI for this while we wait for UUM-27687 and UUM-27688 to be fixed
            container.Add(new IMGUIContainer(() =>
            {
                serializedObject.Update();
                if (m_InstructionList == null)
                    SetupInstructionList();
                EditorGUI.BeginChangeCheck();
                m_InstructionList.DoLayoutList();
                if (EditorGUI.EndChangeCheck())
                    serializedObject.ApplyModifiedProperties();
            }));
            container.TrackAnyUserActivity(() =>
            {
                if (targets.Length == 1)
                    UpdateCameraCandidates();
            });
#else
            var vcam = Target;
            var header = container.AddChild(new VisualElement { style = { flexDirection = FlexDirection.Row, marginBottom = -2 } });
            FormatInstructionElement(
                header.AddChild(new Label("Child")), 
                header.AddChild(new Label("  Blend in")), 
                header.AddChild(new Label("Hold")));
            header.AddToClassList("unity-collection-view--with-border");

            var list = container.AddChild(new ListView()
            {
                reorderable = true,
                reorderMode = ListViewReorderMode.Animated,
                showAddRemoveFooter = true,
                showBorder = true,
                showBoundCollectionSize = false,
                showFoldoutHeader = false,
                style = { borderTopWidth = 0 },
            });
            var instructions = serializedObject.FindProperty(() => Target.Instructions);
            list.BindProperty(instructions);

            list.makeItem = () => new BindableElement { style = { flexDirection = FlexDirection.Row }};
            list.bindItem = (row, index) =>
            {
                // Remove children - items seem to get recycled
                for (int i = row.childCount - 1; i >= 0; --i)
                    row.RemoveAt(i);
                //row.parent.style.paddingLeft = row.parent.style.paddingRight = 0;

                var def = new CinemachineSequencerCamera.Instruction();
                var element = instructions.GetArrayElementAtIndex(index);
                ((BindableElement)row).BindProperty(element);

                var vcamSelProp = element.FindPropertyRelative(() => def.Camera);
                var vcamSel = row.AddChild(new PopupField<Object>());
                vcamSel.BindProperty(vcamSelProp);
                vcamSel.formatListItemCallback = (obj) => obj == null ? "(null)" : obj.name;
                vcamSel.formatSelectedValueCallback = (obj) => obj == null ? "(null)" : obj.name;
                vcamSel.TrackAnyUserActivity(() => vcamSel.choices = Target.ChildCameras.Cast<Object>().ToList());
        
                var blend = row.AddChild(
                    new PropertyField(element.FindPropertyRelative(() => def.Blend), ""));
                var hold = row.AddChild(
                    new InspectorUtility.CompactPropertyField(element.FindPropertyRelative(() => def.Hold), " "));
                hold.RemoveFromClassList(InspectorUtility.kAlignFieldClass);
                    
                FormatInstructionElement(vcamSel, blend, hold);
            };

            // Local function
            static void FormatInstructionElement(VisualElement e1, VisualElement e2, VisualElement e3)
            {
                var floatFieldWidth = EditorGUIUtility.singleLineHeight * 3f;
                
                e1.style.marginLeft = 3;
                e1.style.flexBasis = floatFieldWidth; 
                e1.style.flexGrow = 1;
                
                e2.style.flexBasis = floatFieldWidth; 
                e2.style.flexGrow = 1;

                e3.style.marginRight = 4;
                e3.style.flexBasis = floatFieldWidth; 
                e3.style.flexGrow = 0;
                e3.style.unityTextAlign = TextAnchor.MiddleRight;
            }
#endif
            container.AddSpace();
            this.AddChildCameras(container, null);

            container.TrackAnyUserActivity(() =>
            {
                var isMultiSelect = targets.Length > 1;
                multiSelectMsg.SetVisible(isMultiSelect);
                container.SetVisible(!isMultiSelect);
            });
            this.AddExtensionsDropdown(ux);

            return ux;
        }

#if USE_IMGUI_INSTRUCTION_LIST
        UnityEditorInternal.ReorderableList m_InstructionList;
        string[] m_CameraCandidates;
        Dictionary<CinemachineVirtualCameraBase, int> m_CameraIndexLookup;

        void OnEnable() => m_InstructionList = null;

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
            var def = new CinemachineSequencerCamera.Instruction();

            const float vSpace = 2f;
            const float hSpace = 3f;
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

            m_InstructionList.drawElementCallback = (Rect rect, int index, bool _, bool _) =>
            {
                if (m_CameraCandidates == null)
                    return;
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
#endif
    }
}
