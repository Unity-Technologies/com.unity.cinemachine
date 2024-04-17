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
    
    protected override ISet<Job> LoadJobs()
        => Combine.Collections(GetTriggers()).SelectJobs();

    private ISet<IJobBuilder> GetTriggers()
    {
        HashSet<IJobBuilder> builders = new();
        var nightlyDeps = config.Wrench.WrenchJobs["com.unity.cinemachine"][JobTypes.Validation];
        var projectTests = new ProjectTest().AsDependencies();
        
        builders.Add(JobBuilder.Create($"Nightly Trigger")
            .WithDependencies(projectTests)
            .WithDependencies(nightlyDeps)
            .WithPullRequestTrigger(pr => pr.ExcludeDraft())
        );
        return builders;
    }
}