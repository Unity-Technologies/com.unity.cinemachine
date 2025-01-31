#if false
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Unity.Cinemachine.Editor
{
    /// <summary>
    /// Useful class for setting script icons in one go, instead of setting them by hand one by one
    /// </summary>
    static class IconUtility
    {
        static Dictionary<string, Texture2D> s_IconCache = new();
        static Texture2D LoadAssetAtPathCached(string path)
        {
            if (!s_IconCache.ContainsKey(path))
                s_IconCache.Add(path, AssetDatabase.LoadAssetAtPath<Texture2D>(path));
            return s_IconCache[path];
        }

        /// <summary>Checks if CinemachineCamera script has the correct icon or not.</summary>
        /// <returns>True, when icons don't match. False, otherwise.</returns>
        public static bool DoIconsNeedToBeUpdated()
        {
            var cmCameraPath = CinemachineCore.kPackageRoot + "/Runtime/Behaviours/CinemachineCamera.cs";
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
                var directories = Directory.GetDirectories(CinemachineCore.kPackageRoot + "/Runtime");
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
                return CinemachineCore.kPackageRoot + "/Editor/EditorResources/Icons/CmExtension@256.png";
            if (scriptClass.IsSubclassOf(typeof(CinemachineComponentBase)))
                return CinemachineCore.kPackageRoot + "/Editor/EditorResources/Icons/CmComponent@256.png";
            if (scriptClass == typeof(CinemachineSplineRoll) || scriptClass == typeof(CinemachineSplineCart))
                return CinemachineCore.kPackageRoot + "/Editor/EditorResources/Icons/CmTrack@256.png";
            if (scriptClass.IsSubclassOf(typeof(CinemachineVirtualCameraBase)) || scriptClass == typeof(CinemachineBrain))
                return CinemachineCore.kPackageRoot + "/Editor/EditorResources/Icons/CmCamera@256.png";
            return string.Empty;
        }

        [MenuItem("Cinemachine/Darken Icons")]
        static void DarkenIcons()
        {
            var icons = GetAllIcons();
            foreach (var path in icons)
            {
                var icon = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
                for (int m = 0; m < icon.mipmapCount; ++m)
                {
                    var pixels = icon.GetPixels(m);
                    for (int i = 0; i < pixels.Length; ++i)
                    {
                        var pixel = pixels[i];
                        Color.RGBToHSV(pixel, out var h, out var s, out var v);
                        if (s < 0.01f) // modify grey only
                            pixels[i] = Color.Lerp(pixel, new Color(0, 0, 0, pixel.a), 0.2f);
                    }
                    icon.SetPixels(pixels, m);
                }

                var bytes = icon.EncodeToPNG();
                File.WriteAllBytes(path, bytes);
            }

            // local function
            static string[] GetAllIcons()
            {
                return new[]
                {
                    CinemachineCore.kPackageRoot + "/Editor/EditorResources/Icons/CmExtension@256.png",
                    CinemachineCore.kPackageRoot + "/Editor/EditorResources/Icons/CmComponent@256.png",
                    CinemachineCore.kPackageRoot + "/Editor/EditorResources/Icons/CmTrack@256.png",
                    CinemachineCore.kPackageRoot + "/Editor/EditorResources/Icons/CmCamera@256.png"
                };
            }
        }

    }
}
#endif
