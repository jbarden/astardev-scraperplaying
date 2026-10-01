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
    /// <summary>Ingests the page, reports the progress so far through the request's hook, then saves.</summary>
    /// <param name="run">The scrape the page belongs to.</param>
    /// <param name="page">The page to ingest.</param>
    public async Task IngestPageAsync(IngestionRun run, FetchedPage page)
    {
        await wallpaperIngestionService.IngestPageAsync(page.Response.Data, run.Context, run.Progress, run.CancellationToken);
        run.Request.Hooks.OnPageCompleted(new SearchCategoryProgress(page.Response.Meta.Total, page.Number, page.Response.Meta.LastPage));
        _ = await unitOfWork.SaveChangesAsync(run.CancellationToken);
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
