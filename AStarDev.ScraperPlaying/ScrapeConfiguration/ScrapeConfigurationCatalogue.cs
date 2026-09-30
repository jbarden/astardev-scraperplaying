using AStarDev.ControlDb;
using AStarDev.ControlDb.ScrapeConfiguration;
using AStarDev.FunctionalParadigm;
using Microsoft.Extensions.DependencyInjection;

namespace AStarDev.ScraperPlaying.ScrapeConfiguration;

/// <inheritdoc/>
public sealed class ScrapeConfigurationCatalogue(IServiceScopeFactory scopeFactory) : IScrapeConfigurationCatalogue
{
    /// <inheritdoc/>
    public async Task<Exceptional<IReadOnlyList<ScrapeConfigurationSummary>>> ListAsync()
    {
        using var scope = scopeFactory.CreateScope();
        var lookup = scope.ServiceProvider.GetRequiredService<IScrapeConfigurationLookup>();

        var result = await lookup.ListHeadersAsync();

        return result.Match(
            headers => (Exceptional<IReadOnlyList<ScrapeConfigurationSummary>>)new Success<IReadOnlyList<ScrapeConfigurationSummary>>([.. headers.Select(ScrapeConfigurationSummary.From)]),
            exception => exception);
    }

    /// <inheritdoc/>
    public async Task<Exceptional<Option<ScrapeConfigurationEntity>>> FindAsync(ScrapeConfigurationId id)
    {
        using var scope = scopeFactory.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IUnitOfWork>().GetRepository<ScrapeConfigurationEntity, ScrapeConfigurationId>();

        return await repository.TryFindAsync(id);
    }
}
