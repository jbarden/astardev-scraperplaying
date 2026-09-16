using Microsoft.Playwright;

namespace AStarDev.ScraperPlaying.Scraping;

/// <summary>Provides a single browser page reused across a whole scrape run.</summary>
public interface IPlaywrightBrowserSession
{
    /// <summary>
    /// Returns the session's page, lazily launching the browser, context, and page on the first call.
    /// Subsequent calls return the same page.
    /// </summary>
    /// <param name="useHeadless">Whether the browser should be launched headless.</param>
    /// <param name="cancellationToken">The cancellation token to cancel the launch.</param>
    Task<IPage> GetPageAsync(bool useHeadless, CancellationToken cancellationToken);
}
