namespace AStarDev.ScraperPlaying.Scraping;

/// <summary>The Wallhaven API credentials and target host a page-fetching request authenticates against.</summary>
/// <param name="ApiKey">The API key sent in the X-API-Key header.</param>
/// <param name="BaseUrl">The base URL of the website to fetch pages from.</param>
/// <param name="UseHeadless">Whether the Playwright browser used to fetch pages runs headless.</param>
public sealed record WallhavenConnection(string ApiKey, Uri BaseUrl, bool UseHeadless = false);
