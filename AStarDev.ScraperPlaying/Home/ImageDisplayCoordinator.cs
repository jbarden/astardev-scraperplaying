namespace AStarDev.ScraperPlaying.Home;

/// <summary>
/// Bridges <see cref="IImageDownloadNotifier"/> to a display-ready PNG stream: decodes each downloaded
/// wallpaper's image and raises <see cref="ImageReady"/> with the result. A decode failure is swallowed - a
/// missing preview for one wallpaper isn't worth interrupting the scrape over.
/// </summary>
public sealed class ImageDisplayCoordinator
{
    /// <summary>Raised with the decoded PNG stream once a downloaded wallpaper's image is ready to display.</summary>
    public event EventHandler<Stream>? ImageReady;

    public ImageDisplayCoordinator(IImageDownloadNotifier notifier, IDownloadedImageDecoder decoder)
        => notifier.ImageDownloaded += (_, filePath) =>
        {
            try
            {
                ImageReady?.Invoke(this, decoder.DecodeToPng(filePath));
            }
            catch (Exception exception) when (exception is IOException or InvalidOperationException)
            {
                // Best-effort preview - a decode failure just means no preview for this wallpaper.
            }
        };
}
