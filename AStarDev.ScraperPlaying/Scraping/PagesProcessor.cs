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
    public async Task<Option<SearchCategoryProgress>> FetchAndProcessPagesAsync(string logLabel, Option<string> categoryName, Option<SearchCategoryProgress> previousProgress, Func<int, Uri> pageUrlFactory, WallhavenConnection connection, IReadOnlyList<string> personCategories, IProgress<string> progress, CancellationToken cancellationToken)
    {
        var client = clientFactory.Create(connection);
        try
        {
            var page = 1;
            var fileRepository = unitOfWork.GetRepository<FileEntity, FileId>();
            var directory = await saveDirectoryResolver.ResolveSaveDirectoryAsync(categoryName, cancellationToken);
            var categoryLabel = categoryName.Match(name => name, () => "Top Wallpapers");
            var ingestionContext = new WallpaperIngestionContext(directory, client, fileRepository, categoryLabel, personCategories);
            SearchResponse pageResult;
            do
            {
                pageResult = await pageFetcher.FetchPageAsync(logLabel, pageUrlFactory(page), page, client, progress, cancellationToken);
                if (page == 1 && IsUnchangedSincePreviousScrape(previousProgress, pageResult.Meta))
                {
                    progress.Report($"Skipping {logLabel} - nothing has changed since the last scrape.");

                    return Option.None<SearchCategoryProgress>();
                }

                await wallpaperIngestionService.IngestPageAsync(pageResult.Data, ingestionContext, progress, cancellationToken);

                _ = await unitOfWork.SaveChangesAsync(cancellationToken);
                page++;
            } while (page <= pageResult.Meta.LastPage && page <= limits.MaximumPagesPerSearch);

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

    private bool IsUnchangedSincePreviousScrape(Option<SearchCategoryProgress> previousProgress, Meta meta)
        => previousProgress.Match(
            previous => previous.LastKnownImageCount == meta.Total && previous.TotalPages == meta.LastPage && previous.LastPageVisited >= Math.Min(meta.LastPage, limits.MaximumPagesPerSearch),
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
