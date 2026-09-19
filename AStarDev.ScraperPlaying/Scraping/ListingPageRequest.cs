namespace AStarDev.ScraperPlaying.Scraping;

/// <summary>One search-result listing page to scrape.</summary>
/// <param name="LogLabel">A label used to identify the search in progress messages.</param>
/// <param name="PageNumber">The 1-based page number within the search.</param>
/// <param name="Url">The absolute URL of the listing page.</param>
public sealed record ListingPageRequest(string LogLabel, int PageNumber, Uri Url);
