using AStarDev.ControlDb.ScrapeConfiguration;

namespace AStarDev.ScraperPlaying.Scraping;

/// <summary>Decides which searches a scrape runs for a configuration and runs them in order.</summary>
public interface ISearchOrchestrator
{
    /// <summary>Runs the configured category searches, then the top wallpapers search.</summary>
    /// <param name="configuration">The scrape configuration to search with.</param>
    /// <param name="progress">The progress reporter to report search progress.</param>
    /// <param name="cancellationToken">The cancellation token to cancel the operation.</param>
    Task RunSearchesAsync(ScrapeConfigurationEntity configuration, IProgress<string> progress, CancellationToken cancellationToken);
}
