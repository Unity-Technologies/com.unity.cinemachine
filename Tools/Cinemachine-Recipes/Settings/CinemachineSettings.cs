using RecipeEngine.Api.Platforms;
using RecipeEngine.Modules.Wrench.Models;
using RecipeEngine.Modules.Wrench.Settings;
using RecipeEngine.Platforms;

namespace Cinemachine.Cookbook.Settings;

public class CinemachineSettings
{
    // Path from the root of the repository where packages are located.
    string[] PackagesRootPaths = { "." };
    
    // Environment variables
    public const string packageName = "com.unity.cinemachine";


    // update this to list all packages in this repo that you want to release.
    Dictionary<string, PackageOptions> PackageOptions = new()
    {
        {
            "com.unity.cinemachine",
            new PackageOptions()
            {
                ReleaseOptions = new ReleaseOptions() { IsReleasing = true }, // Will generate jobs for this packages.
                ValidationOptions = new ValidationOptions()
                {
                    // Pin code coverage package to 1.3.0 temporarily
                    // See https://unity.slack.com/archives/C18KJF78T/p1773654217935869 for details
                    AdditionalUtrArguments = ["--coverage-pkg-version=1.3.0"] 
                }
            }
        },
    };

    public readonly string ProjectsDir = "Projects";

    public readonly string[] ProjectNames = new[]
        { "HDRP", "HDRPInputSystem", "Standalone", "StandaloneInputSystem", "URP", "URPInputSystem" };

    ISet<string> PvPprofilesToCheck = new HashSet<string>() { "supported" };

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