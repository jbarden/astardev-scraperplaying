using System.ComponentModel.DataAnnotations;

namespace AStarDev.ScraperPlaying.Scraping;

/// <summary>Configuration for the Playwright browser session used to scrape wallhaven.cc.</summary>
public sealed record ScraperAppSettings
{
    /// <summary>The configuration section name this record binds to.</summary>
    public static string SectionName => "scraperAppConfiguration";

    /// <summary>
    /// The Chrome DevTools Protocol endpoint (e.g. "http://localhost:9222") Playwright attaches to.
    /// The browser must already be running under this endpoint, launched normally (not by Playwright)
    /// with a "--remote-debugging-port" flag, so it carries none of the automation flags a
    /// Playwright-launched browser would - and so it can be logged in and past any bot-detection
    /// challenge before a scrape starts.
    /// </summary>
    [Required]
    public required string CdpEndpointUrl { get; init; }
}
