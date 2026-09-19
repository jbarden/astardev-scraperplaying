namespace AStarDev.ScraperPlaying.ScrapeConfiguration;

/// <summary>Interface for importing a scrape configuration from a file.</summary>
public interface IScrapeConfigurationImportService
{
    /// <summary>Reads the scrape configuration file at <paramref name="filePath"/> and replaces the persisted configuration with it.</summary>
    /// <param name="filePath">The path of the file to import.</param>
    /// <param name="cancellationToken">The cancellation token to cancel the operation.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task ImportAsync(string filePath, CancellationToken cancellationToken = default);
}
