using AStarDev.FunctionalParadigm;
using Microsoft.Playwright;

namespace AStarDev.ScraperPlaying.Scraping;

/// <summary>
/// Fetches wallhaven.cc search-result pages by driving a real browser via Playwright instead of
/// calling the JSON API. This is a basic, investigative implementation: it proves out navigating the
/// site's listing pages and identifying the wallpapers present, but it does not yet download images or
/// extract full wallpaper metadata - that requires visiting each wallpaper's own detail page.
/// </summary>
/// <inheritdoc/>
public class WebsitePagesProcessor(IPlaywrightBrowserSession browserSession, Func<TimeSpan> pacingDelay) : IPagesProcessor
{
    private const int MaxPagesPerSearch = 4;

    /// <inheritdoc/>
    public async Task FetchAndProcessPagesAsync(string logLabel, Option<string> categoryName, Func<int, string> pageUrlFactory, WallhavenConnection connection, IProgress<string> progress, CancellationToken cancellationToken)
    {
        try
        {
            var page = await browserSession.GetPageAsync(connection.UseHeadless, cancellationToken);
            var pageNumber = 1;
            await Task.Delay(pacingDelay(), cancellationToken);

            int wallpaperCount;
            do
            {
                wallpaperCount = await FetchPageAsync(logLabel, pageUrlFactory, pageNumber, page, connection.BaseUrl, progress, cancellationToken);
                await Task.Delay(pacingDelay(), cancellationToken);
                pageNumber++;
            } while (wallpaperCount > 0 && pageNumber <= MaxPagesPerSearch);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            progress.Report("Scrape cancelled.");

            throw;
        }
        catch (PlaywrightException ex)
        {
            progress.Report($"An error occurred navigating the website during {logLabel}: {ex.Message}");

            throw;
        }
    }

    private static async Task<int> FetchPageAsync(string logLabel, Func<int, string> pageUrlFactory, int pageNumber, IPage page, Uri baseUrl, IProgress<string> progress, CancellationToken cancellationToken)
    {
        progress.Report($"Navigating to {logLabel} page {pageNumber}.");
        var targetUrl = new Uri(baseUrl, pageUrlFactory(pageNumber));

        await page.GotoAsync(targetUrl.ToString(), new PageGotoOptions { WaitUntil = WaitUntilState.DOMContentLoaded }).WaitAsync(cancellationToken);

        var wallpaperIds = await page.Locator("figure.thumb")
            .EvaluateAllAsync<string[]>("elements => elements.map(element => element.dataset.wallpaperId)")
            .WaitAsync(cancellationToken);

        progress.Report(wallpaperIds.Length == 0
            ? $"No wallpapers found on {logLabel} page {pageNumber}."
            : $"Found {wallpaperIds.Length} wallpaper(s) on {logLabel} page {pageNumber}: {string.Join(", ", wallpaperIds)}.");

        return wallpaperIds.Length;
    }
}
