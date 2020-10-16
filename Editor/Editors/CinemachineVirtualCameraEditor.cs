using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;
using Cinemachine.Utility;
using System.Reflection;
using System.Linq;

namespace Cinemachine.Editor
{
    [CustomEditor(typeof(CinemachineVirtualCamera))]
    [CanEditMultipleObjects]
    internal class CinemachineVirtualCameraEditor
        : CinemachineVirtualCameraBaseEditor<CinemachineVirtualCamera>
    {
        // Static state and caches - Call UpdateStaticData() to refresh this
        struct StageData
        {
            string ExpandedKey { get { return "CNMCN_Core_Vcam_Expanded_" + Name; } }
            public bool IsExpanded
            {
                get { return EditorPrefs.GetBool(ExpandedKey, false); }
                set { EditorPrefs.SetBool(ExpandedKey, value); }
            }
            public string Name;
            public Type[] types;   // first entry is null
            public GUIContent[] PopupOptions;
        }
        static StageData[] sStageData = null;
        bool[] m_hasSameStageDataTypes = new bool[Enum.GetValues(typeof(CinemachineCore.Stage)).Length];

        // Instance data - call UpdateInstanceData() to refresh this
        int[] m_stageState = null;
        bool[] m_stageError = null;
        CinemachineComponentBase[] m_components;
        UnityEditor.Editor[] m_componentEditors = new UnityEditor.Editor[0];
        bool IsPrefab { get; set; }

        protected override void OnEnable()
        {
            // Build static menu arrays via reflection
            base.OnEnable();
            IsPrefab = Target.gameObject.scene.name == null; // causes a small GC alloc

            UpdateStaticData();
            UpdateStageDataTypeMatchesForMultiSelection();
            Undo.undoRedoPerformed += ResetTargetOnUndo;
        }

        void ResetTargetOnUndo() 
        {
            UpdateInstanceData();
            ResetTarget();
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            Undo.undoRedoPerformed -= ResetTargetOnUndo;
            // Must destroy editors or we get exceptions
            if (m_componentEditors != null)
                foreach (UnityEditor.Editor e in m_componentEditors)
                    if (e != null)
                        UnityEngine.Object.DestroyImmediate(e);
        }

        Vector3 mPreviousPosition;
        private void OnSceneGUI()
        {
            if (!Target.UserIsDragging)
                mPreviousPosition = Target.transform.position;
            if (Selection.Contains(Target.gameObject) && Tools.current == Tool.Move
                && Event.current.type == EventType.MouseDrag)
            {
                // User might be dragging our position handle
                Target.UserIsDragging = true;
                Vector3 delta = Target.transform.position - mPreviousPosition;
                if (!delta.AlmostZero())
                {
                    OnPositionDragged(delta);
                    mPreviousPosition = Target.transform.position;
                }
            }
            else if (GUIUtility.hotControl == 0 && Target.UserIsDragging)
            {
                // We're not dragging anything now, but we were
                InspectorUtility.RepaintGameView();
                Target.UserIsDragging = false;
            }
        }

        [MenuItem("CONTEXT/CinemachineVirtualCamera/Adopt Current Camera Settings")]
        static void AdoptCurrentCameraSettings(MenuCommand command)
        {
            var vcam = command.context as CinemachineVirtualCamera;
            var brain = CinemachineCore.Instance.FindPotentialTargetBrain(vcam);
            if (brain != null)
            {
                vcam.m_Lens = brain.CurrentCameraState.Lens;
                vcam.transform.position = brain.transform.position;
                vcam.transform.rotation = brain.transform.rotation;
            }
        }

        [MenuItem("CONTEXT/CinemachineVirtualCamera/Adopt Scene View Camera Settings")]
        static void AdoptSceneViewCameraSettings(MenuCommand command)
        {
            var vcam = command.context as CinemachineVirtualCamera;
            CinemachineMenu.SetVcamFromSceneView(vcam);
        }

        void OnPositionDragged(Vector3 delta)
        {
            if (m_componentEditors != null)
            {
                foreach (UnityEditor.Editor e in m_componentEditors)
                {
                    if (e != null)
                    {
                        MethodInfo mi = e.GetType().GetMethod("OnVcamPositionDragged"
                            , BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
                        if (mi != null && e.target != null)
                        {
                            mi.Invoke(e, new object[] { delta } );
                        }
                    }
                }
            }
        }

        public override void OnInspectorGUI()
        {
            BeginInspector();
            DrawHeaderInInspector();
            DrawPropertyInInspector(FindProperty(x => x.m_Priority));
            DrawTargetsInInspector(FindProperty(x => x.m_Follow), FindProperty(x => x.m_LookAt));
            DrawPropertyInInspector(FindProperty(x => x.m_StandbyUpdate));
            DrawLensSettingsInInspector(FindProperty(x => x.m_Lens));
            DrawRemainingPropertiesInInspector();
            DrawPipelineInInspector();
            DrawExtensionsWidgetInInspector();
        }

        protected void DrawPipelineInInspector()
        {
            UpdateInstanceData();
            foreach (CinemachineCore.Stage stage in Enum.GetValues(typeof(CinemachineCore.Stage)))
            {
                int index = (int)stage;

                // Skip pipeline stages that have no implementations
                if (index < 0 || sStageData[index].PopupOptions.Length <= 1)
                    continue;

                const float indentOffset = 3;

                GUIStyle stageBoxStyle = GUI.skin.box;
                EditorGUILayout.BeginVertical(stageBoxStyle);
                Rect rect = EditorGUILayout.GetControlRect(true);

                // Don't use PrefixLabel() because it will link the enabled status of field and label
                GUIContent label = new GUIContent(InspectorUtility.NicifyClassName(stage.ToString()));
                if (m_stageError[index])
                    label.image = EditorGUIUtility.IconContent("console.warnicon.sml").image;
                float labelWidth = EditorGUIUtility.labelWidth - (indentOffset + EditorGUI.indentLevel * 15);
                Rect r = rect; r.width = labelWidth;
                EditorGUI.LabelField(r, label);
                r = rect; r.width -= labelWidth; r.x += labelWidth;

                EditorGUI.BeginChangeCheck();
                GUI.enabled = !StageIsLocked(stage);
                EditorGUI.showMixedValue = !m_hasSameStageDataTypes[index];
                int newSelection = EditorGUI.Popup(r, m_stageState[index], sStageData[index].PopupOptions);
                EditorGUI.showMixedValue = false;
                GUI.enabled = true;
                Type type = sStageData[index].types[newSelection];
                if (EditorGUI.EndChangeCheck())
                {
                    SetPipelineStage(stage, type);
                    if (newSelection != 0)
                        sStageData[index].IsExpanded = true;
                    UpdateInstanceData(); // because we changed it
                    ResetTarget(); // to allow multi-selection correctly adjust every target 

                    return;
                }
                if (type != null)
                {
                    Rect stageRect = new Rect(
                        rect.x - indentOffset, rect.y, rect.width + indentOffset, rect.height);
                    sStageData[index].IsExpanded = EditorGUI.Foldout(
                            stageRect, sStageData[index].IsExpanded, GUIContent.none, true);
                    if (sStageData[index].IsExpanded)
                    {
                        // Make the editor for that stage
                        UnityEditor.Editor e = GetEditorForPipelineStage(stage);
                        if (e != null)
                        {
                            ++EditorGUI.indentLevel;
                            EditorGUILayout.Separator();
                            e.OnInspectorGUI();

                            EditorGUILayout.Separator();
                            --EditorGUI.indentLevel;
                        }
                    }
                }
                EditorGUILayout.EndVertical();
            }
        }

        bool StageIsLocked(CinemachineCore.Stage stage)
        {
            if (IsPrefab)
                return true;
            CinemachineCore.Stage[] locked = Target.m_LockStageInInspector;
            if (locked != null)
                for (int i = 0; i < locked.Length; ++i)
                    if (locked[i] == stage)
                        return true;
            return false;
        }

        UnityEditor.Editor GetEditorForPipelineStage(CinemachineCore.Stage stage)
        {
            if (m_componentEditors != null)
            {
                foreach (UnityEditor.Editor e in m_componentEditors)
                {
                    if (e != null)
                    {
                        CinemachineComponentBase c = e.target as CinemachineComponentBase;
                        if (c != null && c.Stage == stage)
                            return e;
                    }
                }
            }
            return null;
        }
        
        /// <summary>
        /// Register with CinemachineVirtualCamera to create the pipeline in an undo-friendly manner
        /// </summary>
        [InitializeOnLoad]
        class CreatePipelineWithUndo
        {
            static CreatePipelineWithUndo()
            {
                CinemachineVirtualCamera.CreatePipelineOverride =
                    (CinemachineVirtualCamera vcam, string name, CinemachineComponentBase[] copyFrom) =>
                    {
                        // Create a new pipeline
                        GameObject go =  InspectorUtility.CreateGameObject(name);
                        Undo.RegisterCreatedObjectUndo(go, "created pipeline");
                        Undo.SetTransformParent(go.transform, vcam.transform, "parenting pipeline");
                        Undo.AddComponent<CinemachinePipeline>(go);

                        // If copying, transfer the components
                        if (copyFrom != null)
                        {
                            foreach (Component c in copyFrom)
                            {
                                Component copy = Undo.AddComponent(go, c.GetType());
                                Undo.RecordObject(copy, "copying pipeline");
                                ReflectionHelpers.CopyFields(c, copy);
                            }
                        }
                        return go.transform;
                    };
                CinemachineVirtualCamera.DestroyPipelineOverride = (GameObject pipeline) =>
                    {
                        Undo.DestroyObjectImmediate(pipeline);
                    };
            }
        }

        void SetPipelineStage(CinemachineCore.Stage stage, Type type)
        {
            Undo.SetCurrentGroupName("Cinemachine pipeline change");

            // Get the existing components
            for(int j = 0; j < targets.Length; j++)
            {
                var vCam = targets[j] as CinemachineVirtualCamera;
                Transform owner = vCam.GetComponentOwner();
                if (owner == null)
                    continue; // maybe it's a prefab

                CinemachineComponentBase[] components = owner.GetComponents<CinemachineComponentBase>();
                if (components == null)
                    components = new CinemachineComponentBase[0];

                // Find an appropriate insertion point
                int numComponents = components.Length;
                int insertPoint = 0;
                for (insertPoint = 0; insertPoint < numComponents; ++insertPoint)
                    if (components[insertPoint].Stage >= stage)
                        break;

                // Remove the existing components at that stage
                for (int i = numComponents - 1; i >= 0; --i)
                {
                    if (components[i].Stage == stage)
                    {
                        Undo.DestroyObjectImmediate(components[i]);
                        components[i] = null;
                        --numComponents;
                        if (i < insertPoint)
                            --insertPoint;
                    }
                }

                // Add the new stage
                if (type != null)
                {
                    MonoBehaviour b = Undo.AddComponent(owner.gameObject, type) as MonoBehaviour;

                    while (numComponents-- > insertPoint)
                        UnityEditorInternal.ComponentUtility.MoveComponentDown(b);
                }
            }
        }

        // This code dynamically discovers eligible classes and builds the menu
        // data for the various component pipeline stages.
        static void UpdateStaticData()
        {
            if (sStageData != null)
                return;
            sStageData = new StageData[Enum.GetValues(typeof(CinemachineCore.Stage)).Length];

            var stageTypes = new List<Type>[Enum.GetValues(typeof(CinemachineCore.Stage)).Length];
            for (int i = 0; i < stageTypes.Length; ++i)
            {
                sStageData[i].Name = ((CinemachineCore.Stage)i).ToString();
                stageTypes[i] = new List<Type>();
            }

            // Get all ICinemachineComponents
            var allTypes
                = ReflectionHelpers.GetTypesInAllDependentAssemblies(
                        (Type t) => typeof(CinemachineComponentBase).IsAssignableFrom(t) && !t.IsAbstract);

            // Create a temp game object so we can instance behaviours
            GameObject go = new GameObject("Cinemachine Temp Object");
            go.hideFlags = HideFlags.DontSaveInBuild | HideFlags.DontSaveInEditor;
            foreach (Type t in allTypes)
            {
                MonoBehaviour b = go.AddComponent(t) as MonoBehaviour;
                CinemachineComponentBase c = b != null ? (CinemachineComponentBase)b : null;
                if (c != null)
                {
                    CinemachineCore.Stage stage = c.Stage;
                    stageTypes[(int)stage].Add(t);
                }
            }
            GameObject.DestroyImmediate(go);

            // Create the static lists
            for (int i = 0; i < stageTypes.Length; ++i)
            {
                stageTypes[i].Insert(0, null);  // first item is "none"
                sStageData[i].types = stageTypes[i].ToArray();
                GUIContent[] names = new GUIContent[sStageData[i].types.Length];
                for (int n = 0; n < names.Length; ++n)
                {
                    if (n == 0)
                    {
                        bool useSimple
                            = (i == (int)CinemachineCore.Stage.Aim)
                                || (i == (int)CinemachineCore.Stage.Body);
                        names[n] = new GUIContent((useSimple) ? "Do nothing" : "none");
                    }
                    else
                        names[n] = new GUIContent(InspectorUtility.NicifyClassName(sStageData[i].types[n].Name));
                }
                sStageData[i].PopupOptions = names;
            }
        }

        void GetPipelineTypes(CinemachineVirtualCamera vcam, ref Type[] types)
        {
            for (int i = 0; i < types.Length; ++i)
                types[i] = null;
            if (vcam != null)
            {
                var components = vcam.GetComponentPipeline();
                for (int j = 0; j < components.Length; ++j)
                    types[(int)components[j].Stage] = components[j].GetType();
            }
        }

        // scratch buffers for pipeline types
        Type[] m_PipelineTypeCache0 = new Type[Enum.GetValues(typeof(CinemachineCore.Stage)).Length];
        Type[] m_PipelineTypeCacheN = new Type[Enum.GetValues(typeof(CinemachineCore.Stage)).Length];

        void UpdateStageDataTypeMatchesForMultiSelection()
        {
            for (int i = 0; i < m_hasSameStageDataTypes.Length; ++i)
                m_hasSameStageDataTypes[i] = true;

            if (targets.Length > 1)
            {
                GetPipelineTypes(serializedObject.targetObjects[0] as CinemachineVirtualCamera, ref m_PipelineTypeCache0);
                for (int i = 1; i < targets.Length; ++i)
                {
                    GetPipelineTypes(serializedObject.targetObjects[i] as CinemachineVirtualCamera, ref m_PipelineTypeCacheN);
                    for (int j = 0; j < m_PipelineTypeCache0.Length; ++j)
                        if (m_PipelineTypeCache0[j] != m_PipelineTypeCacheN[j])
                            m_hasSameStageDataTypes[j] = false;
                }
            }
        }

        void UpdateInstanceData()
        {
            // Invalidate the target's cache - this is to support Undo
            for (int i = 0; i < targets.Length; i++)
            {
                var cam = targets[i] as CinemachineVirtualCamera;
                if(cam != null)
                    cam.InvalidateComponentPipeline();
            }
            UpdateStageDataTypeMatchesForMultiSelection();
            UpdateComponentEditors();
            UpdateStageState(m_components);
        }

        // This code dynamically builds editors for the pipeline components.
        // Expansion state is cached statically to preserve foldout state.
        void UpdateComponentEditors()
        {
            if (Target == null)
            {
                m_components = new CinemachineComponentBase[0];
                return;
            }
            CinemachineComponentBase[] components = Target.GetComponentPipeline();
            int numComponents = components != null ? components.Length : 0;
            if (m_components == null || m_components.Length != numComponents)
                m_components = new CinemachineComponentBase[numComponents];
            bool dirty = (numComponents == 0);
            for (int i = 0; i < numComponents; ++i)
            {
                if (m_components[i] == null || components[i] != m_components[i])
                {
                    dirty = true;
                    m_components[i] = components[i];
                }
            }
            if (dirty)
            {
                // Destroy the subeditors
                if (m_componentEditors != null)
                    foreach (UnityEditor.Editor e in m_componentEditors)
                        if (e != null)
                            UnityEngine.Object.DestroyImmediate(e);

                // Create new editors
                m_componentEditors = new UnityEditor.Editor[numComponents];
                for (int i = 0; i < numComponents; ++i)
                {
                    List<MonoBehaviour> behaviours = new List<MonoBehaviour>();
                    for (int j = 0; j < targets.Length; j++)
                    {
                        var cinemachineVirtualCamera = targets[j] as CinemachineVirtualCamera;
                        if (cinemachineVirtualCamera == null)
                            continue;

                        var behaviour = cinemachineVirtualCamera.GetCinemachineComponent(components[i].Stage) as MonoBehaviour;
                        if (behaviour != null)
                            behaviours.Add(behaviour);
                    }

                    var behaviourArray = behaviours.ToArray();
                    if (behaviourArray.Length > 0 && m_hasSameStageDataTypes[(int)components[i].Stage])
                        CreateCachedEditor(behaviourArray, null, ref m_componentEditors[i]);
                }
            }
        }

        void UpdateStageState(CinemachineComponentBase[] components)
        {
            m_stageState = new int[Enum.GetValues(typeof(CinemachineCore.Stage)).Length];
            m_stageError = new bool[Enum.GetValues(typeof(CinemachineCore.Stage)).Length];
            foreach (var c in components)
            {
                CinemachineCore.Stage stage = c.Stage;
                int index = 0;
                for (index = sStageData[(int)stage].types.Length - 1; index > 0; --index)
                    if (sStageData[(int)stage].types[index] == c.GetType())
                        break;
                m_stageState[(int)stage] = index;
                m_stageError[(int)stage] = c == null || !c.IsValid;
            }
        }

        // Because the cinemachine components are attached to hidden objects, their
        // gizmos don't get drawn by default.  We have to do it explicitly.
        [InitializeOnLoad]
        static class CollectGizmoDrawers
        {
            static CollectGizmoDrawers()
            {
                m_GizmoDrawers = new Dictionary<Type, MethodInfo>();
                string definedIn = typeof(CinemachineComponentBase).Assembly.GetName().Name;
                Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
                foreach (Assembly assembly in assemblies)
                {
                    // Note that we have to call GetName().Name.  Just GetName() will not work.
                    if ((!assembly.GlobalAssemblyCache)
                        && ((assembly.GetName().Name == definedIn)
                            || assembly.GetReferencedAssemblies().Any(a => a.Name == definedIn)))
                    {
                        try
                        {
                            foreach (var type in assembly.GetTypes())
                            {
                                try
                                {
                                    bool added = false;
                                    foreach (var method in type.GetMethods(
                                                 BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static))
                                    {
                                        if (added)
                                            break;
                                        if (!method.IsStatic)
                                            continue;
                                        var attributes = method.GetCustomAttributes(typeof(DrawGizmo), true) as DrawGizmo[];
                                        foreach (var a in attributes)
                                        {
                                            if (typeof(CinemachineComponentBase).IsAssignableFrom(a.drawnType) && !a.drawnType.IsAbstract)
                                            {
                                                m_GizmoDrawers.Add(a.drawnType, method);
                                                added = true;
                                                break;
                                            }
                                        }
                                    }
                                }
                                catch (System.Exception) {} // Just skip uncooperative types
                            }
                        }
                        catch (System.Exception) {} // Just skip uncooperative assemblies
                    }
                }
            }
            public static Dictionary<Type, MethodInfo> m_GizmoDrawers;
        }

        [DrawGizmo(GizmoType.Active | GizmoType.InSelectionHierarchy, typeof(CinemachineVirtualCamera))]
        internal static void DrawVirtualCameraGizmos(CinemachineVirtualCamera vcam, GizmoType selectionType)
        {
            var pipeline = vcam.GetComponentPipeline();
            if (pipeline != null)
            {
                foreach (var c in pipeline)
                {
                    if (c == null)
                        continue;

                    MethodInfo method;
                    if (CollectGizmoDrawers.m_GizmoDrawers.TryGetValue(c.GetType(), out method))
                    {
                        if (method != null)
                            method.Invoke(null, new object[] {c, selectionType});
                    }
                }
            }
        }
    }
}
