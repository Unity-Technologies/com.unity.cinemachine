#if CINEMACHINE_UNITY_ANIMATION
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEditor.Animations;
using Object = UnityEngine.Object;
using UnityEngine.UIElements;
using UnityEditor.UIElements;

namespace Unity.Cinemachine.Editor
{
    [CustomEditor(typeof(CinemachineStateDrivenCamera))]
    class CinemachineStateDrivenCameraEditor : UnityEditor.Editor
    {
        CinemachineStateDrivenCamera Target => target as CinemachineStateDrivenCamera;

        List<string> m_LayerNames = new();
        List<int> m_TargetStates = new();
        List<string> m_TargetStateNames = new();
        Dictionary<int, int> m_StateIndexLookup;

        public override VisualElement CreateInspectorGUI()
        {
            var ux = new VisualElement();

            var noTargetHelp = ux.AddChild(new HelpBox("An Animated Target is required.", HelpBoxMessageType.Warning));

            this.AddCameraStatus(ux);
            this.AddTransitionsSection(ux);

            ux.AddHeader("Global Settings");
            this.AddGlobalControls(ux);

            ux.AddHeader("State Driven Camera");
            ux.Add(new PropertyField(serializedObject.FindProperty(() => Target.DefaultTarget)));
            ux.Add(new PropertyField(serializedObject.FindProperty(() => Target.DefaultBlend)));
            ux.Add(new PropertyField(serializedObject.FindProperty(() => Target.CustomBlends)));
            ux.Add(new PropertyField(serializedObject.FindProperty(() => Target.AnimatedTarget)));

            var layerProp = serializedObject.FindProperty(() => Target.LayerIndex);
            var layerSel = ux.AddChild(new PopupField<string>(layerProp.displayName) { tooltip = layerProp.tooltip });
            layerSel.AddToClassList(InspectorUtility.kAlignFieldClass);
            layerSel.RegisterValueChangedCallback((evt) => 
            {
                layerProp.intValue = Mathf.Max(0, m_LayerNames.FindIndex(v => v == evt.newValue));
                serializedObject.ApplyModifiedProperties();
            });

            ux.TrackAnyUserActivity(() =>
            {
                if (Target == null)
                    return; // object deleted
                UpdateTargetStates();
                layerSel.choices = m_LayerNames;
                layerSel.SetValueWithoutNotify(m_LayerNames[layerProp.intValue]);
                noTargetHelp.SetVisible(Target.AnimatedTarget == null);
            });
            
            var multiSelectMsg = ux.AddChild(new HelpBox(
                "Child Cameras and State Instructions cannot be displayed when multiple objects are selected.", 
                HelpBoxMessageType.Info));

            var container = ux.AddChild(new VisualElement() { style = { marginTop = 6 }});
            var vcam = Target;
            var header = container.AddChild(new VisualElement { style = { flexDirection = FlexDirection.Row, marginBottom = -2 } });
            FormatInstructionElement(true,
                header.AddChild(new Label("State")), 
                header.AddChild(new Label("Camera")), 
                header.AddChild(new Label("Wait")),
                header.AddChild(new Label("Min")));
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

                var def = new CinemachineStateDrivenCamera.Instruction();
                var element = instructions.GetArrayElementAtIndex(index);

                var stateSelProp = element.FindPropertyRelative(() => def.FullHash);
                int currentState = GetStateHashIndex(stateSelProp.intValue);
                var stateSel = row.AddChild(new PopupField<string> 
                {
                    name = $"stateSelector{index}", 
                    choices = m_TargetStateNames, 
                    tooltip = "The state that will activate the camera" 
                });
                stateSel.RegisterValueChangedCallback((evt) => 
                {
                    if (evt.target == stateSel)
                    {
                        var index = stateSel.index;
                        if (index >= 0 && index < m_TargetStates.Count)
                            stateSelProp.intValue = index;
                        evt.StopPropagation();
                    }
                });
                stateSel.TrackPropertyWithInitialCallback(stateSelProp, (p) =>
                {
                    var hash = p.intValue;
                    for (int i = 0; i < m_TargetStates.Count; ++i)
                    {
                        if (hash == m_TargetStates[i])
                        {
                            stateSel.SetValueWithoutNotify(m_TargetStateNames[i]);
                            return;
                        }
                    }
                    stateSel.SetValueWithoutNotify(m_TargetStateNames[0]);
                });

                var vcamSelProp = element.FindPropertyRelative(() => def.Camera);
                var vcamSel = row.AddChild(new PopupField<Object> { name = $"vcamSelector{index}", choices = new() });
                vcamSel.formatListItemCallback = (obj) => obj == null ? "(null)" : obj.name;
                vcamSel.formatSelectedValueCallback = (obj) => obj == null ? "(null)" : obj.name;
                vcamSel.TrackPropertyWithInitialCallback(instructions, (p) => UpdateCameraDropdowns());
        
                var wait = row.AddChild(
                    new InspectorUtility.CompactPropertyField(element.FindPropertyRelative(() => def.ActivateAfter), " "));
                var hold = row.AddChild(
                    new InspectorUtility.CompactPropertyField(element.FindPropertyRelative(() => def.MinDuration), " "));
                    
                FormatInstructionElement(false, stateSel, vcamSel, wait, hold);

                // Bind must be last
                ((BindableElement)row).BindProperty(element);
                vcamSel.BindProperty(vcamSelProp);
            };

            container.TrackAnyUserActivity(() =>
            {
                if (Target == null || list.itemsSource == null)
                    return; // object deleted

                var isMultiSelect = targets.Length > 1;
                multiSelectMsg.SetVisible(isMultiSelect);
                container.SetVisible(!isMultiSelect);
                UpdateCameraDropdowns();
            });
            container.AddSpace();
            this.AddChildCameras(container, null);
            container.AddSpace();
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
    Debug.Log($"vcamSelector{index}: {children[i].name}");
                        for (int i = 0; i < children.Count; ++i)
                            vcamSel.choices.Add(children[i]);
                    }
                    ++index;
                }
            }

            // Local function
            static void FormatInstructionElement(
                bool isHeader, VisualElement e1, VisualElement e2, VisualElement e3, VisualElement e4)
            {
                var floatFieldWidth = EditorGUIUtility.singleLineHeight * 3f;
                
                e1.style.marginLeft = isHeader ? 2 * InspectorUtility.SingleLineHeight - 3 : 0;
                e1.style.flexBasis = floatFieldWidth + InspectorUtility.SingleLineHeight; 
                e1.style.flexGrow = 1;
                
                e2.style.flexBasis = floatFieldWidth + InspectorUtility.SingleLineHeight; 
                e2.style.flexGrow = 1;

                e3.style.flexBasis = floatFieldWidth; 
                e3.style.flexGrow = 0;

                e4.style.marginRight = 4;
                e4.style.flexBasis = floatFieldWidth; 
                e4.style.flexGrow = 0;
                e4.style.unityTextAlign = TextAnchor.MiddleRight;
            }
        }

        static AnimatorController GetControllerFromAnimator(Animator animator)
        {
            if (animator == null)
                return null;
            var ovr = animator.runtimeAnimatorController as AnimatorOverrideController;
            if (ovr)
                return ovr.runtimeAnimatorController as AnimatorController;
            return animator.runtimeAnimatorController as AnimatorController;
        }

        void UpdateTargetStates()
        {
            // Scrape the Animator Controller for states
            var ac = GetControllerFromAnimator(Target.AnimatedTarget);
            var collector = new StateCollector();
            collector.CollectStates(ac, Target.LayerIndex);
            m_TargetStates = collector.States;
            m_TargetStateNames = collector.StateNames;
            m_StateIndexLookup = collector.StateIndexLookup;

            m_LayerNames.Clear();
            for (int i = 0; ac != null && i < ac.layers.Length; ++i)
                m_LayerNames.Add(ac.layers[i].name);
            if (m_LayerNames.Count == 0)
                m_LayerNames.Add("(missing animated target)");

            // Create the parent map in the target
            List<CinemachineStateDrivenCamera.ParentHash> parents = new();
            var iter = collector.StateParentLookup.GetEnumerator();
            while (iter.MoveNext())
                parents.Add(new CinemachineStateDrivenCamera.ParentHash 
                    { Hash = iter.Current.Key, HashOfParent = iter.Current.Value });
            Target.HashOfParent = parents.ToArray();
        }

        class StateCollector
        {
            public List<int> States;
            public List<string> StateNames;
            public Dictionary<int, int> StateIndexLookup;
            public Dictionary<int, int> StateParentLookup;

            public void CollectStates(AnimatorController ac, int layerIndex)
            {
                States = new List<int>();
                StateNames = new List<string>();
                StateIndexLookup = new Dictionary<int, int>();
                StateParentLookup = new Dictionary<int, int>();

                StateIndexLookup[0] = States.Count;
                StateNames.Add("(default)");
                States.Add(0);

                if (ac != null && layerIndex >= 0 && layerIndex < ac.layers.Length)
                {
                    AnimatorStateMachine fsm = ac.layers[layerIndex].stateMachine;
                    string name = fsm.name;
                    int hash = Animator.StringToHash(name);
                    CollectStatesFromFSM(fsm, name + ".", hash, string.Empty);
                }
            }

            void CollectStatesFromFSM(
                AnimatorStateMachine fsm, string hashPrefix, int parentHash, string displayPrefix)
            {
                var states = fsm.states;
                for (int i = 0; i < states.Length; i++)
                {
                    var state = states[i].state;
                    int hash = AddState(Animator.StringToHash(hashPrefix + state.name),
                        parentHash, displayPrefix + state.name);

                    // Also process clips as pseudo-states, if more than 1 is present.
                    // Since they don't have hashes, we can manufacture some.
                    var clips = CollectClips(state.motion);
                    if (clips.Count > 1)
                    {
                        string substatePrefix = displayPrefix + state.name + ".";
                        for (int j = 0; j < clips.Count; ++j)
                            AddState(
                                CinemachineStateDrivenCamera.CreateFakeHash(hash, clips[j]),
                                hash, substatePrefix + clips[j].name);
                    }
                }

                var fsmChildren = fsm.stateMachines;
                for (int i = 0; i < fsmChildren.Length; ++i)
                {
                    var child = fsmChildren[i];
                    string name = hashPrefix + child.stateMachine.name;
                    string displayName = displayPrefix + child.stateMachine.name;
                    int hash = AddState(Animator.StringToHash(name), parentHash, displayName);
                    CollectStatesFromFSM(child.stateMachine, name + ".", hash, displayName + ".");
                }
            }

            List<AnimationClip> CollectClips(UnityEngine.Motion motion)
            {
                var clips = new List<AnimationClip>();
                var clip = motion as AnimationClip;
                if (clip != null)
                    clips.Add(clip);
                var tree = motion as BlendTree;
                if (tree != null)
                {
                    var children = tree.children;
                    for (int i = 0; i < children.Length; ++i)
                        clips.AddRange(CollectClips(children[i].motion));
                }
                return clips;
            }

            int AddState(int hash, int parentHash, string displayName)
            {
                if (parentHash != 0)
                    StateParentLookup[hash] = parentHash;
                StateIndexLookup[hash] = States.Count;
                StateNames.Add(displayName);
                States.Add(hash);
                return hash;
            }
        }

        int GetStateHashIndex(int stateHash)
        {
            if (stateHash == 0)
                return 0;
            if (!m_StateIndexLookup.ContainsKey(stateHash))
                return 0;
            return m_StateIndexLookup[stateHash];
        }
    }
}
#endif
