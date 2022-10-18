using UnityEditor;
using Cinemachine.Utility;
using UnityEngine.UIElements;
using UnityEditor.UIElements;

namespace Cinemachine.Editor
{
    [CustomEditor(typeof(CinemachineRotationComposer))]
    [CanEditMultipleObjects]
    class CinemachineRotationComposerEditor : UnityEditor.Editor
    {
        CmPipelineComponentInspectorUtility m_PipelineUtility;
        CinemachineScreenComposerGuides m_ScreenGuideEditor = new();

        CinemachineRotationComposer Target => target as CinemachineRotationComposer;

        protected virtual void OnEnable()
        {
            m_PipelineUtility = new (this);

            m_ScreenGuideEditor.GetComposition = () => Target.Composition;
            m_ScreenGuideEditor.SetComposition = (s) => Target.Composition = s;
            m_ScreenGuideEditor.Target = () => serializedObject;
            m_ScreenGuideEditor.OnEnable();

            CinemachineDebug.OnGUIHandlers -= OnGUI;
            CinemachineDebug.OnGUIHandlers += OnGUI;
            if (CinemachineCorePrefs.ShowInGameGuides.Value)
                InspectorUtility.RepaintGameView();
   
            CinemachineSceneToolUtility.RegisterTool(typeof(TrackedObjectOffsetTool));
        }

        protected virtual void OnDisable()
        {
            m_PipelineUtility.OnDisable();
            m_ScreenGuideEditor.OnDisable();
            CinemachineDebug.OnGUIHandlers -= OnGUI;
            if (CinemachineCorePrefs.ShowInGameGuides.Value)
                InspectorUtility.RepaintGameView();
  
            CinemachineSceneToolUtility.UnregisterTool(typeof(TrackedObjectOffsetTool));
        }

        public override VisualElement CreateInspectorGUI()
        {
            var ux = new VisualElement();

            m_PipelineUtility.AddMissingCmCameraHelpBox(ux, CmPipelineComponentInspectorUtility.RequiredTargets.LookAt);
            ux.Add(new PropertyField(serializedObject.FindProperty(() => Target.TrackedObjectOffset)));
            ux.Add(new PropertyField(serializedObject.FindProperty(() => Target.Damping)));
            ux.Add(new PropertyField(serializedObject.FindProperty(() => Target.CenterOnActivate)));
            ux.Add(new PropertyField(serializedObject.FindProperty(() => Target.Composition)));
            ux.Add(new PropertyField(serializedObject.FindProperty(() => Target.Lookahead)));
            m_PipelineUtility.UpdateState();
            return ux;
        }

        protected virtual void OnGUI()
        {
            // Draw the camera guides
            if (Target == null || !CinemachineCorePrefs.ShowInGameGuides.Value || !Target.isActiveAndEnabled)
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
                    null, Target.TrackedPoint, 
                    vcam.State.GetFinalOrientation(), brain.OutputCamera);
            }
        }

        void OnSceneGUI()
        {
            var composer = Target;
            if (composer == null || !composer.IsValid)
                return;

            if (CinemachineSceneToolUtility.IsToolActive(typeof(TrackedObjectOffsetTool)))
            {
                CinemachineSceneToolHelpers.TrackedObjectOffsetTool(
                    Target.VirtualCamera, 
                    new SerializedObject(Target).FindProperty(() => Target.TrackedObjectOffset),
                    CinemachineCore.Stage.Aim);
            }
        }
    }
}
