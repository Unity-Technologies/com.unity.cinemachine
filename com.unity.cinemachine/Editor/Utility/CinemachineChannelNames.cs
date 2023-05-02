using UnityEngine;
using System;
using UnityEditor;

namespace Unity.Cinemachine.Editor
{
    /// <summary>
    ///  This class contains names for Cinemachine Channels.  These work like Unity Layers, you can
    ///  define and name them, and create masks to filter only the channels you want.
    /// </summary>
    [Serializable]
    public class CinemachineChannelNames : ScriptableObject 
    {
        static CinemachineChannelNames s_Instance = null;
        static bool s_AlreadySearched = false;

        /// <summary>Get the singleton instance of this object, or null if it doesn't exist</summary>
        public static CinemachineChannelNames InstanceIfExists
        {
            get
            {
                if (!s_AlreadySearched)
                {
                    s_AlreadySearched = true;
                    var guids = AssetDatabase.FindAssets("t:CinemachineChannelNames");
                    for (int i = 0; i < guids.Length && s_Instance == null; ++i)
                        s_Instance = AssetDatabase.LoadAssetAtPath<CinemachineChannelNames>(
                            AssetDatabase.GUIDToAssetPath(guids[i]));
                }
                if (s_Instance != null)
                    s_Instance.EnsureDefaultChannel();
                return s_Instance;
            }
        }

        /// <summary>Get the singleton instance of this object.  Creates asset if nonexistant</summary>
        public static CinemachineChannelNames Instance
        {
            get
            {
                if (InstanceIfExists == null)
                {
                    var newAssetPath = EditorUtility.SaveFilePanelInProject(
                            "Create Channel Names asset", "CinemachineChannelNames", "asset", 
                            "This editor-only file will define the Cinemachine Channel Names for this project");
                    if (!string.IsNullOrEmpty(newAssetPath))
                    {
                        s_Instance = CreateInstance<CinemachineChannelNames>();
                        AssetDatabase.CreateAsset(s_Instance, newAssetPath);
                        AssetDatabase.SaveAssets();
                        AssetDatabase.Refresh();
                    }
                }
                if (s_Instance != null)
                    s_Instance.EnsureDefaultChannel();
                return s_Instance;
            }
        }

        // Make sure it has at least one layer in it
        void EnsureDefaultChannel()
        {
            if (ChannelNames == null || ChannelNames.Length == 0)
                ChannelNames = new string[] { "Default" };
        }

        /// <summary>The currently-defined Impulse channel names</summary>
        public string[] ChannelNames;
    }
}
