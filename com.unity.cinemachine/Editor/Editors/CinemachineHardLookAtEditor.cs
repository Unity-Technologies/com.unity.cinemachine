using UnityEditor;
using UnityEditor.EditorTools;
using UnityEngine;

namespace Unity.Cinemachine.Editor
{
    [CustomEditor(typeof(CinemachineHardLookAt))]
    [CanEditMultipleObjects]
    class CinemachineHardLookAtEditor : CinemachineComponentBaseEditor
    {
        CinemachineHardLookAt Target => target as CinemachineHardLookAt;

        protected virtual void OnEnable()
        {
            CinemachineDebug.OnGUIHandlers -= OnGuiHandler;
            CinemachineDebug.OnGUIHandlers += OnGuiHandler;
            if (CinemachineCorePrefs.ShowInGameGuides.Value)
                InspectorUtility.RepaintGameView();
        }

        protected virtual void OnDisable()
        {
            CinemachineDebug.OnGUIHandlers -= OnGuiHandler;
            if (CinemachineCorePrefs.ShowInGameGuides.Value)
                InspectorUtility.RepaintGameView();
        }

        protected virtual void OnGuiHandler(CinemachineBrain brain)
        {
            // Draw the camera guides
            if (Target == null || !CinemachineCorePrefs.ShowInGameGuides.Value || !Target.isActiveAndEnabled)
                return;

            var vcam = Target.VirtualCamera;
            if (brain == null || brain != CinemachineCore.FindPotentialTargetBrain(vcam)
                || (brain.OutputCamera.activeTexture != null && CinemachineBrain.ActiveBrainCount > 1))
                return;

            bool isLive = targets.Length <= 1 && brain.IsLiveChild(vcam, true);
            var t = Target.LookAtTarget;
            if (Target.LookAtTarget != null && isLive)
            {
                var point = t.position + t.rotation * Target.LookAtOffset;
                CmPipelineComponentInspectorUtility.OnGUI_DrawOnscreenTargetMarker(
                    point, brain.OutputCamera);
            }
        }

        [EditorTool("LookAt Offset Tool", typeof(CinemachineHardLookAt))]
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
                var hardLookAt = target as CinemachineHardLookAt;
                if (hardLookAt == null || !hardLookAt.IsValid)
                    return;

                CinemachineSceneToolHelpers.DoTrackedObjectOffsetTool(
                    hardLookAt.VirtualCamera,
                    new SerializedObject(hardLookAt).FindProperty(() => hardLookAt.LookAtOffset),
                    CinemachineCore.Stage.Aim);
            }
        }
    }
}
