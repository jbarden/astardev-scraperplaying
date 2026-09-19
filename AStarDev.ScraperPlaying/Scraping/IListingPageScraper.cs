using Microsoft.Playwright;

namespace AStarDev.ScraperPlaying.Scraping;

/// <summary>Interface for scraping the wallpaper ids from a search-result listing page.</summary>
public interface IListingPageScraper
{
    /// <summary>Waits for the pacing delay, navigates to the listing page and reads the id of every wallpaper on it.</summary>
    /// <param name="request">The listing page to scrape.</param>
    /// <param name="page">The browser page used to navigate.</param>
    /// <param name="progress">The progress reporter to report navigation progress.</param>
    /// <param name="cancellationToken">The cancellation token to cancel the operation.</param>
    /// <returns>The wallpaper ids found on the page, empty when there are none.</returns>
    Task<string[]> ScrapeWallpaperIdsAsync(ListingPageRequest request, IPage page, IProgress<string> progress, CancellationToken cancellationToken);
}
