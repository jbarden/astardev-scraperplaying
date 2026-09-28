namespace AStarDev.ScraperPlaying;

/// <summary>Application-wide constants shared across multiple types.</summary>
public static class ApplicationConstants
{
    /// <summary>The named <see cref="HttpClient"/> configured with the scraper's static headers via <c>AddHttpClient</c>.</summary>
    public const string WallhavenHttpClientName = "Wallhaven";

    /// <summary>The relative path, appended to a wallpaper id, of Wallhaven's per-wallpaper detail endpoint.</summary>
    public const string WallhavenDetailPathTemplate = "api/v1/w/";
}
