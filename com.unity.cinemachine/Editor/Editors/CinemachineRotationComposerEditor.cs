using UnityEditor;
using UnityEditor.EditorTools;
using UnityEngine;

namespace Unity.Cinemachine.Editor
{
    [CustomEditor(typeof(CinemachineRotationComposer))]
    [CanEditMultipleObjects]
    class CinemachineRotationComposerEditor : CinemachineComponentBaseEditor
    {
        readonly GameViewComposerGuides m_GameViewGuides = new();

        CinemachineRotationComposer Target => target as CinemachineRotationComposer;

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
        }

        protected virtual void OnDisable()
        {
            m_GameViewGuides.OnDisable();
            CinemachineDebug.OnGUIHandlers -= OnGuiHandler;
            if (CinemachineCorePrefs.ShowInGameGuides.Value)
                InspectorUtility.RepaintGameView();
        }

        protected virtual void OnGuiHandler(CinemachineBrain brain)
        {
            // Draw the camera guides
            if (Target == null || !CinemachineCorePrefs.ShowInGameGuides.Value || !Target.isActiveAndEnabled)
                return;

            // Don't draw the guides if rendering to texture
            if (brain == null || brain.OutputCamera == null
                    || (brain.OutputCamera.activeTexture != null && CinemachineBrain.ActiveBrainCount > 1))
                return;

            var vcam = Target.VirtualCamera;
            if (!brain.IsValidChannel(vcam))
                return;

            // Screen guides
            bool isLive = targets.Length <= 1 && brain.IsLiveChild(vcam, true);
            m_GameViewGuides.OnGUI_DrawGuides(isLive, brain.OutputCamera, vcam.State.Lens);

            // Draw an on-screen gizmo for the target
            if (Target.LookAtTarget != null && isLive)
                CmPipelineComponentInspectorUtility.OnGUI_DrawOnscreenTargetMarker(
                    Target.TrackedPoint, brain.OutputCamera);
        }

        [EditorTool("LookAt Offset Tool", typeof(CinemachineRotationComposer))]
        class LookAtOffsetTool : EditorTool
        {
            GUIContent m_IconContent;
            public override GUIContent toolbarIcon => m_IconContent;
            void OnEnable()
            {
                m_IconContent = new GUIContent
                {
                    image = AssetDatabase.LoadAssetAtPath<Texture2D>($"{CinemachineSceneToolHelpers.IconPath}/TrackedObjectOffset.png"),
                    tooltip = "Adjust the LookAt Offset",
                };
            }

            public override void OnToolGUI(EditorWindow window)
            {
                var composer = target as CinemachineRotationComposer;
                if (composer == null || !composer.IsValid)
                    return;

                CinemachineSceneToolHelpers.DoTrackedObjectOffsetTool(
                    composer.VirtualCamera, 
                    new SerializedObject(composer).FindProperty(() => composer.TargetOffset),
                    CinemachineCore.Stage.Aim);
            }
        }
    }
}
