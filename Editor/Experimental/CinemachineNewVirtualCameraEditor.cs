#if CINEMACHINE_EXPERIMENTAL_VCAM
using UnityEngine;
using UnityEditor;
using Cinemachine.Editor;
using System.Linq;

namespace Cinemachine
{
    [CustomEditor(typeof(CinemachineNewVirtualCamera))]
    [CanEditMultipleObjects]
    internal sealed class CinemachineNewVirtualCameraEditor
        : CinemachineVirtualCameraBaseEditor<CinemachineNewVirtualCamera>
    {
        VcamStageEditorPipeline m_PipelineSet = new VcamStageEditorPipeline();

        protected override void OnEnable()
        {
            base.OnEnable();
            Undo.undoRedoPerformed += ResetTargetOnUndo;
            m_PipelineSet.Initialize(
                // GetComponent
                (stage, result) =>
                {
                    int numNullComponents = 0;
                    foreach (var obj in targets)
                    {
                        var vcam = obj as CinemachineNewVirtualCamera;
                        if (vcam != null)
                        {
                            var c = vcam.GetCinemachineComponent(stage);
                            if (c != null)
                                result.Add(c);
                            else
                                ++numNullComponents;
                        }
                    }
                    return numNullComponents;
                },
                // SetComponent
                (stage, type) => 
                {
                    Undo.SetCurrentGroupName("Cinemachine pipeline change");
                    foreach (var obj in targets)
                    {
                        var vcam = obj as CinemachineNewVirtualCamera;
                        if (vcam != null)
                        {
                            Component c = vcam.GetCinemachineComponent(stage);
                            if (c != null && c.GetType() == type)
                                continue;
                            if (c != null)
                            {
                                Undo.DestroyObjectImmediate(c);
                                vcam.InvalidateComponentCache();
                            }
                            if (type != null)
                            {
                                Undo.AddComponent(vcam.gameObject, type);
                                vcam.InvalidateComponentCache();
                            }
                        }
                    }
                });
            
#if UNITY_2021_2_OR_NEWER
            CinemachineSceneToolUtility.RegisterTool(typeof(SoloVcamTool));
            CinemachineSceneToolUtility.RegisterTool(typeof(FoVTool));
            CinemachineSceneToolUtility.RegisterTool(typeof(FarNearClipTool));
#endif
        }

        protected override void OnDisable()
        {
            Undo.undoRedoPerformed -= ResetTargetOnUndo;
            m_PipelineSet.Shutdown();
            base.OnDisable();
            
#if UNITY_2021_2_OR_NEWER
            CinemachineSceneToolUtility.UnregisterTool(typeof(SoloVcamTool));
            CinemachineSceneToolUtility.UnregisterTool(typeof(FoVTool));
            CinemachineSceneToolUtility.UnregisterTool(typeof(FarNearClipTool));
#endif
        }

        void ResetTargetOnUndo() 
        {
            ResetTarget();
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
            m_PipelineSet.OnInspectorGUI(true);
            DrawExtensionsWidgetInInspector();
        }

#if UNITY_2021_2_OR_NEWER
        void OnSceneGUI()
        {
            DrawSceneTools();
            m_PipelineSet.OnSceneGUI(); // call hidden editors
        }

        float m_Fov; // needed for reversing the scale slider
        bool m_SoloSetByTools;
        public void DrawSceneTools()
        {
            var vcam = Target;
            if (vcam == null || !vcam.IsValid || vcam.m_ExcludedPropertiesInInspector.Contains("m_Lens"))
            {
                return;
            }

            var originalColor = Handles.color;
            Handles.color = Handles.preselectionColor;
            if (CinemachineSceneToolUtility.IsToolActive(typeof(FoVTool)))
            {
                if (GUIUtility.hotControl == 0)
                {
                    m_Fov = Target.m_Lens.Orthographic ? Target.m_Lens.OrthographicSize : Target.m_Lens.FieldOfView;
                }
                
                CinemachineSceneToolHelpers.FovToolHandle(vcam, ref vcam.m_Lens, 
                    m_LensSettingsInspectorHelper == null ? false : m_LensSettingsInspectorHelper.UseHorizontalFOV,
                    ref m_Fov, ref m_SoloSetByTools);
            }
            else if (CinemachineSceneToolUtility.IsToolActive(typeof(FarNearClipTool)))
            {
                CinemachineSceneToolHelpers.NearFarClipHandle(vcam, ref vcam.m_Lens, ref m_SoloSetByTools);
            }
            Handles.color = originalColor;
        }
#endif
    }
}
#endif
