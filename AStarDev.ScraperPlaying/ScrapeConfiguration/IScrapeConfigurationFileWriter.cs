namespace AStarDev.ScraperPlaying.ScrapeConfiguration;

/// <summary>Interface for writing a scrape configuration file.</summary>
public interface IScrapeConfigurationFileWriter
{
    /// <summary>Writes <paramref name="document"/> to <paramref name="filePath"/> as indented JSON, replacing any existing file.</summary>
    /// <param name="document">The configuration document to write.</param>
    /// <param name="filePath">The path of the file to write.</param>
    /// <param name="cancellationToken">The cancellation token to cancel the operation.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task WriteAsync(ScrapeConfigurationImportDocument document, string filePath, CancellationToken cancellationToken = default);
}
