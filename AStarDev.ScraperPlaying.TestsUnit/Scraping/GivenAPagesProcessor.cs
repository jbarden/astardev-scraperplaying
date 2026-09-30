using AStarDev.ControlDb.FileDetail;
using AStarDev.FunctionalParadigm;
using AStarDev.ScraperPlaying.Scraping;
using AStarDev.ScraperPlaying.Scraping.WallhavenResponses.SearchResponse;
using AStarDev.ScraperPlaying.TestsUnit.Fakes;
using AStarDev.ScraperPlaying.WallpaperIngestion;
using Microsoft.EntityFrameworkCore;

namespace AStarDev.ScraperPlaying.TestsUnit.Scraping;

public sealed class GivenAPagesProcessor
{
    private static readonly string[] personCategories = ["Celebrities"];
    private readonly FakeUnitOfWork unitOfWork = new();
    private readonly FakeRepository<FileEntity, FileId> fileRepository;
    private readonly FakeJsonResponseProcessor jsonResponseProcessor = new();
    private readonly FakeIngestionService wallpaperIngestionService = new();
    private readonly CapturingProgress progress = new();
    private readonly PagesProcessor processor;

    public GivenAPagesProcessor()
    {
        fileRepository = unitOfWork.Register<FileEntity, FileId>();
        processor = new(new FakeClientFactory(), new FakePageFetcher(jsonResponseProcessor), unitOfWork, new FakeSaveDirectoryResolver(), wallpaperIngestionService, new ScrapeLimits(3, 4));
    }

    [Fact]
    public async Task when_a_page_is_fetched_then_all_of_its_wallpapers_are_ingested_in_a_single_call()
    {
        SetUpPage(1, CreateSearchResponse(lastPage: 1, CreateWallpaper("wallpaper-1"), CreateWallpaper("wallpaper-2"), CreateWallpaper("wallpaper-3")));

        await Run();

        wallpaperIngestionService.PageSizes.ShouldBe([3]);
    }

    [Fact]
    public async Task when_a_page_is_fetched_then_each_wallpaper_on_it_is_ingested_into_the_resolved_directory()
    {
        var wallpaper = CreateWallpaper("wallpaper-1");
        SetUpPage(1, CreateSearchResponse(lastPage: 1, wallpaper));

        await Run();

        var ingested = wallpaperIngestionService.Calls.Single();
        ingested.Wallpaper.ShouldBe(wallpaper);
        ingested.Context.Directory.ShouldBe("resolved-directory");
        ingested.Context.FileRepository.ShouldBeSameAs(fileRepository);
        ingested.Context.CategoryLabel.ShouldBe("Top Wallpapers");
        ingested.Context.PersonCategories.ShouldBe(personCategories);
        ingested.Progress.ShouldBeSameAs(progress);
        unitOfWork.SaveCount.ShouldBe(1);
    }

    [Fact]
    public async Task when_a_specific_search_category_is_supplied_then_wallpapers_are_ingested_with_that_category_as_the_label()
    {
        var wallpaper = CreateWallpaper("wallpaper-category");
        SetUpPage(1, CreateSearchResponse(lastPage: 1, wallpaper));

        await processor.FetchAndProcessPagesAsync(
            "search category Cars",
            Option.Some("Cars"),
            page => new Uri($"https://example.test/page/{page}"),
            new WallhavenConnection("api-key", new Uri("https://example.test")),
            personCategories,
            progress,
            CancellationToken.None);

        var ingested = wallpaperIngestionService.Calls.Single();
        ingested.Wallpaper.ShouldBe(wallpaper);
        ingested.Context.CategoryLabel.ShouldBe("Cars");
    }

    [Fact]
    public async Task when_ingesting_a_wallpaper_fails_then_the_page_still_completes()
    {
        var wallpaper = CreateWallpaper("wallpaper-2");
        SetUpPage(1, CreateSearchResponse(lastPage: 1, wallpaper));
        wallpaperIngestionService.OnIngest = () => throw new InvalidOperationException("ingestion failed");

        await Should.ThrowAsync<InvalidOperationException>(Run);

        progress.Messages.ShouldContain("An error occurred during the fetching and processing of pages: ingestion failed");
    }

    [Fact]
    public async Task when_the_reported_last_page_is_reached_before_the_hard_cap_then_paging_stops_there()
    {
        SetUpPage(page: null, CreateSearchResponse(lastPage: 2));

        await Run();

        progress.Messages.ShouldContain("Fetching wallpapers page 1.");
        progress.Messages.ShouldContain("Fetching wallpapers page 2.");
        progress.Messages.ShouldNotContain(message => message.Contains("page 3"));
        unitOfWork.SaveCount.ShouldBe(2);
    }

    [Fact]
    public async Task when_the_reported_last_page_exceeds_the_hard_cap_then_paging_stops_at_page_4()
    {
        SetUpPage(page: null, CreateSearchResponse(lastPage: 10));

        await Run();

        progress.Messages.ShouldContain("Fetching wallpapers page 4.");
        progress.Messages.ShouldNotContain(message => message.Contains("page 5"));
        unitOfWork.SaveCount.ShouldBe(4);
    }

    [Fact]
    public async Task when_the_page_limit_is_lower_than_the_reported_last_page_then_paging_stops_at_the_limit()
    {
        SetUpPage(page: null, CreateSearchResponse(lastPage: 10));
        var limitedProcessor = new PagesProcessor(new FakeClientFactory(), new FakePageFetcher(jsonResponseProcessor), unitOfWork, new FakeSaveDirectoryResolver(), wallpaperIngestionService, new ScrapeLimits(3, 2));

        await limitedProcessor.FetchAndProcessPagesAsync("wallpapers", Option.None<string>(), page => new Uri($"https://example.test/page/{page}"), new WallhavenConnection("api-key", new Uri("https://example.test")), personCategories, progress, CancellationToken.None);

        (unitOfWork.SaveCount, progress.Messages.Any(message => message.Contains("page 3", StringComparison.Ordinal))).ShouldBe((2, false));
    }

    [Fact]
    public async Task when_ingesting_a_wallpaper_is_cancelled_mid_page_then_the_partial_page_is_saved_before_the_cancellation_propagates()
    {
        var wallpaper = CreateWallpaper("wallpaper-cancelled");
        SetUpPage(1, CreateSearchResponse(lastPage: 1, wallpaper));
        using var cancellationTokenSource = new CancellationTokenSource();
        wallpaperIngestionService.OnIngest = () =>
        {
            cancellationTokenSource.Cancel();

            throw new OperationCanceledException(cancellationTokenSource.Token);
        };

        await Should.ThrowAsync<OperationCanceledException>(() => FetchWithCancellation(cancellationTokenSource.Token));

        unitOfWork.SaveTokens.ShouldBe([CancellationToken.None]);
        progress.Messages.ShouldContain("Scrape cancelled - saved wallpapers downloaded so far this page.");
    }

    [Fact]
    public async Task when_the_partial_page_save_on_cancellation_itself_fails_then_the_cancellation_still_propagates()
    {
        var wallpaper = CreateWallpaper("wallpaper-cancelled-save-fails");
        SetUpPage(1, CreateSearchResponse(lastPage: 1, wallpaper));
        using var cancellationTokenSource = new CancellationTokenSource();
        wallpaperIngestionService.OnIngest = () =>
        {
            cancellationTokenSource.Cancel();

            throw new OperationCanceledException(cancellationTokenSource.Token);
        };
        unitOfWork.OnSave = token =>
        {
            if (token == CancellationToken.None) throw new DbUpdateException("save failed", new InvalidOperationException("UNIQUE constraint failed: FileDetail.FileHandle"));
        };

        await Should.ThrowAsync<OperationCanceledException>(() => FetchWithCancellation(cancellationTokenSource.Token));

        progress.Messages.ShouldContain("Scrape cancelled - failed to save wallpapers downloaded so far this page: save failed Caused by: UNIQUE constraint failed: FileDetail.FileHandle");
    }

    [Fact]
    public async Task when_saving_a_page_fails_with_a_database_update_error_then_the_inner_cause_is_reported_and_the_failure_rethrown()
    {
        SetUpPage(1, CreateSearchResponse(lastPage: 1, CreateWallpaper("wallpaper-duplicate")));
        unitOfWork.OnSave = _ => throw new DbUpdateException("An error occurred while saving the entity changes. See the inner exception for details.", new InvalidOperationException("UNIQUE constraint failed: FileDetail.FileHandle"));

        _ = await Should.ThrowAsync<DbUpdateException>(Run);

        progress.Messages.ShouldContain("An error occurred during the fetching and processing of pages: An error occurred while saving the entity changes. See the inner exception for details. Caused by: UNIQUE constraint failed: FileDetail.FileHandle");
    }

    [Fact]
    public async Task when_fetching_a_page_fails_then_the_failure_is_reported_and_rethrown()
    {
        var exception = new InvalidOperationException("page fetch failed");
        jsonResponseProcessor.Response = exception;

        var thrown = await Should.ThrowAsync<InvalidOperationException>(Run);

        thrown.ShouldBeSameAs(exception);
        progress.Messages.ShouldContain("An error occurred during the fetching and processing of pages: page fetch failed");
    }

    private Task FetchWithCancellation(CancellationToken cancellationToken)
        => processor.FetchAndProcessPagesAsync(
            "wallpapers",
            Option.None<string>(),
            page => new Uri($"https://example.test/page/{page}"),
            new WallhavenConnection("api-key", new Uri("https://example.test")),
            personCategories,
            progress,
            cancellationToken);

    private Task Run()
        => FetchWithCancellation(CancellationToken.None);

    private void SetUpPage(int? page, SearchResponse response)
    {
        if (page is { } specificPage)
        {
            jsonResponseProcessor.PageResponses[new Uri($"https://example.test/page/{specificPage}")] = response;

            return;
        }

        jsonResponseProcessor.Response = (Option<SearchResponse>)response;
    }

    private static SearchResponse CreateSearchResponse(int lastPage, params Data[] wallpapers)
        => new(wallpapers, new Meta(lastPage));

    private static Data CreateWallpaper(string id, string path = "")
        => new(id, 0, 0, 0, "", path);

    private sealed class FakeClientFactory : IWallhavenClientFactory
    {
        public HttpClient Create(WallhavenConnection connection) => new();
    }

    private sealed class FakePageFetcher(FakeJsonResponseProcessor jsonResponseProcessor) : IWallhavenPageFetcher
    {
        public Task<SearchResponse> FetchPageAsync(string logLabel, Uri pageUrl, int page, HttpClient client, IProgress<string> progress, CancellationToken cancellationToken)
        {
            progress.Report($"Fetching {logLabel} page {page}.");

            return Task.FromResult(jsonResponseProcessor.Fetch(pageUrl));
        }
    }

    private sealed class FakeSaveDirectoryResolver : ISaveDirectoryResolver
    {
        public Task<string> ResolveSaveDirectoryAsync(Option<string> categoryName, CancellationToken cancellationToken) => Task.FromResult("resolved-directory");
    }

    private sealed class FakeJsonResponseProcessor : IJsonResponseProcessor
    {
        public Dictionary<Uri, SearchResponse> PageResponses { get; } = [];

        public Exceptional<Option<SearchResponse>> Response { get; set; } = Option<SearchResponse>.None.Instance;

        public Task<Exceptional<Option<T>>> GetFromJsonAsync<T>(Uri url, HttpClient client, CancellationToken cancellationToken)
        {
            Exceptional<Option<SearchResponse>> result = PageResponses.TryGetValue(url, out var page) ? (Option<SearchResponse>)page : Response;

            return Task.FromResult((Exceptional<Option<T>>)(object)result);
        }

        public SearchResponse Fetch(Uri url)
        {
            Exceptional<Option<SearchResponse>> result = PageResponses.TryGetValue(url, out var page) ? (Option<SearchResponse>)page : Response;

            return result.Match(
                option => option.Match(value => value, () => throw new InvalidOperationException($"No response body received for {url}.")),
                exception => throw exception);
        }
    }

    private sealed record IngestCall(Data Wallpaper, WallpaperIngestionContext Context, IProgress<string> Progress);

    private sealed class FakeIngestionService : IWallpaperIngestionService
    {
        public List<IngestCall> Calls { get; } = [];

        public Action OnIngest { get; set; } = () => { };

        public List<int> PageSizes { get; } = [];

        public Task IngestPageAsync(IReadOnlyList<Data> wallpapers, WallpaperIngestionContext context, IProgress<string> progress, CancellationToken cancellationToken)
        {
            PageSizes.Add(wallpapers.Count);
            Calls.AddRange(wallpapers.Select(wallpaper => new IngestCall(wallpaper, context, progress)));
            OnIngest();

            return Task.CompletedTask;
        }
    }

    private sealed class CapturingProgress : IProgress<string>
    {
        public List<string> Messages { get; } = [];

        public void Report(string value) => Messages.Add(value);
    }
}
