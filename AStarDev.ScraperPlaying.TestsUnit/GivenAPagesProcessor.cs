using AStarDev.ControlDb;
using AStarDev.ControlDb.FileDetail;
using AStarDev.FunctionalParadigm;
using AStarDev.ScraperPlaying.Home;
using AStarDev.ScraperPlaying.SearchAPI.SearchResponse;

namespace AStarDev.ScraperPlaying.TestsUnit;

public sealed class GivenAPagesProcessor
{
    private readonly IHttpClientFactory httpClientFactory = Substitute.For<IHttpClientFactory>();
    private readonly IUnitOfWork unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly IRepository<FileEntity, FileId> fileRepository = Substitute.For<IRepository<FileEntity, FileId>>();
    private readonly IJsonResponseProcessor jsonResponseProcessor = Substitute.For<IJsonResponseProcessor>();
    private readonly ISaveDirectoryResolver saveDirectoryResolver = Substitute.For<ISaveDirectoryResolver>();
    private readonly IWallpaperIngestionService wallpaperIngestionService = Substitute.For<IWallpaperIngestionService>();
    private readonly CapturingProgress progress = new();
    private readonly PagesProcessor processor;

    public GivenAPagesProcessor()
    {
        httpClientFactory.CreateClient(ApplicationConstants.WallhavenHttpClientName).Returns(_ => new HttpClient());
        unitOfWork.GetRepository<FileEntity, FileId>().Returns(fileRepository);
        unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(1);
        saveDirectoryResolver.ResolveSaveDirectoryAsync(Arg.Any<Option<string>>(), Arg.Any<CancellationToken>()).Returns("resolved-directory");
        wallpaperIngestionService.IngestAsync(Arg.Any<Data>(), Arg.Any<string>(), Arg.Any<HttpClient>(), Arg.Any<IRepository<FileEntity, FileId>>(), Arg.Any<IProgress<string>>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);
        processor = new(httpClientFactory, unitOfWork, jsonResponseProcessor, saveDirectoryResolver, wallpaperIngestionService, () => TimeSpan.FromMilliseconds(1));
    }

    [Fact]
    public async Task when_a_page_is_fetched_then_each_wallpaper_on_it_is_ingested_into_the_resolved_directory()
    {
        var wallpaper = CreateWallpaper("wallpaper-1");
        SetUpPage(1, CreateSearchResponse(lastPage: 1, wallpaper));

        await Run();

        await wallpaperIngestionService.Received(1).IngestAsync(wallpaper, "resolved-directory", Arg.Any<HttpClient>(), fileRepository, progress, Arg.Any<CancellationToken>());
        await unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task when_ingesting_a_wallpaper_fails_then_the_page_still_completes()
    {
        var wallpaper = CreateWallpaper("wallpaper-2");
        SetUpPage(1, CreateSearchResponse(lastPage: 1, wallpaper));
        wallpaperIngestionService.IngestAsync(Arg.Any<Data>(), Arg.Any<string>(), Arg.Any<HttpClient>(), Arg.Any<IRepository<FileEntity, FileId>>(), Arg.Any<IProgress<string>>(), Arg.Any<CancellationToken>())
            .Returns(_ => throw new InvalidOperationException("ingestion failed"));

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
        await unitOfWork.Received(2).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task when_the_reported_last_page_exceeds_the_hard_cap_then_paging_stops_at_page_4()
    {
        SetUpPage(page: null, CreateSearchResponse(lastPage: 10));

        await Run();

        progress.Messages.ShouldContain("Fetching wallpapers page 4.");
        progress.Messages.ShouldNotContain(message => message.Contains("page 5"));
        await unitOfWork.Received(4).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task when_fetching_a_page_fails_then_the_failure_is_reported_and_rethrown()
    {
        var exception = new InvalidOperationException("page fetch failed");
        jsonResponseProcessor.GetFromJsonAsync<SearchResponse>(Arg.Any<string>(), Arg.Any<HttpClient>(), Arg.Any<CancellationToken>())
            .Returns((Exceptional<Option<SearchResponse>>)exception);

        var thrown = await Should.ThrowAsync<InvalidOperationException>(Run);

        thrown.ShouldBeSameAs(exception);
        progress.Messages.ShouldContain("An error occurred during the fetching and processing of pages: page fetch failed");
    }

    private Task Run()
        => processor.FetchAndProcessPagesAsync(
            "wallpapers",
            Option.None<string>(),
            page => $"https://example.test/page/{page}",
            new WallhavenConnection("api-key", new Uri("https://example.test")),
            progress,
            CancellationToken.None);

    private void SetUpPage(int? page, SearchResponse response)
    {
        if (page is { } specificPage)
        {
            jsonResponseProcessor.GetFromJsonAsync<SearchResponse>($"https://example.test/page/{specificPage}", Arg.Any<HttpClient>(), Arg.Any<CancellationToken>())
                .Returns((Exceptional<Option<SearchResponse>>)(Option<SearchResponse>)response);

            return;
        }

        jsonResponseProcessor.GetFromJsonAsync<SearchResponse>(Arg.Any<string>(), Arg.Any<HttpClient>(), Arg.Any<CancellationToken>())
            .Returns((Exceptional<Option<SearchResponse>>)(Option<SearchResponse>)response);
    }

    private static SearchResponse CreateSearchResponse(int lastPage, params Data[] wallpapers)
        => new(wallpapers, new Meta(1, lastPage, 24, wallpapers.Length, default, new object()));

    private static Data CreateWallpaper(string id, string path = "")
        => new(id, "", "", 0, 0, "", "", "", 0, 0, "", "", 0, "", "", [], path, new Thumbs("", "", ""));

    private sealed class CapturingProgress : IProgress<string>
    {
        public List<string> Messages { get; } = [];

        public void Report(string value) => Messages.Add(value);
    }
}
