#if CINEMACHINE_EXPERIMENTAL_VCAM
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEditor;
using Cinemachine.Editor;
using Cinemachine.Utility;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace Cinemachine
{
    [CustomEditor(typeof(CmCamera))]
    [CanEditMultipleObjects]
    sealed class CmCameraEditor : UnityEditor.Editor 
    {
        /// <summary>
        /// The target object, cast as the same class as the object being edited
        /// </summary>
        CmCamera Target => target as CmCamera;

        public VisualTreeAsset inspectorXML;
        int m_StageSelection;
        public override VisualElement CreateInspectorGUI()
        {
            // Create a new VisualElement to be the root of our inspector UI
            var myInspector = new VisualElement();
            // Load from default reference
            inspectorXML.CloneTree(myInspector);

            // Inject procedural behaviours into myInspector
            var dropdownBlock = myInspector.Q("ProceduralMotionBlock");
            
            var cmCamera = Target;
            FindStages(cmCamera);
            for (var i = 0; i < s_Stages.Length; ++i)
            {
                var stage = i; // need local copy for lambda expression
                var dropdown = new DropdownField
                {
                    name = s_Stages[stage] + " Component Selection list",
                    label = s_Stages[stage],
                    choices = s_StageTypeNames[stage],
                    index = s_SelectionCache[stage],
                };
                dropdown.AddToClassList("unity-base-field__aligned");
                
                dropdown.RegisterValueChangedCallback(
                    evt => HandleDropdownSelection(evt.newValue, evt.previousValue, stage, cmCamera));
                dropdownBlock.Add(dropdown);
            }
            
            // Return the finished inspector UI
            return myInspector;
        }

        static void HandleDropdownSelection(string selection, string previousSelection, int stage, CmCamera cmCam)
        {
            if (previousSelection.Equals(selection)) return; // no change
            
            int index = GetTypeIndexFromSelection(selection, stage);
            // set vcam according to selected component
            var type = s_StageTypes[stage][index];

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
                for (var j = 0; j < s_StageTypeNames[stage].Count; ++j)
                    if (s_StageTypeNames[stage][j].Equals(selection))
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
            return m_LensSettingsInspectorHelper != null && m_LensSettingsInspectorHelper.UseHorizontalFOV;
        }
#endif
        
        static string[] s_Stages;
        static List<string>[] s_StageTypeNames;
        static List<Type>[] s_StageTypes;
        static List<int> s_SelectionCache;
        static void FindStages(CmCamera cmCamera)
        {
            s_Stages = Enum.GetNames(typeof(CinemachineCore.Stage));
            s_StageTypeNames = new List<string>[Enum.GetValues(typeof(CinemachineCore.Stage)).Length];
            s_StageTypes = new List<Type>[Enum.GetValues(typeof(CinemachineCore.Stage)).Length];
            s_SelectionCache = new List<int>(s_Stages.Length);
            for (int i = 0; i < s_StageTypeNames.Length; ++i)
            {
                s_StageTypeNames[i] = new List<string> { "Do Nothing" };
                s_StageTypes[i] = new List<Type>{null};
                s_SelectionCache.Add(0);
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
                s_StageTypes[stage].Add(t);
                s_StageTypeNames[stage].Add(InspectorUtility.NicifyClassName(t.Name));
            }

            for (int i = 0; i < s_SelectionCache.Count; ++i)
            {
                var component = cmCamera.GetCinemachineComponent((CinemachineCore.Stage)i);
                s_SelectionCache[i] = component == null ? 0 : GetTypeIndexFromSelection(component.GetType(), i);
            }
            
            static int GetTypeIndexFromSelection(Type type, int stage)
            {
                for (var j = 0; j < s_StageTypes[stage].Count; ++j)
                    if (s_StageTypes[stage][j] == type)
                        return j;
                return 0;
            }
        }
    }
}
#endif
