using AStarDev.FunctionalParadigm;
using AStarDev.ScraperPlaying.ScrapeConfiguration;
using AStarDev.ScraperPlaying.WallpaperIngestion;
using AStarDev.Utilities;

namespace AStarDev.ScraperPlaying.Scraping;

/// <inheritdoc/>
public sealed class PagesProcessor(IWallpaperIngestionContextFactory contextFactory, IWallhavenPageFetcher pageFetcher, PageIngestionStep ingestionStep, ScrapeResumePolicy resumePolicy) : IPagesProcessor
{
    /// <inheritdoc/>
    public async Task<Option<SearchCategoryProgress>> FetchAndProcessPagesAsync(PageScrapeRequest request, IProgress<string> progress, CancellationToken cancellationToken)
    {
        try
        {
            return await ScrapeAsync(request, progress, cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            await ingestionStep.SavePartiallyIngestedPageAsync(progress);

            throw;
        }
    }

    private async Task<Option<SearchCategoryProgress>> ScrapeAsync(PageScrapeRequest request, IProgress<string> progress, CancellationToken cancellationToken)
    {
        var run = new IngestionRun(request, await contextFactory.CreateAsync(request, cancellationToken), progress, cancellationToken);
        var startPage = await ResolveStartPageAsync(run);
        if (resumePolicy.IsUnchangedSincePreviousScrape(request.PreviousProgress, startPage.Response.Meta))
        {
            progress.Report($"Skipping {request.Label.LogLabel} - nothing has changed since the last scrape.");

            return Option.None<SearchCategoryProgress>();
        }

        return await IngestPagesAsync(run, startPage);
    }

    private async Task<FetchedPage> ResolveStartPageAsync(IngestionRun run)
    {
        var previousProgress = run.Request.PreviousProgress;
        var resumePage = resumePolicy.ResumePage(previousProgress);
        var fetched = await FetchPageAsync(run, resumePage);
        if (resumePage > 1 && !ScrapeResumePolicy.IsSameCategoryAsPreviousScrape(previousProgress, fetched.Response.Meta)) fetched = await FetchPageAsync(run, 1);

        return fetched;
    }

    /// <summary>Visits each page in turn. Once a page is not fully ingested, no later page records progress either, so the next scrape resumes at that page and retries it.</summary>
    /// <returns>The progress of the last page recorded, or <see cref="Option{T}.None"/> when no page was.</returns>
    private async Task<Option<SearchCategoryProgress>> IngestPagesAsync(IngestionRun run, FetchedPage startPage)
    {
        var current = startPage;
        var recorded = Option.None<SearchCategoryProgress>();
        var progressWithheld = false;
        while (true)
        {
            var outcome = await ingestionStep.IngestPageAsync(run, current, !progressWithheld);
            if (!progressWithheld && outcome == IngestOutcome.Complete) recorded = Option.Some(new SearchCategoryProgress(current.Response.Meta.Total, current.Number, current.Response.Meta.LastPage));
            else if (!progressWithheld) run.Progress.Report($"Not every wallpaper on page {current.Number} was ingested - progress stays before that page so it is retried on the next scrape.");

            progressWithheld |= outcome == IngestOutcome.Incomplete;
            if (resumePolicy.IsLastPageToVisit(current.Number, current.Response.Meta)) return recorded;

            current = await FetchPageAsync(run, current.Number + 1);
        }
    }

    private async Task<FetchedPage> FetchPageAsync(IngestionRun run, int page)
        => new(page, await pageFetcher.FetchPageAsync(new PageFetchRequest(run.Request.Label.LogLabel, run.Request.Hooks.PageUrlFactory(page), page), run.Context.Client, run.Progress, run.CancellationToken));
}
