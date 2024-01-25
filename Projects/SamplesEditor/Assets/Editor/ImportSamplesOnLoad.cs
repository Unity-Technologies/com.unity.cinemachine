using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

public class ImportSamplesOnLoad
{
    
    [MenuItem("Cinemachine Samples/Load Samples")]
    // Start is called before the first frame update
    static void ImportSamples()
    {
        var samplesName = GetAllSamplesNames();
        ImportSamples(samplesName);
    }
    
    [MenuItem("Cinemachine Samples/Save Samples")]
    static void SaveSamples()
    {
        var paths = GetAllSamplesNames();
        SaveSamples(paths);
    }

    static List<string> GetAllSamplesNames()
    {
        var samplesName = new List<string>();
        samplesName.Add("Shared Assets");
        samplesName.Add("3D Samples");
        samplesName.Add("2D Samples");
        samplesName.Add("Input System Samples");
        return samplesName;
    }

    static void SaveSamples(List<string> paths)
    {
        foreach (var path in paths)
        {
            var dependencyPath = $"{Application.dataPath}/Samples/Cinemachine/SamplesEditor/{path}";
            if (Directory.Exists(dependencyPath))
            {
                var copyTo = Path.GetFullPath($"Packages/com.unity.cinemachine/Samples~/{path}");
                CopyDirectory(dependencyPath, copyTo);
            }
        }
        AssetDatabase.Refresh();
    }
    
    static void ImportSamples(List<string> paths)
    {
        foreach (var path in paths)
        {
            var dependencyPath = Path.GetFullPath($"Packages/com.unity.cinemachine/Samples~/{path}");
            if (Directory.Exists(dependencyPath))
            {
                var copyTo = 
                    $"{Application.dataPath}/Samples/Cinemachine/SamplesEditor/{path}";
                CopyDirectory(dependencyPath, copyTo);
            }
        }
        AssetDatabase.Refresh();
    }
    
    /// <summary>Copies a directory from the source to target path. Overwrites existing directories.</summary>
    static void CopyDirectory(string sourcePath, string targetPath)
    {
        // Verify source directory
        var source = new DirectoryInfo(sourcePath);
        if (!source.Exists)
            throw new DirectoryNotFoundException($"{sourcePath}  directory not found");

        // Delete pre-existing directory at target path
        var target = new DirectoryInfo(targetPath);
        if (target.Exists)
            target.Delete(true);

        Directory.CreateDirectory(targetPath);

        // Copy all files to target path
        foreach (FileInfo file in source.GetFiles())
        {
            var newFilePath = Path.Combine(targetPath, file.Name);
            file.CopyTo(newFilePath);
        }

        // Recursively copy all subdirectories
        foreach (DirectoryInfo child in source.GetDirectories())
        {
            var newDirectoryPath = Path.Combine(targetPath, child.Name);
            CopyDirectory(child.FullName, newDirectoryPath);
        }
    }
}
