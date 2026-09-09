using AStarDev.FunctionalParadigm;

namespace AStarDev.ScraperPlaying.SearchAPI;

/// <summary>
///  Represents a service for importing scrape configuration settings.
/// </summary>
public interface IScrapeConfigurationImporter
{
    /// <summary>Replaces the persisted scrape configuration aggregate.</summary>
    Task<Exceptional<UnitFp>> ImportScrapeConfigurationAsync(ScrapeConfigurationImportDocument document);
}
