using AStarDev.ScraperPlaying.Startup;
using AStarDev.ScraperPlaying.UI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Xunit;

namespace AStarDev.ScraperPlaying.TestsIntegration.Startup;

/// <summary>Builds the real registrations and resolves the main window's collaborators, so a missing registration fails here rather than when the application starts.</summary>
public sealed class GivenTheApplicationServices : IDisposable
{
    private readonly string databasePath = Path.Combine(Path.GetTempPath(), $"astardev-scraperplaying-applicationservices-{Guid.CreateVersion7():N}.db");
    private readonly ServiceProvider serviceProvider;
    private bool disposed;

    public GivenTheApplicationServices()
    {
        var configuration = new ConfigurationBuilder().Build();
        serviceProvider = new ServiceCollection()
            .AddConfigurationServices(configuration)
            .AddDataServices(databasePath)
            .AddInfrastructureServices()
            .AddApplicationServices(configuration)
            .AddLogging()
            .BuildServiceProvider();
    }

    [Fact]
    public void when_the_real_registrations_are_validated_then_no_service_captures_a_shorter_lived_dependency()
    {
        var configuration = new ConfigurationBuilder().Build();
        using var validated = new ServiceCollection()
            .AddConfigurationServices(configuration)
            .AddDataServices(databasePath)
            .AddInfrastructureServices()
            .AddApplicationServices(configuration)
            .AddLogging()
            .BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true, ValidateOnBuild = true });

        validated.ShouldNotBeNull();
    }

    [Fact]
    public void when_the_main_window_collaborators_are_resolved_then_each_is_a_single_shared_instance()
    {
        var resolved = new object[]
        {
            serviceProvider.GetRequiredService<StatusReporter>(),
            serviceProvider.GetRequiredService<ApplicationReadiness>(),
            serviceProvider.GetRequiredService<UserOperationRunner>(),
            serviceProvider.GetRequiredService<ConfigurationBrowser>(),
            serviceProvider.GetRequiredService<ScrapeRunner>(),
            serviceProvider.GetRequiredService<ImageDisplayCoordinator>()
        };

        resolved.ShouldBe(
        [
            serviceProvider.GetRequiredService<StatusReporter>(),
            serviceProvider.GetRequiredService<ApplicationReadiness>(),
            serviceProvider.GetRequiredService<UserOperationRunner>(),
            serviceProvider.GetRequiredService<ConfigurationBrowser>(),
            serviceProvider.GetRequiredService<ScrapeRunner>(),
            serviceProvider.GetRequiredService<ImageDisplayCoordinator>()
        ]);
    }

    public void Dispose()
    {
        if (disposed) return;

        disposed = true;
        serviceProvider.Dispose();
        if (File.Exists(databasePath)) File.Delete(databasePath);
    }
}
