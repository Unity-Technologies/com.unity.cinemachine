#if CINEMACHINE_EXPERIMENTAL_VCAM
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEditor;
using Cinemachine.Editor;
using Cinemachine.Utility;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

// TODO: think about property drawers ([attributes] that will format our stuff automatically)
// TODO: or do we want to write simple editors all the time, is it easier
// TODO: or baseEditor like solution?

namespace Cinemachine
{
    [CustomEditor(typeof(CmCamera))]
    [CanEditMultipleObjects]
    sealed class CmCameraEditor : BaseEditor<CmCamera>
    {
        public override VisualElement CreateInspectorGUI()
        {
            // Create a new VisualElement to be the root of our inspector UI
            var myInspector = new VisualElement();
            // inspectorXML.CloneTree(myInspector);
            var soloHolder = new IMGUIContainer();
            myInspector.Add(soloHolder);
            
            var serializedTarget = new SerializedObject(Target);
            m_PriorityField = new PropertyField(serializedTarget.FindProperty(() => Target.m_Priority));
            myInspector.Add(m_PriorityField);
            myInspector.Add(new PropertyField(serializedTarget.FindProperty(() => Target.m_Follow)));
            myInspector.Add(new PropertyField(serializedTarget.FindProperty(() => Target.m_LookAt)));
            myInspector.Add(new PropertyField(serializedTarget.FindProperty(() => Target.m_StandbyUpdate)));
            myInspector.Add(new PropertyField(serializedTarget.FindProperty(() => Target.m_Lens)));
            myInspector.Add(new PropertyField(serializedTarget.FindProperty(() => Target.m_Transitions)));

            // // Inject Status
            soloHolder.onGUIHandler = DrawCameraStatusInInspector;

            // Inject procedural behaviour dropdowns into myInspector
            
            var cmCamera = Target;
            FindStages(cmCamera);
            for (var i = 0; i < s_StageData.Length; ++i)
            {
                if (s_StageData[i].type.Count <= 1) continue;
                
                var stage = i; // need local copy for lambda expression
                var dropdown = new DropdownField
                {
                    name = s_StageData[stage].name + " Component Selection list",
                    label = s_StageData[stage].name,
                    choices = s_StageData[stage].typeName,
                    index = s_StageData[stage].selection,
                };
                dropdown.AddToClassList("unity-base-field__aligned");
                
                dropdown.RegisterValueChangedCallback(
                    evt => HandleDropdownSelection(evt.newValue, evt.previousValue, stage, cmCamera));
                myInspector.Add(dropdown);
            }
            
            // Return the finished inspector UI
            return myInspector;
        }

        static void HandleDropdownSelection(string selection, string previousSelection, int stage, CmCamera cmCam)
        {
            if (previousSelection.Equals(selection)) return; // no change
            
            int index = GetTypeIndexFromSelection(selection, stage);
            // set vcam according to selected component
            var type = s_StageData[stage].type[index];

            if (cmCam.m_Components[stage] != null)
            {
                Undo.DestroyObjectImmediate(cmCam.m_Components[stage]);
                cmCam.m_Components[stage] = null;
            }
            
            if (type != null)
            {
                var component = (CinemachineComponentBase) Undo.AddComponent(cmCam.gameObject, type);
                cmCam.m_Components[stage] = component;
            }
            
            static int GetTypeIndexFromSelection(string selection, int stage)
            {
                for (var j = 0; j < s_StageData[stage].typeName.Count; ++j)
                    if (s_StageData[stage].typeName[j].Equals(selection))
                        return j;
                return 0;
            }
        }
        
        void OnEnable()
        {
            Undo.undoRedoPerformed += ResetTarget;

#if UNITY_2021_2_OR_NEWER
            CinemachineSceneToolUtility.RegisterTool(typeof(FoVTool));
            CinemachineSceneToolUtility.RegisterTool(typeof(FarNearClipTool));
#endif
        }

        void OnDisable()
        {
            Undo.undoRedoPerformed -= ResetTarget;
            
#if UNITY_2021_2_OR_NEWER
            CinemachineSceneToolUtility.UnregisterTool(typeof(FoVTool));
            CinemachineSceneToolUtility.UnregisterTool(typeof(FarNearClipTool));
#endif
        }

        void OnSceneGUI()
        {
#if UNITY_2021_2_OR_NEWER
            DrawSceneTools();
#endif
        }

#if UNITY_2021_2_OR_NEWER
        void DrawSceneTools()
        {
            var cmCam = Target;
            if (cmCam == null)
            {
                return;
            }

            var originalColor = Handles.color;
            Handles.color = Handles.preselectionColor;
            if (CinemachineSceneToolUtility.IsToolActive(typeof(FoVTool)))
            {
                CinemachineSceneToolHelpers.FovToolHandle(cmCam, 
                    new SerializedObject(cmCam).FindProperty(() => cmCam.m_Lens), 
                    cmCam.m_Lens, IsHorizontalFOVUsed());
            }
            else if (CinemachineSceneToolUtility.IsToolActive(typeof(FarNearClipTool)))
            {
                CinemachineSceneToolHelpers.NearFarClipHandle(cmCam,
                    new SerializedObject(cmCam).FindProperty(() => cmCam.m_Lens));
            }
            Handles.color = originalColor;
        }

        // TODO: LensSettingsInspectorHelper does much more than we need!
        LensSettingsInspectorHelper m_LensSettingsInspectorHelper;
        bool IsHorizontalFOVUsed()
        {
            if (m_LensSettingsInspectorHelper == null)
                m_LensSettingsInspectorHelper = new LensSettingsInspectorHelper();
            return m_LensSettingsInspectorHelper.UseHorizontalFOV;
        }

        VisualElement m_PriorityField;
        void DrawCameraStatusInInspector()
        {
            if (Selection.objects.Length > 1)
                return;
            
            // Is the camera navel-gazing?
            CameraState state = Target.State;
            if (state.HasLookAt && (state.ReferenceLookAt - state.CorrectedPosition).AlmostZero())
                EditorGUILayout.HelpBox(
                    "The camera is positioned on the same point at which it is trying to look.",
                    MessageType.Warning);

            // TODO: No status and Solo for prefabs
            // if (IsPrefabBase)
            //     return;

            // Active status and Solo button
            Rect rect = EditorGUILayout.GetControlRect(true);
            Rect rectLabel = new Rect(rect.x+1f, rect.y, EditorGUIUtility.labelWidth, rect.height);
            rect.width -= rectLabel.width;
            rect.x += rectLabel.width;

            Color color = GUI.color;
            bool isSolo = (CinemachineBrain.SoloCamera == (ICinemachineCamera)Target);
            if (isSolo)
                GUI.color = CinemachineBrain.GetSoloGUIColor();

            bool isLive = CinemachineCore.Instance.IsLive(Target);
            GUI.enabled = isLive;
            GUI.Label(rectLabel, isLive ? "Status: Live"
                : (Target.isActiveAndEnabled ? "Status: Standby" : "Status: Disabled"));
            GUI.enabled = true;

            float labelWidth = 0;
            GUIContent updateText = GUIContent.none;
            UpdateTracker.UpdateClock updateMode = CinemachineCore.Instance.GetVcamUpdateStatus(Target);
            if (Application.isPlaying)
            {
                updateText = new GUIContent(
                    updateMode == UpdateTracker.UpdateClock.Fixed ? " Fixed Update" : " Late Update");
                var textDimensions = GUI.skin.label.CalcSize(updateText);
                labelWidth = textDimensions.x;
            }
            rect.width -= labelWidth;
            if (m_PriorityField.Children() != null)
            {
                // correcting the rect so it is aligned correctly with UI toolkit standard
                var correction = m_PriorityField.Children().ToArray()[0].Children().ToArray()[1];
                rect.width -= (correction.layout.x - rect.x);
                rect.x = correction.layout.x + 3;
            }

            if (GUI.Button(rect, "Solo", "Button"))
            {
                isSolo = !isSolo;
                CinemachineBrain.SoloCamera = isSolo ? Target : null;
                InspectorUtility.RepaintGameView();
            }
            GUI.color = color;
            if (isSolo && !Application.isPlaying)
                InspectorUtility.RepaintGameView();

            if (labelWidth > 0)
            {
                GUI.enabled = false;
                rect.x += rect.width; rect.width = labelWidth;
                GUI.Label(rect, updateText);
                GUI.enabled = true;
            }
        }

#endif

        struct StageData
        {
            public string name;
            public List<string> typeName;
            public List<Type> type;
            public int selection;
        }

        static StageData[] s_StageData;
        static void FindStages(CmCamera cmCamera)
        {
            var names = Enum.GetNames(typeof(CinemachineCore.Stage));
            s_StageData = new StageData[names.Length];
            for (int stage = 0; stage < s_StageData.Length; ++stage)
            {
                s_StageData[stage].name = names[stage];
                s_StageData[stage].typeName = new List<string> { "None" };
                s_StageData[stage].type = new List<Type> { null };
                s_StageData[stage].selection = 0;
            }

            // Get all ICinemachineComponents
            var allTypes
                = ReflectionHelpers.GetTypesInAllDependentAssemblies((Type t) => 
                    typeof(CinemachineComponentBase).IsAssignableFrom(t) 
                    && !t.IsAbstract && t.GetCustomAttribute<ObsoleteAttribute>() == null);
            
            foreach (var t in allTypes)
            {
                var pipelineAttribute = t.GetCustomAttribute<CameraPipelineAttribute>();
                var stage = (int)pipelineAttribute.Stage;
                s_StageData[stage].type.Add(t);
                s_StageData[stage].typeName.Add(InspectorUtility.NicifyClassName(t.Name));
            }

            for (int stage = 0; stage < s_StageData.Length; ++stage)
            {
                var component = cmCamera.GetCinemachineComponent((CinemachineCore.Stage)stage);
                s_StageData[stage].selection = component == null ? 0 : GetTypeIndexFromSelection(component.GetType(), stage);
            }
            
            static int GetTypeIndexFromSelection(Type type, int stage)
            {
                for (var j = 0; j < s_StageData[stage].type.Count; ++j)
                    if (s_StageData[stage].type[j] == type)
                        return j;
                return 0;
            }
        }
    }
}
#endif
