using UnityEngine;
using System;
using System.Linq;
using UnityEditor;

namespace Cinemachine.Editor
{
    [Serializable]
    public class CinemachineImpulseSettings : ScriptableObject 
    {
        static CinemachineImpulseSettings _instance = null;
        public static CinemachineImpulseSettings Instance
        {
            get
            {
                if (_instance == null)
                    _instance = Resources.FindObjectsOfTypeAll<
                        CinemachineImpulseSettings>().FirstOrDefault();
                if (_instance == null)
                {
                    _instance = CreateInstance<CinemachineImpulseSettings>();
                    AssetDatabase.CreateAsset(_instance, "Assets/CinemachineImpulseSettings.asset");
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

    // GML this is here temporarily to maintain separation.  Move to CinemachineMenu.
    class ImpulseMenu 
    {
        [MenuItem(CinemachineMenu.kCinemachineRootMenu + "Impulse/Perlin Impulse Definition")]
        private static void CreatePerlinImpulseDefinition()
        {
            ScriptableObjectUtility.Create<CinemachinePerlinImpulseDefinition>();
        }

        [MenuItem(CinemachineMenu.kCinemachineRootMenu + "Impulse/Fixed Impulse Definition")]
        private static void CreateFixedImpulseDefinition()
        {
            ScriptableObjectUtility.Create<CinemachineFixedImpulseDefinition>();
        }
    }
}
