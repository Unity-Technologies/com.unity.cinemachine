using Cinemachine.Cookbook.Settings;
using RecipeEngine.Api.Extensions;
using RecipeEngine.Api.Jobs;
using RecipeEngine.Api.Recipes;
using RecipeEngine.Modules.Wrench.Models;
using Unity.Yamato.JobDefinition;
using Dependency = RecipeEngine.Api.Dependencies.Dependency;

namespace Cinemachine.Cookbook.Recipes;

public class Triggers : RecipeBase
{
    private readonly CinemachineSettings config = new ();
    private const string packageName = "com.unity.cinemachine";

    protected override ISet<Job> LoadJobs()
        => Combine.Collections(GetTriggers()).SelectJobs();

    private ISet<IJobBuilder> GetTriggers()
    {
        HashSet<IJobBuilder> builders = new();
        var validationTests = config.Wrench.WrenchJobs[packageName][JobTypes.Validation];
        var projectTests = new ProjectTest().AsDependencies();
        var codeCoverage = new CodeCoverage().AsDependencies();
        builders.Add(JobBuilder.Create($"Nightly Trigger")
            .WithDependencies(projectTests)
            .WithDependencies(validationTests)
            .WithDescription("Nightly check on main")

        );
        builders.Add(JobBuilder.Create($"All Trigger")
            .WithDependencies(projectTests)
            .WithDependencies(validationTests)
            .WithDependencies(codeCoverage)
            .WithPullRequestTrigger(pr => pr.ExcludeDraft())
            .WithDescription("All tests defined in recipes.")
        );

        var prsubset = config.Wrench.WrenchJobs[packageName][JobTypes.Validation].Where(job => job.JobId.Contains("windows") || job.JobId.Contains("6000"));

        builders.Add(JobBuilder.Create("Package CI")
            .WithDependencies(prsubset)
            .WithPullRequestTrigger(pr => pr.ExcludeDraft())
            .WithBranchesTrigger(b => b.Only("main", "release[/]\\\\d+[.]\\\\d+)"))
            .WithDescription("Tests to run on PRs and mainline branches.")
        );
        return builders;
    }
}