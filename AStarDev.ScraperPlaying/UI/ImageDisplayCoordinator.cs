using AStarDev.ScraperPlaying.WallpaperIngestion;

namespace AStarDev.ScraperPlaying.UI;

/// <summary>
/// Bridges <see cref="IImageDownloadNotifier"/> to a display-ready preview: decodes each downloaded
/// wallpaper's image (scaled down to preview size, on a background thread so the scrape is not held up) and raises
/// <see cref="ImageReady"/> with the result. Nothing is decoded while <see cref="IsEnabled"/> is <see langword="false"/>.
/// A decode failure is swallowed - a missing preview for one wallpaper isn't worth interrupting the scrape over.
/// </summary>
public sealed class ImageDisplayCoordinator
{
    private const int PreviewMaxDimension = 1280;

    private volatile bool isEnabled = true;

    /// <summary>Raised with the decoded, display-ready image once a downloaded wallpaper's image is ready.</summary>
    public event EventHandler<WallpaperPreviewImage>? ImageReady;

    /// <summary>Gets or sets whether downloaded images are decoded for preview. Defaults to <see langword="true"/>.</summary>
    public bool IsEnabled
    {
        get => isEnabled;
        set => isEnabled = value;
    }

    public ImageDisplayCoordinator(IImageDownloadNotifier notifier, IDownloadedImageDecoder decoder)
        => notifier.ImageDownloaded += (_, details) =>
        {
            if (!isEnabled) return;

            _ = Task.Run(() =>
            {
                try
                {
                    var pngStream = decoder.DecodeToPng(details.FilePath, PreviewMaxDimension);
                    ImageReady?.Invoke(this, new WallpaperPreviewImage(pngStream, details.Name, details.CategoryLabel, details.FileSizeBytes, details.Width, details.Height));
                }
                catch (Exception exception) when (exception is IOException or InvalidOperationException)
                {
                    // Best-effort preview - a decode failure just means no preview for this wallpaper.
                }
            });
        };
}
