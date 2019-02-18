#if CINEMACHINE_POST_PROCESSING_V3

using UnityEngine;
using UnityEditor;
using UnityEngine.Rendering;
using UnityEditor.Rendering;
using UnityEngine.Experimental.Rendering.HDPipeline;

namespace Cinemachine.PostFX.Editor
{
    [CustomEditor(typeof(CinemachineVolumeSettings))]
    public sealed class CinemachineVolumeSettingsEditor
        : Cinemachine.Editor.BaseEditor<CinemachineVolumeSettings>
    {
        SerializedProperty m_Profile;
        SerializedProperty m_FocusTracksTarget;
        SerializedProperty m_FocusOffset;

        VolumeComponentListEditor m_ComponentList;

        GUIContent m_ProfileLabel;
        GUIContent m_NewLabel;
        GUIContent m_CloneLabel;

        void OnEnable()
        {
            m_ProfileLabel = new GUIContent("Profile", "A reference to a profile asset");
            m_NewLabel = new GUIContent("New", "Create a new profile.");
            m_CloneLabel = new GUIContent("Clone", "Create a new profile and copy the content of the currently assigned profile.");

            m_FocusTracksTarget = FindProperty(x => x.m_FocusTracksTarget);
            m_FocusOffset = FindProperty(x => x.m_FocusOffset);
            m_Profile = FindProperty(x => x.m_Profile);

            RefreshVolumeComponentEditor(Target.m_Profile);
        }

        void OnDisable()
        {
            if (m_ComponentList != null)
                m_ComponentList.Clear();
        }

        void RefreshVolumeComponentEditor(VolumeProfile asset)
        {
            if (m_ComponentList == null)
                m_ComponentList = new VolumeComponentListEditor(this);
            m_ComponentList.Clear();
            if (asset != null)
                m_ComponentList.Init(asset, new SerializedObject(asset));
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            if (m_FocusTracksTarget.boolValue)
            {
                bool valid = false;
                DepthOfField dof;
                if (Target.m_Profile != null && Target.m_Profile.TryGet(out dof))
                    valid = dof.active && dof.focusDistance.overrideState
                        && dof.focusMode == DepthOfFieldMode.UsePhysicalCamera;
                if (!valid)
                    EditorGUILayout.HelpBox(
                        "Focus Tracking requires an active DepthOfField/FocusDistance effect and FoculdMode set to Physical Camera in the profile",
                        MessageType.Warning);
                else
                {
                    if (!Target.VirtualCamera.State.HasLookAt)
                        EditorGUILayout.HelpBox(
                            "Focus Offset is relative to the Camera position",
                            MessageType.Info);
                     else
                        EditorGUILayout.HelpBox(
                            "Focus Offset is relative to the Target position",
                            MessageType.Info);
                }
            }

            var rect = GUILayoutUtility.GetRect(1, EditorGUIUtility.singleLineHeight); rect.y += 2;
            float checkboxWidth = rect.height + 5;
            rect = EditorGUI.PrefixLabel(rect, new GUIContent(m_FocusTracksTarget.displayName));
            EditorGUI.PropertyField(new Rect(rect.x, rect.y, checkboxWidth, rect.height), m_FocusTracksTarget, GUIContent.none);
            rect.x += checkboxWidth; rect.width -= checkboxWidth;
            if (m_FocusTracksTarget.boolValue)
            {
                GUIContent offsetText = new GUIContent("Offset ");
                var textDimensions = GUI.skin.label.CalcSize(offsetText);
                float oldWidth = EditorGUIUtility.labelWidth;
                EditorGUIUtility.labelWidth = textDimensions.x;
                EditorGUI.PropertyField(rect, m_FocusOffset, offsetText);
                EditorGUIUtility.labelWidth = oldWidth;
            }

            DrawProfileInspectorGUI();
            Target.InvalidateCachedProfile();

            serializedObject.ApplyModifiedProperties();
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
                    = (VolumeProfile)EditorGUI.ObjectField(
                        fieldRect, m_Profile.objectReferenceValue, typeof(VolumeProfile), false);
                assetHasChanged = scope.changed;
            }

            if (GUI.Button(buttonNewRect, m_NewLabel,
                showCopy ? EditorStyles.miniButtonLeft : EditorStyles.miniButton))
            {
                // By default, try to put assets in a folder next to the currently active
                // scene file. If the user isn't a scene, put them in root instead.
                var targetName = Target.name;
                var scene = Target.gameObject.scene;
                var asset = CreateVolumeProfile(scene, targetName);
                m_Profile.objectReferenceValue = asset;
                assetHasChanged = true;
            }

            if (showCopy && GUI.Button(buttonCopyRect, m_CloneLabel, EditorStyles.miniButtonRight))
            {
                // Duplicate the currently assigned profile and save it as a new profile
                var origin = (VolumeProfile)m_Profile.objectReferenceValue;
                var path = AssetDatabase.GetAssetPath(origin);
                path = AssetDatabase.GenerateUniqueAssetPath(path);

                var asset = Instantiate(origin);
                asset.components.Clear();
                AssetDatabase.CreateAsset(asset, path);

                foreach (var item in origin.components)
                {
                    var itemCopy = Instantiate(item);
                    itemCopy.hideFlags = HideFlags.HideInInspector | HideFlags.HideInHierarchy;
                    itemCopy.name = item.name;
                    asset.components.Add(itemCopy);
                    AssetDatabase.AddObjectToAsset(itemCopy, asset);
                }

                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();

                m_Profile.objectReferenceValue = asset;
                assetHasChanged = true;
            }

            if (m_Profile.objectReferenceValue == null)
            {
                if (assetHasChanged && m_ComponentList != null)
                    m_ComponentList.Clear(); // Asset wasn't null before, do some cleanup

                EditorGUILayout.HelpBox(
                    "Assign an existing Volume Profile by choosing an asset, or create a new one by clicking the \"New\" button.\nNew assets are automatically put in a folder next to your scene file. If your scene hasn't been saved yet they will be created at the root of the Assets folder.",
                    MessageType.Info);
            }
            else
            {
                EditorGUILayout.Space();
                if (assetHasChanged)
                    RefreshVolumeComponentEditor((VolumeProfile)m_Profile.objectReferenceValue);
                if (m_ComponentList != null)
                    m_ComponentList.OnGUI();
            }
        }

        // Copied from UnityEditor.Rendering.PostProcessing.ProfileFactory.CreateVolumeProfile() because it's internal
        static VolumeProfile CreateVolumeProfile(UnityEngine.SceneManagement.Scene scene, string targetName)
        {
            var path = string.Empty;

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

            var profile = ScriptableObject.CreateInstance<VolumeProfile>();
            AssetDatabase.CreateAsset(profile, path);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            return profile;
        }
    }
}
#endif
