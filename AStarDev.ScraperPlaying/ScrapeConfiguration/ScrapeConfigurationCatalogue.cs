using AStarDev.ControlDb;
using AStarDev.ControlDb.ScrapeConfiguration;
using AStarDev.FunctionalParadigm;
using AStarDev.ScraperPlaying.Scoping;

namespace AStarDev.ScraperPlaying.ScrapeConfiguration;

/// <inheritdoc/>
public sealed class ScrapeConfigurationCatalogue(IScopedRunner scopedRunner) : IScrapeConfigurationCatalogue
{
    /// <inheritdoc/>
    public async Task<Exceptional<IReadOnlyList<ScrapeConfigurationSummary>>> ListAsync() =>
        (await scopedRunner.RunAsync<IScrapeConfigurationLookup, Exceptional<IReadOnlyList<ScrapeConfigurationSummary>>>(async lookup =>
            (await lookup.ListHeadersAsync()).Match(
                headers => (Exceptional<IReadOnlyList<ScrapeConfigurationSummary>>)new Success<IReadOnlyList<ScrapeConfigurationSummary>>([.. headers.Select(ScrapeConfigurationSummary.From)]),
                exception => exception)));

    /// <inheritdoc/>
    public Task<Exceptional<Option<ScrapeConfigurationEntity>>> FindAsync(ScrapeConfigurationId id) =>
        scopedRunner.RunAsync<IUnitOfWork, Exceptional<Option<ScrapeConfigurationEntity>>>(unitOfWork =>
            unitOfWork.GetRepository<ScrapeConfigurationEntity, ScrapeConfigurationId>().TryFindAsync(id));
}
