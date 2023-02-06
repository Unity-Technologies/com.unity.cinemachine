#if CINEMACHINE_HDRP || CINEMACHINE_URP

using UnityEngine;
using UnityEditor;
using UnityEngine.Rendering;
using UnityEditor.Rendering;
using System.Collections.Generic;
using System.IO;

#if CINEMACHINE_HDRP
    using UnityEngine.Rendering.HighDefinition;
#elif CINEMACHINE_URP
using UnityEngine.Rendering.Universal;
#endif

namespace Cinemachine.Editor
{
    [CustomEditor(typeof(CinemachineVolumeSettings))]
    class CinemachineVolumeSettingsEditor : UnityEditor.Editor
    {
        CinemachineVolumeSettings Target => target as CinemachineVolumeSettings;

        SerializedProperty m_Profile;
        SerializedProperty m_FocusTracking;

        GUIContent m_NewLabel = new ("New",  "Create a new PostProcessing profile");
        GUIContent m_CloneLabel = new ("Clone",  "Create a new PostProcessing profile "
            + "and copy the content of the currently assigned profile");

        VolumeComponentListEditor m_ComponentList;
        float m_ButtonWidth;

        void OnEnable()
        {
            m_NewLabel = new GUIContent("New", "Create a new profile.");
            m_CloneLabel = new GUIContent("Clone", "Create a new profile and copy the content of the currently assigned profile.");

            m_FocusTracking = serializedObject.FindProperty(() => Target.FocusTracking);
            m_Profile = serializedObject.FindProperty(() => Target.Profile);

            RefreshVolumeComponentEditor(Target.Profile);
        }

        void OnDisable()
        {
            if (m_ComponentList != null)
                m_ComponentList.Clear();
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            CmPipelineComponentInspectorUtility.IMGUI_DrawMissingCmCameraHelpBox(this);

            EditorGUI.BeginChangeCheck();

            EditorGUILayout.PropertyField(serializedObject.FindProperty(() => Target.Weight));
            EditorGUILayout.PropertyField(m_FocusTracking);
            var focusMode = (CinemachineVolumeSettings.FocusTrackingMode)m_FocusTracking.intValue;
            if (focusMode != CinemachineVolumeSettings.FocusTrackingMode.None)
                EditorGUILayout.PropertyField(serializedObject.FindProperty(() => Target.FocusOffset));
            if (focusMode == CinemachineVolumeSettings.FocusTrackingMode.CustomTarget)
                EditorGUILayout.PropertyField(serializedObject.FindProperty(() => Target.FocusTarget));

            if (focusMode != CinemachineVolumeSettings.FocusTrackingMode.None)
            {
                bool valid = false;
                DepthOfField dof;
                if (Target.Profile != null && Target.Profile.TryGet(out dof))
#if CINEMACHINE_URP && !CINEMACHINE_HDRP
                {
                    valid = dof.active && dof.focusDistance.overrideState
                        && dof.mode.overrideState && dof.mode == DepthOfFieldMode.Bokeh;
                }
                if (!valid)
                    EditorGUILayout.HelpBox(
                        "Focus Tracking requires an active Depth Of Field override in the profile "
                            + "with Focus Distance activated and Mode activated and set to Bokeh",
                        MessageType.Warning);
#else
                {
                    valid = dof.active 
                        && (dof.focusDistance.overrideState || dof.focusDistanceMode == FocusDistanceMode.Camera)
                        && dof.focusMode.overrideState && dof.focusMode == DepthOfFieldMode.UsePhysicalCamera;
                }
                if (!valid)
                    EditorGUILayout.HelpBox(
                        "Focus Tracking requires an active Depth Of Field override in the profile "
                            + "with Focus Distance activated and Focus Mode activated and set to Use Physical Camera",
                        MessageType.Warning);
#endif
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
                var path = AssetDatabase.GenerateUniqueAssetPath("Volume Profile");
                path = EditorUtility.SaveFilePanelInProject(
                    "Create Volume Profile", Path.GetFileName(path), "asset", 
                    "This asset will contain the post processing settings");
                if (path.Length > 0)
                {
                    m_Profile.objectReferenceValue = CreateVolumeProfile(path);
                    serializedObject.ApplyModifiedProperties();
                    assetHasChanged = true;
                }
            }

            rect.x += rect.width; rect.width = m_ButtonWidth;
            if (haveAsset && GUI.Button(rect, m_CloneLabel, EditorStyles.miniButton))
            {
                var origin = m_Profile.objectReferenceValue as VolumeProfile;
                var path = AssetDatabase.GenerateUniqueAssetPath(AssetDatabase.GetAssetPath(origin));
                path = EditorUtility.SaveFilePanelInProject(
                    "Clone Volume Profile", Path.GetFileName(path), "asset", 
                    "This asset will contain the post processing settings", Path.GetDirectoryName(path));
                if (path.Length > 0)
                {
                    m_Profile.objectReferenceValue = CopyVolumeProfile(origin, path);
                    serializedObject.ApplyModifiedProperties();
                    assetHasChanged = true;
                }
            }

            if (m_Profile.objectReferenceValue == null)
            {
                if (assetHasChanged && m_ComponentList != null)
                    m_ComponentList.Clear(); // Asset wasn't null before, do some cleanup

                EditorGUILayout.HelpBox(
                    "Assign an existing Volume Profile by choosing an asset, or create a "
                    + "new one by clicking the \"New\" button.",
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

        void RefreshVolumeComponentEditor(VolumeProfile asset)
        {
            if (m_ComponentList == null)
                m_ComponentList = new VolumeComponentListEditor(this);
            m_ComponentList.Clear();
            if (asset != null)
                m_ComponentList.Init(asset, new SerializedObject(asset));
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
            return asset;
        }
    }
}
#endif
