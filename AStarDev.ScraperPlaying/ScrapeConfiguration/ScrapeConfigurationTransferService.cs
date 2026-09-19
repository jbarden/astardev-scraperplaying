using Microsoft.Extensions.DependencyInjection;

namespace AStarDev.ScraperPlaying.ScrapeConfiguration;

/// <inheritdoc/>
/// <remarks>Singleton: the import and export services use the scoped database context, so each operation resolves them from a scope of its own.</remarks>
public sealed class ScrapeConfigurationTransferService(IServiceScopeFactory scopeFactory) : IScrapeConfigurationTransferService
{
    /// <inheritdoc/>
    public async Task ImportAsync(string filePath, CancellationToken cancellationToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        await scope.ServiceProvider.GetRequiredService<IScrapeConfigurationImportService>().ImportAsync(filePath, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<bool> ExportAsync(string filePath, CancellationToken cancellationToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();

        return await scope.ServiceProvider.GetRequiredService<IScrapeConfigurationExportService>().ExportAsync(filePath, cancellationToken);
    }
}
