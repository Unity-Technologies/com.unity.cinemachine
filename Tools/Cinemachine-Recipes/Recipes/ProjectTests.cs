using Cinemachine.Cookbook.Settings;
using RecipeEngine.Api.Artifacts;
using RecipeEngine.Api.Extensions;
using RecipeEngine.Api.Jobs;
using RecipeEngine.Api.Platforms;
using RecipeEngine.Api.Recipes;
using RecipeEngine.Modules.UnifiedTestRunner;
using RecipeEngine.Modules.UpmPvp;
using RecipeEngine.Modules.Wrench.Models;
using RecipeEngine.Platforms;
using RecipeEngine.Api.Dependencies;

namespace Cinemachine.Cookbook.Recipes;

public class ProjectTests : RecipeBase
{
    protected override ISet<Job> LoadJobs()
        => Combine.Collections(GetJobs()).SelectJobs();

    public string GetJobName(string packageShortName, string project, string editorVersion, SystemType systemType)
        => $"Test Project - {packageShortName} - {project} - {editorVersion} - {systemType}";

    public IEnumerable<Dependency> AsDependencies()
    {
        return Jobs.ToDependencies(this);
    }

    public IEnumerable<IJobBuilder> GetJobs()
    {
        List<IJobBuilder> builders = new();
        CinemachineSettings settings = new();
        foreach (var packageName in settings.Wrench.PackagesToRelease)
        {
            var supportedEditors = settings.Wrench.Packages[packageName].UnityEditors;
            foreach (var unityEditor in supportedEditors)
            {
                var version = unityEditor.Version.ToString();
                foreach (var platform in unityEditor.EditorPlatforms)
                {
                    var yamatoSourceDir = platform.System == SystemType.Windows ? "%YAMATO_SOURCE_DIR%" : "$YAMATO_SOURCE_DIR";
                    var branch = settings.Wrench.EditorVersionToBranches[version];
                    foreach (var project in settings.ProjectNames)
                    {
                        IJobBuilder job = JobBuilder.Create(GetJobName(settings.Wrench.Packages[packageName].ShortName, project, version, platform.System))
                            .WithPlatform(platform)
                            .WithCommands(c => c
                                .Add($"unity-downloader-cli -u {branch} -c Editor --fast")
                                // Use the package tarball for testing.
                                .Add($"upm-pvp create-test-project {settings.ProjectsDir}/{project} --packages \"{UpmPvpCommand.kDefaultPackagesGlob}\" --unity .Editor")
                                .Add(UtrCommand.Run(platform.System, b => b
                                    .WithTestProject($"{settings.ProjectsDir}/{project}")
                                    .WithEditor(".Editor")
                                    .WithRerun(1, true)
                                    .WithArtifacts("artifacts")
                                    .WithSuite(UtrTestSuiteType.Editor)
                                    .WithExtraArgs("--suite=PlayMode")
                                    .WithExtraArgs("--enable-code-coverage", 
                                        "--coverage-options=\"generateAdditionalMetrics;generateHtmlReport;" + 
                                        $"assemblyFilters:+Unity.Cinemachine,+Unity.Cinemachine.Editor;pathReplacePatterns:@*,,**/PackageCache/,;sourcePaths:{yamatoSourceDir}/Packages;\"",
                                        $"--coverage-results-path={yamatoSourceDir}/upm-ci~/CodeCoverage",
                                        $"--coverage-upload-options=\"reportsDir:upm-ci~/CodeCoverage;name:inputsystem_{platform.System.ToString()}_{version}_project;flags:inputsystem_{platform.System.ToString()}_{version}_project\""))))
                            .WithDescription($"Run {project} project tests for {settings.Wrench.Packages[packageName].DisplayName} on {platform.System}")
                            .WithDependencies(settings.Wrench.WrenchJobs[packageName][JobTypes.Pack])
                            .WithArtifact(new Artifact("artifacts", "artifacts/*"));
                        builders.Add(job);
                    }
                }
            }
        }
        return builders;
    }
}