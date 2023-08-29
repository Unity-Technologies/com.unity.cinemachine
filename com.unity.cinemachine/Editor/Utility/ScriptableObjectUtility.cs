using UnityEngine;
using UnityEditor;
using System.IO;
using System;

namespace Unity.Cinemachine.Editor
{
    /// <summary>
    /// This is a collection of utilities surrounding ScriptableObjects
    /// </summary>
    class ScriptableObjectUtility
    {
        /// <summary>Create a scriptable object asset</summary>
        /// <typeparam name="T">The type of asset to create</typeparam>
        /// <param name="assetPath">The full path and filename of the asset to create</param>
        /// <returns>The newly-created asset</returns>
        public static T CreateAt<T>(string assetPath) where T : ScriptableObject
        {
            return CreateAt(typeof(T), assetPath) as T;
        }

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

        /// <summary>Create a ScriptableObject asset</summary>
        /// <typeparam name="T">The type of asset to create</typeparam>
        /// <param name="prependFolderName">If true, prepend the selected asset folder name to the asset name</param>
        /// <param name="trimName">If true, remove instances of the "Asset", "Attributes", "Container" strings from the name</param>
        public static void Create<T>(bool prependFolderName = false, bool trimName = true) where T : ScriptableObject
        {
            string className = typeof(T).Name;
            string assetName = className;
            string folder = GetSelectedAssetFolder();

            if (trimName)
            {
                var standardNames = new string[] { "Asset", "Attributes", "Container" };
                for (int i = 0; i < standardNames.Length; ++i)
                    assetName = assetName.Replace(standardNames[i], "");
            }

            if (prependFolderName)
            {
                string folderName = Path.GetFileName(folder);
                assetName = (string.IsNullOrEmpty(assetName) ? folderName : string.Format("{0}_{1}", folderName, assetName));
            }

            Create(className, assetName, folder);
        }

        private static ScriptableObject Create(string className, string assetName, string folder)
        {
            var asset = ScriptableObject.CreateInstance(className);
            if (asset == null)
            {
                Debug.LogError("failed to create instance of " + className);
                return null;
            }

            asset.name = assetName ?? className;

            string assetPath = GetUnusedAssetPath(folder, asset.name);
            AssetDatabase.CreateAsset(asset, assetPath);

            return asset;
        }

        private static string GetSelectedAssetFolder()
        {
            if ((Selection.activeObject != null) && AssetDatabase.Contains(Selection.activeObject))
            {
                string assetPath = AssetDatabase.GetAssetPath(Selection.activeObject);
                string assetPathAbsolute = string.Format("{0}/{1}", Path.GetDirectoryName(Application.dataPath), assetPath);

                if (Directory.Exists(assetPathAbsolute))
                {
                    return assetPath;
                }
                else
                {
                    return Path.GetDirectoryName(assetPath);
                }
            }

            return "Assets";
        }

        private static string GetUnusedAssetPath(string folder, string assetName)
        {
            for (int n = 0; n < 9999; n++)
            {
                string assetPath = string.Format("{0}/{1}{2}.asset", folder, assetName, (n == 0 ? "" : n.ToString()));
                string existingGUID = AssetDatabase.AssetPathToGUID(assetPath);
                if (string.IsNullOrEmpty(existingGUID))
                {
                    return assetPath;
                }
            }

            return null;
        }
    }
}
