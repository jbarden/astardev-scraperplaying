namespace AStarDev.ScraperPlaying.Home;

/// <summary>
/// Interface for the scrape service that handles running the scraper.
/// </summary>
public interface IScrapeService
{
    /// <summary>
    /// Runs the scraper asynchronously, reporting progress through the provided progress reporter.
    /// </summary>
    /// <param name="progress">The progress reporter to report the scraping progress.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task RunScraperAsync(IProgress<string> progress);

    /// <summary>
    /// Checks whether the current scrape configuration's root directory exists on disk.
    /// </summary>
    /// <returns><see langword="true"/> if the root directory exists; otherwise, <see langword="false"/>.</returns>
    Task<bool> RootDirectoryExistsAsync();
}
