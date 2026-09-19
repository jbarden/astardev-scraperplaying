namespace AStarDev.ScraperPlaying.Scraping;

/// <summary>Interface for the scrape service that handles running the scraper.</summary>
public interface IScrapeService
{
    /// <summary>Runs the scraper asynchronously, reporting progress through the provided progress reporter.</summary>
    /// <param name="progress">The progress reporter to report the scraping progress.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task RunScraperAsync(IProgress<string> progress);

    /// <summary>Checks the current scrape configuration's root directory exists on disk, and that its famous root directory is configured and exists.</summary>
    /// <returns>A message for each problem found, empty when both root directories are usable.</returns>
    Task<IReadOnlyList<string>> ValidateRootDirectoriesAsync();
}
