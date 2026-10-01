using AStarDev.ControlDb;
using AStarDev.ControlDb.FileDetail;
using AStarDev.FunctionalParadigm;
using AStarDev.ScraperPlaying.Scraping;
using AStarDev.ScraperPlaying.Scraping.WallhavenResponses.SearchResponse;
using AStarDev.ScraperPlaying.TestsUnit.Fakes;
using AStarDev.ScraperPlaying.WallpaperIngestion;
using Tag = AStarDev.ScraperPlaying.Scraping.WallhavenResponses.DetailResponse.Tag;

namespace AStarDev.ScraperPlaying.TestsUnit.WallpaperIngestion;

public sealed class GivenANewWallpaperIngestor
{
    private readonly FakeRepository<FileEntity, FileId> fileRepository = new();
    private readonly FakeImageDownloader imageDownloader = new();
    private readonly FakeFileRecorder fileRecorder = new();
    private readonly FakeTagsProcessor tagsProcessor = new();
    private readonly ImageDownloadNotifier notifier = new();
    private readonly List<WallpaperDownloadDetails> notifications = [];
    private readonly CapturingProgress progress = new();
    private readonly NewWallpaperIngestor ingestor;

    public GivenANewWallpaperIngestor()
    {
        notifier.ImageDownloaded += (_, details) => notifications.Add(details);
        ingestor = new(tagsProcessor, imageDownloader, fileRecorder, notifier);
    }

    [Fact]
    public async Task when_a_wallpaper_is_ingested_then_it_is_downloaded_recorded_and_its_tags_are_linked()
    {
        var wallpaper = CreateWallpaper("new-wallpaper");
        var fileEntity = new FileEntity { FileName = new("new-wallpaper.jpg"), DirectoryName = new(""), FileHandle = new(""), FileSize = 0 };
        fileRecorder.Result = fileEntity;

        await Ingest(wallpaper, ".jpg", directory: "resolved-directory");

        var expectedRequest = new WallpaperFileRequest(wallpaper, "resolved-directory", new FileName("new-wallpaper.jpg"), "resolved-category");
        progress.Messages.ShouldContain("No existing data found for wallpaper new-wallpaper.");
        progress.Messages.ShouldContain("Downloaded image data for wallpaper new-wallpaper");
        progress.Messages.ShouldNotContain(message => message.Contains("Failed"));
        (imageDownloader.Requests.Single(), fileRecorder.Recorded.Single(), tagsProcessor.Linked.Single().FileId).ShouldBe((expectedRequest, expectedRequest, fileEntity.Id));
    }

    [Fact]
    public async Task when_a_wallpaper_has_a_tag_flagged_to_ignore_images_then_it_is_not_downloaded_recorded_or_linked()
    {
        tagsProcessor.FetchResult = Exceptional.Success<IReadOnlyList<Tag>>([new Tag(1, "kept", "kept", 1, "Nature", "sfw"), new Tag(2, "unwanted", "unwanted", 1, "Nature", "sfw", IgnoreImage: true)]);

        await Ingest(CreateWallpaper("ignored-wallpaper"), ".jpg");

        progress.Messages.ShouldContain("Ignoring wallpaper ignored-wallpaper: it has a tag flagged to ignore images.");
        (imageDownloader.Requests.Count, fileRecorder.Recorded.Count, tagsProcessor.Linked.Count, notifications.Count).ShouldBe((0, 0, 0, 0));
    }

    [Fact]
    public async Task when_a_wallpaper_is_downloaded_then_the_download_is_announced_with_the_saved_path_and_details()
    {
        var wallpaper = new Data("wallpaper-3", 1920, 1080, 5, "image/jpeg", "https://example.test/image.jpg");
        imageDownloader.SavedPath = "/saved/wallpaper-3.jpg";

        await Ingest(wallpaper, ".jpg");

        notifications.ShouldBe([new WallpaperDownloadDetails("/saved/wallpaper-3.jpg", "wallpaper-3.jpg", "resolved-category", 5, 1920, 1080)]);
    }

    [Fact]
    public async Task when_only_the_tags_are_fetched_then_nothing_is_downloaded_recorded_or_linked()
    {
        using var client = new HttpClient();

        var tags = await ingestor.FetchTagsAsync(CreateWallpaper("tags-only"), new WallpaperIngestionContext(new SaveDirectories("some-directory", "famous-some-directory"), client, fileRepository, "resolved-category", []), progress, TestContext.Current.CancellationToken);

        (tags.Count, imageDownloader.Requests.Count, fileRecorder.Recorded.Count, tagsProcessor.Linked.Count, progress.Messages.Contains("No existing data found for wallpaper tags-only.")).ShouldBe((0, 0, 0, 0, true));
    }

    [Fact]
    public async Task when_downloading_fails_then_the_failure_is_reported_and_nothing_is_recorded_linked_or_announced()
    {
        imageDownloader.OnDownload = () => throw new HttpRequestException("download failed");

        await Ingest(CreateWallpaper("failing-download"), ".jpg");

        progress.Messages.ShouldContain("Failed to process image for wallpaper failing-download: download failed");
        (fileRecorder.Recorded.Count, tagsProcessor.Linked.Count, notifications.Count).ShouldBe((0, 0, 0));
    }

    [Fact]
    public async Task when_recording_the_file_fails_then_the_failure_is_reported_and_no_tags_are_linked()
    {
        fileRecorder.Result = new InvalidOperationException("process failed");

        await Ingest(CreateWallpaper("failing-process"), ".jpg");

        progress.Messages.ShouldContain("Failed to process image for wallpaper failing-process: process failed");
        tagsProcessor.Linked.ShouldBeEmpty();
    }

    [Fact]
    public async Task when_fetching_the_tags_fails_then_it_is_reported_distinctly_and_the_image_is_still_ingested_without_tags()
    {
        tagsProcessor.FetchResult = new InvalidOperationException("tag fetch failed");

        await Ingest(CreateWallpaper("failing-tags"), ".jpg");

        progress.Messages.ShouldContain("Failed to fetch tags for wallpaper failing-tags: tag fetch failed");
        progress.Messages.ShouldNotContain(message => message.Contains("Failed to process image"));
        progress.Messages.ShouldContain("Downloaded image data for wallpaper failing-tags");
        tagsProcessor.Linked.Single().Tags.ShouldBeEmpty();
    }

    [Fact]
    public async Task when_linking_the_tags_fails_then_it_is_reported_distinctly_and_not_thrown()
    {
        tagsProcessor.LinkResult = new InvalidOperationException("link failed");

        await Ingest(CreateWallpaper("failing-link"), ".jpg");

        progress.Messages.ShouldContain("Failed to link tags for wallpaper failing-link: link failed");
        progress.Messages.ShouldNotContain(message => message.Contains("Failed to process image"));
    }

    [Fact]
    public async Task when_the_operation_is_cancelled_while_downloading_then_the_cancellation_propagates()
    {
        using var cancellationTokenSource = new CancellationTokenSource();
        imageDownloader.OnDownload = () =>
        {
            cancellationTokenSource.Cancel();

            throw new OperationCanceledException(cancellationTokenSource.Token);
        };

        _ = await Should.ThrowAsync<OperationCanceledException>(() => IngestWithCancellation(CreateWallpaper("cancelled"), ".jpg", cancellationTokenSource.Token));

        progress.Messages.ShouldNotContain(message => message.Contains("Failed"));
    }

    [Fact]
    public async Task when_the_operation_is_cancelled_after_downloading_then_nothing_is_recorded_and_the_cancellation_propagates()
    {
        using var cancellationTokenSource = new CancellationTokenSource();
        imageDownloader.OnDownload = cancellationTokenSource.Cancel;

        _ = await Should.ThrowAsync<OperationCanceledException>(() => IngestWithCancellation(CreateWallpaper("cancelled-after"), ".jpg", cancellationTokenSource.Token));

        fileRecorder.Recorded.ShouldBeEmpty();
    }

    [Fact]
    public async Task when_a_wallpaper_has_a_famous_tag_then_it_is_saved_under_the_famous_directory()
    {
        tagsProcessor.FetchResult = Exceptional.Success<IReadOnlyList<Tag>>([new Tag(1, "Emma Watson", "emma-watson", 1, "Celebrities", "sfw", IsFamous: true)]);
        var wallpaper = CreateWallpaper("famous-wallpaper");

        await Ingest(wallpaper, ".jpg", directory: "resolved-directory");

        imageDownloader.Requests.Single().Directory.ShouldBe("famous-resolved-directory");
    }

    private Task Ingest(Data wallpaper, string extension, string directory = "some-directory")
        => IngestWithToken(wallpaper, extension, directory, TestContext.Current.CancellationToken);

    private Task IngestWithCancellation(Data wallpaper, string extension, CancellationToken cancellationToken)
        => IngestWithToken(wallpaper, extension, "some-directory", cancellationToken);

    private async Task IngestWithToken(Data wallpaper, string extension, string directory, CancellationToken cancellationToken)
    {
        using var client = new HttpClient();
        var context = new WallpaperIngestionContext(new SaveDirectories(directory, $"famous-{directory}"), client, fileRepository, "resolved-category", []);

        var tags = await ingestor.FetchTagsAsync(wallpaper, context, progress, cancellationToken);
        await ingestor.IngestAsync(wallpaper, extension, tags, context, progress, cancellationToken);
    }

    private static Data CreateWallpaper(string id) => new(id, 0, 0, 0, "", "");

    private sealed class FakeImageDownloader : IImageDownloader
    {
        public List<WallpaperFileRequest> Requests { get; } = [];

        public string SavedPath { get; set; } = "/saved/file.jpg";

        public Action OnDownload { get; set; } = () => { };

        public Task<string> DownloadAsync(WallpaperFileRequest request, IProgress<string> progress, HttpClient client, CancellationToken cancellationToken)
        {
            OnDownload();
            Requests.Add(request);

            return Task.FromResult(SavedPath);
        }
    }

    private sealed class FakeFileRecorder : IWallpaperFileRecorder
    {
        private static readonly FileEntity DefaultEntity = new() { FileName = new("default"), DirectoryName = new(""), FileHandle = new(""), FileSize = 0 };

        public List<WallpaperFileRequest> Recorded { get; } = [];

        public Exceptional<FileEntity> Result { get; set; } = DefaultEntity;

        public Exceptional<FileEntity> Record(IRepository<FileEntity, FileId> fileRepository, WallpaperFileRequest request)
        {
            Recorded.Add(request);

            return Result;
        }
    }

    private sealed class FakeTagsProcessor : ITagsProcessor
    {
        public Exceptional<IReadOnlyList<Tag>> FetchResult { get; set; } = Exceptional.Success<IReadOnlyList<Tag>>([]);

        public Exceptional<Unit> LinkResult { get; set; } = Unit.Instance;

        public List<(FileId FileId, IReadOnlyList<Tag> Tags)> Linked { get; } = [];

        public Task<Exceptional<IReadOnlyList<Tag>>> FetchTagsAsync(string wallpaperId, HttpClient client, IReadOnlyList<string> personCategories, IProgress<string> progress, CancellationToken cancellationToken) =>
            Task.FromResult(FetchResult);

        public Task<Exceptional<Unit>> LinkTagsAsync(FileId fileId, IReadOnlyList<Tag> tags, CancellationToken cancellationToken)
        {
            Linked.Add((fileId, tags));

            return Task.FromResult(LinkResult);
        }
    }

    private sealed class CapturingProgress : IProgress<string>
    {
        public List<string> Messages { get; } = [];

        public void Report(string value) => Messages.Add(value);
    }
}
