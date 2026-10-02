using AStarDev.ControlDb;
using AStarDev.ScraperPlaying.ScrapeConfiguration;
using AStarDev.ScraperPlaying.WallpaperIngestion;
using AStarDev.Utilities;
using Microsoft.EntityFrameworkCore;

namespace AStarDev.ScraperPlaying.Scraping;

/// <summary>Ingests one fetched page: its wallpapers, the progress report and the save that commits them.</summary>
/// <param name="wallpaperIngestionService">Ingests the wallpapers on a page.</param>
/// <param name="unitOfWork">Saves what the ingestion recorded.</param>
public sealed class PageIngestionStep(IWallpaperIngestionService wallpaperIngestionService, IUnitOfWork unitOfWork)
{
    /// <summary>Ingests the page, reports the progress so far through the request's hook (only when the page was fully ingested and <paramref name="recordProgress"/> allows it), then saves.</summary>
    /// <param name="run">The scrape the page belongs to.</param>
    /// <param name="page">The page to ingest.</param>
    /// <param name="recordProgress">False once an earlier page of this scrape was not fully ingested, so progress never moves past a page that must be retried.</param>
    /// <returns>Whether the page was fully ingested.</returns>
    public async Task<IngestOutcome> IngestPageAsync(IngestionRun run, FetchedPage page, bool recordProgress)
    {
        var outcome = await wallpaperIngestionService.IngestPageAsync(page.Response.Data, run.Context, run.Progress, run.CancellationToken);
        if (recordProgress && outcome == IngestOutcome.Complete) run.Request.Hooks.OnPageCompleted(new SearchCategoryProgress(page.Response.Meta.Total, page.Number, page.Response.Meta.LastPage));
        _ = await unitOfWork.SaveChangesAsync(run.CancellationToken);

        return outcome;
    }

    /// <summary>Saves the wallpapers downloaded so far on a page that was cancelled part-way, ignoring the cancellation that interrupted it.</summary>
    /// <param name="progress">Receives the outcome.</param>
    public async Task SavePartiallyIngestedPageAsync(IProgress<string> progress)
    {
        try
        {
            _ = await unitOfWork.SaveChangesAsync(CancellationToken.None);
            progress.Report("Scrape cancelled - saved wallpapers downloaded so far this page.");
        }
        catch (DbUpdateException ex)
        {
            progress.Report($"Scrape cancelled - failed to save wallpapers downloaded so far this page: {ex.ToMessageChain()}");
        }
    }
}
