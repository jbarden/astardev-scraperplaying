using AStarDev.FunctionalParadigm;

namespace AStarDev.ScraperPlaying.ScrapeConfiguration;

public sealed class ScrapeConfigurationExportService(
    IScrapeConfigurationExporter repository,
    IScrapeConfigurationFileWriter fileWriter) : IScrapeConfigurationExportService
{
    public async Task<bool> ExportAsync(string filePath, ApiKeyExport apiKeys, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var result = (await repository.ExportScrapeConfigurationAsync(apiKeys, cancellationToken)).Match(option => option, exception => throw exception);

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
