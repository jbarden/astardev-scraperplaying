using AStarDev.ControlDb.TagDetail;
using AStarDev.ControlDb.TestsIntegration.TestDataFactories;
using AStarDev.FunctionalParadigm;
using Microsoft.EntityFrameworkCore;

namespace AStarDev.ControlDb.TestsIntegration;

public sealed class GivenATagsQuery : IDisposable
{
    private readonly string databasePath = Path.Combine(Path.GetTempPath(), $"astardev-controldb-tags-query-{Guid.CreateVersion7():N}.db");
    private readonly ControlDbContext context;
    private bool disposed;

    public GivenATagsQuery()
    {
        var options = new DbContextOptionsBuilder<ControlDbContext>()
            .UseSqlite($"Data Source={databasePath}")
            .Options;

        context = new ControlDbContext(options);
        context.Database.EnsureDeleted();
        context.Database.EnsureCreated();
    }

    [Fact]
    public async Task when_a_tag_with_a_matching_wallhaven_id_exists_then_it_is_returned()
    {
        var tagEntity = TagEntityFactory.CreateTagEntity(wallhavenTagId: 99);
        await context.Tags.AddAsync(tagEntity, TestContext.Current.CancellationToken);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);
        var query = new TagsQuery(context);

        var result = await query.TryFindByWallhavenIdAsync(99, TestContext.Current.CancellationToken);

        var found = result.Match(option => option, exception => throw exception).Match(entity => entity, () => null!);

        found.ShouldNotBeNull();
        found.Name.ShouldBe(tagEntity.Name);
    }

    [Fact]
    public async Task when_no_tag_with_a_matching_wallhaven_id_exists_then_none_is_returned()
    {
        var query = new TagsQuery(context);

        var result = await query.TryFindByWallhavenIdAsync(999, TestContext.Current.CancellationToken);

        var found = result.Match(option => option, exception => throw exception).Match(_ => true, () => false);

        found.ShouldBeFalse();
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
