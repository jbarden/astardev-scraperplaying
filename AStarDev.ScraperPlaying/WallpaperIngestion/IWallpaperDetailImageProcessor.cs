using AStarDev.ControlDb;
using AStarDev.ControlDb.FileDetail;
using AStarDev.FunctionalParadigm;
using AStarDev.ScraperPlaying.Scraping;
using Microsoft.Playwright;

namespace AStarDev.ScraperPlaying.WallpaperIngestion;

/// <summary>Interface for downloading and recording the image of a wallpaper scraped from its detail page, using the browser session rather than the JSON API.</summary>
public interface IWallpaperDetailImageProcessor
{
    /// <summary>Downloads the wallpaper's full-size image using the browser session's request context (so its login state is reused), saves it to disk, and records its <see cref="FileEntity"/>. Wallpapers whose file already exists are skipped.</summary>
    /// <param name="detail">The wallpaper scraped from its detail page.</param>
    /// <param name="page">The browser page whose context is used to make the download request.</param>
    /// <param name="directory">The directory to save the image into (created if it does not already exist).</param>
    /// <param name="categoryLabel">The search category the wallpaper came from, or "Top Wallpapers" when it did not come from a specific search category.</param>
    /// <param name="fileRepository">The repository used to store the file entity.</param>
    /// <param name="progress">The progress reporter to report download progress.</param>
    /// <param name="cancellationToken">The cancellation token to cancel the operation.</param>
    /// <returns>
    /// A task containing an <see cref="Exceptional{T}"/> wrapping the added <see cref="FileEntity"/>, or
    /// <see cref="Option{T}.None"/> when the wallpaper already existed and was skipped, or the captured failure.
    /// </returns>
    Task<Exceptional<Option<FileEntity>>> DownloadAndRecordAsync(WallpaperDetail detail, IPage page, string directory, string categoryLabel, IRepository<FileEntity, FileId> fileRepository, IProgress<string> progress, CancellationToken cancellationToken);
}
