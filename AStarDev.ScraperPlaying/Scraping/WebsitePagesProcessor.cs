using AStarDev.ScraperPlaying.WallpaperIngestion;
using Microsoft.Playwright;

namespace AStarDev.ScraperPlaying.Scraping;

/// <summary>
/// Fetches wallhaven.cc search-result pages by driving a real browser via Playwright instead of
/// calling the JSON API. Owns the paging loop: for each listing page it scrapes the wallpaper ids,
/// skips those already downloaded, hands the rest to <see cref="IWallpaperIngestor"/>, and saves the page.
/// </summary>
/// <inheritdoc/>
public class WebsitePagesProcessor(IPlaywrightBrowserSession browserSession, IListingPageScraper listingPageScraper, INewWallpaperFilter newWallpaperFilter, IWallpaperIngestor wallpaperIngestor, IIngestionStore ingestionStore) : IPagesProcessor
{
    private const int MaxPagesPerSearch = 4;

    /// <inheritdoc/>
    public async Task FetchAndProcessPagesAsync(SearchRequest request, WallhavenConnection connection, IProgress<string> progress, CancellationToken cancellationToken)
    {
        try
        {
            var page = await browserSession.GetPageAsync(connection.UseHeadless, cancellationToken);
            var context = new PageIngestionContext(page, connection.BaseUrl, request.CategoryName, request.CategoryLabel, ingestionStore.FileRepository);
            var pageNumber = 1;

            int wallpaperCount;
            do
            {
                var listingPage = new ListingPageRequest(request.LogLabel, pageNumber, new Uri(connection.BaseUrl, request.PageUrlFactory(pageNumber)));
                var wallpaperIds = await listingPageScraper.ScrapeWallpaperIdsAsync(listingPage, page, progress, cancellationToken);
                wallpaperCount = wallpaperIds.Length;

                foreach (var wallpaperId in await newWallpaperFilter.ExcludeAlreadyDownloadedAsync(wallpaperIds, progress, cancellationToken))
                {
                    await wallpaperIngestor.IngestAsync(wallpaperId, context, progress, cancellationToken);
                }

                await ingestionStore.SavePageAsync(cancellationToken);
                pageNumber++;
            } while (wallpaperCount > 0 && pageNumber <= MaxPagesPerSearch);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            progress.Report("Scrape cancelled.");
            await ingestionStore.SaveAfterCancellationAsync(progress);

            throw;
        }
        catch (PlaywrightException ex)
        {
            progress.Report($"An error occurred navigating the website during {request.LogLabel}: {ex.Message}");

            throw;
        }
    }
}
