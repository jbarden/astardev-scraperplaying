using AStarDev.ControlDb.ScrapeConfiguration;

namespace AStarDev.ScraperPlaying.Scraping;

/// <summary>Interface for checking that the configured root directories can be used for a scrape.</summary>
public interface IRootDirectoryValidator
{
    /// <summary>Checks the configured root directory exists, and that the famous root directory is configured and exists.</summary>
    /// <param name="directories">The configured scrape directories.</param>
    /// <returns>A message for each problem found, empty when both root directories are usable.</returns>
    IReadOnlyList<string> Validate(ScrapeDirectoriesEntity directories);
}
