using UnityEditor;
using UnityEngine.UIElements;

namespace Cinemachine.Editor
{
    [CustomEditor(typeof(CinemachinePixelPerfect))]
    [CanEditMultipleObjects]
    class CinemachinePixelPerfectEditor : UnityEditor.Editor
    {
        CmPipelineComponentInspectorUtility m_PipelineUtility;

        void OnEnable() => m_PipelineUtility = new (this);
        void OnDisable() => m_PipelineUtility.OnDisable();

        public override VisualElement CreateInspectorGUI()
        {
            var ux = new VisualElement();

#if CINEMACHINE_LWRP_7_3_1 || CINEMACHINE_PIXEL_PERFECT_2_0_3
            m_PipelineUtility.AddMissingCmCameraHelpBox(ux);
            m_PipelineUtility.UpdateState();
#else
            ux.Add(new HelpBox("This component is only valid within URP projects", HelpBoxMessageType.Warning));
#endif
            return ux;
        }
    }
}
