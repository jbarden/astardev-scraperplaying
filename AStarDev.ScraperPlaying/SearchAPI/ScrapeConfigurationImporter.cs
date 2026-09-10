
using AStarDev.ControlDb;
using AStarDev.ControlDb.ScrapeConfiguration;
using AStarDev.FunctionalParadigm;

namespace AStarDev.ScraperPlaying.SearchAPI;

/// <summary>
/// Represents a repository for scrape configuration settings repository.
/// </summary>
/// <param name="dbContextFactory">The factory for creating instances of the ControlDbContext.</param>
public sealed class ScrapeConfigurationImporter(IUnitOfWork unitOfWork) : IScrapeConfigurationImporter
{
    /// <inheritdoc/>
    public async Task<Exceptional<UnitFp>> ImportScrapeConfigurationAsync(ScrapeConfigurationImportDocument document)
    {
        var dbContext = unitOfWork.GetRepository<ScrapeConfigurationEntity, ScrapeConfigurationId>();

        return await Try.RunAsync(async () =>
        {
            var current = (await dbContext.TryGetFirstAsync()).Match(option => option, exception => throw exception);

            await current.MatchAsync(
                async e =>
                {
                    dbContext.Delete(e).Match(unit => unit, exception => throw exception);
                    await unitOfWork.SaveChangesAsync();
                },
                () => { });

            dbContext.Add(document.ToEntity()).Match(entity => entity, exception => throw exception);
            await unitOfWork.SaveChangesAsync();

            return UnitFp.Instance;
        });
    }
}
