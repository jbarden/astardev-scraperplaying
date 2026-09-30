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
    public async Task when_some_of_the_wallhaven_ids_exist_then_find_by_wallhaven_ids_returns_only_the_stored_tags()
    {
        await context.Tags.AddRangeAsync([TagEntityFactory.CreateTagEntity(wallhavenTagId: 1, name: "one"), TagEntityFactory.CreateTagEntity(wallhavenTagId: 2, name: "two"), TagEntityFactory.CreateTagEntity(wallhavenTagId: 3, name: "three")], TestContext.Current.CancellationToken);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);
        var query = new TagsQuery(context);

        var result = await query.FindByWallhavenIdsAsync([1, 3, 999], TestContext.Current.CancellationToken);

        result.Match(tags => tags.Select(tag => tag.Name).Order().ToList(), exception => throw exception).ShouldBe(["one", "three"]);
    }

    [Fact]
    public async Task when_no_wallhaven_ids_are_supplied_then_find_by_wallhaven_ids_returns_an_empty_list()
    {
        var query = new TagsQuery(context);

        var result = await query.FindByWallhavenIdsAsync([], TestContext.Current.CancellationToken);

        result.Match(tags => tags.Count, exception => throw exception).ShouldBe(0);
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
