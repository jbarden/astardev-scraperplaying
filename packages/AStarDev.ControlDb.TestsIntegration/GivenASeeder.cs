using AStarDev.ControlDb.ScrapeConfiguration;
using Microsoft.EntityFrameworkCore;

namespace AStarDev.ControlDb.TestsIntegration;

public sealed class GivenASeeder : IDisposable
{
    private readonly string databasePath = Path.Combine(Path.GetTempPath(), $"astardev-controldb-seeder-{Guid.CreateVersion7():N}.db");
    private readonly ControlDbContext context;
    private bool disposed;

    public GivenASeeder()
    {
        var options = new DbContextOptionsBuilder<ControlDbContext>()
            .UseSqlite($"Data Source={databasePath}")
            .Options;

        context = new ControlDbContext(options);
        context.Database.EnsureDeleted();
        context.Database.EnsureCreated();
    }

    [Fact]
    public async Task when_the_database_is_empty_then_seeding_adds_a_scrape_configuration()
    {
        await Seeder.SeedAsync(context, cancellationToken: TestContext.Current.CancellationToken);

        var configurations = await context.Set<ScrapeConfigurationEntity>().ToListAsync(TestContext.Current.CancellationToken);

        configurations.Count.ShouldBe(1);
    }

    [Fact]
    public async Task when_the_database_already_contains_a_scrape_configuration_then_seeding_again_does_not_duplicate_it()
    {
        await Seeder.SeedAsync(context, cancellationToken: TestContext.Current.CancellationToken);

        await Seeder.SeedAsync(context, cancellationToken: TestContext.Current.CancellationToken);

        var configurations = await context.Set<ScrapeConfigurationEntity>().ToListAsync(TestContext.Current.CancellationToken);
        configurations.Count.ShouldBe(1);
    }

    [Fact]
    public async Task when_the_backup_search_categories_file_has_an_incompatible_schema_then_seeding_falls_back_to_defaults_instead_of_throwing()
    {
        var backupFilePath = Path.Combine(Path.GetTempPath(), $"astardev-controldb-seeder-backup-{Guid.CreateVersion7():N}.json");
        await File.WriteAllTextAsync(
            backupFilePath,
            """[{"Id":"1","SearchConfigurationId":"01a0a970-be92-736e-b4a0-baab52d8e0f6","Name":"Legacy","IncludeInSearch":1,"IsFamous":0,"IsInternet":0,"CreatedAt_Ticks":0,"UpdatedAt_Ticks":0}]""",
            TestContext.Current.CancellationToken);

        try
        {
            await Seeder.SeedAsync(context, backupFilePath, TestContext.Current.CancellationToken);

            var configurations = await context.Set<ScrapeConfigurationEntity>().ToListAsync(TestContext.Current.CancellationToken);
            configurations.Count.ShouldBe(1);
        }
        finally
        {
            File.Delete(backupFilePath);
        }
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    private void Dispose(bool disposing)
    {
        if (disposed)
            return;

        disposed = true;

        if (!disposing)
            return;

        context.Dispose();
        if (File.Exists(databasePath))
        {
            File.Delete(databasePath);
        }
    }
}
