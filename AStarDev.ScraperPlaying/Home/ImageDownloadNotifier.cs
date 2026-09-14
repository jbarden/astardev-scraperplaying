namespace AStarDev.ScraperPlaying.Home;

/// <inheritdoc/>
public sealed class ImageDownloadNotifier : IImageDownloadNotifier
{
    /// <inheritdoc/>
    public event EventHandler<string>? ImageDownloaded;

    /// <inheritdoc/>
    public void NotifyImageDownloaded(string filePath) => ImageDownloaded?.Invoke(this, filePath);
}
