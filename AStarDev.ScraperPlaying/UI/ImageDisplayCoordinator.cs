using System.Threading.Channels;
using AStarDev.ScraperPlaying.WallpaperIngestion;

namespace AStarDev.ScraperPlaying.UI;

/// <summary>
/// Bridges <see cref="IImageDownloadNotifier"/> to a display-ready preview: decodes downloaded wallpapers' images
/// (scaled down to preview size, on a background thread so the scrape is not held up) and raises
/// <see cref="ImageReady"/> with the result. Only one image is decoded at a time and only the newest waiting image is kept,
/// so a slow decode never queues up work or memory and previews always arrive in download order. Nothing is decoded while
/// <see cref="IsEnabled"/> is <see langword="false"/>. A decode failure is swallowed - a missing preview for one wallpaper
/// isn't worth interrupting the scrape over.
/// </summary>
public sealed class ImageDisplayCoordinator : IDisposable
{
    private const int PreviewMaxDimension = 1280;

    private readonly Channel<WallpaperDownloadDetails> pending = Channel.CreateBounded<WallpaperDownloadDetails>(new BoundedChannelOptions(1) { FullMode = BoundedChannelFullMode.DropOldest, SingleReader = true });
    private readonly IDownloadedImageDecoder decoder;
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
    {
        this.decoder = decoder;
        notifier.ImageDownloaded += (_, details) =>
        {
            if (isEnabled) pending.Writer.TryWrite(details);
        };
        _ = Task.Run(DecodePendingAsync);
    }

    /// <inheritdoc/>
    public void Dispose() => pending.Writer.TryComplete();

    private async Task DecodePendingAsync()
    {
        await foreach (var details in pending.Reader.ReadAllAsync())
        {
            if (!isEnabled) continue;

            try
            {
                var pngStream = decoder.DecodeToPng(details.FilePath, PreviewMaxDimension);
                ImageReady?.Invoke(this, new WallpaperPreviewImage(pngStream, details.Name, details.CategoryLabel, details.FileSizeBytes, details.Width, details.Height));
            }
            catch (Exception exception) when (exception is IOException or InvalidOperationException)
            {
                // Best-effort preview - a decode failure just means no preview for this wallpaper.
            }
        }
    }
}
