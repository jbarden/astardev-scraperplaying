using System.ComponentModel.DataAnnotations;

namespace AStarDev.ScraperPlaying.Scraping;

/// <summary>Configuration for the Playwright browser session used to scrape wallhaven.cc.</summary>
public sealed record ScraperAppSettings
{
    /// <summary>The configuration section name this record binds to.</summary>
    public static string SectionName => "scraperAppConfiguration";

    /// <summary>
    /// The directory Playwright persists the Chromium browser profile in, so cookies and login
    /// state survive between scrape runs.
    /// </summary>
    [Required]
    public required string UserDataDirectory { get; init; }
}
