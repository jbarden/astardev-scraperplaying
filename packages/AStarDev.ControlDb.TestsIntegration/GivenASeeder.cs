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
        await Seeder.SeedAsync(context, TestContext.Current.CancellationToken);

        var configurations = await context.Set<ScrapeConfigurationEntity>().ToListAsync(TestContext.Current.CancellationToken);

        configurations.Count.ShouldBe(1);
    }

    [Fact]
    public async Task when_the_database_already_contains_a_scrape_configuration_then_seeding_again_does_not_duplicate_it()
    {
        await Seeder.SeedAsync(context, TestContext.Current.CancellationToken);

        await Seeder.SeedAsync(context, TestContext.Current.CancellationToken);

        var configurations = await context.Set<ScrapeConfigurationEntity>().ToListAsync(TestContext.Current.CancellationToken);
        configurations.Count.ShouldBe(1);
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
