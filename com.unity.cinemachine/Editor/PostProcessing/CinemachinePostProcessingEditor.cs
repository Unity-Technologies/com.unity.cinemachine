#if CINEMACHINE_POST_PROCESSING_V2
using UnityEngine;
using UnityEditor;
using System.IO;
using UnityEngine.Rendering.PostProcessing;
using UnityEditor.Rendering.PostProcessing;
using UnityEngine.UIElements;
using UnityEditor.UIElements;

namespace Unity.Cinemachine.Editor
{
    [CustomEditor(typeof(CinemachinePostProcessing))]
    class CinemachinePostProcessingEditor : UnityEditor.Editor
    {
        EffectListEditor m_EffectList;

        void OnEnable() => m_EffectList = new EffectListEditor(this);
        void OnDisable() => m_EffectList?.Clear();

        public override VisualElement CreateInspectorGUI()
        {
            var ux = new VisualElement();
            this.AddMissingCmCameraHelpBox(ux);

#if CINEMACHINE_HDRP || CINEMACHINE_URP
            ux.Add(new HelpBox(
                "This component is not valid for HDRP and URP projects.  Use the CinemachineVolumeSettings component instead.",
                HelpBoxMessageType.Warning));
#endif

            var post = target as CinemachinePostProcessing;
            ux.AddChild(new PropertyField(serializedObject.FindProperty(() => post.Weight)));

            var trackingProp = serializedObject.FindProperty(() => post.FocusTracking);
            ux.AddChild(new PropertyField(trackingProp));
            var focusTargetField = ux.AddChild(new PropertyField(serializedObject.FindProperty(() => post.FocusTarget)));
            var focusOffsetField = ux.AddChild(new PropertyField(serializedObject.FindProperty(() => post.FocusOffset)));
            var trackingHelp = ux.AddChild(new HelpBox(
                "Focus Tracking requires an active DepthOfField/FocusDistance effect in the profile.",
                HelpBoxMessageType.Warning));

            ux.TrackAnyUserActivity(() =>
            {
                var mode = (CinemachinePostProcessing.FocusTrackingMode)trackingProp.intValue;
                focusTargetField.SetVisible(mode == CinemachinePostProcessing.FocusTrackingMode.CustomTarget);
                focusOffsetField.SetVisible(mode != CinemachinePostProcessing.FocusTrackingMode.None);
                bool valid = false;
                if (mode != CinemachinePostProcessing.FocusTrackingMode.None
                    && post.Profile != null && post.Profile.TryGetSettings(out DepthOfField dof))
                        valid = dof.enabled && dof.active && dof.focusDistance.overrideState;
                trackingHelp.SetVisible(mode != CinemachinePostProcessing.FocusTrackingMode.None && !valid);
            });

            var profileProp = serializedObject.FindProperty(() => post.Profile);
            var row = ux.AddChild(InspectorUtility.PropertyRow(profileProp, out var _));
            var newButton = row.Contents.AddChild(new Button(() =>
            {
                var path = AssetDatabase.GenerateUniqueAssetPath("PostProcessing Profile");
                path = EditorUtility.SaveFilePanelInProject(
                    "Create PostProcessing Profile", Path.GetFileName(path), "asset",
                    "This asset will contain the post processing settings");
                if (path.Length > 0)
                {
                    profileProp.objectReferenceValue = CreatePostProcessProfile(path);
                    serializedObject.ApplyModifiedProperties();
                }
            }) { text = "New", style = { marginRight = 0 }} );
            var cloneButton = row.Contents.AddChild(new Button(() =>
            {
                var origin = profileProp.objectReferenceValue as PostProcessProfile;
                var path = AssetDatabase.GenerateUniqueAssetPath(AssetDatabase.GetAssetPath(origin));
                path = EditorUtility.SaveFilePanelInProject(
                    "Clone PostProcessing Profile", Path.GetFileName(path), "asset",
                    "This asset will contain the post processing settings", Path.GetDirectoryName(path));
                if (path.Length > 0)
                {
                    profileProp.objectReferenceValue = CopyPostProcessProfile(origin, path);
                    serializedObject.ApplyModifiedProperties();
                }
            }) { text = "Clone", style = { marginLeft = 0 }} );
            cloneButton.TrackPropertyWithInitialCallback(profileProp, (p) => cloneButton.SetVisible(p.objectReferenceValue != null));

            var missingProfileHelp = ux.AddChild(new HelpBox(
                "Assign an existing Post-process Profile by choosing an asset, or create a new one by "
                    + "clicking the \"New\" button.",
                HelpBoxMessageType.Info));

            var effectsEditor = ux.AddChild(new IMGUIContainer(() => m_EffectList.OnGUI()));

            ux.TrackPropertyWithInitialCallback(profileProp, (p) =>
            {
                missingProfileHelp.SetVisible(p.objectReferenceValue == null);
                effectsEditor.SetVisible(p.objectReferenceValue != null);
                var profile = (PostProcessProfile)p.objectReferenceValue;
                m_EffectList.Clear();
                if (profile != null)
                    m_EffectList.Init(profile, new SerializedObject(profile));
            });

            return ux;
        }

        static PostProcessProfile CreatePostProcessProfile(string path)
        {
            path = AssetDatabase.GenerateUniqueAssetPath(path);
            var profile = ScriptableObject.CreateInstance<PostProcessProfile>();
            AssetDatabase.CreateAsset(profile, path);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            return profile;
        }

        // Duplicate the currently assigned profile and save it as a new profile
        static PostProcessProfile CopyPostProcessProfile(PostProcessProfile origin, string path)
        {
            var asset = UnityEngine.Object.Instantiate(origin);
            asset.settings.Clear();
            AssetDatabase.CreateAsset(asset, path);

            for (int i = 0; i < origin.settings.Count; ++i)
            {
                var item = origin.settings[i];
                var itemCopy = UnityEngine.Object.Instantiate(item);
                itemCopy.hideFlags = HideFlags.HideInInspector | HideFlags.HideInHierarchy;
                itemCopy.name = item.name;
                asset.settings.Add(itemCopy);
                AssetDatabase.AddObjectToAsset(itemCopy, asset);
            }
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            return asset;
        }
    }
}
#endif