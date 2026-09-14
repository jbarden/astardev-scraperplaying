using System.Net;
using AStarDev.ControlDb;
using AStarDev.ControlDb.FileDetail;
using AStarDev.FunctionalParadigm;
using AStarDev.ScraperPlaying.Home;
using AStarDev.ScraperPlaying.SearchAPI.SearchResponse;
using Testably.Abstractions.Testing;

namespace AStarDev.ScraperPlaying.TestsUnit;

public sealed class GivenAnImageProcessor
{
    private static readonly DateTimeOffset now = new(2026, 1, 2, 3, 4, 5, TimeSpan.Zero);
    private readonly IRepository<FileEntity, FileId> fileRepository = Substitute.For<IRepository<FileEntity, FileId>>();
    private readonly IImageDownloadNotifier imageDownloadNotifier = Substitute.For<IImageDownloadNotifier>();
    private readonly MockFileSystem fileSystem = new();
    private readonly ImageProcessor processor;

    public GivenAnImageProcessor()
    {
        processor = new(() => now, fileSystem, () => TimeSpan.FromMilliseconds(1), imageDownloadNotifier);
    }

    [Fact]
    public async Task when_processing_a_wallpaper_then_a_matching_file_entity_is_added_and_returned()
    {
        fileRepository.Add(Arg.Any<FileEntity>()).Returns(call => (Exceptional<FileEntity>)call.Arg<FileEntity>());
        var wallpaper = CreateWallpaper(id: "wallpaper-1", fileSize: 1234, fileType: "image/jpeg", dimensionX: 1920, dimensionY: 1080, path: "https://example.test/full/wallpaper-1.jpg");

        var result = await processor.ProcessTheImageAsync(fileRepository, new WallpaperFileRequest(wallpaper, "some-directory", ".jpg"), CancellationToken.None);

        var addedEntity = result.Match(entity => entity, _ => (FileEntity?)null);
        addedEntity.ShouldNotBeNull();
        addedEntity.FileName.Value.ShouldBe("wallpaper-1.jpg");
        addedEntity.FileHandle.Value.ShouldBe("wallpaper-1");
        addedEntity.FileSize.ShouldBe(1234);
        addedEntity.FileType.ShouldBe("image/jpeg");
        addedEntity.IsImage.ShouldBeTrue();
        addedEntity.ImageDetail!.Width.ShouldBe(1920);
        addedEntity.ImageDetail!.Height.ShouldBe(1080);
        addedEntity.FileAccessDetail.DetailsLastUpdated.ShouldBe(now.UtcDateTime);
    }

    [Fact]
    public async Task when_a_non_jpg_extension_is_supplied_then_the_file_name_uses_that_extension()
    {
        fileRepository.Add(Arg.Any<FileEntity>()).Returns(call => (Exceptional<FileEntity>)call.Arg<FileEntity>());
        var wallpaper = CreateWallpaper(id: "wallpaper-7", path: "https://example.test/full/wallpaper-7.png");

        var result = await processor.ProcessTheImageAsync(fileRepository, new WallpaperFileRequest(wallpaper, "some-directory", ".png"), CancellationToken.None);

        var addedEntity = result.Match(entity => entity, _ => (FileEntity?)null);
        addedEntity.ShouldNotBeNull();
        addedEntity.FileName.Value.ShouldBe("wallpaper-7.png");
    }

    [Fact]
    public async Task when_a_directory_is_supplied_then_it_is_used_as_the_directory_name()
    {
        fileRepository.Add(Arg.Any<FileEntity>()).Returns(call => (Exceptional<FileEntity>)call.Arg<FileEntity>());
        var wallpaper = CreateWallpaper(id: "wallpaper-top");

        var result = await processor.ProcessTheImageAsync(fileRepository, new WallpaperFileRequest(wallpaper, "root-directory/top-wallpapers", ".jpg"), CancellationToken.None);

        var addedEntity = result.Match(entity => entity, _ => (FileEntity?)null);
        addedEntity.ShouldNotBeNull();
        addedEntity.DirectoryName.Value.ShouldBe("root-directory/top-wallpapers");
    }

    [Fact]
    public async Task when_the_wallpaper_path_does_not_have_an_image_extension_then_is_image_is_false()
    {
        fileRepository.Add(Arg.Any<FileEntity>()).Returns(call => (Exceptional<FileEntity>)call.Arg<FileEntity>());
        var wallpaper = CreateWallpaper(id: "wallpaper-5", path: "https://example.test/full/wallpaper-5.txt");

        var result = await processor.ProcessTheImageAsync(fileRepository, new WallpaperFileRequest(wallpaper, "some-directory", ".txt"), CancellationToken.None);

        var addedEntity = result.Match(entity => entity, _ => (FileEntity?)null);
        addedEntity.ShouldNotBeNull();
        addedEntity.IsImage.ShouldBeFalse();
    }

    [Fact]
    public async Task when_adding_the_file_entity_fails_then_the_failure_is_returned_not_swallowed()
    {
        var exception = new InvalidOperationException("add failed");
        fileRepository.Add(Arg.Any<FileEntity>()).Returns((Exceptional<FileEntity>)exception);
        var wallpaper = CreateWallpaper(id: "wallpaper-2");

        var result = await processor.ProcessTheImageAsync(fileRepository, new WallpaperFileRequest(wallpaper, "some-directory", ".jpg"), CancellationToken.None);

        var capturedException = result.Match(_ => (Exception?)null, ex => ex);
        capturedException.ShouldBeSameAs(exception);
    }

    [Fact]
    public async Task when_downloading_an_image_succeeds_then_it_is_written_to_the_resolved_directory_with_the_supplied_extension_and_progress_is_reported()
    {
        var imageBytes = new byte[] { 1, 2, 3, 4, 5 };
        using var client = CreateClient(_ => new HttpResponseMessage(HttpStatusCode.OK) { Content = new ByteArrayContent(imageBytes) });
        var progress = new CapturingProgress();
        var directory = fileSystem.Path.Combine("root-directory", "top-wallpapers");
        var expectedPath = fileSystem.Path.Combine(directory, "wallpaper-3.jpg");
        var wallpaper = CreateWallpaper(id: "wallpaper-3", path: "https://example.test/image.jpg");

        await processor.DownloadImageAsync(new WallpaperFileRequest(wallpaper, directory, ".jpg"), progress, client, CancellationToken.None);

        fileSystem.File.Exists(expectedPath).ShouldBeTrue();
        fileSystem.File.ReadAllBytes(expectedPath).ShouldBe(imageBytes);
        progress.Messages.ShouldContain("Downloading image for wallpaper wallpaper-3 from https://example.test/image.jpg");
        imageDownloadNotifier.Received(1).NotifyImageDownloaded(expectedPath);
    }

    [Fact]
    public async Task when_downloading_a_non_jpg_image_then_it_is_written_with_the_supplied_extension()
    {
        var imageBytes = new byte[] { 9, 9, 9 };
        using var client = CreateClient(_ => new HttpResponseMessage(HttpStatusCode.OK) { Content = new ByteArrayContent(imageBytes) });
        var progress = new CapturingProgress();
        var directory = fileSystem.Path.Combine("root-directory", "top-wallpapers");
        var expectedPath = fileSystem.Path.Combine(directory, "wallpaper-8.png");
        var wallpaper = CreateWallpaper(id: "wallpaper-8", path: "https://example.test/image.png");

        await processor.DownloadImageAsync(new WallpaperFileRequest(wallpaper, directory, ".png"), progress, client, CancellationToken.None);

        fileSystem.File.Exists(expectedPath).ShouldBeTrue();
        fileSystem.File.Exists(fileSystem.Path.Combine(directory, "wallpaper-8.jpg")).ShouldBeFalse();
        imageDownloadNotifier.Received(1).NotifyImageDownloaded(expectedPath);
    }

    [Fact]
    public async Task when_the_target_directory_does_not_exist_yet_then_it_is_created_before_writing_the_file()
    {
        var imageBytes = new byte[] { 1, 2, 3 };
        using var client = CreateClient(_ => new HttpResponseMessage(HttpStatusCode.OK) { Content = new ByteArrayContent(imageBytes) });
        var progress = new CapturingProgress();
        var directory = fileSystem.Path.Combine("root-directory", "my-category");

        fileSystem.Directory.Exists(directory).ShouldBeFalse();
        var wallpaper = CreateWallpaper(id: "wallpaper-6", path: "https://example.test/image.jpg");

        await processor.DownloadImageAsync(new WallpaperFileRequest(wallpaper, directory, ".jpg"), progress, client, CancellationToken.None);

        fileSystem.Directory.Exists(directory).ShouldBeTrue();
        fileSystem.File.Exists(fileSystem.Path.Combine(directory, "wallpaper-6.jpg")).ShouldBeTrue();
        imageDownloadNotifier.Received(1).NotifyImageDownloaded(fileSystem.Path.Combine(directory, "wallpaper-6.jpg"));
    }

    [Fact]
    public async Task when_downloading_an_image_receives_a_non_success_status_then_it_throws_and_writes_no_file()
    {
        using var client = CreateClient(_ => new HttpResponseMessage(HttpStatusCode.NotFound));
        var progress = new CapturingProgress();
        var directory = fileSystem.Path.Combine("root-directory", "top-wallpapers");
        var wallpaper = CreateWallpaper(id: "wallpaper-4", path: "https://example.test/image.jpg");

        await Should.ThrowAsync<HttpRequestException>(
            () => processor.DownloadImageAsync(new WallpaperFileRequest(wallpaper, directory, ".jpg"), progress, client, CancellationToken.None));

        fileSystem.File.Exists(fileSystem.Path.Combine(directory, "wallpaper-4.jpg")).ShouldBeFalse();
        imageDownloadNotifier.DidNotReceive().NotifyImageDownloaded(Arg.Any<string>());
    }

    private static Data CreateWallpaper(string id, int fileSize = 0, string fileType = "", int dimensionX = 0, int dimensionY = 0, string path = "")
        => new(id, "", "", 0, 0, "", "", "", dimensionX, dimensionY, "", "", fileSize, fileType, "", [], path, new Thumbs("", "", ""));

    private static HttpClient CreateClient(Func<HttpRequestMessage, HttpResponseMessage> responder)
        => new(new StubHttpMessageHandler(responder));

    private sealed class CapturingProgress : IProgress<string>
    {
        public List<string> Messages { get; } = [];

        public void Report(string value) => Messages.Add(value);
    }

    private sealed class StubHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> responder) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            => Task.FromResult(responder(request));
    }
}
