using AStarDev.FunctionalParadigm;

namespace AStarDev.ScraperPlaying.WallpaperIngestion;

/// <summary>Interface for resolving the directory a wallpaper's image and file record should be saved under.</summary>
public interface ISaveDirectoryResolver
{
    /// <summary>
    /// Resolves the directory a wallpaper's image and file record should be saved under: the configured
    /// scrape root directory (or the famous root directory when <paramref name="isFamous"/>) combined with either
    /// "top-wallpapers" or the slugified search category name.
    /// </summary>
    /// <param name="categoryName">The search category name when a category search is being performed, or <see cref="Option{T}.None"/> when processing the "Top Wallpapers" scrape.</param>
    /// <param name="isFamous"><see langword="true"/> when the wallpaper is tagged as a famous person and belongs under the famous root directory.</param>
    /// <param name="cancellationToken">The cancellation token to cancel the operation.</param>
    /// <returns>A task representing the asynchronous operation, containing the resolved directory.</returns>
    /// <exception cref="InvalidOperationException">Thrown when <paramref name="isFamous"/> is set but no famous root directory is configured.</exception>
    Task<string> ResolveSaveDirectoryAsync(Option<string> categoryName, bool isFamous, CancellationToken cancellationToken);
}
