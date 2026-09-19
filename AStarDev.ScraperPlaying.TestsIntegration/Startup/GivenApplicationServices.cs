using AStarDev.ControlDb;
using AStarDev.ScraperPlaying.Startup;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Xunit;

namespace AStarDev.ScraperPlaying.TestsIntegration.Startup;

/// <summary>Builds the real service provider with scope validation on, so a singleton that depends on a scoped service fails the build.</summary>
public sealed class GivenApplicationServices
{
    [Fact]
    public async Task when_the_database_is_migrated_from_the_root_provider_with_scope_validation_then_the_context_factory_can_create_a_context()
    {
        var databasePath = Path.Combine(Path.GetTempPath(), $"astardev-scraperplaying-appservices-{Guid.CreateVersion7():N}.db");
        var configuration = new ConfigurationBuilder().Build();
        await using var provider = new ServiceCollection()
            .AddConfigurationServices(configuration)
            .AddDataServices(databasePath)
            .AddInfrastructureServices()
            .AddApplicationServices(configuration)
            .AddLogging()
            .BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true, ValidateOnBuild = true });

        try
        {
            await DatabaseMigrator.MigrateAsync(provider.GetRequiredService<IDbContextFactory<ControlDbContext>>(), provider.GetRequiredService<ILogger<GivenApplicationServices>>());
        }
        finally
        {
            SqliteConnection.ClearAllPools();
            if (File.Exists(databasePath)) File.Delete(databasePath);
        }
    }

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
