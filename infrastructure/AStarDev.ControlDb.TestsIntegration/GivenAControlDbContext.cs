using AStarDev.ControlDb.FileDetail;
using AStarDev.ControlDb.ScrapeConfiguration;
using AStarDev.ControlDb.TagDetail;
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
            UserConfiguration = new UserConfigurationEntity(new UserConfigurationId(Guid.CreateVersion7()), explicitId, "user@example.com", "username", "password", "apiKey"),
            SearchConfiguration = new SearchConfigurationEntity(new SearchConfigurationId(Guid.CreateVersion7()), explicitId, "search-config", 10, []),
            ScrapeDirectories = new ScrapeDirectoriesEntity(new ScrapeDirectoriesId(Guid.CreateVersion7()), explicitId, "root-save-directory", "root-directory-famous", "sub-directory-name")
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

    [Fact]
    public async Task when_two_new_file_entities_are_added_with_empty_ids_in_the_same_batch_then_both_are_tracked_and_generated_distinct_ids()
    {
        var firstFile = CreateNewFileEntityWithEmptyIds("first-file-handle");
        var secondFile = CreateNewFileEntityWithEmptyIds("second-file-handle");

        await context.Files.AddAsync(firstFile, TestContext.Current.CancellationToken);
        await context.Files.AddAsync(secondFile, TestContext.Current.CancellationToken);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        firstFile.Id.Value.ShouldNotBe(Guid.Empty);
        secondFile.Id.Value.ShouldNotBe(Guid.Empty);
        firstFile.Id.ShouldNotBe(secondFile.Id);

        firstFile.FileAccessDetail.Id.Value.ShouldNotBe(Guid.Empty);
        secondFile.FileAccessDetail.Id.Value.ShouldNotBe(Guid.Empty);
        firstFile.FileAccessDetail.Id.ShouldNotBe(secondFile.FileAccessDetail.Id);

        firstFile.ImageDetail.ShouldNotBeNull();
        secondFile.ImageDetail.ShouldNotBeNull();
        firstFile.ImageDetail.Id.Value.ShouldNotBe(Guid.Empty);
        secondFile.ImageDetail.Id.Value.ShouldNotBe(Guid.Empty);
        firstFile.ImageDetail.Id.ShouldNotBe(secondFile.ImageDetail.Id);
    }

    /// <summary>Mirrors exactly how ImageProcessor.ProcessTheImageAsync builds a new FileEntity - every id set to its Empty sentinel, relying on EF to generate a real one on insert.</summary>
    private static FileEntity CreateNewFileEntityWithEmptyIds(string fileHandle)
        => new()
        {
            Id = FileId.Empty,
            FileName = FileName.Create(fileHandle),
            DirectoryName = DirectoryName.Create("directory-name"),
            FileHandle = FileHandle.Create(fileHandle),
            FileSize = 12345,
            FileAccessDetail = new FileAccessDetailEntity { Id = FileAccessDetailId.Empty, FileId = FileId.Empty },
            ImageDetail = new ImageDetailEntity { Id = ImageId.Empty, FileId = FileId.Empty, Width = 1920, Height = 1080 }
        };

    [Fact]
    public void when_accessed_the_tags_and_file_tags_repositories_should_be_dbsets()
    {
        context.Tags.ShouldBeAssignableTo<DbSet<TagEntity>>();
        context.FileTags.ShouldBeAssignableTo<DbSet<FileTagEntity>>();
    }

    [Fact]
    public async Task when_one_tag_is_linked_to_two_files_then_it_is_stored_once_with_two_links()
    {
        var firstFile = FileEntityFactory.CreateFileEntity();
        var secondFileId = FileId.Create();
        var secondFile = new FileEntity
        {
            Id = secondFileId,
            FileName = FileName.Create("second-file"),
            DirectoryName = DirectoryName.Create("directory-name"),
            FileHandle = FileHandle.Create("second-file-handle"),
            FileSize = 54321,
            FileAccessDetail = new FileAccessDetailEntity { Id = FileAccessDetailId.Create(), FileId = secondFileId }
        };
        var tagEntity = TagEntityFactory.CreateTagEntity();
        await context.Files.AddRangeAsync([firstFile, secondFile], TestContext.Current.CancellationToken);
        await context.Tags.AddAsync(tagEntity, TestContext.Current.CancellationToken);
        await context.FileTags.AddRangeAsync(
        [
            new FileTagEntity { FileId = firstFile.Id, TagId = tagEntity.Id },
            new FileTagEntity { FileId = secondFile.Id, TagId = tagEntity.Id }
        ], TestContext.Current.CancellationToken);

        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        (await context.Tags.CountAsync(TestContext.Current.CancellationToken)).ShouldBe(1);
        (await context.FileTags.CountAsync(fileTag => fileTag.TagId == tagEntity.Id, TestContext.Current.CancellationToken)).ShouldBe(2);
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
