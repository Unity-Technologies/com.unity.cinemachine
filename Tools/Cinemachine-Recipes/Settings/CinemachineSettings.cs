using RecipeEngine.Api.Platforms;
using RecipeEngine.Modules.Wrench.Models;
using RecipeEngine.Modules.Wrench.Settings;
using RecipeEngine.Platforms;

namespace Cinemachine.Cookbook.Settings;

public class CinemachineSettings
{
    // Path from the root of the repository where packages are located.
    string[] PackagesRootPaths = { "." };

    public static readonly string CinemachinePackageName = "com.unity.cinemachine";

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

    public readonly string[] ProjectNames = new[] { "HDRP", "Standalone", "URP" };

    public bool ProjectAndEditorAreCompatible(string project, string editorVersion) => true;

    //ISet<string> PvPprofilesToCheck = new HashSet<string>() { "PVP-20-1" };

    public CinemachineSettings()
    {
        Wrench = new WrenchSettings(
            PackagesRootPaths,
            PackageOptions,
            useLocalPvpExemptions: true
        );

        //Wrench.PvpProfilesToCheck = PvPprofilesToCheck;

        var defaultUbuntuPlatform = WrenchPackage.DefaultEditorPlatforms[SystemType.Ubuntu];
        // Use Ubuntu image package-ci/ubuntu-22.04 which is required by 6000.0+ versions.
        Wrench.Packages[CinemachinePackageName].EditorPlatforms[SystemType.Ubuntu] = new Platform(new Agent("package-ci/ubuntu-22.04:default",
            defaultUbuntuPlatform.Agent.Flavor, defaultUbuntuPlatform.Agent.Resource), defaultUbuntuPlatform.System);
    }

    public WrenchSettings Wrench { get; set; }
}