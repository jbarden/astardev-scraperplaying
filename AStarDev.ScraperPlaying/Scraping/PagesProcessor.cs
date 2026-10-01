using AStarDev.ControlDb;
using AStarDev.ControlDb.FileDetail;
using AStarDev.FunctionalParadigm;
using AStarDev.ScraperPlaying.ScrapeConfiguration;
using AStarDev.ScraperPlaying.Scraping.WallhavenResponses.SearchResponse;
using AStarDev.ScraperPlaying.WallpaperIngestion;
using AStarDev.Utilities;
using Microsoft.EntityFrameworkCore;

namespace AStarDev.ScraperPlaying.Scraping;

/// <inheritdoc/>
public sealed class PagesProcessor(IWallhavenClientFactory clientFactory, IWallhavenPageFetcher pageFetcher, IUnitOfWork unitOfWork, ISaveDirectoryResolver saveDirectoryResolver, IWallpaperIngestionService wallpaperIngestionService, ScrapeLimits limits) : IPagesProcessor
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
        var client = clientFactory.Create(request.Connection);
        var ingestionContext = await CreateIngestionContextAsync(request, client, cancellationToken);
        var startPage = await ResolveStartPageAsync(request, client, progress, cancellationToken);
        if (IsUnchangedSincePreviousScrape(request.PreviousProgress, startPage.Response.Meta))
        {
            progress.Report($"Skipping {request.LogLabel} - nothing has changed since the last scrape.");

            return Option.None<SearchCategoryProgress>();
        }

        return Option.Some(await IngestPagesAsync(request, ingestionContext, client, startPage, progress, cancellationToken));
    }

    private async Task<WallpaperIngestionContext> CreateIngestionContextAsync(PageScrapeRequest request, HttpClient client, CancellationToken cancellationToken)
    {
        var directories = await saveDirectoryResolver.ResolveSaveDirectoriesAsync(request.CategoryName, cancellationToken);
        var categoryLabel = request.CategoryName.Match(name => name, () => "Top Wallpapers");

        return new WallpaperIngestionContext(directories, client, unitOfWork.GetRepository<FileEntity, FileId>(), categoryLabel, request.PersonCategories);
    }

    private async Task<FetchedPage> ResolveStartPageAsync(PageScrapeRequest request, HttpClient client, IProgress<string> progress, CancellationToken cancellationToken)
    {
        var resumePage = ResumePage(request.PreviousProgress);
        var fetched = await FetchPageAsync(request, client, resumePage, progress, cancellationToken);
        if (resumePage > 1 && !IsSameCategoryAsPreviousScrape(request.PreviousProgress, fetched.Response.Meta)) fetched = await FetchPageAsync(request, client, 1, progress, cancellationToken);

        return fetched;
    }

    private async Task<SearchCategoryProgress> IngestPagesAsync(PageScrapeRequest request, WallpaperIngestionContext ingestionContext, HttpClient client, FetchedPage startPage, IProgress<string> progress, CancellationToken cancellationToken)
    {
        var current = startPage;
        while (true)
        {
            await IngestPageAsync(request, ingestionContext, current, progress, cancellationToken);
            if (IsLastPageToVisit(current)) return new SearchCategoryProgress(current.Response.Meta.Total, current.Number, current.Response.Meta.LastPage);

            current = await FetchPageAsync(request, client, current.Number + 1, progress, cancellationToken);
        }
    }

    private async Task IngestPageAsync(PageScrapeRequest request, WallpaperIngestionContext ingestionContext, FetchedPage page, IProgress<string> progress, CancellationToken cancellationToken)
    {
        await wallpaperIngestionService.IngestPageAsync(page.Response.Data, ingestionContext, progress, cancellationToken);
        request.OnPageCompleted(new SearchCategoryProgress(page.Response.Meta.Total, page.Number, page.Response.Meta.LastPage));
        _ = await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private async Task<FetchedPage> FetchPageAsync(PageScrapeRequest request, HttpClient client, int page, IProgress<string> progress, CancellationToken cancellationToken)
        => new(page, await pageFetcher.FetchPageAsync(request.LogLabel, request.PageUrlFactory(page), page, client, progress, cancellationToken));

    private bool IsLastPageToVisit(FetchedPage page)
        => page.Number >= page.Response.Meta.LastPage || page.Number >= limits.MaximumPagesPerSearch;

    private int ResumePage(Option<SearchCategoryProgress> previousProgress)
        => previousProgress.Match(
            previous => previous.LastPageVisited > 0 && !IsComplete(previous) ? previous.LastPageVisited + 1 : 1,
            () => 1);

    private bool IsComplete(SearchCategoryProgress previous)
        => previous.LastPageVisited >= Math.Min(previous.TotalPages, limits.MaximumPagesPerSearch);

    private static bool IsSameCategoryAsPreviousScrape(Option<SearchCategoryProgress> previousProgress, Meta meta)
        => previousProgress.Match(
            previous => previous.LastKnownImageCount == meta.Total && previous.TotalPages == meta.LastPage,
            () => false);

    private bool IsUnchangedSincePreviousScrape(Option<SearchCategoryProgress> previousProgress, Meta meta)
        => previousProgress.Match(
            previous => IsSameCategoryAsPreviousScrape(previousProgress, meta) && IsComplete(previous),
            () => false);

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
