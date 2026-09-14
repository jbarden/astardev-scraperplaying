using AStarDev.ScraperPlaying.Home;

namespace AStarDev.ScraperPlaying.TestsUnit;

public sealed class GivenAnImageDisplayCoordinator
{
    private readonly IImageDownloadNotifier notifier = new ImageDownloadNotifier();
    private readonly IDownloadedImageDecoder decoder = Substitute.For<IDownloadedImageDecoder>();

    [Fact]
    public void when_a_notified_image_decodes_successfully_then_image_ready_is_raised_with_the_decoded_stream_and_details()
    {
        var decodedStream = new MemoryStream([1, 2, 3]);
        decoder.DecodeToPng("/some/path/wallpaper-1.jpg").Returns(decodedStream);
        var coordinator = new ImageDisplayCoordinator(notifier, decoder);
        WallpaperPreviewImage? received = null;
        coordinator.ImageReady += (_, preview) => received = preview;

        notifier.NotifyImageDownloaded(new WallpaperDownloadDetails("/some/path/wallpaper-1.jpg", "wallpaper-1", "Cars", 1234, 1920, 1080));

        received.ShouldNotBeNull();
        received.PngStream.ShouldBeSameAs(decodedStream);
        received.Name.ShouldBe("wallpaper-1");
        received.CategoryLabel.ShouldBe("Cars");
        received.FileSizeBytes.ShouldBe(1234);
        received.Width.ShouldBe(1920);
        received.Height.ShouldBe(1080);
    }

    [Fact]
    public void when_a_notified_image_fails_to_decode_then_image_ready_is_not_raised_and_nothing_throws()
    {
        decoder.DecodeToPng("/some/path/wallpaper-1.jpg").Returns(_ => throw new InvalidOperationException("bad image"));
        var coordinator = new ImageDisplayCoordinator(notifier, decoder);
        var raised = false;
        coordinator.ImageReady += (_, _) => raised = true;

        Should.NotThrow(() => notifier.NotifyImageDownloaded(new WallpaperDownloadDetails("/some/path/wallpaper-1.jpg", "wallpaper-1", "Cars", 1234, 1920, 1080)));

        raised.ShouldBeFalse();
    }
}
