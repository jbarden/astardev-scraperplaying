using AStarDev.ControlDb;
using AStarDev.ControlDb.FileDetail;
using AStarDev.FunctionalParadigm;
using AStarDev.ScraperPlaying.Scraping;
using AStarDev.ScraperPlaying.WallpaperIngestion;
using Microsoft.EntityFrameworkCore;
using Microsoft.Playwright;

namespace AStarDev.ScraperPlaying.TestsUnit.Scraping;

public sealed class GivenAWebsitePagesProcessor
{
    private static readonly WallhavenConnection Connection = new("api-key", new Uri("https://example.test"));

    private readonly IPlaywrightBrowserSession browserSession = Substitute.For<IPlaywrightBrowserSession>();
    private readonly IPage page = Substitute.For<IPage>();
    private readonly IListingPageScraper listingPageScraper = Substitute.For<IListingPageScraper>();
    private readonly IFilesQuery filesQuery = Substitute.For<IFilesQuery>();
    private readonly IWallpaperIngestor wallpaperIngestor = Substitute.For<IWallpaperIngestor>();
    private readonly IUnitOfWork unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly IRepository<FileEntity, FileId> fileRepository = Substitute.For<IRepository<FileEntity, FileId>>();
    private readonly Dictionary<int, string[]> wallpapersByPage = [];
    private readonly List<string> ingestedWallpaperIds = [];
    private readonly List<ListingPageRequest> requestedPages = [];
    private readonly CapturingProgress progress = new();
    private readonly WebsitePagesProcessor processor;

    public GivenAWebsitePagesProcessor()
    {
        browserSession.GetPageAsync(Arg.Any<bool>(), Arg.Any<CancellationToken>()).Returns(page);
        listingPageScraper.ScrapeWallpaperIdsAsync(Arg.Any<ListingPageRequest>(), page, Arg.Any<IProgress<string>>(), Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                callInfo.Arg<CancellationToken>().ThrowIfCancellationRequested();
                var request = callInfo.Arg<ListingPageRequest>();
                requestedPages.Add(request);

                return Task.FromResult(wallpapersByPage.GetValueOrDefault(request.PageNumber, []));
            });
        filesQuery.GetExistingHandlesAsync(Arg.Any<IReadOnlyCollection<FileHandle>>(), Arg.Any<CancellationToken>()).Returns(NoExistingHandles());
        unitOfWork.GetRepository<FileEntity, FileId>().Returns(fileRepository);
        wallpaperIngestor.IngestAsync(Arg.Any<string>(), Arg.Any<PageIngestionContext>(), Arg.Any<IProgress<string>>(), Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                ingestedWallpaperIds.Add(callInfo.Arg<string>());

                return Task.CompletedTask;
            });
        processor = new(browserSession, listingPageScraper, new NewWallpaperFilter(filesQuery), wallpaperIngestor, new IngestionStore(unitOfWork));
    }

    [Fact]
    public async Task when_a_page_has_wallpapers_then_the_next_page_is_fetched()
    {
        wallpapersByPage[1] = ["wallpaper-1", "wallpaper-2"];

        await Run();

        requestedPages.Select(request => request.PageNumber).ShouldBe([1, 2]);
    }

    [Fact]
    public async Task when_a_listing_page_is_requested_then_its_url_is_built_from_the_base_url_and_page_number()
    {
        await Run();

        requestedPages[0].LogLabel.ShouldBe("wallpapers");
        requestedPages[0].Url.ShouldBe(new Uri("https://example.test/page/1"));
    }

    [Fact]
    public async Task when_the_hard_cap_is_reached_then_paging_stops_at_page_4()
    {
        for (var pageNumber = 1; pageNumber <= 5; pageNumber++) wallpapersByPage[pageNumber] = [$"wallpaper-{pageNumber}"];

        await Run();

        requestedPages.Select(request => request.PageNumber).ShouldBe([1, 2, 3, 4]);
    }

    [Fact]
    public async Task when_navigation_fails_then_the_failure_is_reported_and_rethrown()
    {
        var exception = new PlaywrightException("navigation failed");
        listingPageScraper.ScrapeWallpaperIdsAsync(Arg.Any<ListingPageRequest>(), page, Arg.Any<IProgress<string>>(), Arg.Any<CancellationToken>()).Returns<string[]>(_ => throw exception);

        var thrown = await Should.ThrowAsync<PlaywrightException>(Run);

        thrown.ShouldBeSameAs(exception);
        progress.Messages.ShouldContain("An error occurred navigating the website during wallpapers: navigation failed");
    }

    [Fact]
    public async Task when_cancelled_then_cancellation_is_reported_and_rethrown()
    {
        using var cancellationTokenSource = new CancellationTokenSource();
        await cancellationTokenSource.CancelAsync();

        await Should.ThrowAsync<OperationCanceledException>(() => RunWith(cancellationTokenSource.Token));

        progress.Messages.ShouldContain("Scrape cancelled.");
    }

    [Fact]
    public async Task when_a_page_has_wallpapers_then_each_is_ingested_with_the_search_context()
    {
        wallpapersByPage[1] = ["wallpaper-1", "wallpaper-2"];

        await Run();

        ingestedWallpaperIds.ShouldBe(["wallpaper-1", "wallpaper-2"]);
        await wallpaperIngestor.Received(2).IngestAsync(Arg.Any<string>(), Arg.Is<PageIngestionContext>(context => context.Page == page && context.BaseUrl == Connection.BaseUrl && context.CategoryLabel == "Top Wallpapers" && context.FileRepository == fileRepository), progress, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task when_a_category_is_being_searched_then_its_name_and_label_are_in_the_context()
    {
        wallpapersByPage[1] = ["wallpaper-1"];

        await processor.FetchAndProcessPagesAsync(new SearchRequest("nature", Option.Some("Nature"), pageNumber => $"page/{pageNumber}"), Connection, progress, CancellationToken.None);

        await wallpaperIngestor.Received(1).IngestAsync("wallpaper-1", Arg.Is<PageIngestionContext>(context => context.CategoryLabel == "Nature" && context.CategoryName.Match(name => name == "Nature", () => false)), progress, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task when_a_page_is_scraped_then_the_processor_does_not_repeat_the_wallpaper_count_the_scraper_reports()
    {
        wallpapersByPage[1] = ["wallpaper-1", "wallpaper-2"];

        await Run();

        progress.Messages.ShouldNotContain(message => message.StartsWith("Found "));
    }

    [Fact]
    public async Task when_a_wallpaper_was_already_downloaded_then_it_is_not_ingested_and_the_rest_of_the_page_still_is()
    {
        wallpapersByPage[1] = ["wallpaper-1", "wallpaper-2"];
        filesQuery.GetExistingHandlesAsync(Arg.Any<IReadOnlyCollection<FileHandle>>(), Arg.Any<CancellationToken>()).Returns(ExistingHandles("wallpaper-1"));

        await Run();

        progress.Messages.ShouldContain("Wallpaper wallpaper-1 was already downloaded - skipping.");
        ingestedWallpaperIds.ShouldBe(["wallpaper-2"]);
    }

    [Fact]
    public async Task when_a_page_has_several_wallpapers_then_their_existence_is_checked_in_a_single_query()
    {
        wallpapersByPage[1] = ["wallpaper-1", "wallpaper-2", "wallpaper-3"];

        await Run();

        await filesQuery.Received(1).GetExistingHandlesAsync(Arg.Is<IReadOnlyCollection<FileHandle>>(handles => handles.Count == 3), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task when_checking_which_wallpapers_were_already_downloaded_fails_then_it_is_reported_and_the_page_is_skipped()
    {
        wallpapersByPage[1] = ["wallpaper-1"];
        filesQuery.GetExistingHandlesAsync(Arg.Any<IReadOnlyCollection<FileHandle>>(), Arg.Any<CancellationToken>()).Returns((Exceptional<IReadOnlySet<FileHandle>>)new InvalidOperationException("db down"));

        await Run();

        progress.Messages.ShouldContain("Failed to check which wallpapers were already downloaded, skipping this page: db down");
        ingestedWallpaperIds.ShouldBeEmpty();
    }

    [Fact]
    public async Task when_a_page_is_processed_then_its_changes_are_saved()
    {
        wallpapersByPage[1] = ["wallpaper-1"];

        await Run();

        await unitOfWork.Received(2).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task when_cancelled_part_way_through_a_page_then_the_wallpapers_downloaded_so_far_are_saved_and_cancellation_is_rethrown()
    {
        wallpapersByPage[1] = ["wallpaper-1", "wallpaper-2"];
        using var cancellationTokenSource = new CancellationTokenSource();
        wallpaperIngestor.IngestAsync("wallpaper-2", Arg.Any<PageIngestionContext>(), Arg.Any<IProgress<string>>(), Arg.Any<CancellationToken>())
            .Returns(_ =>
            {
                cancellationTokenSource.Cancel();

                return Task.FromCanceled(cancellationTokenSource.Token);
            });

        await Should.ThrowAsync<OperationCanceledException>(() => RunWith(cancellationTokenSource.Token));

        await unitOfWork.Received(1).SaveChangesAsync(CancellationToken.None);
        progress.Messages.ShouldContain("Scrape cancelled.");
    }

    [Fact]
    public async Task when_saving_after_cancellation_fails_then_the_failure_is_reported_and_cancellation_is_still_rethrown()
    {
        using var cancellationTokenSource = new CancellationTokenSource();
        await cancellationTokenSource.CancelAsync();
        unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns<Task<int>>(_ => throw new DbUpdateException("save failed"));

        await Should.ThrowAsync<OperationCanceledException>(() => RunWith(cancellationTokenSource.Token));

        progress.Messages.ShouldContain("Scrape cancelled - failed to save wallpapers downloaded so far this page: save failed");
    }

    private static Exceptional<IReadOnlySet<FileHandle>> ExistingHandles(params string[] wallpaperIds)
    {
        IReadOnlySet<FileHandle> handles = wallpaperIds.Select(FileHandle.Create).ToHashSet();

        return Exceptional.Success(handles);
    }

    private static Exceptional<IReadOnlySet<FileHandle>> NoExistingHandles() => ExistingHandles();

    private Task Run() => RunWith(TestContext.Current.CancellationToken);

    private Task RunWith(CancellationToken cancellationToken)
        => processor.FetchAndProcessPagesAsync(new SearchRequest("wallpapers", Option.None<string>(), pageNumber => $"page/{pageNumber}"), Connection, progress, cancellationToken);

    private sealed class CapturingProgress : IProgress<string>
    {
        public List<string> Messages { get; } = [];

        public void Report(string value) => Messages.Add(value);
    }
}
