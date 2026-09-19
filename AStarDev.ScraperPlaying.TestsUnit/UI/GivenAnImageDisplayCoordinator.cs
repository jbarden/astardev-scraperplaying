using AStarDev.ScraperPlaying.UI;
using AStarDev.ScraperPlaying.WallpaperIngestion;

namespace AStarDev.ScraperPlaying.TestsUnit.UI;

public sealed class GivenAnImageDisplayCoordinator : IDisposable
{
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(1);
    private readonly IImageDownloadNotifier notifier = new ImageDownloadNotifier();
    private readonly IDownloadedImageDecoder decoder = Substitute.For<IDownloadedImageDecoder>();
    private readonly List<ImageDisplayCoordinator> coordinators = [];

    public void Dispose()
    {
        foreach (var coordinator in coordinators) coordinator.Dispose();
    }

    [Fact]
    public async Task when_a_notified_image_decodes_successfully_then_image_ready_is_raised_with_the_decoded_stream_and_details()
    {
        var decodedStream = new MemoryStream([1, 2, 3]);
        decoder.DecodeToPng("/some/path/wallpaper-1.jpg", Arg.Any<int>()).Returns(decodedStream);
        var coordinator = Create();
        var received = new TaskCompletionSource<WallpaperPreviewImage>(TaskCreationOptions.RunContinuationsAsynchronously);
        coordinator.ImageReady += (_, preview) => received.TrySetResult(preview);

        NotifyDownloaded();

        var preview = await received.Task.WaitAsync(Timeout, TestContext.Current.CancellationToken);
        preview.PngStream.ShouldBeSameAs(decodedStream);
        preview.Name.ShouldBe("wallpaper-1");
        preview.CategoryLabel.ShouldBe("Cars");
        preview.FileSizeBytes.ShouldBe(1234);
        preview.Width.ShouldBe(1920);
        preview.Height.ShouldBe(1080);
    }

    [Fact]
    public async Task when_a_notified_image_is_decoded_then_it_is_scaled_to_the_preview_size()
    {
        var requestedDimension = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
        decoder.DecodeToPng(Arg.Any<string>(), Arg.Any<int>()).Returns(call =>
        {
            requestedDimension.TrySetResult(call.ArgAt<int>(1));

            return new MemoryStream();
        });
        Create();

        NotifyDownloaded();

        (await requestedDimension.Task.WaitAsync(Timeout, TestContext.Current.CancellationToken)).ShouldBe(1280);
    }

    [Fact]
    public async Task when_a_notified_image_fails_to_decode_then_image_ready_is_not_raised_and_nothing_throws()
    {
        var decodeAttempted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        decoder.DecodeToPng(Arg.Any<string>(), Arg.Any<int>()).Returns<Stream>(_ =>
        {
            decodeAttempted.TrySetResult();

            throw new InvalidOperationException("bad image");
        });
        var coordinator = Create();
        var raised = false;
        coordinator.ImageReady += (_, _) => raised = true;

        Should.NotThrow(() => NotifyDownloaded());
        await decodeAttempted.Task.WaitAsync(Timeout, TestContext.Current.CancellationToken);
        await Task.Delay(50, TestContext.Current.CancellationToken);

        raised.ShouldBeFalse();
    }

    [Fact]
    public void when_the_preview_is_disabled_then_a_notified_image_is_not_decoded()
    {
        var coordinator = Create(isEnabled: false);
        var raised = false;
        coordinator.ImageReady += (_, _) => raised = true;

        NotifyDownloaded();

        decoder.ReceivedCalls().ShouldBeEmpty();
        raised.ShouldBeFalse();
    }

    [Fact]
    public async Task when_the_preview_is_disabled_and_then_re_enabled_then_images_are_decoded_again()
    {
        decoder.DecodeToPng(Arg.Any<string>(), Arg.Any<int>()).Returns(new MemoryStream([1]));
        var coordinator = Create(isEnabled: false);
        var received = new TaskCompletionSource<WallpaperPreviewImage>(TaskCreationOptions.RunContinuationsAsynchronously);
        coordinator.ImageReady += (_, preview) => received.TrySetResult(preview);
        coordinator.IsEnabled = true;

        NotifyDownloaded();

        (await received.Task.WaitAsync(Timeout, TestContext.Current.CancellationToken)).Name.ShouldBe("wallpaper-1");
    }

    [Fact]
    public async Task when_images_arrive_faster_than_they_decode_then_only_the_latest_waiting_one_is_decoded_and_previews_stay_in_order()
    {
        var firstDecodeStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var releaseFirstDecode = new ManualResetEventSlim();
        var decodedPaths = new List<string>();
        decoder.DecodeToPng(Arg.Any<string>(), Arg.Any<int>()).Returns(call =>
        {
            var path = call.Arg<string>();
            decodedPaths.Add(path);
            if (path.EndsWith("1.jpg", StringComparison.Ordinal))
            {
                firstDecodeStarted.TrySetResult();
                releaseFirstDecode.Wait(Timeout);
            }

            return new MemoryStream();
        });
        var coordinator = Create();
        var lastPreview = new TaskCompletionSource<WallpaperPreviewImage>(TaskCreationOptions.RunContinuationsAsynchronously);
        var names = new List<string>();
        coordinator.ImageReady += (_, preview) =>
        {
            names.Add(preview.Name);
            if (preview.Name == "wallpaper-3") lastPreview.TrySetResult(preview);
        };

        NotifyDownloaded("wallpaper-1");
        await firstDecodeStarted.Task.WaitAsync(Timeout, TestContext.Current.CancellationToken);
        NotifyDownloaded("wallpaper-2");
        NotifyDownloaded("wallpaper-3");
        releaseFirstDecode.Set();
        await lastPreview.Task.WaitAsync(Timeout, TestContext.Current.CancellationToken);

        decodedPaths.ShouldBe(["/some/path/wallpaper-1.jpg", "/some/path/wallpaper-3.jpg"]);
        names.ShouldBe(["wallpaper-1", "wallpaper-3"]);
    }

    [Fact]
    public async Task when_the_preview_is_disabled_while_an_image_is_waiting_then_it_is_not_decoded()
    {
        var firstDecodeStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var releaseFirstDecode = new ManualResetEventSlim();
        var decodedPaths = new List<string>();
        decoder.DecodeToPng(Arg.Any<string>(), Arg.Any<int>()).Returns(call =>
        {
            decodedPaths.Add(call.Arg<string>());
            firstDecodeStarted.TrySetResult();
            releaseFirstDecode.Wait(Timeout);

            return new MemoryStream();
        });
        var coordinator = Create();

        NotifyDownloaded("wallpaper-1");
        await firstDecodeStarted.Task.WaitAsync(Timeout, TestContext.Current.CancellationToken);
        NotifyDownloaded("wallpaper-2");
        coordinator.IsEnabled = false;
        releaseFirstDecode.Set();
        await Task.Delay(100, TestContext.Current.CancellationToken);

        decodedPaths.ShouldBe(["/some/path/wallpaper-1.jpg"]);
    }

    private ImageDisplayCoordinator Create(bool isEnabled = true)
    {
        var coordinator = new ImageDisplayCoordinator(notifier, decoder) { IsEnabled = isEnabled };
        coordinators.Add(coordinator);

        return coordinator;
    }

    private void NotifyDownloaded(string name = "wallpaper-1")
        => notifier.NotifyImageDownloaded(new WallpaperDownloadDetails($"/some/path/{name}.jpg", name, "Cars", 1234, 1920, 1080));
}
