#if CINEMACHINE_HDRP || CINEMACHINE_URP

using UnityEngine;
using UnityEditor;
using UnityEngine.Rendering;
using UnityEditor.Rendering;
using System.Collections.Generic;
using System.IO;
using UnityEngine.UIElements;
using UnityEditor.UIElements;

#if CINEMACHINE_HDRP
    using UnityEngine.Rendering.HighDefinition;
#else
    using UnityEngine.Rendering.Universal;
#endif

namespace Unity.Cinemachine.Editor
{
    [CustomEditor(typeof(CinemachineVolumeSettings))]
    class CinemachineVolumeSettingsEditor : UnityEditor.Editor
    {
        VolumeComponentListEditor m_EffectList;

        void OnEnable() => m_EffectList = new VolumeComponentListEditor(this);
        void OnDisable() => m_EffectList?.Clear();

        public override VisualElement CreateInspectorGUI()
        {
            var ux = new VisualElement();
            this.AddMissingCmCameraHelpBox(ux);

            var post = target as CinemachineVolumeSettings;
            ux.AddChild(new PropertyField(serializedObject.FindProperty(() => post.Weight)));

            var trackingProp = serializedObject.FindProperty(() => post.FocusTracking);
            ux.AddChild(new PropertyField(trackingProp));
            var focusTargetField = ux.AddChild(new PropertyField(serializedObject.FindProperty(() => post.FocusTarget)));
            var focusOffsetField = ux.AddChild(new PropertyField(serializedObject.FindProperty(() => post.FocusOffset)));
            var trackingHelp = ux.AddChild(new HelpBox(
#if CINEMACHINE_HDRP
                "Focus Tracking requires an active Depth Of Field override in the profile "
                    + "with Focus Distance activated and Focus Mode activated and set to Use Physical Camera",
#else
                "Focus Tracking requires an active Depth Of Field override in the profile "
                    + "with Focus Distance activated and Mode activated and set to Bokeh",
#endif
                HelpBoxMessageType.Warning));

            ux.TrackAnyUserActivity(() =>
            {
                var mode = (CinemachineVolumeSettings.FocusTrackingMode)trackingProp.intValue;
                focusTargetField.SetVisible(mode == CinemachineVolumeSettings.FocusTrackingMode.CustomTarget);
                focusOffsetField.SetVisible(mode != CinemachineVolumeSettings.FocusTrackingMode.None);

                bool valid = false;
                if (mode != CinemachineVolumeSettings.FocusTrackingMode.None)
                {
                    if (post.Profile != null && post.Profile.TryGet(out DepthOfField dof))
                    {
#if CINEMACHINE_HDRP
                        valid = dof.active 
                            && (dof.focusDistance.overrideState || dof.focusDistanceMode == FocusDistanceMode.Camera)
                            && dof.focusMode.overrideState && dof.focusMode == DepthOfFieldMode.UsePhysicalCamera;
#else
                        valid = dof.active && dof.focusDistance.overrideState
                            && dof.mode.overrideState && dof.mode == DepthOfFieldMode.Bokeh;
#endif
                    }
                }
                trackingHelp.SetVisible(mode != CinemachineVolumeSettings.FocusTrackingMode.None && !valid);
            });

            var profileProp = serializedObject.FindProperty(() => post.Profile);
            var row = ux.AddChild(InspectorUtility.PropertyRow(profileProp, out var _));
            var newButton = row.Contents.AddChild(new Button(() =>
            {
                var path = AssetDatabase.GenerateUniqueAssetPath("Volume Profile");
                path = EditorUtility.SaveFilePanelInProject(
                    "Create Volume Profile", Path.GetFileName(path), "asset", 
                    "This asset will contain the Volume settings");
                if (path.Length > 0)
                {
                    profileProp.objectReferenceValue = CreateVolumeProfile(path);
                    serializedObject.ApplyModifiedProperties();
                }                
            }) { text = "New", style = { marginRight = 0 }} );
            var cloneButton = row.Contents.AddChild(new Button(() =>
            {
                var origin = profileProp.objectReferenceValue as VolumeProfile;
                var path = AssetDatabase.GenerateUniqueAssetPath(AssetDatabase.GetAssetPath(origin));
                path = EditorUtility.SaveFilePanelInProject(
                    "Clone Volume Profile", Path.GetFileName(path), "asset", 
                    "This asset will contain the Volume settings", Path.GetDirectoryName(path));
                if (path.Length > 0)
                {
                    profileProp.objectReferenceValue = CopyVolumeProfile(origin, path);
                    serializedObject.ApplyModifiedProperties();
                }                
            }) { text = "Clone", style = { marginLeft = 0 }} );
            cloneButton.TrackPropertyWithInitialCallback(profileProp, (p) => cloneButton.SetVisible(p.objectReferenceValue != null));

            var missingProfileHelp = ux.AddChild(new HelpBox(
                "Assign an existing Volume Profile by choosing an asset, or create a new one by "
                    + "clicking the \"New\" button.",
                HelpBoxMessageType.Info));

            var effectsEditor = ux.AddChild(new IMGUIContainer(() => m_EffectList.OnGUI()));

            ux.TrackPropertyWithInitialCallback(profileProp, (p) =>
            {
                missingProfileHelp.SetVisible(p.objectReferenceValue == null);
                effectsEditor.SetVisible(p.objectReferenceValue != null);
                var profile = p.objectReferenceValue as VolumeProfile;
                m_EffectList.Clear();
                if (profile != null)
                    m_EffectList.Init(profile, new SerializedObject(profile));
            });

            return ux;
        }

        static VolumeProfile CreateVolumeProfile(string path)
        {
            path = AssetDatabase.GenerateUniqueAssetPath(path);
            var profile = ScriptableObject.CreateInstance<VolumeProfile>();
            AssetDatabase.CreateAsset(profile, path);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            return profile;
        }

        // Duplicate the currently assigned profile and save it as a new profile
        static VolumeProfile CopyVolumeProfile(VolumeProfile origin, string path)
        {
            var asset = UnityEngine.Object.Instantiate(origin);
            asset.components.Clear();
            AssetDatabase.CreateAsset(asset, path);

            for (int i = 0; i < origin.components.Count; ++i)
            {
                var item = origin.components[i];
                var itemCopy = Instantiate(item);
                itemCopy.hideFlags = HideFlags.HideInInspector | HideFlags.HideInHierarchy;
                itemCopy.name = item.name;
                asset.components.Add(itemCopy);
                AssetDatabase.AddObjectToAsset(itemCopy, asset);
            }
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            return asset;
        }
    }
}
#endif
