using AStarDev.ControlDb.FileDetail;
using AStarDev.ControlDb.TestsIntegration.TestDataFactories;
using AStarDev.FunctionalParadigm;
using Microsoft.EntityFrameworkCore;

namespace AStarDev.ControlDb.TestsIntegration;

public sealed class GivenAFileRepository : IDisposable
{
    private readonly string databasePath = Path.Combine(Path.GetTempPath(), $"astardev-controldb-file-repository-{Guid.CreateVersion7():N}.db");
    private readonly ControlDbContext context;
    private bool disposed;

    public GivenAFileRepository()
    {
        var options = new DbContextOptionsBuilder<ControlDbContext>()
            .UseSqlite($"Data Source={databasePath}")
            .Options;

        context = new ControlDbContext(options);
        context.Database.EnsureDeleted();
        context.Database.EnsureCreated();
    }

    [Fact]
    public async Task when_a_file_is_added_and_found_by_key_then_its_sub_entities_are_eagerly_loaded()
    {
        var fileEntity = FileEntityFactory.CreateFileEntity();
        var repository = context.GetRepository<FileEntity, FileId>();
        repository.Add(fileEntity).Match(entity => entity, exception => throw exception);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        using var untrackedContext = new ControlDbContext(new DbContextOptionsBuilder<ControlDbContext>().UseSqlite($"Data Source={databasePath}").Options);
        var result = await untrackedContext.GetRepository<FileEntity, FileId>().TryFindAsync(fileEntity.Id);

        var reloaded = result.Match(option => option, exception => throw exception).Match(entity => entity, () => null!);

        reloaded.ShouldNotBeNull();
        reloaded.FileAccessDetail.ShouldNotBeNull();
        reloaded.ImageDetail.ShouldNotBeNull();
        reloaded.DeletionStatus.ShouldNotBeNull();
    }

    [Fact]
    public async Task when_finding_by_a_key_that_does_not_exist_then_none_is_returned()
    {
        var repository = context.GetRepository<FileEntity, FileId>();

        var result = await repository.TryFindAsync(FileId.Create());

        var found = result.Match(option => option, exception => throw exception).Match(_ => true, () => false);

        found.ShouldBeFalse();
    }

    [Fact]
    public async Task when_multiple_files_are_added_then_get_all_returns_every_file()
    {
        var repository = context.GetRepository<FileEntity, FileId>();
        var first = FileEntityFactory.CreateFileEntity();
        var secondId = FileId.Create();
        var second = new FileEntity
        {
            Id = secondId,
            FileName = FileName.Create("second-file"),
            DirectoryName = DirectoryName.Create("directory-name"),
            FileHandle = FileHandle.Create("second-file-handle"),
            FileSize = 54321,
            FileAccessDetail = new FileAccessDetailEntity { Id = FileAccessDetailId.Create(), FileId = secondId }
        };
        repository.Add(first).Match(entity => entity, exception => throw exception);
        repository.Add(second).Match(entity => entity, exception => throw exception);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var result = await repository.TryGetAllAsync();

        var all = result.Match(option => option, exception => throw exception).Match(entities => entities.ToList(), () => []);

        all.Count.ShouldBe(2);
    }

    [Fact]
    public async Task when_a_file_is_deleted_then_it_can_no_longer_be_found()
    {
        var repository = context.GetRepository<FileEntity, FileId>();
        var fileEntity = FileEntityFactory.CreateFileEntity();
        repository.Add(fileEntity).Match(entity => entity, exception => throw exception);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        (await repository.DeleteAsync(fileEntity.Id)).Match(unit => unit, exception => throw exception);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var result = await repository.TryFindAsync(fileEntity.Id);
        var found = result.Match(option => option, exception => throw exception).Match(_ => true, () => false);

        found.ShouldBeFalse();
    }

    [Fact]
    public async Task when_deleting_by_a_key_that_does_not_exist_then_no_exception_is_thrown()
    {
        var repository = context.GetRepository<FileEntity, FileId>();

        var result = await repository.DeleteAsync(FileId.Create());

        result.Match(_ => true, exception => throw exception).ShouldBeTrue();
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
