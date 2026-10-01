
using AStarDev.ControlDb;
using AStarDev.ControlDb.ScrapeConfiguration;
using AStarDev.FunctionalParadigm;
using AStarDev.ScraperPlaying.Scoping;

namespace AStarDev.ScraperPlaying.ScrapeConfiguration;

/// <summary>Represents a repository for scrape configuration settings repository.</summary>
/// <param name="dbContextFactory">The factory for creating instances of the ControlDbContext.</param>
public sealed class ScrapeConfigurationImporter(IScopedRunner scopedRunner) : IScrapeConfigurationImporter
{
    /// <inheritdoc/>
    public async Task<Exceptional<Unit>> ImportScrapeConfigurationAsync(ScrapeConfigurationImportDocument document, CancellationToken cancellationToken = default)
    {
        return await scopedRunner.RunAsync<IUnitOfWork, Exceptional<Unit>>(unitOfWork => Try.RunAsync(async () =>
        {
            var dbContext = unitOfWork.GetRepository<ScrapeConfigurationEntity, ScrapeConfigurationId>();

            // Map first: an unmappable document must not cost the user their current configuration.
            var replacement = document.ToEntity();
            var current = (await dbContext.TryGetFirstAsync(cancellationToken)).Match(option => option, exception => throw exception);

            // Exports leave the API keys out by default, so a file without them must not blank the keys already stored.
            _ = current.Match(existing => KeepStoredApiKeysWhereFileHasNone(replacement, existing), () => Unit.Instance);

            return await ReplaceAsync(unitOfWork, dbContext, current, replacement, cancellationToken);
        }));
    }

    // Delete and add commit together, so a failure while saving the replacement leaves the current configuration in place.
    private static Task<Unit> ReplaceAsync(IUnitOfWork unitOfWork, IRepository<ScrapeConfigurationEntity, ScrapeConfigurationId> repository, Option<ScrapeConfigurationEntity> current, ScrapeConfigurationEntity replacement, CancellationToken cancellationToken)
        => unitOfWork.InTransactionAsync(async () =>
        {
            await current.MatchAsync(
                async existing =>
                {
                    _ = repository.Delete(existing).Match(unit => unit, exception => throw exception);
                    _ = await unitOfWork.SaveChangesAsync(cancellationToken);
                },
                () => { });

            _ = repository.Add(replacement).Match(entity => entity, exception => throw exception);
            _ = await unitOfWork.SaveChangesAsync(cancellationToken);

            return Unit.Instance;
        }, cancellationToken);

    private static Unit KeepStoredApiKeysWhereFileHasNone(ScrapeConfigurationEntity replacement, ScrapeConfigurationEntity existing)
    {
        if (string.IsNullOrEmpty(replacement.ApiKey)) replacement.ApiKey = existing.ApiKey;
        if (string.IsNullOrEmpty(replacement.UserConfiguration.ApiKey)) replacement.UserConfiguration.ApiKey = existing.UserConfiguration.ApiKey;

        return Unit.Instance;
    }
}
