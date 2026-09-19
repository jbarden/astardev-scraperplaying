using AStarDev.FunctionalParadigm;

namespace AStarDev.ScraperPlaying.ScrapeConfiguration;

/// <inheritdoc/>
public sealed class ScrapeConfigurationExportService(IScrapeConfigurationExporter repository, IScrapeConfigurationFileWriter fileWriter) : IScrapeConfigurationExportService
{
    /// <inheritdoc/>
    public async Task<bool> ExportAsync(string filePath, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var result = (await repository.ExportScrapeConfigurationAsync()).Match(option => option, exception => throw exception);

        return await result.MatchAsync(
            async document =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                await fileWriter.WriteAsync(document, filePath, cancellationToken);

                return true;
            },
            () => false);
    }
}
