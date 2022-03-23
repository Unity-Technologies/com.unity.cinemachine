#if CINEMACHINE_EXPERIMENTAL_VCAM
using UnityEngine;
using UnityEditor;
using Cinemachine.Editor;

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
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
        
            var vcam = Target;
            CinemachineComponentBaseEditorUtility.DrawPopups(vcam);
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
        LensSettingsInspectorHelper m_LensSettingsInspectorHelper = new LensSettingsInspectorHelper();
        bool IsHorizontalFOVUsed() => 
            m_LensSettingsInspectorHelper != null && m_LensSettingsInspectorHelper.UseHorizontalFOV;
#endif
    }
}
#endif
