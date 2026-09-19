using AStarDev.ControlDb;
using AStarDev.ControlDb.FileDetail;
using Microsoft.EntityFrameworkCore;

namespace AStarDev.ScraperPlaying.Scraping;

/// <inheritdoc/>
public class IngestionStore(IUnitOfWork unitOfWork, ITagsProcessor tagsProcessor) : IIngestionStore
{
    /// <inheritdoc/>
    public IRepository<FileEntity, FileId> FileRepository => unitOfWork.GetRepository<FileEntity, FileId>();

    /// <inheritdoc/>
    public async Task SavePageAsync(IProgress<string> progress, CancellationToken cancellationToken)
    {
        try
        {
            await unitOfWork.SaveChangesAsync(cancellationToken);
            tagsProcessor.AcceptPendingTags();
        }
        catch (DbUpdateException ex)
        {
            tagsProcessor.DiscardPendingTags();
            progress.Report($"Failed to save the wallpapers ingested from this page, continuing with the next page: {ex.Message}");
        }

        unitOfWork.ClearChangeTracker();
    }

    /// <inheritdoc/>
    public async Task SaveAfterCancellationAsync(IProgress<string> progress)
    {
        try
        {
            await unitOfWork.SaveChangesAsync(CancellationToken.None);
            progress.Report("Scrape cancelled - saved wallpapers downloaded so far this page.");
        }
        catch (DbUpdateException ex)
        {
            unitOfWork.ClearChangeTracker();
            progress.Report($"Scrape cancelled - failed to save wallpapers downloaded so far this page: {ex.Message}");
        }
    }
}
