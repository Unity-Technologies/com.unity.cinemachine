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
            var vcam = Target;
            var header = container.AddChild(new VisualElement { style = { flexDirection = FlexDirection.Row, marginBottom = -2 } });
            FormatInstructionElement(true,
                header.AddChild(new Label("Child Camera")), 
                header.AddChild(new Label("Blend in")), 
                header.AddChild(new Label("Hold")));
            header.AddToClassList("unity-collection-view--with-border");

            var list = container.AddChild(new ListView()
            {
                name = "InstructionList",
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
                // Remove children - items get recycled
                for (int i = row.childCount - 1; i >= 0; --i)
                    row.RemoveAt(i);

                var def = new CinemachineSequencerCamera.Instruction();
                var element = instructions.GetArrayElementAtIndex(index);

                var vcamSelProp = element.FindPropertyRelative(() => def.Camera);
                var vcamSel = row.AddChild(new PopupField<Object> { name = $"vcamSelector{index}", choices = new() });
                vcamSel.formatListItemCallback = (obj) => obj == null ? "(null)" : obj.name;
                vcamSel.formatSelectedValueCallback = (obj) => obj == null ? "(null)" : obj.name;
                vcamSel.TrackPropertyWithInitialCallback(instructions, (p) => UpdateCameraDropdowns());
        
                var blend = row.AddChild(new PropertyField(element.FindPropertyRelative(() => def.Blend), ""));
                if (index == 0)
                    blend.name = "FirstItemBlend";
                var hold = row.AddChild(
                    new InspectorUtility.CompactPropertyField(element.FindPropertyRelative(() => def.Hold), " "));
                hold.RemoveFromClassList(InspectorUtility.kAlignFieldClass);
                    
                FormatInstructionElement(false, vcamSel, blend, hold);

                // Bind must be last
                ((BindableElement)row).BindProperty(element);
                vcamSel.BindProperty(vcamSelProp);
            };

            container.AddSpace();
            this.AddChildCameras(container, null);

            container.TrackAnyUserActivity(() =>
            {
                if (Target == null)
                    return; // object deleted

                var isMultiSelect = targets.Length > 1;
                multiSelectMsg.SetVisible(isMultiSelect);
                container.SetVisible(!isMultiSelect);

                // Hide the first blend if not looped
                list.Q<VisualElement>("FirstItemBlend")?.SetEnabled(Target.Loop);

                // Update the list items
                UpdateCameraDropdowns();
            });
            this.AddExtensionsDropdown(ux);

            return ux;

            // Local function
            void UpdateCameraDropdowns()
            {
                var children = Target.ChildCameras;
                int index = 0;
                var iter = list.itemsSource.GetEnumerator();
                while (iter.MoveNext())
                {
                    var vcamSel = list.Q<PopupField<Object>>($"vcamSelector{index}");
                    if (vcamSel != null)
                    {
                        vcamSel.choices.Clear();
                        for (int i = 0; i < children.Count; ++i)
                            vcamSel.choices.Add(children[i]);
                    }
                    ++index;
                }
            }

            // Local function
            static void FormatInstructionElement(bool isHeader, VisualElement e1, VisualElement e2, VisualElement e3)
            {
                var floatFieldWidth = EditorGUIUtility.singleLineHeight * 3f;
                
                e1.style.marginLeft = isHeader ? 2 * InspectorUtility.SingleLineHeight - 3 : 0;
                e1.style.flexBasis = floatFieldWidth + InspectorUtility.SingleLineHeight; 
                e1.style.flexGrow = 1;
                
                e2.style.flexBasis = floatFieldWidth; 
                e2.style.flexGrow = 1;

                e3.style.marginRight = 4;
                e3.style.flexBasis = floatFieldWidth; 
                e3.style.flexGrow = 0;
                e3.style.unityTextAlign = TextAnchor.MiddleRight;
            }
        }
    }
}
