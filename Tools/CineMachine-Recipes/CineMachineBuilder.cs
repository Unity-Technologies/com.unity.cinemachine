using RecipeEngine.Api.Jobs;
using RecipeEngine.Platforms;

namespace Cinemachine.Cookbook;

public class CineMachineBuilder
{
    static IJobBuilder Create(string name, Platform platform)
    {
        return JobBuilder.Create(name)
            .WithPlatform(platform);
    }
}