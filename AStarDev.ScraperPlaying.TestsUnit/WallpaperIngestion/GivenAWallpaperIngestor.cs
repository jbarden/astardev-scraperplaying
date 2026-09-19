using AStarDev.ControlDb;
using AStarDev.ControlDb.FileDetail;
using AStarDev.FunctionalParadigm;
using AStarDev.ScraperPlaying.Scraping;
using AStarDev.ScraperPlaying.WallpaperIngestion;
using Microsoft.Playwright;

namespace AStarDev.ScraperPlaying.TestsUnit.WallpaperIngestion;

public sealed class GivenAWallpaperIngestor
{
    private readonly IPage page = Substitute.For<IPage>();
    private readonly IWallpaperDetailPageScraper detailPageScraper = Substitute.For<IWallpaperDetailPageScraper>();
    private readonly IWallpaperDetailImageProcessor imageProcessor = Substitute.For<IWallpaperDetailImageProcessor>();
    private readonly ITagsProcessor tagsProcessor = Substitute.For<ITagsProcessor>();
    private readonly ISaveDirectoryResolver saveDirectoryResolver = Substitute.For<ISaveDirectoryResolver>();
    private readonly IRepository<FileEntity, FileId> fileRepository = Substitute.For<IRepository<FileEntity, FileId>>();
    private readonly Dictionary<string, FileEntity> recordedFiles = [];
    private readonly CapturingProgress progress = new();
    private readonly WallpaperIngestor ingestor;
    private readonly PageIngestionContext context;

    private int pacingDelayCount;

    public GivenAWallpaperIngestor()
    {
        context = new PageIngestionContext(page, new Uri("https://example.test"), Option.None<string>(), "Top Wallpapers", fileRepository);
        saveDirectoryResolver.ResolveSaveDirectoryAsync(Arg.Any<Option<string>>(), Arg.Any<bool>(), Arg.Any<CancellationToken>()).Returns(callInfo => callInfo.Arg<bool>() ? "famous-directory" : "some-directory");
        detailPageScraper.ScrapeAsync(Arg.Any<string>(), page, Arg.Any<Uri>(), Arg.Any<IProgress<string>>(), Arg.Any<CancellationToken>())
            .Returns(callInfo => CreateDetail((string)callInfo[0]));
        imageProcessor.DownloadAndRecordAsync(Arg.Any<ImageDownloadRequest>(), Arg.Any<IProgress<string>>(), Arg.Any<CancellationToken>())
            .Returns(callInfo => (Exceptional<FileEntity>)RecordedFile(callInfo.Arg<ImageDownloadRequest>().Detail.WallpaperId));
        tagsProcessor.LinkTagsAsync(Arg.Any<FileId>(), Arg.Any<IReadOnlyList<WallpaperTag>>(), Arg.Any<CancellationToken>()).Returns((Exceptional<Unit>)Unit.Instance);
        ingestor = new(detailPageScraper, saveDirectoryResolver, imageProcessor, tagsProcessor, () =>
        {
            pacingDelayCount++;

            return TimeSpan.FromMilliseconds(1);
        });
    }

    [Fact]
    public async Task when_a_wallpaper_is_ingested_then_it_is_scraped_downloaded_and_has_its_tags_linked_to_its_file()
    {
        await Run("wallpaper-1");

        await detailPageScraper.Received(1).ScrapeAsync("wallpaper-1", page, new Uri("https://example.test"), progress, Arg.Any<CancellationToken>());
        await imageProcessor.Received(1).DownloadAndRecordAsync(Arg.Is<ImageDownloadRequest>(request => request.Detail.WallpaperId == "wallpaper-1" && request.Page == page && request.Directory == "some-directory" && request.CategoryLabel == "Top Wallpapers" && request.FileRepository == fileRepository), progress, Arg.Any<CancellationToken>());
        await tagsProcessor.Received(1).LinkTagsAsync(recordedFiles["wallpaper-1"].Id, Arg.Is<IReadOnlyList<WallpaperTag>>(tags => tags.Count == 1 && tags[0].Name == "anime"), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task when_a_wallpaper_is_ingested_then_a_pacing_delay_is_applied_after_scraping_and_after_downloading()
    {
        await Run("wallpaper-1");

        pacingDelayCount.ShouldBe(2);
    }

    [Fact]
    public async Task when_a_category_is_being_searched_then_its_directory_and_label_are_used()
    {
        var categoryContext = context with { CategoryName = Option.Some("Nature"), CategoryLabel = "Nature" };

        await ingestor.IngestAsync("wallpaper-1", categoryContext, progress, CancellationToken.None);

        await saveDirectoryResolver.Received(1).ResolveSaveDirectoryAsync(Arg.Is<Option<string>>(name => name.Match(value => value == "Nature", () => false)), false, Arg.Any<CancellationToken>());
        await imageProcessor.Received(1).DownloadAndRecordAsync(Arg.Is<ImageDownloadRequest>(request => request.Detail.WallpaperId == "wallpaper-1" && request.Page == page && request.Directory == "some-directory" && request.CategoryLabel == "Nature" && request.FileRepository == fileRepository), progress, Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData("actress")]
    [InlineData("Model")]
    [InlineData("female singer")]
    [InlineData("SINGER-songwriter")]
    public async Task when_a_wallpaper_has_a_famous_tag_then_it_is_saved_under_the_famous_directory(string tagName)
    {
        detailPageScraper.ScrapeAsync("wallpaper-1", page, Arg.Any<Uri>(), Arg.Any<IProgress<string>>(), Arg.Any<CancellationToken>())
            .Returns(new WallpaperDetail("wallpaper-1", "https://example.test/wallpaper-1.jpg", 1920, 1080, [new WallpaperTag(1, "anime"), new WallpaperTag(2, tagName)]));

        await Run("wallpaper-1");

        await imageProcessor.Received(1).DownloadAndRecordAsync(Arg.Is<ImageDownloadRequest>(request => request.Detail.WallpaperId == "wallpaper-1" && request.Page == page && request.Directory == "famous-directory" && request.CategoryLabel == "Top Wallpapers" && request.FileRepository == fileRepository), progress, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task when_resolving_the_directory_fails_then_it_is_reported_and_the_wallpaper_is_skipped()
    {
        saveDirectoryResolver.ResolveSaveDirectoryAsync(Arg.Any<Option<string>>(), Arg.Any<bool>(), Arg.Any<CancellationToken>())
            .Returns<string>(_ => throw new InvalidOperationException("The famous root directory is not configured."));

        await Run("wallpaper-1");

        progress.Messages.ShouldContain("Failed to resolve the save directory for wallpaper wallpaper-1: The famous root directory is not configured.");
        await imageProcessor.DidNotReceive().DownloadAndRecordAsync(Arg.Any<ImageDownloadRequest>(), Arg.Any<IProgress<string>>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task when_scraping_the_wallpaper_fails_then_it_is_reported_and_nothing_is_downloaded()
    {
        detailPageScraper.ScrapeAsync("wallpaper-1", page, Arg.Any<Uri>(), Arg.Any<IProgress<string>>(), Arg.Any<CancellationToken>())
            .Returns<WallpaperDetail>(_ => throw new PlaywrightException("detail page timed out"));

        await Run("wallpaper-1");

        progress.Messages.ShouldContain("Failed to scrape wallpaper wallpaper-1: detail page timed out");
        await imageProcessor.DidNotReceive().DownloadAndRecordAsync(Arg.Any<ImageDownloadRequest>(), Arg.Any<IProgress<string>>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task when_downloading_the_wallpaper_fails_then_it_is_reported_and_its_tags_are_not_linked()
    {
        imageProcessor.DownloadAndRecordAsync(Arg.Any<ImageDownloadRequest>(), Arg.Any<IProgress<string>>(), Arg.Any<CancellationToken>())
            .Returns((Exceptional<FileEntity>)new HttpRequestException("status 403"));

        await Run("wallpaper-1");

        progress.Messages.ShouldContain("Failed to process image for wallpaper wallpaper-1: status 403");
        await tagsProcessor.DidNotReceive().LinkTagsAsync(Arg.Any<FileId>(), Arg.Any<IReadOnlyList<WallpaperTag>>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task when_linking_tags_fails_then_it_is_reported()
    {
        tagsProcessor.LinkTagsAsync(Arg.Any<FileId>(), Arg.Any<IReadOnlyList<WallpaperTag>>(), Arg.Any<CancellationToken>())
            .Returns((Exceptional<Unit>)new InvalidOperationException("tag insert failed"));

        await Run("wallpaper-1");

        progress.Messages.ShouldContain("Failed to link tags for wallpaper wallpaper-1: tag insert failed");
    }

    [Fact]
    public async Task when_cancelled_while_scraping_then_the_cancellation_propagates()
    {
        using var cancellationTokenSource = new CancellationTokenSource();
        detailPageScraper.ScrapeAsync("wallpaper-1", page, Arg.Any<Uri>(), Arg.Any<IProgress<string>>(), Arg.Any<CancellationToken>())
            .Returns<WallpaperDetail>(_ =>
            {
                cancellationTokenSource.Cancel();

                throw new OperationCanceledException(cancellationTokenSource.Token);
            });

        await Should.ThrowAsync<OperationCanceledException>(() => ingestor.IngestAsync("wallpaper-1", context, progress, cancellationTokenSource.Token));
    }

    private Task Run(string wallpaperId) => ingestor.IngestAsync(wallpaperId, context, progress, CancellationToken.None);

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

    private sealed class CapturingProgress : IProgress<string>
    {
        public List<string> Messages { get; } = [];

        public void Report(string value) => Messages.Add(value);
    }
}
