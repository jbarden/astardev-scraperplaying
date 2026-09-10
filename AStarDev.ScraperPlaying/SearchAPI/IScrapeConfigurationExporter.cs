using AStarDev.FunctionalParadigm;

namespace AStarDev.ScraperPlaying.SearchAPI;

/// <summary>
///  Represents a service for exporting scrape configuration settings.
/// </summary>
public interface IScrapeConfigurationExporter
{
    /// <summary>Reads the persisted scrape configuration aggregate, if one exists.</summary>
    Task<Exceptional<Option<ScrapeConfigurationImportDocument>>> ExportScrapeConfigurationAsync();
}
