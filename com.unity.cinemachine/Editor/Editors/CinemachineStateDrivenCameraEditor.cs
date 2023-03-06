#if CINEMACHINE_UNITY_ANIMATION
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEditor.Animations;
using Object = UnityEngine.Object;
using UnityEngine.UIElements;
using UnityEditor.UIElements;

namespace Cinemachine.Editor
{
    [CustomEditor(typeof(CinemachineStateDrivenCamera))]
    class CinemachineStateDrivenCameraEditor : UnityEditor.Editor
    {
        CinemachineStateDrivenCamera Target => target as CinemachineStateDrivenCamera;

        UnityEditorInternal.ReorderableList m_InstructionList;
        List<string> m_LayerNames = new();
        int[] m_TargetStates;
        string[] m_TargetStateNames;
        Dictionary<int, int> m_StateIndexLookup;

        string[] m_CameraCandidates;
        Dictionary<CinemachineVirtualCameraBase, int> m_CameraIndexLookup;

        void OnEnable() => m_InstructionList = null;

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
            ux.Add(EmbeddedAssetEditorUtility.EmbeddedAssetInspector<CinemachineBlenderSettings>(
                serializedObject.FindProperty(() => Target.CustomBlends),
                (ed) =>
                {
                    var editor = ed as CinemachineBlenderSettingsEditor;
                    if (editor != null)
                        editor.GetAllVirtualCameras = (list) => list.AddRange(Target.ChildCameras);
                }));

            ux.Add(new PropertyField(serializedObject.FindProperty(() => Target.AnimatedTarget)));

            var layerProp = serializedObject.FindProperty(() => Target.LayerIndex);
            var layerSel = ux.AddChild(new PopupField<string>(layerProp.displayName) { tooltip = layerProp.tooltip });
            layerSel.AddToClassList(InspectorUtility.kAlignFieldClass);
            layerSel.RegisterValueChangedCallback((evt) => 
            {
                layerProp.intValue = Mathf.Max(0, m_LayerNames.FindIndex(v => v == evt.newValue));
                serializedObject.ApplyModifiedProperties();
            });

            // GML todo: We use IMGUI for this while we wait for UUM-27687 and UUM-27688 to be fixed
            ux.AddSpace();
            ux.Add(new IMGUIContainer(() =>
            {
                serializedObject.Update();
                if (m_InstructionList == null)
                    SetupInstructionList();
                EditorGUI.BeginChangeCheck();
                m_InstructionList.DoLayoutList();
                if (EditorGUI.EndChangeCheck())
                    serializedObject.ApplyModifiedProperties();
            }));

            ux.AddSpace();
            this.AddChildCameras(ux, null);
            this.AddExtensionsDropdown(ux);

            UpdateStatesAndCache();
            ux.TrackAnyUserActivity(UpdateStatesAndCache);

            return ux;
            
            // local function
            void UpdateStatesAndCache()
            {
                UpdateTargetStates();
                layerSel.choices = m_LayerNames;
                layerSel.SetValueWithoutNotify(m_LayerNames[layerProp.intValue]);
                UpdateCameraCandidates();
                noTargetHelp.SetVisible(Target.AnimatedTarget == null);
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
            m_TargetStates = collector.States.ToArray();
            m_TargetStateNames = collector.StateNames.ToArray();
            m_StateIndexLookup = collector.StateIndexLookup;

            m_LayerNames.Clear();
            for (int i = 0; ac != null && i < ac.layers.Length; ++i)
                m_LayerNames.Add(ac.layers[i].name);
            if (m_LayerNames.Count == 0)
                m_LayerNames.Add("(missing animated target)");

            // Create the parent map in the target
            List<CinemachineStateDrivenCamera.ParentHash> parents = new();
            foreach (var i in collector.StateParentLookup)
                parents.Add(new CinemachineStateDrivenCamera.ParentHash { Hash = i.Key, HashOfParent = i.Value });
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
                ChildAnimatorState[] states = fsm.states;
                for (int i = 0; i < states.Length; i++)
                {
                    AnimatorState state = states[i].state;
                    int hash = AddState(Animator.StringToHash(hashPrefix + state.name),
                        parentHash, displayPrefix + state.name);

                    // Also process clips as pseudo-states, if more than 1 is present.
                    // Since they don't have hashes, we can manufacture some.
                    var clips = CollectClips(state.motion);
                    if (clips.Count > 1)
                    {
                        string substatePrefix = displayPrefix + state.name + ".";
                        foreach (AnimationClip c in clips)
                            AddState(
                                CinemachineStateDrivenCamera.CreateFakeHash(hash, c),
                                hash, substatePrefix + c.name);
                    }
                }

                ChildAnimatorStateMachine[] fsmChildren = fsm.stateMachines;
                foreach (var child in fsmChildren)
                {
                    string name = hashPrefix + child.stateMachine.name;
                    string displayName = displayPrefix + child.stateMachine.name;
                    int hash = AddState(Animator.StringToHash(name), parentHash, displayName);
                    CollectStatesFromFSM(child.stateMachine, name + ".", hash, displayName + ".");
                }
            }

            List<AnimationClip> CollectClips(Motion motion)
            {
                var clips = new List<AnimationClip>();
                var clip = motion as AnimationClip;
                if (clip != null)
                    clips.Add(clip);
                var tree = motion as BlendTree;
                if (tree != null)
                {
                    var children = tree.children;
                    foreach (var child in children)
                        clips.AddRange(CollectClips(child.motion));
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

        void UpdateCameraCandidates()
        {
            List<string> vcams = new();
            m_CameraIndexLookup = new();
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
            CinemachineStateDrivenCamera.Instruction def = new();

            var vSpace = 2f;
            var hSpace = 3f;
            var floatFieldWidth = EditorGUIUtility.singleLineHeight * 2.5f;
            var hBigSpace = EditorGUIUtility.singleLineHeight * 2 / 3;
            m_InstructionList.drawHeaderCallback = (Rect rect) =>
                {
                    float sharedWidth = rect.width - EditorGUIUtility.singleLineHeight
                        - 2 * (hBigSpace + floatFieldWidth) - hSpace;
                    rect.x += EditorGUIUtility.singleLineHeight; rect.width = sharedWidth / 2;
                    EditorGUI.LabelField(rect, "State");

                    rect.x += rect.width + hSpace;
                    EditorGUI.LabelField(rect, "Camera");

                    rect.x += rect.width + hBigSpace; rect.width = floatFieldWidth;
                    EditorGUI.LabelField(rect, "Wait");

                    rect.x += rect.width + hBigSpace;
                    EditorGUI.LabelField(rect, "Min");
                };

            m_InstructionList.drawElementCallback
                = (Rect rect, int index, bool isActive, bool isFocused) =>
                {
                    SerializedProperty instProp
                        = m_InstructionList.serializedProperty.GetArrayElementAtIndex(index);
                    float sharedWidth = rect.width - 2 * (hBigSpace + floatFieldWidth) - hSpace;
                    rect.y += vSpace; rect.height = EditorGUIUtility.singleLineHeight;

                    rect.width = sharedWidth / 2;
                    SerializedProperty stateSelProp = instProp.FindPropertyRelative(() => def.FullHash);
                    int currentState = GetStateHashIndex(stateSelProp.intValue);
                    int stateSelection = EditorGUI.Popup(rect, currentState, m_TargetStateNames);
                    if (currentState != stateSelection)
                        stateSelProp.intValue = m_TargetStates[stateSelection];

                    rect.x += rect.width + hSpace;
                    SerializedProperty vcamSelProp = instProp.FindPropertyRelative(() => def.Camera);
                    int currentVcam = GetCameraIndex(vcamSelProp.objectReferenceValue);
                    int vcamSelection = EditorGUI.Popup(rect, currentVcam, m_CameraCandidates);
                    if (currentVcam != vcamSelection)
                        vcamSelProp.objectReferenceValue = (vcamSelection == 0)
                            ? null : Target.ChildCameras[vcamSelection - 1];

                    float oldWidth = EditorGUIUtility.labelWidth;
                    EditorGUIUtility.labelWidth = hBigSpace;

                    rect.x += rect.width; rect.width = floatFieldWidth + hBigSpace;
                    SerializedProperty activeAfterProp = instProp.FindPropertyRelative(() => def.ActivateAfter);
                    EditorGUI.PropertyField(rect, activeAfterProp, new GUIContent(" ", activeAfterProp.tooltip));

                    rect.x += rect.width;
                    SerializedProperty minDurationProp = instProp.FindPropertyRelative(() => def.MinDuration);
                    EditorGUI.PropertyField(rect, minDurationProp, new GUIContent(" ", minDurationProp.tooltip));

                    EditorGUIUtility.labelWidth = oldWidth;
                };

            m_InstructionList.onAddDropdownCallback = (Rect buttonRect, UnityEditorInternal.ReorderableList l) =>
                {
                    var menu = new GenericMenu();
                    menu.AddItem(new GUIContent("New State"),
                        false, (object data) =>
                    {
                        ++m_InstructionList.serializedProperty.arraySize;
                        serializedObject.ApplyModifiedProperties();
                        Target.ValidateInstructions();
                    },
                        null);
                    menu.AddItem(new GUIContent("All Unhandled States"),
                        false, (object data) =>
                    {
                        CinemachineStateDrivenCamera target = Target;
                        int len = m_InstructionList.serializedProperty.arraySize;
                        for (var i = 0; i < m_TargetStates.Length; ++i)
                        {
                            int hash = m_TargetStates[i];
                            if (hash == 0)
                                continue;
                            bool alreadyThere = false;
                            for (int j = 0; j < len; ++j)
                            {
                                if (target.Instructions[j].FullHash == hash)
                                {
                                    alreadyThere = true;
                                    break;
                                }
                            }
                            if (!alreadyThere)
                            {
                                int index = m_InstructionList.serializedProperty.arraySize;
                                ++m_InstructionList.serializedProperty.arraySize;
                                SerializedProperty p = m_InstructionList.serializedProperty.GetArrayElementAtIndex(index);
                                p.FindPropertyRelative(() => def.FullHash).intValue = hash;
                            }
                        }
                        serializedObject.ApplyModifiedProperties();
                        Target.ValidateInstructions();
                    },
                        null);
                    menu.ShowAsContext();
                };
        }
    }
}
#endif
