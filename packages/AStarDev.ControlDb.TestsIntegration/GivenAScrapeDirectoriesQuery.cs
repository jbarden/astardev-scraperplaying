using AStarDev.ControlDb.ScrapeConfiguration;
using AStarDev.ControlDb.TestsIntegration.TestDataFactories;
using AStarDev.FunctionalParadigm;
using Microsoft.EntityFrameworkCore;

namespace AStarDev.ControlDb.TestsIntegration;

public sealed class GivenAScrapeDirectoriesQuery : IDisposable
{
    private readonly string databasePath = Path.Combine(Path.GetTempPath(), $"astardev-controldb-directories-query-{Guid.CreateVersion7():N}.db");
    private readonly ControlDbContext context;
    private bool disposed;

    public GivenAScrapeDirectoriesQuery()
    {
        var options = new DbContextOptionsBuilder<ControlDbContext>()
            .UseSqlite($"Data Source={databasePath}")
            .Options;

        context = new ControlDbContext(options);
        context.Database.EnsureDeleted();
        context.Database.EnsureCreated();
    }

    [Fact]
    public async Task when_a_configuration_is_stored_then_its_directories_are_returned()
    {
        await ReplaceConfigurationAsync();
        var query = new ScrapeDirectoriesQuery(context);

        var result = await query.GetAsync(TestContext.Current.CancellationToken);

        var directories = result.Match(option => option, exception => throw exception).Match(found => found, () => throw new InvalidOperationException("Expected directories."));
        directories.RootDirectory.ShouldBe("root-save-directory");
        directories.RootDirectoryFamous.ShouldBe("root-directory-famous");
    }

    [Fact]
    public async Task when_the_directories_are_loaded_then_nothing_is_tracked_by_the_context()
    {
        await ReplaceConfigurationAsync();
        var query = new ScrapeDirectoriesQuery(context);

        await query.GetAsync(TestContext.Current.CancellationToken);

        context.ChangeTracker.Entries().ShouldBeEmpty();
    }

    [Fact]
    public async Task when_no_configuration_is_stored_then_none_is_returned()
    {
        context.ScrapeConfigurations.RemoveRange(context.ScrapeConfigurations);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);
        context.ChangeTracker.Clear();
        var query = new ScrapeDirectoriesQuery(context);

        var result = await query.GetAsync(TestContext.Current.CancellationToken);

        result.Match(option => option, exception => throw exception).Match(_ => false, () => true).ShouldBeTrue();
    }

    private async Task ReplaceConfigurationAsync()
    {
        context.ScrapeConfigurations.RemoveRange(context.ScrapeConfigurations);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);
        await context.ScrapeConfigurations.AddAsync(ScrapeConfigurationEntityFactory.CreateScrapeConfigurationEntity(), TestContext.Current.CancellationToken);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);
        context.ChangeTracker.Clear();
    }

    public void Dispose()
    {
        if (disposed) return;

        context.Dispose();
        if (File.Exists(databasePath)) File.Delete(databasePath);

        disposed = true;
    }
}
