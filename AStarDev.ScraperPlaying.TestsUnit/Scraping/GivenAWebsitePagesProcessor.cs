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
    private readonly IPlaywrightBrowserSession browserSession = Substitute.For<IPlaywrightBrowserSession>();
    private readonly IPage page = Substitute.For<IPage>();
    private readonly ILocator locator = Substitute.For<ILocator>();
    private readonly IWallpaperDetailPageScraper detailPageScraper = Substitute.For<IWallpaperDetailPageScraper>();
    private readonly IWallpaperDetailImageProcessor imageProcessor = Substitute.For<IWallpaperDetailImageProcessor>();
    private readonly ITagsProcessor tagsProcessor = Substitute.For<ITagsProcessor>();
    private readonly ISaveDirectoryResolver saveDirectoryResolver = Substitute.For<ISaveDirectoryResolver>();
    private readonly IFilesQuery filesQuery = Substitute.For<IFilesQuery>();
    private readonly IUnitOfWork unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly IRepository<FileEntity, FileId> fileRepository = Substitute.For<IRepository<FileEntity, FileId>>();
    private readonly Dictionary<string, FileEntity> recordedFiles = [];
    private readonly Dictionary<int, string[]> wallpapersByPage = [];
    private readonly CapturingProgress progress = new();
    private readonly WebsitePagesProcessor processor;

    private int currentPageNumber;

    public GivenAWebsitePagesProcessor()
    {
        browserSession.GetPageAsync(Arg.Any<bool>(), Arg.Any<CancellationToken>()).Returns(page);
        page.GotoAsync(Arg.Any<string>(), Arg.Any<PageGotoOptions>())
            .Returns(callInfo =>
            {
                currentPageNumber = int.Parse(((string)callInfo[0]).Split('/')[^1]);

                return Substitute.For<IResponse>();
            });
        page.Locator("figure.thumb", Arg.Any<PageLocatorOptions?>()).Returns(locator);
        locator.EvaluateAllAsync<string[]>(Arg.Any<string>(), Arg.Any<object?>())
            .Returns(_ => wallpapersByPage.GetValueOrDefault(currentPageNumber, []));
        filesQuery.CheckExistsByHandleAsync(Arg.Any<FileHandle>(), Arg.Any<CancellationToken>()).Returns((Exceptional<bool>)false);
        unitOfWork.GetRepository<FileEntity, FileId>().Returns(fileRepository);
        saveDirectoryResolver.ResolveSaveDirectoryAsync(Arg.Any<Option<string>>(), Arg.Any<bool>(), Arg.Any<CancellationToken>()).Returns(callInfo => callInfo.Arg<bool>() ? "famous-directory" : "some-directory");
        detailPageScraper.ScrapeAsync(Arg.Any<string>(), page, Arg.Any<Uri>(), Arg.Any<IProgress<string>>(), Arg.Any<CancellationToken>())
            .Returns(callInfo => CreateDetail((string)callInfo[0]));
        imageProcessor.DownloadAndRecordAsync(Arg.Any<WallpaperDetail>(), page, Arg.Any<string>(), Arg.Any<string>(), fileRepository, Arg.Any<IProgress<string>>(), Arg.Any<CancellationToken>())
            .Returns(callInfo => (Exceptional<Option<FileEntity>>)Option.Some(RecordedFile(callInfo.Arg<WallpaperDetail>().WallpaperId)));
        tagsProcessor.LinkTagsAsync(Arg.Any<FileId>(), Arg.Any<IReadOnlyList<WallpaperTag>>(), Arg.Any<CancellationToken>()).Returns((Exceptional<Unit>)Unit.Instance);
        processor = new(browserSession, detailPageScraper, imageProcessor, tagsProcessor, saveDirectoryResolver, filesQuery, unitOfWork, () => TimeSpan.FromMilliseconds(1));
    }

    [Fact]
    public async Task when_a_page_has_wallpapers_then_they_are_reported_and_the_next_page_is_fetched()
    {
        wallpapersByPage[1] = ["wallpaper-1", "wallpaper-2"];

        await Run();

        progress.Messages.ShouldContain("Navigating to wallpapers page 1.");
        progress.Messages.ShouldContain("Found 2 wallpaper(s) on wallpapers page 1: wallpaper-1, wallpaper-2.");
        progress.Messages.ShouldContain("Navigating to wallpapers page 2.");
        progress.Messages.ShouldContain("No wallpapers found on wallpapers page 2.");
        progress.Messages.ShouldNotContain(message => message.Contains("page 3"));
    }

    [Fact]
    public async Task when_the_hard_cap_is_reached_then_paging_stops_at_page_4()
    {
        wallpapersByPage[1] = ["wallpaper-1"];
        wallpapersByPage[2] = ["wallpaper-2"];
        wallpapersByPage[3] = ["wallpaper-3"];
        wallpapersByPage[4] = ["wallpaper-4"];
        wallpapersByPage[5] = ["wallpaper-5"];

        await Run();

        progress.Messages.ShouldContain("Navigating to wallpapers page 4.");
        progress.Messages.ShouldNotContain(message => message.Contains("page 5"));
    }

    [Fact]
    public async Task when_navigation_fails_then_the_failure_is_reported_and_rethrown()
    {
        var exception = new PlaywrightException("navigation failed");
        page.GotoAsync(Arg.Any<string>(), Arg.Any<PageGotoOptions>()).Returns<IResponse?>(_ => throw exception);

        var thrown = await Should.ThrowAsync<PlaywrightException>(Run);

        thrown.ShouldBeSameAs(exception);
        progress.Messages.ShouldContain("An error occurred navigating the website during wallpapers: navigation failed");
    }

    [Fact]
    public async Task when_cancelled_then_cancellation_is_reported_and_rethrown()
    {
        using var cancellationTokenSource = new CancellationTokenSource();
        cancellationTokenSource.Cancel();

        await Should.ThrowAsync<OperationCanceledException>(
            () => processor.FetchAndProcessPagesAsync("wallpapers", Option.None<string>(), page => $"page/{page}", new WallhavenConnection("api-key", new Uri("https://example.test")), progress, cancellationTokenSource.Token));

        progress.Messages.ShouldContain("Scrape cancelled.");
    }

    [Fact]
    public async Task when_a_page_has_wallpapers_then_each_is_scraped_downloaded_and_has_its_tags_linked_to_its_file()
    {
        wallpapersByPage[1] = ["wallpaper-1", "wallpaper-2"];

        await Run();

        foreach (var wallpaperId in wallpapersByPage[1])
        {
            await detailPageScraper.Received(1).ScrapeAsync(wallpaperId, page, new Uri("https://example.test"), progress, Arg.Any<CancellationToken>());
            await imageProcessor.Received(1).DownloadAndRecordAsync(Arg.Is<WallpaperDetail>(detail => detail.WallpaperId == wallpaperId), page, "some-directory", "Top Wallpapers", fileRepository, progress, Arg.Any<CancellationToken>());
            await tagsProcessor.Received(1).LinkTagsAsync(recordedFiles[wallpaperId].Id, Arg.Is<IReadOnlyList<WallpaperTag>>(tags => tags.Count == 1 && tags[0].Name == "anime"), Arg.Any<CancellationToken>());
        }
    }

    [Fact]
    public async Task when_a_category_is_being_searched_then_its_directory_and_label_are_used()
    {
        wallpapersByPage[1] = ["wallpaper-1"];

        await processor.FetchAndProcessPagesAsync("nature", Option.Some("Nature"), page => $"page/{page}", Connection, progress, CancellationToken.None);

        await saveDirectoryResolver.Received(1).ResolveSaveDirectoryAsync(Arg.Is<Option<string>>(name => name.Match(value => value == "Nature", () => false)), false, Arg.Any<CancellationToken>());
        await imageProcessor.Received(1).DownloadAndRecordAsync(Arg.Any<WallpaperDetail>(), page, "some-directory", "Nature", fileRepository, progress, Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData("actress")]
    [InlineData("Model")]
    [InlineData("female singer")]
    [InlineData("SINGER-songwriter")]
    public async Task when_a_wallpaper_has_a_famous_tag_then_it_is_saved_under_the_famous_directory(string tagName)
    {
        wallpapersByPage[1] = ["wallpaper-1"];
        detailPageScraper.ScrapeAsync("wallpaper-1", page, Arg.Any<Uri>(), Arg.Any<IProgress<string>>(), Arg.Any<CancellationToken>())
            .Returns(new WallpaperDetail("wallpaper-1", "https://example.test/wallpaper-1.jpg", 1920, 1080, [new WallpaperTag(1, "anime"), new WallpaperTag(2, tagName)]));

        await Run();

        await imageProcessor.Received(1).DownloadAndRecordAsync(Arg.Any<WallpaperDetail>(), page, "famous-directory", "Top Wallpapers", fileRepository, progress, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task when_resolving_the_directory_fails_then_it_is_reported_and_the_wallpaper_is_skipped()
    {
        wallpapersByPage[1] = ["wallpaper-1", "wallpaper-2"];
        saveDirectoryResolver.ResolveSaveDirectoryAsync(Arg.Any<Option<string>>(), Arg.Any<bool>(), Arg.Any<CancellationToken>())
            .Returns<string>(_ => throw new InvalidOperationException("The famous root directory is not configured."), _ => "some-directory");

        await Run();

        progress.Messages.ShouldContain("Failed to resolve the save directory for wallpaper wallpaper-1: The famous root directory is not configured.");
        await imageProcessor.DidNotReceive().DownloadAndRecordAsync(Arg.Is<WallpaperDetail>(detail => detail.WallpaperId == "wallpaper-1"), page, Arg.Any<string>(), Arg.Any<string>(), fileRepository, Arg.Any<IProgress<string>>(), Arg.Any<CancellationToken>());
        await tagsProcessor.Received(1).LinkTagsAsync(recordedFiles["wallpaper-2"].Id, Arg.Any<IReadOnlyList<WallpaperTag>>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task when_a_wallpaper_was_already_downloaded_then_its_detail_page_is_not_visited_and_the_rest_of_the_page_is_still_processed()
    {
        wallpapersByPage[1] = ["wallpaper-1", "wallpaper-2"];
        filesQuery.CheckExistsByHandleAsync(FileHandle.Create("wallpaper-1"), Arg.Any<CancellationToken>()).Returns((Exceptional<bool>)true);

        await Run();

        progress.Messages.ShouldContain("Wallpaper wallpaper-1 was already downloaded - skipping.");
        await detailPageScraper.DidNotReceive().ScrapeAsync("wallpaper-1", Arg.Any<IPage>(), Arg.Any<Uri>(), Arg.Any<IProgress<string>>(), Arg.Any<CancellationToken>());
        await detailPageScraper.Received(1).ScrapeAsync("wallpaper-2", Arg.Any<IPage>(), Arg.Any<Uri>(), Arg.Any<IProgress<string>>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task when_checking_whether_a_wallpaper_was_already_downloaded_fails_then_it_is_reported_and_the_wallpaper_is_skipped()
    {
        wallpapersByPage[1] = ["wallpaper-1"];
        filesQuery.CheckExistsByHandleAsync(Arg.Any<FileHandle>(), Arg.Any<CancellationToken>()).Returns((Exceptional<bool>)new InvalidOperationException("db down"));

        await Run();

        progress.Messages.ShouldContain("Failed to check whether wallpaper wallpaper-1 was already downloaded: db down");
        await detailPageScraper.DidNotReceive().ScrapeAsync(Arg.Any<string>(), Arg.Any<IPage>(), Arg.Any<Uri>(), Arg.Any<IProgress<string>>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task when_a_wallpaper_already_exists_then_its_tags_are_not_linked()
    {
        wallpapersByPage[1] = ["wallpaper-1"];
        imageProcessor.DownloadAndRecordAsync(Arg.Any<WallpaperDetail>(), page, Arg.Any<string>(), Arg.Any<string>(), fileRepository, Arg.Any<IProgress<string>>(), Arg.Any<CancellationToken>())
            .Returns((Exceptional<Option<FileEntity>>)Option.None<FileEntity>());

        await Run();

        await tagsProcessor.DidNotReceive().LinkTagsAsync(Arg.Any<FileId>(), Arg.Any<IReadOnlyList<WallpaperTag>>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task when_scraping_a_wallpaper_fails_then_it_is_reported_and_the_rest_of_the_page_is_still_processed()
    {
        wallpapersByPage[1] = ["wallpaper-1", "wallpaper-2"];
        detailPageScraper.ScrapeAsync("wallpaper-1", page, Arg.Any<Uri>(), Arg.Any<IProgress<string>>(), Arg.Any<CancellationToken>())
            .Returns<WallpaperDetail>(_ => throw new PlaywrightException("detail page timed out"));

        await Run();

        progress.Messages.ShouldContain("Failed to scrape wallpaper wallpaper-1: detail page timed out");
        await imageProcessor.DidNotReceive().DownloadAndRecordAsync(Arg.Is<WallpaperDetail>(detail => detail.WallpaperId == "wallpaper-1"), page, Arg.Any<string>(), Arg.Any<string>(), fileRepository, Arg.Any<IProgress<string>>(), Arg.Any<CancellationToken>());
        await tagsProcessor.Received(1).LinkTagsAsync(recordedFiles["wallpaper-2"].Id, Arg.Any<IReadOnlyList<WallpaperTag>>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task when_downloading_a_wallpaper_fails_then_it_is_reported_its_tags_are_not_linked_and_the_rest_of_the_page_is_still_processed()
    {
        wallpapersByPage[1] = ["wallpaper-1", "wallpaper-2"];
        imageProcessor.DownloadAndRecordAsync(Arg.Is<WallpaperDetail>(detail => detail.WallpaperId == "wallpaper-1"), page, Arg.Any<string>(), Arg.Any<string>(), fileRepository, Arg.Any<IProgress<string>>(), Arg.Any<CancellationToken>())
            .Returns((Exceptional<Option<FileEntity>>)new HttpRequestException("status 403"));

        await Run();

        progress.Messages.ShouldContain("Failed to process image for wallpaper wallpaper-1: status 403");
        await tagsProcessor.Received(1).LinkTagsAsync(Arg.Any<FileId>(), Arg.Any<IReadOnlyList<WallpaperTag>>(), Arg.Any<CancellationToken>());
        await tagsProcessor.Received(1).LinkTagsAsync(recordedFiles["wallpaper-2"].Id, Arg.Any<IReadOnlyList<WallpaperTag>>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task when_linking_tags_fails_then_it_is_reported_and_the_rest_of_the_page_is_still_processed()
    {
        wallpapersByPage[1] = ["wallpaper-1", "wallpaper-2"];
        tagsProcessor.LinkTagsAsync(Arg.Any<FileId>(), Arg.Any<IReadOnlyList<WallpaperTag>>(), Arg.Any<CancellationToken>())
            .Returns((Exceptional<Unit>)new InvalidOperationException("tag insert failed"), (Exceptional<Unit>)Unit.Instance);

        await Run();

        progress.Messages.ShouldContain("Failed to link tags for wallpaper wallpaper-1: tag insert failed");
        await tagsProcessor.Received(2).LinkTagsAsync(Arg.Any<FileId>(), Arg.Any<IReadOnlyList<WallpaperTag>>(), Arg.Any<CancellationToken>());
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
        detailPageScraper.ScrapeAsync("wallpaper-2", page, Arg.Any<Uri>(), Arg.Any<IProgress<string>>(), Arg.Any<CancellationToken>())
            .Returns<WallpaperDetail>(_ =>
            {
                cancellationTokenSource.Cancel();

                throw new OperationCanceledException(cancellationTokenSource.Token);
            });

        await Should.ThrowAsync<OperationCanceledException>(
            () => processor.FetchAndProcessPagesAsync("wallpapers", Option.None<string>(), page => $"page/{page}", Connection, progress, cancellationTokenSource.Token));

        await unitOfWork.Received(1).SaveChangesAsync(CancellationToken.None);
        progress.Messages.ShouldContain("Scrape cancelled.");
    }

    [Fact]
    public async Task when_saving_after_cancellation_fails_then_the_failure_is_reported_and_cancellation_is_still_rethrown()
    {
        using var cancellationTokenSource = new CancellationTokenSource();
        cancellationTokenSource.Cancel();
        unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns<Task<int>>(_ => throw new DbUpdateException("save failed"));

        await Should.ThrowAsync<OperationCanceledException>(
            () => processor.FetchAndProcessPagesAsync("wallpapers", Option.None<string>(), page => $"page/{page}", Connection, progress, cancellationTokenSource.Token));

        progress.Messages.ShouldContain("Scrape cancelled - failed to save wallpapers downloaded so far this page: save failed");
    }

    private static readonly WallhavenConnection Connection = new("api-key", new Uri("https://example.test"));

    private FileEntity RecordedFile(string wallpaperId)
    {
        if (!recordedFiles.TryGetValue(wallpaperId, out var file))
        {
            file = new FileEntity
            {
                Id = FileId.Create(),
                FileName = new FileName($"{wallpaperId}.jpg"),
                DirectoryName = DirectoryName.Create("some-directory"),
                FileHandle = FileHandle.Create(wallpaperId),
                FileSize = 1
            };
            recordedFiles[wallpaperId] = file;
        }

        return file;
    }

    private static WallpaperDetail CreateDetail(string wallpaperId)
        => new(wallpaperId, $"https://example.test/{wallpaperId}.jpg", 1920, 1080, [new WallpaperTag(1, "anime")]);

    private Task Run()
        => processor.FetchAndProcessPagesAsync("wallpapers", Option.None<string>(), page => $"page/{page}", new WallhavenConnection("api-key", new Uri("https://example.test")), progress, CancellationToken.None);

    private sealed class CapturingProgress : IProgress<string>
    {
        public List<string> Messages { get; } = [];

        public void Report(string value) => Messages.Add(value);
    }
}
