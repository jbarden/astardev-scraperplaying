namespace AStarDev.ScraperPlaying.Scraping;

/// <summary>Interface for dropping wallpapers that have already been downloaded from a listing page's wallpaper ids.</summary>
public interface INewWallpaperFilter
{
    /// <summary>Keeps only the wallpapers that have not been downloaded yet, reporting each one skipped.</summary>
    /// <param name="wallpaperIds">The Wallhaven wallpaper ids found on a listing page.</param>
    /// <param name="progress">The progress reporter to report skipped wallpapers and failures.</param>
    /// <param name="cancellationToken">The cancellation token to cancel the operation.</param>
    /// <returns>A task containing the ids not downloaded yet, or none when the check itself failed (the failure is reported).</returns>
    Task<string[]> ExcludeAlreadyDownloadedAsync(string[] wallpaperIds, IProgress<string> progress, CancellationToken cancellationToken);
}
