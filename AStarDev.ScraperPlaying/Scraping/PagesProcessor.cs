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
public class PagesProcessor(IWallhavenClientFactory clientFactory, IWallhavenPageFetcher pageFetcher, IUnitOfWork unitOfWork, ISaveDirectoryResolver saveDirectoryResolver, IWallpaperIngestionService wallpaperIngestionService, ScrapeLimits limits) : IPagesProcessor
{
    /// <inheritdoc/>
    public async Task<Option<SearchCategoryProgress>> FetchAndProcessPagesAsync(string logLabel, Option<string> categoryName, Option<SearchCategoryProgress> previousProgress, Action<SearchCategoryProgress> onPageCompleted, Func<int, Uri> pageUrlFactory, WallhavenConnection connection, IReadOnlyList<string> personCategories, IProgress<string> progress, CancellationToken cancellationToken)
    {
        var client = clientFactory.Create(connection);
        try
        {
            var page = ResumePage(previousProgress);
            var fileRepository = unitOfWork.GetRepository<FileEntity, FileId>();
            var directories = await saveDirectoryResolver.ResolveSaveDirectoriesAsync(categoryName, cancellationToken);
            var categoryLabel = categoryName.Match(name => name, () => "Top Wallpapers");
            var ingestionContext = new WallpaperIngestionContext(directories, client, fileRepository, categoryLabel, personCategories);
            var pageResult = await pageFetcher.FetchPageAsync(logLabel, pageUrlFactory(page), page, client, progress, cancellationToken);
            if (page > 1 && !IsSameCategoryAsPreviousScrape(previousProgress, pageResult.Meta))
            {
                page = 1;
                pageResult = await pageFetcher.FetchPageAsync(logLabel, pageUrlFactory(page), page, client, progress, cancellationToken);
            }

            if (IsUnchangedSincePreviousScrape(previousProgress, pageResult.Meta))
            {
                progress.Report($"Skipping {logLabel} - nothing has changed since the last scrape.");

                return Option.None<SearchCategoryProgress>();
            }

            while (true)
            {
                await wallpaperIngestionService.IngestPageAsync(pageResult.Data, ingestionContext, progress, cancellationToken);
                onPageCompleted(new SearchCategoryProgress(pageResult.Meta.Total, page, pageResult.Meta.LastPage));
                _ = await unitOfWork.SaveChangesAsync(cancellationToken);
                page++;
                if (page > pageResult.Meta.LastPage || page > limits.MaximumPagesPerSearch) break;

                pageResult = await pageFetcher.FetchPageAsync(logLabel, pageUrlFactory(page), page, client, progress, cancellationToken);
            }

            return Option.Some(new SearchCategoryProgress(pageResult.Meta.Total, page - 1, pageResult.Meta.LastPage));
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
}
