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
    public async Task when_some_of_the_requested_tags_exist_then_only_those_are_returned()
    {
        var firstTag = TagEntityFactory.CreateTagEntity(wallhavenTagId: 99);
        var secondTag = TagEntityFactory.CreateTagEntity(wallhavenTagId: 100);
        await context.Tags.AddRangeAsync([firstTag, secondTag], TestContext.Current.CancellationToken);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);
        var query = new TagsQuery(context);

        var result = await query.FindByWallhavenIdsAsync([99, 999], TestContext.Current.CancellationToken);

        var found = result.Match(tags => tags, exception => throw exception);
        found.Count.ShouldBe(1);
        found[0].Name.ShouldBe(firstTag.Name);
    }

    [Fact]
    public async Task when_none_of_the_requested_tags_exist_then_an_empty_list_is_returned()
    {
        var query = new TagsQuery(context);

        var result = await query.FindByWallhavenIdsAsync([999], TestContext.Current.CancellationToken);

        result.Match(tags => tags, exception => throw exception).ShouldBeEmpty();
    }

    [Fact]
    public async Task when_no_tags_are_requested_then_an_empty_list_is_returned()
    {
        var query = new TagsQuery(context);

        var result = await query.FindByWallhavenIdsAsync([], TestContext.Current.CancellationToken);

        result.Match(tags => tags, exception => throw exception).ShouldBeEmpty();
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
