namespace AStarDev.ScraperPlaying.ScrapeConfiguration;

/// <summary>Interface for exporting the scrape configuration to a file.</summary>
public interface IScrapeConfigurationExportService
{
    /// <summary>Writes the persisted scrape configuration to <paramref name="filePath"/>.</summary>
    /// <param name="filePath">The path of the file to write.</param>
    /// <param name="cancellationToken">The cancellation token to cancel the operation.</param>
    /// <returns><see langword="true"/> if a configuration was written; <see langword="false"/> if none is persisted.</returns>
    Task<bool> ExportAsync(string filePath, CancellationToken cancellationToken = default);
}
