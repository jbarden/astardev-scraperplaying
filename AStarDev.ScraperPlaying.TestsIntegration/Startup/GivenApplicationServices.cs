using AStarDev.ScraperPlaying.Startup;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Xunit;

namespace AStarDev.ScraperPlaying.TestsIntegration.Startup;

/// <summary>Builds the real service provider with scope validation on, so a singleton that depends on a scoped service fails the build.</summary>
public sealed class GivenApplicationServices
{
    [Fact]
    public void when_the_provider_is_built_with_scope_validation_then_no_singleton_captures_a_scoped_service()
    {
        var configuration = new ConfigurationBuilder().Build();
        var services = new ServiceCollection()
            .AddConfigurationServices(configuration)
            .AddDataServices(Path.Combine(Path.GetTempPath(), $"astardev-scraperplaying-appservices-{Guid.CreateVersion7():N}.db"))
            .AddInfrastructureServices()
            .AddApplicationServices(configuration)
            .AddLogging();

        Should.NotThrow(() => services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true, ValidateOnBuild = true }).Dispose());
    }
}
