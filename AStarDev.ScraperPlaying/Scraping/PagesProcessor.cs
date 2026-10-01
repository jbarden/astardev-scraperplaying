using AStarDev.ControlDb;
using AStarDev.FunctionalParadigm;
using AStarDev.ScraperPlaying.ScrapeConfiguration;
using AStarDev.ScraperPlaying.Scraping.WallhavenResponses.SearchResponse;
using AStarDev.ScraperPlaying.WallpaperIngestion;
using AStarDev.Utilities;
using Microsoft.EntityFrameworkCore;

namespace AStarDev.ScraperPlaying.Scraping;

/// <inheritdoc/>
public sealed class PagesProcessor(IWallpaperIngestionContextFactory contextFactory, IWallhavenPageFetcher pageFetcher, IUnitOfWork unitOfWork, IWallpaperIngestionService wallpaperIngestionService, ScrapeResumePolicy resumePolicy) : IPagesProcessor
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
            await SavePartiallyIngestedPageAsync(progress);

            throw;
        }
        catch (Exception ex)
        {
            progress.Report($"An error occurred during the fetching and processing of pages: {ex.ToMessageChain()}");
            throw;
        }
    }

    private async Task<Option<SearchCategoryProgress>> ScrapeAsync(PageScrapeRequest request, IProgress<string> progress, CancellationToken cancellationToken)
    {
        var ingestionContext = await contextFactory.CreateAsync(request, cancellationToken);
        var startPage = await ResolveStartPageAsync(request, ingestionContext.Client, progress, cancellationToken);
        if (resumePolicy.IsUnchangedSincePreviousScrape(request.PreviousProgress, startPage.Response.Meta))
        {
            progress.Report($"Skipping {request.Label.LogLabel} - nothing has changed since the last scrape.");

            return Option.None<SearchCategoryProgress>();
        }

        return Option.Some(await IngestPagesAsync(request, ingestionContext, startPage, progress, cancellationToken));
    }

    private async Task<FetchedPage> ResolveStartPageAsync(PageScrapeRequest request, HttpClient client, IProgress<string> progress, CancellationToken cancellationToken)
    {
        var resumePage = resumePolicy.ResumePage(request.PreviousProgress);
        var fetched = await FetchPageAsync(request, client, resumePage, progress, cancellationToken);
        if (resumePage > 1 && !ScrapeResumePolicy.IsSameCategoryAsPreviousScrape(request.PreviousProgress, fetched.Response.Meta)) fetched = await FetchPageAsync(request, client, 1, progress, cancellationToken);

        return fetched;
    }

    private async Task<SearchCategoryProgress> IngestPagesAsync(PageScrapeRequest request, WallpaperIngestionContext ingestionContext, FetchedPage startPage, IProgress<string> progress, CancellationToken cancellationToken)
    {
        var current = startPage;
        while (true)
        {
            await IngestPageAsync(request, ingestionContext, current, progress, cancellationToken);
            if (resumePolicy.IsLastPageToVisit(current.Number, current.Response.Meta)) return new SearchCategoryProgress(current.Response.Meta.Total, current.Number, current.Response.Meta.LastPage);

            current = await FetchPageAsync(request, ingestionContext.Client, current.Number + 1, progress, cancellationToken);
        }
    }

    private async Task IngestPageAsync(PageScrapeRequest request, WallpaperIngestionContext ingestionContext, FetchedPage page, IProgress<string> progress, CancellationToken cancellationToken)
    {
        await wallpaperIngestionService.IngestPageAsync(page.Response.Data, ingestionContext, progress, cancellationToken);
        request.Hooks.OnPageCompleted(new SearchCategoryProgress(page.Response.Meta.Total, page.Number, page.Response.Meta.LastPage));
        _ = await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private async Task<FetchedPage> FetchPageAsync(PageScrapeRequest request, HttpClient client, int page, IProgress<string> progress, CancellationToken cancellationToken)
        => new(page, await pageFetcher.FetchPageAsync(new PageFetchRequest(request.Label.LogLabel, request.Hooks.PageUrlFactory(page), page), client, progress, cancellationToken));

    private async Task SavePartiallyIngestedPageAsync(IProgress<string> progress)
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

    private readonly record struct FetchedPage(int Number, SearchResponse Response);
}
