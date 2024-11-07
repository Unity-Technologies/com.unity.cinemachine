using RecipeEngine.Modules.Wrench.Models;
using RecipeEngine.Modules.Wrench.Settings;

namespace Cinemachine.Cookbook.Settings;

public class CinemachineSettings
{
    // Path from the root of the repository where packages are located.
    string[] PackagesRootPaths = { "." };

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

    public readonly string[] ProjectNames = new[] { "HDRP", "HDRP2019.4", "Standalone", "URP" };

    public bool ProjectAndEditorAreCompatible(string project, string editorVersion)
    {
        if (editorVersion == "2019.4" && project == "HDRP")
            return false;
        if (editorVersion != "2019.4" && project == "HDRP2019.4")
            return false;
        return true;
    }

    ISet<string> PvPprofilesToCheck = new HashSet<string>() { "PVP-20-1" };

    public CinemachineSettings()
    {
        Wrench = new WrenchSettings(
            PackagesRootPaths,
            PackageOptions,
            useLocalPvpExemptions: true
        );

        Wrench.PvpProfilesToCheck = PvPprofilesToCheck;
    }

    public WrenchSettings Wrench { get; set; }
}