using AStarDev.ControlDb;
using AStarDev.ControlDb.ScrapeConfiguration;
using AStarDev.FunctionalParadigm;

namespace AStarDev.ScraperPlaying.SearchAPI;

/// <summary>
/// Represents a repository for reading the scrape configuration settings aggregate for export.
/// </summary>
/// <param name="unitOfWork">The unit of work used to obtain the scrape configuration repository.</param>
public sealed class ScrapeConfigurationExporter(IUnitOfWork unitOfWork) : IScrapeConfigurationExporter
{
    /// <inheritdoc/>
    public async Task<Exceptional<Option<ScrapeConfigurationImportDocument>>> ExportScrapeConfigurationAsync()
    {
        var dbContext = unitOfWork.GetRepository<ScrapeConfigurationEntity, ScrapeConfigurationId>();

        return await Try.RunAsync(async () =>
        {
            var current = (await dbContext.TryGetFirstAsync()).Match(option => option, exception => throw exception);

            return current.Match(entity => (Option<ScrapeConfigurationImportDocument>)entity.ToImportDocument(), () => Option<ScrapeConfigurationImportDocument>.None.Instance);
        });
    }
}
