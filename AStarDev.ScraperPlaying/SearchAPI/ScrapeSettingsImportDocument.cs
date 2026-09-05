namespace AStarDev.ScraperPlaying.SearchAPI;

internal sealed class ScrapeSettingsImportDocument
{
    public ScrapeSettingsImportConfiguration ScrapeConfiguration { get; init; } = new();
}

internal sealed class ScrapeSettingsImportConfiguration
{
    public ScrapeSettingsUserConfiguration UserConfiguration { get; init; } = new();
    public ScrapeSettingsSearchConfiguration SearchConfiguration { get; init; } = new();
    public ScrapeSettingsDirectories ScrapeDirectories { get; init; } = new();
}

internal sealed class ScrapeSettingsUserConfiguration
{
    public string LoginEmailAddress { get; init; } = string.Empty;
    public string Username { get; init; } = string.Empty;
    public string Password { get; init; } = string.Empty;
}

internal sealed class ScrapeSettingsSearchConfiguration
{
    public Uri BaseUrl { get; init; } = new("https://example.com");
    public Uri LoginUrl { get; init; } = new("https://example.com/login");
    public List<SearchCategoryImportDocument> SearchCategories { get; init; } = [];
    public string SearchString { get; init; } = string.Empty;
    public string TopWallpapers { get; init; } = string.Empty;
    public string SearchStringPrefix { get; init; } = string.Empty;
    public string SearchStringSuffix { get; init; } = string.Empty;
    public string Subscriptions { get; init; } = string.Empty;
    public int ImagePauseInSeconds { get; init; }
    public int StartingPageNumber { get; init; }
    public int TotalPages { get; init; }
    public bool UseHeadless { get; init; }
    public float? SlowMotionDelay { get; init; }
    public int SubscriptionsStartingPageNumber { get; init; }
    public int SubscriptionsTotalPages { get; init; }
    public int TopWallpapersTotalPages { get; init; }
    public int TopWallpapersStartingPageNumber { get; init; }
}

internal sealed class ScrapeSettingsDirectories
{
    public string BaseSaveDirectory { get; init; } = string.Empty;
    public string BaseDirectory { get; init; } = string.Empty;
    public string BaseDirectoryFamous { get; init; } = string.Empty;
    public string SubDirectoryName { get; init; } = string.Empty;
}