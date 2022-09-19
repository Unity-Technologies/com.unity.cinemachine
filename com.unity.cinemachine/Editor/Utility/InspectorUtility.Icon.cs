using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Cinemachine.Editor
{
    static partial class InspectorUtility
    {
        /// <summary>
        /// This function is called (because of InitializeOnLoadMethod) anytime Unity loads: theme change, script change, etc.
        /// </summary>
        [InitializeOnLoadMethod]
        static void OnProjectReload()
        {
            if (IconUtility.DoIconsNeedToBeUpdated())
            {
                IconUtility.UpdateIcons();
            }
        }

        static class IconUtility
        {
            static readonly string[] k_Extensions = { ".cs" };
            static readonly string[] k_Packages = { "com.unity.cinemachine" };

            static List<string> s_Scripts;

            static List<string> GetScripts()
            {
                if (s_Scripts == null) 
                    s_Scripts = GetAllAssetPaths(k_Extensions, k_Packages);
                return s_Scripts;
            }

            public static bool DoIconsNeedToBeUpdated()
            {
                s_Scripts = GetAllAssetPaths(k_Extensions, k_Packages);
                foreach (var cmScriptPath in cmScriptPaths)
                {
                    var monoImporter = AssetImporter.GetAtPath(cmScriptPath) as MonoImporter;
                    if (monoImporter == null)
                        continue;

                    var iconPath = GetIconPath(monoImporter);
                    if (iconPath != string.Empty)
                    {
                        var icon = AssetDatabase.LoadAssetAtPath<Texture2D>(iconPath);
                        var icon2 = monoImporter.GetIcon();
                        return icon != icon2;
                    }
                }

                return false;
            }

            public static void UpdateIcons()
            {
                var cmScriptPaths = GetAllAssetPaths(k_Extensions, k_Packages);
                foreach (var cmScriptPath in cmScriptPaths)
                {
                    var monoImporter = AssetImporter.GetAtPath(cmScriptPath) as MonoImporter;
                    if (monoImporter == null)
                        continue;

                    var iconPath = GetIconPath(monoImporter);
                    if (iconPath != string.Empty)
                    {
                        var icon = AssetDatabase.LoadAssetAtPath<Texture2D>(iconPath);
                        monoImporter.SetIcon(icon);
                        monoImporter.SaveAndReimport();
                    }
                }
            }

            static List<string> GetAllAssetPaths(string[] withExtensions, string[] inPackages)
            {
                var assetPaths = AssetDatabase.GetAllAssetPaths();
                var filteredAssetPaths = new List<string>();
                foreach (var assetPath in assetPaths)
                    if (HasExtension(assetPath, withExtensions) && PartOfPackage(assetPath, inPackages))
                        filteredAssetPaths.Add(assetPath);
                return filteredAssetPaths;

                // local functions
                static bool HasExtension(string assetPath, IEnumerable<string> extensions)
                {
                    return extensions.Any(extension => assetPath.EndsWith(extension));
                }

                static bool PartOfPackage(string assetPath, IEnumerable<string> packages)
                {
                    return packages.Any(package => assetPath.Contains(package));
                }
            }

            static string GetIconPath(MonoImporter monoImporter)
            {
                var script = monoImporter.GetScript();
                var scriptClass = script.GetClass();
                if (scriptClass == null)
                    return string.Empty;

                if (scriptClass.IsSubclassOf(typeof(CinemachineExtension)))
                    return ScriptableObjectUtility.CinemachineRelativeInstallPath +
                        "/Editor/EditorResources/Icons/CmExtensions/" + (EditorGUIUtility.isProSkin ? "Dark/" : "Light/") +
                        "CinemachineExtensions@256.png";
                if (scriptClass.IsSubclassOf(typeof(CinemachineComponentBase)))
                    return ScriptableObjectUtility.CinemachineRelativeInstallPath +
                        "/Editor/EditorResources/Icons/CmComponent/" + (EditorGUIUtility.isProSkin ? "Dark/" : "Light/") +
                        "CMComponent@256.png";

                if (scriptClass == typeof(CinemachineSplineRoll) || scriptClass == typeof(CinemachineSplineCart))
                    return ScriptableObjectUtility.CinemachineRelativeInstallPath +
                        "/Editor/EditorResources/Icons/SplineTrack/" + (EditorGUIUtility.isProSkin ? "Dark/" : "Light/") +
                        "DollyTrack@256.png";

                if (scriptClass.IsSubclassOf(typeof(CinemachineVirtualCameraBase)) || scriptClass == typeof(CinemachineBrain))
                    return ScriptableObjectUtility.CinemachineRelativeInstallPath +
                        "/Editor/EditorResources/Icons/CmCamera/" + (EditorGUIUtility.isProSkin ? "Dark/" : "Light/") +
                        "VirtualCamera@256.png";

                return string.Empty;
            }
        }
    }
}
