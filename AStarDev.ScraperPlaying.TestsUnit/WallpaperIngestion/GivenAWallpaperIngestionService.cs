using AStarDev.ControlDb;
using AStarDev.ControlDb.FileDetail;
using AStarDev.FunctionalParadigm;
using AStarDev.ScraperPlaying.Scraping;
using AStarDev.ScraperPlaying.Scraping.WallhavenResponses.SearchResponse;
using AStarDev.ScraperPlaying.TestsUnit.Fakes;
using Tag = AStarDev.ScraperPlaying.Scraping.WallhavenResponses.DetailResponse.Tag;
using AStarDev.ScraperPlaying.WallpaperIngestion;

namespace AStarDev.ScraperPlaying.TestsUnit.WallpaperIngestion;

public sealed class GivenAWallpaperIngestionService
{
    private readonly FakeRepository<FileEntity, FileId> fileRepository = new();
    private readonly FakeFilesQuery filesQuery = new();
    private readonly FakeImageProcessor imageProcessor = new();
    private readonly FakeTagsProcessor tagsProcessor = new();
    private readonly CapturingProgress progress = new();
    private readonly WallpaperIngestionService service;

    public GivenAWallpaperIngestionService() =>
        service = new(filesQuery, imageProcessor, tagsProcessor);

    [Fact]
    public async Task when_a_wallpaper_already_exists_then_it_is_skipped_and_not_downloaded()
    {
        var wallpaper = CreateWallpaper("existing-wallpaper");
        filesQuery.ExistsResult = true;

        await Ingest(wallpaper);

        progress.Messages.ShouldContain("The file details already exist for wallpaper existing-wallpaper - no need to fetch again.");
        progress.Messages.ShouldNotContain(message => message.Contains("Downloaded image data"));
        imageProcessor.Downloads.ShouldBeEmpty();
        tagsProcessor.Linked.ShouldBeEmpty();
    }

    [Fact]
    public async Task when_a_page_has_several_wallpapers_then_existence_is_checked_with_a_single_query_and_only_the_new_ones_are_downloaded()
    {
        var existing = CreateWallpaper("existing-wallpaper", path: "https://example.test/full/existing-wallpaper.jpg");
        var fresh = CreateWallpaper("fresh-wallpaper", path: "https://example.test/full/fresh-wallpaper.jpg");
        var another = CreateWallpaper("another-wallpaper", path: "https://example.test/full/another-wallpaper.jpg");
        filesQuery.ExistingNames.Add(NameFor(existing, ".jpg"));
        using var client = new HttpClient();

        await service.IngestPageAsync([existing, fresh, another], new WallpaperIngestionContext("some-directory", client, fileRepository, "resolved-category", []), progress, CancellationToken.None);

        (filesQuery.ExistingNamesQueryCount, string.Join(",", imageProcessor.Downloads.Select(request => request.Wallpaper.Id))).ShouldBe((1, "fresh-wallpaper,another-wallpaper"));
    }

    [Fact]
    public async Task when_the_stored_name_differs_only_by_case_then_the_wallpaper_is_still_treated_as_existing()
    {
        var wallpaper = CreateWallpaper("Mixed-Case", path: "https://example.test/full/Mixed-Case.jpg");
        filesQuery.ExistingNames.Add(new FileName("mixed-case.jpg"));

        await Ingest(wallpaper);

        imageProcessor.Downloads.ShouldBeEmpty();
    }

    [Fact]
    public async Task when_a_page_has_no_wallpapers_then_no_query_is_made()
    {
        using var client = new HttpClient();

        await service.IngestPageAsync([], new WallpaperIngestionContext("some-directory", client, fileRepository, "resolved-category", []), progress, CancellationToken.None);

        filesQuery.ExistingNamesQueryCount.ShouldBe(0);
    }

    [Fact]
    public async Task when_a_wallpaper_is_new_then_it_is_downloaded_and_processed()
    {
        var wallpaper = CreateWallpaper("new-wallpaper");
        var fileEntity = new FileEntity { FileName = new("new-wallpaper"), DirectoryName = new(""), FileHandle = new(""), FileSize = 0 };
        imageProcessor.ProcessResult = fileEntity;

        await Ingest(wallpaper);

        progress.Messages.ShouldContain("No existing data found for wallpaper new-wallpaper.");
        progress.Messages.ShouldContain("Downloaded image data for wallpaper new-wallpaper");
        progress.Messages.ShouldNotContain(message => message.Contains("Failed"));
        tagsProcessor.Linked.Select(link => link.FileId).ShouldBe([fileEntity.Id]);
    }

    [Fact]
    public async Task when_a_wallpaper_has_a_non_jpg_extension_then_the_real_extension_is_used_for_the_existence_check_and_download()
    {
        var wallpaper = CreateWallpaper("png-wallpaper", path: "https://example.test/full/png-wallpaper.png");
        imageProcessor.ProcessResult = new FileEntity { FileName = new("png-wallpaper.png"), DirectoryName = new(""), FileHandle = new(""), FileSize = 0 };

        await Ingest(wallpaper, directory: "resolved-directory");

        filesQuery.CheckedNames.Select(name => name.Value).ShouldBe(["png-wallpaper.png"]);
        var expectedRequest = new WallpaperFileRequest(wallpaper, "resolved-directory", NameFor(wallpaper, ".png"), "resolved-category");
        imageProcessor.Downloads.ShouldBe([expectedRequest]);
        imageProcessor.Processed.ShouldBe([expectedRequest]);
    }

    [Fact]
    public async Task when_a_wallpaper_source_path_has_no_extension_then_the_jpg_fallback_is_used_for_the_existence_check_and_download()
    {
        var wallpaper = CreateWallpaper("extensionless-wallpaper", path: "https://example.test/full/extensionless-wallpaper");
        imageProcessor.ProcessResult = new FileEntity { FileName = new("extensionless-wallpaper.jpg"), DirectoryName = new(""), FileHandle = new(""), FileSize = 0 };

        await Ingest(wallpaper, directory: "resolved-directory");

        filesQuery.CheckedNames.Select(name => name.Value).ShouldBe(["extensionless-wallpaper.jpg"]);
        var expectedRequest = new WallpaperFileRequest(wallpaper, "resolved-directory", NameFor(wallpaper, ".jpg"), "resolved-category");
        imageProcessor.Downloads.ShouldBe([expectedRequest]);
        imageProcessor.Processed.ShouldBe([expectedRequest]);
    }

    [Fact]
    public async Task when_downloading_a_new_wallpaper_fails_then_the_failure_is_reported_and_not_thrown()
    {
        var wallpaper = CreateWallpaper("failing-download");
        imageProcessor.OnDownload = () => throw new HttpRequestException("download failed");

        await Ingest(wallpaper);

        progress.Messages.ShouldContain("Failed to process image for wallpaper failing-download: download failed");
        tagsProcessor.Linked.ShouldBeEmpty();
    }

    [Fact]
    public async Task when_processing_a_new_wallpaper_fails_then_the_failure_is_reported_and_not_thrown()
    {
        var wallpaper = CreateWallpaper("failing-process");
        imageProcessor.ProcessResult = new InvalidOperationException("process failed");

        await Ingest(wallpaper);

        progress.Messages.ShouldContain("Failed to process image for wallpaper failing-process: process failed");
        tagsProcessor.Linked.ShouldBeEmpty();
    }

    [Fact]
    public async Task when_checking_whether_a_wallpaper_already_exists_fails_then_the_failure_is_reported_and_the_wallpaper_is_skipped()
    {
        var wallpaper = CreateWallpaper("check-fails");
        filesQuery.ExistsResult = new InvalidOperationException("query failed");

        await Ingest(wallpaper);

        progress.Messages.ShouldContain("Failed to check whether the file details already exist for this page of wallpapers: query failed");
        imageProcessor.Downloads.ShouldBeEmpty();
        tagsProcessor.Linked.ShouldBeEmpty();
    }

    [Fact]
    public async Task when_fetching_tags_for_a_new_wallpaper_fails_then_the_failure_is_reported_distinctly_and_not_thrown()
    {
        var wallpaper = CreateWallpaper("failing-tags");
        imageProcessor.ProcessResult = new FileEntity { FileName = new("failing-tags"), DirectoryName = new(""), FileHandle = new(""), FileSize = 0 };
        tagsProcessor.FetchResult = new InvalidOperationException("tag fetch failed");

        await Ingest(wallpaper);

        progress.Messages.ShouldContain("Failed to fetch tags for wallpaper failing-tags: tag fetch failed");
        progress.Messages.ShouldNotContain(message => message.Contains("Failed to process image"));
        progress.Messages.ShouldContain("Downloaded image data for wallpaper failing-tags");
    }

    private async Task Ingest(Data wallpaper, string directory = "some-directory")
    {
        using var client = new HttpClient();

        await service.IngestPageAsync([wallpaper], new WallpaperIngestionContext(directory, client, fileRepository, "resolved-category", []), progress, CancellationToken.None);
    }

    private static FileName NameFor(Data wallpaper, string extension) => new($"{wallpaper.Id}{extension}");

    private static Data CreateWallpaper(string id, string path = "")
        => new(id, 0, 0, 0, "", path);

    private sealed class FakeImageProcessor : IImageProcessor
    {
        private static readonly FileEntity DefaultEntity = new() { FileName = new("default"), DirectoryName = new(""), FileHandle = new(""), FileSize = 0 };

        public List<WallpaperFileRequest> Downloads { get; } = [];

        public List<WallpaperFileRequest> Processed { get; } = [];

        public Action OnDownload { get; set; } = () => { };

        public Exceptional<FileEntity> ProcessResult { get; set; } = DefaultEntity;

        public Task DownloadImageAsync(WallpaperFileRequest request, IProgress<string> progress, HttpClient client, CancellationToken cancellationToken)
        {
            OnDownload();
            Downloads.Add(request);

            return Task.CompletedTask;
        }

        public Task<Exceptional<FileEntity>> ProcessTheImageAsync(IRepository<FileEntity, FileId> fileRepository, WallpaperFileRequest request, CancellationToken cancellationToken)
        {
            Processed.Add(request);

            return Task.FromResult(ProcessResult);
        }
    }

    private sealed class FakeTagsProcessor : ITagsProcessor
    {
        public Exceptional<IReadOnlyList<Tag>> FetchResult { get; set; } = Exceptional.Success<IReadOnlyList<Tag>>([]);

        public List<(FileId FileId, IReadOnlyList<Tag> Tags)> Linked { get; } = [];

        public Task<Exceptional<IReadOnlyList<Tag>>> FetchTagsAsync(string wallpaperId, HttpClient client, IProgress<string> progress, CancellationToken cancellationToken) =>
            Task.FromResult(FetchResult);

        public Task<Exceptional<Unit>> LinkTagsAsync(FileId fileId, IReadOnlyList<Tag> tags, CancellationToken cancellationToken)
        {
            Linked.Add((fileId, tags));

            return Task.FromResult<Exceptional<Unit>>(Unit.Instance);
        }
    }

    private sealed class CapturingProgress : IProgress<string>
    {
        public List<string> Messages { get; } = [];

        public void Report(string value) => Messages.Add(value);
    }
}
