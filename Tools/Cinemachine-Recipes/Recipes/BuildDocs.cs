using Cinemachine.Cookbook.Settings;
using RecipeEngine.Api.Dependencies;
using RecipeEngine.Api.Extensions;
using RecipeEngine.Api.Jobs;
using RecipeEngine.Api.Platforms;
using RecipeEngine.Api.Recipes;
using RecipeEngine.Modules.UpmCi;
using RecipeEngine.Platforms;

namespace Cinemachine.Cookbook.Recipes;

public class BuildDocs : RecipeBase
{
    protected override ISet<Job> LoadJobs()
        => Combine.Collections(GetJobs()).SelectJobs();

    private static readonly CinemachineSettings Settings = new();
    private const string PackageName = "com.unity.cinemachine";

    private static readonly Platform
        Platform = Settings.Wrench.Packages[PackageName].EditorPlatforms[SystemType.MacOS];

    private const string EditorVersion = "trunk";

    public IEnumerable<Dependency> AsDependencies()
    {
        return this.Jobs.ToDependencies(this);
    }

    private List<IJobBuilder> GetJobs()
    {
        List<IJobBuilder> builders = new();
        IJobBuilder job = JobBuilder.Create($"Code coverage - {Platform.System}  - {EditorVersion}")
            .WithPlatform(Platform)
            .WithCommands(c => c
                .AddBrick("git@github.cds.internal.unity3d.com:wind-xu/virtual_production_doc_generation.git@v0.3.0",
                    ("EDITOR_VERSION", "trunk"), ("PACKAGE_NAME", PackageName), ("PACKAGE_PATH", PackageName))
            )
            .WithDescription(
                $"Generate codecov data for {Settings.Wrench.Packages[PackageName].DisplayName} on {Platform.System}");


        builders.Add(job);
        return builders;
    }
}