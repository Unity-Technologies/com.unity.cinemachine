using Cinemachine.Cookbook.Settings;
using RecipeEngine.Api.Extensions;
using RecipeEngine.Api.Jobs;
using RecipeEngine.Api.Recipes;
using RecipeEngine.Modules.Wrench.Models;
using CancelLeftoverJobs = RecipeEngine.Api.Triggers.CancelLeftoverJobs;
using Schedule = RecipeEngine.Api.Triggers.Recurring.Schedule;

namespace Cinemachine.Cookbook.Recipes;

public class Triggers : RecipeBase
{
    private readonly CinemachineSettings config = new();
    private const string packageName = "com.unity.cinemachine";
    private const string branchName = "main";

    protected override ISet<Job> LoadJobs()
        => Combine.Collections(GetTriggers()).SelectJobs();

    private ISet<IJobBuilder> GetTriggers()
    {
        HashSet<IJobBuilder> builders = new();
        var validationTests = config.Wrench.WrenchJobs[packageName][JobTypes.Validation];
        var projectTests = new ProjectTests().AsDependencies();
        var cleanConsoleTests = new CleanConsoleTests().AsDependencies();
        
        builders.Add(JobBuilder.Create($"Nightly Trigger")
            .WithDescription("Nightly check on main")
            .WithDependencies(projectTests)
            .WithDependencies(validationTests)
            .WithDependencies(cleanConsoleTests)
            .WithScheduleTrigger(Schedule.RunDaily(branchName))
        );
        
        builders.Add(JobBuilder.Create($"All Trigger")
            .WithDependencies(projectTests)
            .WithDependencies(validationTests)
            .WithBranchesTrigger(b => b.Only(branchName, "release[/]\\\\d+[.]\\\\d+)"))
            .WithDescription("All tests defined in recipes. Run in changes to mainline and release branches.")
        );

        var prProjectTests = projectTests.Where(job => job.JobId.Contains("Windows"));
        var prValidationTests = config.Wrench.WrenchJobs[packageName][JobTypes.Validation].Where(job => job.JobId.Contains("win"));

        builders.Add(JobBuilder.Create("Pull Request Tests Trigger")
            .WithDependencies(prProjectTests)
            .WithDependencies(prValidationTests)
            .WithPullRequestTrigger(pr => pr.ExcludeDraft(),
                true, CancelLeftoverJobs.Always)
            .WithDescription("Tests to run on PRs.")
        );

        return builders;
    }
}