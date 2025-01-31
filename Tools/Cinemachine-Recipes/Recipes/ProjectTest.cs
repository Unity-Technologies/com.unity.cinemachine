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

public class ProjectTest : RecipeBase
{


    protected override ISet<Job> LoadJobs()
        => Combine.Collections(GetJobs()).SelectJobs();

    public string GetJobName(string packageShortName, string project, string editorVersion, SystemType systemType)
        => $"Test Project - {packageShortName} - {project} - {editorVersion} - {systemType}";


    public IEnumerable<Dependency> AsDependencies()
    {
        return this.Jobs.ToDependencies(this);
    }

    public IEnumerable<IJobBuilder> GetJobs()
    {
        List<IJobBuilder> builders = new();
        CinemachineSettings settings = new();
        foreach (var packageName in settings.Wrench.PackagesToRelease)
        {
            var platforms = settings.Wrench.Packages[packageName].EditorPlatforms;
            foreach (var platform in platforms)
            {
                var supportedVersions = settings.Wrench.Packages[packageName].SupportedEditorVersions;
                foreach (var editorVersion in supportedVersions)
                    // foreach (var VARIABLE in _settings.Wrench.Packages[packageName].SupportedEditorVersions)
                {
                    foreach (var project in settings.ProjectNames)
                    {
                        IJobBuilder job = JobBuilder.Create(GetJobName(settings.Wrench.Packages[packageName].ShortName, project, editorVersion, platform.Key))
                            .WithPlatform(platform.Value)
                            .WithOptionalCommands(
                                platform.Value.RunsOnLinux(), c => c
                                    .Add("rm com.unity.cinemachine/Tests/.tests.json "))
                            .WithCommands(c => c
                                .Add($"unity-downloader-cli -u {editorVersion} -c Editor --fast")
                                // Use the package tarball for testing.
                                .Add($"upm-pvp create-test-project {settings.ProjectsDir}/{project} --packages \"{UpmPvpCommand.kDefaultPackagesGlob}\" --unity .Editor")
                                .Add(UtrCommand.Run(platform.Value.System, b => b
                                    .WithTestProject($"{settings.ProjectsDir}/{project}")
                                    .WithEditor(".Editor")
                                    .WithRerun(1, true)
                                    .WithArtifacts("artifacts")
                                    .WithSuite(UtrTestSuiteType.Editor)
                                    .WithExtraArgs("--suite=PlayMode"))))
                            .WithDescription($"Run {project} project tests for {settings.Wrench.Packages[packageName].DisplayName} on {platform.Key}")
                            .WithDependencies(settings.Wrench.WrenchJobs[packageName][JobTypes.Pack])
                            .WithArtifact(new Artifact("artifacts", "artifacts/*"));
                            ;

                        builders.Add(job);
                    }
                }
            }
        }

        return builders;
    }
}