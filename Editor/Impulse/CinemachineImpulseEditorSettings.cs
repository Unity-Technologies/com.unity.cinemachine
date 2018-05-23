using UnityEngine;
using System;
using System.Linq;
using UnityEditor;

namespace Cinemachine.Editor
{
    /// <summary>
    ///  This class contains setting for the Impulse system.  Specifically, it holds
    ///  the Impulse Channel definitions.  These work like Unity Layers, you can
    ///  define and name them, and create masks to filter only the layers you want.
    /// </summary>
    [Serializable]
    public class CinemachineImpulseEditorSettings : ScriptableObject 
    {
        static CinemachineImpulseEditorSettings _instance = null;
        public static CinemachineImpulseEditorSettings Instance
        {
            get
            {
                if (_instance == null)
                    _instance = Resources.FindObjectsOfTypeAll<
                        CinemachineImpulseEditorSettings>().FirstOrDefault();
                if (_instance == null)
                {
                    _instance = CreateInstance<CinemachineImpulseEditorSettings>();
                    AssetDatabase.CreateAsset(_instance, "Assets/CinemachineImpulseEditorSettings.asset");
                    AssetDatabase.SaveAssets();
                    AssetDatabase.Refresh();
                }
                // Make sure it has at least one layer in it
                if (_instance.ImpulseChannels == null || _instance.ImpulseChannels.Length == 0)
                    _instance.ImpulseChannels = new string[] { "default" };
                return _instance;
            }
        }
        public string[] ImpulseChannels;
    }
}
