using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

#if CINEMACHINE_POST_PROCESSING_V2
using UnityEngine.Rendering.PostProcessing;
using UnityEditor.Rendering.PostProcessing;
#endif

namespace Cinemachine.Editor
{
    [CustomEditor(typeof(CinemachinePostProcessing))]
    class CinemachinePostProcessingEditor : BaseEditor<CinemachinePostProcessing>
    {
#if !CINEMACHINE_POST_PROCESSING_V2
        public override void OnInspectorGUI()
        {
    #if CINEMACHINE_HDRP || CINEMACHINE_LWRP_7_3_1
            EditorGUILayout.HelpBox(
                "This component is not valid for HDRP and URP projects.  Use the CinemachineVolumeSettings component instead.",
                MessageType.Warning);
    #else
            EditorGUILayout.HelpBox(
                "This component requires the PostProcessing package, which can be downloaded from the Package Manager.\n\n"
                + "Note: For HDRP and URP projects, use the CinemachineVolumeSettings component instead.",
                MessageType.Warning);
    #endif
        }
#else
        SerializedProperty m_Profile;
        SerializedProperty m_FocusTracking;

        EffectListEditor m_EffectList;
        GUIContent m_ProfileLabel;

        void OnEnable()
        {
            Texture texture = AssetDatabase.LoadAssetAtPath<Texture>(
                $"{ScriptableObjectUtility.kPackageRoot}/Editor/EditorResources/PostProcessLayer.png");
            m_ProfileLabel = new GUIContent("Profile", texture, "A reference to a profile asset");

            m_FocusTracking = FindProperty(x => x.FocusTracking);
            m_Profile = FindProperty(x => x.Profile);

            m_EffectList = new EffectListEditor(this);
            RefreshEffectListEditor(Target.Profile);
        }

        void OnDisable()
        {
            if (m_EffectList != null)
                m_EffectList.Clear();
        }

        void RefreshEffectListEditor(PostProcessProfile asset)
        {
            if (m_EffectList == null)
                m_EffectList = new EffectListEditor(this);
            m_EffectList.Clear();
            if (asset != null)
                m_EffectList.Init(asset, new SerializedObject(asset));
        }

        /// <summary>Get the property names to exclude in the inspector.</summary>
        /// <param name="excluded">Add the names to this list</param>
        protected override void GetExcludedPropertiesInInspector(List<string> excluded)
        {
            base.GetExcludedPropertiesInInspector(excluded);
            var mode = (CinemachinePostProcessing.FocusTrackingMode)m_FocusTracking.intValue;
            if (mode != CinemachinePostProcessing.FocusTrackingMode.CustomTarget)
                excluded.Add(FieldPath(x => x.FocusTarget));
            if (mode == CinemachinePostProcessing.FocusTrackingMode.None)
                excluded.Add(FieldPath(x => x.FocusOffset));
            excluded.Add(FieldPath(x => x.Profile));
        }

        public override void OnInspectorGUI()
        {
#if CINEMACHINE_HDRP || CINEMACHINE_LWRP_7_3_1
            EditorGUILayout.HelpBox(
                "This component is not valid for HDRP and URP projects.  Use the CinemachineVolumeSettings component instead.",
                MessageType.Warning);
#endif
            BeginInspector();
            DrawRemainingPropertiesInInspector();

            var focusMode = (CinemachinePostProcessing.FocusTrackingMode)m_FocusTracking.intValue;
            if (focusMode != CinemachinePostProcessing.FocusTrackingMode.None)
            {
                bool valid = false;
                DepthOfField dof;
                if (Target.Profile != null && Target.Profile.TryGetSettings(out dof))
                    valid = dof.enabled && dof.active && dof.focusDistance.overrideState;
                if (!valid)
                    EditorGUILayout.HelpBox(
                        "Focus Tracking requires an active DepthOfField/FocusDistance effect in the profile",
                        MessageType.Warning);
            }

            EditorGUI.BeginChangeCheck();
            DrawProfileInspectorGUI();
            if (EditorGUI.EndChangeCheck())
            {
                Target.InvalidateCachedProfile();
                serializedObject.ApplyModifiedProperties();
            }
        }

        void DrawProfileInspectorGUI()
        {
            EditorGUILayout.Space();

            bool assetHasChanged = false;
            bool showCopy = m_Profile.objectReferenceValue != null;

            // The layout system sort of break alignement when mixing inspector fields with custom
            // layouted fields, do the layout manually instead
            int buttonWidth = showCopy ? 45 : 60;
            float indentOffset = EditorGUI.indentLevel * 15f;
            var lineRect = GUILayoutUtility.GetRect(1, EditorGUIUtility.singleLineHeight);
            var labelRect = new Rect(lineRect.x, lineRect.y, EditorGUIUtility.labelWidth - indentOffset, lineRect.height);
            var fieldRect = new Rect(labelRect.xMax, lineRect.y, lineRect.width - labelRect.width - buttonWidth * (showCopy ? 2 : 1), lineRect.height);
            var buttonNewRect = new Rect(fieldRect.xMax, lineRect.y, buttonWidth, lineRect.height);
            var buttonCopyRect = new Rect(buttonNewRect.xMax, lineRect.y, buttonWidth, lineRect.height);

            EditorGUI.PrefixLabel(labelRect, m_ProfileLabel);

            using (var scope = new EditorGUI.ChangeCheckScope())
            {
                m_Profile.objectReferenceValue
                    = (PostProcessProfile)EditorGUI.ObjectField(
                        fieldRect, m_Profile.objectReferenceValue, typeof(PostProcessProfile), false);
                assetHasChanged = scope.changed;
            }

            if (GUI.Button(
                buttonNewRect,
                EditorUtilities.GetContent("New|Create a new profile."),
                showCopy ? EditorStyles.miniButtonLeft : EditorStyles.miniButton))
            {
                // By default, try to put assets in a folder next to the currently active
                // scene file. If the user isn't a scene, put them in root instead.
                var targetName = Target.name;
                var scene = Target.gameObject.scene;
                var asset = CreatePostProcessProfile(scene, targetName);
                m_Profile.objectReferenceValue = asset;
                assetHasChanged = true;
            }

            if (showCopy && GUI.Button(
                buttonCopyRect,
                EditorUtilities.GetContent("Clone|Create a new profile and copy the content of the currently assigned profile."),
                EditorStyles.miniButtonRight))
            {
                // Duplicate the currently assigned profile and save it as a new profile
                var origin = (PostProcessProfile)m_Profile.objectReferenceValue;
                var path = AssetDatabase.GetAssetPath(origin);
                path = AssetDatabase.GenerateUniqueAssetPath(path);

                var asset = Instantiate(origin);
                asset.settings.Clear();
                AssetDatabase.CreateAsset(asset, path);

                foreach (var item in origin.settings)
                {
                    var itemCopy = Instantiate(item);
                    itemCopy.hideFlags = HideFlags.HideInInspector | HideFlags.HideInHierarchy;
                    itemCopy.name = item.name;
                    asset.settings.Add(itemCopy);
                    AssetDatabase.AddObjectToAsset(itemCopy, asset);
                }

                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();

                m_Profile.objectReferenceValue = asset;
                assetHasChanged = true;
            }

            if (m_Profile.objectReferenceValue == null)
            {
                if (assetHasChanged && m_EffectList != null)
                    m_EffectList.Clear(); // Asset wasn't null before, do some cleanup

                EditorGUILayout.HelpBox(
                    "Assign an existing Post-process Profile by choosing an asset, or create a new one by "
                        + "clicking the \"New\" button.\nNew assets are automatically put in a folder next " 
                        + "to your scene file. If your scene hasn't been saved yet they will be created " 
                        + "at the root of the Assets folder.",
                    MessageType.Info);
            }
            else
            {
                if (assetHasChanged)
                    RefreshEffectListEditor((PostProcessProfile)m_Profile.objectReferenceValue);
                if (m_EffectList != null)
                    m_EffectList.OnGUI();
            }
        }

        // Copied from UnityEditor.Rendering.PostProcessing.ProfileFactory.CreatePostProcessProfile() because it's internal
        static PostProcessProfile CreatePostProcessProfile(UnityEngine.SceneManagement.Scene scene, string targetName)
        {
            string path;
            if (string.IsNullOrEmpty(scene.path))
            {
                path = "Assets/";
            }
            else
            {
                var scenePath = System.IO.Path.GetDirectoryName(scene.path);
                var extPath = scene.name + "_Profiles";
                var profilePath = scenePath + "/" + extPath;

                if (!AssetDatabase.IsValidFolder(profilePath))
                    AssetDatabase.CreateFolder(scenePath, extPath);

                path = profilePath + "/";
            }

            path += targetName + " Profile.asset";
            path = AssetDatabase.GenerateUniqueAssetPath(path);

            var profile = ScriptableObject.CreateInstance<PostProcessProfile>();
            AssetDatabase.CreateAsset(profile, path);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            return profile;
        }
#endif
    }
}
