using UnityEditor;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
using UnityEngine;

namespace Unity.Cinemachine.Editor
{
    [CustomEditor(typeof(CinemachineAutoFocus))]
    class CinemachineAutoFocusEditor : UnityEditor.Editor
    {
        CinemachineAutoFocus Target => target as CinemachineAutoFocus;

#if CINEMACHINE_HDRP
        const string k_ComputeShaderName = "CinemachineFocusDistanceCompute";
#endif

        public override VisualElement CreateInspectorGUI()
        {
            var ux = new VisualElement();
            this.AddMissingCmCameraHelpBox(ux);
#if CINEMACHINE_HDRP
            ux.AddChild(new HelpBox(
                "Note: focus control requires an active Volume containing a Depth Of Field override "
                    + "having Focus Mode activated and set to Physical Camera, "
                    + "and Focus Distance Mode activated and set to Camera",
                HelpBoxMessageType.Info));
#else
            ux.AddChild(new HelpBox(
                "Note: focus control will only set the focusDistance field in the Camera.  This will have no visible "
                    + "effect unless the Camera is set to physical mode, and something is installed to process that value.",
                HelpBoxMessageType.Info));

            var hdrpOnlyMessage = ux.AddChild(
                new HelpBox("ScreenCenter mode is only valid within HDRP projects.", HelpBoxMessageType.Warning));
#endif
            var focusTargetProp = serializedObject.FindProperty(() => Target.FocusTarget);
            ux.Add(new PropertyField(focusTargetProp));
            var customTarget = ux.AddChild(new PropertyField(serializedObject.FindProperty(() => Target.CustomTarget)));
            var offset = ux.AddChild(new PropertyField(serializedObject.FindProperty(() => Target.FocusDepthOffset)));
            var damping = ux.AddChild(new PropertyField(serializedObject.FindProperty(() => Target.Damping)));
#if CINEMACHINE_HDRP
            var radius = ux.AddChild(new PropertyField(serializedObject.FindProperty(() => Target.AutoDetectionRadius)));

            var computeShaderProp = serializedObject.FindProperty("m_ComputeShader");
            var shaderDisplay = ux.AddChild(new PropertyField(computeShaderProp));
            shaderDisplay.SetEnabled(false);

            var importHelp = ux.AddChild(InspectorUtility.HelpBoxWithButton(
                $"The {k_ComputeShaderName} shader needs to be imported into "
                    + "the project. It will be imported by default into the Assets root.  "
                    + "After importing, you can move it elsewhere but don't rename it.",
                    HelpBoxMessageType.Warning,
                "Import\nShader", () =>
            {
                // Check if it's already imported, just in case
                var shader = FindShader();
                if (shader == null)
                {
                    // Import the asset from the package
                    var shaderAssetPath = $"Assets/{k_ComputeShaderName}.compute";
                    FileUtil.CopyFileOrDirectory(
                        $"{CinemachineCore.kPackageRoot}/Runtime/PostProcessing/HDRP Resources~/{k_ComputeShaderName}.compute",
                        shaderAssetPath);
                    AssetDatabase.ImportAsset(shaderAssetPath);
                    shader = AssetDatabase.LoadAssetAtPath<ComputeShader>(shaderAssetPath);
                }
                AssignShaderToTarget(shader);
            }));
#endif

            ux.TrackPropertyWithInitialCallback(focusTargetProp, (p) =>
            {
                if (p.serializedObject == null)
                    return; // object deleted
                var mode = (CinemachineAutoFocus.FocusTrackingMode)p.intValue;
                customTarget.SetVisible(mode == CinemachineAutoFocus.FocusTrackingMode.CustomTarget);
                offset.SetVisible(mode != CinemachineAutoFocus.FocusTrackingMode.None);
                damping.SetVisible(mode != CinemachineAutoFocus.FocusTrackingMode.None);
#if CINEMACHINE_HDRP
                radius.SetVisible(mode == CinemachineAutoFocus.FocusTrackingMode.ScreenCenter);
                shaderDisplay.SetVisible(mode == CinemachineAutoFocus.FocusTrackingMode.ScreenCenter);
                bool importHelpVisible = false;
                if (mode == CinemachineAutoFocus.FocusTrackingMode.ScreenCenter && computeShaderProp.objectReferenceValue == null)
                {
                    var shader = FindShader(); // slow!!!!
                    if (shader != null)
                        AssignShaderToTarget(shader);
                    else
                        importHelpVisible = true;
                }
                importHelp.SetVisible(importHelpVisible);
#else
                hdrpOnlyMessage.SetVisible(mode == CinemachineAutoFocus.FocusTrackingMode.ScreenCenter);
#endif
            });

#if CINEMACHINE_HDRP
            // Make the import box disappear after import
            ux.TrackPropertyWithInitialCallback(computeShaderProp, (p) =>
            {
                if (p.serializedObject == null)
                    return; // object deleted
                var mode = (CinemachineAutoFocus.FocusTrackingMode)focusTargetProp.intValue;
                importHelp.SetVisible(mode == CinemachineAutoFocus.FocusTrackingMode.ScreenCenter
                    && p.objectReferenceValue == null);
            });
#endif

            return ux;
        }

#if CINEMACHINE_HDRP
        static ComputeShader FindShader()
        {
            var guids = AssetDatabase.FindAssets($"{k_ComputeShaderName}, t:ComputeShader", new [] { "Assets" });
            if (guids.Length > 0)
                return AssetDatabase.LoadAssetAtPath<ComputeShader>(
                    AssetDatabase.GUIDToAssetPath(guids[0]));
            return null;
        }

        void AssignShaderToTarget(ComputeShader shader)
        {
            for (int i = 0; i < targets.Length; ++i)
            {
                var t = new SerializedObject(targets[i]);
                t.FindProperty("m_ComputeShader").objectReferenceValue = shader;
                t.ApplyModifiedProperties();
            }
        }
#endif
    }
}
