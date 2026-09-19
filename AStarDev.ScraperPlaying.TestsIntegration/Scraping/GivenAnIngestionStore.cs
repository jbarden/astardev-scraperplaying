using AStarDev.ControlDb;
using AStarDev.ControlDb.TagDetail;
using AStarDev.ScraperPlaying.Scraping;
using AStarDev.ScraperPlaying.Startup;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Xunit;

namespace AStarDev.ScraperPlaying.TestsIntegration.Scraping;

/// <summary>Exercises the real ingestion store, tags processor and database context across several pages of a scrape.</summary>
public sealed class GivenAnIngestionStore : IDisposable
{
    private readonly string databasePath = Path.Combine(Path.GetTempPath(), $"astardev-scraperplaying-ingestionstore-{Guid.CreateVersion7():N}.db");
    private readonly ServiceProvider serviceProvider;
    private readonly IServiceScope scope;
    private readonly ControlDbContext context;
    private readonly IngestionStore store;
    private readonly List<string> messages = [];
    private bool disposed;

    public GivenAnIngestionStore()
    {
        serviceProvider = new ServiceCollection().AddDataServices(databasePath).BuildServiceProvider();
        scope = serviceProvider.CreateScope();
        context = scope.ServiceProvider.GetRequiredService<ControlDbContext>();
        context.Database.EnsureCreated();
        store = new(context, new TagsProcessor(new TagsQuery(context), context, new FileTagRepository(context)));
    }

    [Fact]
    public async Task when_a_page_save_fails_then_the_next_page_is_still_saved_and_the_failed_rows_are_not_retried()
    {
        context.Tags.Add(NewTag(1, "first"));
        context.Tags.Add(NewTag(1, "duplicate-of-first"));
        await store.SavePageAsync(new Progress(messages), TestContext.Current.CancellationToken);
        context.Tags.Add(NewTag(2, "second"));

        await store.SavePageAsync(new Progress(messages), TestContext.Current.CancellationToken);

        messages.ShouldHaveSingleItem().ShouldStartWith("Failed to save the wallpapers ingested from this page");
        var stored = await context.Tags.AsNoTracking().Select(tag => tag.WallhavenTagId).ToListAsync(TestContext.Current.CancellationToken);
        stored.ShouldBe([2]);
    }

    [Fact]
    public async Task when_several_pages_are_saved_then_nothing_stays_tracked_by_the_context()
    {
        for (var page = 1; page <= 3; page++)
        {
            context.Tags.Add(NewTag(page, $"tag-{page}"));
            await store.SavePageAsync(new Progress(messages), TestContext.Current.CancellationToken);

            context.ChangeTracker.Entries().ShouldBeEmpty();
        }

        messages.ShouldBeEmpty();
        (await context.Tags.CountAsync(TestContext.Current.CancellationToken)).ShouldBe(3);
    }

    public void Dispose()
    {
        if (disposed) return;

        scope.Dispose();
        serviceProvider.Dispose();
        if (File.Exists(databasePath)) File.Delete(databasePath);

        disposed = true;
    }

    private static TagEntity NewTag(int wallhavenTagId, string name) => new() { Id = TagId.Empty, WallhavenTagId = wallhavenTagId, Name = name };

    private sealed class Progress(List<string> messages) : IProgress<string>
    {
        public void Report(string value) => messages.Add(value);
    }
}
