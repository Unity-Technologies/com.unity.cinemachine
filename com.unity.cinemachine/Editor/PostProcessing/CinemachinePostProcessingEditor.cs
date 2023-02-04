#if CINEMACHINE_POST_PROCESSING_V2
using UnityEngine;
using UnityEditor;
using System.IO;

using UnityEngine.Rendering.PostProcessing;
using UnityEditor.Rendering.PostProcessing;

namespace Cinemachine.Editor
{
    [CustomEditor(typeof(CinemachinePostProcessing))]
    class CinemachinePostProcessingEditor : UnityEditor.Editor
    {
        CinemachinePostProcessing Target => target as CinemachinePostProcessing;

        SerializedProperty m_Profile;
        SerializedProperty m_FocusTracking;

        EffectListEditor m_EffectList;
        float m_ButtonWidth;

        GUIContent m_NewLabel = new ("New",  "Create a new PostProcessing profile");
        GUIContent m_CloneLabel = new ("Clone",  "Create a new PostProcessing profile "
            + "and copy the content of the currently assigned profile");

        void OnEnable()
        {
            Texture texture = AssetDatabase.LoadAssetAtPath<Texture>(
                $"{ScriptableObjectUtility.kPackageRoot}/Editor/EditorResources/PostProcessLayer.png");

            m_FocusTracking = serializedObject.FindProperty(() => Target.FocusTracking);
            m_Profile = serializedObject.FindProperty(() => Target.Profile);

            m_EffectList = new EffectListEditor(this);
            RefreshEffectListEditor(Target.Profile);
        }

        void OnDisable()
        {
            if (m_EffectList != null)
                m_EffectList.Clear();
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

#if CINEMACHINE_HDRP || CINEMACHINE_URP
            EditorGUILayout.HelpBox(
                "This component is not valid for HDRP and URP projects.  Use the CinemachineVolumeSettings component instead.",
                MessageType.Warning);
#endif

            CmPipelineComponentInspectorUtility.IMGUI_DrawMissingCmCameraHelpBox(this);

            EditorGUI.BeginChangeCheck();

            EditorGUILayout.PropertyField(m_FocusTracking);
            var focusMode = (CinemachinePostProcessing.FocusTrackingMode)m_FocusTracking.intValue;
            if (focusMode != CinemachinePostProcessing.FocusTrackingMode.None)
                EditorGUILayout.PropertyField(serializedObject.FindProperty(() => Target.FocusOffset));
            if (focusMode == CinemachinePostProcessing.FocusTrackingMode.CustomTarget)
                EditorGUILayout.PropertyField(serializedObject.FindProperty(() => Target.FocusTarget));

            if (focusMode != CinemachinePostProcessing.FocusTrackingMode.None)
            {
                bool valid = false;
                if (Target.Profile != null && Target.Profile.TryGetSettings(out DepthOfField dof))
                    valid = dof.enabled && dof.active && dof.focusDistance.overrideState;
                if (!valid)
                    EditorGUILayout.HelpBox(
                        "Focus Tracking requires an active DepthOfField/FocusDistance effect in the profile",
                        MessageType.Warning);
            }
            if (EditorGUI.EndChangeCheck())
                serializedObject.ApplyModifiedProperties();

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
            if (m_ButtonWidth == 0)
                m_ButtonWidth = GUI.skin.button.CalcSize(m_CloneLabel).x;

            bool assetHasChanged = false;
            bool haveAsset = m_Profile.objectReferenceValue != null;

            var rect = EditorGUILayout.GetControlRect();
            rect.width -= m_ButtonWidth * (haveAsset ? 2 : 1);
            using (var scope = new EditorGUI.ChangeCheckScope())
            {
                EditorGUI.PropertyField(rect, m_Profile);
                assetHasChanged = scope.changed;
            }

            rect.x += rect.width; rect.width = m_ButtonWidth;
            if (GUI.Button(rect, m_NewLabel, EditorStyles.miniButton))
            {
                var path = AssetDatabase.GenerateUniqueAssetPath("PostProcessing Profile");
                path = EditorUtility.SaveFilePanelInProject(
                    "Create PostProcessing Profile", Path.GetFileName(path), "asset", 
                    "This asset will contain the post processing settings");
                if (path.Length > 0)
                {
                    m_Profile.objectReferenceValue = CreatePostProcessProfile(path);
                    serializedObject.ApplyModifiedProperties();
                    assetHasChanged = true;
                }
            }

            rect.x += rect.width; rect.width = m_ButtonWidth;
            if (haveAsset && GUI.Button(rect, m_CloneLabel, EditorStyles.miniButton))
            {
                var origin = m_Profile.objectReferenceValue as PostProcessProfile;
                var path = AssetDatabase.GenerateUniqueAssetPath(AssetDatabase.GetAssetPath(origin));
                path = EditorUtility.SaveFilePanelInProject(
                    "Clone PostProcessing Profile", Path.GetFileName(path), "asset", 
                    "This asset will contain the post processing settings", Path.GetDirectoryName(path));
                if (path.Length > 0)
                {
                    m_Profile.objectReferenceValue = CopyPostProcessProfile(origin, path);
                    serializedObject.ApplyModifiedProperties();
                    assetHasChanged = true;
                }
            }

            if (m_Profile.objectReferenceValue == null)
            {
                if (assetHasChanged && m_EffectList != null)
                    m_EffectList.Clear(); // Asset wasn't null before, do some cleanup
                EditorGUILayout.Space();
                EditorGUILayout.HelpBox(
                    "Assign an existing Post-process Profile by choosing an asset, or create a new one by "
                        + "clicking the \"New\" button.",
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

        void RefreshEffectListEditor(PostProcessProfile asset)
        {
            if (m_EffectList == null)
                m_EffectList = new EffectListEditor(this);
            m_EffectList.Clear();
            if (asset != null)
                m_EffectList.Init(asset, new SerializedObject(asset));
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

            foreach (var item in origin.settings)
            {
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
