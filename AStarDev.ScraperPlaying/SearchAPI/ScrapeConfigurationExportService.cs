using System.Text.Json;
using AStarDev.FunctionalParadigm;

namespace AStarDev.ScraperPlaying.SearchAPI;

public interface IScrapeConfigurationExportService
{
    Task<bool> ExportAsync(string filePath, CancellationToken cancellationToken = default);
}

public sealed class ScrapeConfigurationExportService(
    IScrapeConfigurationExporter repository,
    IScrapeConfigurationFileWriter fileWriter) : IScrapeConfigurationExportService
{
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

public interface IScrapeConfigurationFileWriter
{
    Task WriteAsync(ScrapeConfigurationImportDocument document, string filePath, CancellationToken cancellationToken = default);
}

public sealed class ScrapeConfigurationFileWriter : IScrapeConfigurationFileWriter
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web) { WriteIndented = true };

    public async Task WriteAsync(ScrapeConfigurationImportDocument document, string filePath, CancellationToken cancellationToken = default)
    {
        await using var stream = File.Create(filePath);
        await JsonSerializer.SerializeAsync(stream, document, JsonOptions, cancellationToken);
    }
}
