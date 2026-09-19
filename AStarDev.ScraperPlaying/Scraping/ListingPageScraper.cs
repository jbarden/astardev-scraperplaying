using Microsoft.Playwright;

namespace AStarDev.ScraperPlaying.Scraping;

/// <inheritdoc/>
public class ListingPageScraper(Func<TimeSpan> pacingDelay) : IListingPageScraper
{
    /// <inheritdoc/>
    public async Task<string[]> ScrapeWallpaperIdsAsync(ListingPageRequest request, IPage page, IProgress<string> progress, CancellationToken cancellationToken)
    {
        await Task.Delay(pacingDelay(), cancellationToken);

        progress.Report($"Navigating to {request.LogLabel} page {request.PageNumber}.");
        await page.GotoAsync(request.Url.ToString(), new PageGotoOptions { WaitUntil = WaitUntilState.DOMContentLoaded }).WaitAsync(cancellationToken);

        var wallpaperIds = await page.Locator("figure.thumb")
            .EvaluateAllAsync<string[]>("elements => elements.map(element => element.dataset.wallpaperId)")
            .WaitAsync(cancellationToken);

        progress.Report(wallpaperIds.Length == 0
            ? $"No wallpapers found on {request.LogLabel} page {request.PageNumber}."
            : $"Found {wallpaperIds.Length} wallpaper(s) on {request.LogLabel} page {request.PageNumber}: {string.Join(", ", wallpaperIds)}.");

        return wallpaperIds;
    }
}
