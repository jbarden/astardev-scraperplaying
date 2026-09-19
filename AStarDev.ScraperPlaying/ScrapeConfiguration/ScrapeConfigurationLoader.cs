using AStarDev.ControlDb;
using AStarDev.ControlDb.ScrapeConfiguration;
using AStarDev.FunctionalParadigm;

namespace AStarDev.ScraperPlaying.ScrapeConfiguration;

/// <summary>Loads the single stored scrape configuration.</summary>
public static class ScrapeConfigurationLoader
{
    /// <summary>Loads the stored scrape configuration.</summary>
    /// <param name="unitOfWork">The unit of work used to read the configuration.</param>
    /// <returns>The scrape configuration.</returns>
    /// <exception cref="InvalidOperationException">Thrown when no scrape configuration is stored.</exception>
    public static async Task<ScrapeConfigurationEntity> LoadAsync(IUnitOfWork unitOfWork)
        => (await unitOfWork.GetRepository<ScrapeConfigurationEntity, ScrapeConfigurationId>().TryGetFirstAsync())
            .Match(
                option => option.Match(scrapeConfig => scrapeConfig, () => throw new InvalidOperationException("Scrape configuration not found")),
                exception => throw exception);
}
