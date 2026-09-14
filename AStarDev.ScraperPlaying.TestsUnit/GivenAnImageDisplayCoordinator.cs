using AStarDev.ScraperPlaying.Home;

namespace AStarDev.ScraperPlaying.TestsUnit;

public sealed class GivenAnImageDisplayCoordinator
{
    private readonly IImageDownloadNotifier notifier = new ImageDownloadNotifier();
    private readonly IDownloadedImageDecoder decoder = Substitute.For<IDownloadedImageDecoder>();

    [Fact]
    public void when_a_notified_image_decodes_successfully_then_image_ready_is_raised_with_the_decoded_stream()
    {
        var decodedStream = new MemoryStream([1, 2, 3]);
        decoder.DecodeToPng("/some/path/wallpaper-1.jpg").Returns(decodedStream);
        var coordinator = new ImageDisplayCoordinator(notifier, decoder);
        Stream? receivedStream = null;
        coordinator.ImageReady += (_, stream) => receivedStream = stream;

        notifier.NotifyImageDownloaded("/some/path/wallpaper-1.jpg");

        receivedStream.ShouldBeSameAs(decodedStream);
    }

    [Fact]
    public void when_a_notified_image_fails_to_decode_then_image_ready_is_not_raised_and_nothing_throws()
    {
        decoder.DecodeToPng("/some/path/wallpaper-1.jpg").Returns(_ => throw new InvalidOperationException("bad image"));
        var coordinator = new ImageDisplayCoordinator(notifier, decoder);
        var raised = false;
        coordinator.ImageReady += (_, _) => raised = true;

        Should.NotThrow(() => notifier.NotifyImageDownloaded("/some/path/wallpaper-1.jpg"));

        raised.ShouldBeFalse();
    }
}
