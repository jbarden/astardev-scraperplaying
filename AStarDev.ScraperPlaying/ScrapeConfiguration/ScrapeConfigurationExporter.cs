using AStarDev.ControlDb;
using AStarDev.ControlDb.ScrapeConfiguration;
using AStarDev.FunctionalParadigm;
using AStarDev.ScraperPlaying.Scoping;

namespace AStarDev.ScraperPlaying.ScrapeConfiguration;

/// <summary>Represents a repository for reading the scrape configuration settings aggregate for export.</summary>
/// <param name="unitOfWork">The unit of work used to obtain the scrape configuration repository.</param>
public sealed class ScrapeConfigurationExporter(IScopedRunner scopedRunner) : IScrapeConfigurationExporter
{
    /// <inheritdoc/>
    public async Task<Exceptional<Option<ScrapeConfigurationImportDocument>>> ExportScrapeConfigurationAsync(ApiKeyExport apiKeys, CancellationToken cancellationToken = default)
        =>
        await scopedRunner.RunAsync<IUnitOfWork, Exceptional<Option<ScrapeConfigurationImportDocument>>>(unitOfWork => Try.RunAsync(async () =>
        {
            var dbContext = unitOfWork.GetRepository<ScrapeConfigurationEntity, ScrapeConfigurationId>();
            var current = (await dbContext.TryGetFirstAsync(cancellationToken)).Match(option => option, exception => throw exception);

            return current.Match(entity => (Option<ScrapeConfigurationImportDocument>)entity.ToImportDocument(apiKeys), () => Option<ScrapeConfigurationImportDocument>.None.Instance);
        }));
}
