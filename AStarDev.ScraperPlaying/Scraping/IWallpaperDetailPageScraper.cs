using Microsoft.Playwright;

namespace AStarDev.ScraperPlaying.Scraping;

/// <summary>Interface for scraping a wallpaper's own detail page for its image and tags.</summary>
public interface IWallpaperDetailPageScraper
{
    /// <summary>Navigates to a wallpaper's detail page and scrapes its image URL, dimensions, and tags.</summary>
    /// <param name="wallpaperId">Wallhaven's id for the wallpaper.</param>
    /// <param name="page">The Playwright page to navigate and scrape with.</param>
    /// <param name="baseUrl">The target host the wallpaper's detail page is served from.</param>
    /// <param name="progress">The progress reporter to report scraping progress.</param>
    /// <param name="cancellationToken">The cancellation token to cancel the operation.</param>
    /// <returns>A task representing the asynchronous operation, containing the scraped <see cref="WallpaperDetail"/>.</returns>
    Task<WallpaperDetail> ScrapeAsync(string wallpaperId, IPage page, Uri baseUrl, IProgress<string> progress, CancellationToken cancellationToken);
}
