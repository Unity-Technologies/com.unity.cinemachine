using System;
using System.Collections.Generic;
using System.IO;
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
            static Dictionary<string, Texture2D> s_IconCache = new();
            static Texture2D LoadAssetAtPathCached(string path)
            {
                if (!s_IconCache.ContainsKey(path)) 
                    s_IconCache.Add(path, AssetDatabase.LoadAssetAtPath<Texture2D>(path));
                return s_IconCache[path];
            }

            /// <summary>Checks if CmCamera script has the correct icon or not.</summary>
            /// <returns>True, when icons don't match. False, otherwise.</returns>
            public static bool DoIconsNeedToBeUpdated()
            {
                var cmCameraPath = ScriptableObjectUtility.CinemachineRelativeInstallPath + 
                    "/Runtime/Behaviours/CmCamera.cs";
                var monoImporter = AssetImporter.GetAtPath(cmCameraPath) as MonoImporter;
                if (monoImporter == null)
                    return false;

                var iconPath = GetIconPathForScript(monoImporter.GetScript());
                if (iconPath != string.Empty)
                {
                    var icon = LoadAssetAtPathCached(iconPath);
                    var scriptIcon = monoImporter.GetIcon();
                    return icon != scriptIcon;
                }

                return false;
            }

            /// <summary>Updates all icons of cinemachine runtime scripts according to the current theme.</summary>
            public static void UpdateIcons()
            {
                var cmScriptPaths = GetAllCinemachineRuntimeScripts();
                foreach (var cmScriptPath in cmScriptPaths)
                {
                    var monoImporter = AssetImporter.GetAtPath(cmScriptPath) as MonoImporter;
                    if (monoImporter == null)
                        continue;

                    var iconPath = GetIconPathForScript(monoImporter.GetScript());
                    if (iconPath != string.Empty)
                    {
                        var icon = LoadAssetAtPathCached(iconPath);
                        monoImporter.SetIcon(icon);
                        monoImporter.SaveAndReimport();
                    }
                }
                
                // local function
                static List<string> GetAllCinemachineRuntimeScripts()
                {
                    var cmRuntimeScripts = new List<string>();
                    var directories = Directory.GetDirectories(ScriptableObjectUtility.CinemachineRelativeInstallPath + "/Runtime");
                    foreach (var directory in directories)
                        cmRuntimeScripts.AddRange(Directory.GetFiles(directory, "*.cs"));
                    
                    return cmRuntimeScripts;
                }
            }
            
            static string GetIconPathForScript(MonoScript monoScript)
            {
                var scriptClass = monoScript.GetClass();
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
