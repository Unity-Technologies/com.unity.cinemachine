using RecipeEngine.Modules.Wrench.Models;
using RecipeEngine.Modules.Wrench.Settings;

namespace Cinemachine.Cookbook.Settings;

public class CinemachineSettings
{
    // Path from the root of the repository where packages are located.
    string[] PackagesRootPaths = { "." };
    
    // Environment variables
    private const string packageName = "com.unity.cinemachine";


    // update this to list all packages in this repo that you want to release.
    Dictionary<string, PackageOptions> PackageOptions = new()
    {
        {
            "com.unity.cinemachine",
            new PackageOptions()
            {
                ReleaseOptions = new ReleaseOptions() { IsReleasing = true }, // Will generate jobs for this packages.
            }
        },
    };

    public readonly string ProjectsDir = "Projects";

    public readonly string[] ProjectNames = new[]
        { "HDRP", "HDRPInputSystem", "Standalone", "StandaloneInputSystem", "URP", "URPInputSystem" };

    ISet<string> PvPprofilesToCheck = new HashSet<string>() { "PVP-20-1" };

    public CinemachineSettings()
    {
        Wrench = new WrenchSettings(
            PackagesRootPaths,
            PackageOptions,
            useLocalPvpExemptions: true
        );

        Wrench.Packages[packageName].CoverageCommands.Enabled = true;
        
        // Exclude code coverage for tests
        Wrench.Packages[packageName].CoverageCommands.AssemblyAllowList.Add("^Unity.Cinemachine$");
        Wrench.Packages[packageName].CoverageCommands.AssemblyAllowList.Add("^Unity.Cinemachine.Editor$");

        Wrench.PvpProfilesToCheck = PvPprofilesToCheck;
    }

    public WrenchSettings Wrench { get; set; }
}