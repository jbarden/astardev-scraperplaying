using AStarDev.FunctionalParadigm;
using AStarDev.ScraperPlaying.Scraping;
using Microsoft.Playwright;

namespace AStarDev.ScraperPlaying.WallpaperIngestion;

/// <summary>Interface for downloading a wallpaper's full-size image using the browser session and saving it to disk.</summary>
public interface IWallpaperImageDownloader
{
    /// <summary>Downloads the wallpaper's image using the browser session's request context (so its login state is reused) and saves it as <c>{id}{extension}</c> in <paramref name="directory"/>, creating the directory if needed.</summary>
    /// <param name="detail">The wallpaper scraped from its detail page.</param>
    /// <param name="page">The browser page whose context is used to make the download request.</param>
    /// <param name="directory">The directory to save the image into.</param>
    /// <param name="progress">The progress reporter to report download progress.</param>
    /// <param name="cancellationToken">The cancellation token to cancel the operation.</param>
    /// <returns>A task containing an <see cref="Exceptional{T}"/> wrapping the downloaded image, or the captured failure (no image URL, non-success status, IO error).</returns>
    Task<Exceptional<DownloadedWallpaperImage>> DownloadAsync(WallpaperDetail detail, IPage page, string directory, IProgress<string> progress, CancellationToken cancellationToken);
}
