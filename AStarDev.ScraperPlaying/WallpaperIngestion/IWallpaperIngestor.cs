namespace AStarDev.ScraperPlaying.WallpaperIngestion;

/// <summary>Interface for ingesting a single wallpaper: scraping its detail page, downloading and recording its image, and linking its tags.</summary>
public interface IWallpaperIngestor
{
    /// <summary>Ingests a wallpaper. Failures at any step are reported to <paramref name="progress"/> and the wallpaper is skipped; only cancellation propagates.</summary>
    /// <param name="wallpaperId">The Wallhaven wallpaper id.</param>
    /// <param name="context">The state shared by every wallpaper in the current search.</param>
    /// <param name="progress">The progress reporter to report progress and failures.</param>
    /// <param name="cancellationToken">The cancellation token to cancel the operation.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task IngestAsync(string wallpaperId, PageIngestionContext context, IProgress<string> progress, CancellationToken cancellationToken);
}
