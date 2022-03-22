#if CINEMACHINE_EXPERIMENTAL_VCAM
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using Cinemachine.Editor;
using System.Linq;

namespace Cinemachine
{
    [CustomEditor(typeof(CmCamera))]
    [CanEditMultipleObjects]
    internal sealed class CinemachineNewVirtualCameraEditor
        : CinemachineVirtualCameraBaseEditor<CmCamera>
    {
        protected override void OnEnable()
        {
            base.OnEnable();
            Undo.undoRedoPerformed += ResetTargetOnUndo;

#if UNITY_2021_2_OR_NEWER
            CinemachineSceneToolUtility.RegisterTool(typeof(FoVTool));
            CinemachineSceneToolUtility.RegisterTool(typeof(FarNearClipTool));
#endif
        }

        protected override void OnDisable()
        {
            Undo.undoRedoPerformed -= ResetTargetOnUndo;
            base.OnDisable();
            
#if UNITY_2021_2_OR_NEWER
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

            var vcam = Target;
            CinemachineComponentBaseEditorUtility.DrawPopups(vcam);
            
            DrawRemainingPropertiesInInspector();
            DrawExtensionsWidgetInInspector();
        }

        void OnSceneGUI()
        {
            // TODO: OnSceneGUI calls for handles of components
            //m_PipelineSet.OnSceneGUI(); // call hidden editors
            
#if UNITY_2021_2_OR_NEWER
            DrawSceneTools();
#endif
        }

#if UNITY_2021_2_OR_NEWER
        void DrawSceneTools()
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
                CinemachineSceneToolHelpers.FovToolHandle(vcam, 
                    new SerializedObject(vcam).FindProperty(() => vcam.m_Lens), 
                    vcam.m_Lens, IsHorizontalFOVUsed());
            }
            else if (CinemachineSceneToolUtility.IsToolActive(typeof(FarNearClipTool)))
            {
                CinemachineSceneToolHelpers.NearFarClipHandle(vcam,
                    new SerializedObject(vcam).FindProperty(() => vcam.m_Lens));
            }
            Handles.color = originalColor;
        }
#endif
    }
}
#endif
