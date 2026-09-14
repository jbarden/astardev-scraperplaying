using AStarDev.ScraperPlaying.Home;

namespace AStarDev.ScraperPlaying.TestsUnit;

public sealed class GivenAnImageDownloadNotifier
{
    [Fact]
    public void when_an_image_download_is_notified_then_subscribers_receive_the_file_path()
    {
        var notifier = new ImageDownloadNotifier();
        string? receivedPath = null;
        notifier.ImageDownloaded += (_, path) => receivedPath = path;

        notifier.NotifyImageDownloaded("/some/path/wallpaper-1.jpg");

        receivedPath.ShouldBe("/some/path/wallpaper-1.jpg");
    }

    [Fact]
    public void when_notified_with_no_subscribers_then_it_does_not_throw()
    {
        var notifier = new ImageDownloadNotifier();

        Should.NotThrow(() => notifier.NotifyImageDownloaded("/some/path/wallpaper-1.jpg"));
    }
}
