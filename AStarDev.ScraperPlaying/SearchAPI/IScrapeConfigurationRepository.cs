using AStarDev.FunctionalParadigm;

namespace AStarDev.ScraperPlaying.SearchAPI;

/// <summary>
///  Represents a repository for scrape configuration settings repository.
/// </summary>
public interface IScrapeConfigurationRepository
{
    /// <summary>
    ///  Retrieves the scrape configuration settings.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation. The task result contains the scrape configuration settings.</returns>
    Task<Exceptional<ScrapeConfiguration>> GetScrapeConfigurationAsync();
}
