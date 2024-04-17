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
    
    public readonly string ProjectsDir = "Projects";
    public readonly string[] ProjectNames = new[] { "HDRP", "HDRPInputSystem", "Standalone", "StandaloneInputSystem", "URP", "URPInputSystem" };
    
    ISet<string> PvPprofilesToCheck = new HashSet<string>() { "PVP-20-1" };
    public CinemachineSettings()
    {
        Wrench = new WrenchSettings(
            PackagesRootPath,
            PackagesToRelease,
            useLocalPvpExemptions:true
        );
        
        Wrench.PvpProfilesToCheck = PvPprofilesToCheck;
    }

    public WrenchSettings Wrench { get; set; }
}
