
using AStarDev.ControlDb;
using AStarDev.ControlDb.ScrapeConfiguration;
using AStarDev.FunctionalParadigm;

namespace AStarDev.ScraperPlaying.ScrapeConfiguration;

/// <summary>Replaces the persisted scrape configuration with an imported one.</summary>
/// <param name="unitOfWork">The unit of work used to read, replace and save the scrape configuration.</param>
public sealed class ScrapeConfigurationImporter(IUnitOfWork unitOfWork) : IScrapeConfigurationImporter
{
    /// <inheritdoc/>
    public async Task<Exceptional<Unit>> ImportScrapeConfigurationAsync(ScrapeConfigurationImportDocument document)
    {
        var dbContext = unitOfWork.GetRepository<ScrapeConfigurationEntity, ScrapeConfigurationId>();

        return await Try.RunAsync(async () =>
        {
            var current = (await dbContext.TryGetFirstAsync()).Match(option => option, exception => throw exception);

            // Delete and add are saved separately (the replacement can reuse the current configuration's ids) but in one transaction, so a failed import keeps the current configuration.
            await unitOfWork.InTransactionAsync(async () =>
            {
                await current.MatchAsync(
                    async e =>
                    {
                        dbContext.Delete(e).Match(unit => unit, exception => throw exception);
                        await unitOfWork.SaveChangesAsync();
                    },
                    () => { });

                dbContext.Add(document.ToEntity()).Match(entity => entity, exception => throw exception);
                await unitOfWork.SaveChangesAsync();
            });

            return Unit.Instance;
        });
    }
}
