using AStarDev.ControlDb;
using AStarDev.ControlDb.FileDetail;
using AStarDev.ScraperPlaying.SearchAPI.SearchResponse;

namespace AStarDev.ScraperPlaying.Home;

/// <summary>
/// Interface for ingesting a single wallpaper: skipping it if a file record already exists, otherwise
/// downloading its image, persisting its file record, and fetching/linking its tags.
/// </summary>
public interface IWallpaperIngestionService
{
    /// <summary>
    /// Ingests a single wallpaper into <paramref name="directory"/>, reporting progress and any failure at
    /// each step without throwing - a failure at any step is reported and the wallpaper is skipped, allowing
    /// the caller to continue processing the rest of the page.
    /// </summary>
    /// <param name="wallpaper">The wallpaper data to ingest.</param>
    /// <param name="directory">The directory to save the wallpaper's image into.</param>
    /// <param name="client">The HTTP client used to make requests.</param>
    /// <param name="fileRepository">The repository used to store the wallpaper's file entity.</param>
    /// <param name="progress">The progress reporter to report ingestion progress.</param>
    /// <param name="cancellationToken">The cancellation token to cancel the operation.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task IngestAsync(Data wallpaper, string directory, HttpClient client, IRepository<FileEntity, FileId> fileRepository, IProgress<string> progress, CancellationToken cancellationToken);
}
