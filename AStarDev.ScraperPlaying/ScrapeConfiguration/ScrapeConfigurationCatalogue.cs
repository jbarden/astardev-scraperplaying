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
        var repository = scope.ServiceProvider.GetRequiredService<IUnitOfWork>().GetRepository<ScrapeConfigurationEntity, ScrapeConfigurationId>();

        var result = await repository.TryGetAllAsync();

        return result.Match(
            option => (Exceptional<IReadOnlyList<ScrapeConfigurationSummary>>)new Success<IReadOnlyList<ScrapeConfigurationSummary>>(Summarise(option)),
            exception => exception);
    }

    /// <inheritdoc/>
    public async Task<Exceptional<Option<ScrapeConfigurationEntity>>> FindAsync(ScrapeConfigurationId id)
    {
        using var scope = scopeFactory.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IUnitOfWork>().GetRepository<ScrapeConfigurationEntity, ScrapeConfigurationId>();

        return await repository.TryFindAsync(id);
    }

    private static IReadOnlyList<ScrapeConfigurationSummary> Summarise(Option<IEnumerable<ScrapeConfigurationEntity>> option) =>
        option.Match<IReadOnlyList<ScrapeConfigurationSummary>>(
            entities => [.. entities.Select(ScrapeConfigurationSummary.From)],
            () => []);
}
