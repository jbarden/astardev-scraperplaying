using AStarDev.ControlDb;
using AStarDev.ControlDb.FileDetail;
using AStarDev.FunctionalParadigm;
using AStarDev.ScraperPlaying.WallpaperIngestion;
using Microsoft.EntityFrameworkCore;
using Microsoft.Playwright;

namespace AStarDev.ScraperPlaying.Scraping;

/// <summary>
/// Fetches wallhaven.cc search-result pages by driving a real browser via Playwright instead of
/// calling the JSON API. Owns the paging loop: for each listing page it scrapes the wallpaper ids,
/// skips those already downloaded, hands the rest to <see cref="IWallpaperIngestor"/>, and saves the page.
/// </summary>
/// <inheritdoc/>
public class WebsitePagesProcessor(IPlaywrightBrowserSession browserSession, IListingPageScraper listingPageScraper, IFilesQuery filesQuery, IWallpaperIngestor wallpaperIngestor, IUnitOfWork unitOfWork) : IPagesProcessor
{
    private const int MaxPagesPerSearch = 4;

    /// <inheritdoc/>
    public async Task FetchAndProcessPagesAsync(string logLabel, Option<string> categoryName, Func<int, string> pageUrlFactory, WallhavenConnection connection, IProgress<string> progress, CancellationToken cancellationToken)
    {
        try
        {
            var page = await browserSession.GetPageAsync(connection.UseHeadless, cancellationToken);
            var context = new PageIngestionContext(page, connection.BaseUrl, categoryName, categoryName.Match(name => name, () => "Top Wallpapers"), unitOfWork.GetRepository<FileEntity, FileId>());
            var pageNumber = 1;

            int wallpaperCount;
            do
            {
                var request = new ListingPageRequest(logLabel, pageNumber, new Uri(connection.BaseUrl, pageUrlFactory(pageNumber)));
                var wallpaperIds = await listingPageScraper.ScrapeWallpaperIdsAsync(request, page, progress, cancellationToken);
                wallpaperCount = wallpaperIds.Length;
                progress.Report($"Found {wallpaperCount} wallpaper(s) on {logLabel} page {pageNumber}.");

                foreach (var wallpaperId in await ExcludeAlreadyDownloadedAsync(wallpaperIds, progress, cancellationToken))
                {
                    await wallpaperIngestor.IngestAsync(wallpaperId, context, progress, cancellationToken);
                }

                await unitOfWork.SaveChangesAsync(cancellationToken);
                pageNumber++;
            } while (wallpaperCount > 0 && pageNumber <= MaxPagesPerSearch);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            progress.Report("Scrape cancelled.");
            await SavePartiallyIngestedPageAsync(progress);

            throw;
        }
        catch (PlaywrightException ex)
        {
            progress.Report($"An error occurred navigating the website during {logLabel}: {ex.Message}");

            throw;
        }
    }

    private async Task<string[]> ExcludeAlreadyDownloadedAsync(string[] wallpaperIds, IProgress<string> progress, CancellationToken cancellationToken)
        => (await filesQuery.GetExistingHandlesAsync([.. wallpaperIds.Select(FileHandle.Create)], cancellationToken))
            .Match(
                existing =>
                {
                    foreach (var wallpaperId in wallpaperIds.Where(id => existing.Contains(FileHandle.Create(id)))) progress.Report($"Wallpaper {wallpaperId} was already downloaded - skipping.");

                    return wallpaperIds.Where(id => !existing.Contains(FileHandle.Create(id))).ToArray();
                },
                exception =>
                {
                    progress.Report($"Failed to check which wallpapers were already downloaded, skipping this page: {exception.Message}");

                    return [];
                });

    private async Task SavePartiallyIngestedPageAsync(IProgress<string> progress)
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
