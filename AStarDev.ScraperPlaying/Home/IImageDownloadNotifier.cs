namespace AStarDev.ScraperPlaying.Home;

/// <summary>
/// Interface for publishing when a wallpaper's image has been saved to disk, so interested parties (such as a
/// live preview) can react without the download path needing to know about them.
/// </summary>
public interface IImageDownloadNotifier
{
    /// <summary>Raised after a wallpaper's image has been written to disk.</summary>
    event EventHandler<string>? ImageDownloaded;

    /// <summary>Raises <see cref="ImageDownloaded"/> for the image saved at <paramref name="filePath"/>.</summary>
    /// <param name="filePath">The full path the image was saved to.</param>
    void NotifyImageDownloaded(string filePath);
}
