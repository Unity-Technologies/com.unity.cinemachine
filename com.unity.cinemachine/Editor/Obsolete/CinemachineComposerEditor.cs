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
        CinemachineScreenComposerGuides m_ScreenGuideEditor = new();

        protected virtual void OnEnable()
        {
            m_ScreenGuideEditor.GetComposition = () => new ScreenComposerSettings
            {
                ScreenPosition = new Vector2(Target.m_ScreenX, Target.m_ScreenY) - new Vector2(0.5f, 0.5f),
                DeadZone = new () { Enabled = true, Size = new Vector2(Target.m_DeadZoneWidth, Target.m_DeadZoneHeight) },
                HardLimits = new ()
                {
                    Enabled = true,
                    Size = new Vector2(Target.m_SoftZoneWidth, Target.m_SoftZoneHeight),
                    Bias = new Vector2(Target.m_BiasX, Target.m_BiasY) * 2
                }
            };
            m_ScreenGuideEditor.SetComposition = (s) =>
            {
                Target.m_ScreenX = s.ScreenPosition.x + 0.5f;
                Target.m_ScreenY = s.ScreenPosition.y + 0.5f;
                Target.m_DeadZoneWidth = s.DeadZone.Size.x;
                Target.m_DeadZoneHeight = s.DeadZone.Size.y;
                Target.m_SoftZoneWidth = s.HardLimits.Size.x;
                Target.m_SoftZoneHeight = s.HardLimits.Size.y;
            };
            m_ScreenGuideEditor.Target = () => { return serializedObject; };
            m_ScreenGuideEditor.OnEnable();

            CinemachineDebug.OnGUIHandlers -= OnGUI;
            CinemachineDebug.OnGUIHandlers += OnGUI;
            if (CinemachineCorePrefs.ShowInGameGuides.Value)
                InspectorUtility.RepaintGameView();
   
            CinemachineSceneToolUtility.RegisterTool(typeof(TrackedObjectOffsetTool));
        }

        protected virtual void OnDisable()
        {
            m_ScreenGuideEditor.OnDisable();
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

            // Screen guides
            bool isLive = targets.Length <= 1 && brain.IsLive(vcam, true);
            m_ScreenGuideEditor.OnGUI_DrawGuides(isLive, brain.OutputCamera, Target.VcamState.Lens);

            // Draw an on-screen gizmo for the target
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
