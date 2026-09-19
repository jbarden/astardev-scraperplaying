using AStarDev.ControlDb;
using AStarDev.ControlDb.FileDetail;
using AStarDev.FunctionalParadigm;
using AStarDev.ScraperPlaying.Scraping;
using AStarDev.ScraperPlaying.WallpaperIngestion;
using Microsoft.Playwright;

namespace AStarDev.ScraperPlaying.TestsUnit.WallpaperIngestion;

public sealed class GivenAWallpaperDetailImageProcessor
{
    private readonly IWallpaperImageDownloader imageDownloader = Substitute.For<IWallpaperImageDownloader>();
    private readonly IWallpaperFileRecorder fileRecorder = Substitute.For<IWallpaperFileRecorder>();
    private readonly IImageDownloadNotifier imageDownloadNotifier = Substitute.For<IImageDownloadNotifier>();
    private readonly IRepository<FileEntity, FileId> fileRepository = Substitute.For<IRepository<FileEntity, FileId>>();
    private readonly IPage page = Substitute.For<IPage>();
    private readonly CapturingProgress progress = new();
    private readonly WallpaperDetailImageProcessor processor;
    private readonly ImageDownloadRequest request;
    private readonly FileEntity recordedFile = new()
    {
        Id = FileId.Create(),
        FileName = new FileName("wallpaper-1.png"),
        DirectoryName = DirectoryName.Create("some-directory"),
        FileHandle = FileHandle.Create("wallpaper-1"),
        FileSize = 5
    };

    public GivenAWallpaperDetailImageProcessor()
    {
        request = new ImageDownloadRequest(new WallpaperDetail("wallpaper-1", "https://example.test/full/wallpaper-1.png", 1920, 1080, []), page, "some-directory", "Top Wallpapers", fileRepository);
        imageDownloader.DownloadAsync(request.Detail, page, "some-directory", progress, Arg.Any<CancellationToken>())
            .Returns((Exceptional<DownloadedWallpaperImage>)new DownloadedWallpaperImage(new FileName("wallpaper-1.png"), "some-directory/wallpaper-1.png", 5, "image/png"));
        fileRecorder.Record(request.Detail, Arg.Any<DownloadedWallpaperImage>(), "some-directory", fileRepository).Returns((Exceptional<FileEntity>)recordedFile);
        processor = new(imageDownloader, fileRecorder, imageDownloadNotifier);
    }

    [Fact]
    public async Task when_the_image_is_downloaded_and_recorded_then_the_file_entity_is_returned_and_the_download_announced()
    {
        var result = await processor.DownloadAndRecordAsync(request, progress, TestContext.Current.CancellationToken);

        result.Match(file => file, ex => throw ex).ShouldBeSameAs(recordedFile);
        imageDownloadNotifier.Received(1).NotifyImageDownloaded(Arg.Is<WallpaperDownloadDetails>(details =>
            details.FilePath == "some-directory/wallpaper-1.png"
            && details.Name == "wallpaper-1"
            && details.CategoryLabel == "Top Wallpapers"
            && details.FileSizeBytes == 5
            && details.Width == 1920
            && details.Height == 1080));
    }

    [Fact]
    public async Task when_the_download_fails_then_the_failure_is_returned_and_nothing_is_recorded_or_announced()
    {
        var exception = new HttpRequestException("status 403");
        imageDownloader.DownloadAsync(request.Detail, page, "some-directory", progress, Arg.Any<CancellationToken>()).Returns((Exceptional<DownloadedWallpaperImage>)exception);

        var result = await processor.DownloadAndRecordAsync(request, progress, TestContext.Current.CancellationToken);

        result.Match(_ => (Exception?)null, ex => ex).ShouldBeSameAs(exception);
        fileRecorder.DidNotReceive().Record(Arg.Any<WallpaperDetail>(), Arg.Any<DownloadedWallpaperImage>(), Arg.Any<string>(), Arg.Any<IRepository<FileEntity, FileId>>());
        imageDownloadNotifier.DidNotReceive().NotifyImageDownloaded(Arg.Any<WallpaperDownloadDetails>());
    }

    [Fact]
    public async Task when_recording_fails_then_the_failure_is_returned_and_the_download_is_not_announced()
    {
        var exception = new InvalidOperationException("insert failed");
        fileRecorder.Record(request.Detail, Arg.Any<DownloadedWallpaperImage>(), "some-directory", fileRepository).Returns((Exceptional<FileEntity>)exception);

        var result = await processor.DownloadAndRecordAsync(request, progress, TestContext.Current.CancellationToken);

        result.Match(_ => (Exception?)null, ex => ex).ShouldBeSameAs(exception);
        imageDownloadNotifier.DidNotReceive().NotifyImageDownloaded(Arg.Any<WallpaperDownloadDetails>());
    }

    private sealed class CapturingProgress : IProgress<string>
    {
        public List<string> Messages { get; } = [];

        public void Report(string value) => Messages.Add(value);
    }
}
