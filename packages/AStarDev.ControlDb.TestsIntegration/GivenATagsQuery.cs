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

    [Fact]
    public async Task when_some_tags_are_flagged_to_ignore_images_then_only_their_wallhaven_ids_are_returned()
    {
        var ignored = TagEntityFactory.CreateTagEntity(wallhavenTagId: 10, name: "ignored");
        ignored.IgnoreImage = true;
        await context.Tags.AddRangeAsync([ignored, TagEntityFactory.CreateTagEntity(wallhavenTagId: 11, name: "kept")], TestContext.Current.CancellationToken);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);
        var query = new TagsQuery(context);

        var result = await query.GetIgnoredWallhavenIdsAsync(TestContext.Current.CancellationToken);

        result.Match(ids => ids.ToList(), exception => throw exception).ShouldBe([10]);
    }

    [Fact]
    public async Task when_tags_are_stored_then_list_returns_every_tag_ordered_by_name()
    {
        await context.Tags.AddRangeAsync([TagEntityFactory.CreateTagEntity(wallhavenTagId: 1, name: "beta"), TagEntityFactory.CreateTagEntity(wallhavenTagId: 2, name: "alpha")], TestContext.Current.CancellationToken);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);
        var query = new TagsQuery(context);

        var result = await query.ListAsync(TestContext.Current.CancellationToken);

        result.Match(tags => tags.Select(tag => tag.Name).ToList(), exception => throw exception).ShouldBe(["alpha", "beta"]);
    }

    [Fact]
    public async Task when_some_tags_are_flagged_as_names_then_only_their_wallhaven_ids_are_returned()
    {
        var name = TagEntityFactory.CreateTagEntity(wallhavenTagId: 30, name: "some name");
        name.IsName = true;
        await context.Tags.AddRangeAsync([name, TagEntityFactory.CreateTagEntity(wallhavenTagId: 31, name: "not a name")], TestContext.Current.CancellationToken);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);
        var query = new TagsQuery(context);

        var result = await query.GetNameWallhavenIdsAsync(TestContext.Current.CancellationToken);

        result.Match(ids => ids.ToList(), exception => throw exception).ShouldBe([30]);
    }

    [Fact]
    public async Task when_a_tag_is_stored_then_it_is_not_a_name_by_default()
    {
        await context.Tags.AddAsync(TagEntityFactory.CreateTagEntity(wallhavenTagId: 32, name: "default"), TestContext.Current.CancellationToken);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        context.Tags.Single().IsName.ShouldBeFalse();
    }

    [Fact]
    public async Task when_a_tag_is_stored_then_it_does_not_ignore_images_by_default()
    {
        await context.Tags.AddAsync(TagEntityFactory.CreateTagEntity(wallhavenTagId: 12, name: "default"), TestContext.Current.CancellationToken);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        context.Tags.Single().IgnoreImage.ShouldBeFalse();
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
