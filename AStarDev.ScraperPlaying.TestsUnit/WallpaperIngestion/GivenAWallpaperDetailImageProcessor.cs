using AStarDev.ControlDb;
using AStarDev.ControlDb.FileDetail;
using AStarDev.FunctionalParadigm;
using AStarDev.ScraperPlaying.Scraping;
using AStarDev.ScraperPlaying.WallpaperIngestion;
using Microsoft.Playwright;
using Testably.Abstractions.Testing;

namespace AStarDev.ScraperPlaying.TestsUnit.WallpaperIngestion;

public sealed class GivenAWallpaperDetailImageProcessor
{
    private static readonly DateTimeOffset now = new(2026, 1, 2, 3, 4, 5, TimeSpan.Zero);
    private static readonly byte[] imageBytes = [1, 2, 3, 4, 5];
    private readonly IFilesQuery filesQuery = Substitute.For<IFilesQuery>();
    private readonly IImageDownloadNotifier imageDownloadNotifier = Substitute.For<IImageDownloadNotifier>();
    private readonly IRepository<FileEntity, FileId> fileRepository = Substitute.For<IRepository<FileEntity, FileId>>();
    private readonly IPage page = Substitute.For<IPage>();
    private readonly IAPIRequestContext requestContext = Substitute.For<IAPIRequestContext>();
    private readonly IAPIResponse response = Substitute.For<IAPIResponse>();
    private readonly MockFileSystem fileSystem = new();
    private readonly CapturingProgress progress = new();
    private readonly WallpaperDetailImageProcessor processor;

    public GivenAWallpaperDetailImageProcessor()
    {
        var browserContext = Substitute.For<IBrowserContext>();
        page.Context.Returns(browserContext);
        browserContext.APIRequest.Returns(requestContext);
        requestContext.GetAsync(Arg.Any<string>(), Arg.Any<APIRequestContextOptions?>()).Returns(response);
        response.Ok.Returns(true);
        response.Status.Returns(200);
        response.BodyAsync().Returns(imageBytes);
        response.Headers.Returns(new Dictionary<string, string> { ["content-type"] = "image/png" });
        filesQuery.CheckExistsByNameAsync(Arg.Any<FileName>(), Arg.Any<CancellationToken>()).Returns((Exceptional<bool>)false);
        fileRepository.Add(Arg.Any<FileEntity>()).Returns(call => (Exceptional<FileEntity>)call.Arg<FileEntity>());
        processor = new(filesQuery, fileSystem, () => now, imageDownloadNotifier);
    }

    [Fact]
    public async Task when_a_wallpaper_is_new_then_its_image_is_downloaded_with_the_browser_session_and_saved()
    {
        await Run(CreateDetail());

        await requestContext.Received(1).GetAsync("https://example.test/full/wallpaper-1.png", Arg.Any<APIRequestContextOptions?>());
        fileSystem.File.ReadAllBytes(fileSystem.Path.Combine("some-directory", "wallpaper-1.png")).ShouldBe(imageBytes);
    }

    [Fact]
    public async Task when_a_wallpaper_is_new_then_a_matching_file_entity_is_added_and_returned()
    {
        var result = await Run(CreateDetail());

        var entity = result.Match(option => option.Match(file => (FileEntity?)file, () => null), _ => null);
        entity.ShouldNotBeNull();
        entity.FileName.Value.ShouldBe("wallpaper-1.png");
        entity.FileHandle.Value.ShouldBe("wallpaper-1");
        entity.FileSize.ShouldBe(imageBytes.Length);
        entity.FileType.ShouldBe("image/png");
        entity.IsImage.ShouldBeTrue();
        entity.ImageDetail!.Width.ShouldBe(1920);
        entity.ImageDetail.Height.ShouldBe(1080);
        entity.FileAccessDetail.DetailsLastUpdated.ShouldBe(now.UtcDateTime);
        fileRepository.Received(1).Add(entity);
    }

    [Fact]
    public async Task when_a_wallpaper_is_downloaded_then_the_download_is_announced_and_progress_reported()
    {
        await Run(CreateDetail());

        imageDownloadNotifier.Received(1).NotifyImageDownloaded(Arg.Is<WallpaperDownloadDetails>(details =>
            details.FilePath == fileSystem.Path.Combine("some-directory", "wallpaper-1.png")
            && details.Name == "wallpaper-1"
            && details.CategoryLabel == "Top Wallpapers"
            && details.FileSizeBytes == imageBytes.Length
            && details.Width == 1920
            && details.Height == 1080));
        progress.Messages.ShouldContain("Downloading image for wallpaper wallpaper-1 from https://example.test/full/wallpaper-1.png");
    }

    [Fact]
    public async Task when_the_response_has_no_content_type_then_the_file_type_is_empty()
    {
        response.Headers.Returns(new Dictionary<string, string>());

        var result = await Run(CreateDetail());

        var entity = result.Match(option => option.Match(file => (FileEntity?)file, () => null), _ => null);
        entity.ShouldNotBeNull();
        entity.FileType.ShouldBe(string.Empty);
    }

    [Fact]
    public async Task when_the_image_url_has_no_extension_then_the_jpg_fallback_is_used()
    {
        var result = await Run(CreateDetail(imageUrl: "https://example.test/full/wallpaper-1"));

        var entity = result.Match(option => option.Match(file => (FileEntity?)file, () => null), _ => null);
        entity.ShouldNotBeNull();
        entity.FileName.Value.ShouldBe("wallpaper-1.jpg");
        await filesQuery.Received(1).CheckExistsByNameAsync(Arg.Is<FileName>(name => name.Value == "wallpaper-1.jpg"), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task when_the_wallpaper_already_exists_then_it_is_skipped_and_nothing_is_downloaded_or_saved()
    {
        filesQuery.CheckExistsByNameAsync(Arg.Any<FileName>(), Arg.Any<CancellationToken>()).Returns((Exceptional<bool>)true);

        var result = await Run(CreateDetail());

        result.Match(option => option.Match(_ => false, () => true), ex => throw ex).ShouldBeTrue();
        await requestContext.DidNotReceive().GetAsync(Arg.Any<string>(), Arg.Any<APIRequestContextOptions?>());
        fileRepository.DidNotReceive().Add(Arg.Any<FileEntity>());
        imageDownloadNotifier.DidNotReceive().NotifyImageDownloaded(Arg.Any<WallpaperDownloadDetails>());
        progress.Messages.ShouldContain("The file details already exist for wallpaper wallpaper-1 - no need to fetch again.");
    }

    [Fact]
    public async Task when_checking_for_an_existing_file_fails_then_the_failure_is_returned_and_nothing_is_downloaded()
    {
        var exception = new InvalidOperationException("db down");
        filesQuery.CheckExistsByNameAsync(Arg.Any<FileName>(), Arg.Any<CancellationToken>()).Returns((Exceptional<bool>)exception);

        var result = await Run(CreateDetail());

        result.Match(_ => (Exception?)null, ex => ex).ShouldBeSameAs(exception);
        await requestContext.DidNotReceive().GetAsync(Arg.Any<string>(), Arg.Any<APIRequestContextOptions?>());
    }

    [Fact]
    public async Task when_the_image_url_is_empty_then_a_failure_is_returned_and_nothing_is_downloaded()
    {
        var result = await Run(CreateDetail(imageUrl: string.Empty));

        result.Match(_ => (Exception?)null, ex => ex).ShouldBeOfType<InvalidOperationException>();
        await requestContext.DidNotReceive().GetAsync(Arg.Any<string>(), Arg.Any<APIRequestContextOptions?>());
        fileRepository.DidNotReceive().Add(Arg.Any<FileEntity>());
    }

    [Fact]
    public async Task when_the_download_receives_a_non_success_status_then_a_failure_is_returned_and_no_file_is_written()
    {
        response.Ok.Returns(false);
        response.Status.Returns(403);

        var result = await Run(CreateDetail());

        result.Match(_ => (Exception?)null, ex => ex).ShouldBeOfType<HttpRequestException>();
        fileSystem.File.Exists(fileSystem.Path.Combine("some-directory", "wallpaper-1.png")).ShouldBeFalse();
        fileRepository.DidNotReceive().Add(Arg.Any<FileEntity>());
        imageDownloadNotifier.DidNotReceive().NotifyImageDownloaded(Arg.Any<WallpaperDownloadDetails>());
    }

    [Fact]
    public async Task when_adding_the_file_entity_fails_then_the_failure_is_returned()
    {
        var exception = new InvalidOperationException("insert failed");
        fileRepository.Add(Arg.Any<FileEntity>()).Returns((Exceptional<FileEntity>)exception);

        var result = await Run(CreateDetail());

        result.Match(_ => (Exception?)null, ex => ex).ShouldBeSameAs(exception);
    }

    private Task<Exceptional<Option<FileEntity>>> Run(WallpaperDetail detail)
        => processor.DownloadAndRecordAsync(detail, page, "some-directory", "Top Wallpapers", fileRepository, progress, CancellationToken.None);

    private static WallpaperDetail CreateDetail(string imageUrl = "https://example.test/full/wallpaper-1.png")
        => new("wallpaper-1", imageUrl, 1920, 1080, []);

    private sealed class CapturingProgress : IProgress<string>
    {
        public List<string> Messages { get; } = [];

        public void Report(string value) => Messages.Add(value);
    }
}
