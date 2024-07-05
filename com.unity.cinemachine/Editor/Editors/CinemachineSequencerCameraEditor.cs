using System.Collections.Generic;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.Cinemachine.Editor
{
    [CustomEditor(typeof(CinemachineSequencerCamera))]
    [CanEditMultipleObjects]
    class CinemachineSequencerCameraEditor : CinemachineVirtualCameraBaseEditor
    {
        CinemachineSequencerCamera Target => target as CinemachineSequencerCamera;

        protected override void AddInspectorProperties(VisualElement ux)
        {
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
            container.AddHeader("Instructions");
            var vcam = Target;
            var header = container.AddChild(new VisualElement { style = { flexDirection = FlexDirection.Row, marginBottom = -2 } });
            FormatInstructionElement(true,
                header.AddChild(new Label("Child Camera")), 
                header.AddChild(new Label("Blend in")), 
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

            list.makeItem = () => 
            {
                var row = new BindableElement { style = { flexDirection = FlexDirection.Row }};

                var def = new CinemachineSequencerCamera.Instruction();
                var vcamSel = row.AddChild(new PropertyField(null, "") { bindingPath = SerializedPropertyHelper.PropertyName(() => def.Camera) });
        
                var blend = row.AddChild(new PropertyField(null, "") { bindingPath = SerializedPropertyHelper.PropertyName(() => def.Blend), name = "blendSelector" });
                var hold = row.AddChild(InspectorUtility.CreateDraggableField(() => def.Hold, row.AddChild(new Label(" ")), out _));
                hold.SafeSetIsDelayed();
                    
                FormatInstructionElement(false, vcamSel, blend, hold);
                return row;
            };

            container.TrackAnyUserActivity(() =>
            {
                if (Target == null || list.itemsSource == null)
                    return; // object deleted

                var isMultiSelect = targets.Length > 1;
                multiSelectMsg.SetVisible(isMultiSelect);
                container.SetVisible(!isMultiSelect);

                // Hide the first blend if not looped
                var index = 0;
                list.Query<VisualElement>().Where((e) => e.name == "blendSelector").ForEach((e) 
                    => e.style.visibility = (index++ == 0 && !Target.Loop) ? Visibility.Hidden : Visibility.Visible);
            });

            // Local function
            static void FormatInstructionElement(bool isHeader, VisualElement e1, VisualElement e2, VisualElement e3)
            {
                var floatFieldWidth = EditorGUIUtility.singleLineHeight * 3f;
                
                e1.style.marginLeft = isHeader ? 2 * InspectorUtility.SingleLineHeight - 3 : 0;
                e1.style.flexBasis = floatFieldWidth + InspectorUtility.SingleLineHeight; 
                e1.style.flexGrow = 1;
                e1.style.flexShrink = 0;
                
                e2.style.flexBasis = floatFieldWidth + InspectorUtility.SingleLineHeight; 
                e2.style.flexGrow = 1;
                e2.style.flexShrink = 0;

                floatFieldWidth += isHeader ? InspectorUtility.SingleLineHeight/2 - 1 : 0;
                e3.style.flexBasis = floatFieldWidth; 
                e3.style.flexGrow = 0;
            }
        }
    }
}
