using AStarDev.ControlDb;
using AStarDev.ControlDb.FileDetail;
using AStarDev.FunctionalParadigm;
using AStarDev.ScraperPlaying.Home;
using AStarDev.ScraperPlaying.SearchAPI.SearchResponse;

namespace AStarDev.ScraperPlaying.TestsUnit;

public sealed class GivenAWallpaperIngestionService
{
    private readonly IRepository<FileEntity, FileId> fileRepository = Substitute.For<IRepository<FileEntity, FileId>>();
    private readonly IFilesQuery filesQuery = Substitute.For<IFilesQuery>();
    private readonly IImageProcessor imageProcessor = Substitute.For<IImageProcessor>();
    private readonly ITagsProcessor tagsProcessor = Substitute.For<ITagsProcessor>();
    private readonly CapturingProgress progress = new();
    private readonly WallpaperIngestionService service;

    public GivenAWallpaperIngestionService()
    {
        tagsProcessor.FetchAndLinkTagsAsync(Arg.Any<string>(), Arg.Any<FileId>(), Arg.Any<HttpClient>(), Arg.Any<IProgress<string>>(), Arg.Any<CancellationToken>())
            .Returns((Exceptional<UnitFp>)UnitFp.Instance);
        service = new(filesQuery, imageProcessor, tagsProcessor, () => TimeSpan.FromMilliseconds(1));
    }

    [Fact]
    public async Task when_a_wallpaper_already_exists_then_it_is_skipped_and_not_downloaded()
    {
        var wallpaper = CreateWallpaper("existing-wallpaper");
        filesQuery.CheckExistsByNameAsync(Arg.Any<FileName>(), Arg.Any<CancellationToken>()).Returns((Exceptional<bool>)true);

        await Ingest(wallpaper);

        progress.Messages.ShouldContain("The file details already exist for wallpaper existing-wallpaper - no need to fetch again.");
        progress.Messages.ShouldNotContain(message => message.Contains("Downloaded image data"));
        await imageProcessor.DidNotReceive().DownloadImageAsync(Arg.Any<WallpaperFileRequest>(), Arg.Any<IProgress<string>>(), Arg.Any<HttpClient>(), Arg.Any<CancellationToken>());
        await tagsProcessor.DidNotReceive().FetchAndLinkTagsAsync(Arg.Any<string>(), Arg.Any<FileId>(), Arg.Any<HttpClient>(), Arg.Any<IProgress<string>>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task when_a_wallpaper_is_new_then_it_is_downloaded_and_processed()
    {
        var wallpaper = CreateWallpaper("new-wallpaper");
        filesQuery.CheckExistsByNameAsync(Arg.Any<FileName>(), Arg.Any<CancellationToken>()).Returns((Exceptional<bool>)false);
        imageProcessor.DownloadImageAsync(Arg.Any<WallpaperFileRequest>(), Arg.Any<IProgress<string>>(), Arg.Any<HttpClient>(), Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);
        var fileEntity = new FileEntity { FileName = new("new-wallpaper"), DirectoryName = new(""), FileHandle = new(""), FileSize = 0 };
        imageProcessor.ProcessTheImageAsync(Arg.Any<IRepository<FileEntity, FileId>>(), Arg.Any<WallpaperFileRequest>(), Arg.Any<CancellationToken>()).Returns((Exceptional<FileEntity>)fileEntity);

        await Ingest(wallpaper);

        progress.Messages.ShouldContain("No existing file found for wallpaper new-wallpaper.");
        progress.Messages.ShouldContain("Downloaded image data for wallpaper new-wallpaper");
        progress.Messages.ShouldNotContain(message => message.Contains("Failed"));
        await tagsProcessor.Received(1).FetchAndLinkTagsAsync("new-wallpaper", fileEntity.Id, Arg.Any<HttpClient>(), progress, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task when_a_wallpaper_has_a_non_jpg_extension_then_the_real_extension_is_used_for_the_existence_check_and_download()
    {
        var wallpaper = CreateWallpaper("png-wallpaper", path: "https://example.test/full/png-wallpaper.png");
        filesQuery.CheckExistsByNameAsync(Arg.Any<FileName>(), Arg.Any<CancellationToken>()).Returns((Exceptional<bool>)false);
        imageProcessor.DownloadImageAsync(Arg.Any<WallpaperFileRequest>(), Arg.Any<IProgress<string>>(), Arg.Any<HttpClient>(), Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);
        var fileEntity = new FileEntity { FileName = new("png-wallpaper.png"), DirectoryName = new(""), FileHandle = new(""), FileSize = 0 };
        imageProcessor.ProcessTheImageAsync(Arg.Any<IRepository<FileEntity, FileId>>(), Arg.Any<WallpaperFileRequest>(), Arg.Any<CancellationToken>()).Returns((Exceptional<FileEntity>)fileEntity);

        await Ingest(wallpaper, directory: "resolved-directory");

        await filesQuery.Received(1).CheckExistsByNameAsync(Arg.Is<FileName>(name => name.Value == "png-wallpaper.png"), Arg.Any<CancellationToken>());
        await imageProcessor.Received(1).DownloadImageAsync(new WallpaperFileRequest(wallpaper, "resolved-directory", ".png"), progress, Arg.Any<HttpClient>(), Arg.Any<CancellationToken>());
        await imageProcessor.Received(1).ProcessTheImageAsync(Arg.Any<IRepository<FileEntity, FileId>>(), new WallpaperFileRequest(wallpaper, "resolved-directory", ".png"), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task when_a_wallpaper_source_path_has_no_extension_then_the_jpg_fallback_is_used_for_the_existence_check_and_download()
    {
        var wallpaper = CreateWallpaper("extensionless-wallpaper", path: "https://example.test/full/extensionless-wallpaper");
        filesQuery.CheckExistsByNameAsync(Arg.Any<FileName>(), Arg.Any<CancellationToken>()).Returns((Exceptional<bool>)false);
        imageProcessor.DownloadImageAsync(Arg.Any<WallpaperFileRequest>(), Arg.Any<IProgress<string>>(), Arg.Any<HttpClient>(), Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);
        var fileEntity = new FileEntity { FileName = new("extensionless-wallpaper.jpg"), DirectoryName = new(""), FileHandle = new(""), FileSize = 0 };
        imageProcessor.ProcessTheImageAsync(Arg.Any<IRepository<FileEntity, FileId>>(), Arg.Any<WallpaperFileRequest>(), Arg.Any<CancellationToken>()).Returns((Exceptional<FileEntity>)fileEntity);

        await Ingest(wallpaper, directory: "resolved-directory");

        await filesQuery.Received(1).CheckExistsByNameAsync(Arg.Is<FileName>(name => name.Value == "extensionless-wallpaper.jpg"), Arg.Any<CancellationToken>());
        await imageProcessor.Received(1).DownloadImageAsync(new WallpaperFileRequest(wallpaper, "resolved-directory", ".jpg"), progress, Arg.Any<HttpClient>(), Arg.Any<CancellationToken>());
        await imageProcessor.Received(1).ProcessTheImageAsync(Arg.Any<IRepository<FileEntity, FileId>>(), new WallpaperFileRequest(wallpaper, "resolved-directory", ".jpg"), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task when_downloading_a_new_wallpaper_fails_then_the_failure_is_reported_and_not_thrown()
    {
        var wallpaper = CreateWallpaper("failing-download");
        filesQuery.CheckExistsByNameAsync(Arg.Any<FileName>(), Arg.Any<CancellationToken>()).Returns((Exceptional<bool>)false);
        imageProcessor.DownloadImageAsync(Arg.Any<WallpaperFileRequest>(), Arg.Any<IProgress<string>>(), Arg.Any<HttpClient>(), Arg.Any<CancellationToken>())
            .Returns(_ => throw new HttpRequestException("download failed"));

        await Ingest(wallpaper);

        progress.Messages.ShouldContain("Failed to process image for wallpaper failing-download: download failed");
        await tagsProcessor.DidNotReceive().FetchAndLinkTagsAsync(Arg.Any<string>(), Arg.Any<FileId>(), Arg.Any<HttpClient>(), Arg.Any<IProgress<string>>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task when_processing_a_new_wallpaper_fails_then_the_failure_is_reported_and_not_thrown()
    {
        var wallpaper = CreateWallpaper("failing-process");
        filesQuery.CheckExistsByNameAsync(Arg.Any<FileName>(), Arg.Any<CancellationToken>()).Returns((Exceptional<bool>)false);
        imageProcessor.DownloadImageAsync(Arg.Any<WallpaperFileRequest>(), Arg.Any<IProgress<string>>(), Arg.Any<HttpClient>(), Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);
        var exception = new InvalidOperationException("process failed");
        imageProcessor.ProcessTheImageAsync(Arg.Any<IRepository<FileEntity, FileId>>(), Arg.Any<WallpaperFileRequest>(), Arg.Any<CancellationToken>()).Returns((Exceptional<FileEntity>)exception);

        await Ingest(wallpaper);

        progress.Messages.ShouldContain("Failed to process image for wallpaper failing-process: process failed");
        await tagsProcessor.DidNotReceive().FetchAndLinkTagsAsync(Arg.Any<string>(), Arg.Any<FileId>(), Arg.Any<HttpClient>(), Arg.Any<IProgress<string>>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task when_checking_whether_a_wallpaper_already_exists_fails_then_the_failure_is_reported_and_the_wallpaper_is_skipped()
    {
        var wallpaper = CreateWallpaper("check-fails");
        var exception = new InvalidOperationException("query failed");
        filesQuery.CheckExistsByNameAsync(Arg.Any<FileName>(), Arg.Any<CancellationToken>()).Returns((Exceptional<bool>)exception);

        await Ingest(wallpaper);

        progress.Messages.ShouldContain("Failed to check whether the file details already exist for wallpaper check-fails: query failed");
        await imageProcessor.DidNotReceive().DownloadImageAsync(Arg.Any<WallpaperFileRequest>(), Arg.Any<IProgress<string>>(), Arg.Any<HttpClient>(), Arg.Any<CancellationToken>());
        await tagsProcessor.DidNotReceive().FetchAndLinkTagsAsync(Arg.Any<string>(), Arg.Any<FileId>(), Arg.Any<HttpClient>(), Arg.Any<IProgress<string>>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task when_fetching_tags_for_a_new_wallpaper_fails_then_the_failure_is_reported_distinctly_and_not_thrown()
    {
        var wallpaper = CreateWallpaper("failing-tags");
        filesQuery.CheckExistsByNameAsync(Arg.Any<FileName>(), Arg.Any<CancellationToken>()).Returns((Exceptional<bool>)false);
        imageProcessor.DownloadImageAsync(Arg.Any<WallpaperFileRequest>(), Arg.Any<IProgress<string>>(), Arg.Any<HttpClient>(), Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);
        var fileEntity = new FileEntity { FileName = new("failing-tags"), DirectoryName = new(""), FileHandle = new(""), FileSize = 0 };
        imageProcessor.ProcessTheImageAsync(Arg.Any<IRepository<FileEntity, FileId>>(), Arg.Any<WallpaperFileRequest>(), Arg.Any<CancellationToken>()).Returns((Exceptional<FileEntity>)fileEntity);
        var exception = new InvalidOperationException("tag fetch failed");
        tagsProcessor.FetchAndLinkTagsAsync(Arg.Any<string>(), Arg.Any<FileId>(), Arg.Any<HttpClient>(), Arg.Any<IProgress<string>>(), Arg.Any<CancellationToken>())
            .Returns((Exceptional<UnitFp>)exception);

        await Ingest(wallpaper);

        progress.Messages.ShouldContain("Failed to fetch tags for wallpaper failing-tags: tag fetch failed");
        progress.Messages.ShouldNotContain(message => message.Contains("Failed to process image"));
    }

    private Task Ingest(Data wallpaper, string directory = "some-directory")
        => service.IngestAsync(wallpaper, new WallpaperIngestionContext(directory, new HttpClient(), fileRepository), progress, CancellationToken.None);

    private static Data CreateWallpaper(string id, string path = "")
        => new(id, "", "", 0, 0, "", "", "", 0, 0, "", "", 0, "", "", [], path, new Thumbs("", "", ""));

    private sealed class CapturingProgress : IProgress<string>
    {
        public List<string> Messages { get; } = [];

        public void Report(string value) => Messages.Add(value);
    }
}
