using UnityEngine;
using UnityEditor;
using System;

namespace Unity.Cinemachine.Editor
{
    /// <summary>
    /// This is a collection of utilities surrounding ScriptableObjects
    /// </summary>
    class ScriptableObjectUtility
    {
        /// <summary>Create a scriptable object asset</summary>
        /// <param name="assetType">The type of asset to create</param>
        /// <param name="assetPath">The full path and filename of the asset to create</param>
        /// <returns>The newly-created asset</returns>
        public static ScriptableObject CreateAt(Type assetType, string assetPath)
        {
            ScriptableObject asset = ScriptableObject.CreateInstance(assetType);
            if (asset == null)
            {
                Debug.LogError("failed to create instance of " + assetType.Name + " at " + assetPath);
                return null;
            }
            AssetDatabase.CreateAsset(asset, assetPath);
            return asset;
        }

        class CreateAssetAction : UnityEditor.ProjectWindowCallback.EndNameEditAction
        {
            public Type TypeToCreate;
            public override void Action(int instanceId, string pathName, string resourceFile)
            {
                var asset = CreateAt(TypeToCreate, pathName);
                if (asset != null)
                    ProjectWindowUtil.ShowCreatedAsset(asset);
            }
        }

        /// <summary>
        /// Creates a new asset of the specified type in the Unity project.
        /// </summary>
        /// <remarks>This method initializes the asset creation process in the Unity project window. The
        /// user is prompted to name the asset before it is finalized. The asset is created as a `.asset`
        /// file.</remarks>
        /// <typeparam name="T">The type of the asset to create. Must derive from <see cref="ScriptableObject"/>.</typeparam>
        /// <param name="defaultName">The default name for the new asset. If null or empty, a name is 
        /// automatically generated based on the type of the asset.</param>
        public static void Create<T>(string defaultName = null) where T : ScriptableObject
        {
            if (string.IsNullOrEmpty(defaultName))
                defaultName = "New " + ObjectNames.NicifyVariableName(typeof(T).Name);
            var action = ScriptableObject.CreateInstance<CreateAssetAction>();
            action.TypeToCreate = typeof(T);
            var icon = EditorGUIUtility.IconContent("ScriptableObject Icon").image as Texture2D;
            ProjectWindowUtil.StartNameEditingIfProjectWindowExists(0, action, $"{defaultName}.asset", icon , null);
        }
    }
}
