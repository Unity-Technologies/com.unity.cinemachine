using RecipeEngine.Modules.Wrench.Settings;

namespace Cinemachine.Cookbook.Settings;

public class CinemachineSettings
{
    // Path from the root of the repository where packages are located.
    private static string PackagesRootPath = ".";

    // update this to list all packages in this repo that you want to release.
    private static List<string> PackagesToRelease = new()
    {
        "com.unity.cinemachine",
    };
    ISet<string> PvPprofilesToCheck = new HashSet<string>() { "PVP-20-1" };
    public CinemachineSettings()
    {
        Settings = new WrenchSettings(
            PackagesRootPath,
            PackagesToRelease,
            useLocalPvpExemptions:true
        );
        
        Settings.PvpProfilesToCheck = PvPprofilesToCheck;
    }

    public WrenchSettings Settings { get; set; }
}
