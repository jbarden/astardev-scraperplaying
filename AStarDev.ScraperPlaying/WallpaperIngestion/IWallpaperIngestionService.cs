using AStarDev.ScraperPlaying.Scraping.WallhavenResponses.SearchResponse;

namespace AStarDev.ScraperPlaying.WallpaperIngestion;

/// <summary>
/// Interface for ingesting a page of wallpapers: skipping those whose file record already exists (found with a
/// single query for the whole page), and for the rest downloading the image, persisting the file record, and
/// fetching/linking the tags.
/// </summary>
public interface IWallpaperIngestionService
{
    /// <summary>
    /// Ingests a page of wallpapers into <paramref name="context"/>'s directory, reporting progress and any
    /// failure at each step without throwing - a failure at any step is reported and that wallpaper is skipped,
    /// allowing the caller to continue processing the rest of the page.
    /// </summary>
    /// <param name="wallpapers">The wallpaper data to ingest.</param>
    /// <param name="context">The per-page state (save directory, HTTP client, file repository) to ingest the wallpaper into.</param>
    /// <param name="progress">The progress reporter to report ingestion progress.</param>
    /// <param name="cancellationToken">The cancellation token to cancel the operation.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task IngestPageAsync(IReadOnlyList<Data> wallpapers, WallpaperIngestionContext context, IProgress<string> progress, CancellationToken cancellationToken);
}
