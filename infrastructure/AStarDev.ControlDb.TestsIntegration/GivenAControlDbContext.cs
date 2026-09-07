using AStarDev.ControlDb.ScrapeConfiguration;
using AStarDev.ControlDb.TestsIntegration.TestDataFactories;
using Microsoft.EntityFrameworkCore;

namespace AStarDev.ControlDb.TestsIntegration;

public sealed class GivenAControlDbContext : IDisposable
{
    private readonly string databasePath = Path.Combine(Path.GetTempPath(), $"astardev-controldb-context-{Guid.CreateVersion7():N}.db");
    private readonly ControlDbContext context;
    private bool disposed;

    public GivenAControlDbContext()
    {
        var options = new DbContextOptionsBuilder<ControlDbContext>()
            .UseSqlite($"Data Source={databasePath}")
            .Options;

        context = new ControlDbContext(options);
        context.Database.EnsureDeleted();
        context.Database.EnsureCreated();
    }

    [Fact]
    public void when_the_model_is_built_then_no_exception_is_thrown() => context.Model.ShouldNotBeNull();

    [Fact]
    public void when_accessed_the_scrape_configuration_repository_should_be_a_dbset() => context.ScrapeConfigurations.ShouldBeAssignableTo<DbSet<ScrapeConfigurationEntity>>();

    [Fact]
    public async Task when_the_database_is_created_then_a_scrape_configuration_entity_with_related_details_can_be_saved_and_reloaded()
    {
        var scrapeConfigurationEntity = ScrapeConfigurationEntityFactory.CreateScrapeConfigurationEntity();
        await context.ScrapeConfigurations.AddAsync(scrapeConfigurationEntity, TestContext.Current.CancellationToken);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var reloaded = await context.ScrapeConfigurations.FindAsync([scrapeConfigurationEntity.Id], TestContext.Current.CancellationToken);

        reloaded.ShouldNotBeNull();
        reloaded.UserConfiguration.EmailAddress.ShouldBe(scrapeConfigurationEntity.UserConfiguration.EmailAddress);
    }

    [Fact]
    public async Task when_the_database_is_created_then_a_file_detail_entity_with_related_details_can_be_saved_and_reloaded()
    {
        var FileEntity = FileEntityFactory.CreateFileEntity();
        await context.Files.AddAsync(FileEntity, TestContext.Current.CancellationToken);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var reloaded = await context.Files.FindAsync([FileEntity.Id], TestContext.Current.CancellationToken);

        reloaded.ShouldNotBeNull();
        reloaded.FileName.ShouldBe(FileEntity.FileName);
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
