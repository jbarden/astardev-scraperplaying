using AStarDev.ScraperPlaying.Scraping.WallhavenResponses.SearchResponse;

namespace AStarDev.ScraperPlaying.WallpaperIngestion;

/// <summary>Ingests one wallpaper known not to have a file record yet: fetches its tags, downloads its image, records the file and links the tags.</summary>
public interface INewWallpaperIngestor
{
    /// <summary>
    /// Ingests <paramref name="wallpaper"/> into <paramref name="context"/>'s directory, reporting a failure at any step without throwing so the
    /// caller can continue with the rest of the page. A cancellation is not swallowed.
    /// </summary>
    /// <param name="wallpaper">The wallpaper data to ingest.</param>
    /// <param name="extension">The file extension to save the image under.</param>
    /// <param name="context">The per-page state to ingest the wallpaper into.</param>
    /// <param name="progress">The progress reporter to report ingestion progress.</param>
    /// <param name="cancellationToken">The cancellation token to cancel the operation.</param>
    Task IngestAsync(Data wallpaper, string extension, WallpaperIngestionContext context, IProgress<string> progress, CancellationToken cancellationToken);
}
