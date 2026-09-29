using AStarDev.ControlDb;
using AStarDev.ScraperPlaying.Startup;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Shouldly;
using Xunit;

namespace AStarDev.ScraperPlaying.TestsIntegration.Startup;

/// <summary>Runs the real startup migration against a temp SQLite file, so a model change without a matching migration fails here rather than at application start.</summary>
public sealed class GivenTheDatabaseMigrator : IDisposable
{
    private readonly string databasePath = Path.Combine(Path.GetTempPath(), $"astardev-scraperplaying-migrator-{Guid.CreateVersion7():N}.db");
    private readonly ServiceProvider serviceProvider;
    private bool disposed;

    public GivenTheDatabaseMigrator() =>
        serviceProvider = new ServiceCollection().AddDataServices(databasePath).BuildServiceProvider();

    [Fact]
    public async Task when_a_new_database_is_migrated_then_it_succeeds()
    {
        var factory = serviceProvider.GetRequiredService<IDbContextFactory<ControlDbContext>>();

        await Should.NotThrowAsync(() => DatabaseMigrator.MigrateAsync(factory, NullLogger.Instance));
    }

    [Fact]
    public async Task when_a_new_database_is_migrated_then_the_model_has_no_pending_changes()
    {
        var factory = serviceProvider.GetRequiredService<IDbContextFactory<ControlDbContext>>();
        await DatabaseMigrator.MigrateAsync(factory, NullLogger.Instance);

        await using var context = await factory.CreateDbContextAsync(TestContext.Current.CancellationToken);

        context.Database.HasPendingModelChanges().ShouldBeFalse();
    }

    [Fact]
    public async Task when_a_database_with_data_is_migrated_to_the_latest_version_then_the_data_is_kept()
    {
        var factory = serviceProvider.GetRequiredService<IDbContextFactory<ControlDbContext>>();
        await using (var older = await factory.CreateDbContextAsync(TestContext.Current.CancellationToken))
        {
            await older.GetService<IMigrator>().MigrateAsync("20260929073110_AddPersonCategories", TestContext.Current.CancellationToken);
            await Seeder.SeedAsync(older, TestContext.Current.CancellationToken);
            await older.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        await DatabaseMigrator.MigrateAsync(factory, NullLogger.Instance);

        await using var context = await factory.CreateDbContextAsync(TestContext.Current.CancellationToken);
        var configuration = await context.ScrapeConfigurations.Include(scrapeConfiguration => scrapeConfiguration.SearchConfiguration).ThenInclude(search => search.SearchCategories).Include(scrapeConfiguration => scrapeConfiguration.SearchConfiguration).ThenInclude(search => search.PersonCategories).SingleAsync(TestContext.Current.CancellationToken);
        configuration.LoginUrl.ShouldBe("https://example.com/login");
        configuration.SearchConfiguration.SearchCategories.Count.ShouldBe(3);
        configuration.SearchConfiguration.PersonCategories.Count.ShouldBe(5);
    }

    public void Dispose()
    {
        if (disposed) return;

        disposed = true;
        serviceProvider.Dispose();
        if (File.Exists(databasePath)) File.Delete(databasePath);
    }
}
