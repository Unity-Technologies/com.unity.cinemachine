using Cinemachine.Cookbook.Settings;
using RecipeEngine.Api.Recipes;
using RecipeEngine.Api.Dependencies;
using RecipeEngine.Api.Extensions;
using RecipeEngine.Api.Jobs;
using RecipeEngine.Api.Platforms;
using RecipeEngine.Modules.Wrench.Platforms;
using RecipeEngine.Platforms;

namespace Cinemachine.Cookbook.Recipes;

public class CleanConsoleTests: RecipeBase
{
    CinemachineSettings settings = new();

    protected override ISet<Job> LoadJobs()
        => Combine.Collections(GetJobs()).SelectJobs();

    public string GetJobName(UnityEditor editor, SystemType systemType)
        => $"Clean console tests - {editor.Version} - {systemType}";

    public IEnumerable<Dependency> AsDependencies()
    {
        return this.Jobs.ToDependencies(this);
    }

    private List<IJobBuilder> GetJobs()
    {
        var allSupportedEditors =
            settings.Wrench.Packages[CinemachineSettings.packageName].UnityEditors;

        List<IJobBuilder> builders = new();
        foreach (var unityEditor in allSupportedEditors)
        {
            foreach (var platform in unityEditor.EditorPlatforms)
            {
                var jobName = GetJobName(unityEditor, platform.System);
                IJobBuilder job = JobBuilder.Create(jobName)
                    .WithDescription(
                        $"Clean console tests for {settings.Wrench.Packages[CinemachineSettings.packageName].DisplayName} on {platform.System}")
                    .WithPlatform(platform)
                    .WithCommands(c => c
                        .AddBrick("git@github.cds.internal.unity3d.com:wind-xu/clean_console_test_brick.git@v0.3.7",
                            ("EDITOR_VERSION", settings.Wrench.EditorVersionToBranches[unityEditor.Version.ToString()]),
                            ("CLEAN_CONSOLE_TEST_FOR", "package"),
                            ("PACKAGE_PATH", CinemachineSettings.packageName),
                            ("EXEMPTION_FILE_DIR", ".yamato"),
                            ("WARNINGS_AS_ERRORS", false))
                    )
                    .WithArtifact("logs", "TestResults/**/*");
                builders.Add(job);
            }
        }

        return builders;
    }
}
