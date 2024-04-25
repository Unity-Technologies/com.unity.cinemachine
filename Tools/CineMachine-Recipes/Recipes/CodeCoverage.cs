using Cinemachine.Cookbook.Settings;
using RecipeEngine.Api.Dependencies;
using RecipeEngine.Api.Extensions;
using RecipeEngine.Api.Jobs;
using RecipeEngine.Api.Platforms;
using RecipeEngine.Api.Recipes;
using RecipeEngine.Modules.UnifiedTestRunner;
using RecipeEngine.Modules.UpmCi;
using RecipeEngine.Modules.UpmPvp;
using RecipeEngine.Modules.Wrench.Models;
using RecipeEngine.Platforms;

namespace Cinemachine.Cookbook.Recipes;

public class CodeCoverage :RecipeBase
{
    protected override ISet<Job> LoadJobs()
        => Combine.Collections(GetJobs()).SelectJobs();

    private static readonly CinemachineSettings settings = new();
    private const string PackageName = "com.unity.cinemachine";
    private static readonly Platform Platform = settings.Wrench.Packages[PackageName].EditorPlatforms[SystemType.Ubuntu];
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
                .WithCommands( c => c
                    .Add("npm install upm-ci-utils@stable -g --registry https://artifactory.prd.cds.internal.unity3d.com/artifactory/api/npm/upm-npm")
                    .Add("upm-ci package test -u trunk --package-path com.unity.cinemachine --type package-tests --enable-code-coverage --code-coverage-options \"generateAdditionalMetrics;generateHtmlReport;assemblyFilters:+Unity.Cinemachine*\" --extra-utr-arg=--coverage-results-path=${YAMATO_SOURCE_DIR}/upm-ci~/test-results/CoverageResults")
                    .Add("curl -Os https://uploader.codecov.io/latest/linux/codecov")
                    .Add("./codecov -v -t \"${CODECOV_TOKEN}\" -B \"${GIT_BRANCH}\" -T \"${GIT_TAG}\" -P \"${YAMATO_PR_ID}\" -f \"upm-ci~/test-results/CoverageResults/**/*.xml\"")
                    )
                .WithUpmCiArtifacts()
                .WithDescription($"Generate codecov data for {settings.Wrench.Packages[PackageName].DisplayName} on {Platform.System}")
                .WithDependencies(settings.Wrench.WrenchJobs[PackageName][JobTypes.Pack]);
                            
        builders.Add(job);
        return builders;
    }
}