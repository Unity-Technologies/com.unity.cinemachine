using UnityEngine;
using UnityEditor;
using Cinemachine.Utility;
using UnityEngine.UIElements;
using UnityEditor.UIElements;

namespace Cinemachine.Editor
{
    [CustomEditor(typeof(CinemachineRotationComposer))]
    [CanEditMultipleObjects]
    internal class CinemachineRotationComposerEditor : UnityEditor.Editor
    {
        CinemachineScreenComposerGuides m_ScreenGuideEditor;
        GameViewEventCatcher m_GameViewEventCatcher;

        CinemachineRotationComposer Target => target as CinemachineRotationComposer;

        protected virtual void OnEnable()
        {
            m_ScreenGuideEditor = new CinemachineScreenComposerGuides();
            m_ScreenGuideEditor.GetHardGuide = () => { return Target.HardGuideRect; };
            m_ScreenGuideEditor.GetSoftGuide = () => { return Target.SoftGuideRect; };
            m_ScreenGuideEditor.SetHardGuide = (Rect r) => { Target.HardGuideRect = r; };
            m_ScreenGuideEditor.SetSoftGuide = (Rect r) => { Target.SoftGuideRect = r; };
            m_ScreenGuideEditor.Target = () => { return serializedObject; };

            m_GameViewEventCatcher = new GameViewEventCatcher();
            m_GameViewEventCatcher.OnEnable();

            CinemachineDebug.OnGUIHandlers -= OnGUI;
            CinemachineDebug.OnGUIHandlers += OnGUI;
            if (CinemachineSettings.CinemachineCoreSettings.ShowInGameGuides)
                InspectorUtility.RepaintGameView();
   
            CinemachineSceneToolUtility.RegisterTool(typeof(TrackedObjectOffsetTool));
        }

        protected virtual void OnDisable()
        {
            m_GameViewEventCatcher.OnDisable();
            CinemachineDebug.OnGUIHandlers -= OnGUI;
            if (CinemachineSettings.CinemachineCoreSettings.ShowInGameGuides)
                InspectorUtility.RepaintGameView();
  
            CinemachineSceneToolUtility.UnregisterTool(typeof(TrackedObjectOffsetTool));
        }

        public override VisualElement CreateInspectorGUI()
        {
            var serializedTarget = new SerializedObject(Target);
            var ux = new VisualElement();

            var noTargetHelp = ux.AddChild(new HelpBox(
                "A Tracking target is required.  Change Rotation Control to None if you don't want a Tracking or LookAt target.", 
                HelpBoxMessageType.Warning));

            ux.Add(new PropertyField(serializedTarget.FindProperty(() => Target.TrackedObjectOffset)));
            ux.Add(new PropertyField(serializedTarget.FindProperty(() => Target.Lookahead)));
            ux.Add(new PropertyField(serializedTarget.FindProperty(() => Target.Damping)));
            ux.Add(new PropertyField(serializedTarget.FindProperty(() => Target.Composition)));
            ux.Add(new PropertyField(serializedTarget.FindProperty(() => Target.CenterOnActivate)));

            // GML: This is rather evil.  Is there a better (event-driven) way?
            UpdateNoTargetHelp();
            ux.schedule.Execute(UpdateNoTargetHelp).Every(250);

            void UpdateNoTargetHelp()
            {
                bool noTarget = false;
                for (int i = 0; i < targets.Length; ++i)
                    noTarget |= targets[i] != null && (targets[i] as CinemachineRotationComposer).LookAtTarget == null;
                if (noTargetHelp != null)
                    noTargetHelp.SetVisible(noTarget);
            }
            return ux;
        }

        protected virtual void OnGUI()
        {
            // Draw the camera guides
            if (Target == null || !CinemachineSettings.CinemachineCoreSettings.ShowInGameGuides || !Target.isActiveAndEnabled)
                return;

            // Don't draw the guides if rendering to texture
            var vcam = Target.VirtualCamera;
            CinemachineBrain brain = CinemachineCore.Instance.FindPotentialTargetBrain(vcam);
            if (brain == null || (brain.OutputCamera.activeTexture != null && CinemachineCore.Instance.BrainCount > 1))
                return;

            // Screen guides
            bool isLive = targets.Length <= 1 && brain.IsLive(vcam, true);
            m_ScreenGuideEditor.OnGUI_DrawGuides(isLive, brain.OutputCamera, Target.VcamState.Lens, true);

            // Draw an on-screen gizmo for the target
            if (Target.LookAtTarget != null && isLive)
            {
                Vector3 targetScreenPosition = brain.OutputCamera.WorldToScreenPoint(Target.TrackedPoint);
                if (targetScreenPosition.z > 0)
                {
                    targetScreenPosition.y = Screen.height - targetScreenPosition.y;

                    GUI.color = CinemachineSettings.ComposerSettings.TargetColour;
                    Rect r = new Rect(targetScreenPosition, Vector2.zero);
                    float size = (CinemachineSettings.ComposerSettings.TargetSize
                        + CinemachineScreenComposerGuides.kGuideBarWidthPx) / 2;
                    GUI.DrawTexture(r.Inflated(new Vector2(size, size)), Texture2D.whiteTexture);
                    size -= CinemachineScreenComposerGuides.kGuideBarWidthPx;
                    if (size > 0)
                    {
                        Vector4 overlayOpacityScalar
                            = new Vector4(1f, 1f, 1f, CinemachineSettings.ComposerSettings.OverlayOpacity);
                        GUI.color = Color.black * overlayOpacityScalar;
                        GUI.DrawTexture(r.Inflated(new Vector2(size, size)), Texture2D.whiteTexture);
                    }
                }
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
