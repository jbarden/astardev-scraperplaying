
using AStarDev.ControlDb;
using AStarDev.ControlDb.ScrapeConfiguration;
using AStarDev.FunctionalParadigm;
using Microsoft.Extensions.DependencyInjection;

namespace AStarDev.ScraperPlaying.ScrapeConfiguration;

/// <summary>Represents a repository for scrape configuration settings repository.</summary>
/// <param name="dbContextFactory">The factory for creating instances of the ControlDbContext.</param>
public sealed class ScrapeConfigurationImporter(IServiceScopeFactory scopeFactory) : IScrapeConfigurationImporter
{
    /// <inheritdoc/>
    public async Task<Exceptional<Unit>> ImportScrapeConfigurationAsync(ScrapeConfigurationImportDocument document)
    {
        using var scope = scopeFactory.CreateScope();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var dbContext = unitOfWork.GetRepository<ScrapeConfigurationEntity, ScrapeConfigurationId>();

        return await Try.RunAsync(async () =>
        {
            // Map first: an unmappable document must not cost the user their current configuration.
            var replacement = document.ToEntity();
            var current = (await dbContext.TryGetFirstAsync()).Match(option => option, exception => throw exception);

            // Exports leave the API keys out by default, so a file without them must not blank the keys already stored.
            _ = current.Match(existing => KeepStoredApiKeysWhereFileHasNone(replacement, existing), () => Unit.Instance);

            // Delete and add commit together, so a failure while saving the replacement leaves the current configuration in place.
            return await unitOfWork.InTransactionAsync(async () =>
            {
                await current.MatchAsync(
                    async e =>
                    {
                        _ = dbContext.Delete(e).Match(unit => unit, exception => throw exception);
                        _ = await unitOfWork.SaveChangesAsync();
                    },
                    () => { });

                _ = dbContext.Add(replacement).Match(entity => entity, exception => throw exception);
                _ = await unitOfWork.SaveChangesAsync();

                return Unit.Instance;
            });
        });
    }

    private static Unit KeepStoredApiKeysWhereFileHasNone(ScrapeConfigurationEntity replacement, ScrapeConfigurationEntity existing)
    {
        if (string.IsNullOrEmpty(replacement.ApiKey)) replacement.ApiKey = existing.ApiKey;
        if (string.IsNullOrEmpty(replacement.UserConfiguration.ApiKey)) replacement.UserConfiguration.ApiKey = existing.UserConfiguration.ApiKey;

        return Unit.Instance;
    }
}
