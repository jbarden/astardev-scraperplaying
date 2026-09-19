using AStarDev.ControlDb;
using AStarDev.ControlDb.FileDetail;
using Microsoft.EntityFrameworkCore;

namespace AStarDev.ScraperPlaying.Scraping;

/// <inheritdoc/>
public class IngestionStore(IUnitOfWork unitOfWork) : IIngestionStore
{
    /// <inheritdoc/>
    public IRepository<FileEntity, FileId> FileRepository => unitOfWork.GetRepository<FileEntity, FileId>();

    /// <inheritdoc/>
    public Task SavePageAsync(CancellationToken cancellationToken) => unitOfWork.SaveChangesAsync(cancellationToken);

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
            progress.Report($"Scrape cancelled - failed to save wallpapers downloaded so far this page: {ex.Message}");
        }
    }
}
