namespace AStarDev.ScraperPlaying.ScrapeConfiguration;

/// <inheritdoc/>
public sealed class ScrapeConfigurationImportService(IScrapeConfigurationImporter repository, IScrapeConfigurationFileReader fileReader) : IScrapeConfigurationImportService
{
    /// <inheritdoc/>
    public async Task ImportAsync(string filePath, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var document = await fileReader.ReadAsync(filePath, cancellationToken);
        cancellationToken.ThrowIfCancellationRequested();
        await repository.ImportScrapeConfigurationAsync(document);
    }
}
