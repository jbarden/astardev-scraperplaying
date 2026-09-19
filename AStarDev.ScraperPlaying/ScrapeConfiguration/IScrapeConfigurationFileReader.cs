namespace AStarDev.ScraperPlaying.ScrapeConfiguration;

/// <summary>Interface for reading a scrape configuration file.</summary>
public interface IScrapeConfigurationFileReader
{
    /// <summary>Reads and validates the scrape configuration file at <paramref name="filePath"/>, accepting either an exported configuration or application settings.</summary>
    /// <param name="filePath">The path of the file to read.</param>
    /// <param name="cancellationToken">The cancellation token to cancel the operation.</param>
    /// <returns>The configuration document read from the file.</returns>
    Task<ScrapeConfigurationImportDocument> ReadAsync(string filePath, CancellationToken cancellationToken = default);
}
