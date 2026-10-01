using AStarDev.ScraperPlaying.Scraping.WallhavenResponses.SearchResponse;
using Tag = AStarDev.ScraperPlaying.Scraping.WallhavenResponses.DetailResponse.Tag;

namespace AStarDev.ScraperPlaying.WallpaperIngestion;

/// <summary>
/// Ingests one wallpaper known not to have a file record yet, in two steps so a caller can fetch the next wallpaper's tags while this one downloads:
/// <see cref="FetchTagsAsync"/> only talks to the network (safe to run alongside <see cref="IngestAsync"/>), while <see cref="IngestAsync"/> downloads the image
/// and uses the database, so it must not be run for two wallpapers at once.
/// </summary>
public interface INewWallpaperIngestor
{
    /// <summary>Fetches the wallpaper's tags. A failure to fetch them is reported and gives no tags rather than throwing; a cancellation is not swallowed.</summary>
    /// <param name="wallpaper">The wallpaper whose tags to fetch.</param>
    /// <param name="context">The per-page state, supplying the HTTP client.</param>
    /// <param name="progress">The progress reporter to report progress.</param>
    /// <param name="cancellationToken">The cancellation token to cancel the operation.</param>
    /// <returns>The wallpaper's tags, or none if they could not be fetched.</returns>
    Task<IReadOnlyList<Tag>> FetchTagsAsync(Data wallpaper, WallpaperIngestionContext context, IProgress<string> progress, CancellationToken cancellationToken);

    /// <summary>
    /// Downloads the wallpaper into <paramref name="context"/>'s directory, records it and links <paramref name="tags"/> (or, if any tag is flagged to ignore images, does nothing), reporting a failure at any step without throwing so the
    /// caller can continue with the rest of the page. A cancellation is not swallowed.
    /// </summary>
    /// <param name="candidate">The wallpaper data to ingest, and the file extension to save the image under.</param>
    /// <param name="tags">The wallpaper's tags, from <see cref="FetchTagsAsync"/>.</param>
    /// <param name="context">The per-page state to ingest the wallpaper into.</param>
    /// <param name="progress">The progress reporter to report ingestion progress.</param>
    /// <param name="cancellationToken">The cancellation token to cancel the operation.</param>
    Task IngestAsync(WallpaperCandidate candidate, IReadOnlyList<Tag> tags, WallpaperIngestionContext context, IProgress<string> progress, CancellationToken cancellationToken);
}
