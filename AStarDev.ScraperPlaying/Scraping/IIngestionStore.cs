using AStarDev.ControlDb;
using AStarDev.ControlDb.FileDetail;

namespace AStarDev.ScraperPlaying.Scraping;

/// <summary>Interface for where ingested wallpapers are stored and when they are saved.</summary>
public interface IIngestionStore
{
    /// <summary>Gets the repository ingested wallpapers' file entities are added to.</summary>
    IRepository<FileEntity, FileId> FileRepository { get; }

    /// <summary>
    /// Saves the wallpapers ingested from the current listing page and stops tracking them so the context does not grow over a long run.
    /// A database failure is reported and the page's unsaved changes are discarded, so the next page starts clean.
    /// </summary>
    /// <param name="progress">The progress reporter to report a failed save.</param>
    /// <param name="cancellationToken">The cancellation token to cancel the operation.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task SavePageAsync(IProgress<string> progress, CancellationToken cancellationToken);

    /// <summary>Saves the wallpapers ingested so far after the scrape was cancelled, reporting the outcome instead of throwing on a database failure.</summary>
    /// <param name="progress">The progress reporter to report whether the save succeeded.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task SaveAfterCancellationAsync(IProgress<string> progress);
}
