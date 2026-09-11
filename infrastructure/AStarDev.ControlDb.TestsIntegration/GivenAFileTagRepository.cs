using AStarDev.ControlDb.TagDetail;
using AStarDev.ControlDb.TestsIntegration.TestDataFactories;
using AStarDev.FunctionalParadigm;
using Microsoft.EntityFrameworkCore;

namespace AStarDev.ControlDb.TestsIntegration;

public sealed class GivenAFileTagRepository : IDisposable
{
    private readonly string databasePath = Path.Combine(Path.GetTempPath(), $"astardev-controldb-file-tag-repository-{Guid.CreateVersion7():N}.db");
    private readonly ControlDbContext context;
    private bool disposed;

    public GivenAFileTagRepository()
    {
        var options = new DbContextOptionsBuilder<ControlDbContext>()
            .UseSqlite($"Data Source={databasePath}")
            .Options;

        context = new ControlDbContext(options);
        context.Database.EnsureDeleted();
        context.Database.EnsureCreated();
    }

    [Fact]
    public async Task when_a_file_tag_is_added_then_it_links_the_file_and_the_tag()
    {
        var fileEntity = FileEntityFactory.CreateFileEntity();
        var tagEntity = TagEntityFactory.CreateTagEntity();
        await context.Files.AddAsync(fileEntity, TestContext.Current.CancellationToken);
        await context.Tags.AddAsync(tagEntity, TestContext.Current.CancellationToken);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);
        var repository = new FileTagRepository(context);

        repository.Add(new FileTagEntity { FileId = fileEntity.Id, TagId = tagEntity.Id }).Match(fileTag => fileTag, exception => throw exception);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var linked = await context.FileTags.SingleAsync(TestContext.Current.CancellationToken);
        linked.FileId.ShouldBe(fileEntity.Id);
        linked.TagId.ShouldBe(tagEntity.Id);
    }

    [Fact]
    public async Task when_the_linked_file_is_deleted_then_the_file_tag_is_also_deleted()
    {
        var fileEntity = FileEntityFactory.CreateFileEntity();
        var tagEntity = TagEntityFactory.CreateTagEntity();
        await context.Files.AddAsync(fileEntity, TestContext.Current.CancellationToken);
        await context.Tags.AddAsync(tagEntity, TestContext.Current.CancellationToken);
        new FileTagRepository(context).Add(new FileTagEntity { FileId = fileEntity.Id, TagId = tagEntity.Id }).Match(fileTag => fileTag, exception => throw exception);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        context.Files.Remove(fileEntity);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        (await context.FileTags.AnyAsync(TestContext.Current.CancellationToken)).ShouldBeFalse();
    }

    [Fact]
    public async Task when_the_linked_tag_is_deleted_while_still_referenced_then_saving_fails()
    {
        var fileEntity = FileEntityFactory.CreateFileEntity();
        var tagEntity = TagEntityFactory.CreateTagEntity();
        await context.Files.AddAsync(fileEntity, TestContext.Current.CancellationToken);
        await context.Tags.AddAsync(tagEntity, TestContext.Current.CancellationToken);
        new FileTagRepository(context).Add(new FileTagEntity { FileId = fileEntity.Id, TagId = tagEntity.Id }).Match(fileTag => fileTag, exception => throw exception);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        Should.Throw<InvalidOperationException>(() => context.Tags.Remove(tagEntity));
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
