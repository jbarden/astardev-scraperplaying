namespace AStarDev.ScraperPlaying.ScrapeConfiguration;

/// <summary>Interface for importing the scrape configuration from, and exporting it to, a file at a known path.</summary>
public interface IScrapeConfigurationTransferService
{
    /// <summary>Imports the scrape configuration from the file at <paramref name="filePath"/>, replacing the stored one.</summary>
    /// <param name="filePath">The path of the file to import.</param>
    /// <param name="cancellationToken">The cancellation token to cancel the operation.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task ImportAsync(string filePath, CancellationToken cancellationToken);

    /// <summary>Exports the stored scrape configuration to the file at <paramref name="filePath"/>.</summary>
    /// <param name="filePath">The path of the file to write.</param>
    /// <param name="cancellationToken">The cancellation token to cancel the operation.</param>
    /// <returns>A task containing whether a scrape configuration existed to export.</returns>
    Task<bool> ExportAsync(string filePath, CancellationToken cancellationToken);
}
