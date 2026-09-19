using AStarDev.ScraperPlaying.UI;
using AStarDev.ScraperPlaying.WallpaperIngestion;

namespace AStarDev.ScraperPlaying.TestsUnit.UI;

public sealed class GivenAnImageDisplayCoordinator
{
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(5);
    private readonly IImageDownloadNotifier notifier = new ImageDownloadNotifier();
    private readonly IDownloadedImageDecoder decoder = Substitute.For<IDownloadedImageDecoder>();

    [Fact]
    public async Task when_a_notified_image_decodes_successfully_then_image_ready_is_raised_with_the_decoded_stream_and_details()
    {
        var decodedStream = new MemoryStream([1, 2, 3]);
        decoder.DecodeToPng("/some/path/wallpaper-1.jpg", Arg.Any<int>()).Returns(decodedStream);
        var coordinator = new ImageDisplayCoordinator(notifier, decoder);
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
        _ = new ImageDisplayCoordinator(notifier, decoder);

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
        var coordinator = new ImageDisplayCoordinator(notifier, decoder);
        var raised = false;
        coordinator.ImageReady += (_, _) => raised = true;

        Should.NotThrow(NotifyDownloaded);
        await decodeAttempted.Task.WaitAsync(Timeout, TestContext.Current.CancellationToken);
        await Task.Delay(50, TestContext.Current.CancellationToken);

        raised.ShouldBeFalse();
    }

    [Fact]
    public void when_the_preview_is_disabled_then_a_notified_image_is_not_decoded()
    {
        var coordinator = new ImageDisplayCoordinator(notifier, decoder) { IsEnabled = false };
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
        var coordinator = new ImageDisplayCoordinator(notifier, decoder) { IsEnabled = false };
        var received = new TaskCompletionSource<WallpaperPreviewImage>(TaskCreationOptions.RunContinuationsAsynchronously);
        coordinator.ImageReady += (_, preview) => received.TrySetResult(preview);
        coordinator.IsEnabled = true;

        NotifyDownloaded();

        (await received.Task.WaitAsync(Timeout, TestContext.Current.CancellationToken)).Name.ShouldBe("wallpaper-1");
    }

    private void NotifyDownloaded()
        => notifier.NotifyImageDownloaded(new WallpaperDownloadDetails("/some/path/wallpaper-1.jpg", "wallpaper-1", "Cars", 1234, 1920, 1080));
}
