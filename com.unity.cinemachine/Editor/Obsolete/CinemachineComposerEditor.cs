using UnityEngine;
using UnityEditor;
using Cinemachine.Utility;

namespace Cinemachine.Editor
{
    [System.Obsolete]
    [CustomEditor(typeof(CinemachineComposer))]
    [CanEditMultipleObjects]
    class CinemachineComposerEditor : BaseEditor<CinemachineComposer>
    {
        protected virtual void OnEnable()
        {
            CinemachineDebug.OnGUIHandlers -= OnGUI;
            CinemachineDebug.OnGUIHandlers += OnGUI;
            if (CinemachineCorePrefs.ShowInGameGuides.Value)
                InspectorUtility.RepaintGameView();
   
            CinemachineSceneToolUtility.RegisterTool(typeof(TrackedObjectOffsetTool));
        }

        protected virtual void OnDisable()
        {
            CinemachineDebug.OnGUIHandlers -= OnGUI;
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

            // First snapshot some settings
            Rect oldHard = Target.HardGuideRect;
            Rect oldSoft = Target.SoftGuideRect;

            // Draw the properties
            DrawRemainingPropertiesInInspector();
        }

        protected virtual void OnGUI()
        {
            // Draw the camera guides
            if (Target == null || !CinemachineCorePrefs.ShowInGameGuides.Value)
                return;

            // If inspector is collapsed in the vcam editor, don't draw the guides
            if (!VcamStageEditor.ActiveEditorRegistry.IsActiveEditor(this))
                return;

            // Don't draw the guides if rendering to texture
            var vcam = Target.VirtualCamera;
            CinemachineBrain brain = CinemachineCore.Instance.FindPotentialTargetBrain(vcam);
            if (brain == null || (brain.OutputCamera.activeTexture != null && CinemachineCore.Instance.BrainCount > 1))
                return;

            // Draw an on-screen gizmo for the target
            bool isLive = targets.Length <= 1 && brain.IsLive(vcam, true);
            if (Target.LookAtTarget != null && isLive)
            {
                CmPipelineComponentInspectorUtility.OnGUI_DrawOnscreenTargetMarker(
                    Target.LookAtTargetAsGroup, Target.TrackedPoint, 
                    Target.VcamState.GetFinalOrientation(), brain.OutputCamera);
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
