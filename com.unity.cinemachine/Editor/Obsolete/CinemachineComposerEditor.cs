#if !CINEMACHINE_NO_CM2_SUPPORT
using UnityEditor;

namespace Unity.Cinemachine.Editor
{
    [System.Obsolete]
    [CustomEditor(typeof(CinemachineComposer))]
    [CanEditMultipleObjects]
    class CinemachineComposerEditor : BaseEditor<CinemachineComposer>
    {
        GameViewComposerGuides m_GameViewGuides = new();

        protected virtual void OnEnable()
        {
            m_GameViewGuides.GetComposition = () => Target.Composition;
            m_GameViewGuides.SetComposition = (s) => Target.Composition = s;
            m_GameViewGuides.Target = () => serializedObject;
            m_GameViewGuides.OnEnable();

            CinemachineDebug.OnGUIHandlers -= OnGuiHandler;
            CinemachineDebug.OnGUIHandlers += OnGuiHandler;
            if (CinemachineCorePrefs.ShowInGameGuides.Value)
                InspectorUtility.RepaintGameView();
   
            CinemachineSceneToolUtility.RegisterTool(typeof(TrackedObjectOffsetTool));
        }

        protected virtual void OnDisable()
        {
            m_GameViewGuides.OnDisable();
            CinemachineDebug.OnGUIHandlers -= OnGuiHandler;
            if (CinemachineCorePrefs.ShowInGameGuides.Value)
                InspectorUtility.RepaintGameView();
  
            CinemachineSceneToolUtility.UnregisterTool(typeof(TrackedObjectOffsetTool));
        }

        public override void OnInspectorGUI()
        {
            BeginInspector();
            bool needWarning = false;
            for (int i = 0; !needWarning && i < targets.Length; ++i)
                needWarning = (targets[i] as CinemachineComposer).LookAtTarget == null;
            if (needWarning)
                EditorGUILayout.HelpBox(
                    "A LookAt target is required.  Change Aim to Do Nothing if you don't want a LookAt target.",
                    MessageType.Warning);

            // Draw the properties
            DrawRemainingPropertiesInInspector();
        }

        protected virtual void OnGuiHandler(CinemachineBrain brain)
        {
            // Draw the camera guides
            if (Target == null || !CinemachineCorePrefs.ShowInGameGuides.Value)
                return;

            // If inspector is collapsed in the vcam editor, don't draw the guides
            if (!VcamStageEditor.ActiveEditorRegistry.IsActiveEditor(this))
                return;

            // Don't draw the guides if rendering to texture
            if (brain == null || (brain.OutputCamera.activeTexture != null && CinemachineBrain.ActiveBrainCount > 1))
                return;

            var vcam = Target.VirtualCamera;
            if (!brain.IsLiveChild(vcam))
                return;

            // Screen guides
            bool isLive = targets.Length <= 1 && brain.IsLiveChild(vcam, true);
            m_GameViewGuides.OnGUI_DrawGuides(isLive, brain.OutputCamera, vcam.State.Lens);

            // Draw an on-screen gizmo for the target
            if (Target.LookAtTarget != null && isLive)
            {
                CmPipelineComponentInspectorUtility.OnGUI_DrawOnscreenTargetMarker(
                    Target.LookAtTargetAsGroup, Target.TrackedPoint, 
                    vcam.State.GetFinalOrientation(), brain.OutputCamera);
            }
        }

        void OnSceneGUI()
        {
            if (Target == null || !Target.IsValid)
                return;

            if (CinemachineSceneToolUtility.IsToolActive(typeof(TrackedObjectOffsetTool)))
            {
                CinemachineSceneToolHelpers.TrackedObjectOffsetTool(
                    Target.VirtualCamera, 
                    new SerializedObject(Target).FindProperty(() => Target.m_TrackedObjectOffset),
                    CinemachineCore.Stage.Aim);
            }
        }
    }
}
#endif
