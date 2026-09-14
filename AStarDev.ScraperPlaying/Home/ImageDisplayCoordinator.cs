namespace AStarDev.ScraperPlaying.Home;

/// <summary>
/// Bridges <see cref="IImageDownloadNotifier"/> to a display-ready preview: decodes each downloaded
/// wallpaper's image and raises <see cref="ImageReady"/> with the result. A decode failure is swallowed - a
/// missing preview for one wallpaper isn't worth interrupting the scrape over.
/// </summary>
public sealed class ImageDisplayCoordinator
{
    /// <summary>Raised with the decoded, display-ready image once a downloaded wallpaper's image is ready.</summary>
    public event EventHandler<WallpaperPreviewImage>? ImageReady;

    public ImageDisplayCoordinator(IImageDownloadNotifier notifier, IDownloadedImageDecoder decoder)
        => notifier.ImageDownloaded += (_, details) =>
        {
            try
            {
                var pngStream = decoder.DecodeToPng(details.FilePath);
                ImageReady?.Invoke(this, new WallpaperPreviewImage(pngStream, details.Name, details.CategoryLabel, details.FileSizeBytes, details.Width, details.Height));
            }
            catch (Exception exception) when (exception is IOException or InvalidOperationException)
            {
                // Best-effort preview - a decode failure just means no preview for this wallpaper.
            }
        };
}
