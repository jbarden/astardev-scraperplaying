using AStarDev.ControlDb;
using AStarDev.ControlDb.ScrapeConfiguration;
using AStarDev.FunctionalParadigm;

namespace AStarDev.ScraperPlaying.ScrapeConfiguration;

/// <summary>Loading of the scrape configuration from a unit of work.</summary>
public static class ScrapeConfigurationLoading
{
    extension(IUnitOfWork unitOfWork)
    {
        /// <summary>Loads the scrape configuration, with everything a scrape needs.</summary>
        /// <returns>The scrape configuration.</returns>
        /// <exception cref="InvalidOperationException">No scrape configuration exists.</exception>
        public async Task<ScrapeConfigurationEntity> LoadScrapeConfigurationAsync()
            => (await unitOfWork.GetRepository<ScrapeConfigurationEntity, ScrapeConfigurationId>().TryGetFirstAsync())
                .Match(
                    option => option.Match(scrapeConfig => scrapeConfig, () => throw new InvalidOperationException("Scrape configuration not found")),
                    exception => throw exception);
    }
}
