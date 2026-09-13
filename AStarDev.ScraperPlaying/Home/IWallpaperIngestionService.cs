using AStarDev.ScraperPlaying.SearchAPI.SearchResponse;

namespace AStarDev.ScraperPlaying.Home;

/// <summary>
/// Interface for ingesting a single wallpaper: skipping it if a file record already exists, otherwise
/// downloading its image, persisting its file record, and fetching/linking its tags.
/// </summary>
public interface IWallpaperIngestionService
{
    /// <summary>
    /// Ingests a single wallpaper into <paramref name="context"/>'s directory, reporting progress and any
    /// failure at each step without throwing - a failure at any step is reported and the wallpaper is skipped,
    /// allowing the caller to continue processing the rest of the page.
    /// </summary>
    /// <param name="wallpaper">The wallpaper data to ingest.</param>
    /// <param name="context">The per-page state (save directory, HTTP client, file repository) to ingest the wallpaper into.</param>
    /// <param name="progress">The progress reporter to report ingestion progress.</param>
    /// <param name="cancellationToken">The cancellation token to cancel the operation.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task IngestAsync(Data wallpaper, WallpaperIngestionContext context, IProgress<string> progress, CancellationToken cancellationToken);
}
