using AStarDev.ControlDb.ScrapeConfiguration;
using AStarDev.ControlDb.TestsIntegration.TestDataFactories;
using AStarDev.FunctionalParadigm;
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
    public async Task when_a_scrape_configuration_is_added_with_empty_ids_then_they_are_generated_and_related_rows_stay_linked()
    {
        var scrapeConfigurationEntity = ScrapeConfigurationEntityFactory.CreateScrapeConfigurationEntity();
        await context.ScrapeConfigurations.AddAsync(scrapeConfigurationEntity, TestContext.Current.CancellationToken);

        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        scrapeConfigurationEntity.Id.Value.ShouldNotBe(Guid.Empty);
        scrapeConfigurationEntity.UserConfiguration.Id.Value.ShouldNotBe(Guid.Empty);
        scrapeConfigurationEntity.SearchConfiguration.Id.Value.ShouldNotBe(Guid.Empty);
        scrapeConfigurationEntity.ScrapeDirectories.Id.Value.ShouldNotBe(Guid.Empty);
        scrapeConfigurationEntity.UserConfiguration.ScrapeConfigurationEntityId.ShouldBe(scrapeConfigurationEntity.Id);
        scrapeConfigurationEntity.SearchConfiguration.ScrapeConfigurationId.ShouldBe(scrapeConfigurationEntity.Id);
        scrapeConfigurationEntity.ScrapeDirectories.ScrapeConfigurationEntityId.ShouldBe(scrapeConfigurationEntity.Id);
    }

    [Fact]
    public async Task when_a_scrape_configuration_is_added_with_explicit_ids_then_they_are_preserved()
    {
        var explicitId = new ScrapeConfigurationId(Guid.CreateVersion7());
        var scrapeConfigurationEntity = new ScrapeConfigurationEntity(explicitId)
        {
            UserConfiguration = new UserConfigurationEntity(new UserConfigurationId(Guid.CreateVersion7()), explicitId, "user@example.com", "username", "password", "session-cookie", "apiKey"),
            SearchConfiguration = new SearchConfigurationEntity(new SearchConfigurationId(Guid.CreateVersion7()), explicitId, "search-config", 10, []),
            ScrapeDirectories = new ScrapeDirectoriesEntity(new ScrapeDirectoriesId(Guid.CreateVersion7()), explicitId, "scrape-directory", "base-save-directory", "base-directory", "base-directory-famous", "sub-directory-name")
        };
        await context.ScrapeConfigurations.AddAsync(scrapeConfigurationEntity, TestContext.Current.CancellationToken);

        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        scrapeConfigurationEntity.Id.ShouldBe(explicitId);
    }

    [Fact]
    public async Task when_try_get_first_async_is_called_on_a_fresh_context_then_the_related_sub_entities_are_auto_included()
    {
        var scrapeConfigurationEntity = ScrapeConfigurationEntityFactory.CreateScrapeConfigurationEntity();
        await context.ScrapeConfigurations.AddAsync(scrapeConfigurationEntity, TestContext.Current.CancellationToken);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var options = new DbContextOptionsBuilder<ControlDbContext>()
            .UseSqlite($"Data Source={databasePath}")
            .Options;

        using var untrackedContext = new ControlDbContext(options);
        var repository = untrackedContext.GetRepository<ScrapeConfigurationEntity, ScrapeConfigurationId>();

        var result = await repository.TryGetFirstAsync();

        var reloaded = result.Match(option => option, exception => throw exception).Match(entity => entity, () => null!);

        reloaded.ShouldNotBeNull();
        reloaded.UserConfiguration.ShouldNotBeNull();
        reloaded.SearchConfiguration.ShouldNotBeNull();
        reloaded.SearchConfiguration.SearchCategories.ShouldNotBeNull();
        reloaded.ScrapeDirectories.ShouldNotBeNull();
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
