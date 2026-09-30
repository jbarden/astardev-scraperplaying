using System.Diagnostics.CodeAnalysis;
using System.Net;
using AStarDev.ControlDb.FileDetail;
using AStarDev.ScraperPlaying.Scraping.WallhavenResponses.SearchResponse;
using AStarDev.ScraperPlaying.WallpaperIngestion;
using Testably.Abstractions.Testing;

namespace AStarDev.ScraperPlaying.TestsUnit.WallpaperIngestion;

public sealed class GivenAnImageDownloader
{
    private readonly MockFileSystem fileSystem = new();
    private readonly ImageDownloader downloader;

    public GivenAnImageDownloader() => downloader = new(fileSystem);

    [Fact]
    public async Task when_downloading_an_image_succeeds_then_it_is_written_to_the_directory_and_the_saved_path_and_progress_are_reported()
    {
        var imageBytes = new byte[] { 1, 2, 3, 4, 5 };
        using var client = CreateClient(_ => new HttpResponseMessage(HttpStatusCode.OK) { Content = new ByteArrayContent(imageBytes) });
        var progress = new CapturingProgress();
        var directory = fileSystem.Path.Combine("root-directory", "top-wallpapers");
        var expectedPath = fileSystem.Path.Combine(directory, "wallpaper-3.jpg");
        var wallpaper = CreateWallpaper(id: "wallpaper-3", path: "https://example.test/image.jpg");

        var savedPath = await downloader.DownloadAsync(new WallpaperFileRequest(wallpaper, directory, NameFor(wallpaper, ".jpg"), "Cars"), progress, client, CancellationToken.None);

        savedPath.ShouldBe(expectedPath);
        (await fileSystem.File.ReadAllBytesAsync(expectedPath, TestContext.Current.CancellationToken)).ShouldBe(imageBytes);
        progress.Messages.ShouldContain("Downloading image for wallpaper wallpaper-3 from https://example.test/image.jpg");
    }

    [Fact]
    public async Task when_downloading_a_non_jpg_image_then_it_is_written_with_the_supplied_extension()
    {
        using var client = CreateClient(_ => new HttpResponseMessage(HttpStatusCode.OK) { Content = new ByteArrayContent([9, 9, 9]) });
        var directory = fileSystem.Path.Combine("root-directory", "top-wallpapers");
        var wallpaper = CreateWallpaper(id: "wallpaper-8", path: "https://example.test/image.png");

        _ = await downloader.DownloadAsync(new WallpaperFileRequest(wallpaper, directory, NameFor(wallpaper, ".png"), "Top Wallpapers"), new CapturingProgress(), client, CancellationToken.None);

        (fileSystem.File.Exists(fileSystem.Path.Combine(directory, "wallpaper-8.png")), fileSystem.File.Exists(fileSystem.Path.Combine(directory, "wallpaper-8.jpg"))).ShouldBe((true, false));
    }

    [Fact]
    public async Task when_the_request_has_a_prefixed_file_name_then_the_file_is_saved_under_it()
    {
        using var client = CreateClient(_ => new HttpResponseMessage(HttpStatusCode.OK) { Content = new ByteArrayContent([1, 2, 3]) });
        var wallpaper = CreateWallpaper(id: "abc123", path: "https://example.test/full/abc123.jpg");
        var request = new WallpaperFileRequest(wallpaper, "some-directory", new FileName("Max_Verstappen_abc123.jpg"), "Top Wallpapers");

        _ = await downloader.DownloadAsync(request, new CapturingProgress(), client, CancellationToken.None);

        fileSystem.File.Exists(fileSystem.Path.Combine("some-directory", "Max_Verstappen_abc123.jpg")).ShouldBeTrue();
    }

    [Fact]
    public async Task when_the_target_directory_does_not_exist_yet_then_it_is_created_before_writing_the_file()
    {
        using var client = CreateClient(_ => new HttpResponseMessage(HttpStatusCode.OK) { Content = new ByteArrayContent([1, 2, 3]) });
        var directory = fileSystem.Path.Combine("root-directory", "my-category");
        fileSystem.Directory.Exists(directory).ShouldBeFalse();
        var wallpaper = CreateWallpaper(id: "wallpaper-6", path: "https://example.test/image.jpg");

        _ = await downloader.DownloadAsync(new WallpaperFileRequest(wallpaper, directory, NameFor(wallpaper, ".jpg"), "Top Wallpapers"), new CapturingProgress(), client, CancellationToken.None);

        (fileSystem.Directory.Exists(directory), fileSystem.File.Exists(fileSystem.Path.Combine(directory, "wallpaper-6.jpg"))).ShouldBe((true, true));
    }

    [Fact]
    public async Task when_downloading_an_image_receives_a_non_success_status_then_it_throws_and_writes_no_file()
    {
        using var client = CreateClient(_ => new HttpResponseMessage(HttpStatusCode.NotFound));
        var directory = fileSystem.Path.Combine("root-directory", "top-wallpapers");
        var wallpaper = CreateWallpaper(id: "wallpaper-4", path: "https://example.test/image.jpg");

        await Should.ThrowAsync<HttpRequestException>(
            () => downloader.DownloadAsync(new WallpaperFileRequest(wallpaper, directory, NameFor(wallpaper, ".jpg"), "Top Wallpapers"), new CapturingProgress(), client, CancellationToken.None));

        fileSystem.File.Exists(fileSystem.Path.Combine(directory, "wallpaper-4.jpg")).ShouldBeFalse();
    }

    [Fact]
    public async Task when_reading_the_image_fails_part_way_then_no_partial_file_is_left_behind()
    {
        using var client = CreateClient(_ => new HttpResponseMessage(HttpStatusCode.OK) { Content = new StreamContent(new FailingStream()) });
        var directory = fileSystem.Path.Combine("root-directory", "top-wallpapers");
        var wallpaper = CreateWallpaper(id: "wallpaper-9", path: "https://example.test/image.jpg");

        await Should.ThrowAsync<IOException>(
            () => downloader.DownloadAsync(new WallpaperFileRequest(wallpaper, directory, NameFor(wallpaper, ".jpg"), "Top Wallpapers"), new CapturingProgress(), client, CancellationToken.None));

        fileSystem.Directory.GetFiles(directory).ShouldBeEmpty();
    }

    [Fact]
    public async Task when_reading_the_image_fails_part_way_then_an_existing_file_of_the_same_name_is_left_untouched()
    {
        var directory = fileSystem.Path.Combine("root-directory", "top-wallpapers");
        var existingPath = fileSystem.Path.Combine(directory, "wallpaper-10.jpg");
        fileSystem.Directory.CreateDirectory(directory);
        await fileSystem.File.WriteAllBytesAsync(existingPath, [7, 7, 7], TestContext.Current.CancellationToken);
        using var client = CreateClient(_ => new HttpResponseMessage(HttpStatusCode.OK) { Content = new StreamContent(new FailingStream()) });
        var wallpaper = CreateWallpaper(id: "wallpaper-10", path: "https://example.test/image.jpg");

        await Should.ThrowAsync<IOException>(
            () => downloader.DownloadAsync(new WallpaperFileRequest(wallpaper, directory, NameFor(wallpaper, ".jpg"), "Top Wallpapers"), new CapturingProgress(), client, CancellationToken.None));

        (await fileSystem.File.ReadAllBytesAsync(existingPath, TestContext.Current.CancellationToken)).ShouldBe([7, 7, 7]);
    }

    [Fact]
    public async Task when_the_download_succeeds_then_only_the_final_file_is_left_and_it_replaces_an_existing_one()
    {
        var directory = fileSystem.Path.Combine("root-directory", "top-wallpapers");
        var finalPath = fileSystem.Path.Combine(directory, "wallpaper-11.jpg");
        fileSystem.Directory.CreateDirectory(directory);
        await fileSystem.File.WriteAllBytesAsync(finalPath, [7, 7, 7], TestContext.Current.CancellationToken);
        using var client = CreateClient(_ => new HttpResponseMessage(HttpStatusCode.OK) { Content = new ByteArrayContent([1, 2]) });
        var wallpaper = CreateWallpaper(id: "wallpaper-11", path: "https://example.test/image.jpg");

        _ = await downloader.DownloadAsync(new WallpaperFileRequest(wallpaper, directory, NameFor(wallpaper, ".jpg"), "Top Wallpapers"), new CapturingProgress(), client, CancellationToken.None);

        (fileSystem.Directory.GetFiles(directory).Length, Convert.ToHexString(await fileSystem.File.ReadAllBytesAsync(finalPath, TestContext.Current.CancellationToken))).ShouldBe((1, "0102"));
    }

    private static FileName NameFor(Data wallpaper, string extension) => new($"{wallpaper.Id}{extension}");

    private static Data CreateWallpaper(string id, string path = "")
        => new(id, 0, 0, 0, "", path);

    [SuppressMessage("Reliability", "CA2000:Dispose objects before losing scope", Justification = "HttpClient owns and disposes the handler.")]
    private static HttpClient CreateClient(Func<HttpRequestMessage, HttpResponseMessage> responder)
        => new(new StubHttpMessageHandler(responder));

    private sealed class FailingStream : Stream
    {
        public override bool CanRead => true;

        public override bool CanSeek => false;

        public override bool CanWrite => false;

        public override long Length => throw new NotSupportedException();

        public override long Position
        {
            get => throw new NotSupportedException();
            set => throw new NotSupportedException();
        }

        public override void Flush()
        {
        }

        public override int Read(byte[] buffer, int offset, int count) => throw new IOException("connection reset");

        public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken) => throw new IOException("connection reset");

        public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default) => throw new IOException("connection reset");

        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();

        public override void SetLength(long value) => throw new NotSupportedException();

        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
    }

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
