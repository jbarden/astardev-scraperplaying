using Microsoft.Playwright;

namespace AStarDev.ScraperPlaying.Scraping;

/// <summary>Provides a single browser page reused across a whole scrape run.</summary>
public interface IPlaywrightBrowserSession
{
    /// <summary>
    /// Returns the session's page, lazily attaching to the already-running browser and resolving its
    /// context and page on the first call. Subsequent calls return the same page.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token to cancel the connection attempt.</param>
    Task<IPage> GetPageAsync(CancellationToken cancellationToken);
}
