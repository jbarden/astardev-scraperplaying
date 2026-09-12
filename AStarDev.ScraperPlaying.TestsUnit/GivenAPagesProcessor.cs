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
    private readonly IFilesQuery filesQuery = Substitute.For<IFilesQuery>();
    private readonly IJsonResponseProcessor jsonResponseProcessor = Substitute.For<IJsonResponseProcessor>();
    private readonly IImageProcessor imageProcessor = Substitute.For<IImageProcessor>();
    private readonly ITagsProcessor tagsProcessor = Substitute.For<ITagsProcessor>();
    private readonly CapturingProgress progress = new();
    private readonly PagesProcessor processor;

    public GivenAPagesProcessor()
    {
        httpClientFactory.CreateClient(ApplicationConstants.WallhavenHttpClientName).Returns(_ => new HttpClient());
        unitOfWork.GetRepository<FileEntity, FileId>().Returns(fileRepository);
        unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(1);
        tagsProcessor.FetchAndLinkTagsAsync(Arg.Any<string>(), Arg.Any<FileId>(), Arg.Any<HttpClient>(), Arg.Any<IProgress<string>>(), Arg.Any<CancellationToken>())
            .Returns((Exceptional<UnitFp>)UnitFp.Instance);
        processor = new(httpClientFactory, unitOfWork, filesQuery, jsonResponseProcessor, imageProcessor, tagsProcessor, () => TimeSpan.FromMilliseconds(1));
    }

    [Fact]
    public async Task when_a_wallpaper_already_exists_then_it_is_skipped_and_not_downloaded()
    {
        var wallpaper = CreateWallpaper("existing-wallpaper");
        SetUpPage(1, CreateSearchResponse(lastPage: 1, wallpaper));
        filesQuery.CheckExistsByNameAsync(Arg.Any<FileName>(), Arg.Any<CancellationToken>()).Returns((Exceptional<bool>)true);

        await Run();

        progress.Messages.ShouldContain("The file details already exist for wallpaper existing-wallpaper - no need to fetch again.");
        progress.Messages.ShouldNotContain(message => message.Contains("Downloaded image data"));
        await imageProcessor.DidNotReceive().DownloadImageAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<IProgress<string>>(), Arg.Any<HttpClient>(), Arg.Any<CancellationToken>());
        await tagsProcessor.DidNotReceive().FetchAndLinkTagsAsync(Arg.Any<string>(), Arg.Any<FileId>(), Arg.Any<HttpClient>(), Arg.Any<IProgress<string>>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task when_a_wallpaper_is_new_then_it_is_downloaded_and_processed()
    {
        var wallpaper = CreateWallpaper("new-wallpaper");
        SetUpPage(1, CreateSearchResponse(lastPage: 1, wallpaper));
        filesQuery.CheckExistsByNameAsync(Arg.Any<FileName>(), Arg.Any<CancellationToken>()).Returns((Exceptional<bool>)false);
        imageProcessor.DownloadImageAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<IProgress<string>>(), Arg.Any<HttpClient>(), Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);
        var fileEntity = new FileEntity { FileName = new("new-wallpaper"), DirectoryName = new(""), FileHandle = new(""), FileSize = 0 };
        imageProcessor.ProcessTheImageAsync(Arg.Any<IRepository<FileEntity, FileId>>(), Arg.Any<Data>(), Arg.Any<CancellationToken>()).Returns((Exceptional<FileEntity>)fileEntity);

        await Run();

        progress.Messages.ShouldContain("No existing file found for wallpaper new-wallpaper.");
        progress.Messages.ShouldContain("Downloaded image data for wallpaper new-wallpaper");
        progress.Messages.ShouldNotContain(message => message.Contains("Failed"));
        await unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
        await tagsProcessor.Received(1).FetchAndLinkTagsAsync("new-wallpaper", fileEntity.Id, Arg.Any<HttpClient>(), progress, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task when_downloading_a_new_wallpaper_fails_then_the_failure_is_reported_and_the_page_completes()
    {
        var wallpaper = CreateWallpaper("failing-download");
        SetUpPage(1, CreateSearchResponse(lastPage: 1, wallpaper));
        filesQuery.CheckExistsByNameAsync(Arg.Any<FileName>(), Arg.Any<CancellationToken>()).Returns((Exceptional<bool>)false);
        imageProcessor.DownloadImageAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<IProgress<string>>(), Arg.Any<HttpClient>(), Arg.Any<CancellationToken>())
            .Returns(_ => throw new HttpRequestException("download failed"));

        await Run();

        progress.Messages.ShouldContain("Failed to process image for wallpaper failing-download: download failed");
        await unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
        await tagsProcessor.DidNotReceive().FetchAndLinkTagsAsync(Arg.Any<string>(), Arg.Any<FileId>(), Arg.Any<HttpClient>(), Arg.Any<IProgress<string>>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task when_processing_a_new_wallpaper_fails_then_the_failure_is_reported_and_the_page_completes()
    {
        var wallpaper = CreateWallpaper("failing-process");
        SetUpPage(1, CreateSearchResponse(lastPage: 1, wallpaper));
        filesQuery.CheckExistsByNameAsync(Arg.Any<FileName>(), Arg.Any<CancellationToken>()).Returns((Exceptional<bool>)false);
        imageProcessor.DownloadImageAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<IProgress<string>>(), Arg.Any<HttpClient>(), Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);
        var exception = new InvalidOperationException("process failed");
        imageProcessor.ProcessTheImageAsync(Arg.Any<IRepository<FileEntity, FileId>>(), Arg.Any<Data>(), Arg.Any<CancellationToken>()).Returns((Exceptional<FileEntity>)exception);

        await Run();

        progress.Messages.ShouldContain("Failed to process image for wallpaper failing-process: process failed");
        await unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
        await tagsProcessor.DidNotReceive().FetchAndLinkTagsAsync(Arg.Any<string>(), Arg.Any<FileId>(), Arg.Any<HttpClient>(), Arg.Any<IProgress<string>>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task when_checking_whether_a_wallpaper_already_exists_fails_then_the_failure_is_reported_and_the_wallpaper_is_skipped()
    {
        var wallpaper = CreateWallpaper("check-fails");
        SetUpPage(1, CreateSearchResponse(lastPage: 1, wallpaper));
        var exception = new InvalidOperationException("query failed");
        filesQuery.CheckExistsByNameAsync(Arg.Any<FileName>(), Arg.Any<CancellationToken>()).Returns((Exceptional<bool>)exception);

        await Run();

        progress.Messages.ShouldContain("Failed to check whether the file details already exist for wallpaper check-fails: query failed");
        await imageProcessor.DidNotReceive().DownloadImageAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<IProgress<string>>(), Arg.Any<HttpClient>(), Arg.Any<CancellationToken>());
        await unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
        await tagsProcessor.DidNotReceive().FetchAndLinkTagsAsync(Arg.Any<string>(), Arg.Any<FileId>(), Arg.Any<HttpClient>(), Arg.Any<IProgress<string>>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task when_fetching_tags_for_a_new_wallpaper_fails_then_the_failure_is_reported_distinctly_and_the_page_completes()
    {
        var wallpaper = CreateWallpaper("failing-tags");
        SetUpPage(1, CreateSearchResponse(lastPage: 1, wallpaper));
        filesQuery.CheckExistsByNameAsync(Arg.Any<FileName>(), Arg.Any<CancellationToken>()).Returns((Exceptional<bool>)false);
        imageProcessor.DownloadImageAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<IProgress<string>>(), Arg.Any<HttpClient>(), Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);
        var fileEntity = new FileEntity { FileName = new("failing-tags"), DirectoryName = new(""), FileHandle = new(""), FileSize = 0 };
        imageProcessor.ProcessTheImageAsync(Arg.Any<IRepository<FileEntity, FileId>>(), Arg.Any<Data>(), Arg.Any<CancellationToken>()).Returns((Exceptional<FileEntity>)fileEntity);
        var exception = new InvalidOperationException("tag fetch failed");
        tagsProcessor.FetchAndLinkTagsAsync(Arg.Any<string>(), Arg.Any<FileId>(), Arg.Any<HttpClient>(), Arg.Any<IProgress<string>>(), Arg.Any<CancellationToken>())
            .Returns((Exceptional<UnitFp>)exception);

        await Run();

        progress.Messages.ShouldContain("Failed to fetch tags for wallpaper failing-tags: tag fetch failed");
        progress.Messages.ShouldNotContain(message => message.Contains("Failed to process image"));
        await unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
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
            page => $"https://example.test/page/{page}",
            "api-key",
            new Uri("https://example.test"),
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

    private static Data CreateWallpaper(string id)
        => new(id, "", "", 0, 0, "", "", "", 0, 0, "", "", 0, "", "", [], "", new Thumbs("", "", ""));

    private sealed class CapturingProgress : IProgress<string>
    {
        public List<string> Messages { get; } = [];

        public void Report(string value) => Messages.Add(value);
    }
}
