using AStarDev.ControlDb.FileDetail;
using AStarDev.ControlDb.TestsIntegration.TestDataFactories;
using AStarDev.FunctionalParadigm;
using Microsoft.EntityFrameworkCore;

namespace AStarDev.ControlDb.TestsIntegration;

public sealed class GivenAFilesQuery : IDisposable
{
    private readonly string databasePath = Path.Combine(Path.GetTempPath(), $"astardev-controldb-files-query-{Guid.CreateVersion7():N}.db");
    private readonly ControlDbContext context;
    private bool disposed;

    public GivenAFilesQuery()
    {
        var options = new DbContextOptionsBuilder<ControlDbContext>()
            .UseSqlite($"Data Source={databasePath}")
            .Options;

        context = new ControlDbContext(options);
        context.Database.EnsureDeleted();
        context.Database.EnsureCreated();
    }

    [Fact]
    public async Task when_a_file_with_a_matching_name_exists_then_try_get_by_name_returns_it_with_sub_entities_loaded()
    {
        var fileEntity = FileEntityFactory.CreateFileEntity();
        await context.Files.AddAsync(fileEntity, TestContext.Current.CancellationToken);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);
        var query = new FilesQuery(context);

        var result = await query.TryGetByNameAsync(fileEntity.FileName, TestContext.Current.CancellationToken);

        var found = result.Match(option => option, exception => throw exception).Match(entity => entity, () => null!);

        found.ShouldNotBeNull();
        found.FileAccessDetail.ShouldNotBeNull();
        found.ImageDetail.ShouldNotBeNull();
        found.DeletionStatus.ShouldNotBeNull();
    }

    [Fact]
    public async Task when_no_file_with_a_matching_name_exists_then_try_get_by_name_returns_none()
    {
        var query = new FilesQuery(context);

        var result = await query.TryGetByNameAsync(FileName.Create("does-not-exist"), TestContext.Current.CancellationToken);

        var found = result.Match(option => option, exception => throw exception).Match(_ => true, () => false);

        found.ShouldBeFalse();
    }

    [Fact]
    public async Task when_a_file_with_a_matching_name_exists_then_check_exists_returns_true()
    {
        var fileEntity = FileEntityFactory.CreateFileEntity();
        await context.Files.AddAsync(fileEntity, TestContext.Current.CancellationToken);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);
        var query = new FilesQuery(context);

        var result = await query.CheckExistsByNameAsync(fileEntity.FileName, TestContext.Current.CancellationToken);

        result.Match(exists => exists, exception => throw exception).ShouldBeTrue();
    }

    [Fact]
    public async Task when_no_file_with_a_matching_name_exists_then_check_exists_returns_false()
    {
        var query = new FilesQuery(context);

        var result = await query.CheckExistsByNameAsync(FileName.Create("does-not-exist"), TestContext.Current.CancellationToken);

        result.Match(exists => exists, exception => throw exception).ShouldBeFalse();
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
